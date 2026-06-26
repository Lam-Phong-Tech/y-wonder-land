using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Data
{
    /// <summary>
    /// Định nghĩa 1 CỬA HÀNG (catalog) — mỗi shop NPC = 1 asset.
    /// Chỉ lưu DANH SÁCH ID hàng; giá/tên/icon tra từ ItemDatabase (nguồn dữ liệu DUY NHẤT —
    /// đổi giá 1 chỗ trong ItemDatabase là mọi shop cập nhật theo). Kéo asset này vào
    /// ShopZoneTrigger (chạm nhà) hoặc MerchantNPC (click — phương án phụ).
    /// </summary>
    [CreateAssetMenu(fileName = "NewShop", menuName = "YWonderLand/Shop Definition")]
    public class ShopDefinition : ScriptableObject
    {
        public enum AccessMode { Both, BuyOnly, SellOnly }

        [Header("Thông tin")]
        public string shopName = "Cửa hàng";

        [Tooltip("Both = mua & bán; BuyOnly = chỉ mua; SellOnly = chỉ thu mua.")]
        public AccessMode accessMode = AccessMode.Both;

        [Header("Hàng MUA (ID tra giá/tên/icon từ ItemDatabase)")]
        public List<string> buyItemIds = new List<string>();

        [Header("Hàng được phép BÁN (whitelist)")]
        [Tooltip("Để TRỐNG = thu mua MỌI thứ bán được trong túi. Điền ID để giới hạn " +
                 "(vd Mini Garden chỉ thu nông sản, Fish Shop chỉ thu cá).")]
        public List<string> sellItemIds = new List<string>();

        /// <summary>Có hiện tab Bán không (mọi chế độ trừ BuyOnly).</summary>
        public bool HasSellTab => accessMode != AccessMode.BuyOnly;
    }
}
