using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using YWonderLand.Data;
using static YWonderLand.Core.GameTimeConfig; // Days()/Hours() — quy đổi thời gian thật

namespace YWonderLand.EditorScripts
{
    [InitializeOnLoad]
    public class ItemDataGenerator
    {
        static ItemDataGenerator()
        {
            EditorApplication.delayCall += GenerateOnce;
        }

        private static void GenerateOnce()
        {
            if (EditorPrefs.GetBool("YW_MockDataGenerated_v4", false)) return;
            EditorPrefs.SetBool("YW_MockDataGenerated_v4", true);
            
            GenerateItems();
        }

        [MenuItem("Y WONDER GREEN FARM/Tools/Generate Mock Items")]
        public static void GenerateItems()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Items"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Items");
            }

            ItemDatabase db = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Resources/ItemDatabase.asset");
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<ItemDatabase>();
                AssetDatabase.CreateAsset(db, "Assets/Resources/ItemDatabase.asset");
            }
            else
            {
                db.items.Clear();
            }

            // Hạt giống (8 loại theo kịch bản)
            AddItem(db, "carrot_seed_01", "H\u1EA1t c\u00E0 r\u1ED1t", "H\u1EA1t gi\u1ED1ng c\u00E0 r\u1ED1t. Thu ho\u1EA1ch sau 24h.", "\ud83e\udd55", "seeds", 3, 5, true);
            AddItem(db, "cabbage_seed_01", "H\u1EA1t c\u1EA3i", "H\u1EA1t gi\u1ED1ng rau c\u1EA3i xanh t\u01B0\u01A1i.", "\ud83e\udd6c", "seeds", 3, 7, true);
            AddItem(db, "watermelon_seed_01", "H\u1EA1t d\u01B0a h\u1EA5u", "H\u1EA1t gi\u1ED1ng d\u01B0a h\u1EA5u ng\u1ECDt.", "\ud83c\udf49", "seeds", 3, 15, true);
            AddItem(db, "corn_seed_01", "H\u1EA1t b\u1EAFp", "H\u1EA1t gi\u1ED1ng b\u1EAFp ng\u00F4 v\u00E0ng.", "\ud83c\udf3d", "seeds", 8, 10, true);
            AddItem(db, "pumpkin_seed_01", "H\u1EA1t b\u00ED ng\u00F4", "H\u1EA1t gi\u1ED1ng b\u00ED ng\u00F4 m\u00F9a event.", "\ud83c\udf83", "seeds", 7, 12, true);
            AddItem(db, "grass_seed_01", "C\u1ECF voi", "C\u1ECF voi l\u00E0m th\u1EE9c \u0103n cho v\u1EADt nu\u00F4i.", "\ud83c\udf3f", "seeds", 12, 2, true);
            AddItem(db, "morning_glory_seed_01", "H\u1EA1t rau mu\u1ED1ng", "H\u1EA1t gi\u1ED1ng rau mu\u1ED1ng (chu k\u1EF3 ng\u1EAFn).", "\ud83c\udf3e", "seeds", 4, 4, true);
            AddItem(db, "sweet_potato_seed_01", "D\u00E2y khoai lang", "D\u00E2y khoai lang gi\u1ED1ng.", "\ud83c\udf60", "seeds", 7, 6, true);

            // \u2500\u2500 H\u1ea1t gi\u1ED1ng C\u00c2Y L\u00c2U N\u0102M (10 c\u00E2y) \u2014 gi\u00e1 DEMO \u0111\u1ec3 test (gi\u00e1 th\u1eadt trong CayTrong.md, Phase 2).
            AddItem(db, "banana_seed_01", "Gi\u1ED1ng chu\u1ED1i", "Gi\u1ED1ng c\u00E2y chu\u1ED1i l\u00E2u n\u0103m.", "\ud83c\udf4c", "seeds", 200, 20, true);
            AddItem(db, "coconut_seed_01", "Gi\u1ED1ng d\u1eeba", "Gi\u1ED1ng c\u00E2y d\u1eeba.", "\ud83e\udd65", "seeds", 200, 20, true);
            AddItem(db, "areca_seed_01", "Gi\u1ED1ng cau", "Gi\u1ED1ng c\u00E2y cau.", "\ud83c\udf34", "seeds", 200, 20, true);
            AddItem(db, "date_seed_01", "Gi\u1ED1ng ch\u00e0 l\u00e0", "Gi\u1ED1ng c\u00E2y ch\u00e0 l\u00e0.", "\ud83c\udf34", "seeds", 300, 30, true);
            AddItem(db, "sacha_seed_01", "Gi\u1ED1ng Sa Chi", "Gi\u1ED1ng c\u00E2y d\u01b0\u1ee3c li\u1ec7u Sa Chi.", "\ud83c\udf30", "seeds", 7020, 30, true);
            AddItem(db, "tea_seed_01", "Gi\u1ED1ng tr\u00e0", "Gi\u1ED1ng c\u00E2y tr\u00e0.", "\ud83c\udf75", "seeds", 250, 25, true);
            AddItem(db, "durian_seed_01", "Gi\u1ED1ng s\u1ea7u ri\u00eang", "Gi\u1ED1ng c\u00E2y s\u1ea7u ri\u00eang cao c\u1ea5p.", "\ud83e\udd6d", "seeds", 18200, 50, true);
            AddItem(db, "asparagus_seed_01", "Gi\u1ED1ng m\u0103ng t\u00E2y", "Gi\u1ED1ng c\u00E2y m\u0103ng t\u00E2y.", "\ud83c\udf31", "seeds", 250, 25, true);
            AddItem(db, "red_ginseng_seed_01", "Gi\u1ED1ng h\u1ed3ng s\u00E2m", "Gi\u1ED1ng c\u00E2y h\u1ed3ng s\u00E2m qu\u00fd.", "\ud83c\udf3f", "seeds", 400, 40, true);
            AddItem(db, "royal_ginseng_seed_01", "Gi\u1ED1ng s\u00E2m ti\u1ebfn vua", "Gi\u1ED1ng s\u00E2m ti\u1ebfn vua qu\u00fd hi\u1ebfm.", "\ud83c\udf3f", "seeds", 800, 80, true);
            AddItem(db, "passion_fruit_seed_01", "Gi\u1ED1ng chanh leo", "Gi\u1ED1ng c\u00E2y chanh leo (chanh d\u00E2y) l\u00E2u n\u0103m.", "\ud83c\udf47", "seeds", 1560, 3, true); // kh\u00e1ch ch\u1ED1t 22/06: 26 Point (20 USDT/20 c\u00E2y)
            
            // Vật phẩm tiêu hao
            AddItem(db, "fertilizer_01", "Ph\u00E2n b\u00F3n", "Gi\u1EA3m 50% th\u1EDDi gian sinh tr\u01B0\u1EDFng.", "\ud83e\uddea", "items", 50, 25, true);
            AddItem(db, "vaccine_01", "V\u1EAFc-xin", "Ph\u00F2ng b\u1EC7nh 7 ng\u00E0y.", "\ud83d\udc89", "items", 30, 15, true);   // kh\u00E1ch ch\u1ED1t 22/06: gi\u00E1 mua 30 (theo VatNuoi)
            AddItem(db, "medicine_01", "Thu\u1ED1c tr\u1ECB", "Thu\u1ED1c \u0111i\u1EC1u tr\u1ECB v\u1EADt nu\u00F4i b\u1EC7nh.", "\ud83d\udc8a", "items", 70, 35, true);  // kh\u00E1ch ch\u1ED1t 22/06: gi\u00E1 mua 70 (theo VatNuoi)
            AddItem(db, "bait_01", "M\u1ED3i c\u00E2u", "T\u0103ng 20% c\u00E1 hi\u1EBFm.", "\ud83e\udeb1", "items", 20, 10, true);
            AddItem(db, "mine_ticket_01", "V\u00E9 \u0111\u00E0o m\u1ECF", "Th\u00EAm 5 l\u01B0\u1EE3t \u0111\u00E0o qu\u1EB7ng.", "\ud83c\udfab", "items", 100, 0, false);

            // Nông sản (8 loại tương ứng 8 seed)
            // N\u00F4ng s\u1EA3n NG\u1EAEN NG\u00C0Y = TH\u1EE8C \u0102N CH\u0102N NU\u00D4I, KH\u00D4NG b\u00E1n (kh\u00E1ch ch\u1ED1t 22/06): sellPrice 0 + canSell=false.
            AddItem(db, "carrot_01", "C\u00E0 r\u1ED1t", "Th\u1EE9c \u0103n ch\u0103n nu\u00F4i (kh\u00F4ng b\u00E1n).", "\ud83e\udd55", "food", 0, 0, false);
            AddItem(db, "cabbage_01", "B\u1EAFp c\u1EA3i", "Th\u1EE9c \u0103n ch\u0103n nu\u00F4i (kh\u00F4ng b\u00E1n).", "\ud83e\udd6c", "food", 0, 0, false);
            AddItem(db, "watermelon_01", "D\u01B0a h\u1EA5u", "Th\u1EE9c \u0103n ch\u0103n nu\u00F4i (kh\u00F4ng b\u00E1n).", "\ud83c\udf49", "food", 0, 0, false);
            AddItem(db, "corn_01", "B\u1EAFp ng\u00F4", "Th\u1EE9c \u0103n ch\u0103n nu\u00F4i (kh\u00F4ng b\u00E1n).", "\ud83c\udf3d", "food", 0, 0, false);
            AddItem(db, "pumpkin_01", "B\u00ED ng\u00F4", "Th\u1EE9c \u0103n ch\u0103n nu\u00F4i (kh\u00F4ng b\u00E1n).", "\ud83c\udf83", "food", 0, 0, false);
            AddItem(db, "morning_glory_01", "Rau mu\u1ED1ng", "Th\u1EE9c \u0103n ch\u0103n nu\u00F4i (kh\u00F4ng b\u00E1n).", "\ud83c\udf3e", "food", 0, 0, false);
            AddItem(db, "sweet_potato_01", "Khoai lang", "Th\u1EE9c \u0103n ch\u0103n nu\u00F4i (kh\u00F4ng b\u00E1n).", "\ud83c\udf60", "food", 0, 0, false);
            AddItem(db, "grass_01", "C\u1ECF Voi", "Th\u1EE9c \u0103n ch\u0103n nu\u00F4i (kh\u00F4ng b\u00E1n).", "\ud83c\udf3f", "food", 0, 0, false);
            
            // Sản phẩm chăn nuôi
            // S\u1EA3n ph\u1EA9m ch\u00EDnh (Pro1) \u2014 gi\u00E1 b\u00E1n theo c\u1ED9t "Gi\u00E1 Product 1" trong VatNuoi.md
            AddItem(db, "egg_01", "Tr\u1EE9ng g\u00E0", "Tr\u1EE9ng g\u00E0 ta.", "\ud83e\udd5a", "products", 0, 11, true);
            AddItem(db, "milk_01", "S\u1EEFa b\u00F2", "S\u1EEFa b\u00F2 t\u01B0\u01A1i.", "\ud83e\udd5b", "products", 0, 50, true); // kh\u00E1ch ch\u1ED1t 22/06: l\u1EA5y s\u1ED1 l\u1EDBn h\u01A1n (233 vs 305)
            AddItem(db, "pigskin_01", "Da heo", "Da heo thu\u1ED9c.", "\uD83D\uDC16", "products", 0, 7042, true);
            AddItem(db, "ostrich_egg_01", "Tr\u1EE9ng \u0111\u00E0 \u0111i\u1EC3u", "Tr\u1EE9ng \u0111\u00E0 \u0111i\u1EC3u kh\u1ED5ng l\u1ED3.", "\ud83e\udd5a", "products", 0, 409, true);
            AddItem(db, "deer_velvet_01", "Nhung h\u01B0\u01A1u", "Nhung h\u01B0\u01A1u qu\u00FD.", "\ud83e\uDD8C", "products", 0, 12368, true);
            AddItem(db, "goat_milk_01", "S\u1EEFa d\u00EA", "S\u1EEFa d\u00EA t\u01B0\u01A1i.", "\ud83e\udd5b", "products", 0, 12, true);
            AddItem(db, "rabbit_fur_01", "L\u00F4ng th\u1ECF", "L\u00F4ng th\u1ECF m\u1EC1m.", "\uD83D\uDC07", "products", 0, 21, true);
            AddItem(db, "goose_egg_01", "Tr\u1EE9ng ng\u1ED7ng", "Tr\u1EE9ng ng\u1ED7ng.", "\ud83e\udd5a", "products", 0, 14, true);
            AddItem(db, "duck_egg_01", "Tr\u1EE9ng v\u1ECBt", "Tr\u1EE9ng v\u1ECBt.", "\ud83e\udd5a", "products", 0, 5, true);
            AddItem(db, "turtle_shell_01", "Mai r\u00F9a", "Mai r\u00F9a c\u1EE9ng.", "\uD83D\uDC22", "products", 0, 11893, true);

            // S\u1EA3n ph\u1EA9m th\u1ECBt (Pro2 \u2014 v\u1EE5 cu\u1ED1i) \u2014 gi\u00E1 b\u00E1n theo c\u1ED9t "Gi\u00E1 product 2" trong VatNuoi.md
            AddItem(db, "pork_01", "Th\u1ECBt heo", "Th\u1ECBt heo t\u01B0\u01A1i.", "\ud83e\udd69", "products", 0, 292, true);
            AddItem(db, "chicken_meat_01", "Th\u1ECBt g\u00E0", "Th\u1ECBt g\u00E0 t\u01B0\u01A1i.", "\uD83C\uDF57", "products", 0, 310, true);
            AddItem(db, "beef_01", "Th\u1ECBt b\u00F2", "Th\u1ECBt b\u00F2 t\u01B0\u01A1i.", "\ud83e\udd69", "products", 0, 325, true);
            AddItem(db, "ostrich_meat_01", "Th\u1ECBt \u0111\u00E0 \u0111i\u1EC3u", "Th\u1ECBt \u0111\u00E0 \u0111i\u1EC3u.", "\uD83C\uDF57", "products", 0, 1050, true);
            AddItem(db, "deer_meat_01", "Th\u1ECBt h\u01B0\u01A1u", "Th\u1ECBt h\u01B0\u01A1u.", "\uD83C\uDF56", "products", 0, 933, true);
            AddItem(db, "goat_meat_01", "Th\u1ECBt d\u00EA", "Th\u1ECBt d\u00EA.", "\uD83C\uDF56", "products", 0, 118, true);
            AddItem(db, "rabbit_meat_01", "Th\u1ECBt th\u1ECF", "Th\u1ECBt th\u1ECF.", "\uD83C\uDF56", "products", 0, 289, true);
            AddItem(db, "goose_meat_01", "Th\u1ECBt ng\u1ED7ng", "Th\u1ECBt ng\u1ED7ng.", "\uD83C\uDF57", "products", 0, 665, true);
            AddItem(db, "duck_meat_01", "Th\u1ECBt v\u1ECBt", "Th\u1ECBt v\u1ECBt.", "\uD83C\uDF57", "products", 0, 332, true);
            AddItem(db, "turtle_meat_01", "Th\u1ECBt r\u00F9a", "Th\u1ECBt r\u00F9a.", "\uD83C\uDF56", "products", 0, 1084, true);

            // \u2500\u2500 S\u1EA3n ph\u1EA9m C\u00C2Y L\u00C2U N\u0102M (10) \u2014 gi\u00E1 b\u00E1n: Sa Chi/S\u1EA7u Ri\u00EAng theo CayTrong.md, c\u00F2n l\u1EA1i DEMO.
            AddItem(db, "banana_01", "Bu\u1ED3ng chu\u1ED1i", "Bu\u1ED3ng chu\u1ED1i ch\u00EDn.", "\uD83C\uDF4C", "products", 0, 80, true);
            AddItem(db, "coconut_01", "D\u1EEBa", "Tr\u00E1i d\u1EEBa t\u01B0\u01A1i.", "\uD83E\uDD65", "products", 0, 120, true);
            AddItem(db, "areca_01", "Tr\u00E1i cau", "Tr\u00E1i cau.", "\uD83C\uDF34", "products", 0, 100, true);
            AddItem(db, "date_01", "H\u1ED9p Ch\u00E0 L\u00E0", "H\u1ED9p ch\u00E0 l\u00E0 kh\u00F4.", "\uD83C\uDF34", "products", 0, 600, true);
            AddItem(db, "sacha_01", "H\u1ED9p Sa Chi", "H\u1ED9p h\u1EA1t Sa Chi d\u01B0\u1EE3c li\u1EC7u.", "\uD83C\uDF30", "products", 0, 194, true);
            AddItem(db, "tea_01", "T\u00FAi Tr\u00E0", "T\u00FAi l\u00E1 tr\u00E0.", "\uD83C\uDF75", "products", 0, 400, true);
            AddItem(db, "durian_01", "H\u1ED9p S\u1EA7u Ri\u00EAng", "H\u1ED9p s\u1EA7u ri\u00EAng cao c\u1EA5p.", "\uD83E\uDD6D", "products", 0, 619, true);
            AddItem(db, "asparagus_01", "B\u00FAp M\u0103ng T\u00E2y", "B\u00FAp m\u0103ng t\u00E2y t\u01B0\u01A1i.", "\uD83C\uDF31", "products", 0, 300, true);
            AddItem(db, "red_ginseng_01", "H\u1ED9p h\u1ED3ng s\u00E2m", "H\u1ED9p h\u1ED3ng s\u00E2m qu\u00FD.", "\uD83C\uDF3F", "products", 0, 2000, true);
            AddItem(db, "royal_ginseng_01", "H\u1ED9p S\u00E2m Ti\u1EBFn Vua", "S\u1EA3n ph\u1EA9m s\u00E2m cao c\u1EA5p nh\u1EA5t.", "\uD83C\uDF3F", "products", 0, 5000, true);
            AddItem(db, "passion_fruit_01", "H\u1ED9p m\u1EE9t chanh leo", "H\u1ED9p m\u1EE9t chanh leo (chanh d\u00E2y).", "\uD83C\uDF47", "products", 0, 57, true); // CayTrongLauNam2: ban 57
            
            // Vật liệu
            AddItem(db, "wood_01", "G\u1ED7", "G\u1ED7 ch\u1EB7t t\u1EEB c\u00E2y.", "\ud83e\udeb5", "materials", 0, 8, true);
            AddItem(db, "stone_01", "\u0110\u00E1", "\u0110\u00E1 \u0111\u00E0o t\u1EEB m\u1ECF.", "\ud83e\udea8", "materials", 0, 12, true);
            AddItem(db, "gem_kyanite_01", "Kyanite", "\u0110\u00E1 qu\u00FD Kyanite. B\u00E1n \u0111\u01B0\u1EE3c 2 Point.", "\uD83D\uDC8E", "materials", 0, 2, true);
            AddItem(db, "gem_orange_calcite_01", "Orange Calcite", "\u0110\u00E1 qu\u00FD Orange Calcite. B\u00E1n \u0111\u01B0\u1EE3c 3 Point.", "\uD83D\uDC8E", "materials", 0, 3, true);
            AddItem(db, "gem_green_calcite_01", "Green Calcite", "\u0110\u00E1 qu\u00FD Green Calcite. B\u00E1n \u0111\u01B0\u1EE3c 6 Point.", "\uD83D\uDC8E", "materials", 0, 6, true);
            AddItem(db, "gem_fire_quartz_01", "Fire Quartz", "\u0110\u00E1 qu\u00FD Fire Quartz. B\u00E1n \u0111\u01B0\u1EE3c 12 Point.", "\uD83D\uDC8E", "materials", 0, 12, true);
            AddItem(db, "gem_amethyst_01", "Amethyst", "\u0110\u00E1 qu\u00FD Amethyst. B\u00E1n \u0111\u01B0\u1EE3c 500 Point.", "\uD83D\uDC8E", "materials", 0, 500, true);
            AddItem(db, "gem_ruby_01", "Ruby qu\u00FD hi\u1EBFm", "Ruby qu\u00FD hi\u1EBFm. B\u00E1n \u0111\u01B0\u1EE3c 3000 Point.", "\uD83D\uDC8E", "materials", 0, 3000, true);
            AddItem(db, "iron_01", "S\u1EAFt", "Qu\u1EB7ng s\u1EAFt.", "\u26d3", "materials", 0, 50, true);
            AddItem(db, "ore_01", "Qu\u1EB7ng", "Qu\u1EB7ng th\u00F4 t\u1EEB m\u1ECF.", "\u2699", "materials", 0, 30, true);
            AddItem(db, "brick_01", "G\u1EA1ch", "G\u1EA1ch nung v\u1EEFng ch\u1EAFc.", "\ud83e\uddf1", "materials", 0, 10, true);
            
            // Công cụ
            AddItem(db, "hoe_01", "Cuốc Lv1", "Dụng cụ cuốc đất.", "⛏", "tools", 100, 50, true);
            AddItem(db, "pickaxe_01", "Cuốc chim", "Cuốc chim đập đá.", "⛏️", "tools", 100, 50, true);
            AddItem(db, "fishing_rod_01", "Cần câu", "Cần câu bằng tre.", "🎣", "tools", 100, 50, true);
            AddItem(db, "axe_01", "Rìu gỗ", "Rìu gỗ đốn củi.", "🪓", "tools", 100, 50, true);
            AddItem(db, "watering_can_01", "Xô tưới", "Xô đựng nước làm vườn.", "🪣", "tools", 100, 50, true);
            AddItem(db, "watering_water_01", "Nước tưới", "Xô nước múc từ ao trên đảo. Mỗi lần tưới cây tốn 1 xô.", "💧", "materials", 0, 0, false);
            
            // Thực phẩm
            AddItem(db, "bread_01", "B\u00E1nh m\u00EC", "B\u00E1nh m\u00EC th\u01A1m ngon.", "\ud83c\udf5e", "food", 20, 5, true);
            AddItem(db, "apple_01", "T\u00E1o \u0111\u1ECF", "T\u00E1o ch\u00EDn \u0111\u1ECF ng\u1ECDt l\u1ECBm.", "\ud83c\udf4e", "food", 10, 2, true);

            // Vật nuôi (mua tại shop) — GIÁ MUA (Point) = USDT × 26 (theo VatNuoi2.xlsx, áp chính thức 22/06).
            AddItem(db, "chicken_01", "Gà", "Gà ta đẻ trứng.", "🐔", "animals", 156, 0, false);
            AddItem(db, "rabbit_01", "Thỏ", "Thỏ con dễ thương.", "🐰", "animals", 130, 0, false);
            AddItem(db, "ostrich_01", "Đà điểu", "Đà điểu cho trứng lớn.", "🦤", "animals", 4420, 0, false);
            AddItem(db, "goat_01", "Dê", "Dê cho sữa.", "🐐", "animals", 1300, 0, false);
            AddItem(db, "cow_01", "Bò", "Bò sữa.", "🐄", "animals", 7800, 0, false);
            AddItem(db, "deer_01", "Hươu", "Hươu cho nhung quý.", "🦌", "animals", 10400, 0, false);
            AddItem(db, "pig_01", "Heo", "Heo thịt.", "🐷", "animals", 2600, 0, false);
            AddItem(db, "duck_01", "Vịt", "Vịt cho trứng.", "🦆", "animals", 208, 0, false);
            AddItem(db, "goose_01", "Ngỗng", "Ngỗng cho trứng to.", "🦢", "animals", 260, 0, false);
            AddItem(db, "turtle_01", "Rùa", "Rùa cho mai quý.", "🐢", "animals", 2340, 0, false);

            // Câu cá
            AddItem(db, "fish_01", "Cá chép", "Cá chép tươi sống.", "🐟", "food", 0, 50, true);
            AddItem(db, "fish_02", "Cá hiếm", "Loài cá quý hiếm.", "🐡", "food", 0, 200, true);
            AddItem(db, "fish_ca_com_01", "C\u00e1 c\u01a1m", "C\u00e1 c\u01a1m t\u01b0\u01a1i. B\u00e1n \u0111\u01b0\u1ee3c 2 Point.", "\ud83d\udc1f", "food", 0, 2, true);
            AddItem(db, "fish_ca_nuc_01", "C\u00e1 n\u1ee5c", "C\u00e1 n\u1ee5c t\u01b0\u01a1i. B\u00e1n \u0111\u01b0\u1ee3c 2 Point.", "\ud83d\udc1f", "food", 0, 2, true);
            AddItem(db, "fish_ca_hong_01", "C\u00e1 h\u1ed3ng", "C\u00e1 h\u1ed3ng t\u01b0\u01a1i. B\u00e1n \u0111\u01b0\u1ee3c 2 Point.", "\ud83d\udc1f", "food", 0, 2, true);
            AddItem(db, "fish_ca_su_tu_01", "C\u00e1 s\u01b0 t\u1eed", "C\u00e1 s\u01b0 t\u1eed. B\u00e1n \u0111\u01b0\u1ee3c 4 Point.", "\ud83d\udc1f", "food", 0, 4, true);
            AddItem(db, "fish_ca_naso_01", "C\u00e1 naso", "C\u00e1 naso. B\u00e1n \u0111\u01b0\u1ee3c 4 Point.", "\ud83d\udc1f", "food", 0, 4, true);
            AddItem(db, "fish_ca_nhong_01", "C\u00e1 nh\u1ed3ng", "C\u00e1 nh\u1ed3ng. B\u00e1n \u0111\u01b0\u1ee3c 4 Point.", "\ud83d\udc1f", "food", 0, 4, true);
            AddItem(db, "fish_ca_soc_dua_01", "C\u00e1 s\u1ecdc d\u01b0a", "C\u00e1 s\u1ecdc d\u01b0a. B\u00e1n \u0111\u01b0\u1ee3c 6 Point.", "\ud83d\udc1f", "food", 0, 6, true);
            AddItem(db, "fish_ca_khe_01", "C\u00e1 kh\u1ebf", "C\u00e1 kh\u1ebf. B\u00e1n \u0111\u01b0\u1ee3c 6 Point.", "\ud83d\udc1f", "food", 0, 6, true);
            AddItem(db, "fish_ca_mu_01", "C\u00e1 m\u00fa", "C\u00e1 m\u00fa. B\u00e1n \u0111\u01b0\u1ee3c 6 Point.", "\ud83d\udc1f", "food", 0, 6, true);
            AddItem(db, "fish_ca_mat_quy_01", "C\u00e1 m\u1eb7t qu\u1ef7", "C\u00e1 m\u1eb7t qu\u1ef7. B\u00e1n \u0111\u01b0\u1ee3c 10 Point.", "\ud83d\udc1f", "food", 0, 10, true);
            AddItem(db, "fish_ca_heo_bien_01", "C\u00e1 heo bi\u1ec3n", "C\u00e1 heo bi\u1ec3n. B\u00e1n \u0111\u01b0\u1ee3c 10 Point.", "\ud83d\udc1f", "food", 0, 10, true);
            AddItem(db, "fish_ca_hoang_de_01", "C\u00e1 ho\u00e0ng \u0111\u1ebf", "C\u00e1 ho\u00e0ng \u0111\u1ebf. B\u00e1n \u0111\u01b0\u1ee3c 15 Point.", "\ud83d\udc1f", "food", 0, 15, true);
            AddItem(db, "fish_ca_ngu_hoang_kim_01", "C\u00e1 ng\u1eeb ho\u00e0ng kim", "C\u00e1 ng\u1eeb ho\u00e0ng kim. B\u00e1n \u0111\u01b0\u1ee3c 15 Point.", "\ud83d\udc1f", "food", 0, 15, true);
            AddItem(db, "fish_ca_rong_do_01", "C\u00e1 r\u1ed3ng \u0111\u1ecf", "C\u00e1 r\u1ed3ng \u0111\u1ecf. B\u00e1n \u0111\u01b0\u1ee3c 25 Point.", "\ud83d\udc1f", "food", 0, 25, true);
            AddItem(db, "gift_box_01", "Hộp quà", "Hộp quà sự kiện từ đại dương.", "🎁", "items", 0, 500, true);

            AssignIconTextures(db);

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            
            Debug.Log("[Mock Data] Generated ItemDatabase with " + db.items.Count + " items!");
        }

        private static void AddItem(ItemDatabase db, string id, string name, string desc, string emoji, string cat, int buyP, int sellP, bool canSell)
        {
            string path = $"Assets/Resources/Items/{id}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                item.id = id;
                AssetDatabase.CreateAsset(item, path);
            }
            
            item.itemName = name;
            item.description = desc;
            item.iconEmoji = emoji;
            item.category = cat;
            item.buyPrice = buyP;
            item.sellPrice = sellP;
            item.canSell = canSell;

            EditorUtility.SetDirty(item);
            db.items.Add(item);
        }

        private static void AssignIconTextures(ItemDatabase db)
        {
            if (db == null) return;

            var iconPaths = new Dictionary<string, string>
            {
                // Animals
                ["chicken_01"] = "Assets/_Project/UI/Sprites/icon/animals/chicken.png",
                ["cow_01"] = "Assets/_Project/UI/Sprites/icon/animals/cow.png",
                ["deer_01"] = "Assets/_Project/UI/Sprites/icon/animals/deer.png",
                ["duck_01"] = "Assets/_Project/UI/Sprites/icon/animals/duck.png",
                ["goose_01"] = "Assets/_Project/UI/Sprites/icon/animals/food.png",
                ["goat_01"] = "Assets/_Project/UI/Sprites/icon/animals/goat.png",
                ["ostrich_01"] = "Assets/_Project/UI/Sprites/icon/animals/ostrich.png",
                ["pig_01"] = "Assets/_Project/UI/Sprites/icon/animals/pig.png",
                ["rabbit_01"] = "Assets/_Project/UI/Sprites/icon/animals/rabbit.png",
                ["turtle_01"] = "Assets/_Project/UI/Sprites/icon/animals/turtle.png",

                // Short crop seeds
                ["cabbage_seed_01"] = "Assets/_Project/UI/Sprites/icon/seed/cabbage.png",
                ["carrot_seed_01"] = "Assets/_Project/UI/Sprites/icon/seed/carrrot.png",
                ["corn_seed_01"] = "Assets/_Project/UI/Sprites/icon/seed/corn.png",
                ["morning_glory_seed_01"] = "Assets/_Project/UI/Sprites/icon/seed/glory.png",
                ["grass_seed_01"] = "Assets/_Project/UI/Sprites/icon/seed/grass.png",
                ["watermelon_seed_01"] = "Assets/_Project/UI/Sprites/icon/seed/melon.png",
                ["pumpkin_seed_01"] = "Assets/_Project/UI/Sprites/icon/seed/pumpkin.png",
                ["sweet_potato_seed_01"] = "Assets/_Project/UI/Sprites/icon/seed/sweet_potato.png",

                // Short crop products / feed
                ["cabbage_01"] = "Assets/_Project/UI/Sprites/icon/products/bap cai.png",
                ["corn_01"] = "Assets/_Project/UI/Sprites/icon/products/bap ngo.png",
                ["pumpkin_01"] = "Assets/_Project/UI/Sprites/icon/products/bi ngo.png",
                ["carrot_01"] = "Assets/_Project/UI/Sprites/icon/products/ca rot.png",
                ["grass_01"] = "Assets/_Project/UI/Sprites/icon/products/co voi.png",
                ["watermelon_01"] = "Assets/_Project/UI/Sprites/icon/products/dua hau.png",
                ["sweet_potato_01"] = "Assets/_Project/UI/Sprites/icon/products/giong khoai lang.png",
                ["morning_glory_01"] = "Assets/_Project/UI/Sprites/icon/products/rau muong.png",

                // Perennial crop seeds
                ["banana_seed_01"] = "Assets/_Project/UI/Sprites/icon/trees/caychuoi.png",
                ["coconut_seed_01"] = "Assets/_Project/UI/Sprites/icon/trees/coconut-tree.png",
                ["areca_seed_01"] = "Assets/_Project/UI/Sprites/icon/trees/cay cau.png",
                ["date_seed_01"] = "Assets/_Project/UI/Sprites/icon/trees/cay cha la.png",
                ["sacha_seed_01"] = "Assets/_Project/UI/Sprites/icon/trees/cay sa chi.png",
                ["tea_seed_01"] = "Assets/_Project/UI/Sprites/icon/trees/cay tra.png",
                ["durian_seed_01"] = "Assets/_Project/UI/Sprites/icon/trees/cay sau rieng.png",
                ["asparagus_seed_01"] = "Assets/_Project/UI/Sprites/icon/trees/cay mang tay.png",
                ["red_ginseng_seed_01"] = "Assets/_Project/UI/Sprites/icon/trees/cay hong sam.png",
                ["royal_ginseng_seed_01"] = "Assets/_Project/UI/Sprites/icon/trees/sam tien vua.png",
                ["passion_fruit_seed_01"] = "Assets/_Project/UI/Sprites/icon/trees/cay chanh leo.png",

                // Perennial boxed products
                ["sacha_01"] = "Assets/_Project/UI/Sprites/icon/boxes/sacha_inchi_box.png",
                ["durian_01"] = "Assets/_Project/UI/Sprites/icon/boxes/durian_box.png",
                ["passion_fruit_01"] = "Assets/_Project/UI/Sprites/icon/boxes/passion_box.png",

                // New product icons from artist (Assets/Sprites/icon/SanPham)
                ["banana_01"] = "Assets/Sprites/icon/SanPham/CayLauNam/Chuoi.png",
                ["coconut_01"] = "Assets/Sprites/icon/SanPham/CayLauNam/Dua.png",
                ["areca_01"] = "Assets/Sprites/icon/SanPham/CayLauNam/QuaCau.png",
                ["date_01"] = "Assets/Sprites/icon/SanPham/CayLauNam/ChaLa.png",
                ["tea_01"] = "Assets/Sprites/icon/SanPham/CayLauNam/Tra.png",
                ["asparagus_01"] = "Assets/Sprites/icon/SanPham/CayLauNam/MangTay.png",
                ["red_ginseng_01"] = "Assets/Sprites/icon/SanPham/CayLauNam/HongSam.png",
                ["royal_ginseng_01"] = "Assets/Sprites/icon/SanPham/CayLauNam/SamTienVua.png",

                ["apple_01"] = "Assets/Sprites/icon/SanPham/DoAn/Tao.png",
                ["bread_01"] = "Assets/Sprites/icon/SanPham/DoAn/BanhMi.png",
                ["fish_01"] = "Assets/Sprites/icon/SanPham/DoAn/CaChep.png",
                ["fish_02"] = "Assets/Sprites/icon/SanPham/DoAn/CaHiem.png",
                ["fish_ca_com_01"] = "Assets/Sprites/icon/CacLoaiCa/CaCom.png",
                ["fish_ca_nuc_01"] = "Assets/Sprites/icon/CacLoaiCa/CaNuc.png",
                ["fish_ca_hong_01"] = "Assets/Sprites/icon/CacLoaiCa/CaHong.png",
                ["fish_ca_su_tu_01"] = "Assets/Sprites/icon/CacLoaiCa/CaSuTu.png",
                ["fish_ca_naso_01"] = "Assets/Sprites/icon/CacLoaiCa/CaNaso.png",
                ["fish_ca_nhong_01"] = "Assets/Sprites/icon/CacLoaiCa/CaNhong.png",
                ["fish_ca_soc_dua_01"] = "Assets/Sprites/icon/CacLoaiCa/CaSocDua.png",
                ["fish_ca_khe_01"] = "Assets/Sprites/icon/CacLoaiCa/CaKhe.png",
                ["fish_ca_mu_01"] = "Assets/Sprites/icon/CacLoaiCa/CaMu.png",
                ["fish_ca_mat_quy_01"] = "Assets/Sprites/icon/CacLoaiCa/CaMatQuy.png",
                ["fish_ca_heo_bien_01"] = "Assets/Sprites/icon/CacLoaiCa/CaHeoBien.png",
                ["fish_ca_hoang_de_01"] = "Assets/Sprites/icon/CacLoaiCa/CaHoangDe.png",
                ["fish_ca_ngu_hoang_kim_01"] = "Assets/Sprites/icon/CacLoaiCa/CaNguHoangKim.png",
                ["fish_ca_rong_do_01"] = "Assets/Sprites/icon/CacLoaiCa/CaRongDo.png",

                ["egg_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/TrungGa.png",
                ["milk_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/SuaBo.png",
                ["pigskin_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/DaHeo.png",
                ["ostrich_egg_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/TrungDaDieu.png",
                ["deer_velvet_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/NhungHuou.png",
                ["goat_milk_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/SuaDe.png",
                ["rabbit_fur_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/LongTho.png",
                ["goose_egg_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/TrungNgong.png",
                ["duck_egg_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/TrungVit.png",
                ["turtle_shell_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/MaiRua.png",
                ["pork_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/ThitHeo.png",
                ["beef_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/ThitBo.png",
                ["deer_meat_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/ThitHuou.png",
                ["goat_meat_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/ThitDe.png",
                ["rabbit_meat_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/ThitTho.png",
                ["turtle_meat_01"] = "Assets/Sprites/icon/SanPham/SanPhamVatNuoi/ThitRua.png",
                ["chicken_meat_01"] = "Assets/Sprites/icon/ThitGiaCam/ThitGa.png",
                ["ostrich_meat_01"] = "Assets/Sprites/icon/ThitGiaCam/ThitDaDieu.png",
                ["goose_meat_01"] = "Assets/Sprites/icon/ThitGiaCam/ThitNgong.png",
                ["duck_meat_01"] = "Assets/Sprites/icon/ThitGiaCam/ThitVit.png",

                ["fertilizer_01"] = "Assets/Sprites/icon/SanPham/VatPham/fertilizer.png",
                ["vaccine_01"] = "Assets/Sprites/icon/SanPham/VatPham/syringe.png",
                ["medicine_01"] = "Assets/Sprites/icon/SanPham/VatPham/bottle.png",
                ["bait_01"] = "Assets/Sprites/icon/SanPham/VatPham/worm.png",
                ["gift_box_01"] = "Assets/Sprites/icon/SanPham/VatPham/giftbox.png",
                ["mine_ticket_01"] = "Assets/Sprites/icon/SanPham/VatPham/tickets.png",

                ["gem_kyanite_01"] = "Assets/Sprites/icon/CacLoaiDaQuy/Kyanite[1].png",
                ["gem_orange_calcite_01"] = "Assets/Sprites/icon/CacLoaiDaQuy/OrangeCalcite[2].png",
                ["gem_green_calcite_01"] = "Assets/Sprites/icon/CacLoaiDaQuy/GreenCalcite[3].png",
                ["gem_fire_quartz_01"] = "Assets/Sprites/icon/CacLoaiDaQuy/FireQuartz[4].png",
                ["gem_amethyst_01"] = "Assets/Sprites/icon/CacLoaiDaQuy/Amethyst[5].png",
                ["gem_ruby_01"] = "Assets/Sprites/icon/CacLoaiDaQuy/Ruby[6].png",
            };

            foreach (var item in db.items)
            {
                if (item == null || string.IsNullOrEmpty(item.id)) continue;
                if (!iconPaths.TryGetValue(item.id, out string path)) continue;

                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null)
                {
                    Debug.LogWarning($"[Mock Data] Missing icon texture for '{item.id}': {path}");
                    continue;
                }

                item.iconTexture = texture;
                EditorUtility.SetDirty(item);
            }
        }

        [MenuItem("YWonderLand/Generate Animal Data")]
        public static void GenerateAnimalData()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Items"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Items");
            }

            // Thông tin chăn nuôi (giá/ô/thức ăn/sản phẩm) — nguồn: bảng VatNuoi khách gửi.
            // Chỉ set trường hiển thị + tên/giá; GIỮ nguyên field gameplay (cycle, produceItemId) nếu asset đã có.
            SetHusbandry("chicken_01", "Gà mái V2", 156, 1, "Bắp Ngô", 2, "Cám", 0, "Trứng gà", 1, "Thịt gà", 5);
            SetHusbandry("cow_01", "Bò sữa", 7800, 9, "Cỏ Voi", 2, "Khoai Lang", 4, "Sữa bò", 10, "Thịt bò", 50);
            SetHusbandry("pig_01", "Heo con", 2600, 9, "Khoai lang", 2, "Bí ngô", 2, "Da heo", 1, "Thịt heo", 50);
            SetHusbandry("ostrich_01", "Đà điểu V2", 4420, 1, "Dưa Hấu", 4, "Cám", 0, "Trứng đà điểu", 1, "Thịt đà điểu", 20);
            SetHusbandry("deer_01", "Hươu", 10400, 9, "Bắp Ngô", 5, "Cám", 0, "Nhung hươu", 2, "Thịt hươu", 40);
            SetHusbandry("goat_01", "Dê con V2", 1300, 9, "Bí ngô", 2, "Cỏ voi", 2, "Sữa dê", 2, "Thịt dê", 20);
            SetHusbandry("rabbit_01", "Thỏ con V2", 130, 1, "Cà rốt", 1, "Bắp ngô", 1, "Lông thỏ", 8, "Thịt thỏ", 5);
            SetHusbandry("goose_01", "Ngỗng con V2", 260, 1, "Bắp Cải", 2, "Bắp Ngô", 3, "Trứng ngỗng", 2, "Thịt ngỗng", 5);
            SetHusbandry("duck_01", "Vịt V3", 208, 1, "Bắp Ngô", 1, "Cám", 0, "Trứng vịt", 1, "Thịt vịt", 5);
            SetHusbandry("turtle_01", "Rùa con", 2340, 1, "Rau Muống", 7, "Dưa hấu", 12, "Mai rùa", 1, "Thịt rùa", 10);

            // Logic gameplay theo VatNuoi: produceId, SL sản phẩm/vụ (Pro1), TỔNG SỐ LẦN THU, thịt vụ cuối (Pro2), SL thịt.
            // produceCycle/feed để giây DEMO (số ngày thật chờ khách quy đổi).  (cycle 25s, feed 40s)
            // Chu kỳ thu / cho ăn = NGÀY THẬT theo VatNuoi (quy đổi qua Days()). Con chu kỳ dài (heo/hươu/rùa)
            // là vật nuôi "đầu tư dài hạn" — demo chỉnh nhanh bằng SecondsPerGameDay nếu cần.
            SetAnimalGameplay("chicken_01", "egg_01", 1, 45, 3000, "chicken_meat_01", 5, Days(2f), Days(1f), Days(1f), Days(2f)); // gia cầm: trứng theo chu kỳ, thịt ở vụ cuối (khách đổi lại 29/06)
            SetAnimalGameplay("cow_01", "milk_01", 10, 38, 10000, "beef_01", 50, Days(7f), Days(1f), Days(1f), Days(2f));
            SetAnimalGameplay("pig_01", "pigskin_01", 1, 1, 8000, "pork_01", 50, Days(180f), Days(1f), Days(1f), Days(2f));
            SetAnimalGameplay("ostrich_01", "ostrich_egg_01", 1, 30, 5000, "ostrich_meat_01", 20, Days(6f), Days(1f), Days(1f), Days(2f)); // gia cầm: trứng theo chu kỳ, thịt ở vụ cuối
            SetAnimalGameplay("deer_01", "deer_velvet_01", 2, 2, 15000, "deer_meat_01", 40, Days(180f), Days(1f), Days(1f), Days(2f));
            SetAnimalGameplay("goat_01", "goat_milk_01", 2, 60, 8000, "goat_meat_01", 20, Days(3f), Days(1f), Days(1f), Days(2f));
            SetAnimalGameplay("rabbit_01", "rabbit_fur_01", 8, 2, 1500, "rabbit_meat_01", 5, Days(40f), Days(1f), Days(1f), Days(2f));
            SetAnimalGameplay("goose_01", "goose_egg_01", 2, 30, 4500, "goose_meat_01", 5, Days(3f), Days(1f), Days(1f), Days(2f)); // gia cầm: trứng theo chu kỳ, thịt ở vụ cuối
            SetAnimalGameplay("duck_01", "duck_egg_01", 1, 45, 1000, "duck_meat_01", 5, Days(1f), Days(0.5f), Days(1f), Days(2f)); // gia cầm: trứng theo chu kỳ, thịt ở vụ cuối (đói từ 12h = thanh 50%)
            SetAnimalGameplay("turtle_01", "turtle_shell_01", 1, 1, 20000, "turtle_meat_01", 10, Days(300f), Days(7f), Days(5f), Days(10f)); // rùa: chưa ăn 5 ngày chết / cho ăn 10 ngày

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AnimalData] Generated/updated 10 animal definitions (kèm thông tin chăn nuôi + logic VatNuoi)!");
        }

        // Set logic gameplay theo VatNuoi (giữ nguyên phần hiển thị do SetHusbandry set trước đó).
        private static void SetAnimalGameplay(string id, string produceId, int produceAmt, int maxHarv, int expReward,
            string meatId, int meatAmt, float cycle, float feed, float noFeedDeath, float fedLife)
        {
            string path = "Assets/Resources/Items/Animal_" + id + ".asset";
            var a = AssetDatabase.LoadAssetAtPath<YWonderLand.Data.AnimalDefinition>(path);
            if (a == null)
            {
                Debug.LogWarning($"[AnimalData] Chưa có asset Animal_{id} để set gameplay (SetHusbandry phải chạy trước).");
                return;
            }
            a.produceItemId = produceId;
            a.produceAmount = produceAmt;
            a.maxHarvests = maxHarv;
            a.expReward = expReward;
            a.meatItemId = meatId;
            a.meatAmount = meatAmt;
            a.produceCycleTimeSec = cycle;
            a.feedIntervalSec = feed;
            a.noFeedDeathSec = noFeedDeath; // chưa cho ăn sống bao lâu (24h / rùa 5 ngày)
            a.fedLifeSec = fedLife;         // cho ăn 1 lần sống bao lâu (48h / rùa 10 ngày)
            a.canGetSick = true;
            EditorUtility.SetDirty(a);
        }

        // Set CHỈ thông tin hiển thị chăn nuôi + tên/giá; giữ field gameplay cũ nếu asset đã tồn tại.
        private static void SetHusbandry(string id, string name, int price, int slots,
            string foodMain, int foodMainAmt, string foodAlt, int foodAltAmt,
            string prodMain, int prodMainAmt, string prodAlt, int prodAltAmt)
        {
            string path = "Assets/Resources/Items/Animal_" + id + ".asset";
            var animal = AssetDatabase.LoadAssetAtPath<YWonderLand.Data.AnimalDefinition>(path);
            if (animal == null)
            {
                animal = ScriptableObject.CreateInstance<YWonderLand.Data.AnimalDefinition>();
                animal.animalId = id;
                AssetDatabase.CreateAsset(animal, path);
            }
            animal.animalName = name;
            animal.buyPrice = price;
            animal.penSlots = slots;
            animal.foodMainName = foodMain; animal.foodMainAmount = foodMainAmt;
            animal.foodAltName = foodAlt; animal.foodAltAmount = foodAltAmt;
            animal.productMainName = prodMain; animal.productMainAmount = prodMainAmt;
            animal.productAltName = prodAlt; animal.productAltAmount = prodAltAmt;
            EditorUtility.SetDirty(animal);
        }

        private static void CreateAnimalEntry(string id, string name, int price, float cycle, string prodId, int prodAmt, int maxHarv, float feed, bool sick)
        {
            string path = "Assets/Resources/Items/Animal_" + id + ".asset";
            var animal = AssetDatabase.LoadAssetAtPath<YWonderLand.Data.AnimalDefinition>(path);
            if (animal == null)
            {
                animal = ScriptableObject.CreateInstance<YWonderLand.Data.AnimalDefinition>();
                AssetDatabase.CreateAsset(animal, path);
            }
            animal.animalId = id;
            animal.animalName = name;
            animal.buyPrice = price;
            animal.produceCycleTimeSec = cycle;
            animal.produceItemId = prodId;
            animal.produceAmount = prodAmt;
            animal.maxHarvests = maxHarv;
            animal.feedIntervalSec = feed;
            animal.canGetSick = sick;
            EditorUtility.SetDirty(animal);
        }
    }
}
