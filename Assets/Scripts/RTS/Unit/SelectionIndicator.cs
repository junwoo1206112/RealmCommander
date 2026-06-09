using UnityEngine;

namespace RealmCommander.RTS
{
    /// <summary>
    /// 유닛 선택 시 시각적 피드백을 제공하는 컴포넌트
    /// </summary>
    public class SelectionIndicator : MonoBehaviour
    {
        [Header("Indicator Settings")]
        [SerializeField] private Color selectedColor = Color.green;
        [SerializeField] private Color unselectedColor = Color.white;
        [SerializeField] private float indicatorHeight = 0.1f;
        [SerializeField] private float indicatorRadius = 0.8f;

        private Renderer indicatorRenderer;
        private bool isSelected = false;

        private void Awake()
        {
            // 링 모양 메쉬 생성
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

            // 링 메쉬 생성
            Mesh ringMesh = CreateRing(indicatorRadius, indicatorRadius * 0.8f, 32);
            meshFilter.mesh = ringMesh;

            // 머티리얼 생성
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = unselectedColor;
            meshRenderer.material = mat;

            indicatorRenderer = meshRenderer;
            gameObject.SetActive(false);
        }

        private Mesh CreateRing(float outerRadius, float innerRadius, int segments)
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[segments * 2];
            int[] triangles = new int[segments * 6];
            Vector2[] uv = new Vector2[segments * 2];

            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float nextAngle = (i + 1) * angleStep * Mathf.Deg2Rad;

                // 바깥쪽 꼭짓점
                vertices[i * 2] = new Vector3(Mathf.Cos(angle) * outerRadius, Mathf.Sin(angle) * outerRadius, 0);
                uv[i * 2] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);

                // 안쪽 꼭짓점
                vertices[i * 2 + 1] = new Vector3(Mathf.Cos(angle) * innerRadius, Mathf.Sin(angle) * innerRadius, 0);
                uv[i * 2 + 1] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);

                // 삼각형 인덱스
                int nextI = (i + 1) % segments;
                triangles[i * 6] = i * 2;
                triangles[i * 6 + 1] = nextI * 2;
                triangles[i * 6 + 2] = i * 2 + 1;

                triangles[i * 6 + 3] = nextI * 2;
                triangles[i * 6 + 4] = nextI * 2 + 1;
                triangles[i * 6 + 5] = i * 2 + 1;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();

            return mesh;
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            gameObject.SetActive(selected);

            if (indicatorRenderer != null)
            {
                indicatorRenderer.material.color = selected ? selectedColor : unselectedColor;
            }
        }

        public bool IsSelected => isSelected;
    }
}
