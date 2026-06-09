using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace RealmCommander.RPG
{
    [Serializable]
    public class HeroData
    {
        public string heroName;
        public int level = 1;
        public float currentExp;
        public float expToNextLevel = 100f;

        public float maxHealth = 200f;
        public float currentHealth;
        public float maxMana = 100f;
        public float currentMana;

        public float attackDamage = 25f;
        public float attackSpeed = 1.5f;
        public float moveSpeed = 6f;
        public float attackRange = 3f;

        public List<SkillData> skills = new List<SkillData>();

        public float HealthPercent => currentHealth / maxHealth;
        public float ManaPercent => currentMana / maxMana;
        public float ExpPercent => currentExp / expToNextLevel;
    }

    [Serializable]
    public class SkillData
    {
        public string skillName;
        public string description;
        public float cooldown = 5f;
        public float manaCost = 20f;
        public float damage = 50f;
        public float range = 5f;
        public Sprite icon;

        [HideInInspector] public float currentCooldown;

        public bool IsReady => currentCooldown <= 0;
        public float CooldownPercent => currentCooldown / cooldown;
    }

    [RequireComponent(typeof(NetworkIdentity))]
    public class Hero : NetworkBehaviour
    {
        [Header("Hero Data")]
        [SerializeField] private HeroData heroData;

        [Header("Visual")]
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private Renderer heroRenderer;
        [SerializeField] private Color heroColor = Color.yellow;

        [SyncVar(hook = nameof(OnSyncHealthChanged))]
        private float syncHealth;
        [SyncVar(hook = nameof(OnSyncManaChanged))]
        private float syncMana;
        [SyncVar(hook = nameof(OnSyncLevelChanged))]
        private int syncLevel = 1;
        [SyncVar(hook = nameof(OnSyncExpChanged))]
        private float syncExp;

        private float lastAttackTime;
        private GameObject currentTarget;
        private bool isSelected;

        public HeroData Data => heroData;
        public bool IsAlive => heroData.currentHealth > 0;
        public bool IsSelected => isSelected;

        public event Action<HeroData> OnStatsChanged;
        public event Action OnLevelUp;
        public event Action OnDeath;

        private void Awake()
        {
            heroData ??= new HeroData { heroName = gameObject.name };

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }

            if (heroRenderer != null)
            {
                heroRenderer.material.color = heroColor;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            heroData.currentHealth = heroData.maxHealth;
            heroData.currentMana = heroData.maxMana;
            syncHealth = heroData.currentHealth;
            syncMana = heroData.currentMana;
            syncLevel = heroData.level;
            syncExp = heroData.currentExp;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            heroData.currentHealth = syncHealth;
            heroData.currentMana = syncMana;
            heroData.level = syncLevel;
            heroData.currentExp = syncExp;
        }

        private void Update()
        {
            if (!IsAlive) return;

            RegenerateMana();

            if (currentTarget != null)
            {
                var targetUnit = currentTarget.GetComponent<RTS.Unit>();
                if (targetUnit != null && targetUnit.IsAlive)
                {
                    float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

                    if (distance <= heroData.attackRange)
                    {
                        TryAttack();
                    }
                }
                else
                {
                    currentTarget = null;
                }
            }

            UpdateSkillCooldowns();
        }

        private void RegenerateMana()
        {
            if (heroData.currentMana < heroData.maxMana)
            {
                heroData.currentMana = Mathf.Min(heroData.maxMana, heroData.currentMana + 2f * Time.deltaTime);
                if (isServer)
                {
                    syncMana = heroData.currentMana;
                }
                OnStatsChanged?.Invoke(heroData);
            }
        }

        private void UpdateSkillCooldowns()
        {
            foreach (var skill in heroData.skills)
            {
                if (skill.currentCooldown > 0)
                {
                    skill.currentCooldown -= Time.deltaTime;
                    if (skill.currentCooldown < 0) skill.currentCooldown = 0;
                }
            }
        }

        public void SetSelected(bool selected)
        {
            if (!isOwned) return;
            isSelected = selected;
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(selected);
            }
        }

        [Server]
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            heroData.currentHealth = Mathf.Max(0, heroData.currentHealth - damage);
            syncHealth = heroData.currentHealth;
            OnStatsChanged?.Invoke(heroData);

            if (heroData.currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            heroData.currentHealth = Mathf.Min(heroData.maxHealth, heroData.currentHealth + amount);
            if (isServer)
            {
                syncHealth = heroData.currentHealth;
            }
            OnStatsChanged?.Invoke(heroData);
        }

        public void GainExp(float amount)
        {
            heroData.currentExp += amount;
            if (isServer)
            {
                syncExp = heroData.currentExp;
            }

            while (heroData.currentExp >= heroData.expToNextLevel)
            {
                LevelUp();
            }

            OnStatsChanged?.Invoke(heroData);
        }

        private void LevelUp()
        {
            heroData.currentExp -= heroData.expToNextLevel;
            heroData.level++;
            heroData.expToNextLevel *= 1.5f;

            heroData.maxHealth += 20f;
            heroData.currentHealth = heroData.maxHealth;
            heroData.maxMana += 10f;
            heroData.currentMana = heroData.maxMana;
            heroData.attackDamage += 5f;

            if (isServer)
            {
                syncLevel = heroData.level;
                syncHealth = heroData.currentHealth;
                syncMana = heroData.currentMana;
                syncExp = heroData.currentExp;
            }

            OnLevelUp?.Invoke();
            OnStatsChanged?.Invoke(heroData);

            Debug.Log($"Hero leveled up to {heroData.level}!");
        }

        [Command]
        public void CmdCastSkill(int skillIndex, GameObject target)
        {
            if (skillIndex < 0 || skillIndex >= heroData.skills.Count) return;

            var skill = heroData.skills[skillIndex];
            if (!skill.IsReady) return;
            if (heroData.currentMana < skill.manaCost) return;

            heroData.currentMana -= skill.manaCost;
            syncMana = heroData.currentMana;
            skill.currentCooldown = skill.cooldown;

            if (target != null)
            {
                var targetUnit = target.GetComponent<RTS.Unit>();
                if (targetUnit == null || !targetUnit.IsAlive) return;
                if (Vector3.Distance(transform.position, target.transform.position) > skill.range) return;

                var combat = Network.CombatManager.Instance;
                if (combat != null)
                {
                    combat.ApplySkillDamage(gameObject, target, skill.damage);
                }
                GainExp(skill.damage * 0.5f);
            }

            OnStatsChanged?.Invoke(heroData);
            RpcOnSkillCast(skillIndex);
        }

        [ClientRpc]
        private void RpcOnSkillCast(int skillIndex)
        {
            Debug.Log($"Skill {skillIndex} cast!");
        }

        public bool TryCastSkill(int skillIndex, GameObject target)
        {
            if (!CanCastSkill(skillIndex, target)) return false;

            if (isServer)
            {
                InternalCastSkill(skillIndex, target);
                return true;
            }
            else
            {
                CmdCastSkill(skillIndex, target);
                return true;
            }
        }

        private void InternalCastSkill(int skillIndex, GameObject target)
        {
            if (skillIndex < 0 || skillIndex >= heroData.skills.Count) return;

            var skill = heroData.skills[skillIndex];
            if (!skill.IsReady) return;
            if (heroData.currentMana < skill.manaCost) return;

            heroData.currentMana -= skill.manaCost;
            syncMana = heroData.currentMana;
            skill.currentCooldown = skill.cooldown;

            if (target != null)
            {
                var targetUnit = target.GetComponent<RTS.Unit>();
                if (targetUnit == null || !targetUnit.IsAlive) return;
                if (Vector3.Distance(transform.position, target.transform.position) > skill.range) return;

                var combat = Network.CombatManager.Instance;
                if (combat != null)
                {
                    combat.ApplySkillDamage(gameObject, target, skill.damage);
                }
                GainExp(skill.damage * 0.5f);
            }

            OnStatsChanged?.Invoke(heroData);
        }

        private void TryAttack()
        {
            if (Time.time - lastAttackTime >= heroData.attackSpeed)
            {
                lastAttackTime = Time.time;
                var targetUnit = currentTarget.GetComponent<RTS.Unit>();
                if (targetUnit != null)
                {
                    if (isServer)
                    {
                        targetUnit.TakeDamage(heroData.attackDamage);
                    }
                    else
                    {
                        CmdRequestAttack(currentTarget);
                    }
                    GainExp(heroData.attackDamage * 0.1f);
                }
            }
        }

        [Command]
        private void CmdRequestAttack(GameObject target)
        {
            if (target == null) return;

            var combat = Network.CombatManager.Instance;
            if (combat != null)
            {
                combat.ApplyCombatDamage(gameObject, target, heroData.attackDamage);
            }
        }

        private bool CanCastSkill(int skillIndex, GameObject target)
        {
            if (skillIndex < 0 || skillIndex >= heroData.skills.Count) return false;

            var skill = heroData.skills[skillIndex];
            if (!skill.IsReady) return false;
            if (heroData.currentMana < skill.manaCost) return false;
            if (target == null) return true;

            var targetUnit = target.GetComponent<RTS.Unit>();
            if (targetUnit == null || !targetUnit.IsAlive) return false;

            return Vector3.Distance(transform.position, target.transform.position) <= skill.range;
        }

        private void OnSyncHealthChanged(float oldValue, float newValue)
        {
            heroData.currentHealth = newValue;
            OnStatsChanged?.Invoke(heroData);
        }

        private void OnSyncManaChanged(float oldValue, float newValue)
        {
            heroData.currentMana = newValue;
            OnStatsChanged?.Invoke(heroData);
        }

        private void OnSyncLevelChanged(int oldValue, int newValue)
        {
            heroData.level = newValue;
            OnStatsChanged?.Invoke(heroData);
        }

        private void OnSyncExpChanged(float oldValue, float newValue)
        {
            heroData.currentExp = newValue;
            OnStatsChanged?.Invoke(heroData);
        }

        public void SetTarget(GameObject target)
        {
            currentTarget = target;
        }

        private void Die()
        {
            OnDeath?.Invoke();

            if (isServer)
            {
                RpcOnDeath();
            }

            Debug.Log("Hero has fallen!");
        }

        [ClientRpc]
        private void RpcOnDeath()
        {
            Debug.Log("Hero has fallen!");
        }
    }
}
