using System;
using System.Collections.Generic;
using UnityEngine;
using RealmCommander.Core;

namespace RealmCommander.RTS
{
    public class Building : MonoBehaviour
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

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;
        public event Action OnSelected;
        public event Action OnDeselected;
        public event Action<UnitProductionData> OnProductionStarted;
        public event Action<UnitProductionData> OnProductionCompleted;

        private void Awake()
        {
            currentHealth = maxHealth;

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }

            UpdateBuildingColor();
        }

        private void Start()
        {
            SelectionManager.Instance?.RegisterSelectableUnit(gameObject);
            CommandManager.Instance.OnAttackCommand += HandleAttackCommand;
        }

        private void OnDestroy()
        {
            SelectionManager.Instance?.UnregisterSelectableUnit(gameObject);

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
                ProduceUnit(currentItem);
                OnProductionCompleted?.Invoke(currentItem);
            }
        }

        public void SetSelected(bool selected)
        {
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

        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Repair(float amount)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void StartConstruction()
        {
            isConstructing = true;
            constructionProgress = 0;
        }

        public void QueueProduction(UnitProductionData data)
        {
            if (data == null) return;

            if (!ResourceManager.Instance.CanAfford(data.goldCost, data.manaCost))
            {
                Debug.Log("자원이 부족합니다!");
                return;
            }

            if (!ResourceManager.Instance.SpendGold(data.goldCost)) return;
            if (!ResourceManager.Instance.SpendMana(data.manaCost)) return;

            currentProduction.Enqueue(data);
            OnProductionStarted?.Invoke(data);

            Debug.Log($"{data.unitName} 생산 시작!");
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

            Debug.Log($"{data.unitName} 생산 완료!");
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
            if (!SelectionManager.Instance.IsUnitSelected(gameObject)) return;

            if (target != null && target == gameObject)
            {
                Debug.Log("건물은 공격할 수 없습니다!");
            }
        }

        private void Die()
        {
            OnDeath?.Invoke();
            SelectionManager.Instance?.UnregisterSelectableUnit(gameObject);

            if (buildingRenderer != null)
            {
                buildingRenderer.material.color = Color.black;
            }

            Debug.Log($"{buildingName} 파괴됨!");
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
            if (Input.GetMouseButtonUp(0))
            {
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
