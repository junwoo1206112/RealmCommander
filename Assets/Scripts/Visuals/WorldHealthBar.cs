using RealmCommander.RTS;
using RealmCommander.Core;
using UnityEngine;

namespace RealmCommander.Visuals
{
    public class WorldHealthBar : MonoBehaviour
    {
        [SerializeField] private Vector3 localPosition = new Vector3(0f, 1.65f, 0f);
        [SerializeField] private Vector2 size = new Vector2(0.9f, 0.08f);
        [SerializeField] private Color friendlyColor = new Color(0.1f, 0.75f, 1f, 1f);
        [SerializeField] private Color enemyColor = new Color(1f, 0.2f, 0.16f, 1f);
        [SerializeField] private Color neutralColor = new Color(0.2f, 1f, 0.35f, 1f);
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.75f);

        private Transform pivot;
        private SpriteRenderer background;
        private SpriteRenderer fill;
        private Unit unit;
        private Building building;
        private static Sprite pixelSprite;

        private void Awake()
        {
            unit = GetComponent<Unit>();
            building = GetComponent<Building>();
            EnsureRenderers();
            UpdateVisual();
        }

        private void LateUpdate()
        {
            UpdateVisual();

            Camera camera = Camera.main;
            if (camera == null || pivot == null) return;
            Vector3 direction = pivot.position - camera.transform.position;
            if (direction.sqrMagnitude > 0.0001f)
                pivot.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        public void SetLayout(Vector3 position, Vector2 newSize)
        {
            localPosition = position;
            size = newSize;
            if (pivot != null) pivot.localPosition = localPosition;
            ApplySize(1f);
        }

        private void EnsureRenderers()
        {
            if (pixelSprite == null)
            {
                var texture = StaticResources.CreatePixelTexture(Color.white);
                pixelSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }

            if (pivot != null) return;

            GameObject pivotObject = new GameObject("WorldHealthBar");
            pivotObject.transform.SetParent(transform, false);
            pivotObject.transform.localPosition = localPosition;
            pivot = pivotObject.transform;

            background = CreateSegment("Background", backgroundColor, 19);
            fill = CreateSegment("Fill", neutralColor, 20);
            ApplySize(1f);
        }

        private SpriteRenderer CreateSegment(string segmentName, Color color, int sortingOrder)
        {
            GameObject segment = new GameObject(segmentName);
            segment.transform.SetParent(pivot, false);
            var renderer = segment.AddComponent<SpriteRenderer>();
            renderer.sprite = pixelSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void UpdateVisual()
        {
            EnsureRenderers();

            float percent = 1f;
            bool isEnemy = false;
            bool hasTarget = true;

            if (unit != null)
            {
                percent = unit.HealthPercent;
                isEnemy = unit.IsEnemy;
                hasTarget = unit.IsAlive;
            }
            else if (building != null)
            {
                percent = building.HealthPercent;
                isEnemy = building.TeamId == 1;
                hasTarget = building.IsAlive;
            }

            bool visible = hasTarget && percent < 0.999f;
            background.enabled = visible;
            fill.enabled = visible;
            if (!visible) return;

            fill.color = isEnemy ? enemyColor : unit != null ? friendlyColor : neutralColor;
            ApplySize(Mathf.Clamp01(percent));
        }

        private void ApplySize(float percent)
        {
            if (background == null || fill == null) return;

            background.transform.localScale = new Vector3(size.x, size.y, 1f);
            fill.transform.localScale = new Vector3(size.x * percent, size.y * 0.72f, 1f);
            fill.transform.localPosition = new Vector3(-size.x * (1f - percent) * 0.5f, 0f, -0.01f);
        }
    }
}
