using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Data
{
    /// <summary>
    /// Database chứa danh sách tất cả CropDefinition trong game.
    /// Tạo bằng menu: Assets → Create → YWonderLand → Crop Database
    /// </summary>
    [CreateAssetMenu(fileName = "CropDatabase", menuName = "YWonderLand/Crop Database")]
    public class CropDatabase : ScriptableObject
    {
        [SerializeField] private List<CropDefinition> crops = new List<CropDefinition>();

        private Dictionary<string, CropDefinition> _cache;

        /// <summary>
        /// Tìm CropDefinition theo seedItemId.
        /// </summary>
        public CropDefinition GetCropBySeedId(string seedId)
        {
            if (_cache == null) BuildCache();
            _cache.TryGetValue(seedId, out CropDefinition crop);
            return crop;
        }

        /// <summary>
        /// Tìm CropDefinition theo harvestItemId.
        /// </summary>
        public CropDefinition GetCropByHarvestId(string harvestId)
        {
            if (_cache == null) BuildCache();
            foreach (var crop in crops)
            {
                if (crop.harvestItemId == harvestId) return crop;
            }
            return null;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách crops.
        /// </summary>
        public List<CropDefinition> GetAllCrops() => crops;

        private void BuildCache()
        {
            _cache = new Dictionary<string, CropDefinition>();
            foreach (var crop in crops)
            {
                if (crop != null && !string.IsNullOrEmpty(crop.seedItemId))
                {
                    _cache[crop.seedItemId] = crop;
                }
            }
        }

        private void OnEnable()
        {
            _cache = null; // Force rebuild cache khi load
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: Xóa danh sách crops.
        /// </summary>
        public void ClearCrops()
        {
            crops.Clear();
            _cache = null;
        }

        /// <summary>
        /// Editor-only: Thêm một CropDefinition vào database.
        /// </summary>
        public void AddCropEntry(CropDefinition crop)
        {
            if (crop != null) crops.Add(crop);
            _cache = null;
        }
#endif
    }
}
