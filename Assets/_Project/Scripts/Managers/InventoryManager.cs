using System;
using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Managers
{
    [System.Serializable]
    public class InventorySlot
    {
        public string itemId;
        public int quantity;

        public InventorySlot(string id, int qty)
        {
            itemId = id;
            quantity = qty;
        }
    }

    [System.Serializable]
    public class InventoryData
    {
        public int maxSlots = 50;
        public List<InventorySlot> slots = new List<InventorySlot>();
    }

    /// <summary>
    /// Quản lý túi đồ của người chơi. Tạm dùng PlayerPrefs, sau này sẽ dùng UGS Cloud Save.
    /// Khả năng tự mở rộng slot (maxSlots++) nếu mua dư.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        public event Action OnInventoryChanged;

        private const string INV_KEY = "YW_Inventory_Data";
        private InventoryData inventoryData;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadInventory();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadInventory()
        {
            string json = PlayerPrefs.GetString(INV_KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                inventoryData = new InventoryData();
                // Add default items for testing & tutorial
                inventoryData.slots.Add(new InventorySlot("hoe_01", 1));
                inventoryData.slots.Add(new InventorySlot("watering_can_01", 1));
                inventoryData.slots.Add(new InventorySlot("carrot_seed_01", 5));
                // Demo: tặng sẵn vài con vật để test thả chuồng (production: mua từ shop)
                inventoryData.slots.Add(new InventorySlot("chicken_01", 2));
                inventoryData.slots.Add(new InventorySlot("ostrich_01", 2));
                inventoryData.slots.Add(new InventorySlot("cow_01", 2));
            }
            else
            {
                inventoryData = JsonUtility.FromJson<InventoryData>(json);
            }
        }

        private void SaveInventory()
        {
            string json = JsonUtility.ToJson(inventoryData);
            PlayerPrefs.SetString(INV_KEY, json);
            PlayerPrefs.Save();
        }

        public int GetMaxSlots() => inventoryData.maxSlots;

        public List<InventorySlot> GetAllSlots()
        {
            return new List<InventorySlot>(inventoryData.slots);
        }

        public int GetItemQuantity(string itemId)
        {
            int total = 0;
            foreach (var slot in inventoryData.slots)
            {
                if (slot.itemId == itemId)
                    total += slot.quantity;
            }
            return total;
        }

        public void AddItem(string itemId, int quantity)
        {
            if (quantity <= 0) return;

            bool found = false;
            foreach (var slot in inventoryData.slots)
            {
                if (slot.itemId == itemId)
                {
                    slot.quantity += quantity;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // Auto expand if full (theo logic của khách hàng)
                if (inventoryData.slots.Count >= inventoryData.maxSlots)
                {
                    inventoryData.maxSlots += 1;
                    Debug.Log($"[Inventory] Auto expanded max slots to {inventoryData.maxSlots}");
                }
                inventoryData.slots.Add(new InventorySlot(itemId, quantity));
            }

            SaveInventory();
            Debug.Log($"[Inventory] Added {quantity}x {itemId}");
            OnInventoryChanged?.Invoke();
        }

        public bool RemoveItem(string itemId, int quantity)
        {
            if (quantity <= 0) return true;
            if (GetItemQuantity(itemId) < quantity) return false;

            int remainingToRemove = quantity;
            for (int i = inventoryData.slots.Count - 1; i >= 0; i--)
            {
                var slot = inventoryData.slots[i];
                if (slot.itemId == itemId)
                {
                    if (slot.quantity > remainingToRemove)
                    {
                        slot.quantity -= remainingToRemove;
                        remainingToRemove = 0;
                        break;
                    }
                    else
                    {
                        remainingToRemove -= slot.quantity;
                        inventoryData.slots.RemoveAt(i);
                    }
                }
            }

            SaveInventory();
            Debug.Log($"[Inventory] Removed {quantity}x {itemId}");
            OnInventoryChanged?.Invoke();
            return true;
        }
    }
}
