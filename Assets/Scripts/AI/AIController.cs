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
        [SerializeField] private int maxUnits = 15;

        [Header("Strategy")]
        [SerializeField] private float aggroRange = 12f;
        [SerializeField] private float regroupRange = 8f;
        [SerializeField] private float retreatHealthPercent = 0.25f;
        [SerializeField] private int minGroupSize = 3;

        [Header("References")]
        [SerializeField] private Transform baseTransform;
        [SerializeField] private float baseRadius = 5f;

        private List<GameObject> controlledUnits = new List<GameObject>();
        private Transform playerBase;
        private float lastUpdateTime;
        private float lastSpawnTime;
        private float lastDiscoveryTime;
        private float lastHumanCheckTime;
        private int currentWaveSize;

        public AIDifficulty Difficulty => difficulty;

        public void Initialize(GameObject[] prefabs, Transform spawnBase = null)
        {
            if (prefabs != null && prefabs.Length > 0)
                unitPrefabs = prefabs;
            if (spawnBase != null)
                baseTransform = spawnBase;
            EnsureUnitPrefabs();
        }

        private void Start()
        {
            EnsureUnitPrefabs();
            baseTransform ??= FindEnemyBase();
            playerBase = FindPlayerBase();
            CheckForHumanEnemy();
            RegisterExistingEnemyUnits();
        }

        private void EnsureUnitPrefabs()
        {
            if (unitPrefabs != null && unitPrefabs.Length > 0) return;

            GameObject defaultUnit = Resources.Load<GameObject>("Unit");
            if (defaultUnit == null)
            {
                Debug.LogError("[AI] Resources/Unit.prefab is missing; AI cannot spawn units.");
                return;
            }

            unitPrefabs = new[] { defaultUnit };
        }

        private void CheckForHumanEnemy()
        {
            bool previouslyHadHumanEnemy = hasHumanEnemy;
            hasHumanEnemy = false;
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn.identity == null) continue;
                var player = conn.identity.GetComponent<NetworkPlayer>();
                if (player != null && player.TeamId == 1)
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
                playerBase = FindPlayerBase();

            var registry = Core.EntityRegistry.Instance;
            List<GameObject> enemies = new List<GameObject>();
            int friendlyCount = 0;

            if (registry != null)
            {
                foreach (var u in registry.AllUnits)
                {
                    if (u == null || !u.IsAlive) continue;
                    if (!u.IsEnemy)
                        enemies.Add(u.gameObject);
                    else
                        friendlyCount++;
                }

                foreach (var b in registry.AllBuildings)
                {
                    if (b == null || !b.IsAlive) continue;
                    if (b.TeamId == 0)
                        enemies.Add(b.gameObject);
                }
            }

            List<GameObject> aliveUnits = new List<GameObject>();
            foreach (var unit in controlledUnits)
            {
                if (unit != null && unit.GetComponent<Unit>() != null && unit.GetComponent<Unit>().IsAlive)
                    aliveUnits.Add(unit);
            }

            bool shouldAttack = aliveUnits.Count >= minGroupSize;

            foreach (var unit in aliveUnits)
            {
                var unitComponent = unit.GetComponent<Unit>();
                if (unitComponent == null) continue;

                float healthPercent = unitComponent.CurrentHealth / unitComponent.MaxHealth;
                if (healthPercent < retreatHealthPercent)
                {
                    RetreatToBase(unit);
                    continue;
                }

                GameObject nearestEnemy = FindNearestEnemy(unit.transform.position, enemies);
                float nearestDist = nearestEnemy != null
                    ? Vector3.Distance(unit.transform.position, nearestEnemy.transform.position)
                    : float.MaxValue;

                if (shouldAttack && nearestEnemy != null && nearestDist < aggroRange)
                {
                    unitComponent.SetTarget(nearestEnemy);
                }
                else if (shouldAttack && playerBase != null)
                {
                    AttackBase(unit, playerBase.position);
                }
                else
                {
                    RegroupAtRallyPoint(unit);
                }
            }
        }

        private GameObject FindNearestEnemy(Vector3 position, List<GameObject> enemies)
        {
            GameObject nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(position, enemy.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        private void RetreatToBase(GameObject unit)
        {
            var unitComponent = unit.GetComponent<Unit>();
            if (unitComponent == null) return;

            unitComponent.ClearTarget();

            Vector3 retreatPos = baseTransform != null
                ? baseTransform.position + Random.insideUnitSphere * baseRadius
                : new Vector3(20f, 0f, 0f);

            var agent = unit.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(retreatPos);
            }
        }

        private void AttackBase(GameObject unit, Vector3 basePosition)
        {
            var unitComponent = unit.GetComponent<Unit>();
            if (unitComponent == null) return;

            var agent = unit.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                if (!agent.hasPath || agent.remainingDistance < 2f)
                {
                    unitComponent.ClearTarget();
                    Vector3 offset = Random.insideUnitSphere * 3f;
                    offset.y = 0;
                    agent.SetDestination(basePosition + offset);
                }
            }
        }

        private void RegroupAtRallyPoint(GameObject unit)
        {
            var agent = unit.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

            Vector3 rallyPoint = baseTransform != null
                ? baseTransform.position + new Vector3(0f, 0f, -3f)
                : new Vector3(20f, 0f, -3f);

            float distToRally = Vector3.Distance(unit.transform.position, rallyPoint);
            if (distToRally > regroupRange)
            {
                agent.isStopped = false;
                if (!agent.hasPath || agent.remainingDistance < 1f)
                {
                    var unitComponent = unit.GetComponent<Unit>();
                    unitComponent?.ClearTarget();
                    Vector3 offset = Random.insideUnitSphere * 2f;
                    offset.y = 0;
                    agent.SetDestination(rallyPoint + offset);
                }
            }
        }

        private Transform FindPlayerBase()
        {
            var registry = Core.EntityRegistry.Instance;
            if (registry == null) return null;

            foreach (var b in registry.AllBuildings)
            {
                if (b != null && b.BuildingType == BuildingType.Base && b.TeamId == 0 && b.IsAlive)
                    return b.transform;
            }

            foreach (var b in registry.AllBuildings)
            {
                if (b != null && b.BuildingType == BuildingType.Base && b.TeamId == 0)
                    return b.transform;
            }

            return null;
        }

        private Transform FindEnemyBase()
        {
            var registry = Core.EntityRegistry.Instance;
            if (registry == null) return null;

            foreach (var b in registry.AllBuildings)
            {
                if (b != null && b.BuildingType == BuildingType.Base && b.TeamId == 1 && b.IsAlive)
                    return b.transform;
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
                : new Vector3(20f, 0f, 0f) + Random.insideUnitSphere * 3f;

            spawnPos.y = 0.5f;
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, baseRadius + 3f, NavMesh.AllAreas))
                spawnPos = hit.position;

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
                    unit.ApplyDifficultyMultiplier(0.7f, 0.8f, 1.2f);
                    break;
                case AIDifficulty.Normal:
                    break;
                case AIDifficulty.Hard:
                    unit.ApplyDifficultyMultiplier(1.3f, 1.5f, 0.8f);
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
                controlledUnits.Add(unit);
        }

        private void RegisterExistingEnemyUnits()
        {
            var registry = Core.EntityRegistry.Instance;
            if (registry == null) return;

            foreach (Unit unit in registry.AllUnits)
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
