using RealmCommander.RTS;
using RealmCommander.Network;
using UnityEngine;
using UnityEngine.AI;

namespace RealmCommander.Core
{
    public class RTSGameplayLoop : MonoBehaviour
    {
        [SerializeField] private float barracksGoldCost = 140f;
        [SerializeField] private float generatorGoldCost = 110f;
        [SerializeField] private float generatorManaCost = 20f;

        private float nextHotkeyTime;
        private const float BuildGridSize = 1f;
        private const float BuildClearance = 2.8f;

        private bool isPlacing;
        private BuildingType pendingBuildType;
        private string pendingBuildName;
        private float pendingGoldCost;
        private float pendingManaCost;
        private GameObject placementGhost;
        private MaterialPropertyBlock ghostColorBlock;
        private static readonly Color GhostValidColor = new Color(0f, 1f, 0f, 0.35f);
        private static readonly Color GhostInvalidColor = new Color(1f, 0f, 0f, 0.35f);

        private void Start()
        {
            EnsureResourceNodes();
        }

        private void OnDestroy()
        {
            if (placementGhost != null)
                Destroy(placementGhost);
        }

        private void StartPlacementMode(BuildingType type, string name, float goldCost, float manaCost)
        {
            int localTeam = GetLocalTeamId();
            ResourceManager resources = ResourceManager.Instance;
            if (resources == null || !resources.CanAfford(localTeam, goldCost, manaCost))
            {
                Debug.Log($"[RTSLoop] Not enough resources for {name}.");
                return;
            }

            isPlacing = true;
            pendingBuildType = type;
            pendingBuildName = name;
            pendingGoldCost = goldCost;
            pendingManaCost = manaCost;

            if (placementGhost == null)
            {
                placementGhost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                placementGhost.name = "PlacementGhost";
                Collider col = placementGhost.GetComponent<Collider>();
                if (col != null) Destroy(col);
                ghostColorBlock = new MaterialPropertyBlock();
            }

            placementGhost.transform.localScale = type == BuildingType.ResourceGenerator
                ? new Vector3(1.6f, 0.9f, 1.6f)
                : new Vector3(2.1f, 1f, 2.1f);

            ApplyGhostColor(false);
            placementGhost.SetActive(true);
        }

        private void HandlePlacementMode()
        {
            Camera cam = NetworkUtils.GetMainCamera();
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                Vector3 pos = SnapToGrid(hit.point, BuildGridSize);
                pos.y = 0f;
                placementGhost.transform.position = pos;

                bool valid = IsBuildAreaClear(pos, BuildClearance)
                    && NavMesh.SamplePosition(pos, out NavMeshHit _, 1f, NavMesh.AllAreas);
                ApplyGhostColor(valid);

                if (Input.GetMouseButtonDown(0) && valid)
                {
                    PlaceBuildingAtPosition(pos);
                    return;
                }
            }

            if (Input.GetMouseButtonDown(1))
                CancelPlacementMode();
        }

        private void ApplyGhostColor(bool valid)
        {
            if (placementGhost == null || ghostColorBlock == null) return;
            Renderer renderer = placementGhost.GetComponent<Renderer>();
            if (renderer == null) return;
            ghostColorBlock.SetColor("_Color", valid ? GhostValidColor : GhostInvalidColor);
            renderer.SetPropertyBlock(ghostColorBlock);
        }

        private void CancelPlacementMode()
        {
            isPlacing = false;
            if (placementGhost != null)
                placementGhost.SetActive(false);
        }

        private void PlaceBuildingAtPosition(Vector3 position)
        {
            isPlacing = false;
            if (placementGhost != null)
                placementGhost.SetActive(false);

            int localTeam = GetLocalTeamId();
            ResourceManager resources = ResourceManager.Instance;
            if (resources == null || !resources.TrySpend(localTeam, pendingGoldCost, pendingManaCost)) return;

            GameObject buildingObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buildingObject.name = pendingBuildName;
            buildingObject.transform.position = position;
            buildingObject.transform.localScale = pendingBuildType == BuildingType.ResourceGenerator
                ? new Vector3(1.6f, 0.9f, 1.6f)
                : new Vector3(2.1f, 1f, 2.1f);
            if (buildingObject.GetComponent<Mirror.NetworkIdentity>() == null)
                buildingObject.AddComponent<Mirror.NetworkIdentity>();

            Building building = buildingObject.AddComponent<Building>();
            building.ConfigureRuntimeBuilding(pendingBuildName, pendingBuildType, localTeam);
            building.StartConstruction();

            if (pendingBuildType == BuildingType.ResourceGenerator)
                buildingObject.AddComponent<ResourceGenerator>();

            Mirror.NetworkServer.Spawn(buildingObject, NetworkUtils.FindTeamConnection(localTeam));
            SelectionManager.Instance?.SelectUnit(buildingObject);
            Debug.Log($"[RTSLoop] Built {pendingBuildName} at {position}.");
        }

