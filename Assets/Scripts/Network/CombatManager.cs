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
            }
        }

        [Server]
        public bool ValidateAttack(GameObject attacker, GameObject target)
        {
            if (attacker == null || target == null) return false;

            var attackerUnit = attacker.GetComponent<RTS.Unit>();
            var attackerHero = attacker.GetComponent<RPG.Hero>();
            var targetUnit = target.GetComponent<RTS.Unit>();

            if (targetUnit == null) return false;
            if (attackerUnit == null && attackerHero == null) return false;
            if (attackerUnit != null && !attackerUnit.IsAlive) return false;
            if (attackerHero != null && !attackerHero.IsAlive) return false;
            if (!targetUnit.IsAlive) return false;

            float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
            float attackRange = attackerUnit != null ? attackerUnit.AttackRange : attackerHero.Data.attackRange;
            if (distance > attackRange * 1.2f) return false;

            bool attackerIsEnemy = attackerUnit != null && attackerUnit.IsEnemy;
            if (attackerIsEnemy == targetUnit.IsEnemy) return false;

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
                RpcOnDamageApplied(target, damage);
            }
        }

        [Server]
        public void ApplySkillDamage(GameObject caster, GameObject target, float damage)
        {
            if (caster == null || target == null) return;

            if (caster == target) return;

            var targetUnit = target.GetComponent<RTS.Unit>();
            if (targetUnit != null && targetUnit.IsAlive)
            {
                targetUnit.TakeDamage(damage);
                RpcOnDamageApplied(target, damage);
            }
        }

        [ClientRpc]
        private void RpcOnDamageApplied(GameObject target, float damage)
        {
            Debug.Log($"Damage: {damage} applied to {target.name}");
        }
    }
}
