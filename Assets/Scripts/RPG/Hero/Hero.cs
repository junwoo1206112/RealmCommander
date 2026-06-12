using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using RealmCommander.Core;

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

        public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;
        public float ManaPercent => maxMana > 0f ? currentMana / maxMana : 0f;
        public float ExpPercent => expToNextLevel > 0f ? currentExp / expToNextLevel : 0f;
    }

    [Serializable]
    public enum SkillEffectType
    {
        TargetDamage,
        SelfHeal
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
        public SkillEffectType effectType;
        public Sprite icon;

        [HideInInspector] public float currentCooldown;

        public bool IsReady => currentCooldown <= 0;
        public float CooldownPercent => cooldown > 0f ? currentCooldown / cooldown : 0f;
    }

    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(NavMeshAgent))]
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
        [SyncVar(hook = nameof(OnTeamChanged))]
        private int teamId;
        [SyncVar(hook = nameof(OnSkill0CooldownChanged))]
        private float syncSkill0Cooldown;
        [SyncVar(hook = nameof(OnSkill1CooldownChanged))]
        private float syncSkill1Cooldown;

        private float lastAttackTime;
        private GameObject currentTarget;
        private bool isSelected;
        private NavMeshAgent agent;
        private bool isSubscribed;

        public HeroData Data => heroData;
        public bool IsAlive => heroData.currentHealth > 0;
        public bool IsSelected => isSelected;
        public bool IsEnemy => teamId == 1;
        public int TeamId => teamId;
        public bool CanIssueLocalCommands => !NetworkClient.active || isOwned || (NetworkServer.active && teamId == 0);
        public GameObject CurrentTarget => currentTarget;

        public event Action<HeroData> OnStatsChanged;
        public event Action OnLevelUp;
        public event Action OnDeath;

        private void Awake()
        {
            heroData ??= new HeroData { heroName = gameObject.name };
            EnsureDefaultSkills();
            agent = GetComponent<NavMeshAgent>();
            agent.speed = heroData.moveSpeed;
            agent.acceleration = 24f;
            agent.angularSpeed = 420f;
            agent.stoppingDistance = 0.4f;
            agent.radius = 0.35f;
            agent.height = 1.2f;

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }

            if (heroRenderer != null)
            {
                heroRenderer.material.color = heroColor;
            }
        }

        protected override void OnValidate() { }

        public override void OnStartServer()
        {
            base.OnStartServer();
            heroData.currentHealth = heroData.maxHealth;
            heroData.currentMana = heroData.maxMana;
            syncHealth = heroData.currentHealth;
            syncMana = heroData.currentMana;
            syncLevel = heroData.level;
            syncExp = heroData.currentExp;
            UpdateCooldownSync();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            heroData.currentHealth = syncHealth;
            heroData.currentMana = syncMana;
            heroData.level = syncLevel;
            heroData.currentExp = syncExp;
            ApplyTeamVisual();
            SubscribeToCommands();
        }

        private void Start()
        {
            SubscribeToCommands();
        }

        private void Update()
        {
            if (!IsAlive) return;

            if (!isSubscribed && CanIssueLocalCommands)
                SubscribeToCommands();

            if (isServer)
                RegenerateMana();

            if (isServer && currentTarget != null)
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

            if (isServer)
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
            UpdateCooldownSync();
        }

        public void SetSelected(bool selected)
        {
            if (!CanIssueLocalCommands) return;
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

        [Server]
        public void Heal(float amount)
        {
            if (amount <= 0f || !IsAlive) return;
            heroData.currentHealth = Mathf.Min(heroData.maxHealth, heroData.currentHealth + amount);
            syncHealth = heroData.currentHealth;
            OnStatsChanged?.Invoke(heroData);
        }

        [Server]
        public void GainExp(float amount)
        {
            if (amount <= 0f || !IsAlive) return;
            heroData.currentExp += amount;
            syncExp = heroData.currentExp;

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
            InternalCastSkill(skillIndex, target);
        }

        [ClientRpc]
        private void RpcOnSkillCast(int skillIndex)
        {
            if (!isServer && skillIndex >= 0 && skillIndex < heroData.skills.Count)
                heroData.skills[skillIndex].currentCooldown = heroData.skills[skillIndex].cooldown;
            Debug.Log($"Skill {skillIndex} cast!");
        }

        public bool TryCastSkill(int skillIndex, GameObject target)
        {
            if (!isOwned && !isServer) return false;
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
            if (!CanCastSkill(skillIndex, target)) return;

            var skill = heroData.skills[skillIndex];

            heroData.currentMana -= skill.manaCost;
            syncMana = heroData.currentMana;
            skill.currentCooldown = skill.cooldown;

            if (skill.effectType == SkillEffectType.TargetDamage)
            {
                var combat = Network.CombatManager.Instance;
                if (combat != null && target != null)
                {
                    combat.ApplySkillDamage(gameObject, target, skill.damage);
                    GainExp(skill.damage * 0.5f);
                }
            }
            else if (skill.effectType == SkillEffectType.SelfHeal)
            {
                Heal(skill.damage);
            }

            UpdateCooldownSync();
            OnStatsChanged?.Invoke(heroData);
            RpcOnSkillCast(skillIndex);
        }

        private void TryAttack()
        {
            if (Time.time - lastAttackTime >= heroData.attackSpeed)
            {
                if (!CanTarget(currentTarget)) return;
                lastAttackTime = Time.time;
                var combat = Network.CombatManager.Instance;
                if (combat != null)
                {
                    combat.ApplyCombatDamage(gameObject, currentTarget, heroData.attackDamage);
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
            if (skill.effectType == SkillEffectType.SelfHeal)
                return target == null && heroData.currentHealth < heroData.maxHealth;
            if (target == null) return false;

            var targetUnit = target.GetComponent<RTS.Unit>();
            if (targetUnit == null || !targetUnit.IsAlive) return false;

            return Vector3.Distance(transform.position, target.transform.position) <= skill.range;
        }

        [Server]
        public void ConfigureTeam(int newTeamId)
        {
            teamId = Mathf.Clamp(newTeamId, 0, 1);
            ApplyTeamVisual();
        }

        private void SubscribeToCommands()
        {
            if (isSubscribed || !CanIssueLocalCommands || CommandManager.Instance == null) return;
            SelectionManager.Instance?.RegisterSelectableUnit(gameObject);
            CommandManager.Instance.OnMoveCommand += HandleMoveCommand;
            CommandManager.Instance.OnAttackCommand += HandleAttackCommand;
            isSubscribed = true;
        }

        private void OnDestroy()
        {
            SelectionManager.Instance?.UnregisterSelectableUnit(gameObject);
            if (CommandManager.Instance != null && isSubscribed)
            {
                CommandManager.Instance.OnMoveCommand -= HandleMoveCommand;
                CommandManager.Instance.OnAttackCommand -= HandleAttackCommand;
            }
        }

        private void HandleMoveCommand(Vector3 position)
        {
            if (!CanIssueLocalCommands || SelectionManager.Instance == null || !SelectionManager.Instance.IsUnitSelected(gameObject)) return;
            currentTarget = null;
            if (isServer || !NetworkClient.active)
                ApplyMove(position);
            else
                CmdMove(position);
        }

        [Command]
        private void CmdMove(Vector3 position)
        {
            ApplyMove(position);
        }

        [Server]
        private void ApplyMove(Vector3 position)
        {
            if (agent == null || !agent.enabled) return;
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit startHit, 5f, NavMesh.AllAreas))
                agent.Warp(startHit.position);
            if (agent.isOnNavMesh && NavMesh.SamplePosition(position, out NavMeshHit hit, 1.5f, agent.areaMask))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }
        }

        private void HandleAttackCommand(GameObject target)
        {
            if (!CanIssueLocalCommands || SelectionManager.Instance == null || !SelectionManager.Instance.IsUnitSelected(gameObject)) return;
            if (isServer || !NetworkClient.active)
                SetTarget(target);
            else
                CmdSetTarget(target);
        }

        [Command]
        private void CmdSetTarget(GameObject target)
        {
            SetTarget(target);
        }

        private void EnsureDefaultSkills()
        {
            heroData.skills ??= new List<SkillData>();
            if (heroData.skills.Count >= 2) return;
            heroData.skills.Clear();
            heroData.skills.Add(new SkillData
            {
                skillName = "Arc Strike",
                description = "Deal server-authoritative damage to an enemy in range.",
                cooldown = 5f,
                manaCost = 25f,
                damage = 55f,
                range = 6f,
                effectType = SkillEffectType.TargetDamage
            });
            heroData.skills.Add(new SkillData
            {
                skillName = "Rally Heal",
                description = "Restore the hero's health.",
                cooldown = 9f,
                manaCost = 30f,
                damage = 70f,
                range = 0f,
                effectType = SkillEffectType.SelfHeal
            });
        }

        private void UpdateCooldownSync()
        {
            if (!isServer || heroData.skills.Count < 2) return;
            syncSkill0Cooldown = heroData.skills[0].currentCooldown;
            syncSkill1Cooldown = heroData.skills[1].currentCooldown;
        }

        private void OnSkill0CooldownChanged(float oldValue, float newValue)
        {
            if (heroData.skills.Count > 0) heroData.skills[0].currentCooldown = newValue;
        }

        private void OnSkill1CooldownChanged(float oldValue, float newValue)
        {
            if (heroData.skills.Count > 1) heroData.skills[1].currentCooldown = newValue;
        }

        private void OnTeamChanged(int oldValue, int newValue)
        {
            ApplyTeamVisual();
        }

        private void ApplyTeamVisual()
        {
            if (heroRenderer != null)
                heroRenderer.material.color = teamId == 1 ? new Color(1f, 0.25f, 0.1f) : heroColor;
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
            currentTarget = CanTarget(target) ? target : null;
        }

        private bool CanTarget(GameObject target)
        {
            if (target == null || target == gameObject) return false;
            RTS.Unit unit = target.GetComponent<RTS.Unit>();
            if (unit != null) return unit.IsAlive && unit.IsEnemy != IsEnemy;
            RTS.Building building = target.GetComponent<RTS.Building>();
            return building != null && building.IsAlive && building.TeamId == (IsEnemy ? 0 : 1);
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
