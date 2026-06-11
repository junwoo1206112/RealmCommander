using System;
using UnityEngine;
using Mirror;
using RealmCommander.Core;
using RealmCommander.RTS;

namespace RealmCommander.RTS
{
    public class BuildingPlacer : MonoBehaviour
    {
        [Header("Building Settings")]
        [SerializeField] private BuildingData[] availableBuildings;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask buildingLayer;

        [Header("Visual")]
        [SerializeField] private GameObject placementIndicator;
        [SerializeField] private Color validColor = Color.green;
        [SerializeField] private Color invalidColor = Color.red;

        private bool isPlacing;
        private BuildingData currentBuilding;
        private Vector3 placementPosition;
        private bool isValidPosition;

        public event Action<BuildingData> OnBuildingPlaced;

        private void Update()
        {
            if (isPlacing)
            {
                HandlePlacement();
            }
        }

        public void StartPlacement(BuildingData buildingData)
        {
            if (buildingData == null) return;

            if (NetworkClient.active && !NetworkServer.active)
            {
                Debug.LogWarning("멀티플레이 Client 건설은 아직 지원되지 않습니다. Host에서 건설해 주세요.");
                return;
            }

            if (ResourceManager.Instance == null) return;

            if (!ResourceManager.Instance.CanAfford(buildingData.goldCost, buildingData.manaCost))
            {
                Debug.Log("자원이 부족합니다!");
                return;
            }

            currentBuilding = buildingData;
            isPlacing = true;

            if (placementIndicator != null)
            {
                placementIndicator.SetActive(true);
            }

            Debug.Log($"{buildingData.buildingName} 배치 모드 시작 (취소: 우클릭)");
        }

        public void CancelPlacement()
        {
            isPlacing = false;
            currentBuilding = null;

            if (placementIndicator != null)
            {
                placementIndicator.SetActive(false);
            }

            Debug.Log("배치 취소");
        }

        private void HandlePlacement()
        {
            Camera camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (camera == null) return;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
            {
                placementPosition = hit.point;
                placementPosition.y = 0;

                if (placementIndicator != null)
                {
                    placementIndicator.transform.position = placementPosition;
                }

                isValidPosition = CheckValidPosition(placementPosition);
                UpdateIndicatorColor();

                if (Input.GetMouseButtonDown(0) && isValidPosition)
                {
                    PlaceBuilding();
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
            }
        }

        private bool CheckValidPosition(Vector3 position)
        {
            Collider[] colliders = Physics.OverlapSphere(position, currentBuilding.buildingRadius, buildingLayer);
            return colliders.Length == 0;
        }

        private void UpdateIndicatorColor()
        {
            if (placementIndicator != null)
            {
                Renderer renderer = placementIndicator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = isValidPosition ? validColor : invalidColor;
                }
            }
        }

        private void PlaceBuilding()
        {
            if (!NetworkServer.active) return;

            if (!CanAffordBuilding()) return;
            ResourceManager.Instance.SpendGold(currentBuilding.goldCost);
            ResourceManager.Instance.SpendMana(currentBuilding.manaCost);

            GameObject buildingObj = Instantiate(currentBuilding.buildingPrefab, placementPosition, Quaternion.identity);
            buildingObj.name = currentBuilding.buildingName;

            NetworkServer.Spawn(buildingObj);

            Building building = buildingObj.GetComponent<Building>();
            if (building != null)
            {
                building.StartConstruction();
            }

            isPlacing = false;
            var placedBuilding = currentBuilding;
            currentBuilding = null;

            if (placementIndicator != null)
            {
                placementIndicator.SetActive(false);
            }

            OnBuildingPlaced?.Invoke(placedBuilding);
            Debug.Log($"{placedBuilding.buildingName} 건설 시작!");
        }

        private bool CanAffordBuilding()
        {
            if (ResourceManager.Instance == null) return false;
            return ResourceManager.Instance.CanAfford(currentBuilding.goldCost, currentBuilding.manaCost);
        }

        private void OnDrawGizmosSelected()
        {
            if (!isPlacing || currentBuilding == null) return;

            Gizmos.color = isValidPosition ? Color.green : Color.red;
            Gizmos.DrawWireSphere(placementPosition, currentBuilding.buildingRadius);
        }
    }

    [System.Serializable]
    public class BuildingData
    {
        public string buildingName;
        public GameObject buildingPrefab;
        public BuildingType buildingType;
        public float goldCost = 100f;
        public float manaCost = 0f;
        public float buildingRadius = 2f;
        public Sprite icon;
    }
}
