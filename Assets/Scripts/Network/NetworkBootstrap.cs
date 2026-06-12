using UnityEngine;
using Mirror;

namespace RealmCommander.Network
{
    public static class NetworkBootstrap
    {
        private static GameObject _cachedPlayerPrefab;

        public static GameObject CachedPlayerPrefab => _cachedPlayerPrefab;

        public static NetworkManager EnsureNetworkManager()
        {
            if (NetworkManager.singleton != null)
            {
                ConfigureNetworkPrefabs(NetworkManager.singleton);
                return NetworkManager.singleton;
            }

            var existing = Object.FindAnyObjectByType<NetworkManager>();
            if (existing != null)
            {
                ConfigureNetworkPrefabs(existing);
                Debug.Log("[NetworkBootstrap] Using existing NetworkManager from scene");
                return existing;
            }

            GameObject nmGo = new GameObject("NetworkManager");
            nmGo.SetActive(false);
            var transport = nmGo.AddComponent<TelepathyTransport>();
            var nm = nmGo.AddComponent<RealmCommanderNetworkManager>();
            nm.transport = transport;
            nm.onlineScene = "Assets/Scenes/MainScene.unity";
            nm.offlineScene = "Assets/Scenes/MainMenuScene.unity";
            
            ConfigureNetworkPrefabs(nm);

            Object.DontDestroyOnLoad(nmGo);
            nmGo.SetActive(true);

            Debug.Log("[NetworkBootstrap] NetworkManager created");
            return NetworkManager.singleton;
        }

        private static void ConfigureNetworkPrefabs(NetworkManager networkManager)
        {
            _cachedPlayerPrefab = Resources.Load<GameObject>("Player");
            if (_cachedPlayerPrefab == null)
            {
                Debug.LogError("[NetworkBootstrap] Resources/Player.prefab is missing.");
                return;
            }

            networkManager.playerPrefab = _cachedPlayerPrefab;
            networkManager.autoCreatePlayer = true;

            GameObject unitPrefab = Resources.Load<GameObject>("Unit");
            if (unitPrefab != null && !networkManager.spawnPrefabs.Contains(unitPrefab))
                networkManager.spawnPrefabs.Add(unitPrefab);

            GameObject heroPrefab = Resources.Load<GameObject>("CommanderHero");
            if (heroPrefab != null && !networkManager.spawnPrefabs.Contains(heroPrefab))
                networkManager.spawnPrefabs.Add(heroPrefab);

            Debug.Log("[NetworkBootstrap] Network prefabs configured");
        }
    }
}
