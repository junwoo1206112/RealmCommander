using System;
using UnityEngine;
using UnityEngine.AI;
using RealmCommander.Core;

namespace RealmCommander.RTS
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Unit : MonoBehaviour
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

        private NavMeshAgent agent;
        private float currentHealth;
        private float lastAttackTime;
        private GameObject currentTarget;
        private bool isSelected;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercent => currentHealth / maxHealth;
        public bool IsEnemy => isEnemy;
        public bool IsAlive => currentHealth > 0;
        public bool IsSelected => isSelected;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;
        public event Action OnSelected;
        public event Action OnDeselected;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
            currentHealth = maxHealth;

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }

            UpdateTeamColor();
        }

        private void Start()
        {
            SelectionManager.Instance?.RegisterSelectableUnit(gameObject);

            CommandManager.Instance.OnMoveCommand += HandleMoveCommand;
            CommandManager.Instance.OnAttackCommand += HandleAttackCommand;
        }

        private void OnDestroy()
        {
            SelectionManager.Instance?.UnregisterSelectableUnit(gameObject);

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

        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void TryAttack()
        {
            if (Time.time - lastAttackTime >= attackSpeed)
            {
                lastAttackTime = Time.time;
                var targetUnit = currentTarget.GetComponent<Unit>();
                if (targetUnit != null)
                {
                    targetUnit.TakeDamage(attackDamage);
                }
            }
        }

        private void Die()
        {
            agent.isStopped = true;
            agent.enabled = false;
            OnDeath?.Invoke();

            SelectionManager.Instance?.UnregisterSelectableUnit(gameObject);

            if (unitRenderer != null)
            {
                unitRenderer.material.color = Color.gray;
            }
        }

        private void HandleMoveCommand(Vector3 position)
        {
            if (!SelectionManager.Instance.IsUnitSelected(gameObject)) return;

            ClearTarget();
            agent.isStopped = false;
            agent.SetDestination(position);
        }

        private void HandleAttackCommand(GameObject target)
        {
            if (!SelectionManager.Instance.IsUnitSelected(gameObject)) return;

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
