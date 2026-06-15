using UnityEngine;
using UnityEngine.AI;
using Mirror;
using RealmCommander.RTS;
using RealmCommander.Network;

namespace RealmCommander.Core
{
    public class UnitSpawner : NetworkBehaviour
    {
        [Header("Unit Prefabs")]
        [SerializeField] private GameObject friendlyUnitPrefab;
        [SerializeField] private GameObject enemyUnitPrefab;

        public void Initialize(GameObject unitPrefab)
        {
            friendlyUnitPrefab = unitPrefab;
            enemyUnitPrefab = unitPrefab;
        }

        [Header("Spawn Settings")]
        [SerializeField] private int friendlyUnitCount = 8;
        [SerializeField] private int enemyUnitCount = 8;
        [SerializeField] private Vector3 friendlySpawnPosition = new Vector3(-22, 0, 0);
        [SerializeField] private Vector3 enemySpawnPosition = new Vector3(22, 0, 0);
        [SerializeField] private float spawnRadius = 3f;

        [Header("CSV Spec IDs")]
        [SerializeField] private string[] unitSpecIds = { "unit_worker", "unit_worker", "unit_soldier", "unit_soldier", "unit_soldier", "unit_archer", "unit_archer", "unit_mage" };

        private bool hasSpawned;

        public override void OnStartServer()
        {
            base.OnStartServer();
            SpawnUnitsNow();
        }

        [Server]
        public void SpawnUnitsNow()
        {
            if (hasSpawned) return;
            hasSpawned = true;

            EnsureStartingBases();

            NetworkConnectionToClient friendlyOwner = NetworkUtils.FindTeamConnection(0);
            NetworkConnectionToClient enemyOwner = NetworkUtils.FindTeamConnection(1);

            for (int i = 0; i < friendlyUnitCount; i++)
            {
                if (friendlyUnitPrefab != null)
                {
                    Vector3 spawnPos = friendlySpawnPosition + Random.insideUnitSphere * spawnRadius;
                    spawnPos.y = 0.5f;
                    spawnPos = GetNavMeshPosition(spawnPos);

                    GameObject unit = Instantiate(friendlyUnitPrefab, spawnPos, Quaternion.identity);
                    unit.name = $"FriendlyUnit_{i}";

                    var unitComponent = unit.GetComponent<Unit>();
                    unitComponent?.ConfigureTeam(false);

                    if (unitSpecIds.Length > 0 && unitComponent != null)
                    {
                        string specId = unitSpecIds[i % unitSpecIds.Length];
                        unitComponent.ApplySpec(specId);
                    }

                    SpawnWithOwner(unit, friendlyOwner);
                }
            }

            for (int i = 0; i < enemyUnitCount; i++)
            {
                if (enemyUnitPrefab != null)
                {
                    Vector3 spawnPos = enemySpawnPosition + Random.insideUnitSphere * spawnRadius;
                    spawnPos.y = 0.5f;
                    spawnPos = GetNavMeshPosition(spawnPos);

                    GameObject unit = Instantiate(enemyUnitPrefab, spawnPos, Quaternion.identity);
                    unit.name = $"EnemyUnit_{i}";

                    var unitComponent = unit.GetComponent<Unit>();
                    unitComponent?.ConfigureTeam(true);

                    if (unitSpecIds.Length > 0 && unitComponent != null)
                    {
                        string specId = unitSpecIds[i % unitSpecIds.Length];
                        unitComponent.ApplySpec(specId);
                    }

                    SpawnWithOwner(unit, enemyOwner);
                }
            }

            Debug.Log($"[UnitSpawner] Spawned: {friendlyUnitCount} friendly, {enemyUnitCount} enemy units");
        }

