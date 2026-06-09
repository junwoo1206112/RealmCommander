using UnityEngine;
using UnityEditor;
using Mirror;
using System.Collections.Generic;

namespace RealmCommander.Editor
{
    public class PrefabNetworkIdentityFixer
    {
        [MenuItem("Tools/Realm Commander/Fix Prefab NetworkIdentity")]
        public static void FixPrefabNetworkIdentities()
        {
            int fixedCount = 0;
            var fixedPrefabs = new List<string>();

            // 모든 프리팹 검색
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");

            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;

                // NetworkBehaviour 컴포넌트가 있는지 확인
                var networkBehaviours = prefab.GetComponentsInChildren<NetworkBehaviour>();

                if (networkBehaviours.Length > 0 && prefab.GetComponent<NetworkIdentity>() == null)
                {
                    // 프리팹 수정 모드로 진입
                    using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
                    {
                        var prefabRoot = editScope.prefabContentsRoot;

                        // NetworkIdentity 추가
                        if (prefabRoot.GetComponent<NetworkIdentity>() == null)
                        {
                            prefabRoot.AddComponent<NetworkIdentity>();
                            fixedCount++;
                            fixedPrefabs.Add(path);
                            Debug.Log($"[FIX] Added NetworkIdentity to prefab: {path}");
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string message = fixedCount > 0
                ? $"{fixedCount}개의 프리팹에 NetworkIdentity를 추가했습니다:\n\n{string.Join("\n", fixedPrefabs)}"
                : "NetworkIdentity가 필요한 프리팹을 찾지 못했습니다.";

            EditorUtility.DisplayDialog("Prefab NetworkIdentity Fix Complete", message, "확인");
        }

        [MenuItem("Tools/Realm Commander/Validate Prefabs")]
        public static void ValidatePrefabs()
        {
            var issues = new List<string>();
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");

            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;

                var networkBehaviours = prefab.GetComponentsInChildren<NetworkBehaviour>();

                if (networkBehaviours.Length > 0 && prefab.GetComponent<NetworkIdentity>() == null)
                {
                    issues.Add($"❌ {path}: NetworkBehaviour 있음 but NetworkIdentity 없음");
                }
            }

            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Prefab Validation",
                    "모든 프리팹이 올바릅니다!", "확인");
            }
            else
            {
                string message = "발견된 문제:\n\n" + string.Join("\n", issues);
                EditorUtility.DisplayDialog("Prefab Validation", message, "확인");
            }
        }
    }
}
