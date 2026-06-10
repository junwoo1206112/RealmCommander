using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mirror;
using RealmCommander.Core;
using RealmCommander.RTS;

namespace RealmCommander.Editor
{
    public class CompleteProjectSetup
    {
        [MenuItem("Tools/Realm Commander/Complete Setup (All-in-One)")]
        public static void CompleteSetup()
        {
            if (!EditorUtility.DisplayDialog("Complete Setup",
                "프로젝트 전체 설정을 완료하시겠습니까?\n\n" +
                "1. MainMenuScene 생성\n" +
                "2. LobbyScene 생성\n" +
                "3. MainScene 유닛 렌더러 수정\n" +
                "4. CommandInput/BoxSelector 추가\n" +
                "5. NetworkManagerHUD 추가",
                "실행", "취소"))
            {
                return;
            }

            CreateMainMenuScene();
            CreateLobbyScene();
            FixUnitRenderers();
            AddInputControllers();
            AddNetworkManagerHUD();

            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Complete!",
                "모든 설정이 완료되었습니다!", "확인");
        }

        private static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "MainMenuScene";

            GameObject canvas = new GameObject("Canvas");
            canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();

            // Title
            CreateText(canvas.transform, "Title", "영웅의 전장\nRealm Commander", 48, new Vector2(0.5f, 0.8f), new Vector2(400, 200));

            // Start Button
            GameObject startBtn = CreateButton(canvas.transform, "StartButton", "Start Game", new Color(0.2f, 0.6f, 0.2f), new Vector2(0.5f, 0.4f), new Vector2(200, 50));
            startBtn.GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene("LobbyScene"));

            // Quit Button
            GameObject quitBtn = CreateButton(canvas.transform, "QuitButton", "Quit", new Color(0.6f, 0.2f, 0.2f), new Vector2(0.5f, 0.3f), new Vector2(200, 50));
            quitBtn.GetComponent<Button>().onClick.AddListener(() => Application.Quit());

            CreateCameraAndEventSystem();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenuScene.unity");
            EditorSceneManager.CloseScene(scene, true);
        }

        private static void CreateLobbyScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "LobbyScene";

            GameObject canvas = new GameObject("Canvas");
            canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();

            CreateText(canvas.transform, "Title", "Game Lobby", 36, new Vector2(0.5f, 0.8f), new Vector2(300, 100));

            GameObject hostBtn = CreateButton(canvas.transform, "HostButton", "Host Game", new Color(0.2f, 0.4f, 0.8f), new Vector2(0.5f, 0.6f), new Vector2(200, 50));
            hostBtn.GetComponent<Button>().onClick.AddListener(() => {
                var nm = NetworkManager.singleton;
                if (nm != null) { nm.StartHost(); SceneManager.LoadScene("MainScene"); }
            });

            GameObject joinBtn = CreateButton(canvas.transform, "JoinButton", "Join Game", new Color(0.2f, 0.6f, 0.4f), new Vector2(0.5f, 0.5f), new Vector2(200, 50));
            joinBtn.GetComponent<Button>().onClick.AddListener(() => {
                var nm = NetworkManager.singleton;
                if (nm != null) { nm.StartClient(); SceneManager.LoadScene("MainScene"); }
            });

            CreateCameraAndEventSystem();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/LobbyScene.unity");
            EditorSceneManager.CloseScene(scene, true);
        }

        private static GameObject CreateText(Transform parent, string name, string text, int fontSize, Vector2 anchor, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            var t = obj.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            return obj;
        }

        private static GameObject CreateButton(Transform parent, string name, string text, Color color, Vector2 anchor, Vector2 size)
        {
            GameObject btn = new GameObject(name);
            btn.transform.SetParent(parent);
            btn.AddComponent<Image>().color = color;
            var rect = btn.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            btn.AddComponent<Button>();

            var btnText = CreateText(btn.transform, "Text", text, 20, new Vector2(0.5f, 0.5f), size);
            return btn;
        }

        private static void CreateCameraAndEventSystem()
        {
            GameObject camera = new GameObject("Main Camera");
            camera.AddComponent<Camera>();
            camera.AddComponent<AudioListener>();

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private static void FixUnitRenderers()
        {
            var units = Object.FindObjectsByType<RTS.Unit>(FindObjectsSortMode.None);
            foreach (var unit in units)
            {
                if (unit.GetComponent<Renderer>() == null)
                    unit.gameObject.AddComponent<MeshRenderer>();

                if (unit.GetComponent<MeshFilter>() == null)
                {
                    var mf = unit.gameObject.AddComponent<MeshFilter>();
                    mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                }

                var renderer = unit.GetComponent<Renderer>();
                if (renderer.sharedMaterial == null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = unit.IsEnemy ? Color.red : Color.blue;
                    renderer.sharedMaterial = mat;
                }
            }
        }

        private static void AddInputControllers()
        {
            if (GameObject.Find("CommandInput") == null)
                new GameObject("CommandInput").AddComponent<RealmCommander.RTS.CommandInput>();

            if (GameObject.Find("BoxSelector") == null)
                new GameObject("BoxSelector").AddComponent<RealmCommander.RTS.BoxSelector>();
        }

        private static void AddNetworkManagerHUD()
        {
            var nm = Object.FindFirstObjectByType<NetworkManager>();
            if (nm != null && nm.GetComponent<NetworkManagerHUD>() == null)
                nm.gameObject.AddComponent<NetworkManagerHUD>();
        }
    }
}
