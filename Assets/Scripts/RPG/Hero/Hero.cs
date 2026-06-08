using System;
using System.Collections.Generic;
using UnityEngine;

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

    public class Hero : MonoBehaviour
    {
        [Header("Hero Data")]
        [SerializeField] private HeroData heroData;

        [Header("Visual")]
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private Renderer heroRenderer;
        [SerializeField] private Color heroColor = Color.yellow;

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
            heroData.currentHealth = heroData.maxHealth;
            heroData.currentMana = heroData.maxMana;

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }

            if (heroRenderer != null)
            {
                heroRenderer.material.color = heroColor;
            }
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
            isSelected = selected;
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(selected);
            }
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            heroData.currentHealth = Mathf.Max(0, heroData.currentHealth - damage);
            OnStatsChanged?.Invoke(heroData);

            if (heroData.currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            heroData.currentHealth = Mathf.Min(heroData.maxHealth, heroData.currentHealth + amount);
            OnStatsChanged?.Invoke(heroData);
        }

        public void GainExp(float amount)
        {
            heroData.currentExp += amount;

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

            OnLevelUp?.Invoke();
            OnStatsChanged?.Invoke(heroData);

            Debug.Log($"Hero leveled up to {heroData.level}!");
        }

        public bool TryCastSkill(int skillIndex, GameObject target)
        {
            if (skillIndex < 0 || skillIndex >= heroData.skills.Count) return false;

            var skill = heroData.skills[skillIndex];
            if (!skill.IsReady) return false;
            if (heroData.currentMana < skill.manaCost) return false;

            heroData.currentMana -= skill.manaCost;
            skill.currentCooldown = skill.cooldown;

            if (target != null)
            {
                var targetUnit = target.GetComponent<RTS.Unit>();
                if (targetUnit != null)
                {
                    targetUnit.TakeDamage(skill.damage);
                    GainExp(skill.damage * 0.5f);
                }
            }

            OnStatsChanged?.Invoke(heroData);
            return true;
        }

        private void TryAttack()
        {
            if (Time.time - lastAttackTime >= heroData.attackSpeed)
            {
                lastAttackTime = Time.time;
                var targetUnit = currentTarget.GetComponent<RTS.Unit>();
                if (targetUnit != null)
                {
                    targetUnit.TakeDamage(heroData.attackDamage);
                    GainExp(heroData.attackDamage * 0.1f);
                }
            }
        }

        public void SetTarget(GameObject target)
        {
            currentTarget = target;
        }

        private void Die()
        {
            OnDeath?.Invoke();
            Debug.Log("Hero has fallen!");
        }
    }
}
