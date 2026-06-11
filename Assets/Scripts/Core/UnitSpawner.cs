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
        [SerializeField] private int friendlyUnitCount = 5;
        [SerializeField] private int enemyUnitCount = 5;
        [SerializeField] private Vector3 friendlySpawnPosition = new Vector3(-10, 0, 0);
        [SerializeField] private Vector3 enemySpawnPosition = new Vector3(10, 0, 0);
        [SerializeField] private float spawnRadius = 3f;

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

            NetworkConnectionToClient friendlyOwner = FindTeamConnection(0);
            NetworkConnectionToClient enemyOwner = FindTeamConnection(1);

            // 아군 유닛 생성
            for (int i = 0; i < friendlyUnitCount; i++)
            {
                if (friendlyUnitPrefab != null)
                {
                    Vector3 spawnPos = friendlySpawnPosition + Random.insideUnitSphere * spawnRadius;
                    spawnPos.y = 0.5f;
                    spawnPos = GetNavMeshPosition(spawnPos);

                    GameObject unit = Instantiate(friendlyUnitPrefab, spawnPos, Quaternion.identity);
                    unit.name = $"FriendlyUnit_{i}";
                    unit.GetComponent<Unit>()?.ConfigureTeam(false);
                    SpawnWithOwner(unit, friendlyOwner);
                }
            }

            // 적 유닛 생성
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

                    SpawnWithOwner(unit, enemyOwner);
                }
            }

            Debug.Log($"유닛 생성 완료: 아군 {friendlyUnitCount}명, 적 {enemyUnitCount}명");
        }

        [Server]
        private static NetworkConnectionToClient FindTeamConnection(int teamId)
        {
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                NetworkPlayer player = connection.identity != null
                    ? connection.identity.GetComponent<NetworkPlayer>()
                    : null;
                if (player != null && player.teamId == teamId)
                    return connection;
            }

            return null;
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
