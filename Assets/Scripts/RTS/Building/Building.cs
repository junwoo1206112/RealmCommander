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

        [Header("Production")]
        [SerializeField] private List<UnitProductionData> productionQueue = new List<UnitProductionData>();
        [SerializeField] private float productionRange = 5f;

        [Header("Visual")]
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private Renderer buildingRenderer;
        [SerializeField] private Color buildingColor = Color.gray;
        [SerializeField] private Color selectedColor = Color.cyan;
        private MaterialPropertyBlock colorBlock;

        [SyncVar(hook = nameof(OnHealthChanged))]
        private float currentHealth;
        [SyncVar(hook = nameof(OnTeamChanged))]
        [SerializeField, Range(0, 1)] private int teamId;
        private bool isSelected;
        private bool isConstructing;
        private float constructionProgress;
        private Queue<UnitProductionData> currentProduction = new Queue<UnitProductionData>();
        private float productionTimer;

        public string BuildingName => buildingName;
        public BuildingType BuildingType => buildingType;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercent => currentHealth / maxHealth;
        public bool IsAlive => currentHealth > 0;
        public bool IsSelected => isSelected;
        public bool IsConstructing => isConstructing;
        public bool IsProducing => currentProduction.Count > 0;
        public float ProductionRange => productionRange;
        public int TeamId => teamId;
        public bool CanIssueLocalCommands => !NetworkClient.active ||
            (NetworkPlayer.Local != null && NetworkPlayer.Local.teamId == teamId);

        public event Action<float, float> OnHealthChangedEvent;
        public event Action OnDeath;
        public event Action OnSelected;
        public event Action OnDeselected;
        public event Action<UnitProductionData> OnProductionStarted;
        public event Action<UnitProductionData> OnProductionCompleted;

        private void Awake()
        {
            if (!NetworkClient.active)
            {
                currentHealth = maxHealth;
            }

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }

            NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle == null) obstacle = gameObject.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
            obstacle.carvingMoveThreshold = 0.1f;

            UpdateBuildingColor();
        }

        protected override void OnValidate() { }

        public override void OnStartServer()
        {
            base.OnStartServer();
            currentHealth = maxHealth;
            if (connectionToClient != null && connectionToClient.identity != null)
            {
                NetworkPlayer owner = connectionToClient.identity.GetComponent<NetworkPlayer>();
                if (owner != null) teamId = owner.teamId;
            }
            else if (CompareTag("Enemy"))
            {
                teamId = 1;
            }
            UpdateBuildingColor();
        }

        private void Start()
        {
            if (CanIssueLocalCommands)
            {
                SelectionManager.Instance?.RegisterSelectableUnit(gameObject);
            }

            if (CanIssueLocalCommands && CommandManager.Instance != null)
            {
                CommandManager.Instance.OnAttackCommand += HandleAttackCommand;
            }
        }

        private void OnDestroy()
        {
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance?.UnregisterSelectableUnit(gameObject);
            }

            if (CommandManager.Instance != null)
            {
                CommandManager.Instance.OnAttackCommand -= HandleAttackCommand;
            }
        }

        private void Update()
        {
            if (!IsAlive) return;
            if (NetworkClient.active && !isServer) return;

            if (isConstructing)
            {
                UpdateConstruction();
            }

            if (currentProduction.Count > 0)
            {
                UpdateProduction();
            }
        }

        private void UpdateConstruction()
        {
            constructionProgress += Time.deltaTime;

            if (constructionProgress >= constructionTime)
            {
                isConstructing = false;
                constructionProgress = 0;
                Debug.Log($"{buildingName} 건설 완료!");
            }
        }

        private void UpdateProduction()
        {
            if (currentProduction.Count == 0) return;

            productionTimer += Time.deltaTime;
            var currentItem = currentProduction.Peek();

            if (productionTimer >= currentItem.productionTime)
            {
                productionTimer = 0;
                currentProduction.Dequeue();
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
            constructionProgress = 0;
        }

        [Command]
        public void CmdQueueProduction(int productionIndex)
        {
            if (productionIndex < 0 || productionIndex >= productionQueue.Count) return;
            InternalQueueProduction(productionQueue[productionIndex]);
        }

        public void QueueProduction(UnitProductionData data)
        {
            if (isServer)
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

            NetworkConnectionToClient owner = FindTeamConnection(teamId);
            if (owner != null) NetworkServer.Spawn(unit, owner);
            else NetworkServer.Spawn(unit);

            RpcOnUnitSpawned(spawnPosition);

            Debug.Log($"{data.unitName} 생산 완료!");
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

        private void HandleAttackCommand(GameObject target)
        {
            if (!CanIssueLocalCommands) return;
            if (SelectionManager.Instance == null || !SelectionManager.Instance.IsUnitSelected(gameObject)) return;

            if (target != null && target == gameObject)
            {
                Debug.Log("건물은 공격할 수 없습니다!");
            }
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
            if (gameObject != null)
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

        [Server]
        public void ConfigureTeam(int newTeamId)
        {
            teamId = Mathf.Clamp(newTeamId, 0, 1);
            gameObject.tag = teamId == 1 ? "Enemy" : "Untagged";
            UpdateBuildingColor();
        }

        private void OnTeamChanged(int oldValue, int newValue)
        {
            UpdateBuildingColor();
        }

        [Server]
        private static NetworkConnectionToClient FindTeamConnection(int requestedTeamId)
        {
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                NetworkPlayer player = connection.identity != null
                    ? connection.identity.GetComponent<NetworkPlayer>()
                    : null;
                if (player != null && player.teamId == requestedTeamId)
                    return connection;
            }
            return null;
        }

        private void OnMouseDown()
        {
            if (!CanIssueLocalCommands || SelectionManager.Instance == null) return;
            if (RTS.BoxSelector.WasClickHandled) return;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (isSelected)
                {
                    SelectionManager.Instance.RemoveFromSelection(gameObject);
                }
                else
                {
                    SelectionManager.Instance.AddToSelection(gameObject);
                }
            }
            else
            {
                SelectionManager.Instance.SelectUnit(gameObject);
            }
        }

        public List<UnitProductionData> GetProductionQueue()
        {
            return productionQueue;
        }
    }

    [Serializable]
    public class UnitProductionData
    {
        public string unitName;
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
