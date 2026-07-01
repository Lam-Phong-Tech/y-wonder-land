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
        private int cachedItemListCount = -1;

        public void InitializeCache()
        {
            itemCache = new Dictionary<string, ItemDefinition>();
            if (items == null)
            {
                cachedItemListCount = 0;
                return;
            }

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

            cachedItemListCount = items != null ? items.Count : 0;
        }

        public ItemDefinition GetItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null; // slot rỗng → bỏ qua, không cảnh báo thừa

            if (itemCache == null || cachedItemListCount != (items != null ? items.Count : 0))
            {
                InitializeCache();
            }

            if (itemCache.TryGetValue(id, out var item))
            {
                return item;
            }

            var resourceItem = Resources.Load<ItemDefinition>($"Items/{id}");
            if (resourceItem != null)
            {
                itemCache[id] = resourceItem;
                return resourceItem;
            }

            Debug.LogWarning($"[ItemDatabase] Item ID not found: {id}");
            return null;
        }
    }
}
