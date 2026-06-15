using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RealmCommander.Core;
using RealmCommander.RTS;

namespace RealmCommander.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Resource UI")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI manaText;

        [Header("Selection UI")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private TextMeshProUGUI selectionCountText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI unitInfoText;

        [Header("Game UI")]
        [SerializeField] private TextMeshProUGUI gameSpeedText;
        [SerializeField] private GameObject pausePanel;

        private Unit observedUnit;
        private Building observedBuilding;

        private void Awake()
        {
            AutoWireReferences();
            PolishLayout();
        }

        private void Start()
        {
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnSelectionChanged += UpdateSelectionUI;
            }

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnGoldChangedEvent += UpdateResourceUI;
                ResourceManager.Instance.OnManaChangedEvent += UpdateResourceUI;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameSpeedChanged += UpdateGameSpeedUI;
            }

            UpdateResourceUI(0, 0);
            UpdateSelectionUI(new GameObject[0]);
            UpdateGameSpeedUI(GameManager.Instance?.GameSpeed ?? 1f);
        }

        private void AutoWireReferences()
        {
            goldText ??= FindComponentInChildren<TextMeshProUGUI>("GoldText");
            manaText ??= FindComponentInChildren<TextMeshProUGUI>("ManaText");
            gameSpeedText ??= FindComponentInChildren<TextMeshProUGUI>("SpeedText");

            selectionPanel ??= GameObject.Find("Selection_Panel");
            if (selectionPanel != null)
            {
                selectionCountText ??= FindComponentInChildren<TextMeshProUGUI>(selectionPanel.transform, "SelectionCountText");
                healthBar ??= FindComponentInChildren<Slider>(selectionPanel.transform, "HealthBar");
                unitInfoText ??= FindComponentInChildren<TextMeshProUGUI>(selectionPanel.transform, "UnitInfoText");
            }

            WireButton("PauseButton", OnPauseButton);
            WireButton("SpeedUpButton", OnSpeedUpButton);
            WireButton("SpeedDownButton", OnSpeedDownButton);
        }

        private T FindComponentInChildren<T>(string objectName) where T : Component
        {
            return FindComponentInChildren<T>(transform, objectName);
        }

        private static T FindComponentInChildren<T>(Transform root, string objectName) where T : Component
        {
            if (root == null) return null;
            foreach (T component in root.GetComponentsInChildren<T>(true))
            {
                if (component != null && component.gameObject.name == objectName)
                    return component;
            }

            return null;
        }

        private void WireButton(string objectName, UnityEngine.Events.UnityAction action)
        {
            Button button = FindComponentInChildren<Button>(objectName);
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void PolishLayout()
        {
            Image panel = GetComponent<Image>();
            if (panel != null)
                panel.color = new Color(0.06f, 0.08f, 0.1f, 0.42f);

            SetText("PauseButton", "Pause");
            SetText("SpeedUpButton", "+");
            SetText("SpeedDownButton", "-");

            if (goldText != null) goldText.fontSize = 18f;
            if (manaText != null) manaText.fontSize = 18f;
            if (gameSpeedText != null) gameSpeedText.fontSize = 16f;
        }

        private void SetText(string objectName, string value)
        {
            TextMeshProUGUI text = FindComponentInChildren<TextMeshProUGUI>(GameObject.Find(objectName)?.transform, "Text");
            if (text != null)
                text.text = value;
        }

        private void OnDestroy()
        {
            StopObservingSelection();
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnSelectionChanged -= UpdateSelectionUI;
            }

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnGoldChangedEvent -= UpdateResourceUI;
                ResourceManager.Instance.OnManaChangedEvent -= UpdateResourceUI;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameSpeedChanged -= UpdateGameSpeedUI;
            }
        }

        private void UpdateResourceUI(float current, float change)
        {
            if (ResourceManager.Instance == null) return;

            if (goldText != null)
            {
                goldText.text = $"Gold: {Mathf.FloorToInt(ResourceManager.Instance.CurrentGold)}";
            }

            if (manaText != null)
            {
                manaText.text = $"Mana: {Mathf.FloorToInt(ResourceManager.Instance.CurrentMana)}/{Mathf.FloorToInt(ResourceManager.Instance.MaxMana)}";
            }
        }

        private void UpdateSelectionUI(IReadOnlyList<GameObject> selected)
        {
            StopObservingSelection();
            if (selectionPanel == null) return;

            if (selected == null || selected.Count == 0)
            {
                selectionPanel.SetActive(false);
                return;
            }

            selectionPanel.SetActive(true);

            if (selectionCountText != null)
            {
                selectionCountText.text = $"Selected: {selected.Count}";
            }

            if (selected.Count == 1 && selected[0] != null)
            {
                var unit = selected[0].GetComponent<RTS.Unit>();
                if (unit != null)
                {
                    observedUnit = unit;
                    observedUnit.OnHealthChangedEvent += UpdateObservedHealth;
                    UpdateUnitDisplay(unit);
                    return;
                }

                var building = selected[0].GetComponent<Building>();
                if (building != null)
                {
                    observedBuilding = building;
                    observedBuilding.OnHealthChangedEvent += UpdateObservedHealth;
                    UpdateObservedHealth(building.CurrentHealth, building.MaxHealth);
                }
            }
        }

        private void UpdateObservedHealth(float current, float max)
        {
            if (healthBar != null)
                healthBar.value = max > 0f ? current / max : 0f;
            if (unitInfoText != null && observedUnit != null)
                UpdateUnitDisplay(observedUnit);
            else if (unitInfoText != null)
                unitInfoText.text = $"HP: {Mathf.FloorToInt(current)}/{Mathf.FloorToInt(max)}";
        }

        private void UpdateUnitDisplay(RTS.Unit unit)
        {
            if (unitInfoText == null) return;
            if (healthBar != null)
                healthBar.value = unit.MaxHealth > 0f ? unit.CurrentHealth / unit.MaxHealth : 0f;
            unitInfoText.text = $"{unit.SpecDisplayName}\n" +
                $"HP: {Mathf.FloorToInt(unit.CurrentHealth)}/{Mathf.FloorToInt(unit.MaxHealth)}\n" +
                $"ATK: {unit.AttackDamage}  SPD: {unit.AttackSpeed:F1}\n" +
                $"Range: {unit.AttackRange}  Move: {unit.MoveSpeed}";
        }

        private void StopObservingSelection()
        {
            if (observedUnit != null)
                observedUnit.OnHealthChangedEvent -= UpdateObservedHealth;
            if (observedBuilding != null)
                observedBuilding.OnHealthChangedEvent -= UpdateObservedHealth;
            observedUnit = null;
            observedBuilding = null;
        }

        private void UpdateGameSpeedUI(float speed)
        {
            if (gameSpeedText != null)
            {
                gameSpeedText.text = $"Speed: {speed:F1}x";
            }
        }

        public void OnSpeedUpButton()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameSpeed(GameManager.Instance.GameSpeed + 0.5f);
            }
        }

        public void OnSpeedDownButton()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameSpeed(GameManager.Instance.GameSpeed - 0.5f);
            }
        }

        public void OnPauseButton()
        {
            if (GameManager.Instance == null) return;

            if (GameManager.Instance.IsPaused)
            {
                GameManager.Instance.ResumeGame();
                if (pausePanel != null) pausePanel.SetActive(false);
            }
            else
            {
                GameManager.Instance.PauseGame();
                if (pausePanel != null) pausePanel.SetActive(true);
            }
        }
    }
}
