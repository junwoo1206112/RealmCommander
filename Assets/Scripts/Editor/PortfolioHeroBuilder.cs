using Mirror;
using RealmCommander.RPG;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace RealmCommander.Editor
{
    public static class PortfolioHeroBuilder
    {
        private const string HeroPrefabPath = "Assets/Resources/CommanderHero.prefab";

        [MenuItem("Tools/Realm Commander/Apply Minimal Network Hero")]
        public static void ApplyMinimalNetworkHero()
        {
            GameObject heroPrefab = CreateOrReplaceHeroPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log($"[PortfolioHeroBuilder] PASS prefab={HeroPrefabPath} resourcesRegistration=true");
        }

        private static GameObject CreateOrReplaceHeroPrefab()
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "CommanderHero";
            root.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);

            Renderer renderer = root.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial(new Color(1f, 0.75f, 0.1f));
            renderer.sharedMaterial.color = new Color(1f, 0.75f, 0.1f);

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.speed = 6f;
            agent.acceleration = 24f;
            agent.angularSpeed = 420f;
            agent.radius = 0.35f;
            agent.height = 1.2f;
            agent.baseOffset = 0.6f;

            root.AddComponent<NetworkIdentity>();
            NetworkTransformReliable networkTransform = root.AddComponent<NetworkTransformReliable>();
            networkTransform.target = root.transform;

            Hero hero = root.AddComponent<Hero>();

            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.name = "SelectionIndicator";
            indicator.transform.SetParent(root.transform, false);
            indicator.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            indicator.transform.localScale = new Vector3(1.4f, 0.025f, 1.4f);
            Object.DestroyImmediate(indicator.GetComponent<Collider>());
            indicator.GetComponent<Renderer>().sharedMaterial = CreateMaterial(Color.green);
            indicator.SetActive(false);

            SerializedObject heroObject = new SerializedObject(hero);
            heroObject.FindProperty("selectionIndicator").objectReferenceValue = indicator;
            heroObject.FindProperty("heroRenderer").objectReferenceValue = renderer;
            heroObject.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, HeroPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
                throw new MissingReferenceException("No supported runtime shader was found.");
            return new Material(shader) { color = color };
        }
    }
}
