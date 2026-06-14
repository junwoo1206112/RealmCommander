using UnityEngine;
using UnityEditor;

namespace RealmCommander.Editor
{
    public static class VFXPrefabGenerator
    {
        private const string PrefabFolder = "Assets/Prefabs/VFX";

        [MenuItem("Tools/Realm Commander/Generate VFX Prefabs")]
        public static void GenerateAll()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "VFX");
            }

            CreateProjectileVFX();
            CreateImpactVFX();
            CreateAuraVFX();
            CreateSpawnVFX();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VFX] All VFX prefabs generated.");
        }

        private static void CreateProjectileVFX()
        {
            var go = new GameObject("Projectile_VFX");
            go.transform.localScale = Vector3.one * 0.3f;

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(go.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());

            var renderer = sphere.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = Color.yellow;
            renderer.sharedMaterial = mat;

            var light = new GameObject("Light");
            light.transform.SetParent(go.transform);
            light.transform.localPosition = Vector3.zero;
            var pointLight = light.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = Color.yellow;
            pointLight.range = 3f;
            pointLight.intensity = 2f;

            SavePrefab(go, "Projectile_VFX");
        }

        private static void CreateImpactVFX()
        {
            var go = new GameObject("Impact_VFX");

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(go.transform);
            sphere.transform.localScale = Vector3.one * 1.5f;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());

            var renderer = sphere.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            renderer.sharedMaterial = mat;

            var light = new GameObject("Light");
            light.transform.SetParent(go.transform);
            var pointLight = light.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = Color.orange;
            pointLight.range = 5f;
            pointLight.intensity = 3f;

            SavePrefab(go, "Impact_VFX");
        }

        private static void CreateAuraVFX()
        {
            var go = new GameObject("Aura_VFX");

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(go.transform);
            sphere.transform.localPosition = Vector3.up * 1f;
            sphere.transform.localScale = Vector3.one * 2f;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());

            var renderer = sphere.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0.2f, 1f, 0.4f, 0.5f);
            renderer.sharedMaterial = mat;

            var light = new GameObject("Light");
            light.transform.SetParent(go.transform);
            light.transform.localPosition = Vector3.up * 1f;
            var pointLight = light.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = Color.green;
            pointLight.range = 4f;
            pointLight.intensity = 1.5f;

            SavePrefab(go, "Aura_VFX");
        }

        private static void CreateSpawnVFX()
        {
            var go = new GameObject("Spawn_VFX");

            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.SetParent(go.transform);
            cylinder.transform.localPosition = Vector3.zero;
            cylinder.transform.localScale = new Vector3(2f, 0.05f, 2f);
            Object.DestroyImmediate(cylinder.GetComponent<Collider>());

            var renderer = cylinder.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0.3f, 0.8f, 1f, 0.7f);
            renderer.sharedMaterial = mat;

            var light = new GameObject("Light");
            light.transform.SetParent(go.transform);
            var pointLight = light.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = Color.cyan;
            pointLight.range = 4f;
            pointLight.intensity = 2f;

            SavePrefab(go, "Spawn_VFX");
        }

        private static void SavePrefab(GameObject go, string name)
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[VFX] Created: {path}");
        }
    }
}
