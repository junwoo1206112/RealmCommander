using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using RealmCommander.Network;

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
            var lobby = FindFirstObjectByType<LobbyManager>();
            if (lobby != null)
            {
                lobby.HostGame();
                ShowStatus("Hosting game...", true);
            }
        }

        public void OnJoinGame()
        {
            string address = ipInputField != null ? ipInputField.text : "127.0.0.1";

            var lobby = FindFirstObjectByType<LobbyManager>();
            if (lobby != null)
            {
                lobby.JoinGame(address);
                ShowStatus($"Connecting to {address}...", true);
            }
        }

        public void OnBackToMenu()
        {
            var lobby = FindFirstObjectByType<LobbyManager>();
            if (lobby != null)
            {
                lobby.ReturnToMainMenu();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
            }
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
