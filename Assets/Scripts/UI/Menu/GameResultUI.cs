using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using RealmCommander.Network;

namespace RealmCommander.UI
{
    public class GameResultUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TextMeshProUGUI detailText;
        [SerializeField] private Button returnToLobbyButton;
        [SerializeField] private Button playAgainButton;

        public static GameResultUI Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (resultPanel != null)
                resultPanel.SetActive(false);
        }

        private void Start()
        {
            if (returnToLobbyButton != null)
                returnToLobbyButton.onClick.AddListener(OnReturnToLobby);

            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnPlayAgain);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void Show(bool isVictory)
        {
            if (Instance == null) return;

            if (Instance.resultPanel != null)
                Instance.resultPanel.SetActive(true);

            if (Instance.resultText != null)
            {
                Instance.resultText.text = isVictory ? "VICTORY" : "DEFEAT";
                Instance.resultText.color = isVictory ? Color.yellow : Color.red;
            }

            if (Instance.detailText != null)
            {
                Instance.detailText.text = isVictory
                    ? "All enemy units have been defeated!"
                    : "Your units have been defeated.";
            }
        }

        private void OnReturnToLobby()
        {
            var gameManager = NetworkGameManager.Instance;
            if (gameManager != null)
            {
                gameManager.ReturnToLobby();
            }
        }

        private void OnPlayAgain()
        {
            var gameManager = NetworkGameManager.Instance;
            if (gameManager != null)
            {
                gameManager.RestartGame();
            }
        }
    }
}
