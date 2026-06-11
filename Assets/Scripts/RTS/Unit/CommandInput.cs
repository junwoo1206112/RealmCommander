using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using RealmCommander.Core;

namespace RealmCommander.RTS
{
    public class CommandInput : MonoBehaviour
    {
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask unitLayer;
        [SerializeField] private LayerMask buildingLayer;

        private int combinedMask;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            int g = groundLayer.value;
            int u = unitLayer.value;
            int b = buildingLayer.value;
            combinedMask = (g | u | b) != 0 ? g | u | b : ~0;
        }

        private Camera GetCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            return cam;
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

            Camera cam = GetCamera();
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, combinedMask);
            if (hits.Length == 0) return;
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // Pass 1: Find the closest enemy unit among ALL hits.
            // Enemies behind friendly units are still targetable.
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
                    if (NavMesh.SamplePosition(hits[i].point, out _, 1f, NavMesh.AllAreas))
                    {
                        CommandManager.Instance?.IssueMoveCommand(hits[i].point);
                        return;
                    }
                }
            }

            // Fallback: if no NavMesh hit found, use the first non-unit hit anyway
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.GetComponentInParent<Unit>() == null && hits[i].collider.GetComponentInParent<Building>() == null)
                {
                    CommandManager.Instance?.IssueMoveCommand(hits[i].point);
                    return;
                }
            }
        }
    }
}
