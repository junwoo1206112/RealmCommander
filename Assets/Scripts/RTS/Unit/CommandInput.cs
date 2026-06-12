using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using RealmCommander.Core;

namespace RealmCommander.RTS
{
    public class CommandInput : MonoBehaviour
    {
        [SerializeField] private Camera commandCamera;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask unitLayer;
        [SerializeField] private LayerMask buildingLayer;

        private int combinedMask;

        private void Awake()
        {
            commandCamera = ResolveCommandCamera(commandCamera);
            combinedMask = groundLayer | unitLayer | buildingLayer;
            if (combinedMask == 0)
                combinedMask = Physics.DefaultRaycastLayers;
        }

        private void Update()
        {
            if (MobileRTSInput.TouchControlsActive) return;
            if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick();
            }
        }

        private void HandleRightClick()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (SelectionManager.Instance == null || SelectionManager.Instance.SelectedCount == 0) return;

            Camera cam = ResolveCommandCamera(commandCamera);
            if (cam == null) return;
            commandCamera = cam;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, combinedMask);
            if (hits.Length == 0) return;
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // Pass 1: Find the closest enemy unit among ALL hits.
            int firstEnemyIndex = -1;
            for (int i = 0; i < hits.Length; i++)
            {
                var unit = hits[i].collider.GetComponentInParent<Unit>();
                if (CommandManager.Instance != null && CommandManager.Instance.IsHostileToSelection(unit))
                {
                    firstEnemyIndex = i;
                    break;
                }
            }
            if (firstEnemyIndex >= 0)
            {
                Unit target = hits[firstEnemyIndex].collider.GetComponentInParent<Unit>();
                CommandManager.Instance?.IssueAttackCommand(target.gameObject);
                return;
            }

            // Pass 1b: Find the closest enemy building among ALL hits.
            int firstEnemyBuildingIndex = -1;
            for (int i = 0; i < hits.Length; i++)
            {
                var building = hits[i].collider.GetComponentInParent<Building>();
                if (CommandManager.Instance != null && CommandManager.Instance.IsHostileToSelection(building))
                {
                    firstEnemyBuildingIndex = i;
                    break;
                }
            }
            if (firstEnemyBuildingIndex >= 0)
            {
                Building target = hits[firstEnemyBuildingIndex].collider.GetComponentInParent<Building>();
                CommandManager.Instance?.IssueAttackCommand(target.gameObject);
                return;
            }

            // Pass 2: Move to first non-unit, non-building hit on NavMesh (ground/terrain)
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.GetComponentInParent<Unit>() == null && hits[i].collider.GetComponentInParent<Building>() == null)
                {
                    if (NavMesh.SamplePosition(hits[i].point, out NavMeshHit navHit, 1f, NavMesh.AllAreas))
                    {
                        CommandManager.Instance?.IssueMoveCommand(navHit.position);
                        return;
                    }
                }
            }
        }

        private static Camera ResolveCommandCamera(Camera preferred)
        {
            if (preferred != null && preferred.isActiveAndEnabled && !preferred.orthographic)
                return preferred;

            Camera main = Camera.main;
            if (main != null && main.isActiveAndEnabled && !main.orthographic)
                return main;

            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            Camera best = null;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera candidate = cameras[i];
                if (!candidate.isActiveAndEnabled || candidate.orthographic) continue;
                if (best == null || candidate.depth > best.depth)
                    best = candidate;
            }

            return best;
        }
    }
}
