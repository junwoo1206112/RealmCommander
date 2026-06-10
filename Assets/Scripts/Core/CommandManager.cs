using System;
using UnityEngine;

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

        public void IssueMoveCommand(Vector3 position)
        {
            OnMoveCommand?.Invoke(position);
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
                var targetUnit = hitInfo.collider.GetComponent<RTS.Unit>();
                if (targetUnit != null && targetUnit.IsEnemy)
                {
                    IssueAttackCommand(hitInfo.collider.gameObject);
                    return;
                }
            }

            IssueMoveCommand(worldPosition);
        }
    }
}
