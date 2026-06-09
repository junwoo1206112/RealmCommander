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

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercent => currentHealth / maxHealth;
        public float AttackRange => attackRange;
        public bool IsEnemy => isEnemy;
        public bool IsAlive => currentHealth > 0;
        public bool IsSelected => isSelected;

        public event Action<float, float> OnHealthChangedEvent;
        public event Action OnDeath;
        public event Action OnSelected;
        public event Action OnDeselected;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;

            if (isServer)
            {
                currentHealth = maxHealth;
            }

            // SelectionIndicator가 없으면 자동 생성
            if (selectionIndicator == null)
            {
                var indicator = gameObject.AddComponent<SelectionIndicator>();
                selectionIndicator = indicator.gameObject;
            }

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }

            UpdateTeamColor();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            currentHealth = maxHealth;
        }

        private void Start()
        {
            // 네트워크 소유권이 있거나 서버가 비활성일 때 (싱글플레이어)
            if (isOwned || !NetworkServer.active)
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

        private void Update()
        {
            if (!IsAlive) return;

            if (currentTarget != null)
            {
                var targetUnit = currentTarget.GetComponent<Unit>();
                if (targetUnit != null && targetUnit.IsAlive)
                {
                    float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

                    if (distance <= attackRange)
                    {
                        agent.isStopped = true;
                        TryAttack();
                    }
                    else
                    {
                        agent.isStopped = false;
                        agent.SetDestination(currentTarget.transform.position);
                    }
                }
                else
                {
                    ClearTarget();
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

            if (selected)
            {
                OnSelected?.Invoke();
            }
            else
            {
                OnDeselected?.Invoke();
            }
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

        private void OnHealthChanged(float oldValue, float newValue)
        {
            OnHealthChangedEvent?.Invoke(newValue, maxHealth);
        }

        private void TryAttack()
        {
            if (Time.time - lastAttackTime >= attackSpeed)
            {
                lastAttackTime = Time.time;
                var targetUnit = currentTarget.GetComponent<Unit>();
                if (targetUnit != null)
                {
                    if (isServer)
                    {
                        targetUnit.TakeDamage(attackDamage);
                    }
                    else
                    {
                        CmdRequestAttack(currentTarget);
                    }
                }
            }
        }

        [Command]
        private void CmdRequestAttack(GameObject target)
        {
            var targetUnit = target.GetComponent<Unit>();
            if (targetUnit != null)
            {
                var combat = Network.CombatManager.Instance;
                if (combat != null)
                {
                    combat.ApplyCombatDamage(gameObject, target, attackDamage);
                }
            }
        }

        private void Die()
        {
            agent.isStopped = true;
            agent.enabled = false;
            OnDeath?.Invoke();

            // 네트워크 소유권이 있거나 서버가 비활성일 때 (싱글플레이어)
            if (isOwned || !NetworkServer.active)
            {
                SelectionManager.Instance?.UnregisterSelectableUnit(gameObject);
            }

            if (unitRenderer != null)
            {
                unitRenderer.material.color = Color.gray;
            }

            if (isServer)
            {
                RpcOnDeath();
            }
        }

        [ClientRpc]
        private void RpcOnDeath()
        {
            if (unitRenderer != null)
            {
                unitRenderer.material.color = Color.gray;
            }
        }

        private void HandleMoveCommand(Vector3 position)
        {
            if (SelectionManager.Instance == null || !SelectionManager.Instance.IsUnitSelected(gameObject)) return;

            ClearTarget();
            if (isServer)
            {
                agent.isStopped = false;
                agent.SetDestination(position);
            }
            else
            {
                CmdMove(position);
            }
        }

        [Command]
        private void CmdMove(Vector3 position)
        {
            agent.isStopped = false;
            agent.SetDestination(position);
        }

        private void HandleAttackCommand(GameObject target)
        {
            if (SelectionManager.Instance == null || !SelectionManager.Instance.IsUnitSelected(gameObject)) return;

            SetTarget(target);
        }

        public void SetTarget(GameObject target)
        {
            currentTarget = target;
            if (target != null)
            {
                agent.isStopped = false;
                agent.SetDestination(target.transform.position);
            }
        }

        public void ClearTarget()
        {
            currentTarget = null;
        }

        private void UpdateTeamColor()
        {
            if (unitRenderer != null)
            {
                unitRenderer.material.color = isEnemy ? enemyColor : friendlyColor;
            }
        }

        private void OnMouseDown()
        {
            // 네트워크 소유권이 있거나 서버가 비활성일 때 (싱글플레이어)
            if (!isOwned && NetworkServer.active) return;

            if (Input.GetMouseButtonUp(0))
            {
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
}
