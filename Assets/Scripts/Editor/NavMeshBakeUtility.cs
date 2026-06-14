using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RealmCommander.Editor
{
    public static class NavMeshBakeUtility
    {
        [MenuItem("Tools/Realm Commander/Bake MainScene NavMesh")]
        public static void BakeMainSceneNavMesh()
        {
            const string scenePath = "Assets/Scenes/MainScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath);
            int bakedSurfaces = 0;

            foreach (var surface in Object.FindObjectsByType<Unity.AI.Navigation.NavMeshSurface>(FindObjectsSortMode.None))
            {
                surface.BuildNavMesh();
                EditorUtility.SetDirty(surface);
                bakedSurfaces++;
            }

            if (bakedSurfaces == 0)
            {
                GameObject ground = GameObject.Find("Ground");
                if (ground == null)
                {
                    Debug.LogError("[NavMeshBake] Ground object not found; cannot add NavMeshSurface.");
                    return;
                }

                var surface = ground.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
                surface.collectObjects = Unity.AI.Navigation.CollectObjects.All;
                surface.BuildNavMesh();
                EditorUtility.SetDirty(surface);
                bakedSurfaces = 1;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[NavMeshBake] Baked {bakedSurfaces} NavMeshSurface(s) in MainScene.");
        }
    }
}
