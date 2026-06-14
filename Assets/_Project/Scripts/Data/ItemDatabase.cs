using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Data
{
    /// <summary>
    /// Database chứa toàn bộ vật phẩm trong game. Dễ dàng tra cứu bằng hàm GetItem(id).
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Y WONDER GREEN FARM/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemDefinition> items = new List<ItemDefinition>();

        private Dictionary<string, ItemDefinition> itemCache;

        public void InitializeCache()
        {
            itemCache = new Dictionary<string, ItemDefinition>();
            foreach (var item in items)
            {
                if (item != null && !string.IsNullOrEmpty(item.id))
                {
                    if (!itemCache.ContainsKey(item.id))
                    {
                        itemCache.Add(item.id, item);
                    }
                    else
                    {
                        Debug.LogWarning($"[ItemDatabase] Duplicate ID found: {item.id}");
                    }
                }
            }
        }

        public ItemDefinition GetItem(string id)
        {
            if (itemCache == null || itemCache.Count != items.Count)
            {
                InitializeCache();
            }

            if (itemCache.TryGetValue(id, out var item))
            {
                return item;
            }

            Debug.LogWarning($"[ItemDatabase] Item ID not found: {id}");
            return null;
        }
    }
}
