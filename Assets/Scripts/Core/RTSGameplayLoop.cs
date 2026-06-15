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

        private void Start()
        {
            EnsureResourceNodes();
        }

        private void Update()
        {
            if (!Mirror.NetworkServer.active) return;
            if (Time.time < nextHotkeyTime) return;

            int localTeam = GetLocalTeamId();

            if (Input.GetKeyDown(KeyCode.B))
            {
                nextHotkeyTime = Time.time + 0.2f;
                TryBuildNearCommander(BuildingType.Barracks, "Barracks", barracksGoldCost, 0f, 3.2f, localTeam);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                nextHotkeyTime = Time.time + 0.2f;
                TryBuildNearCommander(BuildingType.ResourceGenerator, "Resource Generator", generatorGoldCost, generatorManaCost, -3.2f, localTeam);
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                nextHotkeyTime = Time.time + 0.2f;
                QueueProductionOnNearestBuilding(0, localTeam);
            }
            else if (Input.GetKeyDown(KeyCode.O))
            {
                nextHotkeyTime = Time.time + 0.2f;
                QueueProductionOnNearestBuilding(1, localTeam);
            }
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
