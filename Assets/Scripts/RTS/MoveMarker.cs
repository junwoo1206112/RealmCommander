using UnityEngine;

namespace RealmCommander.RTS
{
    public class MoveMarker : MonoBehaviour
    {
        [SerializeField] private float duration = 0.8f;
        [SerializeField] private Color moveColor = new Color(0.2f, 0.9f, 0.2f, 0.9f);
        [SerializeField] private Color attackColor = new Color(0.9f, 0.2f, 0.2f, 0.9f);

        private float elapsed;
        private LineRenderer ring;
        private Color baseColor;

        public static MoveMarker Spawn(Vector3 position, bool isAttack = false)
        {
            GameObject marker = new GameObject("MoveMarker");
            marker.name = "MoveMarker";
            marker.transform.position = position + Vector3.up * 0.08f;

            var moveMarker = marker.AddComponent<MoveMarker>();
            moveMarker.baseColor = isAttack ? moveMarker.attackColor : moveMarker.moveColor;

            Destroy(marker, moveMarker.duration + 0.1f);
            return moveMarker;
        }

        private void Awake()
        {
            ring = gameObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 40;
            ring.widthMultiplier = 0.09f;
            ring.numCornerVertices = 2;
            ring.numCapVertices = 2;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            ring.material = new Material(shader);

            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / ring.positionCount;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            }
        }

        private void Start()
        {
            ring.startColor = baseColor;
            ring.endColor = baseColor;
            transform.localScale = Vector3.one * 0.65f;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            if (ring != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, t);
                Color c = baseColor;
                c.a = alpha;
                ring.startColor = c;
                ring.endColor = c;
                transform.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.15f, t);
            }
        }

        private void OnDestroy()
        {
            if (ring != null && ring.material != null)
                Destroy(ring.material);
        }
    }
}
