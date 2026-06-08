using System;
using UnityEngine;

namespace RealmCommander.RTS
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [Header("Starting Resources")]
        [SerializeField] private float startingGold = 500f;
        [SerializeField] private float startingMana = 100f;

        [Header("Resource Generation")]
        [SerializeField] private float goldPerSecond = 1f;
        [SerializeField] private float manaPerSecond = 0.5f;

        private float currentGold;
        private float currentMana;
        private float maxMana = 200f;

        public float CurrentGold => currentGold;
        public float CurrentMana => currentMana;
        public float MaxMana => maxMana;

        public event Action<float, float> OnGoldChanged;
        public event Action<float, float> OnManaChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            currentGold = startingGold;
            currentMana = startingMana;
        }

        private void Update()
        {
            GenerateResources();
        }

        private void GenerateResources()
        {
            AddGold(goldPerSecond * Time.deltaTime);
            AddMana(manaPerSecond * Time.deltaTime);
        }

        public bool SpendGold(float amount)
        {
            if (currentGold >= amount)
            {
                currentGold -= amount;
                OnGoldChanged?.Invoke(currentGold, amount);
                return true;
            }
            return false;
        }

        public bool SpendMana(float amount)
        {
            if (currentMana >= amount)
            {
                currentMana -= amount;
                OnManaChanged?.Invoke(currentMana, amount);
                return true;
            }
            return false;
        }

        public void AddGold(float amount)
        {
            currentGold += amount;
            OnGoldChanged?.Invoke(currentGold, amount);
        }

        public void AddMana(float amount)
        {
            currentMana = Mathf.Min(maxMana, currentMana + amount);
            OnManaChanged?.Invoke(currentMana, amount);
        }

        public void SetMaxMana(float newMax)
        {
            maxMana = newMax;
            currentMana = Mathf.Min(currentMana, maxMana);
        }

        public bool CanAfford(float goldCost, float manaCost)
        {
            return currentGold >= goldCost && currentMana >= manaCost;
        }
    }
}
