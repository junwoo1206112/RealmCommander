using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using RealmCommander.Core;
using RealmCommander.Network;
using UnityEngine.AI;

namespace RealmCommander.RTS
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class Building : NetworkBehaviour
    {
        [Header("Building Settings")]
        [SerializeField] private string buildingName;
        [SerializeField] private BuildingType buildingType;
        [SerializeField] private float maxHealth = 500f;
        [SerializeField] private float constructionTime = 5f;

        [Header("Combat")]
        [SerializeField] private float attackDamage;
        [SerializeField] private float attackRange = 5f;
        [SerializeField] private float attackSpeed = 1.5f;

        [Header("Production")]
        [SerializeField] private List<UnitProductionData> productionQueue = new List<UnitProductionData>();
        [SerializeField] private float productionRange = 5f;

        [Header("Visual")]
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private Renderer buildingRenderer;
        [SerializeField] private Color buildingColor = Color.gray;
        [SerializeField] private Color selectedColor = Color.cyan;
        [SerializeField] private global::RealmCommander.Visuals.WorldModelVisual worldVisual;
        [SerializeField] private global::RealmCommander.Visuals.WorldHealthBar healthBar;
        private MaterialPropertyBlock colorBlock;

        [SyncVar(hook = nameof(OnHealthChanged))]
        private float currentHealth;
        [SyncVar(hook = nameof(OnTeamChanged))]
        [SerializeField, Range(0, 1)] private int teamId;
        [SyncVar]
        private bool syncIsConstructing;
        [SyncVar]
        private float syncConstructionProgress;
        [SyncVar]
        private string syncCurrentProductName = "";
        [SyncVar]
        private float syncProductionProgress;
        [SyncVar]
        private bool syncIsProducing;
        private bool isSelected;
        private bool isConstructing;
        private float constructionProgress;
        private Queue<UnitProductionData> currentProduction = new Queue<UnitProductionData>();
        private float productionTimer;
        private float lastAttackTime;

        public string BuildingName => buildingName;
        public BuildingType BuildingType => buildingType;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;
        public bool IsAlive => currentHealth > 0;
        public bool IsSelected => isSelected;
        public bool IsConstructing => isServer ? isConstructing : syncIsConstructing;
        public bool IsProducing => isServer ? currentProduction.Count > 0 : syncIsProducing;
        public float ProductionRange => productionRange;
        public int TeamId => teamId;
        public bool CanIssueLocalCommands => !NetworkClient.active ||
            (NetworkPlayer.Local != null && NetworkPlayer.Local.TeamId == teamId);

        public float GetProductionProgress()
        {
            if (isServer)
            {
                if (currentProduction.Count == 0) return 0f;
                var current = currentProduction.Peek();
                return current.productionTime > 0f ? productionTimer / current.productionTime : 0f;
            }
            return syncProductionProgress;
        }

        public float GetProductionTimeRemaining()
        {
            if (isServer)
            {
                if (currentProduction.Count == 0) return 0f;
                var current = currentProduction.Peek();
                return Mathf.Max(0f, current.productionTime - productionTimer);
            }
            return 0f;
        }

        public string GetCurrentProductName()
        {
            return isServer
                ? (currentProduction.Count > 0 ? currentProduction.Peek().unitName : "")
                : syncCurrentProductName;
        }

        public event Action<float, float> OnHealthChangedEvent;
        public event Action OnDeath;
        public event Action OnSelected;
        public event Action OnDeselected;
        public event Action<UnitProductionData> OnProductionStarted;
        public event Action<UnitProductionData> OnProductionCompleted;

        private void Awake()
        {
            buildingRenderer ??= GetComponent<Renderer>();
            EnsureDefaultProductionQueue();

            if (!NetworkClient.active)
            {
                currentHealth = maxHealth;
            }

            if (selectionIndicator == null)
            {
                selectionIndicator = CreateSelectionIndicator();
                selectionIndicator.SetActive(false);
            }
            else
            {
                selectionIndicator.SetActive(false);
            }

            NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle == null) obstacle = gameObject.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
            obstacle.carvingMoveThreshold = 0.1f;

            UpdateBuildingColor();
            ApplyWorldArt();
            EnsureHealthBar();
        }

        protected override void OnValidate() { }

        private GameObject CreateSelectionIndicator()
        {
            GameObject indicatorObject = new GameObject("SelectionIndicator");
            indicatorObject.transform.SetParent(transform, false);
            indicatorObject.AddComponent<SelectionIndicator>();
            return indicatorObject;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            currentHealth = maxHealth;
            EnsureDefaultProductionQueue();
            if (connectionToClient != null && connectionToClient.identity != null)
            {
                NetworkPlayer owner = connectionToClient.identity.GetComponent<NetworkPlayer>();
                if (owner != null) teamId = owner.TeamId;
            }
            UpdateBuildingColor();
        }

        private void Start()
        {
            SelectionManager.Instance?.RegisterSelectableUnit(gameObject);
            EntityRegistry.Instance?.Register(this);
        }

        private void OnDestroy()
        {
            EntityRegistry.Instance?.Unregister(this);
            SelectionManager.Instance?.UnregisterSelectableUnit(gameObject);
        }

        private void Update()
        {
            if (!IsAlive) return;
            if (netIdentity == null) return;
            if (NetworkClient.active && !isServer && !NetworkServer.active) return;

            if (isConstructing)
            {
                UpdateConstruction();
            }

            if (currentProduction.Count > 0)
            {
                UpdateProduction();
            }

            if (attackDamage > 0f && isServer)
            {
                AutoAttack();
            }
        }

        [Server]
        private void AutoAttack()
        {
            if (attackDamage <= 0f || Time.time - lastAttackTime < attackSpeed) return;

            var registry = EntityRegistry.Instance;
            if (registry == null) return;

            float closestDist = attackRange;
            GameObject closestTarget = null;

            foreach (var unit in registry.AllUnits)
            {
                if (unit == null || !unit.IsAlive) continue;
                if (unit.IsEnemy == (teamId == 1)) continue;
                float dist = Vector3.Distance(transform.position, unit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestTarget = unit.gameObject;
                }
            }

            if (closestTarget != null)
            {
                lastAttackTime = Time.time;
                var combat = Network.CombatManager.Instance;
                if (combat != null)
                {
                    combat.ApplyCombatDamage(gameObject, closestTarget, attackDamage);
                }
                else
                {
                    var unit = closestTarget.GetComponent<Unit>();
                    unit?.TakeDamage(attackDamage);
                }
            }
        }

        private void UpdateConstruction()
        {
            constructionProgress += Time.deltaTime;
            syncConstructionProgress = constructionProgress;

            if (constructionProgress >= constructionTime)
            {
                isConstructing = false;
                syncIsConstructing = false;
                constructionProgress = 0;
                syncConstructionProgress = 0;
                Audio.AudioManager.Instance?.PlayBuildingComplete();
                Debug.Log($"{buildingName} 건설 완료!");
            }
        }

        private void UpdateProduction()
        {
            if (currentProduction.Count == 0) return;

            productionTimer += Time.deltaTime;
            var currentItem = currentProduction.Peek();

            syncCurrentProductName = currentItem.unitName;
            syncProductionProgress = currentItem.productionTime > 0f ? productionTimer / currentItem.productionTime : 0f;

            if (productionTimer >= currentItem.productionTime)
            {
                productionTimer = 0;
                currentProduction.Dequeue();

                if (currentProduction.Count > 0)
                {
                    var next = currentProduction.Peek();
                    syncCurrentProductName = next.unitName;
                    syncProductionProgress = 0f;
                }
                else
                {
                    syncCurrentProductName = "";
                    syncProductionProgress = 0f;
                    syncIsProducing = false;
                }

                if (isServer)
                {
                    ProduceUnit(currentItem);
                }
                OnProductionCompleted?.Invoke(currentItem);
            }
        }

        public void SetSelected(bool selected)
        {
            if (!CanIssueLocalCommands) return;
            if (isSelected == selected) return;
            isSelected = selected;
            if (selectionIndicator != null)
            {
                SelectionIndicator indicator = selectionIndicator.GetComponent<SelectionIndicator>();
                if (indicator != null)
                    indicator.SetSelected(selected);
                else
                    selectionIndicator.SetActive(selected);
            }

            if (selected)
            {
                OnSelected?.Invoke();
            }
            else
            {
                OnDeselected?.Invoke();
            }
        }

        [Server]
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        [Server]
        public void Repair(float amount)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }

        [Server]
        public void StartConstruction()
        {
            isConstructing = true;
            syncIsConstructing = true;
            constructionProgress = 0;
            syncConstructionProgress = 0;
        }

        [Command]
        public void CmdQueueProduction(int productionIndex)
        {
            if (productionIndex < 0 || productionIndex >= productionQueue.Count) return;
            InternalQueueProduction(productionQueue[productionIndex]);
        }

        public void QueueProduction(UnitProductionData data)
        {
            if (isServer || NetworkServer.active)
            {
                InternalQueueProduction(data);
            }
            else
            {
                int productionIndex = productionQueue.IndexOf(data);
                if (productionIndex >= 0)
                    CmdQueueProduction(productionIndex);
            }
        }

        private void InternalQueueProduction(UnitProductionData data)
        {
            if (data == null) return;

            if (ResourceManager.Instance == null) return;

            if (!ResourceManager.Instance.TrySpend(teamId, data.goldCost, data.manaCost))
            {
                Debug.Log("자원이 부족합니다!");
                return;
            }

            currentProduction.Enqueue(data);

            if (currentProduction.Count == 1)
            {
                syncCurrentProductName = data.unitName;
                syncProductionProgress = 0f;
                syncIsProducing = true;
            }

            OnProductionStarted?.Invoke(data);
            RpcOnProductionStarted(data.unitName);

            Debug.Log($"{data.unitName} 생산 시작!");
        }

        [ClientRpc]
        private void RpcOnProductionStarted(string unitName)
        {
            Debug.Log($"{unitName} 생산 시작!");
        }

        private void ProduceUnit(UnitProductionData data)
        {
            if (!IsAlive) return;
            if (data.unitPrefab == null)
            {
                Debug.LogError("Unit Prefab이 설정되지 않았습니다!");
                return;
            }

            Vector3 spawnPosition = GetSpawnPosition();
            if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, productionRange, NavMesh.AllAreas))
                spawnPosition = hit.position;

            GameObject unit = Instantiate(data.unitPrefab, spawnPosition, Quaternion.identity);
            unit.name = data.unitName;

            Unit unitComponent = unit.GetComponent<Unit>();
            unitComponent?.ConfigureTeam(teamId == 1);

            if (!string.IsNullOrEmpty(data.specId) && unitComponent != null)
                unitComponent.ApplySpec(data.specId);

            NetworkConnectionToClient owner = Network.NetworkUtils.FindTeamConnection(teamId);
            if (owner != null) NetworkServer.Spawn(unit, owner);
            else NetworkServer.Spawn(unit);

            RpcOnUnitSpawned(spawnPosition);

            Debug.Log($"{data.unitName} 생산 완료!");
            Audio.AudioManager.Instance?.PlayUnitSpawn();
        }

        [ClientRpc]
        private void RpcOnUnitSpawned(Vector3 position)
        {
            Debug.Log($"Unit spawned at {position}");
        }

        private Vector3 GetSpawnPosition()
        {
            Vector3 direction = transform.forward;
            Vector3 spawnPos = transform.position + direction * productionRange;

            spawnPos.y = 0.5f;

            return spawnPos;
        }

        private void OnHealthChanged(float oldValue, float newValue)
        {
            OnHealthChangedEvent?.Invoke(newValue, maxHealth);
        }

        private void Die()
        {
            OnDeath?.Invoke();

            if (isServer || isOwned || !NetworkServer.active)
            {
                SelectionManager.Instance?.UnregisterSelectableUnit(gameObject);
            }

            ApplyBuildingColor(Color.black);

            if (isServer)
            {
                RpcOnDestroyed();
                StartCoroutine(DestroyAfterFrame());
            }

            Debug.Log($"{buildingName} 파괴됨!");
        }

        private System.Collections.IEnumerator DestroyAfterFrame()
        {
            yield return null;
            if (gameObject != null && NetworkServer.active)
                NetworkServer.Destroy(gameObject);
        }

        [ClientRpc]
        private void RpcOnDestroyed()
        {
            ApplyBuildingColor(Color.black);
        }

        private void ApplyBuildingColor(Color color)
        {
            if (buildingRenderer == null) return;
            if (colorBlock == null)
                colorBlock = new MaterialPropertyBlock();
            colorBlock.SetColor("_Color", color);
            buildingRenderer.SetPropertyBlock(colorBlock);
        }

        private void UpdateBuildingColor()
        {
            ApplyBuildingColor(teamId == 1 ? Color.red : buildingColor);
        }

        private void ApplyWorldArt()
        {
            worldVisual ??= GetComponent<global::RealmCommander.Visuals.WorldModelVisual>();
            if (worldVisual == null)
                worldVisual = gameObject.AddComponent<global::RealmCommander.Visuals.WorldModelVisual>();

            worldVisual.ApplyBuilding(buildingType, teamId == 1);
        }

        private void EnsureHealthBar()
        {
            healthBar ??= GetComponent<global::RealmCommander.Visuals.WorldHealthBar>();
            if (healthBar == null)
                healthBar = gameObject.AddComponent<global::RealmCommander.Visuals.WorldHealthBar>();
            healthBar.SetLayout(new Vector3(0f, 2.25f, 0f), new Vector2(1.6f, 0.11f));
        }

        [Server]
        public void ConfigureTeam(int newTeamId)
        {
            teamId = Mathf.Clamp(newTeamId, 0, 1);
            if (currentHealth <= 0f)
                currentHealth = maxHealth;
            UpdateBuildingColor();
            ApplyWorldArt();
        }

        [Server]
        public void ConfigureRuntimeBuilding(string newName, BuildingType newType, int newTeamId)
        {
            buildingName = string.IsNullOrWhiteSpace(newName) ? newType.ToString() : newName;
            buildingType = newType;
            if (buildingType != BuildingType.Base && buildingType != BuildingType.Barracks && buildingType != BuildingType.RangedBarracks)
                productionQueue.Clear();
            ConfigureTeam(newTeamId);
            EnsureDefaultProductionQueue();
        }

        [Server]
        public void ApplySpec(string specId)
        {
            var spec = OpenSpec.SpecManager.Instance?.GetSpec("buildings", specId);
            if (spec == null) return;

            maxHealth = OpenSpec.SpecManager.Instance.GetProperty("buildings", specId, "MaxHealth", maxHealth);
            constructionTime = OpenSpec.SpecManager.Instance.GetProperty("buildings", specId, "BuildTime", constructionTime);
            currentHealth = maxHealth;

            Debug.Log($"[Building] Applied spec '{specId}': HP={maxHealth}, BuildTime={constructionTime}");
        }

        private void OnTeamChanged(int oldValue, int newValue)
        {
            UpdateBuildingColor();
            ApplyWorldArt();
        }

        private void OnMouseDown()
        {
            if (!CanIssueLocalCommands) return;
            SelectionHelper.HandleSelection(gameObject, isSelected);
        }

        public IReadOnlyList<UnitProductionData> GetProductionQueue()
        {
            return productionQueue;
        }

        private void EnsureDefaultProductionQueue()
        {
            if (productionQueue == null)
                productionQueue = new List<UnitProductionData>();
            if (productionQueue.Count > 0) return;

            if (buildingType == BuildingType.DefenseTower)
            {
                attackDamage = 15f;
                attackRange = 6f;
                attackSpeed = 1.5f;
                return;
            }

            if (buildingType != BuildingType.Base && buildingType != BuildingType.Barracks && buildingType != BuildingType.RangedBarracks) return;

            GameObject unitPrefab = Resources.Load<GameObject>("Unit");
            if (unitPrefab == null) return;

            productionQueue.Add(new UnitProductionData
            {
                unitName = "Soldier",
                specId = "unit_soldier",
                unitPrefab = unitPrefab,
                productionTime = 3f,
                goldCost = 45f,
                manaCost = 0f
            });

            if (buildingType == BuildingType.Barracks || buildingType == BuildingType.RangedBarracks)
            {
                productionQueue.Add(new UnitProductionData
                {
                    unitName = "Archer",
                    specId = "unit_archer",
                    unitPrefab = unitPrefab,
                    productionTime = 4f,
                    goldCost = 65f,
                    manaCost = 5f
                });
                productionQueue.Add(new UnitProductionData
                {
                    unitName = "Mage",
                    specId = "unit_mage",
                    unitPrefab = unitPrefab,
                    productionTime = 5f,
                    goldCost = 80f,
                    manaCost = 20f
                });
            }
        }
    }

    [Serializable]
    public class UnitProductionData
    {
        public string unitName;
        public string specId;
        public GameObject unitPrefab;
        public float productionTime = 3f;
        public float goldCost = 50f;
        public float manaCost = 0f;
        public Sprite icon;
    }

    public enum BuildingType
    {
        Base,
        Barracks,
        RangedBarracks,
        MagicTower,
        ResourceGenerator,
        DefenseTower
    }
}
