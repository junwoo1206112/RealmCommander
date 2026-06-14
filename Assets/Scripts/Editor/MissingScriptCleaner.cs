using UnityEngine;
using UnityEditor;

namespace RealmCommander.Editor
{
    public static class MissingScriptCleaner
    {
        [MenuItem("Tools/Realm Commander/Remove ALL Missing Scripts (Force)")]
        public static void ForceRemoveAllMissingScripts()
        {
            int totalFixed = 0;

            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab);
                if (count > 0)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
                    EditorUtility.SetDirty(prefab);
                    totalFixed += count;
                    Debug.Log($"[Cleaner] Fixed {count} in prefab: {path}");
                }
            }

            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    if (count > 0)
                    {
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                        totalFixed += count;
                        Debug.Log($"[Cleaner] Fixed {count} on: {t.gameObject.name}");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Cleaner] Total removed: {totalFixed}. Now rebuild.");
        }
    }
}
