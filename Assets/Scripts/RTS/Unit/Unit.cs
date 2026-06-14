using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using RealmCommander.Core;

namespace RealmCommander.RTS
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NetworkIdentity))]
    public class Unit : NetworkBehaviour
    {
        [Header("Unit Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackSpeed = 1f;
        [SerializeField] private float moveSpeed = 5f;

        [Header("Team")]
        [SyncVar(hook = nameof(OnTeamChanged))]
        [SerializeField] private bool isEnemy = false;

        [Header("Visual")]
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private Renderer unitRenderer;
        [SerializeField] private Color friendlyColor = Color.blue;
        [SerializeField] private Color enemyColor = Color.red;
        [SerializeField] private Color selectedColor = Color.green;
        [SerializeField] private global::RealmCommander.Visuals.WorldModelVisual worldVisual;
        [SerializeField] private global::RealmCommander.Visuals.WorldHealthBar healthBar;

        [SyncVar(hook = nameof(OnHealthChanged))]
        private float currentHealth;
        [SyncVar]
        private float syncMaxHealth;
        [SyncVar(hook = nameof(OnArtIdChanged))]
        private string artId = "unit_soldier";
        [SyncVar(hook = nameof(OnSyncTargetChanged))]
        private uint syncTargetNetId;
#pragma warning disable CS0414
        [SyncVar(hook = nameof(OnSyncIsStoppedChanged))]
        private bool syncIsStopped;
#pragma warning restore CS0414
        [SyncVar]
        private Vector3 syncDestination;
        private NavMeshAgent agent;
        private float lastAttackTime;
        private GameObject currentTarget;
        private bool isSelected;
        private MaterialPropertyBlock colorBlock;

        public float MaxHealth => isServer ? maxHealth : syncMaxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercent
        {
            get
            {
                float max = isServer ? maxHealth : syncMaxHealth;
                return max > 0f ? currentHealth / max : 0f;
            }
        }
        public float AttackRange => attackRange;
        public bool IsEnemy => isEnemy;
        public bool IsAlive => currentHealth > 0;
        public bool IsSelected => isSelected;
        public bool CanIssueLocalCommands => !NetworkClient.active || isOwned || (NetworkServer.active && !isEnemy);

        public event Action<float, float> OnHealthChangedEvent;
        public event Action OnDeath;
        public event Action OnSelected;
        public event Action OnDeselected;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.stoppingDistance = Mathf.Max(attackRange * 0.4f, 0.5f);
                agent.acceleration = 30f;
                agent.angularSpeed = 400f;
                agent.autoBraking = false;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                agent.avoidancePriority = isEnemy ? 50 : 10;
                agent.radius = 0.25f;
                agent.height = 0.8f;
                agent.autoRepath = true;
            }

            currentHealth = maxHealth;

            if (selectionIndicator == null)
            {
                selectionIndicator = CreateSelectionIndicator();
                selectionIndicator.SetActive(false);
            }

            UpdateTeamColor();
            ApplyWorldArt();
            EnsureHealthBar();
        }

        protected override void OnValidate() { }

        private GameObject CreateSelectionIndicator()
        {
            GameObject indicatorObject = new GameObject("SelectionIndicator");
            indicatorObject.transform.SetParent(transform, false);
            indicatorObject.AddComponent<SelectionIndicator>();
            return indicatorObject;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            currentHealth = maxHealth;
            syncMaxHealth = maxHealth;
            if (agent != null)
            {
                agent.enabled = true;
                agent.speed = moveSpeed;
                agent.acceleration = 30f;
                agent.angularSpeed = 400f;
                agent.autoBraking = false;
                agent.stoppingDistance = Mathf.Max(attackRange * 0.4f, 0.5f);
                agent.avoidancePriority = isEnemy ? 50 : 10;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!isServer && agent != null)
            {
                if (isOwned)
                {
                    agent.enabled = true;
                    agent.speed = moveSpeed;
                }
                else
                {
                    agent.enabled = false;
                }
            }

            SubscribeToCommands();
        }

        private void Start()
        {
            SubscribeToCommands();
            EntityRegistry.Instance?.Register(this);
        }

        private bool isSubscribed;

        private void SubscribeToCommands()
        {
            if (isSubscribed) return;
            if (!CanIssueLocalCommands) return;

            SelectionManager.Instance?.RegisterSelectableUnit(gameObject);
            if (CommandManager.Instance != null)
            {
                CommandManager.Instance.OnMoveCommand += HandleMoveCommand;
                CommandManager.Instance.OnAttackCommand += HandleAttackCommand;
                isSubscribed = true;
            }
        }

        private void OnDestroy()
        {
            EntityRegistry.Instance?.Unregister(this);

            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance?.UnregisterSelectableUnit(gameObject);
            }

            if (CommandManager.Instance != null && isSubscribed)
            {
                CommandManager.Instance.OnMoveCommand -= HandleMoveCommand;
                CommandManager.Instance.OnAttackCommand -= HandleAttackCommand;
            }
        }

        private float lastAcquireTime;
        private float lastCommandTime;
        private float lastPathTime;
        private static readonly float AcquireInterval = 0.3f;
        private static readonly float AcquireMoveGrace = 1.5f;
        private Collider[] acquireBuffer = new Collider[32];

        private void Update()
        {
            if (!IsAlive) return;
            if (netIdentity == null) return;

            if (!isSubscribed && CanIssueLocalCommands)
            {
                SubscribeToCommands();
            }

            if (currentTarget != null)
            {
                bool targetValid = IsValidHostileTarget(currentTarget);
                if (targetValid)
                {
                    float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

                    if (distance <= attackRange)
                    {
                        agent.isStopped = true;
                        syncIsStopped = true;
                        Vector3 lookDir = currentTarget.transform.position - transform.position;
                        lookDir.y = 0;
                        if (lookDir.sqrMagnitude > 0.01f)
                            transform.rotation = Quaternion.LookRotation(lookDir);
                        TryAttack();
                    }
                    else
                    {
                        agent.isStopped = false;
                        syncIsStopped = false;
                        if (Time.time - lastPathTime >= 0.3f)
                        {
                            lastPathTime = Time.time;
                            TrySetDestination(currentTarget.transform.position);
                        }
                    }
                }
                else
                {
                    ClearTarget();
                }
            }
            else
            {
                if (Time.time - lastCommandTime >= AcquireMoveGrace)
                {
                    AutoAcquireTarget();
                }
            }
        }

        private void AutoAcquireTarget()
        {
            if (Time.time - lastAcquireTime < AcquireInterval) return;
            lastAcquireTime = Time.time;

            if (agent != null && agent.hasPath && agent.remainingDistance > attackRange)
                return;

            float detectRange = attackRange * 1.5f;

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectRange, acquireBuffer);
            GameObject nearest = null;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Unit targetUnit = acquireBuffer[i].GetComponent<Unit>();
                if (targetUnit != null && targetUnit.IsAlive && IsValidHostileTarget(targetUnit.gameObject))
                {
                    float dist = Vector3.Distance(transform.position, targetUnit.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = targetUnit.gameObject;
                    }
                    continue;
                }

                Building targetBuilding = acquireBuffer[i].GetComponent<Building>();
                if (targetBuilding != null && targetBuilding.IsAlive && IsValidHostileTarget(targetBuilding.gameObject))
                {
                    float dist = Vector3.Distance(transform.position, targetBuilding.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = targetBuilding.gameObject;
                    }
                }
            }

            if (nearest != null && nearestDist <= attackRange)
            {
                currentTarget = nearest;
            }
        }

        public void SetSelected(bool selected)
        {
            if (selected == isSelected) return;
            isSelected = selected;
            if (selectionIndicator != null)
            {
                SelectionIndicator indicator = selectionIndicator.GetComponent<SelectionIndicator>();
                if (indicator != null)
                    indicator.SetSelected(selected);
                else
                    selectionIndicator.SetActive(selected);
            }
            ApplyColor();

            if (selected)
                OnSelected?.Invoke();
            else
                OnDeselected?.Invoke();
        }

        [Server]
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        [Server]
        public void Heal(float amount)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }

        [Server]
        public void ConfigureTeam(bool enemyTeam)
        {
            isEnemy = enemyTeam;
            if (agent != null)
                agent.avoidancePriority = enemyTeam ? 50 : 10;
            UpdateTeamColor();
            ApplyWorldArt();
        }

        [Server]
        public void ApplySpec(string specId)
        {
            var spec = OpenSpec.SpecManager.Instance?.GetSpec("units", specId);
            if (spec == null)
            {
                artId = specId;
                ApplyWorldArt();
                return;
            }

            maxHealth = GetSpecFloat(spec, "MaxHealth", maxHealth);
            attackDamage = GetSpecFloat(spec, "AttackDamage", attackDamage);
            attackRange = GetSpecFloat(spec, "AttackRange", attackRange);
            attackSpeed = GetSpecFloat(spec, "AttackSpeed", attackSpeed);
            moveSpeed = GetSpecFloat(spec, "MoveSpeed", moveSpeed);

            currentHealth = maxHealth;
            syncMaxHealth = maxHealth;

            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.stoppingDistance = Mathf.Max(attackRange * 0.4f, 0.5f);
            }

            Debug.Log($"[Unit] Applied spec '{specId}': HP={maxHealth}, ATK={attackDamage}, SPD={moveSpeed}");
            artId = specId;
            ApplyWorldArt();
        }

        private static float GetSpecFloat(OpenSpec.SpecData spec, string key, float fallback)
        {
            return OpenSpec.SpecManager.Instance.GetProperty("units", spec.id, key, fallback);
        }

        private void OnHealthChanged(float oldValue, float newValue)
        {
            OnHealthChangedEvent?.Invoke(newValue, maxHealth);
        }

        private void TryAttack()
        {
            if (!isServer || !NetworkServer.active) return;
            if (Time.time - lastAttackTime >= attackSpeed)
            {
                if (currentTarget == null) return;
                lastAttackTime = Time.time;
                var combat = Network.CombatManager.Instance;
                if (combat != null)
                {
                    combat.ApplyCombatDamage(gameObject, currentTarget, attackDamage);
                    RpcPlayAttackSound();
                }
            }
        }

        [ClientRpc]
        private void RpcPlayAttackSound()
        {
            if (!isServer)
                Audio.AudioManager.Instance?.PlayUnitAttack();
        }

        private void Die()
        {
            agent.isStopped = true;
            syncIsStopped = true;
            agent.enabled = false;
            OnDeath?.Invoke();

            if (isServer || isOwned || !NetworkServer.active)
            {
                SelectionManager.Instance?.UnregisterSelectableUnit(gameObject);
            }

            if (isServer)
            {
                RpcOnDeath();
                StartCoroutine(DestroyAfterFrame());
            }
        }

        private System.Collections.IEnumerator DestroyAfterFrame()
        {
            yield return null;
            if (gameObject != null && NetworkServer.active)
                NetworkServer.Destroy(gameObject);
        }

        [ClientRpc]
        private void RpcOnDeath()
        {
            ApplyColor(Color.gray);
            if (!isServer && agent != null)
                agent.enabled = false;
        }

        private void HandleMoveCommand(Vector3 position)
        {
            if (!CanIssueLocalCommands)
            {
                Debug.LogWarning($"[Unit] {name} HandleMoveCommand blocked: CanIssueLocalCommands=false");
                return;
            }
            if (SelectionManager.Instance == null || !SelectionManager.Instance.IsUnitSelected(gameObject))
            {
                return;
            }

            RequestMove(position);
        }

        public void RequestMove(Vector3 position)
        {
            if (!CanIssueLocalCommands) return;

            Vector3 destination = GetFormationDestination(position);

            if (!NetworkClient.active || isServer)
            {
                ApplyMoveCommand(destination);
            }
            else
            {
                currentTarget = null;
                syncTargetNetId = 0;
                lastCommandTime = Time.time;
                TrySetDestination(destination);
                CmdMove(destination);
            }
        }

        [Command]
        private void CmdMove(Vector3 position)
        {
            ApplyMoveCommand(position);
            RpcConfirmMove(position);
        }

        [ClientRpc]
        private void RpcConfirmMove(Vector3 position)
        {
            if (!isServer && isOwned && agent != null && agent.enabled)
            {
                TrySetDestination(position);
            }
        }

        private void ApplyMoveCommand(Vector3 position)
        {
            currentTarget = null;
            syncTargetNetId = 0;
            lastCommandTime = Time.time;
            TrySetDestination(position);
            if (isServer)
                RpcPlayMoveSound();
        }

        [ClientRpc]
        private void RpcPlayMoveSound()
        {
            if (!isServer)
                Audio.AudioManager.Instance?.PlayUnitMove();
        }

        private void HandleAttackCommand(GameObject target)
        {
            if (!CanIssueLocalCommands) return;
            if (SelectionManager.Instance == null || !SelectionManager.Instance.IsUnitSelected(gameObject)) return;

            lastCommandTime = Time.time;

            if (!NetworkClient.active || isServer)
                ApplyAttackCommand(target);
            else
                CmdSetTarget(target);
        }

        [Command]
        private void CmdSetTarget(GameObject target)
        {
            ApplyAttackCommand(target);
        }

        private void ApplyAttackCommand(GameObject target)
        {
            if (!IsValidHostileTarget(target)) return;
            lastCommandTime = Time.time;
            SetTarget(target);
        }

        public void SetTarget(GameObject target)
        {
            if (NetworkServer.active && !IsValidHostileTarget(target)) return;

            currentTarget = target;
            syncTargetNetId = target != null ? target.GetComponent<NetworkIdentity>()?.netId ?? 0 : 0;

            if (target != null)
            {
                TrySetDestination(target.transform.position);
            }
        }

        public void ClearTarget()
        {
            currentTarget = null;
            syncTargetNetId = 0;
            if (agent != null && agent.enabled)
                agent.isStopped = false;
            syncIsStopped = false;
        }

        private bool TrySetDestination(Vector3 destination)
        {
            if (agent == null) return false;
            if (!agent.enabled) return false;

            if (!agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit snapHit, 5f, NavMesh.AllAreas))
                {
                    agent.Warp(snapHit.position);
                }
                else
                {
                    return false;
                }
            }

            if (!NavMesh.SamplePosition(destination, out NavMeshHit destinationHit, 1.5f, agent.areaMask))
                return false;

            agent.isStopped = false;
            syncIsStopped = false;
            syncDestination = destinationHit.position;
            return agent.SetDestination(destinationHit.position);
        }

        private Vector3 GetFormationDestination(Vector3 center)
        {
            SelectionManager selection = SelectionManager.Instance;
            int count = selection != null ? selection.SelectedCount : 1;
            if (count <= 1)
                return center;

            int index = selection.GetUnitIndex(gameObject);
            if (index < 0) return center;

            float spacing = 1.2f;

            if (count <= 3)
            {
                float angle = (index * 120f) * Mathf.Deg2Rad;
                float r = spacing * 0.5f;
                return center + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
            }
            else if (count <= 6)
            {
                float angle = (index * 60f) * Mathf.Deg2Rad;
                float r = spacing * 0.65f;
                return center + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
            }
            else
            {
                float angle = (index * 137.508f) * Mathf.Deg2Rad;
                float r = spacing * Mathf.Sqrt(index + 1) * 0.4f;
                return center + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
            }
        }

        private void UpdateTeamColor()
        {
            ApplyColor(isEnemy ? enemyColor : friendlyColor);
        }

        private void OnTeamChanged(bool oldValue, bool newValue)
        {
            if (agent != null)
                agent.avoidancePriority = newValue ? 50 : 10;
            UpdateTeamColor();
            ApplyWorldArt();
        }

        private void OnArtIdChanged(string oldValue, string newValue)
        {
            ApplyWorldArt();
        }

        private void OnSyncTargetChanged(uint oldValue, uint newValue)
        {
            if (isServer) return;

            if (newValue == 0)
            {
                currentTarget = null;
                return;
            }

            var allObjects = FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.netId == newValue)
                {
                    currentTarget = obj.gameObject;
                    return;
                }
            }
        }

        private void OnSyncIsStoppedChanged(bool oldValue, bool newValue)
        {
            if (isServer) return;
            if (agent != null && agent.enabled)
                agent.isStopped = newValue;
        }

        private void ApplyWorldArt()
        {
            worldVisual ??= GetComponent<global::RealmCommander.Visuals.WorldModelVisual>();
            if (worldVisual == null)
                worldVisual = gameObject.AddComponent<global::RealmCommander.Visuals.WorldModelVisual>();

            worldVisual.ApplyUnit(artId, isEnemy);
        }

        private void EnsureHealthBar()
        {
            healthBar ??= GetComponent<global::RealmCommander.Visuals.WorldHealthBar>();
            if (healthBar == null)
                healthBar = gameObject.AddComponent<global::RealmCommander.Visuals.WorldHealthBar>();
            healthBar.SetLayout(new Vector3(0f, 1.25f, 0f), new Vector2(0.85f, 0.075f));
        }

        private void ApplyColor()
        {
            ApplyColor(isSelected ? selectedColor
                : isEnemy ? enemyColor : friendlyColor);
        }

        private void ApplyColor(Color color)
        {
            if (unitRenderer == null) return;
            if (colorBlock == null)
                colorBlock = new MaterialPropertyBlock();
            colorBlock.SetColor("_Color", color);
            unitRenderer.SetPropertyBlock(colorBlock);
        }

        private bool IsValidHostileTarget(GameObject target)
        {
            if (target == null || target == gameObject) return false;

            Unit targetUnit = target.GetComponent<Unit>();
            if (targetUnit != null)
                return targetUnit.IsAlive && targetUnit.IsEnemy != isEnemy;

            Building targetBuilding = target.GetComponent<Building>();
            if (targetBuilding != null)
                return targetBuilding.IsAlive && targetBuilding.TeamId == (isEnemy ? 0 : 1);

            return false;
        }

        [Server]
        public void ApplyDifficultyMultiplier(float healthMult, float damageMult, float speedMult)
        {
            maxHealth *= healthMult;
            attackDamage *= damageMult;
            moveSpeed *= speedMult;
            currentHealth = maxHealth;
            syncMaxHealth = maxHealth;

            if (agent != null)
                agent.speed = moveSpeed;

            ApplyWorldArt();
        }

        private void OnMouseDown()
        {
            if (!CanIssueLocalCommands || SelectionManager.Instance == null) return;
            if (RTS.BoxSelector.WasClickHandled) return;

            bool additive = Input.GetKey(KeyCode.LeftShift) || MobileRTSInput.AdditiveSelectionActive;

            if (additive)
            {
                if (isSelected)
                    SelectionManager.Instance.RemoveFromSelection(gameObject);
                else
                    SelectionManager.Instance.AddToSelection(gameObject);
            }
            else
            {
                SelectionManager.Instance.SelectUnit(gameObject);
            }
        }
    }
}
