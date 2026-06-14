using System.Collections.Generic;
using UnityEngine;

namespace RealmCommander.Core
{
    public class EntityRegistry : MonoBehaviour
    {
        public static EntityRegistry Instance { get; private set; }

        private readonly List<RTS.Unit> allUnits = new List<RTS.Unit>();
        private readonly List<RTS.Building> allBuildings = new List<RTS.Building>();

        private float lastCleanupTime;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (Time.time - lastCleanupTime >= 5f)
            {
                lastCleanupTime = Time.time;
                Cleanup();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Register(RTS.Unit unit)
        {
            if (unit != null && !allUnits.Contains(unit))
                allUnits.Add(unit);
        }

        public void Unregister(RTS.Unit unit)
        {
            allUnits.Remove(unit);
        }

        public void Register(RTS.Building building)
        {
            if (building != null && !allBuildings.Contains(building))
                allBuildings.Add(building);
        }

        public void Unregister(RTS.Building building)
        {
            allBuildings.Remove(building);
        }

        public IReadOnlyList<RTS.Unit> AllUnits => allUnits;
        public IReadOnlyList<RTS.Building> AllBuildings => allBuildings;

        public void Cleanup()
        {
            allUnits.RemoveAll(u => u == null);
            allBuildings.RemoveAll(b => b == null);
        }

        public void GetAliveFriendlyUnits(List<RTS.Unit> results)
        {
            results.Clear();
            for (int i = 0; i < allUnits.Count; i++)
            {
                var u = allUnits[i];
                if (u != null && u.IsAlive && !u.IsEnemy)
                    results.Add(u);
            }
        }

        public void GetAliveEnemyUnits(List<RTS.Unit> results)
        {
            results.Clear();
            for (int i = 0; i < allUnits.Count; i++)
            {
                var u = allUnits[i];
                if (u != null && u.IsAlive && u.IsEnemy)
                    results.Add(u);
            }
        }

        public void GetAliveUnits(List<RTS.Unit> results)
        {
            results.Clear();
            for (int i = 0; i < allUnits.Count; i++)
            {
                if (allUnits[i] != null && allUnits[i].IsAlive)
                    results.Add(allUnits[i]);
            }
        }

        public void GetAliveBuildings(List<RTS.Building> results)
        {
            results.Clear();
            for (int i = 0; i < allBuildings.Count; i++)
            {
                if (allBuildings[i] != null && allBuildings[i].IsAlive)
                    results.Add(allBuildings[i]);
            }
        }

        public void GetSelectableUnits(List<GameObject> results)
        {
            results.Clear();
            for (int i = 0; i < allUnits.Count; i++)
            {
                var u = allUnits[i];
                if (u != null && u.IsAlive && u.CanIssueLocalCommands)
                    results.Add(u.gameObject);
            }
        }
    }
}
