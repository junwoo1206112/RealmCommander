using UnityEngine;

namespace RealmCommander.RTS
{
    public class BattlefieldPolisher : MonoBehaviour
    {
        private void Start()
        {
            ApplyGroundScale();
            CreateLaneMarkers();
        }

        private static void ApplyGroundScale()
        {
            GameObject ground = GameObject.Find("Ground");
            Renderer renderer = ground != null ? ground.GetComponent<Renderer>() : null;
            if (renderer == null || renderer.sharedMaterial == null) return;

            renderer.sharedMaterial.mainTextureScale = new Vector2(4f, 4f);
        }

        private static void CreateLaneMarkers()
        {
            DestroyLegacyMarker("Blue Base Zone");
            DestroyLegacyMarker("Red Base Zone");
            DestroyLegacyMarker("Central Fight Zone");

            CreateZoneBorder("Blue Base Border", new Vector3(-20f, 0.045f, 0f), new Vector2(5f, 5f), new Color(0.08f, 0.35f, 0.95f));
            CreateZoneBorder("Red Base Border", new Vector3(20f, 0.045f, 0f), new Vector2(5f, 5f), new Color(0.9f, 0.12f, 0.08f));
            CreateZoneBorder("Center Border", new Vector3(0f, 0.05f, 0f), new Vector2(4f, 4f), new Color(0.95f, 0.75f, 0.12f));

            CreateRocks();
            CreateTrees();
            CreateDecorations();
        }

        private static void CreateRocks()
        {
            Vector3[] rockPositions = new Vector3[]
            {
                new Vector3(-14f, 0.25f, -4f),
                new Vector3(-12f, 0.3f, 4f),
                new Vector3(14f, 0.25f, -4f),
                new Vector3(12f, 0.28f, 4f),
                new Vector3(-8f, 0.2f, -6f),
                new Vector3(8f, 0.22f, 6f),
                new Vector3(-10f, 0.3f, 0f),
                new Vector3(10f, 0.3f, 0f),
                new Vector3(0f, 0.18f, -5f),
                new Vector3(0f, 0.2f, 5f),
            };

            foreach (Vector3 pos in rockPositions)
            {
                CreateRock(pos, Random.Range(0.5f, 0.9f));
            }
        }

        private static void CreateTrees()
        {
            Vector3[] treePositions = new Vector3[]
            {
                new Vector3(-23f, 0f, -5f),
                new Vector3(-23f, 0f, 5f),
                new Vector3(23f, 0f, -5f),
                new Vector3(23f, 0f, 5f),
                new Vector3(-18f, 0f, -12f),
                new Vector3(18f, 0f, 12f),
                new Vector3(-15f, 0f, 10f),
                new Vector3(15f, 0f, -10f),
                new Vector3(-24f, 0f, 0f),
                new Vector3(24f, 0f, 0f),
                new Vector3(0f, 0f, -12f),
                new Vector3(0f, 0f, 12f),
            };

            foreach (Vector3 pos in treePositions)
            {
                CreateTree(pos);
            }
        }

        private static void CreateDecorations()
        {
            CreateSmallRock(new Vector3(-6f, 0.1f, -3f), 0.3f);
            CreateSmallRock(new Vector3(6f, 0.1f, 3f), 0.25f);
            CreateSmallRock(new Vector3(-3f, 0.08f, 5f), 0.2f);
            CreateSmallRock(new Vector3(3f, 0.09f, -5f), 0.22f);
            CreateSmallRock(new Vector3(-9f, 0.12f, 1.5f), 0.28f);
            CreateSmallRock(new Vector3(9f, 0.11f, -1.5f), 0.26f);
        }

        private static void DestroyLegacyMarker(string name)
        {
            GameObject marker = GameObject.Find(name);
            if (marker != null) Destroy(marker);
        }

        private static void CreateZoneBorder(string name, Vector3 center, Vector2 size, Color color)
        {
            if (GameObject.Find(name) != null) return;

            GameObject root = new GameObject(name);
            CreateBorderStrip(root.transform, "North", center + new Vector3(0f, 0f, size.y * 0.5f), new Vector3(size.x, 0.035f, 0.08f), color);
            CreateBorderStrip(root.transform, "South", center + new Vector3(0f, 0f, -size.y * 0.5f), new Vector3(size.x, 0.035f, 0.08f), color);
            CreateBorderStrip(root.transform, "East", center + new Vector3(size.x * 0.5f, 0f, 0f), new Vector3(0.08f, 0.035f, size.y), color);
            CreateBorderStrip(root.transform, "West", center + new Vector3(-size.x * 0.5f, 0f, 0f), new Vector3(0.08f, 0.035f, size.y), color);
        }

        private static void CreateBorderStrip(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = name;
            strip.transform.SetParent(parent, true);
            strip.transform.position = position;
            strip.transform.localScale = scale;
            Collider collider = strip.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Renderer renderer = strip.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial(color);
        }

        private static void CreateRock(Vector3 position, float size)
        {
            string name = $"Battlefield Rock {position.x:F1},{position.z:F1}";
            if (GameObject.Find(name) != null) return;
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = name;
            rock.transform.position = position;
            rock.transform.localScale = new Vector3(size, size * 0.6f, size);
            rock.transform.rotation = Quaternion.Euler(0f, position.z * 23f, 0f);
            Collider collider = rock.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Renderer renderer = rock.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial(new Color(0.33f, 0.36f, 0.34f));
        }

        private static void CreateSmallRock(Vector3 position, float size)
        {
            string name = $"Small Rock {position.x:F1},{position.z:F1}";
            if (GameObject.Find(name) != null) return;
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = name;
            rock.transform.position = position;
            rock.transform.localScale = new Vector3(size, size * 0.5f, size);
            Collider collider = rock.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Renderer renderer = rock.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial(new Color(0.38f, 0.4f, 0.37f));
        }

        private static void CreateTree(Vector3 position)
        {
            string name = $"Tree {position.x:F1},{position.z:F1}";
            if (GameObject.Find(name) != null) return;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = name;
            trunk.transform.position = position + Vector3.up * 0.8f;
            trunk.transform.localScale = new Vector3(0.2f, 0.8f, 0.2f);
            Renderer trunkRenderer = trunk.GetComponent<Renderer>();
            trunkRenderer.sharedMaterial = CreateMaterial(new Color(0.4f, 0.25f, 0.1f));
            Collider trunkCollider = trunk.GetComponent<Collider>();
            if (trunkCollider != null) Destroy(trunkCollider);

            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.name = "Leaves";
            leaves.transform.SetParent(trunk.transform, false);
            leaves.transform.localPosition = new Vector3(0f, 1f, 0f);
            leaves.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            Renderer leavesRenderer = leaves.GetComponent<Renderer>();
            leavesRenderer.sharedMaterial = CreateMaterial(new Color(0.15f, 0.5f, 0.15f));
            Collider leavesCollider = leaves.GetComponent<Collider>();
            if (leavesCollider != null) Destroy(leavesCollider);
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            return new Material(shader) { color = color };
        }
    }
}
