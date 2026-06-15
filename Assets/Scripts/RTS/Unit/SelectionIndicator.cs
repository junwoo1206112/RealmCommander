using UnityEngine;
using RealmCommander.Core;

namespace RealmCommander.RTS
{
    public class SelectionIndicator : MonoBehaviour
    {
        [Header("Indicator Settings")]
        [SerializeField] private Color selectedColor = Color.green;
        [SerializeField] private Color unselectedColor = Color.white;
        [SerializeField] private float indicatorHeight = 0.1f;
        [SerializeField] private float indicatorRadius = 0.8f;

        private Renderer indicatorRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Mesh ringMesh;
        private static Material sharedRingMaterial;
        private bool isSelected = false;

        private void Awake()
        {
            CreateRingMesh();
        }

        private void CreateRingMesh()
        {
            GameObject ring = new GameObject("SelectionRing");
            ring.transform.SetParent(transform);
            ring.transform.localPosition = new Vector3(0, indicatorHeight, 0);
            ring.transform.localRotation = Quaternion.Euler(90, 0, 0);

            MeshFilter meshFilter = ring.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = ring.AddComponent<MeshRenderer>();

            ringMesh = StaticResources.CreateRingMesh(indicatorRadius, indicatorRadius * 0.8f, 32);
            meshFilter.sharedMesh = ringMesh;

            if (sharedRingMaterial == null)
                sharedRingMaterial = StaticResources.GetOrCreateMaterial("Unlit/Color", Color.white);

            meshRenderer.sharedMaterial = sharedRingMaterial;
            propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetColor("_Color", unselectedColor);
            meshRenderer.SetPropertyBlock(propertyBlock);

            indicatorRenderer = meshRenderer;
            gameObject.SetActive(false);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            gameObject.SetActive(selected);

            if (indicatorRenderer != null)
            {
                propertyBlock ??= new MaterialPropertyBlock();
                propertyBlock.SetColor("_Color", selected ? selectedColor : unselectedColor);
                indicatorRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        public bool IsSelected => isSelected;
    }
}
