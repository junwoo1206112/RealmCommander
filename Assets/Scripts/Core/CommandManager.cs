using System;
using UnityEngine;
using UnityEngine.AI;

namespace RealmCommander.Core
{
    public class CommandManager : MonoBehaviour
    {
        public static CommandManager Instance { get; private set; }

        public event Action<Vector3> OnMoveCommand;
        public event Action<GameObject> OnAttackCommand;
        public event Action<Vector3, int> OnBuildCommand;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void IssueMoveCommand(Vector3 position)
        {
            Vector3 final = position;
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                final = hit.position;
            OnMoveCommand?.Invoke(final);
        }

        public void IssueAttackCommand(GameObject target)
        {
            if (target != null)
            {
                OnAttackCommand?.Invoke(target);
            }
        }

        public void IssueBuildCommand(Vector3 position, int buildingType)
        {
            OnBuildCommand?.Invoke(position, buildingType);
        }

        public void ProcessRightClick(Vector3 worldPosition, RaycastHit hitInfo)
        {
            if (hitInfo.collider != null)
            {
                var targetUnit = hitInfo.collider.GetComponentInParent<RTS.Unit>();
                if (IsHostileToSelection(targetUnit))
                {
                    IssueAttackCommand(targetUnit.gameObject);
                    return;
                }

                var targetBuilding = hitInfo.collider.GetComponentInParent<RTS.Building>();
                if (IsHostileToSelection(targetBuilding))
                {
                    IssueAttackCommand(targetBuilding.gameObject);
                    return;
                }
            }

            IssueMoveCommand(worldPosition);
        }

        public bool IsHostileToSelection(RTS.Unit target)
        {
            if (target == null || SelectionManager.Instance == null) return false;

            foreach (GameObject selected in SelectionManager.Instance.SelectedUnits)
            {
                RTS.Unit selectedUnit = selected != null ? selected.GetComponent<RTS.Unit>() : null;
                if (selectedUnit != null)
                    return selectedUnit.IsEnemy != target.IsEnemy;
            }

            return false;
        }

        public bool IsHostileToSelection(RTS.Building target)
        {
            if (target == null || SelectionManager.Instance == null) return false;
            if (!target.IsAlive) return false;

            foreach (GameObject selected in SelectionManager.Instance.SelectedUnits)
            {
                RTS.Unit selectedUnit = selected != null ? selected.GetComponent<RTS.Unit>() : null;
                if (selectedUnit != null)
                    return (target.CompareTag("Enemy")) != selectedUnit.IsEnemy;
            }

            return false;
        }
    }
}
