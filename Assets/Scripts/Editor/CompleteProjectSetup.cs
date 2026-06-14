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
                "3. MainScene 카메라 컨트롤러 추가\n" +
                "4. CommandInput/BoxSelector 추가\n" +
                "5. NetworkManagerHUD 추가",
                "실행", "취소"))
            {
                return;
            }

            CreateMainMenuScene();
            CreateLobbyScene();
            FixMainSceneCamera();
            AddInputControllers();
            AddNetworkManagerHUD();

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Complete!",
                "모든 설정이 완료되었습니다!\n\n" +
                "카메라 조작법:\n" +
                "  WASD: 이동\n" +
                "  Q/E: 시점 회전\n" +
                "  마우스 휠: 줌", "확인");
        }

        private static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenuScene";

            GameObject canvas = new GameObject("Canvas");
            canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();

            // Title
            CreateText(canvas.transform, "Title", "Realm Commander", 48, new Vector2(0.5f, 0.8f), new Vector2(400, 200));

            // Start Button
            GameObject startBtn = CreateButton(canvas.transform, "StartButton", "Start Game", new Color(0.2f, 0.6f, 0.2f), new Vector2(0.5f, 0.4f), new Vector2(200, 50));

            // Quit Button
            GameObject quitBtn = CreateButton(canvas.transform, "QuitButton", "Quit", new Color(0.6f, 0.2f, 0.2f), new Vector2(0.5f, 0.3f), new Vector2(200, 50));

            CreateCameraAndEventSystem();

            // Add MainMenuUI and wire buttons
            var mainMenuUI = canvas.AddComponent<RealmCommander.UI.MainMenuUI>();
            var so = new SerializedObject(mainMenuUI);
            so.FindProperty("startGameButton").objectReferenceValue = startBtn.GetComponent<Button>();
            so.FindProperty("quitButton").objectReferenceValue = quitBtn.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenuScene.unity");
        }

        private static void CreateLobbyScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "LobbyScene";

            GameObject canvas = new GameObject("Canvas");
            canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();

            CreateText(canvas.transform, "Title", "Game Lobby", 36, new Vector2(0.5f, 0.8f), new Vector2(300, 100));

            GameObject hostBtn = CreateButton(canvas.transform, "HostButton", "Host Game", new Color(0.2f, 0.4f, 0.8f), new Vector2(0.5f, 0.6f), new Vector2(200, 50));
            GameObject joinBtn = CreateButton(canvas.transform, "JoinButton", "Join Game", new Color(0.2f, 0.6f, 0.4f), new Vector2(0.5f, 0.5f), new Vector2(200, 50));
            GameObject backBtn = CreateButton(canvas.transform, "BackButton", "Back", new Color(0.5f, 0.5f, 0.5f), new Vector2(0.5f, 0.4f), new Vector2(200, 50));

            CreateCameraAndEventSystem();

            // Add LobbyUI and wire buttons
            var lobbyUI = canvas.AddComponent<RealmCommander.UI.LobbyUI>();
            var so = new SerializedObject(lobbyUI);
            so.FindProperty("hostButton").objectReferenceValue = hostBtn.GetComponent<Button>();
            so.FindProperty("joinButton").objectReferenceValue = joinBtn.GetComponent<Button>();
            so.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/LobbyScene.unity");
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
            Camera cam = camera.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camera.AddComponent<AudioListener>();
            camera.AddComponent<RealmCommander.RTS.MobileRTSCameraController>();

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

        private static void FixMainSceneCamera()
        {
            string mainScenePath = "Assets/Scenes/MainScene.unity";
            if (!System.IO.File.Exists(mainScenePath))
            {
                Debug.LogWarning("[Setup] MainScene.unity not found, skipping camera fix");
                return;
            }

            var scene = EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);

            Camera cam = Camera.main;
            if (cam == null)
            {
                var camObj = GameObject.Find("Main Camera");
                if (camObj != null) cam = camObj.GetComponent<Camera>();
            }
            if (cam == null)
            {
                var camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                cam.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
                Debug.Log("[Setup] Main Camera created in MainScene");
            }

            if (cam.GetComponent<RealmCommander.RTS.MobileRTSCameraController>() == null)
            {
                cam.gameObject.AddComponent<RealmCommander.RTS.MobileRTSCameraController>();
                Debug.Log("[Setup] MobileRTSCameraController added to MainScene camera");
            }

            EditorSceneManager.SaveScene(scene);
        }
    }
}
