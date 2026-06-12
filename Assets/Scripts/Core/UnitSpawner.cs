using UnityEngine;
using UnityEngine.AI;
using Mirror;
using RealmCommander.RTS;
using RealmCommander.Network;
using RealmCommander.RPG;

namespace RealmCommander.Core
{
    public class UnitSpawner : NetworkBehaviour
    {
        [Header("Unit Prefabs")]
        [SerializeField] private GameObject friendlyUnitPrefab;
        [SerializeField] private GameObject enemyUnitPrefab;
        [SerializeField] private GameObject heroPrefab;

        public void Initialize(GameObject unitPrefab, GameObject commanderHeroPrefab = null)
        {
            friendlyUnitPrefab = unitPrefab;
            enemyUnitPrefab = unitPrefab;
            heroPrefab = commanderHeroPrefab;
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

            SpawnHero(0, friendlySpawnPosition + new Vector3(0f, 0f, -4f), friendlyOwner);
            SpawnHero(1, enemySpawnPosition + new Vector3(0f, 0f, 4f), enemyOwner);

            Debug.Log($"[UnitSpawner] Spawned: {friendlyUnitCount} friendly, {enemyUnitCount} enemy, 2 heroes");
        }

        [Server]
        public void ReassignOwnership()
        {
            foreach (RTS.Unit unit in FindObjectsByType<RTS.Unit>(FindObjectsSortMode.None))
            {
                if (unit == null || unit.IsEnemy) continue;
                if (unit.netIdentity.connectionToClient != null) continue;

                NetworkConnectionToClient owner = FindTeamConnection(0);
                if (owner != null)
                {
                    unit.netIdentity.AssignClientAuthority(owner);
                    Debug.Log($"[UnitSpawner] Assigned ownership of {unit.name} to connection {owner.connectionId}");
                }
            }

            foreach (Hero hero in FindObjectsByType<Hero>(FindObjectsSortMode.None))
            {
                if (hero == null || hero.netIdentity.connectionToClient != null) continue;
                NetworkConnectionToClient owner = FindTeamConnection(hero.TeamId);
                if (owner != null) hero.netIdentity.AssignClientAuthority(owner);
            }
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

        [Server]
        private void SpawnHero(int team, Vector3 desiredPosition, NetworkConnectionToClient owner)
        {
            if (heroPrefab == null) return;
            GameObject heroObject = Instantiate(heroPrefab, GetNavMeshPosition(desiredPosition), Quaternion.identity);
            heroObject.name = team == 0 ? "CommanderHero_Team0" : "CommanderHero_Team1";
            heroObject.GetComponent<Hero>()?.ConfigureTeam(team);
            SpawnWithOwner(heroObject, owner);
        }

        private static Vector3 GetNavMeshPosition(Vector3 desiredPosition)
        {
            return NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 8f, NavMesh.AllAreas)
                ? hit.position
                : desiredPosition;
        }
    }
}
