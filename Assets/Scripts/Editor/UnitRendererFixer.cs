using UnityEngine;
using UnityEditor;
using RealmCommander.Core;
using RealmCommander.RTS;

namespace RealmCommander.Editor
{
    public class UnitRendererFixer
    {
        [MenuItem("Tools/Realm Commander/Fix Unit Renderers")]
        public static void FixAllUnitRenderers()
        {
            int fixedCount = 0;

            // 의 모든 유닛 검색
            var units = Object.FindObjectsByType<RTS.Unit>(FindObjectsSortMode.None);

            foreach (var unit in units)
            {
                var renderer = unit.GetComponent<Renderer>();
                if (renderer == null)
                {
                    // Renderer가 없으면 MeshRenderer 추가
                    renderer = unit.gameObject.AddComponent<MeshRenderer>();
                    fixedCount++;
                    Debug.Log($"[FIX] Added MeshRenderer to: {unit.gameObject.name}");
                }

                // MeshFilter 확인
                var meshFilter = unit.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    meshFilter = unit.gameObject.AddComponent<MeshFilter>();
                    // 기본 큐브 메쉬 할당
                    meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                    fixedCount++;
                    Debug.Log($"[FIX] Added MeshFilter with Cube mesh to: {unit.gameObject.name}");
                }

                // 머티리얼 확인
                if (renderer.sharedMaterial == null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = unit.IsEnemy ? Color.red : Color.blue;
                    renderer.sharedMaterial = mat;
                    fixedCount++;
                    Debug.Log($"[FIX] Created material for: {unit.gameObject.name}");
                }
            }

            EditorUtility.DisplayDialog("Unit Renderer Fix",
                $"{fixedCount}개의 유닛을 수정했습니다.", "확인");
        }

        [MenuItem("Tools/Realm Commander/Validate Scene")]
        public static void ValidateScene()
        {
            var issues = new System.Collections.Generic.List<string>();

            // 유닛 확인
            var units = Object.FindObjectsByType<RTS.Unit>(FindObjectsSortMode.None);
            foreach (var unit in units)
            {
                if (unit.GetComponent<Renderer>() == null)
                {
                    issues.Add($"❌ {unit.gameObject.name}: Renderer 없음");
                }
                if (unit.GetComponent<MeshFilter>() == null)
                {
                    issues.Add($"❌ {unit.gameObject.name}: MeshFilter 없음");
                }
                if (unit.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
                {
                    issues.Add($"❌ {unit.gameObject.name}: NavMeshAgent 없음");
                }
            }

            // 매니저 확인
            if (Object.FindFirstObjectByType<RTS.ResourceManager>() == null)
            {
                issues.Add("❌ ResourceManager 없음");
            }
            if (Object.FindFirstObjectByType<Core.SelectionManager>() == null)
            {
                issues.Add("❌ SelectionManager 없음");
            }
            if (Object.FindFirstObjectByType<Core.CommandManager>() == null)
            {
                issues.Add("❌ CommandManager 없음");
            }

            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Scene Validation",
                    "씬이 올바릅니다!", "확인");
            }
            else
            {
                string message = "발견된 문제:\n\n" + string.Join("\n", issues);
                EditorUtility.DisplayDialog("Scene Validation", message, "확인");
            }
        }
    }
}
