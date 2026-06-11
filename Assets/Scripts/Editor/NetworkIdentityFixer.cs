using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mirror;
using System.Collections.Generic;

namespace RealmCommander.Editor
{
    [InitializeOnLoad]
    public class NetworkIdentityFixer
    {
        static NetworkIdentityFixer()
        {
            EditorApplication.delayCall += OnFirstUpdate;
        }

        private static void OnFirstUpdate()
        {
            EditorApplication.delayCall -= OnFirstUpdate;
            EditorApplication.update += AutoFixOnStartup;
        }

        private static void AutoFixOnStartup()
        {
            EditorApplication.update -= AutoFixOnStartup;
            
            if (Application.isPlaying)
                return;

            if (!EditorPrefs.GetBool("RealmCommander_AutoFixNetworkIdentity", true))
                return;

            int fixedCount = FixAllScenesNetworkIdentities(true);
            fixedCount += FixAllPrefabNetworkIdentities();

            if (fixedCount > 0)
            {
                Debug.Log($"[AutoFix] {fixedCount}개 오브젝트에 NetworkIdentity 추가 후 씬 저장 완료");
            }
        }

        private static int FixAllPrefabNetworkIdentities()
        {
            int fixedCount = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
                {
                    GameObject root = editScope.prefabContentsRoot;
                    if (root.GetComponentsInChildren<NetworkBehaviour>(true).Length == 0 ||
                        root.GetComponent<NetworkIdentity>() != null)
                    {
                        continue;
                    }

                    root.AddComponent<NetworkIdentity>();
                    fixedCount++;
                    Debug.Log($"[AutoFix] Added NetworkIdentity to prefab: {path}");
                }
            }

            if (fixedCount > 0) AssetDatabase.SaveAssets();
            return fixedCount;
        }

        [MenuItem("Tools/Realm Commander/Fix NetworkIdentity (Enhanced)")]
        public static void FixAllNetworkIdentities()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("실행 불가", "Play 모드에서는 사용할 수 없습니다.", "확인");
                return;
            }

            int fixedCount = FixAllScenesNetworkIdentities(true);
            string message = fixedCount > 0
                ? $"{fixedCount}개의 GameObject에 NetworkIdentity를 추가하고 씬을 저장했습니다."
                : "모든 씬이 이미 올바르게 설정되어 있습니다.";

            EditorUtility.DisplayDialog("NetworkIdentity Fix Complete", message, "확인");
        }

        public static void RepairProjectNetworkIdentities()
        {
            int fixedCount = FixAllScenesNetworkIdentities(true) + FixAllPrefabNetworkIdentities();
            Debug.Log($"[NetworkIdentityFixer] Project repair complete. Fixed: {fixedCount}");
        }

        private static int FixAllScenesNetworkIdentities(bool saveScenes)
        {
            if (Application.isPlaying)
                return 0;

            int totalFixed = 0;
            var loadedScenes = new HashSet<string>();

            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                loadedScenes.Add(EditorSceneManager.GetSceneAt(i).path);
            }

            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            foreach (var guid in sceneGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                var allObjects = scene.GetRootGameObjects();
                foreach (var rootObj in allObjects)
                {
                    var allChildren = rootObj.GetComponentsInChildren<NetworkBehaviour>(true);
                    foreach (var nb in allChildren)
                    {
                        var go = nb.gameObject;
                        if (go.GetComponent<NetworkIdentity>() == null)
                        {
                            Undo.RecordObject(go, "Add NetworkIdentity");
                            go.AddComponent<NetworkIdentity>();
                            totalFixed++;
                            Debug.Log($"[AutoFix] Added NetworkIdentity to: {go.name} in {scene.name}");
                        }
                    }
                }

                if (saveScenes)
                {
                    EditorSceneManager.SaveScene(scene);
                }

                if (!loadedScenes.Contains(path))
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            if (saveScenes)
            {
                AssetDatabase.SaveAssets();
            }

            return totalFixed;
        }

        [MenuItem("Tools/Realm Commander/Validate Network Setup")]
        public static void ValidateNetworkSetup()
        {
            var issues = new List<string>();

            // NetworkManager 확인
            var nm = Object.FindFirstObjectByType<NetworkManager>();
            if (nm == null)
            {
                issues.Add(" NetworkManager가 씬에 없습니다.");
            }
            else
            {
                if (nm.GetComponent<NetworkIdentity>() == null)
                {
                    issues.Add("❌ NetworkManager에 NetworkIdentity가 없습니다.");
                }
                else
                {
                    issues.Add("✅ NetworkManager 설정 OK");
                }
            }

            // 모든 NetworkBehaviour 확인
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            int checkedCount = 0;
            int okCount = 0;

            foreach (var obj in allObjects)
            {
                if (!obj.scene.IsValid()) continue;

                var networkBehaviours = obj.GetComponents<NetworkBehaviour>();
                if (networkBehaviours.Length > 0)
                {
                    checkedCount++;
                    if (obj.GetComponent<NetworkIdentity>() != null)
                    {
                        okCount++;
                    }
                    else
                    {
                        issues.Add($"❌ {obj.name}: NetworkBehaviour 있음 but NetworkIdentity 없음");
                    }
                }
            }

            issues.Add($"\n📊 통계: {checkedCount}개 확인, {okCount}개 OK, {checkedCount - okCount}개 문제");

            string message = string.Join("\n", issues);
            EditorUtility.DisplayDialog("Network Validation", message, "확인");
        }
    }
}
