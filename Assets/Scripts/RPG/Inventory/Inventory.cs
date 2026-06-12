using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealmCommander.RPG
{
    [Serializable]
    public class ItemData
    {
        public string itemName;
        public string description;
        public Sprite icon;
        public ItemType itemType;
        public int maxStack = 1;

        public float healthBonus;
        public float manaBonus;
        public float attackBonus;
        public float defenseBonus;
    }

    public enum ItemType
    {
        Weapon,
        Armor,
        Accessory,
        Consumable,
        Material
    }

    [Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int quantity;

        public bool IsEmpty => item == null || quantity <= 0;
    }

    [AddComponentMenu("Realm Commander/Prototype/Inventory")]
    public class Inventory : MonoBehaviour
    {
        public const string ScopeLabel = "Prototype - no persistence or production economy integration";
        [Header("Inventory Settings")]
        [SerializeField] private int inventorySize = 20;
        [SerializeField] private int equipmentSlots = 3;

        private List<InventorySlot> items = new List<InventorySlot>();
        private InventorySlot[] equipment;

        public IReadOnlyList<InventorySlot> Items => items;
        public IReadOnlyList<InventorySlot> Equipment => equipment;

        public event Action OnInventoryChanged;

        private void Awake()
        {
            inventorySize = Mathf.Max(1, inventorySize);
            equipmentSlots = Mathf.Max(3, equipmentSlots);
            equipment = new InventorySlot[equipmentSlots];
            for (int i = 0; i < inventorySize; i++)
            {
                items.Add(new InventorySlot());
            }

            for (int i = 0; i < equipmentSlots; i++)
            {
                equipment[i] = new InventorySlot();
            }
        }

        public bool AddItem(ItemData item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;

            int remaining = quantity;

            if (item.maxStack > 1)
            {
                foreach (var slot in items)
                {
                    if (!slot.IsEmpty && slot.item == item && slot.quantity < item.maxStack)
                    {
                        int added = Mathf.Min(remaining, item.maxStack - slot.quantity);
                        slot.quantity += added;
                        remaining -= added;
                        if (remaining == 0) break;
                    }
                }
            }

            while (remaining > 0)
            {
                InventorySlot empty = items.Find(slot => slot.IsEmpty);
                if (empty == null) break;
                empty.item = item;
                empty.quantity = Mathf.Min(remaining, Mathf.Max(1, item.maxStack));
                remaining -= empty.quantity;
            }

            if (remaining != quantity)
                OnInventoryChanged?.Invoke();
            if (remaining > 0)
                Debug.Log("Inventory is full!");
            return remaining == 0;
        }

        public bool RemoveItem(string itemName, int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(itemName) || quantity <= 0) return false;
            int available = 0;
            foreach (InventorySlot slot in items)
                if (!slot.IsEmpty && slot.item.itemName == itemName) available += slot.quantity;
            if (available < quantity) return false;

            int remaining = quantity;
            foreach (var slot in items)
            {
                if (!slot.IsEmpty && slot.item.itemName == itemName)
                {
                    int removed = Mathf.Min(remaining, slot.quantity);
                    slot.quantity -= removed;
                    remaining -= removed;
                    if (slot.quantity <= 0)
                    {
                        slot.item = null;
                        slot.quantity = 0;
                    }
                    if (remaining == 0) break;
                }
            }
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool EquipItem(int inventoryIndex)
        {
            if (inventoryIndex < 0 || inventoryIndex >= items.Count) return false;

            var slot = items[inventoryIndex];
            if (slot.IsEmpty) return false;

            int equipIndex = GetEquipmentIndex(slot.item.itemType);
            if (equipIndex < 0) return false;

            if (!equipment[equipIndex].IsEmpty)
            {
                items[inventoryIndex] = equipment[equipIndex];
            }
            else
            {
                items[inventoryIndex] = new InventorySlot();
            }

            equipment[equipIndex] = slot;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool UnequipItem(int equipIndex)
        {
            if (equipIndex < 0 || equipIndex >= equipment.Length) return false;

            var slot = equipment[equipIndex];
            if (slot.IsEmpty) return false;

            foreach (var itemSlot in items)
            {
                if (itemSlot.IsEmpty)
                {
                    equipment[equipIndex] = new InventorySlot();
                    itemSlot.item = slot.item;
                    itemSlot.quantity = slot.quantity;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }

            Debug.Log("Inventory is full!");
            return false;
        }

        private int GetEquipmentIndex(ItemType type)
        {
            switch (type)
            {
                case ItemType.Weapon: return 0;
                case ItemType.Armor: return 1;
                case ItemType.Accessory: return 2;
                default: return -1;
            }
        }

        public (float health, float mana, float attack, float defense) GetEquipmentBonuses()
        {
            float health = 0, mana = 0, attack = 0, defense = 0;

            foreach (var slot in equipment)
            {
                if (!slot.IsEmpty)
                {
                    health += slot.item.healthBonus;
                    mana += slot.item.manaBonus;
                    attack += slot.item.attackBonus;
                    defense += slot.item.defenseBonus;
                }
            }

            return (health, mana, attack, defense);
        }
    }
}
