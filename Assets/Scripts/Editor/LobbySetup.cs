using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using RealmCommander.UI;

namespace RealmCommander.Editor
{
    public static class LobbySetup
    {
        private const float Spacing = 55f;
        private const float StartY = 250f;

        [MenuItem("Tools/Realm Commander/Setup Lobby UI")]
        public static void SetupLobbyUI()
        {
            if (!EditorSceneManager.GetActiveScene().name.Contains("Lobby"))
            {
                Debug.LogWarning("[LobbySetup] Please open LobbyScene first.");
                return;
            }

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[LobbySetup] No Canvas found in scene.");
                return;
            }

            LobbyUI lobbyUI = canvas.GetComponent<LobbyUI>();
            if (lobbyUI == null)
                lobbyUI = canvas.gameObject.AddComponent<LobbyUI>();

            SerializedObject so = new SerializedObject(lobbyUI);

            GameObject hostBtn = FindChild(canvas.transform, "HostButton");
            GameObject joinBtn = FindChild(canvas.transform, "JoinButton");
            GameObject backBtn = FindChild(canvas.transform, "BackButton");

            GameObject titleObj = FindChild(canvas.transform, "Title");
            if (titleObj != null)
            {
                PositionCentered(titleObj, StartY + Spacing);
                var tmpText = titleObj.GetComponent<TextMeshProUGUI>();
                if (tmpText != null)
                {
                    tmpText.fontSize = 36;
                    tmpText.color = Color.white;
                }
                else
                {
                    var uiText = titleObj.GetComponent<Text>();
                    if (uiText != null)
                    {
                        uiText.fontSize = 36;
                        uiText.color = Color.white;
                    }
                }
            }

            if (hostBtn != null)
            {
                PositionCentered(hostBtn, StartY);
                SetButtonSize(hostBtn, 220, 50);
                SetButtonColor(hostBtn, new Color(0.2f, 0.45f, 0.8f));
            }
            if (joinBtn != null)
            {
                PositionCentered(joinBtn, StartY - Spacing);
                SetButtonSize(joinBtn, 220, 50);
                SetButtonColor(joinBtn, new Color(0.2f, 0.6f, 0.35f));
            }

            GameObject ipFieldObj = FindChild(canvas.transform, "IpInputField");
            if (ipFieldObj == null)
            {
                ipFieldObj = CreateTMPInputField(canvas.transform, "IpInputField", "127.0.0.1");
                Debug.Log("[LobbySetup] Created IpInputField");
            }
            PositionCentered(ipFieldObj, StartY - Spacing * 2);
            SetSize(ipFieldObj, 250, 40);

            if (backBtn != null)
            {
                PositionCentered(backBtn, StartY - Spacing * 3);
                SetButtonSize(backBtn, 220, 50);
                SetButtonColor(backBtn, new Color(0.5f, 0.5f, 0.5f));
            }

            GameObject statusPanelObj = FindChild(canvas.transform, "StatusPanel");
            if (statusPanelObj == null)
            {
                statusPanelObj = new GameObject("StatusPanel");
                statusPanelObj.transform.SetParent(canvas.transform, false);
                Image bg = statusPanelObj.AddComponent<Image>();
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                Debug.Log("[LobbySetup] Created StatusPanel");
            }
            PositionCentered(statusPanelObj, StartY - Spacing * 4);
            SetSize(statusPanelObj, 300, 36);

            GameObject statusTextObj = FindChild(statusPanelObj.transform, "StatusText");
            if (statusTextObj == null)
            {
                statusTextObj = new GameObject("StatusText");
                statusTextObj.transform.SetParent(statusPanelObj.transform, false);
                RectTransform rt = statusTextObj.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.localPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                TextMeshProUGUI tmp = statusTextObj.AddComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 14;
                tmp.color = Color.white;
            }
            so.FindProperty("statusPanel").objectReferenceValue = statusPanelObj;
            so.FindProperty("statusText").objectReferenceValue = statusTextObj.GetComponent<TextMeshProUGUI>();

            GameObject localIPTextObj = FindChild(canvas.transform, "LocalIPText");
            if (localIPTextObj == null)
            {
                localIPTextObj = new GameObject("LocalIPText");
                localIPTextObj.transform.SetParent(canvas.transform, false);
                TextMeshProUGUI tmp = localIPTextObj.AddComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 13;
                tmp.color = new Color(0.7f, 0.7f, 0.7f);
            }
            PositionCentered(localIPTextObj, StartY - Spacing * 5);
            SetSize(localIPTextObj, 300, 30);

            var ipInput = ipFieldObj.GetComponent<TMP_InputField>();
            so.FindProperty("ipInputField").objectReferenceValue = ipInput;
            so.FindProperty("localIPText").objectReferenceValue = localIPTextObj.GetComponent<TextMeshProUGUI>();

            if (hostBtn != null)
            {
                var hostBtnComp = hostBtn.GetComponent<Button>();
                so.FindProperty("hostButton").objectReferenceValue = hostBtnComp;
            }
            if (joinBtn != null)
            {
                var joinBtnComp = joinBtn.GetComponent<Button>();
                so.FindProperty("joinButton").objectReferenceValue = joinBtnComp;
            }
            if (backBtn != null)
            {
                var backBtnComp = backBtn.GetComponent<Button>();
                so.FindProperty("backButton").objectReferenceValue = backBtnComp;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[LobbySetup] PASS - Layout repositioned + all fields connected");
        }

        private static void PositionCentered(GameObject obj, float yOffset)
        {
            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, yOffset);
        }

        private static void SetSize(GameObject obj, float w, float h)
        {
            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(w, h);
        }

        private static void SetButtonSize(GameObject obj, float w, float h)
        {
            SetSize(obj, w, h);
        }

        private static void SetButtonColor(GameObject obj, Color color)
        {
            Image img = obj.GetComponent<Image>();
            if (img != null) img.color = color;
        }

        private static GameObject CreateTMPInputField(Transform parent, string name, string defaultText)
        {
            GameObject inputObj = new GameObject(name);
            inputObj.transform.SetParent(parent, false);

            RectTransform rt = inputObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250, 40);

            Image bg = inputObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            TMP_InputField input = inputObj.AddComponent<TMP_InputField>();

            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform textAreaRT = textArea.AddComponent<RectTransform>();
            textAreaRT.anchorMin = Vector2.zero;
            textAreaRT.anchorMax = Vector2.one;
            textAreaRT.sizeDelta = Vector2.zero;
            textAreaRT.offsetMin = new Vector2(10, 0);
            textAreaRT.offsetMax = new Vector2(-10, 0);

            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(textArea.transform, false);
            RectTransform phRT = placeholder.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.sizeDelta = Vector2.zero;
            TextMeshProUGUI phText = placeholder.AddComponent<TextMeshProUGUI>();
            phText.text = "Enter IP...";
            phText.fontSize = 14;
            phText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            GameObject inputText = new GameObject("Text");
            inputText.transform.SetParent(textArea.transform, false);
            RectTransform txtRT = inputText.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.sizeDelta = Vector2.zero;
            TextMeshProUGUI txt = inputText.AddComponent<TextMeshProUGUI>();
            txt.text = defaultText;
            txt.fontSize = 14;
            txt.color = Color.white;

            input.textViewport = textAreaRT;
            input.textComponent = txt;
            input.placeholder = phText;

            return inputObj;
        }

        private static GameObject FindChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name) return child.gameObject;
                GameObject found = FindChild(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
