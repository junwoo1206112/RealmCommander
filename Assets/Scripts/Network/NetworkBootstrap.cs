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
                return NetworkManager.singleton;

            var existing = Object.FindAnyObjectByType<NetworkManager>();
            if (existing != null)
            {
                Debug.Log("[NetworkBootstrap] Using existing NetworkManager from scene");
                return existing;
            }

            GameObject nmGo = new GameObject("NetworkManager");
            nmGo.SetActive(false);
            var transport = nmGo.AddComponent<TelepathyTransport>();
            var nm = nmGo.AddComponent<NetworkManager>();
            nm.transport = transport;
            nm.onlineScene = "Assets/Scenes/MainScene.unity";
            nm.offlineScene = "Assets/Scenes/MainMenuScene.unity";
            
            CreatePlayerPrefab();
            
            Object.DontDestroyOnLoad(nmGo);
            nmGo.SetActive(true);

            Debug.Log("[NetworkBootstrap] NetworkManager created");
            return NetworkManager.singleton;
        }

        private static void CreatePlayerPrefab()
        {
            _cachedPlayerPrefab = new GameObject("PlayerPrefab");
            _cachedPlayerPrefab.hideFlags = HideFlags.HideAndDontSave;
            _cachedPlayerPrefab.AddComponent<NetworkIdentity>();
            _cachedPlayerPrefab.AddComponent<NetworkPlayer>();
            _cachedPlayerPrefab.AddComponent<CapsuleCollider>();
            _cachedPlayerPrefab.AddComponent<Rigidbody>();
            
            var renderer = _cachedPlayerPrefab.AddComponent<MeshRenderer>();
            _cachedPlayerPrefab.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.cyan;

            Debug.Log("[NetworkBootstrap] PlayerPrefab created");
        }
    }
}
