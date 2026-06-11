using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using RealmCommander.RTS;
using RealmCommander.Network;

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
        private bool hasHumanEnemy;

        private bool ShouldControlEnemies
        {
            get
            {
                if (!NetworkServer.active) return false;
                if (hasHumanEnemy) return false;
                return true;
            }
        }

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
        private float lastDiscoveryTime;
        private float lastHumanCheckTime;

        public AIDifficulty Difficulty => difficulty;

        private void Start()
        {
            playerBase = FindPlayerBase();
            CheckForHumanEnemy();
            RegisterExistingEnemyUnits();
        }

        private void CheckForHumanEnemy()
        {
            bool previouslyHadHumanEnemy = hasHumanEnemy;
            hasHumanEnemy = false;
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn.identity == null) continue;
                var player = conn.identity.GetComponent<NetworkPlayer>();
                if (player != null && player.teamId == 1)
                {
                    hasHumanEnemy = true;
                    break;
                }
            }

            if (!previouslyHadHumanEnemy && hasHumanEnemy)
            {
                foreach (GameObject unit in controlledUnits)
                    unit?.GetComponent<Unit>()?.ClearTarget();
            }
        }

        private void Update()
        {
            if (!NetworkServer.active) return;

            if (Time.time - lastHumanCheckTime >= 1f)
            {
                lastHumanCheckTime = Time.time;
                CheckForHumanEnemy();
            }
            if (!ShouldControlEnemies) return;

            if (Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                UpdateAI();
            }

            if (Time.time - lastDiscoveryTime >= 2f)
            {
                lastDiscoveryTime = Time.time;
                RegisterExistingEnemyUnits();
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

            var aliveEnemies = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            int enemyCount = 0;
            foreach (var u in aliveEnemies)
                if (!u.IsEnemy && u.IsAlive) enemyCount++;

            GameObject[] enemyCache = new GameObject[enemyCount];
            int idx = 0;
            foreach (var u in aliveEnemies)
                if (!u.IsEnemy && u.IsAlive) enemyCache[idx++] = u.gameObject;

            foreach (var unit in controlledUnits)
            {
                if (unit == null) continue;

                var unitComponent = unit.GetComponent<Unit>();
                if (unitComponent == null) continue;

                GameObject nearestEnemy = null;
                float nearestDist = 15f;
                foreach (var e in enemyCache)
                {
                    float d = Vector3.Distance(unit.transform.position, e.transform.position);
                    if (d < nearestDist) { nearestDist = d; nearestEnemy = e; }
                }

                if (nearestEnemy != null)
                {
                    unitComponent.SetTarget(nearestEnemy);
                    continue;
                }

                float distanceToBase = Vector3.Distance(unit.transform.position, playerBase.position);
                if (distanceToBase > 4f)
                {
                    var agent = unit.GetComponent<NavMeshAgent>();
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        if (!agent.hasPath || agent.remainingDistance < 1f)
                        {
                            unitComponent.ClearTarget();
                            Vector3 offset = Random.insideUnitSphere * 2f;
                            offset.y = 0;
                            agent.SetDestination(playerBase.position + offset);
                        }
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
                if (b.BuildingType == BuildingType.Base && b.TeamId == 0 && b.IsAlive)
                {
                    return b.transform;
                }
            }

            foreach (var b in bases)
            {
                if (b.BuildingType == BuildingType.Base && b.TeamId == 0)
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
                unitComponent.ConfigureTeam(true);
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

        private void RegisterExistingEnemyUnits()
        {
            foreach (Unit unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
            {
                if (unit != null && unit.IsEnemy && unit.IsAlive)
                    RegisterUnit(unit.gameObject);
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
