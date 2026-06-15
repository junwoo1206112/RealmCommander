using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RealmCommander.RTS;
using RealmCommander.Core;

namespace RealmCommander.UI
{
    public class BuildingUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Building selectedBuilding;
        [SerializeField] private GameObject buildingPanel;
        [SerializeField] private TextMeshProUGUI buildingNameText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Production")]
        [SerializeField] private Transform productionContent;
        [SerializeField] private ProductionButtonUI productionButtonPrefab;

        [Header("Production Progress")]
        [SerializeField] private GameObject productionProgressPanel;
        [SerializeField] private TextMeshProUGUI productionNameText;
        [SerializeField] private Slider productionProgressBar;
        [SerializeField] private TextMeshProUGUI productionTimeText;

        private ProductionButtonUI[] productionButtons;
        private bool isObservingBuilding;

        private void Start()
        {
            if (SelectionManager.Instance != null)
                SelectionManager.Instance.OnSelectionChanged += UpdateSelection;
            if (buildingPanel != null)
                buildingPanel.SetActive(false);
            if (productionProgressPanel != null)
                productionProgressPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            StopObservingBuilding();
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnSelectionChanged -= UpdateSelection;
            }
        }

        private void UpdateSelection(System.Collections.Generic.IReadOnlyList<GameObject> selected)
        {
            StopObservingBuilding();

            if (selected == null || selected.Count == 0)
            {
                if (buildingPanel != null) buildingPanel.SetActive(false);
                if (productionProgressPanel != null) productionProgressPanel.SetActive(false);
                selectedBuilding = null;
                ClearProductionButtons();
                return;
            }

            if (selected.Count == 1 && selected[0] != null)
            {
                var building = selected[0].GetComponent<Building>();
                if (building != null)
                {
                    selectedBuilding = building;
                    StartObservingBuilding();
                    if (buildingPanel != null) buildingPanel.SetActive(true);
                    CreateProductionButtons();
                    UpdateBuildingInfo();
                    UpdateProductionProgress();
                    return;
                }
            }

            if (buildingPanel != null) buildingPanel.SetActive(false);
            if (productionProgressPanel != null) productionProgressPanel.SetActive(false);
            selectedBuilding = null;
            ClearProductionButtons();
        }

        private void StartObservingBuilding()
        {
            if (selectedBuilding == null || isObservingBuilding) return;
            isObservingBuilding = true;
            selectedBuilding.OnHealthChangedEvent += OnBuildingHealthChanged;
            selectedBuilding.OnProductionStarted += OnProductionStarted;
            selectedBuilding.OnProductionCompleted += OnProductionCompleted;
        }

        private void StopObservingBuilding()
        {
            if (selectedBuilding != null && isObservingBuilding)
            {
                selectedBuilding.OnHealthChangedEvent -= OnBuildingHealthChanged;
                selectedBuilding.OnProductionStarted -= OnProductionStarted;
                selectedBuilding.OnProductionCompleted -= OnProductionCompleted;
            }
            isObservingBuilding = false;
        }

        private void OnBuildingHealthChanged(float current, float max)
        {
            if (healthBar != null)
                healthBar.value = max > 0f ? current / max : 0f;
            if (healthText != null)
                healthText.text = $"HP: {Mathf.FloorToInt(current)}/{Mathf.FloorToInt(max)}";
        }

        private void OnProductionStarted(UnitProductionData data)
        {
            UpdateProductionProgress();
        }

        private void OnProductionCompleted(UnitProductionData data)
        {
            UpdateProductionProgress();
        }

        private void UpdateBuildingInfo()
        {
            if (selectedBuilding == null) return;

            if (buildingNameText != null)
                buildingNameText.text = selectedBuilding.BuildingName;

            OnBuildingHealthChanged(selectedBuilding.CurrentHealth, selectedBuilding.MaxHealth);
        }

        private void UpdateProductionProgress()
        {
            if (selectedBuilding == null || productionProgressPanel == null) return;

            if (selectedBuilding.IsProducing)
            {
                productionProgressPanel.SetActive(true);

                if (productionNameText != null)
                {
                    string productName = selectedBuilding.GetCurrentProductName();
                    productionNameText.text = string.IsNullOrEmpty(productName) ? "Producing..." : $"Producing: {productName}";
                }

                if (productionProgressBar != null)
                {
                    float progress = selectedBuilding.GetProductionProgress();
                    productionProgressBar.value = progress;
                }

                if (productionTimeText != null)
                {
                    float remaining = selectedBuilding.GetProductionTimeRemaining();
                    productionTimeText.text = $"{remaining:F1}s";
                }
            }
            else
            {
                productionProgressPanel.SetActive(false);
            }
        }

        private void CreateProductionButtons()
        {
            if (productionContent == null || productionButtonPrefab == null) return;

            ClearProductionButtons();

            var productionQueue = selectedBuilding.GetProductionQueue();
            if (productionQueue == null || productionQueue.Count == 0) return;

            productionButtons = new ProductionButtonUI[productionQueue.Count];

            for (int i = 0; i < productionQueue.Count; i++)
            {
                var button = Instantiate(productionButtonPrefab, productionContent);
                button.Setup(productionQueue[i], selectedBuilding);
                productionButtons[i] = button;
            }
        }

        private void ClearProductionButtons()
        {
            if (productionButtons == null) return;

            foreach (var button in productionButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            productionButtons = null;
        }
    }

    public class ProductionButtonUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button produceButton;

        private UnitProductionData productionData;
        private Building building;

        public void Setup(UnitProductionData data, Building building)
        {
            productionData = data;
            this.building = building;

            if (unitNameText != null)
            {
                unitNameText.text = data.unitName;
            }

            if (costText != null)
            {
                costText.text = $"Gold: {Mathf.FloorToInt(data.goldCost)}";
            }

            if (timeText != null)
            {
                timeText.text = $"{data.productionTime:F1}s";
            }

            if (iconImage != null && data.icon != null)
            {
                iconImage.sprite = data.icon;
            }
            else if (iconImage != null)
            {
                iconImage.sprite = !string.IsNullOrWhiteSpace(data.specId)
                    ? ArtAssetLookup.LoadUnitIcon(data.specId)
                    : ArtAssetLookup.LoadUnitIcon(data.unitName);
            }

            if (produceButton != null)
            {
                produceButton.onClick.AddListener(OnProduceClicked);
            }
        }

        private void OnProduceClicked()
        {
            if (building != null && productionData != null)
            {
                building.QueueProduction(productionData);
            }
        }

        private void OnDestroy()
        {
            if (produceButton != null)
                produceButton.onClick.RemoveListener(OnProduceClicked);
        }
    }
}
