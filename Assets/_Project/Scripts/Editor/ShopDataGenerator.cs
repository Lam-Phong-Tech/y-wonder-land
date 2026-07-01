using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using YWonderLand.Data;

namespace YWonderLand.EditorTools
{
    /// <summary>
    /// Sinh sẵn các asset ShopDefinition cho nhóm shop MUA/BÁN (giống Generate Animal Data).
    /// ID hàng lấy từ catalog có thật trong ItemDatabase (ItemDataGenerator). Giá tra runtime
    /// từ ItemDatabase nên KHÔNG nhồi giá vào asset (1 nguồn duy nhất).
    ///
    /// Menu: YWonderLand > Generate Shop Data
    /// </summary>
    public static class ShopDataGenerator
    {
        private const string FolderPath = "Assets/_Project/Data/Shops";

        [MenuItem("YWonderLand/Generate Shop Data")]
        public static void GenerateShopData()
        {
            EnsureFolder();

            // 1) Farm Shop — bán hạt giống + con giống (BuyOnly)
            CreateShop("Shop_FarmShop", "Cửa hàng Hạt giống & Vật nuôi", ShopDefinition.AccessMode.BuyOnly,
                buy: new List<string>
                {
                    // Hạt cây NGẮN NGÀY (8)
                    "carrot_seed_01", "cabbage_seed_01", "watermelon_seed_01", "corn_seed_01",
                    "pumpkin_seed_01", "grass_seed_01", "morning_glory_seed_01", "sweet_potato_seed_01",
                    // Hạt cây LÂU NĂM (11)
                    "banana_seed_01", "coconut_seed_01", "areca_seed_01", "date_seed_01", "sacha_seed_01",
                    "tea_seed_01", "durian_seed_01", "asparagus_seed_01", "red_ginseng_seed_01", "royal_ginseng_seed_01",
                    "passion_fruit_seed_01",
                    // Con giống (10)
                    "chicken_01", "rabbit_01", "ostrich_01", "goat_01", "cow_01", "deer_01", "pig_01",
                    "duck_01", "goose_01", "turtle_01"
                });

            // 2) Item Shop — vật tư trồng trọt/chăn nuôi (BuyOnly)
            CreateShop("Shop_ItemShop", "Cửa hàng Vật phẩm", ShopDefinition.AccessMode.BuyOnly,
                buy: new List<string> { "fertilizer_01", "vaccine_01", "medicine_01", "bait_01", "mine_ticket_01" });

            // 3) Fish Shop — mua mồi, thu mua cá (Both)
            CreateShop("Shop_FishShop", "Siêu thị Cá", ShopDefinition.AccessMode.Both,
                buy: new List<string> { "bait_01" },
                sell: new List<string>
                {
                    "fish_01", "fish_02",
                    "fish_ca_com_01", "fish_ca_nuc_01", "fish_ca_hong_01",
                    "fish_ca_su_tu_01", "fish_ca_naso_01", "fish_ca_nhong_01",
                    "fish_ca_soc_dua_01", "fish_ca_khe_01", "fish_ca_mu_01",
                    "fish_ca_mat_quy_01", "fish_ca_heo_bien_01",
                    "fish_ca_hoang_de_01", "fish_ca_ngu_hoang_kim_01",
                    "fish_ca_rong_do_01",
                    "gift_box_01"
                });

            // 4) Mini Garden — thu mua nông sản + sản phẩm chăn nuôi (SellOnly)
            CreateShop("Shop_MiniGarden", "Mini Garden — Thu mua Nông sản", ShopDefinition.AccessMode.SellOnly,
                sell: new List<string>
                {
                    // Nông sản NGẮN NGÀY (8)
                    "carrot_01", "cabbage_01", "watermelon_01", "corn_01", "pumpkin_01",
                    "morning_glory_01", "sweet_potato_01", "grass_01",
                    // Sản phẩm cây LÂU NĂM (11)
                    "banana_01", "coconut_01", "areca_01", "date_01", "sacha_01",
                    "tea_01", "durian_01", "asparagus_01", "red_ginseng_01", "royal_ginseng_01",
                    "passion_fruit_01",
                    // Sản phẩm chăn nuôi CHÍNH (10)
                    "egg_01", "milk_01", "pigskin_01", "ostrich_egg_01", "deer_velvet_01", "goat_milk_01",
                    "rabbit_fur_01", "goose_egg_01", "duck_egg_01", "turtle_shell_01",
                    // THỊT vụ cuối (10 — gia cầm cũng có thịt ở vụ cuối theo cập nhật khách 29/06)
                    "pork_01", "chicken_meat_01", "beef_01", "ostrich_meat_01", "deer_meat_01",
                    "goat_meat_01", "rabbit_meat_01", "goose_meat_01", "duck_meat_01", "turtle_meat_01"
                });

            // 5) Hai Lúa — phân/vắc-xin/thuốc (BuyOnly)
            CreateShop("Shop_HaiLua", "Đại lý Hai Lúa", ShopDefinition.AccessMode.BuyOnly,
                buy: new List<string> { "fertilizer_01", "vaccine_01", "medicine_01" });

            // 6) Verdant — bán nông sản + mua tiêu dùng (Both)
            CreateShop("Shop_Verdant", "Siêu thị Verdant", ShopDefinition.AccessMode.Both,
                buy: new List<string> { "bread_01", "apple_01" },
                sell: new List<string>
                {
                    "carrot_01", "cabbage_01", "watermelon_01", "corn_01", "pumpkin_01",
                    "morning_glory_01", "sweet_potato_01"
                });

            // 7) Thú Y — vắc-xin + thuốc cho vật nuôi (BuyOnly)
            CreateShop("Shop_ThuY", "Phòng khám Thú Y", ShopDefinition.AccessMode.BuyOnly,
                buy: new List<string> { "vaccine_01", "medicine_01" });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ShopDataGenerator] Đã sinh/cập nhật 7 asset shop trong {FolderPath}");
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(FolderPath)) return;
            // Tạo từng cấp folder qua AssetDatabase (Assets/_Project/Data/Shops).
            string[] parts = FolderPath.Split('/');
            string cur = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        // Tạo MỚI hoặc CẬP NHẬT asset cùng tên (không tạo trùng).
        private static void CreateShop(string fileName, string shopName, ShopDefinition.AccessMode mode,
            List<string> buy = null, List<string> sell = null)
        {
            string path = $"{FolderPath}/{fileName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ShopDefinition>(path);
            bool isNew = asset == null;
            if (isNew) asset = ScriptableObject.CreateInstance<ShopDefinition>();

            asset.shopName = shopName;
            asset.accessMode = mode;
            asset.buyItemIds = buy ?? new List<string>();
            asset.sellItemIds = sell ?? new List<string>();

            if (isNew) AssetDatabase.CreateAsset(asset, path);
            else EditorUtility.SetDirty(asset);
        }
    }
}
