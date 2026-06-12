using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Mirror;
using RealmCommander.Network;

namespace RealmCommander.Editor
{
    public class NetworkSetup : EditorWindow
    {
        [MenuItem("Tools/Realm Commander/Setup Network")]
        public static void SetupNetwork()
        {
            if (!EditorUtility.DisplayDialog("Network Setup",
                "Realm Commander 네트워크 환경을 설정하시겠습니까?\n\n" +
                "1. NetworkManager 생성\n" +
                "2. NetworkPlayer 프리팹 설정\n" +
                "3. NetworkGameManager 생성\n" +
                "4. LobbyManager 생성\n" +
                "5. CombatManager 생성",
                "세팅", "취소"))
            {
                return;
            }

            CreateNetworkManager();
            CreateManagers();

            EditorUtility.DisplayDialog("완료", "네트워크 세팅이 완료되었습니다!", "확인");
        }

        private static void CreateNetworkManager()
        {
            GameObject networkManager = GameObject.Find("NetworkManager");
            if (networkManager == null)
            {
                networkManager = new GameObject("NetworkManager");
            }

            var manager = networkManager.GetComponent<NetworkManager>();
            if (manager == null)
            {
                manager = networkManager.AddComponent<NetworkManager>();
            }

            manager.networkAddress = "localhost";

            var identity = networkManager.GetComponent<NetworkIdentity>();
            if (identity == null)
            {
                identity = networkManager.AddComponent<NetworkIdentity>();
            }

            var transport = networkManager.GetComponent<Transport>();
            if (transport == null)
            {
                transport = networkManager.AddComponent<TelepathyTransport>();
            }

            manager.transport = transport;

            Undo.RegisterCreatedObjectUndo(networkManager, "Create NetworkManager");

            manager.onlineScene = "Assets/Scenes/LobbyScene.unity";
            manager.offlineScene = "Assets/Scenes/MainMenuScene.unity";

            Debug.Log("NetworkManager 생성 완료");
        }

        private static void CreateManagers()
        {
            GameObject networkGameManager = new GameObject("NetworkGameManager");
            networkGameManager.AddComponent<NetworkGameManager>();
            Undo.RegisterCreatedObjectUndo(networkGameManager, "Create NetworkGameManager");

            GameObject lobbyManager = new GameObject("LobbyManager");
            lobbyManager.AddComponent<LobbyManager>();
            Undo.RegisterCreatedObjectUndo(lobbyManager, "Create LobbyManager");

            GameObject combatManager = new GameObject("CombatManager");
            combatManager.AddComponent<CombatManager>();
            Undo.RegisterCreatedObjectUndo(combatManager, "Create CombatManager");

            Debug.Log("네트워크 매니저 생성 완료");
        }

        [MenuItem("Tools/Realm Commander/Create Menu Scenes")]
        public static void CreateMenuScenes()
        {
            if (!EditorUtility.DisplayDialog("Create Scenes",
                "메뉴 씬을 생성하시겠습니까?\n\n" +
                "- MainMenuScene\n- LobbyScene",
                "생성", "취소"))
            {
                return;
            }

            if (!System.IO.Directory.Exists("Assets/Scenes"))
            {
                System.IO.Directory.CreateDirectory("Assets/Scenes");
                AssetDatabase.Refresh();
            }

            Scene mainMenu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            mainMenu.name = "MainMenuScene";
            EditorSceneManager.SaveScene(mainMenu, "Assets/Scenes/MainMenuScene.unity");

            Scene lobby = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            lobby.name = "LobbyScene";
            EditorSceneManager.SaveScene(lobby, "Assets/Scenes/LobbyScene.unity");

            EditorSceneManager.CloseScene(mainMenu, true);
            EditorSceneManager.CloseScene(lobby, true);

            AssetDatabase.Refresh();
            Debug.Log("메뉴 씬 생성 완료");
        }

        [MenuItem("Tools/Realm Commander/Fix Transport Warning")]
        public static void FixTransportWarning()
        {
            var nm = Object.FindFirstObjectByType<NetworkManager>();
            if (nm == null)
            {
                EditorUtility.DisplayDialog("Transport Fix", "NetworkManager를 찾을 수 없습니다.", "확인");
                return;
            }

            if (nm.transport != null)
            {
                EditorUtility.DisplayDialog("Transport Fix",
                    $"Transport가 이미 할당되어 있습니다: {nm.transport.GetType().Name}", "확인");
                return;
            }

            var transport = nm.gameObject.GetComponent<Transport>();
            if (transport == null)
            {
                transport = nm.gameObject.AddComponent<TelepathyTransport>();
            }

            Undo.RecordObject(nm, "Assign Transport");
            nm.transport = transport;

            EditorUtility.SetDirty(nm);

                EditorUtility.DisplayDialog("Transport Fix",
                    $"Transport를 할당했습니다: {transport.GetType().Name}\n경고가 사라집니다.", "확인");
        }

        [MenuItem("Tools/Realm Commander/Fix Camera Controller")]
        public static void FixCameraController()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camObj = GameObject.Find("Main Camera");
                if (camObj != null) cam = camObj.GetComponent<Camera>();
            }
            if (cam == null)
            {
                EditorUtility.DisplayDialog("Error", "Main Camera를 찾을 수 없습니다.", "확인");
                return;
            }

            if (cam.GetComponent<RealmCommander.RTS.MobileRTSCameraController>() == null)
            {
                Undo.AddComponent<RealmCommander.RTS.MobileRTSCameraController>(cam.gameObject);
                EditorUtility.DisplayDialog("완료",
                    $"Main Camera에 MobileRTSCameraController를 추가했습니다.\n" +
                    $"위치: {cam.gameObject.name}\n" +
                    $"카메라 이동: WASD / 중앙클릭 드래그\n" +
                    $"시점 회전: Q / E\n" +
                    $"줌: 마우스 휠", "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("Info",
                    "MobileRTSCameraController가 이미 존재합니다.\n" +
                    $"위치: {cam.gameObject.name}\n" +
                    $"enabled: {cam.GetComponent<RealmCommander.RTS.MobileRTSCameraController>().enabled}", "확인");
            }
        }
    }
}