        private void Update()
        {
            if (isPlacing)
            {
                HandlePlacementMode();
                return;
            }

            if (Time.time < nextHotkeyTime) return;

            bool isServer = Mirror.NetworkServer.active;
            bool isClientOnly = Mirror.NetworkClient.active && !isServer;

            if (Input.GetKeyDown(KeyCode.B))
            {
                nextHotkeyTime = Time.time + 0.2f;
                if (isClientOnly)
                    NetworkGameManager.Instance?.CmdRequestBuild((int)BuildingType.Barracks);
                else if (isServer)
                    StartPlacementMode(BuildingType.Barracks, "Barracks", barracksGoldCost, 0f);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                nextHotkeyTime = Time.time + 0.2f;
                if (isClientOnly)
                    NetworkGameManager.Instance?.CmdRequestBuild((int)BuildingType.ResourceGenerator);
                else if (isServer)
                    StartPlacementMode(BuildingType.ResourceGenerator, "Resource Generator", generatorGoldCost, generatorManaCost);
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                nextHotkeyTime = Time.time + 0.2f;
                if (isClientOnly)
                    RequestClientProduction(0);
                else if (isServer)
                    QueueProductionOnNearestBuilding(0, GetLocalTeamId());
            }
            else if (Input.GetKeyDown(KeyCode.O))
            {
                nextHotkeyTime = Time.time + 0.2f;
                if (isClientOnly)
                    RequestClientProduction(1);
                else if (isServer)
                    QueueProductionOnNearestBuilding(1, GetLocalTeamId());
            }
        }

        private static void RequestClientProduction(int productionIndex)
        {
            if (NetworkGameManager.Instance == null) return;
            int localTeam = NetworkPlayer.Local != null ? NetworkPlayer.Local.TeamId : 0;
            Building building = FindNearestFriendlyProducer(localTeam);
            if (building == null) return;
            NetworkGameManager.Instance.CmdRequestProduction(building.netId, productionIndex);
        }

        public static void ExecuteBuildCommand(BuildingType type, int teamId)
        {
            RTSGameplayLoop instance = FindAnyObjectByType<RTSGameplayLoop>();
            if (instance == null) return;

            float goldCost = 0f, manaCost = 0f, sideOffset = 0f;
            string name = "";

            if (type == BuildingType.Barracks)
            {
                name = "Barracks";
                goldCost = instance.barracksGoldCost;
                sideOffset = 3.2f;
            }
            else if (type == BuildingType.ResourceGenerator)
            {
                name = "Resource Generator";
                goldCost = instance.generatorGoldCost;
                manaCost = instance.generatorManaCost;
                sideOffset = -3.2f;
            }

            TryBuildNearCommander(type, name, goldCost, manaCost, sideOffset, teamId);
        }

        private static int GetLocalTeamId()
        {
            return NetworkPlayer.Local != null ? NetworkPlayer.Local.TeamId : 0;
        }

        private static void EnsureResourceNodes()
        {
            CreateResourceNode("Gold Mine", new Vector3(-14f, 0.25f, 0f), ResourceType.Gold, new Vector3(1.4f, 0.5f, 1.4f));
            CreateResourceNode("Gold Mine 2", new Vector3(14f, 0.25f, 0f), ResourceType.Gold, new Vector3(1.4f, 0.5f, 1.4f));
            CreateResourceNode("Mana Spring", new Vector3(-10f, 0.18f, -8f), ResourceType.Mana, new Vector3(1.2f, 0.36f, 1.2f));
            CreateResourceNode("Mana Spring 2", new Vector3(10f, 0.18f, 8f), ResourceType.Mana, new Vector3(1.2f, 0.36f, 1.2f));
        }

        private static void CreateResourceNode(string name, Vector3 position, ResourceType type, Vector3 scale)
        {
            if (GameObject.Find(name) != null) return;
            GameObject node = GameObject.CreatePrimitive(type == ResourceType.Gold ? PrimitiveType.Cube : PrimitiveType.Sphere);
            node.name = name;
            node.transform.position = position;
            node.transform.localScale = scale;
            Collider collider = node.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;
            node.AddComponent<ResourceNode>().Configure(type);
        }

