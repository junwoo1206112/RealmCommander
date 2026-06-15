using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using RealmCommander.Network;
using UnityEngine.SceneManagement;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RealmCommander.UI
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_InputField ipInputField;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject statusPanel;

        [Header("Host Options")]
        [SerializeField] private GameObject hostOptionsPanel;
        [SerializeField] private Button singlePlayerButton;
        [SerializeField] private Button multiplayerButton;

        [Header("Network Info")]
        [SerializeField] private TextMeshProUGUI localIPText;

        private void Start()
        {
            if (hostButton != null)
                hostButton.onClick.AddListener(OnHostGame);

            if (joinButton != null)
                joinButton.onClick.AddListener(OnJoinGame);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackToMenu);

            if (singlePlayerButton != null)
                singlePlayerButton.onClick.AddListener(OnSinglePlayer);

            if (multiplayerButton != null)
                multiplayerButton.onClick.AddListener(OnMultiplayer);

            if (ipInputField != null)
                ipInputField.text = "127.0.0.1";

            if (localIPText != null)
                localIPText.text = "LAN IP: Loading... | TCP 7777";

            if (hostOptionsPanel != null)
                hostOptionsPanel.SetActive(false);

            ShowStatus("", false);

            NetworkBootstrap.EnsureNetworkManager();
            _ = LoadLocalIPAsync();
        }

        private async Task LoadLocalIPAsync()
        {
            string ip = await Task.Run(() =>
            {
                try
                {
                    foreach (IPAddress addr in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    {
                        if (addr.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr))
                            return addr.ToString();
                    }
                }
                catch (SocketException) { }
                return "Unavailable";
            });

            if (localIPText != null)
                localIPText.text = $"LAN IP: {ip} | TCP 7777";
        }

        private void Update()
        {
            UpdateUIState();
        }

        private void UpdateUIState()
        {
            bool isConnected = NetworkClient.active;

            if (hostButton != null)
                hostButton.interactable = !isConnected;

            if (joinButton != null)
                joinButton.interactable = !isConnected && !string.IsNullOrEmpty(ipInputField?.text);

            if (backButton != null)
                backButton.interactable = !isConnected;

            if (isConnected && statusText != null)
                statusText.text = RealmCommanderNetworkManager.LastConnectionStatus;
        }

        public void OnHostGame()
        {
            if (hostOptionsPanel != null)
            {
                hostOptionsPanel.SetActive(true);
                return;
            }

            StartHost(false);
        }

        public void OnSinglePlayer()
        {
            StartHost(true);
        }

        public void OnMultiplayer()
        {
            StartHost(false);
        }

        private void StartHost(bool singlePlayer)
        {
            var nm = NetworkBootstrap.EnsureNetworkManager();
            if (nm == null)
            {
                Debug.LogError("Failed to create NetworkManager!");
                return;
            }

            nm.networkAddress = "0.0.0.0";
            nm.StartHost();

            if (singlePlayer)
            {
                NetworkGameManager.Instance?.SetSinglePlayerMode();
                ShowStatus("Starting single player...", true);
                Debug.Log("[LobbyUI] Starting single player game with AI");
            }
            else
            {
                ShowStatus("Hosting game, waiting for players...", true);
                Debug.Log("[LobbyUI] Hosting multiplayer game");
            }
        }

        public void OnJoinGame()
        {
            string address = ipInputField != null ? ipInputField.text.Trim() : "127.0.0.1";
            if (string.IsNullOrWhiteSpace(address))
            {
                ShowStatus("IP address is required.", true);
                return;
            }

            var nm = NetworkBootstrap.EnsureNetworkManager();
            if (nm == null)
            {
                Debug.LogError("Failed to create NetworkManager!");
                return;
            }

            nm.networkAddress = address;
            nm.StartClient();
            ShowStatus($"Connecting to {address}...", true);
            Debug.Log($"[LobbyUI] Connecting to {address}...");
        }

        public void OnBackToMenu()
        {
            if (hostOptionsPanel != null)
                hostOptionsPanel.SetActive(false);

            if (NetworkServer.active)
            {
                NetworkManager.singleton?.StopHost();
            }
            else if (NetworkClient.active)
            {
                NetworkManager.singleton?.StopClient();
            }

            SceneManager.LoadScene("MainMenuScene");
        }

        private void ShowStatus(string message, bool show)
        {
            if (statusPanel != null)
                statusPanel.SetActive(show);

            if (statusText != null)
                statusText.text = message;
        }
    }
}
