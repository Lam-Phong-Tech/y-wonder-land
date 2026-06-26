using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Bảng ánh xạ "tên item Build Mode" → prefab THẬT để sinh ra khi đặt.
    /// Gắn script này vào 1 GameObject trong scene (vd "[BuildPrefabLibrary]"),
    /// rồi kéo prefab vào danh sách + gõ từ khóa khớp tên item trên thanh Build.
    ///
    /// Khớp theo "tên item CHỨA từ khóa" (không phân biệt hoa thường). Ví dụ:
    ///   nameContains = "ruộng"  → khớp "Ruộng 2x2", "Ruộng 3x3"  → sinh prefab đất.
    ///   nameContains = "rào gỗ" → khớp "Rào gỗ"                  → sinh prefab hàng rào gỗ.
    ///
    /// Nếu KHÔNG có entry khớp, Build Mode dùng khối Cube placeholder như cũ.
    /// </summary>
    public class BuildPrefabLibrary : MonoBehaviour
    {
        public static BuildPrefabLibrary Instance { get; private set; }

        [System.Serializable]
        public class Entry
        {
            [Tooltip("Tên item trên thanh Build CHỨA chuỗi này (không phân biệt hoa thường) sẽ dùng prefab bên dưới.")]
            public string nameContains = "";

            [Tooltip("Prefab THẬT sẽ được sinh ra khi đặt (vd prefab đất có FarmTile, prefab hàng rào...).")]
            public GameObject prefab;

            [Tooltip("BẬT cho ô ĐẤT: co giãn prefab cho vừa kích thước ô lưới (vd 2x2). TẮT cho hàng rào/trang trí (giữ nguyên prefab).")]
            public bool stretchToFootprint = false;

            [Tooltip("Nâng (+) / hạ (-) prefab theo trục Y so với mặt đất (m). Dùng khi prefab bị chìm/nổi.")]
            public float yOffset = 0f;
        }

        [Tooltip("Danh sách ánh xạ tên item → prefab. Kéo prefab vào đây trong Inspector.")]
        public List<Entry> entries = new List<Entry>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        /// <summary>
        /// Tìm entry khớp tên item. Ưu tiên từ khóa CỤ THỂ NHẤT (dài nhất) để tránh nhầm:
        /// "Chuồng gà" sẽ khớp entry "chuồng gà" thay vì entry "chuồng" chung chung. Null nếu không có.
        /// </summary>
        public Entry Find(string itemName)
        {
            if (string.IsNullOrEmpty(itemName) || entries == null) return null;
            string lower = itemName.ToLowerInvariant();
            Entry best = null;
            int bestLen = -1;
            foreach (var e in entries)
            {
                if (e == null || e.prefab == null || string.IsNullOrEmpty(e.nameContains)) continue;
                string key = e.nameContains.ToLowerInvariant();
                if (lower.Contains(key) && key.Length > bestLen)
                {
                    best = e;
                    bestLen = key.Length;
                }
            }
            return best;
        }
    }
}
