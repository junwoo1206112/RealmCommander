using UnityEngine;
using UnityEditor;
using RealmCommander.RTS;

namespace RealmCommander.Editor
{
    public class MaterialFixer
    {
        [MenuItem("Tools/Realm Commander/Fix Materials")]
        public static void FixAllMaterials()
        {
            int fixedCount = 0;
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (var obj in allObjects)
            {
                var renderers = obj.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer.sharedMaterial == null)
                    {
                        // Material이 없는 경우 기본 Material 생성
                        var defaultMaterial = new Material(Shader.Find("Standard"));
                        defaultMaterial.color = Color.white;
                        renderer.sharedMaterial = defaultMaterial;
                        fixedCount++;
                        Debug.Log($"Created default material for: {obj.name}");
                    }
                    else if (renderer.sharedMaterial.shader == null)
                    {
                        // Shader가 깨진 경우
                        renderer.sharedMaterial.shader = Shader.Find("Standard");
                        fixedCount++;
                        Debug.Log($"Fixed shader for: {obj.name}");
                    }
                }
            }

            EditorUtility.DisplayDialog("Material Fix",
                $"{fixedCount}개의 Material을 복구했습니다.", "확인");
        }

        [MenuItem("Tools/Realm Commander/Reset Unit Colors")]
        public static void ResetUnitColors()
        {
            var units = Object.FindObjectsByType<RTS.Unit>(FindObjectsSortMode.None);
            foreach (var unit in units)
            {
                var renderer = unit.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    renderer.sharedMaterial.color = unit.IsEnemy ? Color.red : Color.blue;
                }
            }

            var buildings = Object.FindObjectsByType<RTS.Building>(FindObjectsSortMode.None);
            foreach (var building in buildings)
            {
                var renderer = building.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    renderer.sharedMaterial.color = Color.gray;
                }
            }

            EditorUtility.DisplayDialog("Colors Reset",
                "유닛과 건물의 색상을 초기화했습니다.", "확인");
        }
    }
}
