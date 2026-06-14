using System;
using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Managers
{
    [System.Serializable]
    public class ToolUpgradeRequirement
    {
        public int posCost;
        public int woodCost;
        public int stoneCost;
        public int ironCost;
    }

    /// <summary>
    /// Quản lý cấp độ của các dụng cụ (Cuốc, Rìu, Cần câu, Xô tưới).
    /// </summary>
    public class ToolManager : MonoBehaviour
    {
        public static ToolManager Instance { get; private set; }

        public event Action OnToolUpgraded;

        public const int MAX_TOOL_LEVEL = 10;
        
        public static readonly string[] BaseTools = new string[] 
        {
            "hoe_01", "axe_01", "fishing_rod_01", "watering_can_01"
        };

        private Dictionary<string, int> toolLevels = new Dictionary<string, int>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadToolLevels();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadToolLevels()
        {
            foreach (var toolId in BaseTools)
            {
                // Mặc định cấp 1
                int level = PlayerPrefs.GetInt($"YW_ToolLevel_{toolId}", 1);
                toolLevels[toolId] = level;
            }
        }

        public void SaveToolLevels()
        {
            foreach (var kvp in toolLevels)
            {
                PlayerPrefs.SetInt($"YW_ToolLevel_{kvp.Key}", kvp.Value);
            }
            PlayerPrefs.Save();
        }

        public int GetToolLevel(string toolId)
        {
            if (toolLevels.TryGetValue(toolId, out int level))
            {
                return level;
            }
            return 1;
        }

        public string GetToolDisplayName(string toolId, string baseName)
        {
            int level = GetToolLevel(toolId);
            return $"{baseName} Lv{level}";
        }

        public ToolUpgradeRequirement GetUpgradeRequirement(string toolId, int nextLevel)
        {
            if (nextLevel > MAX_TOOL_LEVEL) return null;

            return new ToolUpgradeRequirement
            {
                posCost = nextLevel * 500,
                woodCost = nextLevel * 10,
                stoneCost = nextLevel * 5,
                ironCost = nextLevel >= 3 ? (nextLevel - 2) * 5 : 0
            };
        }

        public bool UpgradeTool(string toolId)
        {
            int currentLevel = GetToolLevel(toolId);
            if (currentLevel >= MAX_TOOL_LEVEL) return false;

            int nextLevel = currentLevel + 1;
            ToolUpgradeRequirement req = GetUpgradeRequirement(toolId, nextLevel);

            int woodOwned = InventoryManager.Instance.GetItemQuantity("wood_01");
            int stoneOwned = InventoryManager.Instance.GetItemQuantity("stone_01");
            int ironOwned = InventoryManager.Instance.GetItemQuantity("iron_01");
            long posOwned = EconomyManager.Instance.GetPOS();

            if (posOwned >= req.posCost && 
                woodOwned >= req.woodCost && 
                stoneOwned >= req.stoneCost && 
                ironOwned >= req.ironCost)
            {
                // Deduct resources
                if (req.woodCost > 0) InventoryManager.Instance.RemoveItem("wood_01", req.woodCost);
                if (req.stoneCost > 0) InventoryManager.Instance.RemoveItem("stone_01", req.stoneCost);
                if (req.ironCost > 0) InventoryManager.Instance.RemoveItem("iron_01", req.ironCost);
                EconomyManager.Instance.SpendPOS(req.posCost);

                // Level up
                toolLevels[toolId] = nextLevel;
                SaveToolLevels();
                
                OnToolUpgraded?.Invoke();
                Debug.Log($"[ToolManager] Upgraded {toolId} to Lv{nextLevel}");
                return true;
            }

            return false;
        }
    }
}
