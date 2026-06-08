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

        private void Start()
        {
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnSelectionChanged += UpdateSelectionUI;
            }

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnGoldChanged += UpdateResourceUI;
                ResourceManager.Instance.OnManaChanged += UpdateResourceUI;
            }

            UpdateResourceUI(0, 0);
            UpdateSelectionUI(new List<GameObject>());
        }

        private void OnDestroy()
        {
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnSelectionChanged -= UpdateSelectionUI;
            }

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnGoldChanged -= UpdateResourceUI;
                ResourceManager.Instance.OnManaChanged -= UpdateResourceUI;
            }
        }

        private void Update()
        {
            UpdateGameSpeedUI();
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

        private void UpdateSelectionUI(List<GameObject> selected)
        {
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
                    if (healthBar != null)
                    {
                        healthBar.value = unit.HealthPercent;
                    }

                    if (unitInfoText != null)
                    {
                        unitInfoText.text = $"HP: {Mathf.FloorToInt(unit.CurrentHealth)}/{Mathf.FloorToInt(unit.MaxHealth)}";
                    }
                }
            }
        }

        private void UpdateGameSpeedUI()
        {
            if (gameSpeedText != null && GameManager.Instance != null)
            {
                gameSpeedText.text = $"Speed: {GameManager.Instance.GameSpeed:F1}x";
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
