using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using RealmCommander.RTS;

namespace RealmCommander.AI
{
    public enum AIDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    public class AIController : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private AIDifficulty difficulty = AIDifficulty.Normal;
        [SerializeField] private float updateInterval = 1f;

        [Header("Unit Spawning")]
        [SerializeField] private GameObject[] unitPrefabs;
        [SerializeField] private float spawnInterval = 5f;
        [SerializeField] private int maxUnits = 10;

        [Header("References")]
        [SerializeField] private Transform baseTransform;
        [SerializeField] private float baseRadius = 5f;

        private List<GameObject> controlledUnits = new List<GameObject>();
        private Transform playerBase;
        private float lastUpdateTime;
        private float lastSpawnTime;

        public AIDifficulty Difficulty => difficulty;

        private void Start()
        {
            playerBase = FindPlayerBase();
        }

        private void Update()
        {
            if (!NetworkServer.active) return;

            if (Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                UpdateAI();
            }

            if (Time.time - lastSpawnTime >= GetSpawnInterval())
            {
                lastSpawnTime = Time.time;
                TrySpawnUnit();
            }

            CleanupDeadUnits();
        }

        private void UpdateAI()
        {
            if (playerBase == null)
            {
                playerBase = FindPlayerBase();
                return;
            }

            foreach (var unit in controlledUnits)
            {
                if (unit == null) continue;

                var unitComponent = unit.GetComponent<Unit>();
                if (unitComponent == null) continue;

                GameObject nearestEnemy = FindNearestEnemy(unit.transform.position);
                if (nearestEnemy != null)
                {
                    float distanceToEnemy = Vector3.Distance(unit.transform.position, nearestEnemy.transform.position);
                    if (distanceToEnemy < 15f)
                    {
                        unitComponent.SetTarget(nearestEnemy);
                        continue;
                    }
                }

                float distanceToBase = Vector3.Distance(unit.transform.position, playerBase.position);
                if (distanceToBase > 3f)
                {
                    var agent = unit.GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(playerBase.position);
                    }
                }
            }
        }

        private GameObject FindNearestEnemy(Vector3 position)
        {
            GameObject nearest = null;
            float nearestDistance = float.MaxValue;

            var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            foreach (var unit in allUnits)
            {
                if (unit.IsEnemy) continue;
                if (!unit.IsAlive) continue;

                float distance = Vector3.Distance(position, unit.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = unit.gameObject;
                }
            }

            return nearest;
        }

        private Transform FindPlayerBase()
        {
            var bases = FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in bases)
            {
                if (b.BuildingType == BuildingType.Base && b.IsAlive)
                {
                    return b.transform;
                }
            }

            foreach (var b in bases)
            {
                if (b.BuildingType == BuildingType.Base)
                {
                    return b.transform;
                }
            }

            return null;
        }

        private void TrySpawnUnit()
        {
            if (controlledUnits.Count >= maxUnits) return;
            if (unitPrefabs == null || unitPrefabs.Length == 0) return;

            GameObject prefab = unitPrefabs[Random.Range(0, unitPrefabs.Length)];

            Vector3 spawnPos = baseTransform != null
                ? baseTransform.position + Random.insideUnitSphere * baseRadius
                : transform.position + Random.insideUnitSphere * 3f;

            spawnPos.y = 0.5f;

            GameObject unit = Instantiate(prefab, spawnPos, Quaternion.identity);
            unit.name = $"AI_{prefab.name}_{controlledUnits.Count}";

            NetworkServer.Spawn(unit);

            var unitComponent = unit.GetComponent<Unit>();
            if (unitComponent != null)
            {
                ApplyDifficultyStats(unitComponent);
            }

            RegisterUnit(unit);
        }

        private void ApplyDifficultyStats(Unit unit)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    break;
                case AIDifficulty.Normal:
                    break;
                case AIDifficulty.Hard:
                    break;
            }
        }

        private float GetSpawnInterval()
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy: return spawnInterval * 1.5f;
                case AIDifficulty.Normal: return spawnInterval;
                case AIDifficulty.Hard: return spawnInterval * 0.6f;
                default: return spawnInterval;
            }
        }

        public void RegisterUnit(GameObject unit)
        {
            if (unit != null && !controlledUnits.Contains(unit))
            {
                controlledUnits.Add(unit);
            }
        }

        public void UnregisterUnit(GameObject unit)
        {
            controlledUnits.Remove(unit);
        }

        private void CleanupDeadUnits()
        {
            controlledUnits.RemoveAll(u => u == null);
        }
    }
}
