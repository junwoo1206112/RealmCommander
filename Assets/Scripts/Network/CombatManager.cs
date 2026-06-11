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
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (GetComponent<NetworkIdentity>() == null)
            {
                gameObject.AddComponent<NetworkIdentity>();
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
            var attackerHero = attacker.GetComponent<RPG.Hero>();
            var targetUnit = target.GetComponent<RTS.Unit>();
            var targetBuilding = target.GetComponent<RTS.Building>();

            if (targetUnit == null && targetBuilding == null) return false;
            if (attackerUnit == null && attackerHero == null) return false;
            if (attackerUnit != null && !attackerUnit.IsAlive) return false;
            if (attackerHero != null && !attackerHero.IsAlive) return false;
            if (targetUnit != null && !targetUnit.IsAlive) return false;
            if (targetBuilding != null && !targetBuilding.IsAlive) return false;

            float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
            float attackRange = attackerUnit != null ? attackerUnit.AttackRange : attackerHero.Data.attackRange;
            if (distance > attackRange * 1.2f) return false;

            bool attackerIsEnemy = attackerUnit != null ? attackerUnit.IsEnemy : attackerHero.IsEnemy;
            bool targetIsEnemy = targetUnit != null ? targetUnit.IsEnemy
                : (targetBuilding != null && targetBuilding.tag == "Enemy");

            if (attackerIsEnemy == targetIsEnemy) return false;

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
                return;
            }

            var targetBuilding = target.GetComponent<RTS.Building>();
            if (targetBuilding != null)
                targetBuilding.TakeDamage(damage);
        }

        [Server]
        public void ApplySkillDamage(GameObject caster, GameObject target, float damage, bool canHitAllies = false)
        {
            if (caster == null || target == null) return;
            if (caster == target) return;

            if (!canHitAllies)
            {
                var casterUnit = caster.GetComponent<RTS.Unit>();
                var casterHero = caster.GetComponent<RPG.Hero>();
                var targetUnit = target.GetComponent<RTS.Unit>();
                var targetBuilding = target.GetComponent<RTS.Building>();

                bool casterIsEnemy = casterUnit != null ? casterUnit.IsEnemy : (casterHero != null && casterHero.IsEnemy);
                bool targetIsEnemy = targetUnit != null ? targetUnit.IsEnemy
                    : (targetBuilding != null && targetBuilding.tag == "Enemy");

                if (casterIsEnemy == targetIsEnemy) return;
            }

            var tUnit = target.GetComponent<RTS.Unit>();
            if (tUnit != null && tUnit.IsAlive)
            {
                tUnit.TakeDamage(damage);
                return;
            }

            var tBuilding = target.GetComponent<RTS.Building>();
            if (tBuilding != null && tBuilding.IsAlive)
                tBuilding.TakeDamage(damage);
        }
    }
}
