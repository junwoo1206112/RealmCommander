using Mirror;
using UnityEngine;

namespace RealmCommander.Network
{
    public class CombatManager : NetworkBehaviour
    {
        public static CombatManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        [Server]
        public bool ValidateAttack(GameObject attacker, GameObject target)
        {
            if (attacker == null || target == null) return false;
            if (attacker == target) return false;

            var attackerUnit = attacker.GetComponent<RTS.Unit>();
            var targetUnit = target.GetComponent<RTS.Unit>();
            var targetBuilding = target.GetComponent<RTS.Building>();

            if (targetUnit == null && targetBuilding == null) return false;
            if (attackerUnit == null) return false;
            if (!attackerUnit.IsAlive) return false;
            if (targetUnit != null && !targetUnit.IsAlive) return false;
            if (targetBuilding != null && !targetBuilding.IsAlive) return false;

            float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
            if (distance > attackerUnit.AttackRange * 1.2f) return false;

            int attackerTeam = attackerUnit.IsEnemy ? 1 : 0;
            bool targetIsEnemy = targetUnit != null ? targetUnit.IsEnemy
                : (targetBuilding != null && targetBuilding.TeamId != attackerTeam);

            if (attackerUnit.IsEnemy == targetIsEnemy) return false;

            return true;
        }

        [Server]
        public void ApplyCombatDamage(GameObject attacker, GameObject target, float damage)
        {
            if (!ValidateAttack(attacker, target)) return;

            var targetUnit = target.GetComponent<RTS.Unit>();
            if (targetUnit != null)
            {
                targetUnit.TakeDamage(damage);
                RpcShowCombatFeedback(target, damage);
                return;
            }

            var targetBuilding = target.GetComponent<RTS.Building>();
            if (targetBuilding != null)
            {
                targetBuilding.TakeDamage(damage);
                RpcShowCombatFeedback(target, damage);
            }
        }

        [ClientRpc]
        private void RpcShowCombatFeedback(GameObject target, float damage)
        {
            if (target == null) return;
            Color color = new Color(1f, 0.22f, 0.12f);
            Visuals.CombatFeedback.PlayHit(target, color);
            Visuals.CombatFeedback.ShowDamageNumber(target, damage);
        }
    }
}
