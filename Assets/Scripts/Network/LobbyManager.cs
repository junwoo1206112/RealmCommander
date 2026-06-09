using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

namespace RealmCommander.Network
{
    public class LobbyManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject hostButton;
        [SerializeField] private GameObject joinButton;
        [SerializeField] private GameObject ipInputField;
        [SerializeField] private GameObject backButton;
        [SerializeField] private GameObject statusText;

        [Header("Network Settings")]
        [SerializeField] private string gameSceneName = "MainScene";
        [SerializeField] private int port = 7777;

        public string GameSceneName => gameSceneName;

        private string ipAddress = "127.0.0.1";

        public void HostGame()
        {
            var manager = NetworkManager.singleton;
            if (manager == null)
            {
                Debug.LogError("NetworkManager not found!");
                return;
            }

            manager.networkAddress = "0.0.0.0";
            manager.StartHost();

            Debug.Log($"Hosting game on port {port}");
        }

        public void JoinGame(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                address = "127.0.0.1";
            }

            ipAddress = address;

            var manager = NetworkManager.singleton;
            if (manager == null)
            {
                Debug.LogError("NetworkManager not found!");
                return;
            }

            manager.networkAddress = address;
            manager.StartClient();

            Debug.Log($"Connecting to {address}:{port}");
        }

        public void ReturnToMainMenu()
        {
            if (NetworkServer.active)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkClient.active)
            {
                NetworkManager.singleton.StopClient();
            }

            SceneManager.LoadScene("MainMenuScene");
        }
    }
}
