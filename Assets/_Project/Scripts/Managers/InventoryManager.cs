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

        [Header("Debug — Test (nhớ TẮT khi release)")]
        [Tooltip("BẬT: lúc Play tự nạp nhiều thức ăn/nông sản/vật liệu/hạt + tiền vào túi để test NPC mua/bán.")]
        [SerializeField] private bool giveTestLoadoutOnStart = false;
        [Tooltip("Số POS cộng thêm khi nạp loadout test.")]
        [SerializeField] private long testMoney = 100000;

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

        void Start()
        {
            if (giveTestLoadoutOnStart) GiveTestLoadout();
        }

        /// <summary>Nạp nhiều thức ăn/nông sản/vật liệu/hạt + tiền vào túi để TEST NPC mua/bán.</summary>
        public void GiveTestLoadout()
        {
            // Nông sản + sản phẩm + thực phẩm + cá (đồ để BÁN / cho thú ăn)
            string[] foods =
            {
                "carrot_01","cabbage_01","watermelon_01","corn_01","pumpkin_01",
                "morning_glory_01","sweet_potato_01","grass_01",
                "egg_01","milk_01","pork_01","bread_01","apple_01","fish_01","fish_02"
            };
            foreach (var id in foods) AddItem(id, 30);

            // Vật liệu (để bán / xây)
            foreach (var id in new[] { "wood_01","stone_01","iron_01","ore_01","brick_01" })
                AddItem(id, 30);

            // Hạt giống 8 loại
            foreach (var id in new[] { "carrot_seed_01","cabbage_seed_01","watermelon_seed_01","corn_seed_01",
                                       "pumpkin_seed_01","grass_seed_01","morning_glory_seed_01","sweet_potato_seed_01" })
                AddItem(id, 15);

            // Vật phẩm tiêu hao (phân/vắc-xin/thuốc/mồi)
            foreach (var id in new[] { "fertilizer_01","vaccine_01","medicine_01","bait_01" })
                AddItem(id, 15);

            if (EconomyManager.Instance != null) EconomyManager.Instance.AddPOS(testMoney);

            Debug.Log($"[Inventory] Đã nạp LOADOUT TEST: thức ăn/nông sản/vật liệu/hạt + {testMoney} POS.");
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
