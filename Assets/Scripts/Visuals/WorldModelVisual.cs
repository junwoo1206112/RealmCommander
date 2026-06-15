using RealmCommander.RTS;
using UnityEngine;

namespace RealmCommander.Visuals
{
    public class WorldModelVisual : MonoBehaviour
    {
        private const string VisualRootName = "Generated3DVisual";

        [SerializeField] private Vector3 localOffset;
        [SerializeField] private bool hideRootRenderer = true;

        private string currentKey;
        private bool currentEnemy;
        private Transform root;
        private MaterialPropertyBlock colorBlock;
        private static Material sharedMaterial;

        public void ApplyUnit(string unitId, bool isEnemy)
        {
            string key = string.IsNullOrWhiteSpace(unitId) ? "unit_soldier" : unitId;
            if (currentKey == key && currentEnemy == isEnemy && root != null) return;
            currentKey = key;
            currentEnemy = isEnemy;
            Rebuild();
        }

        public void ApplyBuilding(BuildingType buildingType, bool isEnemy)
        {
            ApplyUnit("building_" + buildingType, isEnemy);
        }

        private void Awake()
        {
            if (hideRootRenderer)
            {
                foreach (Renderer renderer in GetComponents<Renderer>())
                    renderer.enabled = false;
            }
        }

        private void Rebuild()
        {
            EnsureMaterial();
            ClearVisual();

            GameObject rootObject = new GameObject(VisualRootName);
            rootObject.transform.SetParent(transform, false);
            rootObject.transform.localPosition = localOffset;
            root = rootObject.transform;
            root.localScale = Vector3.one;

            if (currentKey.StartsWith("building_"))
                BuildStructure();
            else
                BuildCharacter();
        }

        private void BuildCharacter()
        {
            string kind = currentKey.ToLowerInvariant();
            Color primary = currentEnemy ? new Color(0.85f, 0.12f, 0.08f) : new Color(0.1f, 0.32f, 0.85f);
            Color metal = new Color(0.68f, 0.72f, 0.75f);
            Color cloth = currentEnemy ? new Color(0.55f, 0.05f, 0.04f) : new Color(0.05f, 0.18f, 0.55f);
            Color accent = new Color(1f, 0.78f, 0.18f);

            AddPrimitive("Body", PrimitiveType.Capsule, new Vector3(0f, 0.55f, 0f), new Vector3(0.42f, 0.72f, 0.42f), primary);
            AddPrimitive("Head", PrimitiveType.Sphere, new Vector3(0f, 1.16f, 0f), new Vector3(0.34f, 0.28f, 0.34f), new Color(0.95f, 0.72f, 0.48f));
            AddPrimitive("Team Banner", PrimitiveType.Cube, new Vector3(0f, 0.95f, -0.24f), new Vector3(0.55f, 0.12f, 0.08f), cloth);

            if (kind.Contains("worker"))
            {
                AddPrimitive("Pack", PrimitiveType.Cube, new Vector3(0f, 0.72f, -0.3f), new Vector3(0.34f, 0.34f, 0.18f), new Color(0.46f, 0.27f, 0.1f));
                AddPrimitive("Pick Handle", PrimitiveType.Cylinder, new Vector3(0.42f, 0.72f, 0f), new Vector3(0.04f, 0.74f, 0.04f), new Color(0.45f, 0.25f, 0.1f), Quaternion.Euler(0f, 0f, -35f));
                AddPrimitive("Pick Head", PrimitiveType.Cube, new Vector3(0.58f, 1.03f, 0f), new Vector3(0.36f, 0.06f, 0.06f), metal, Quaternion.Euler(0f, 0f, -35f));
            }
            else if (kind.Contains("archer"))
            {
                AddPrimitive("Bow", PrimitiveType.Cylinder, new Vector3(0.42f, 0.62f, 0f), new Vector3(0.045f, 0.78f, 0.045f), new Color(0.45f, 0.24f, 0.08f), Quaternion.Euler(0f, 0f, 25f));
                AddPrimitive("Quiver", PrimitiveType.Cylinder, new Vector3(-0.28f, 0.78f, -0.24f), new Vector3(0.12f, 0.42f, 0.12f), new Color(0.25f, 0.15f, 0.08f), Quaternion.Euler(20f, 0f, 0f));
            }
            else if (kind.Contains("mage"))
            {
                AddPrimitive("Staff", PrimitiveType.Cylinder, new Vector3(0.42f, 0.78f, 0f), new Vector3(0.055f, 0.9f, 0.055f), new Color(0.45f, 0.25f, 0.1f));
                AddPrimitive("Crystal", PrimitiveType.Sphere, new Vector3(0.42f, 1.32f, 0f), new Vector3(0.22f, 0.22f, 0.22f), new Color(0.2f, 0.75f, 1f));
                AddPrimitive("Robe", PrimitiveType.Cylinder, new Vector3(0f, 0.36f, 0f), new Vector3(0.5f, 0.28f, 0.5f), cloth);
            }
            else if (kind.Contains("tank"))
            {
                AddPrimitive("Heavy Shield", PrimitiveType.Cube, new Vector3(0.48f, 0.62f, 0.05f), new Vector3(0.16f, 0.72f, 0.52f), metal);
                AddPrimitive("Shoulders", PrimitiveType.Cube, new Vector3(0f, 0.92f, 0f), new Vector3(0.72f, 0.2f, 0.45f), metal);
            }
            else
            {
                AddPrimitive("Sword", PrimitiveType.Cube, new Vector3(0.42f, 0.72f, 0f), new Vector3(0.07f, 0.75f, 0.07f), metal, Quaternion.Euler(0f, 0f, -25f));
                AddPrimitive("Shield", PrimitiveType.Cylinder, new Vector3(-0.38f, 0.62f, 0.05f), new Vector3(0.32f, 0.08f, 0.32f), primary, Quaternion.Euler(90f, 0f, 0f));
            }

            AddPrimitive("Base Ring", PrimitiveType.Cylinder, new Vector3(0f, 0.035f, 0f), new Vector3(0.58f, 0.025f, 0.58f), accent);
        }

