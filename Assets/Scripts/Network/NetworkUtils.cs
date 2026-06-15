using UnityEngine;
using Mirror;

namespace RealmCommander.Network
{
    public static class NetworkUtils
    {
        public static NetworkConnectionToClient FindTeamConnection(int teamId)
        {
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (connection.identity == null) continue;
                NetworkPlayer player = connection.identity.GetComponent<NetworkPlayer>();
                if (player != null && player.TeamId == teamId)
                    return connection;
            }
            return null;
        }

        public static Camera GetMainCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
                cam = Object.FindFirstObjectByType<Camera>();
            return cam;
        }
    }
}