        private static void TryBuildNearCommander(BuildingType type, string name, float goldCost, float manaCost, float sideOffset, int teamId)
        {
            ResourceManager resources = ResourceManager.Instance;
            if (resources == null || !resources.CanAfford(teamId, goldCost, manaCost))
            {
                Debug.Log($"[RTSLoop] Not enough resources for {name}.");
                return;
            }

            Vector3 anchor = GetFriendlyCommanderPosition(teamId);
            if (!TryFindBuildPosition(anchor, sideOffset, teamId, out Vector3 position))
            {
                Debug.Log($"[RTSLoop] No clear build slot found for {name}.");
                return;
            }

            if (!resources.TrySpend(teamId, goldCost, manaCost))
            {
                Debug.Log($"[RTSLoop] Not enough resources for {name}.");
                return;
            }

            GameObject buildingObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buildingObject.name = name;
            buildingObject.transform.position = position;
            buildingObject.transform.localScale = type == BuildingType.ResourceGenerator
                ? new Vector3(1.6f, 0.9f, 1.6f)
                : new Vector3(2.1f, 1f, 2.1f);
            if (buildingObject.GetComponent<Mirror.NetworkIdentity>() == null)
                buildingObject.AddComponent<Mirror.NetworkIdentity>();

            Building building = buildingObject.AddComponent<Building>();
            building.ConfigureRuntimeBuilding(name, type, teamId);
            building.StartConstruction();

            if (type == BuildingType.ResourceGenerator)
                buildingObject.AddComponent<ResourceGenerator>();

            Mirror.NetworkServer.Spawn(buildingObject, RealmCommander.Network.NetworkUtils.FindTeamConnection(teamId));

            SelectionManager.Instance?.SelectUnit(buildingObject);
            Debug.Log($"[RTSLoop] Built {name} for team {teamId}. B/R build, P/O train units.");
        }

        private static bool TryFindBuildPosition(Vector3 anchor, float preferredSideOffset, int teamId, out Vector3 position)
        {
            float mirror = teamId == 0 ? 1f : -1f;
            float direction = Mathf.Sign(preferredSideOffset == 0f ? 1f : preferredSideOffset) * mirror;
            float distance = Mathf.Max(3.2f, Mathf.Abs(preferredSideOffset));
            Vector3[] offsets =
            {
                new Vector3(direction * distance, 0f, 2.5f),
                new Vector3(direction * distance, 0f, -2.5f),
                new Vector3(direction * (distance + 2.8f), 0f, 0f),
                new Vector3(direction * (distance + 2.8f), 0f, 3.4f),
                new Vector3(direction * (distance + 2.8f), 0f, -3.4f),
                new Vector3(direction * (distance + 5.6f), 0f, 1.7f),
                new Vector3(direction * (distance + 5.6f), 0f, -1.7f)
            };

            foreach (Vector3 offset in offsets)
            {
                Vector3 desired = SnapToGrid(anchor + offset, BuildGridSize);
                Vector3 candidate = NavMesh.SamplePosition(desired, out NavMeshHit hit, 5f, NavMesh.AllAreas)
                    ? SnapToGrid(hit.position, BuildGridSize)
                    : desired;
                candidate.y = 0f;

                if (IsBuildAreaClear(candidate, BuildClearance))
                {
                    position = candidate;
                    return true;
                }
            }

            position = anchor;
            return false;
        }

        private static Vector3 SnapToGrid(Vector3 position, float gridSize)
        {
            if (gridSize <= 0f) return position;
            position.x = Mathf.Round(position.x / gridSize) * gridSize;
            position.z = Mathf.Round(position.z / gridSize) * gridSize;
            return position;
        }

        private static bool IsBuildAreaClear(Vector3 position, float clearance)
        {
            EntityRegistry registry = EntityRegistry.Instance;
            if (registry != null)
            {
                foreach (Building building in registry.AllBuildings)
                {
                    if (building == null || !building.IsAlive) continue;
                    if ((building.transform.position - position).sqrMagnitude < clearance * clearance)
                        return false;
                }
            }

            return true;
        }

        private static void QueueProductionOnNearestBuilding(int productionIndex, int teamId)
        {
            Building building = FindNearestFriendlyProducer(teamId);
            if (building == null)
            {
                Debug.Log("[RTSLoop] No friendly producer found. Press B to build Barracks.");
                return;
            }

            var queue = building.GetProductionQueue();
            if (queue == null || queue.Count == 0) return;
            productionIndex = Mathf.Clamp(productionIndex, 0, queue.Count - 1);
            building.QueueProduction(queue[productionIndex]);
            SelectionManager.Instance?.SelectUnit(building.gameObject);
        }

        private static Building FindNearestFriendlyProducer(int teamId)
        {
            Vector3 anchor = GetFriendlyCommanderPosition(teamId);
            Building best = null;
            float bestDistance = float.MaxValue;
            EntityRegistry registry = EntityRegistry.Instance;
            if (registry == null) return null;

            foreach (Building building in registry.AllBuildings)
            {
                if (building == null || !building.IsAlive || building.TeamId != teamId) continue;
                var queue = building.GetProductionQueue();
                if (queue == null || queue.Count == 0) continue;
                float distance = (building.transform.position - anchor).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = building;
                }
            }

            return best;
        }

        private static Vector3 GetFriendlyCommanderPosition(int teamId)
        {
            EntityRegistry registry = EntityRegistry.Instance;
            if (registry != null)
            {
                foreach (var unit in registry.AllUnits)
                {
                    bool isFriendly = teamId == 0 ? !unit.IsEnemy : unit.IsEnemy;
                    if (unit != null && isFriendly && unit.IsAlive)
                        return unit.transform.position;
                }
            }

            return teamId == 0 ? new Vector3(-18f, 0f, -4f) : new Vector3(18f, 0f, 4f);
        }
    }
}