        private void BuildStructure()
        {
            string kind = currentKey.ToLowerInvariant();
            Color primary = currentEnemy ? new Color(0.55f, 0.12f, 0.1f) : new Color(0.12f, 0.28f, 0.62f);
            Color stone = new Color(0.42f, 0.45f, 0.48f);
            Color roof = currentEnemy ? new Color(0.58f, 0.08f, 0.06f) : new Color(0.08f, 0.35f, 0.78f);
            Color gold = new Color(1f, 0.72f, 0.18f);

            float scale = kind.Contains("base") ? 1.2f : 1f;
            AddPrimitive("Foundation", PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(1.55f * scale, 0.5f, 1.55f * scale), stone);
            AddPrimitive("Upper Keep", PrimitiveType.Cube, new Vector3(0f, 0.74f, 0f), new Vector3(1.05f * scale, 0.5f, 1.05f * scale), primary);
            AddPrimitive("Flat Roof", PrimitiveType.Cube, new Vector3(0f, 1.04f, 0f), new Vector3(1.22f * scale, 0.12f, 1.22f * scale), roof);
            AddPrimitive("Front Banner", PrimitiveType.Cube, new Vector3(0f, 0.6f, -0.82f * scale), new Vector3(0.56f, 0.32f, 0.06f), primary);
            AddPrimitive("Door", PrimitiveType.Cube, new Vector3(0f, 0.22f, -0.79f * scale), new Vector3(0.34f, 0.34f, 0.07f), new Color(0.16f, 0.1f, 0.06f));

            AddPrimitive("Corner NW", PrimitiveType.Cube, new Vector3(-0.7f * scale, 0.72f, 0.7f * scale), new Vector3(0.24f, 0.58f, 0.24f), stone);
            AddPrimitive("Corner NE", PrimitiveType.Cube, new Vector3(0.7f * scale, 0.72f, 0.7f * scale), new Vector3(0.24f, 0.58f, 0.24f), stone);
            AddPrimitive("Corner SW", PrimitiveType.Cube, new Vector3(-0.7f * scale, 0.72f, -0.7f * scale), new Vector3(0.24f, 0.58f, 0.24f), stone);
            AddPrimitive("Corner SE", PrimitiveType.Cube, new Vector3(0.7f * scale, 0.72f, -0.7f * scale), new Vector3(0.24f, 0.58f, 0.24f), stone);

            if (kind.Contains("defensetower"))
            {
                AddPrimitive("Tower Shaft", PrimitiveType.Cube, new Vector3(0f, 1.16f, 0f), new Vector3(0.58f, 0.74f, 0.58f), stone);
                AddPrimitive("Beacon", PrimitiveType.Sphere, new Vector3(0f, 1.6f, 0f), new Vector3(0.24f, 0.24f, 0.24f), gold);
            }
            else if (kind.Contains("magictower"))
            {
                AddPrimitive("Crystal", PrimitiveType.Sphere, new Vector3(0f, 1.42f, 0f), new Vector3(0.3f, 0.42f, 0.3f), new Color(0.2f, 0.78f, 1f));
            }
            else if (kind.Contains("resourcegenerator"))
            {
                AddPrimitive("Gear", PrimitiveType.Cylinder, new Vector3(-0.42f, 0.72f, -0.42f), new Vector3(0.34f, 0.12f, 0.34f), gold, Quaternion.Euler(90f, 0f, 0f));
            }
            else if (kind.Contains("barracks"))
            {
                AddPrimitive("Training Target", PrimitiveType.Cylinder, new Vector3(0.62f, 0.55f, -0.56f), new Vector3(0.24f, 0.05f, 0.24f), gold, Quaternion.Euler(90f, 0f, 0f));
            }
        }

        private GameObject AddPrimitive(string partName, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Color color)
        {
            return AddPrimitive(partName, primitive, localPosition, localScale, color, Quaternion.identity);
        }

        private GameObject AddPrimitive(string partName, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Color color, Quaternion localRotation)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = partName;
            part.transform.SetParent(root, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = sharedMaterial;
            colorBlock ??= new MaterialPropertyBlock();
            colorBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(colorBlock);
            return part;
        }

        private void ClearVisual()
        {
            Transform existing = transform.Find(VisualRootName);
            if (existing != null)
                Destroy(existing.gameObject);
            root = null;
        }

        private static void EnsureMaterial()
        {
            if (sharedMaterial != null) return;
            sharedMaterial = Core.StaticResources.GetOrCreateMaterial("Standard", Color.white);
        }
    }
}
