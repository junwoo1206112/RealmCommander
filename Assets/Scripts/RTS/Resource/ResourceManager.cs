using System;
using Mirror;
using RealmCommander.Network;
using UnityEngine;

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
        [SerializeField] private float maxMana = 200f;

        [SyncVar(hook = nameof(OnTeam0GoldChanged))] private float team0Gold;
        [SyncVar(hook = nameof(OnTeam1GoldChanged))] private float team1Gold;
        [SyncVar(hook = nameof(OnTeam0ManaChanged))] private float team0Mana;
        [SyncVar(hook = nameof(OnTeam1ManaChanged))] private float team1Mana;

        public float CurrentGold => GetGold(GetLocalTeamId());
        public float CurrentMana => GetMana(GetLocalTeamId());
        public float MaxMana => maxMana;

        public event Action<float, float> OnGoldChangedEvent;
        public event Action<float, float> OnManaChangedEvent;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            team0Gold = team1Gold = startingGold;
            team0Mana = team1Mana = startingMana;
        }

        private void Update()
        {
            if (!NetworkServer.active) return;

            float deltaGold = goldPerSecond * Time.deltaTime;
            float deltaMana = manaPerSecond * Time.deltaTime;
            AddGold(0, deltaGold);
            AddGold(1, deltaGold);
            AddMana(0, deltaMana);
            AddMana(1, deltaMana);
        }

        public float GetGold(int teamId) => teamId == 1 ? team1Gold : team0Gold;
        public float GetMana(int teamId) => teamId == 1 ? team1Mana : team0Mana;

        public void RefreshLocalDisplay()
        {
            OnGoldChangedEvent?.Invoke(CurrentGold, 0f);
            OnManaChangedEvent?.Invoke(CurrentMana, 0f);
        }

        public bool CanAfford(float goldCost, float manaCost) =>
            CanAfford(GetLocalTeamId(), goldCost, manaCost);

        public bool CanAfford(int teamId, float goldCost, float manaCost)
        {
            if (goldCost < 0f || manaCost < 0f) return false;
            return GetGold(teamId) >= goldCost && GetMana(teamId) >= manaCost;
        }

        [Server]
        public bool TrySpend(int teamId, float goldCost, float manaCost)
        {
            if (!CanAfford(teamId, goldCost, manaCost)) return false;

            if (teamId == 1)
            {
                team1Gold -= goldCost;
                team1Mana -= manaCost;
            }
            else
            {
                team0Gold -= goldCost;
                team0Mana -= manaCost;
            }
            return true;
        }

        [Server] public bool SpendGold(float amount) => TrySpend(GetLocalTeamId(), amount, 0f);
        [Server] public bool SpendMana(float amount) => TrySpend(GetLocalTeamId(), 0f, amount);
        [Server] public void AddGold(float amount) => AddGold(GetLocalTeamId(), amount);
        [Server] public void AddMana(float amount) => AddMana(GetLocalTeamId(), amount);

        [Server]
        public void AddGold(int teamId, float amount)
        {
            if (amount <= 0f) return;
            if (teamId == 1) team1Gold += amount;
            else team0Gold += amount;
        }

        [Server]
        public void AddMana(int teamId, float amount)
        {
            if (amount <= 0f) return;
            if (teamId == 1) team1Mana = Mathf.Min(maxMana, team1Mana + amount);
            else team0Mana = Mathf.Min(maxMana, team0Mana + amount);
        }

        [Server]
        public void SetMaxMana(float newMax)
        {
            maxMana = Mathf.Max(0f, newMax);
            team0Mana = Mathf.Min(team0Mana, maxMana);
            team1Mana = Mathf.Min(team1Mana, maxMana);
        }

        private static int GetLocalTeamId()
        {
            return NetworkPlayer.Local != null ? NetworkPlayer.Local.teamId : 0;
        }

        private void OnTeam0GoldChanged(float oldValue, float newValue) => NotifyGoldChanged(0, oldValue, newValue);
        private void OnTeam1GoldChanged(float oldValue, float newValue) => NotifyGoldChanged(1, oldValue, newValue);
        private void OnTeam0ManaChanged(float oldValue, float newValue) => NotifyManaChanged(0, oldValue, newValue);
        private void OnTeam1ManaChanged(float oldValue, float newValue) => NotifyManaChanged(1, oldValue, newValue);

        private void NotifyGoldChanged(int teamId, float oldValue, float newValue)
        {
            if (teamId == GetLocalTeamId())
                OnGoldChangedEvent?.Invoke(newValue, newValue - oldValue);
        }

        private void NotifyManaChanged(int teamId, float oldValue, float newValue)
        {
            if (teamId == GetLocalTeamId())
                OnManaChangedEvent?.Invoke(newValue, newValue - oldValue);
        }
    }
}
