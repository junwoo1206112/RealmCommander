using System;
using UnityEngine;
using UnityEngine.UI;
using RealmCommander.Core;

namespace RealmCommander.RTS
{
    public class MinimapController : MonoBehaviour
    {
        [Header("Minimap Settings")]
        [SerializeField] private Camera minimapCamera;
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform playerIndicator;
        [SerializeField] private RectTransform enemyIndicatorPrefab;

        [Header("Map Settings")]
        [SerializeField] private float mapSize = 100f;
        [SerializeField] private Vector2 mapBounds = new Vector2(100f, 100f);

        private void Start()
        {
            if (minimapCamera != null && minimapImage != null)
            {
                RenderTexture rt = new RenderTexture(256, 256, 24);
                minimapCamera.targetTexture = rt;
                minimapImage.texture = rt;
            }
        }

        private void Update()
        {
            UpdateIndicators();
            HandleMinimapClick();
        }

        private void UpdateIndicators()
        {
            if (playerIndicator == null || SelectionManager.Instance == null) return;

            var selectedUnits = SelectionManager.Instance.SelectedUnits;
            if (selectedUnits != null && selectedUnits.Count > 0)
            {
                foreach (var unit in selectedUnits)
                {
                    if (unit != null)
                    {
                        Vector2 minimapPos = WorldToMinimap(unit.transform.position);
                        playerIndicator.anchoredPosition = minimapPos;
                        break;
                    }
                }
            }
        }

        private void HandleMinimapClick()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = Input.mousePosition;

                if (RectTransformUtility.RectangleContainsScreenPoint(viewport, mousePos))
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        viewport, mousePos, null, out Vector2 localPoint))
                    {
                        Vector3 worldPos = MinimapToWorld(localPoint);
                        CommandManager.Instance?.IssueMoveCommand(worldPos);
                    }
                }
            }
        }

        private Vector2 WorldToMinimap(Vector3 worldPos)
        {
            if (viewport == null) return Vector2.zero;
            float x = (worldPos.x / mapBounds.x) * viewport.rect.width;
            float y = (worldPos.z / mapBounds.y) * viewport.rect.height;
            return new Vector2(x, y);
        }

        private Vector3 MinimapToWorld(Vector2 minimapPos)
        {
            if (viewport == null) return Vector3.zero;
            float x = (minimapPos.x / viewport.rect.width) * mapBounds.x;
            float z = (minimapPos.y / viewport.rect.height) * mapBounds.y;
            return new Vector3(x, 0, z);
        }

        public void SetCameraPosition(Vector3 position)
        {
            if (minimapCamera != null)
            {
                minimapCamera.transform.position = new Vector3(position.x, minimapCamera.transform.position.y, position.z);
            }
        }
    }
}
