using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace RealmCommander.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI versionText;

        private void Start()
        {
            if (titleText != null)
            {
                titleText.text = "영웅의 전장\nRealm Commander";
            }

            if (versionText != null)
            {
                versionText.text = $"v1.0.0";
            }

            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGame);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuit);
            }
        }

        public void OnStartGame()
        {
            SceneManager.LoadScene("LobbyScene");
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
