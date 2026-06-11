using System;
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

        [SyncVar(hook = nameof(OnHealthChanged))]
        private float currentHealth;
        private NavMeshAgent agent;
        private float lastAttackTime;
        private GameObject currentTarget;
        private bool isSelected;
        private MaterialPropertyBlock colorBlock;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercent => currentHealth / maxHealth;
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
                agent.stoppingDistance = Mathf.Min(attackRange * 0.5f, 0.5f);
                agent.acceleration = 16f;
                agent.angularSpeed = 480f;
                agent.autoBraking = false;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                agent.avoidancePriority = 0;
            }

            currentHealth = maxHealth;

            if (selectionIndicator == null)
            {
                var indicator = gameObject.AddComponent<SelectionIndicator>();
                selectionIndicator = indicator.gameObject;
                selectionIndicator.SetActive(false);
            }

            UpdateTeamColor();
        }

        protected override void OnValidate() { }

        public override void OnStartServer()
        {
            base.OnStartServer();
            currentHealth = maxHealth;
            if (agent != null)
            {
                agent.enabled = true;
                agent.speed = moveSpeed;
                agent.avoidancePriority = isEnemy ? 50 : 0;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!isServer && agent != null)
                agent.enabled = false;
        }

        private void Start()
        {
            if (CanIssueLocalCommands)
            {
                SelectionManager.Instance?.RegisterSelectableUnit(gameObject);
                if (CommandManager.Instance != null)
                {
                    CommandManager.Instance.OnMoveCommand += HandleMoveCommand;
                    CommandManager.Instance.OnAttackCommand += HandleAttackCommand;
                }
            }

        }

        private void OnDestroy()
        {
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance?.UnregisterSelectableUnit(gameObject);
            }

            if (CommandManager.Instance != null)
            {
                CommandManager.Instance.OnMoveCommand -= HandleMoveCommand;
                CommandManager.Instance.OnAttackCommand -= HandleAttackCommand;
            }
        }

        private float lastAcquireTime;
        private float lastCommandTime;
        private float lastPathTime;
        private static readonly float AcquireInterval = 0.5f;
        private static readonly float AcquireMoveGrace = 1.5f;
        private static readonly float DetectRangeMultiplier = 2.5f;
        private static readonly float MinPathInterval = 0.15f;
        private Collider[] acquireBuffer = new Collider[32];
        private Collider[] pushBuffer = new Collider[8];

        private void Update()
        {
            if (!IsAlive) return;
            if (NetworkClient.active && !isServer) return;

            if (currentTarget != null)
            {
                if (IsValidHostileTarget(currentTarget))
                {
                    float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

                    if (distance <= attackRange)
                    {
                        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                        agent.isStopped = true;
                        TryAttack();
                    }
                    else
                    {
                        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                        bool wasStopped = agent.isStopped;
                        agent.isStopped = false;
                        if (wasStopped || (Time.time - lastPathTime >= MinPathInterval && (!agent.hasPath || agent.remainingDistance < 0.5f || Vector3.Distance(agent.destination, currentTarget.transform.position) > 1f)))
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
                AutoAcquireTarget();
            }

            PushNearbyUnits();
        }

        private void PushNearbyUnits()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

            float pushRadius = agent.radius * 2f;
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, pushRadius, pushBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                if (pushBuffer[i].gameObject == gameObject) continue;

                Unit other = pushBuffer[i].GetComponent<Unit>();
                if (other == null || !other.IsAlive) continue;

                Vector3 away = transform.position - pushBuffer[i].transform.position;
                away.y = 0f;
                float dist = away.magnitude;
                float otherRadius = other.agent != null ? other.agent.radius : agent.radius;
                float minDist = agent.radius + otherRadius;

                if (dist < minDist && dist > 0.001f)
                {
                    float pushAmount = (minDist - dist) * 0.5f;
                    agent.Move(away.normalized * pushAmount);
                }
            }
        }

        private void AutoAcquireTarget()
        {
            if (Time.time - lastAcquireTime < AcquireInterval) return;
            lastAcquireTime = Time.time;

            if (Time.time - lastCommandTime < AcquireMoveGrace) return;
            if (agent.pathPending) return;
            if (agent.hasPath && agent.remainingDistance > attackRange && agent.remainingDistance < float.MaxValue) return;

            float detectRange = attackRange * DetectRangeMultiplier;

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectRange, acquireBuffer);
            GameObject nearest = null;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Unit targetUnit = acquireBuffer[i].GetComponent<Unit>();
                if (targetUnit == null) continue;
                if (!IsValidHostileTarget(targetUnit.gameObject)) continue;
                if (!targetUnit.IsAlive) continue;

                float dist = Vector3.Distance(transform.position, targetUnit.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = targetUnit.gameObject;
                }
            }

            if (nearest != null)
            {
                currentTarget = nearest;
                TrySetDestination(nearest.transform.position);
            }
        }

        public void SetSelected(bool selected)
        {
            if (selected == isSelected) return;
            isSelected = selected;
            if (selectionIndicator != null)
            {
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
                agent.avoidancePriority = enemyTeam ? 50 : 0;
            UpdateTeamColor();
        }

        private void OnHealthChanged(float oldValue, float newValue)
        {
            OnHealthChangedEvent?.Invoke(newValue, maxHealth);
        }

        private void TryAttack()
        {
            if (Time.time - lastAttackTime >= attackSpeed)
            {
                if (currentTarget == null) return;
                lastAttackTime = Time.time;
                var combat = Network.CombatManager.Instance;
                if (combat != null)
                {
                    combat.ApplyCombatDamage(gameObject, currentTarget, attackDamage);
                }
                else
                {
                    Unit targetUnit = currentTarget.GetComponent<Unit>();
                    if (targetUnit != null)
                    {
                        targetUnit.TakeDamage(attackDamage);
                        return;
                    }

                    currentTarget.GetComponent<Building>()?.TakeDamage(attackDamage);
                }
            }
        }

        private void Die()
        {
            agent.isStopped = true;
            agent.enabled = false;
            OnDeath?.Invoke();

            if (isServer || isOwned || !NetworkServer.active)
            {
                SelectionManager.Instance?.UnregisterSelectableUnit(gameObject);
            }

            if (isServer)
            {
                RpcOnDeath();
                NetworkServer.Destroy(gameObject);
            }
        }

        [ClientRpc]
        private void RpcOnDeath()
        {
            ApplyColor(Color.gray);
        }

        private void HandleMoveCommand(Vector3 position)
        {
            if (!CanIssueLocalCommands) return;
            if (SelectionManager.Instance == null || !SelectionManager.Instance.IsUnitSelected(gameObject)) return;

            RequestMove(position);
        }

        public void RequestMove(Vector3 position)
        {
            if (!CanIssueLocalCommands) return;

            Vector3 destination = GetFormationDestination(position);

            if (isServer || !NetworkClient.active)
                ApplyMoveCommand(destination);
            else
                CmdMove(destination);
        }

        [Command]
        private void CmdMove(Vector3 position)
        {
            ApplyMoveCommand(position);
        }

        private void ApplyMoveCommand(Vector3 position)
        {
            if (currentTarget != null)
            {
                currentTarget = null;
            }
            lastCommandTime = Time.time;
            TrySetDestination(position);
        }

        private void HandleAttackCommand(GameObject target)
        {
            if (!CanIssueLocalCommands) return;
            if (SelectionManager.Instance == null || !SelectionManager.Instance.IsUnitSelected(gameObject)) return;

            lastCommandTime = Time.time;

            if (isServer || !NetworkClient.active)
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
            if (target != null)
            {
                TrySetDestination(target.transform.position);
            }
        }

        public void ClearTarget()
        {
            currentTarget = null;
            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        private bool TrySetDestination(Vector3 destination)
        {
            if (agent == null) return false;
            if (!agent.enabled) return false;
            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning($"[Unit] {name} not on NavMesh, can't move", this);
                return false;
            }

            if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, agent.areaMask))
            {
                Debug.LogWarning($"[Unit] {name} could not find NavMesh near {destination}", this);
                return false;
            }

            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.isStopped = false;
            bool success = agent.SetDestination(hit.position);
            if (!success)
                Debug.LogWarning($"[Unit] {name} SetDestination failed to {hit.position}", this);
            return success;
        }

        private Vector3 GetFormationDestination(Vector3 center)
        {
            SelectionManager selection = SelectionManager.Instance;
            int count = selection != null ? selection.SelectedCount : 1;
            if (count <= 1)
                return center;

            int index = selection.GetUnitIndex(gameObject);
            if (index < 0) return center;

            bool centerOnNavMesh = NavMesh.SamplePosition(center, out _, agent != null ? Mathf.Max(agent.radius, 1f) : 1f, agent != null ? agent.areaMask : NavMesh.AllAreas);
            if (!centerOnNavMesh)
                return center;

            int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt(count / (float)columns);
            float spacing = Mathf.Max(1.25f, agent != null ? agent.radius * 2.5f : 1.25f);

            float totalWidth = (columns - 1) * spacing;
            float totalDepth = (rows - 1) * spacing;
            float maxFormation = 12f;
            float scale = 1f;
            if (totalWidth > maxFormation || totalDepth > maxFormation)
                scale = maxFormation / Mathf.Max(totalWidth, totalDepth);

            float x = (index % columns - (columns - 1) * 0.5f) * spacing * scale;
            float z = (index / columns - (rows - 1) * 0.5f) * spacing * scale;
            return center + new Vector3(x, 0f, z);
        }

        private void UpdateTeamColor()
        {
            ApplyColor(isEnemy ? enemyColor : friendlyColor);
        }

        private void OnTeamChanged(bool oldValue, bool newValue)
        {
            if (agent != null)
                agent.avoidancePriority = newValue ? 50 : 0;
            UpdateTeamColor();
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
                return targetBuilding.IsAlive && (targetBuilding.tag == "Enemy") != isEnemy;

            return false;
        }

        private void OnMouseDown()
        {
            if (!CanIssueLocalCommands || SelectionManager.Instance == null) return;
            if (RTS.BoxSelector.WasClickHandled) return;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (isSelected)
                {
                    SelectionManager.Instance.RemoveFromSelection(gameObject);
                }
                else
                {
                    SelectionManager.Instance.AddToSelection(gameObject);
                }
            }
            else
            {
                SelectionManager.Instance.SelectUnit(gameObject);
            }
        }
    }
}
