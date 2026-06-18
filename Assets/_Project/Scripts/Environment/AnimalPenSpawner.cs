using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Gắn lên prefab CHUỒNG. Khi người chơi click vào chuồng -> mở túi đồ (tab Thú nuôi) để chọn con vật.
    /// Chỉ thả được loài CÓ TRONG danh sách của chuồng này (giới hạn theo cỡ chuồng:
    /// chuồng gà chỉ list gà/vịt..., chuồng bò chỉ list bò — không cho bò vào chuồng gà).
    ///
    /// Mỗi entry map: itemId (vật phẩm trong túi) -> prefab con vật sinh ra.
    /// Demo: maxCapacity = 1 (mỗi chuồng 1 con).
    /// </summary>
    public class AnimalPenSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class AnimalOption
        {
            [Tooltip("ID vật phẩm con vật trong túi (vd: chicken_01, ostrich_01, cow_01).")]
            public string itemId;

            [Tooltip("Prefab con vật sinh ra trong chuồng khi chọn vật phẩm này.")]
            public GameObject prefab;
        }

        [Header("Loài cho phép thả vào chuồng này")]
        [Tooltip("Chỉ những loài liệt kê ở đây mới thả được vào chuồng (giới hạn theo cỡ chuồng).")]
        public List<AnimalOption> allowedAnimals = new List<AnimalOption>();

        [Header("Cấu hình")]
        [Tooltip("Số con tối đa trong chuồng (demo = 1).")]
        public int maxCapacity = 1;

        [Tooltip("Nâng/hạ vị trí sinh con so với tâm chuồng (m).")]
        public float spawnHeightOffset = 0f;

        private int spawnedCount = 0;

        /// <summary>Còn chỗ thả thêm con không.</summary>
        public bool HasSpace => spawnedCount < maxCapacity;

        /// <summary>Prefab tương ứng vật phẩm (null nếu loài này KHÔNG được phép trong chuồng).</summary>
        public GameObject GetPrefabFor(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || allowedAnimals == null) return null;
            foreach (var a in allowedAnimals)
                if (a != null && a.itemId == itemId && a.prefab != null) return a.prefab;
            return null;
        }

        /// <summary>Chuồng còn chỗ VÀ loài này được phép.</summary>
        public bool CanAccept(string itemId) => HasSpace && GetPrefabFor(itemId) != null;

        /// <summary>Liệt kê các itemId được phép (để debug khi từ chối).</summary>
        public string AllowedIdsText()
        {
            if (allowedAnimals == null || allowedAnimals.Count == 0) return "(DANH SÁCH RỖNG — chưa gán)";
            var sb = new System.Text.StringBuilder();
            foreach (var a in allowedAnimals)
            {
                if (a == null) continue;
                sb.Append('\'').Append(a.itemId).Append('\'');
                sb.Append(a.prefab != null ? "" : "(THIẾU PREFAB)");
                sb.Append("  ");
            }
            return sb.ToString();
        }

        /// <summary>Thả 1 con theo itemId. Trả true nếu thành công.</summary>
        public bool SpawnByItem(string itemId)
        {
            var prefab = GetPrefabFor(itemId);
            if (prefab == null || !HasSpace) return false;

            Vector3 pos = transform.position + Vector3.up * spawnHeightOffset;
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            go.name = prefab.name; // bỏ hậu tố "(Clone)"
            spawnedCount++;

            // Nếu có hệ FarmAnimal + AnimalPen thì đăng ký để dùng cho ăn / ra sản phẩm.
            var pen = GetComponent<AnimalPen>();
            var fa = go.GetComponentInChildren<FarmAnimal>();
            if (pen != null && fa != null) pen.AddAnimal(fa);

            Debug.Log($"[AnimalPen] Thả '{go.name}' (item {itemId}) vào chuồng '{name}' ({spawnedCount}/{maxCapacity}).");
            return true;
        }
    }
}
