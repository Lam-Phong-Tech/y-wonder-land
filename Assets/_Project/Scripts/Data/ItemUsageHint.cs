using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace YWonderLand.Data
{
    /// <summary>
    /// Sinh câu "vật phẩm này dùng để làm gì" cho túi đồ, TỪ DỮ LIỆU chứ không gõ tay từng món:
    /// hạt tra CropDatabase, thức ăn tra ngược AnimalDefinition, hàng bán tra sellPrice.
    /// Khách đổi số liệu là câu chữ tự đổi theo — không có bảng chữ chết nào phải bảo trì.
    ///
    /// Dùng khi người chơi bấm "Sử dụng" mà ngữ cảnh KHÔNG có việc thật để làm
    /// (không đang gieo hạt / cho ăn / thả thú). Có việc thật thì làm việc, đừng gọi hàm này.
    /// </summary>
    public static class ItemUsageHint
    {
        private static ItemDatabase itemDb;
        private static CropDatabase cropDb;

        // Tên thức ăn (đã chuẩn hoá) -> danh sách "Tên con vật (số lượng)".
        private static Dictionary<string, List<string>> feedIndex;

        /// <summary>Câu gợi ý công dụng. Luôn trả về chuỗi không rỗng.</summary>
        public static string Build(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return "Vật phẩm không xác định.";

            EnsureDatabases();
            ItemDefinition def = itemDb != null ? itemDb.GetItem(itemId) : null;
            if (def == null) return "Vật phẩm không có trong dữ liệu.";

            string name = !string.IsNullOrEmpty(def.itemName) ? def.itemName : itemId;

            switch (def.category)
            {
                case "seeds":     return SeedHint(def, name);
                case "food":      return FoodHint(def, name);
                case "products":  return SellHint(def, name);
                case "materials": return MaterialHint(def, name);
                case "tools":     return ToolHint(def, name);
                case "animals":   return AnimalHint(def, name);
                default:          return FallbackHint(def, name);
            }
        }

        // ── Hạt giống: trồng ở đâu, bao lâu, ra gì ──────────────────────────────
        private static string SeedHint(ItemDefinition def, string name)
        {
            CropDefinition crop = cropDb != null ? cropDb.GetCropBySeedId(def.id) : null;
            if (crop == null) return $"{name}: gieo xuống ruộng đã cuốc để trồng cây.";

            string harvest = DisplayName(crop.harvestItemId, "nông sản");
            var sb = new StringBuilder();
            sb.Append($"{name}: trồng ở ruộng, sau {Duration(crop.growthTimeSec)} thu {harvest} x{Mathf.Max(1, crop.harvestYield)}");

            if (crop.maxHarvests > 1)
                sb.Append($" — thu được {crop.maxHarvests} vụ, mỗi vụ cách {Duration(crop.reHarvestCycleSec)}");
            sb.Append('.');

            if (!string.IsNullOrEmpty(crop.finalProductItemId) && crop.finalProductAmount > 0)
                sb.Append($" Vụ cuối còn thêm {DisplayName(crop.finalProductItemId, "sản phẩm")} x{crop.finalProductAmount}.");

            if (crop.plotSlots > 1)
                sb.Append($" Chiếm {crop.plotSlots} ô đất.");

            return sb.ToString();
        }

        // ── Nhóm "food": vừa có cá (để bán) vừa có nông sản ngắn ngày (thức ăn) ──
        private static string FoodHint(ItemDefinition def, string name)
        {
            string eaters = EatersOf(def.itemName);
            if (!string.IsNullOrEmpty(eaters))
                return $"{name}: thức ăn chăn nuôi — cho {eaters} ăn. Bấm vào con vật đang đói rồi chọn món này.";

            if (def.canSell && def.sellPrice > 0)
                return SellHint(def, name);

            return FallbackHint(def, name);
        }

        // ── Hàng để bán: bán ở đâu, được bao nhiêu ──────────────────────────────
        private static string SellHint(ItemDefinition def, string name)
        {
            if (!def.canSell || def.sellPrice <= 0)
                return FallbackHint(def, name);

            return $"{name}: bán ở {BuyerShopOf(def)} được {def.sellPrice} Point mỗi cái.";
        }

        // ── Nguyên liệu: gỗ/đá/gạch để xây, đá quý & quặng để bán ───────────────
        private static string MaterialHint(ItemDefinition def, string name)
        {
            if (def.id == "wood_01" || def.id == "stone_01" || def.id == "brick_01")
            {
                string sell = def.canSell && def.sellPrice > 0
                    ? $" Bán cũng được {def.sellPrice} Point mỗi cái."
                    : "";
                return $"{name}: vật liệu xây dựng — mở Chế độ Xây để dựng ruộng, chuồng và công trình.{sell}";
            }

            if (def.id == "watering_water_01")
                return $"{name}: thứ dùng để tưới cây đang trồng, mỗi lần tưới tốn 1 xô. " +
                       "Hết thì ra ao/hồ trên đảo bấm \"Múc nước\" để lấy thêm — miễn phí.";

            return SellHint(def, name);
        }

        // ── Dụng cụ: tự động dùng khi tương tác, không cần bấm trong túi ────────
        private static string ToolHint(ItemDefinition def, string name)
        {
            // Xô là dụng cụ MÚC, không phải thứ tưới: tưới cây tốn "Nước tưới" chứ không tốn xô.
            if (def.id == "watering_can_01")
                return $"{name}: dụng cụ múc nước — ra ao/hồ trên đảo bấm \"Múc nước\" để lấy Nước tưới, " +
                       "rồi mới dùng nước đó tưới cây.";

            string job;
            switch (def.id)
            {
                case "axe_01":          job = "chặt cây lấy gỗ"; break;
                case "pickaxe_01":      job = "đập đá lấy đá"; break;
                case "hoe_01":          job = "cuốc đất làm ruộng"; break;
                case "fishing_rod_01":  job = "câu cá ở bờ nước"; break;
                default:                job = "làm việc trên nông trại"; break;
            }
            return $"{name}: dùng để {job}. Tự động dùng khi bấm nút tương tác — không cần chọn trong túi.";
        }

        // ── Con giống: thả vào chuồng, cần bao nhiêu ô, ăn gì ───────────────────
        private static string AnimalHint(ItemDefinition def, string name)
        {
            var d = Managers.AnimalManager.LookupDefinition(def.id);
            if (d == null) return $"{name}: thả vào chuồng đã xây để nuôi.";

            string food = FoodOf(d);
            string foodPart = string.IsNullOrEmpty(food) ? "" : $" Ăn {food}.";
            return $"{name}: mở Chế độ Xây dựng chuồng ({d.penSlots} ô đất), rồi bấm chuồng để thả vào nuôi.{foodPart}";
        }

        // ── Không rơi vào nhóm nào: dùng mô tả sẵn có ───────────────────────────
        private static string FallbackHint(ItemDefinition def, string name)
        {
            return !string.IsNullOrEmpty(def.description) ? $"{name}: {def.description}" : $"{name}.";
        }

        // ── Tra ngược: món ăn này con nào ăn, mỗi lần mấy cái ───────────────────
        private static string EatersOf(string foodDisplayName)
        {
            if (string.IsNullOrEmpty(foodDisplayName)) return null;
            EnsureFeedIndex();

            return feedIndex.TryGetValue(Normalize(foodDisplayName), out var list) && list.Count > 0
                ? string.Join(", ", list)
                : null;
        }

        private static void EnsureFeedIndex()
        {
            if (feedIndex != null) return;
            feedIndex = new Dictionary<string, List<string>>();

            // Tên thức ăn trong AnimalDefinition là CHỮ HIỂN THỊ ("Bắp Ngô"/"Bắp ngô", "Cỏ Voi"/"Cỏ voi"),
            // không phải id, và viết hoa không nhất quán -> phải chuẩn hoá trước khi so.
            foreach (var d in Resources.LoadAll<AnimalDefinition>(""))
            {
                if (d == null) continue;
                AddFeed(d.foodMainName, d.animalName, d.foodMainAmount);
                AddFeed(d.foodAltName, d.animalName, d.foodAltAmount);
            }
        }

        private static void AddFeed(string foodName, string animalName, int amount)
        {
            // amount <= 0 nghĩa là món đó không thật sự dùng (vd "Cám" ghi kèm nhưng số lượng 0).
            if (string.IsNullOrEmpty(foodName) || string.IsNullOrEmpty(animalName) || amount <= 0) return;

            string key = Normalize(foodName);
            if (!feedIndex.TryGetValue(key, out var list))
            {
                list = new List<string>();
                feedIndex[key] = list;
            }
            list.Add($"{animalName} ({amount})");
        }

        private static string FoodOf(AnimalDefinition d)
        {
            string main = !string.IsNullOrEmpty(d.foodMainName) && d.foodMainAmount > 0
                ? $"{d.foodMainAmount}x {d.foodMainName}" : null;
            string alt = !string.IsNullOrEmpty(d.foodAltName) && d.foodAltAmount > 0
                ? $"{d.foodAltAmount}x {d.foodAltName}" : null;

            if (main != null && alt != null) return $"{main} hoặc {alt}";
            return main ?? alt;
        }

        // ── Tiện ích ────────────────────────────────────────────────────────────

        /// <summary>
        /// Tên shop thu mua. Các asset ShopDefinition nằm ngoài Resources nên không Load được
        /// lúc chạy; ánh xạ theo nhóm hàng là đủ và khớp cấu hình hiện tại.
        /// </summary>
        private static string BuyerShopOf(ItemDefinition def)
        {
            if (def.id != null && def.id.StartsWith("fish_")) return "Siêu thị Cá";
            if (def.id != null && def.id.StartsWith("gem_")) return "cửa hàng Thu mua Đá quý";
            return "Mini Garden";
        }

        private static string DisplayName(string itemId, string fallback)
        {
            if (string.IsNullOrEmpty(itemId)) return fallback;
            var d = itemDb != null ? itemDb.GetItem(itemId) : null;
            return d != null && !string.IsNullOrEmpty(d.itemName) ? d.itemName : itemId;
        }

        private static string Duration(float seconds)
        {
            if (seconds >= 86400f)
            {
                float days = seconds / 86400f;
                return days >= 2f ? $"{Mathf.RoundToInt(days)} ngày" : "1 ngày";
            }
            if (seconds >= 3600f) return $"{Mathf.RoundToInt(seconds / 3600f)} giờ";
            if (seconds >= 60f) return $"{Mathf.RoundToInt(seconds / 60f)} phút";
            return $"{Mathf.RoundToInt(seconds)} giây";
        }

        private static string Normalize(string s)
        {
            return string.IsNullOrEmpty(s) ? "" : s.Trim().ToLowerInvariant();
        }

        private static void EnsureDatabases()
        {
            if (itemDb == null) itemDb = Resources.Load<ItemDatabase>("ItemDatabase");
            if (cropDb == null) cropDb = Resources.Load<CropDatabase>("CropDatabase");
        }
    }
}
