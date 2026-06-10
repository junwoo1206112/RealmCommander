using System;
using UnityEngine;
using Mirror;

namespace RealmCommander.RTS
{
    public class ResourceManager : NetworkBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [Header("Starting Resources")]
        [SerializeField] private float startingGold = 500f;
        [SerializeField] private float startingMana = 100f;

        [Header("Resource Generation")]
        [SerializeField] private float goldPerSecond = 1f;
        [SerializeField] private float manaPerSecond = 0.5f;

        [SyncVar(hook = nameof(OnGoldChanged))]
        private float currentGold;
        [SyncVar(hook = nameof(OnManaChanged))]
        private float currentMana;
        private float maxMana = 200f;

        public float CurrentGold => currentGold;
        public float CurrentMana => currentMana;
        public float MaxMana => maxMana;

        public event Action<float, float> OnGoldChangedEvent;
        public event Action<float, float> OnManaChangedEvent;

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
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            currentGold = startingGold;
            currentMana = startingMana;
        }

        private void Update()
        {
            if (!NetworkServer.active || netIdentity == null) return;
            GenerateResources();
        }

        private void GenerateResources()
        {
            AddGold(goldPerSecond * Time.deltaTime);
            AddMana(manaPerSecond * Time.deltaTime);
        }

        [Server]
        public bool SpendGold(float amount)
        {
            if (currentGold >= amount)
            {
                currentGold -= amount;
                return true;
            }
            return false;
        }

        [Server]
        public bool SpendMana(float amount)
        {
            if (currentMana >= amount)
            {
                currentMana -= amount;
                return true;
            }
            return false;
        }

        [Server]
        public void AddGold(float amount)
        {
            currentGold += amount;
        }

        [Server]
        public void AddMana(float amount)
        {
            currentMana = Mathf.Min(maxMana, currentMana + amount);
        }

        public void SetMaxMana(float newMax)
        {
            maxMana = newMax;
            if (isServer)
            {
                currentMana = Mathf.Min(currentMana, maxMana);
            }
        }

        public bool CanAfford(float goldCost, float manaCost)
        {
            return currentGold >= goldCost && currentMana >= manaCost;
        }

        private void OnGoldChanged(float oldValue, float newValue)
        {
            OnGoldChangedEvent?.Invoke(newValue, oldValue);
        }

        private void OnManaChanged(float oldValue, float newValue)
        {
            OnManaChangedEvent?.Invoke(newValue, oldValue);
        }
    }
}
