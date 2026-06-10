using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using RealmCommander.Network;
using UnityEngine.SceneManagement;

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

            if (ipInputField != null)
                ipInputField.text = "127.0.0.1";

            ShowStatus("", false);

            NetworkBootstrap.EnsureNetworkManager();
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
        }

        public void OnHostGame()
        {
            var nm = NetworkBootstrap.EnsureNetworkManager();
            if (nm == null)
            {
                Debug.LogError("Failed to create NetworkManager!");
                return;
            }

            nm.networkAddress = "0.0.0.0";
            nm.StartHost();
            ShowStatus("Hosting game...", true);
            Debug.Log("[LobbyUI] Hosting game, waiting for scene change...");
        }

        public void OnJoinGame()
        {
            string address = ipInputField != null ? ipInputField.text : "127.0.0.1";

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
