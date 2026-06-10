using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RealmCommander.RPG;

namespace RealmCommander.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Inventory inventory;
        [SerializeField] private Transform inventoryContent;
        [SerializeField] private InventorySlotUI slotPrefab;
        [SerializeField] private Transform equipmentContent;
        [SerializeField] private GameObject inventoryPanel;

        private InventorySlotUI[] inventorySlots;
        private InventorySlotUI[] equipmentSlots;

        private void Start()
        {
            if (inventory != null)
            {
                inventory.OnInventoryChanged += RefreshUI;
            }

            if (inventory != null)
            {
                CreateInventorySlots();
                RefreshUI();
            }
        }

        private void OnDestroy()
        {
            if (inventory != null)
            {
                inventory.OnInventoryChanged -= RefreshUI;
            }
        }

        private void CreateInventorySlots()
        {
            if (inventoryContent == null || slotPrefab == null) return;

            inventorySlots = new InventorySlotUI[inventory.Items.Count];
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                var slot = Instantiate(slotPrefab, inventoryContent);
                int index = i;
                slot.OnClicked += () => OnInventorySlotClicked(index);
                inventorySlots[i] = slot;
            }

            if (equipmentContent != null)
            {
                equipmentSlots = new InventorySlotUI[inventory.Equipment.Count];
                for (int i = 0; i < inventory.Equipment.Count; i++)
                {
                    var slot = Instantiate(slotPrefab, equipmentContent);
                    int index = i;
                    slot.OnClicked += () => OnEquipmentSlotClicked(index);
                    equipmentSlots[i] = slot;
                }
            }
        }

        private void RefreshUI()
        {
            if (inventory == null) return;

            for (int i = 0; i < inventorySlots.Length && i < inventory.Items.Count; i++)
            {
                inventorySlots[i]?.UpdateSlot(inventory.Items[i]);
            }

            if (equipmentSlots != null)
            {
                for (int i = 0; i < equipmentSlots.Length && i < inventory.Equipment.Count; i++)
                {
                    equipmentSlots[i]?.UpdateSlot(inventory.Equipment[i]);
                }
            }
        }

        private void OnInventorySlotClicked(int index)
        {
            if (inventory == null) return;

            var slot = inventory.Items[index];
            if (!slot.IsEmpty)
            {
                if (slot.item.itemType == ItemType.Consumable)
                {
                    UseConsumable(slot.item);
                    inventory.RemoveItem(slot.item.itemName, 1);
                }
                else
                {
                    inventory.EquipItem(index);
                }
            }
        }

        private void OnEquipmentSlotClicked(int index)
        {
            inventory?.UnequipItem(index);
        }

        private void UseConsumable(ItemData item)
        {
            Debug.Log($"Used {item.itemName}");
        }

        public void TogglePanel()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);
            }
        }
    }

    public class InventorySlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Button button;

        public event System.Action OnClicked;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(() => OnClicked?.Invoke());
            }
        }

        public void UpdateSlot(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = null;
                    iconImage.gameObject.SetActive(false);
                }
                if (quantityText != null)
                {
                    quantityText.text = "";
                }
            }
            else
            {
                if (iconImage != null)
                {
                    iconImage.sprite = slot.item.icon;
                    iconImage.gameObject.SetActive(true);
                }
                if (quantityText != null)
                {
                    quantityText.text = slot.quantity > 1 ? slot.quantity.ToString() : "";
                }
            }
        }
    }
}
