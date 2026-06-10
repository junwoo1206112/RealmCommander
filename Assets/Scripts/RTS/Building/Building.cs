using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using RealmCommander.Core;

namespace RealmCommander.RTS
{
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

        [SyncVar(hook = nameof(OnHealthChanged))]
        private float currentHealth;
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

        public event Action<float, float> OnHealthChangedEvent;
        public event Action OnDeath;
        public event Action OnSelected;
        public event Action OnDeselected;
        public event Action<UnitProductionData> OnProductionStarted;
        public event Action<UnitProductionData> OnProductionCompleted;

        private void Awake()
        {
            if (GetComponent<NetworkIdentity>() == null)
            {
                gameObject.AddComponent<NetworkIdentity>();
            }

            if (isServer)
            {
                currentHealth = maxHealth;
            }

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }

            UpdateBuildingColor();
        }

        protected override void OnValidate() { }

        public override void OnStartServer()
        {
            base.OnStartServer();
            currentHealth = maxHealth;
        }

        private void Start()
        {
            if (isServer || isOwned || !NetworkServer.active)
            {
                SelectionManager.Instance?.RegisterSelectableUnit(gameObject);
            }

            if (CommandManager.Instance != null)
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
            if (!isServer && !isOwned && NetworkServer.active) return;
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
        public void CmdQueueProduction(UnitProductionData data)
        {
            if (data == null) return;

            if (ResourceManager.Instance == null) return;

            if (!ResourceManager.Instance.CanAfford(data.goldCost, data.manaCost))
            {
                Debug.Log("자원이 부족합니다!");
                return;
            }

            ResourceManager.Instance.SpendGold(data.goldCost);
            ResourceManager.Instance.SpendMana(data.manaCost);

            currentProduction.Enqueue(data);
            RpcOnProductionStarted(data.unitName);

            Debug.Log($"{data.unitName} 생산 시작!");
        }

        public void QueueProduction(UnitProductionData data)
        {
            if (isServer)
            {
                InternalQueueProduction(data);
            }
            else
            {
                CmdQueueProduction(data);
            }
        }

        private void InternalQueueProduction(UnitProductionData data)
        {
            if (data == null) return;

            if (ResourceManager.Instance == null) return;

            if (!ResourceManager.Instance.CanAfford(data.goldCost, data.manaCost))
            {
                Debug.Log("자원이 부족합니다!");
                return;
            }

            ResourceManager.Instance.SpendGold(data.goldCost);
            ResourceManager.Instance.SpendMana(data.manaCost);

            currentProduction.Enqueue(data);
            OnProductionStarted?.Invoke(data);

            Debug.Log($"{data.unitName} 생산 시작!");
        }

        [ClientRpc]
        private void RpcOnProductionStarted(string unitName)
        {
            Debug.Log($"{unitName} 생산 시작!");
        }

        private void ProduceUnit(UnitProductionData data)
        {
            if (data.unitPrefab == null)
            {
                Debug.LogError("Unit Prefab이 설정되지 않았습니다!");
                return;
            }

            Vector3 spawnPosition = GetSpawnPosition();
            GameObject unit = Instantiate(data.unitPrefab, spawnPosition, Quaternion.identity);
            unit.name = data.unitName;

            NetworkServer.Spawn(unit);

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
            if (!isServer && !isOwned && NetworkServer.active) return;
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

            if (buildingRenderer != null)
            {
                buildingRenderer.material.color = Color.black;
            }

            if (isServer)
            {
                RpcOnDestroyed();
                NetworkServer.Destroy(gameObject);
            }

            Debug.Log($"{buildingName} 파괴됨!");
        }

        [ClientRpc]
        private void RpcOnDestroyed()
        {
            if (buildingRenderer != null)
            {
                buildingRenderer.material.color = Color.black;
            }
        }

        private void UpdateBuildingColor()
        {
            if (buildingRenderer != null)
            {
                buildingRenderer.material.color = buildingColor;
            }
        }

        private void OnMouseDown()
        {
            if (!isServer && !isOwned && NetworkServer.active) return;

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
