using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

        public float MapSize => mapSize;
        private RenderTexture minimapTexture;

        private void Start()
        {
            if (minimapCamera != null && minimapImage != null)
            {
                if (minimapTexture != null)
                {
                    minimapCamera.targetTexture = null;
                    minimapTexture.Release();
                    Destroy(minimapTexture);
                }
                minimapTexture = new RenderTexture(256, 256, 24);
                minimapTexture.Create();
                minimapCamera.targetTexture = minimapTexture;
                minimapImage.texture = minimapTexture;
            }
        }

        private void OnDestroy()
        {
            if (minimapCamera != null && minimapCamera.targetTexture == minimapTexture)
                minimapCamera.targetTexture = null;
            if (minimapTexture != null)
            {
                minimapTexture.Release();
                Destroy(minimapTexture);
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
                        playerIndicator.gameObject.SetActive(true);
                        break;
                    }
                }
            }
            else
            {
                playerIndicator.gameObject.SetActive(false);
            }
        }

        private void HandleMinimapClick()
        {
            if (MobileRTSInput.TouchControlsActive && Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Ended &&
                    (EventSystem.current == null || EventSystem.current.IsPointerOverGameObject(touch.fingerId)))
                {
                    TryIssueMinimapCommand(touch.position);
                }
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryIssueMinimapCommand(Input.mousePosition);
            }
        }

        private void TryIssueMinimapCommand(Vector2 screenPosition)
        {
            if (viewport == null || !RectTransformUtility.RectangleContainsScreenPoint(viewport, screenPosition)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPosition, null, out Vector2 localPoint)) return;

            Vector3 worldPos = MinimapToWorld(localPoint);

            GameObject nearestEnemy = FindNearestEnemyAt(worldPos, 3f);
            if (nearestEnemy != null)
            {
                CommandManager.Instance?.IssueAttackCommand(nearestEnemy);
                return;
            }

            CommandManager.Instance?.IssueMoveCommand(worldPos);
        }

        private static GameObject FindNearestEnemyAt(Vector3 position, float radius)
        {
            var registry = Core.EntityRegistry.Instance;
            if (registry == null) return null;

            GameObject nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var unit in registry.AllUnits)
            {
                if (unit == null || !unit.IsAlive) continue;
                if (!unit.IsEnemy) continue;
                float dist = Vector3.Distance(position, unit.transform.position);
                if (dist < radius && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = unit.gameObject;
                }
            }

            if (nearest != null) return nearest;

            foreach (var building in registry.AllBuildings)
            {
                if (building == null || !building.IsAlive) continue;
                if (building.TeamId != 1) continue;
                float dist = Vector3.Distance(position, building.transform.position);
                if (dist < radius && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = building.gameObject;
                }
            }

            return nearest;
        }

        private Vector2 WorldToMinimap(Vector3 worldPos)
        {
            if (viewport == null) return Vector2.zero;
            float x = Mathf.InverseLerp(-mapBounds.x * 0.5f, mapBounds.x * 0.5f, worldPos.x) * viewport.rect.width - viewport.rect.width * 0.5f;
            float y = Mathf.InverseLerp(-mapBounds.y * 0.5f, mapBounds.y * 0.5f, worldPos.z) * viewport.rect.height - viewport.rect.height * 0.5f;
            return new Vector2(x, y);
        }

        private Vector3 MinimapToWorld(Vector2 minimapPos)
        {
            if (viewport == null) return Vector3.zero;
            float normalizedX = minimapPos.x / viewport.rect.width + 0.5f;
            float normalizedY = minimapPos.y / viewport.rect.height + 0.5f;
            float x = Mathf.Lerp(-mapBounds.x * 0.5f, mapBounds.x * 0.5f, normalizedX);
            float z = Mathf.Lerp(-mapBounds.y * 0.5f, mapBounds.y * 0.5f, normalizedY);
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
