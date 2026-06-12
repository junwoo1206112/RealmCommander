using UnityEngine;
using Mirror;

namespace RealmCommander.Network
{
    public class RealmCommanderNetworkManager : NetworkManager
    {
        public static string LastConnectionStatus { get; private set; } = "Idle";

        public override void OnStartHost()
        {
            base.OnStartHost();
            SetStatus("Host started on TCP 7777");
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            SetStatus($"Connected to {networkAddress}:7777");
        }

        public override void OnClientDisconnect()
        {
            SetStatus($"Disconnected from {networkAddress}:7777");
            base.OnClientDisconnect();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);
            NetworkGameManager.Instance?.OnPlayerDisconnected();
            SetStatus($"Player disconnected. Active connections: {NetworkServer.connections.Count}");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            int activePlayers = 0;
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
                if (connection.identity != null) activePlayers++;
            if (activePlayers >= 2)
            {
                Debug.LogWarning("[NetworkManager] Max 2 players, rejecting connection");
                conn.Disconnect();
                return;
            }

            GameObject prefab = playerPrefab != null ? playerPrefab : NetworkBootstrap.CachedPlayerPrefab;
            if (prefab == null)
            {
                Debug.LogError("[NetworkManager] PlayerPrefab is null!");
                return;
            }

            int teamId = GetAvailableTeamId();
            GameObject player = Instantiate(prefab);
            NetworkPlayer networkPlayer = player.GetComponent<NetworkPlayer>();
            if (networkPlayer != null)
                networkPlayer.teamId = teamId;

            NetworkServer.AddPlayerForConnection(conn, player);
            AssignExistingEntities(conn, teamId);

            var spawner = FindAnyObjectByType<Core.UnitSpawner>();
            if (spawner != null)
                spawner.ReassignOwnership();

            Debug.Log($"[NetworkManager] Player created for connection {conn.connectionId}, team {teamId}");
        }

        public override void OnServerSceneChanged(string sceneName)
        {
            base.OnServerSceneChanged(sceneName);
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                NetworkPlayer player = connection.identity != null
                    ? connection.identity.GetComponent<NetworkPlayer>()
                    : null;
                if (player != null)
                    AssignExistingEntities(connection, player.teamId);
            }
        }

        private static void SetStatus(string status)
        {
            LastConnectionStatus = status;
            Debug.Log($"[NetworkStatus] {status}");
        }

        private static int GetAvailableTeamId()
        {
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                NetworkPlayer player = connection.identity != null
                    ? connection.identity.GetComponent<NetworkPlayer>()
                    : null;
                if (player != null && player.teamId == 0)
                    return 1;
            }

            return 0;
        }

        private static void AssignExistingEntities(NetworkConnectionToClient connection, int teamId)
        {
            bool isEnemyTeam = teamId == 1;
            foreach (RTS.Unit unit in FindObjectsByType<RTS.Unit>(FindObjectsSortMode.None))
            {
                if (unit == null || unit.IsEnemy != isEnemyTeam || unit.netIdentity.connectionToClient != null)
                    continue;

                unit.netIdentity.AssignClientAuthority(connection);
            }

            foreach (RTS.Building building in FindObjectsByType<RTS.Building>(FindObjectsSortMode.None))
            {
                if (building == null || building.TeamId != teamId || building.netIdentity.connectionToClient != null)
                    continue;

                building.netIdentity.AssignClientAuthority(connection);
            }


            foreach (RPG.Hero hero in FindObjectsByType<RPG.Hero>(FindObjectsSortMode.None))
            {
                if (hero == null || hero.TeamId != teamId || hero.netIdentity.connectionToClient != null)
                    continue;
                hero.netIdentity.AssignClientAuthority(connection);
            }
        }
    }
}
