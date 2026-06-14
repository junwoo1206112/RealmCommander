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

        private static GameResultUI instance;

        private void Awake()
        {
            instance = this;
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

        public static void Show(bool isVictory)
        {
            if (instance == null) return;

            if (instance.resultPanel != null)
                instance.resultPanel.SetActive(true);

            if (instance.resultText != null)
            {
                instance.resultText.text = isVictory ? "VICTORY" : "DEFEAT";
                instance.resultText.color = isVictory ? Color.yellow : Color.red;
            }

            if (instance.detailText != null)
            {
                instance.detailText.text = isVictory
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
                gameManager.ReturnToLobby();
            }
        }
    }
}