        [Server]
        private static void EnsureStartingBases()
        {
            float friendlyX = -20f;
            float enemyX = 20f;

            bool hasFriendlyBase = false;
            bool hasEnemyBase = false;
            bool hasFriendlyBarracks = false;
            bool hasEnemyBarracks = false;

            foreach (Building building in FindObjectsByType<Building>(FindObjectsSortMode.None))
            {
                if (building == null || !building.IsAlive) continue;

                if (building.BuildingType == BuildingType.Base)
                {
                    if (building.TeamId == 0)
                    {
                        hasFriendlyBase = true;
                        MoveToPosition(building, new Vector3(friendlyX, 0f, 0f));
                    }
                    if (building.TeamId == 1)
                    {
                        hasEnemyBase = true;
                        MoveToPosition(building, new Vector3(enemyX, 0f, 0f));
                    }
                }
                if (building.BuildingType == BuildingType.Barracks)
                {
                    if (building.TeamId == 0)
                    {
                        hasFriendlyBarracks = true;
                        MoveToPosition(building, new Vector3(friendlyX + 4f, 0f, 0f));
                    }
                    if (building.TeamId == 1)
                    {
                        hasEnemyBarracks = true;
                        MoveToPosition(building, new Vector3(enemyX - 4f, 0f, 0f));
                    }
                }
            }

            if (!hasFriendlyBase)
                CreateRuntimeBuilding("Blue Command Base", BuildingType.Base, new Vector3(friendlyX, 0f, 0f), 0, new Vector3(2.4f, 1.1f, 2.4f));
            if (!hasEnemyBase)
                CreateRuntimeBuilding("Red Command Base", BuildingType.Base, new Vector3(enemyX, 0f, 0f), 1, new Vector3(2.4f, 1.1f, 2.4f));

            if (!hasFriendlyBarracks)
                CreateRuntimeBuilding("Blue Barracks", BuildingType.Barracks, new Vector3(friendlyX + 4f, 0f, 0f), 0, new Vector3(2.1f, 1f, 2.1f));
            if (!hasEnemyBarracks)
                CreateRuntimeBuilding("Red Barracks", BuildingType.Barracks, new Vector3(enemyX - 4f, 0f, 0f), 1, new Vector3(2.1f, 1f, 2.1f));
        }

        private static void MoveToPosition(Building building, Vector3 position)
        {
            Vector3 currentPos = building.transform.position;
            if (Vector3.Distance(currentPos, position) > 1f)
            {
                building.transform.position = position;
                Debug.Log($"[UnitSpawner] Moved {building.name} to {position}");
            }
        }

        [Server]
        private static void CreateRuntimeBuilding(string name, BuildingType type, Vector3 position, int teamId, Vector3 scale)
        {
            GameObject buildingObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buildingObject.name = name;
            buildingObject.transform.position = position;
            buildingObject.transform.localScale = scale;

            Collider collider = buildingObject.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = false;

            if (buildingObject.GetComponent<NetworkIdentity>() == null)
                buildingObject.AddComponent<NetworkIdentity>();

            Building building = buildingObject.AddComponent<Building>();
            building.ConfigureRuntimeBuilding(name, type, teamId);

            NetworkConnectionToClient owner = NetworkUtils.FindTeamConnection(teamId);
            if (owner != null) NetworkServer.Spawn(buildingObject, owner);
            else NetworkServer.Spawn(buildingObject);
        }

        [Server]
        public void ReassignOwnership()
        {
            foreach (RTS.Unit unit in FindObjectsByType<RTS.Unit>(FindObjectsSortMode.None))
            {
                if (unit == null || unit.IsEnemy) continue;
                if (unit.netIdentity.connectionToClient != null) continue;

                NetworkConnectionToClient owner = NetworkUtils.FindTeamConnection(0);
                if (owner != null)
                {
                    unit.netIdentity.AssignClientAuthority(owner);
                    Debug.Log($"[UnitSpawner] Assigned ownership of {unit.name} to connection {owner.connectionId}");
                }
            }
        }

        [Server]
        private static void SpawnWithOwner(GameObject unit, NetworkConnectionToClient owner)
        {
            if (owner != null)
                NetworkServer.Spawn(unit, owner);
            else
                NetworkServer.Spawn(unit);
        }

        private static Vector3 GetNavMeshPosition(Vector3 desiredPosition)
        {
            return NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 8f, NavMesh.AllAreas)
                ? hit.position
                : desiredPosition;
        }
    }
}
