using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Thư viện map itemId con vật -> prefab thật, để THẢ vào vùng quây (không còn prefab gắn sẵn
    /// trên từng chuồng như cũ). Gắn 1 cái trong scene, điền danh sách 1 lần.
    /// </summary>
    public class AnimalPrefabLibrary : MonoBehaviour
    {
        public static AnimalPrefabLibrary Instance { get; private set; }

        [System.Serializable]
        public class Entry
        {
            [Tooltip("ID vật phẩm con vật trong túi (vd chicken_01, cow_01).")]
            public string itemId;
            [Tooltip("Prefab con vật thật sinh ra khi thả.")]
            public GameObject prefab;
            [Tooltip("Nâng/hạ vị trí sinh con so với mặt ô đất (m). Chỉnh cho khớp pivot từng loài.")]
            public float spawnHeightOffset = 0f;
        }

        [Tooltip("Map itemId -> prefab con vật.")]
        public List<Entry> animals = new List<Entry>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private Entry Find(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || animals == null) return null;
            foreach (var e in animals)
                if (e != null && e.itemId == itemId) return e;
            return null;
        }

        public GameObject GetPrefab(string itemId)
        {
            var e = Find(itemId);
            return e != null ? e.prefab : null;
        }

        /// <summary>Offset độ cao khi sinh con (m), theo từng loài.</summary>
        public float GetSpawnHeightOffset(string itemId)
        {
            var e = Find(itemId);
            return e != null ? e.spawnHeightOffset : 0f;
        }
    }
}
