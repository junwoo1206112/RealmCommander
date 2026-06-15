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
            AutoWireReferences();

            if (startGameButton == null || titleText == null)
                CreateFallbackUI();

            if (titleText != null)
                titleText.text = "Realm Commander";

            if (versionText != null)
                versionText.text = "v1.0.0";

            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGame);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuit);

            Audio.AudioManager.Instance?.PlayMenuMusic();
        }

        private void AutoWireReferences()
        {
            if (startGameButton == null)
                startGameButton = FindInChildren<Button>("StartButton");
            if (quitButton == null)
                quitButton = FindInChildren<Button>("QuitButton");
            if (titleText == null)
                titleText = FindInChildren<TextMeshProUGUI>("Title");
            if (versionText == null)
                versionText = FindInChildren<TextMeshProUGUI>("VersionText");

            if (titleText == null)
            {
                foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (tmp != null && tmp.text.Contains("Realm"))
                    {
                        titleText = tmp;
                        break;
                    }
                }
            }
        }

        private void CreateFallbackUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            if (titleText == null)
            {
                var titleObj = new GameObject("Title");
                titleObj.transform.SetParent(canvas.transform, false);
                titleText = titleObj.AddComponent<TextMeshProUGUI>();
                titleText.text = "Realm Commander";
                titleText.fontSize = 48;
                titleText.alignment = TextAlignmentOptions.Center;
                var rt = titleObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.8f);
                rt.anchorMax = new Vector2(0.5f, 0.8f);
                rt.sizeDelta = new Vector2(400, 80);
            }

            if (startGameButton == null)
                startGameButton = CreateButton(canvas.transform, "StartButton", "Start Game", new Color(0.2f, 0.6f, 0.2f), new Vector2(0.5f, 0.4f));

            if (quitButton == null)
                quitButton = CreateButton(canvas.transform, "QuitButton", "Quit", new Color(0.6f, 0.2f, 0.2f), new Vector2(0.5f, 0.3f));
        }

        private Button CreateButton(Transform parent, string name, string text, Color color, Vector2 anchor)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var image = btnObj.AddComponent<Image>();
            image.color = color;
            var btn = btnObj.AddComponent<Button>();
            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(200, 50);

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.fontSize = 20;
            txt.alignment = TextAlignmentOptions.Center;
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            return btn;
        }

        private T FindInChildren<T>(string objectName) where T : Component
        {
            foreach (var component in GetComponentsInChildren<T>(true))
            {
                if (component != null && component.gameObject.name == objectName)
                    return component;
            }
            return null;
        }

        public void OnStartGame()
        {
            Audio.AudioManager.Instance?.PlayClick();
            SceneManager.LoadScene("LobbyScene");
        }

        public void OnQuit()
        {
            Audio.AudioManager.Instance?.PlayClick();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
