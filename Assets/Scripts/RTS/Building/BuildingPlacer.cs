using System;
using UnityEngine;
using Mirror;
using RealmCommander.Core;

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

        [Header("Placement Rules")]
        [SerializeField] private bool snapToGrid = true;
        [SerializeField, Min(0.25f)] private float gridSize = 1f;
        [SerializeField, Min(0f)] private float spacingPadding = 0.35f;

        private bool isPlacing;
        private BuildingData currentBuilding;
        private Vector3 placementPosition;
        private bool isValidPosition;
        private MaterialPropertyBlock indicatorColorBlock;

        public event Action<BuildingData> OnBuildingPlaced;

        private void Update()
        {
            if (isPlacing)
                HandlePlacement();
        }

        public void StartPlacement(BuildingData buildingData)
        {
            if (buildingData == null || buildingData.buildingPrefab == null) return;

            if (NetworkClient.active && !NetworkServer.active)
            {
                Debug.LogWarning("[BuildingPlacer] Client-side building placement is not available yet. Please place buildings from the host.");
                return;
            }

            ResourceManager resources = ResourceManager.Instance;
            if (resources == null) return;

            int teamId = Network.NetworkPlayer.Local != null ? Network.NetworkPlayer.Local.TeamId : 0;
            if (!resources.CanAfford(teamId, buildingData.goldCost, buildingData.manaCost))
            {
                Debug.Log("[BuildingPlacer] Not enough resources.");
                return;
            }

            currentBuilding = buildingData;
            isPlacing = true;

            if (placementIndicator != null)
                placementIndicator.SetActive(true);

            Debug.Log($"[BuildingPlacer] Started placement for {buildingData.buildingName}. Right-click to cancel.");
        }

        public void CancelPlacement()
        {
            isPlacing = false;
            currentBuilding = null;

            if (placementIndicator != null)
                placementIndicator.SetActive(false);

            Debug.Log("[BuildingPlacer] Placement canceled.");
        }

        private void HandlePlacement()
        {
            Camera camera = RealmCommander.Network.NetworkUtils.GetMainCamera();
            if (camera == null) return;

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                placementPosition = SnapPosition(hit.point);
                placementPosition.y = 0f;

                if (placementIndicator != null)
                    placementIndicator.transform.position = placementPosition;

                isValidPosition = CheckValidPosition(placementPosition);
                UpdateIndicatorColor();

                if (Input.GetMouseButtonDown(0) && isValidPosition)
                    PlaceBuilding();
            }

            if (Input.GetMouseButtonDown(1))
                CancelPlacement();
        }

        private bool CheckValidPosition(Vector3 position)
        {
            if (currentBuilding == null || currentBuilding.buildingPrefab == null) return false;

            float radius = Mathf.Max(0.25f, currentBuilding.buildingRadius + spacingPadding);
            Collider[] colliders = Physics.OverlapSphere(position, radius, buildingLayer);
            return colliders.Length == 0;
        }

        private Vector3 SnapPosition(Vector3 rawPosition)
        {
            if (!snapToGrid || gridSize <= 0f) return rawPosition;

            rawPosition.x = Mathf.Round(rawPosition.x / gridSize) * gridSize;
            rawPosition.z = Mathf.Round(rawPosition.z / gridSize) * gridSize;
            return rawPosition;
        }

        private void UpdateIndicatorColor()
        {
            if (placementIndicator == null) return;

            Renderer renderer = placementIndicator.GetComponent<Renderer>();
            if (renderer == null) return;

            indicatorColorBlock ??= new MaterialPropertyBlock();
            indicatorColorBlock.SetColor("_Color", isValidPosition ? validColor : invalidColor);
            renderer.SetPropertyBlock(indicatorColorBlock);
        }

        private void PlaceBuilding()
        {
            if (!NetworkServer.active || currentBuilding == null) return;

            ResourceManager resources = ResourceManager.Instance;
            if (resources == null) return;

            int teamId = Network.NetworkPlayer.Local != null ? Network.NetworkPlayer.Local.TeamId : 0;
            if (!resources.TrySpend(teamId, currentBuilding.goldCost, currentBuilding.manaCost))
            {
                Debug.Log("[BuildingPlacer] Not enough resources.");
                return;
            }

            GameObject buildingObj = Instantiate(currentBuilding.buildingPrefab, placementPosition, Quaternion.identity);
            buildingObj.name = currentBuilding.buildingName;

            Building building = buildingObj.GetComponent<Building>();
            if (building != null)
            {
                building.ConfigureTeam(teamId);
                building.StartConstruction();
            }

            NetworkServer.Spawn(buildingObj);

            isPlacing = false;
            BuildingData placedBuilding = currentBuilding;
            currentBuilding = null;

            if (placementIndicator != null)
                placementIndicator.SetActive(false);

            OnBuildingPlaced?.Invoke(placedBuilding);
            Debug.Log($"[BuildingPlacer] Started construction for {placedBuilding.buildingName} at {placementPosition}.");
        }

        private void OnDrawGizmosSelected()
        {
            if (!isPlacing || currentBuilding == null) return;

            Gizmos.color = isValidPosition ? Color.green : Color.red;
            Gizmos.DrawWireSphere(placementPosition, currentBuilding.buildingRadius + spacingPadding);
        }
    }

    [Serializable]
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
