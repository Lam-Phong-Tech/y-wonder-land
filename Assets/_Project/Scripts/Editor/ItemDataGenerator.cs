using UnityEngine;
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
            AddItem(db, "carrot_seed_01", "H\u1EA1t c\u00E0 r\u1ED1t", "H\u1EA1t gi\u1ED1ng c\u00E0 r\u1ED1t. Thu ho\u1EA1ch sau 24h.", "\ud83e\udd55", "seeds", 12, 5, true);
            AddItem(db, "cabbage_seed_01", "H\u1EA1t c\u1EA3i", "H\u1EA1t gi\u1ED1ng rau c\u1EA3i xanh t\u01B0\u01A1i.", "\ud83e\udd6c", "seeds", 17, 7, true);
            AddItem(db, "watermelon_seed_01", "H\u1EA1t d\u01B0a h\u1EA5u", "H\u1EA1t gi\u1ED1ng d\u01B0a h\u1EA5u ng\u1ECDt.", "\ud83c\udf49", "seeds", 17, 15, true);
            AddItem(db, "corn_seed_01", "H\u1EA1t b\u1EAFp", "H\u1EA1t gi\u1ED1ng b\u1EAFp ng\u00F4 v\u00E0ng.", "\ud83c\udf3d", "seeds", 35, 10, true);
            AddItem(db, "pumpkin_seed_01", "H\u1EA1t b\u00ED ng\u00F4", "H\u1EA1t gi\u1ED1ng b\u00ED ng\u00F4 m\u00F9a event.", "\ud83c\udf83", "seeds", 29, 12, true);
            AddItem(db, "grass_seed_01", "C\u1ECF voi", "C\u1ECF voi l\u00E0m th\u1EE9c \u0103n cho v\u1EADt nu\u00F4i.", "\ud83c\udf3f", "seeds", 65, 2, true);
            AddItem(db, "morning_glory_seed_01", "H\u1EA1t rau mu\u1ED1ng", "H\u1EA1t gi\u1ED1ng rau mu\u1ED1ng (chu k\u1EF3 ng\u1EAFn).", "\ud83c\udf3e", "seeds", 20, 4, true);
            AddItem(db, "sweet_potato_seed_01", "D\u00E2y khoai lang", "D\u00E2y khoai lang gi\u1ED1ng.", "\ud83c\udf60", "seeds", 38, 6, true);

            // \u2500\u2500 H\u1ea1t gi\u1ED1ng C\u00c2Y L\u00c2U N\u0102M (10 c\u00E2y) \u2014 gi\u00e1 DEMO \u0111\u1ec3 test (gi\u00e1 th\u1eadt trong CayTrong.md, Phase 2).
            AddItem(db, "banana_seed_01", "Gi\u1ED1ng chu\u1ED1i", "Gi\u1ED1ng c\u00E2y chu\u1ED1i l\u00E2u n\u0103m.", "\ud83c\udf4c", "seeds", 200, 20, true);
            AddItem(db, "coconut_seed_01", "Gi\u1ED1ng d\u1eeba", "Gi\u1ED1ng c\u00E2y d\u1eeba.", "\ud83e\udd65", "seeds", 200, 20, true);
            AddItem(db, "areca_seed_01", "Gi\u1ED1ng cau", "Gi\u1ED1ng c\u00E2y cau.", "\ud83c\udf34", "seeds", 200, 20, true);
            AddItem(db, "date_seed_01", "Gi\u1ED1ng ch\u00e0 l\u00e0", "Gi\u1ED1ng c\u00E2y ch\u00e0 l\u00e0.", "\ud83c\udf34", "seeds", 300, 30, true);
            AddItem(db, "sacha_seed_01", "Gi\u1ED1ng Sa Chi", "Gi\u1ED1ng c\u00E2y d\u01b0\u1ee3c li\u1ec7u Sa Chi.", "\ud83c\udf30", "seeds", 300, 30, true);
            AddItem(db, "tea_seed_01", "Gi\u1ED1ng tr\u00e0", "Gi\u1ED1ng c\u00E2y tr\u00e0.", "\ud83c\udf75", "seeds", 250, 25, true);
            AddItem(db, "durian_seed_01", "Gi\u1ED1ng s\u1ea7u ri\u00eang", "Gi\u1ED1ng c\u00E2y s\u1ea7u ri\u00eang cao c\u1ea5p.", "\ud83e\udd6d", "seeds", 500, 50, true);
            AddItem(db, "asparagus_seed_01", "Gi\u1ED1ng m\u0103ng t\u00E2y", "Gi\u1ED1ng c\u00E2y m\u0103ng t\u00E2y.", "\ud83c\udf31", "seeds", 250, 25, true);
            AddItem(db, "red_ginseng_seed_01", "Gi\u1ED1ng h\u1ed3ng s\u00E2m", "Gi\u1ED1ng c\u00E2y h\u1ed3ng s\u00E2m qu\u00fd.", "\ud83c\udf3f", "seeds", 400, 40, true);
            AddItem(db, "royal_ginseng_seed_01", "Gi\u1ED1ng s\u00E2m ti\u1ebfn vua", "Gi\u1ED1ng s\u00E2m ti\u1ebfn vua qu\u00fd hi\u1ebfm.", "\ud83c\udf3f", "seeds", 800, 80, true);
            
            // Vật phẩm tiêu hao
            AddItem(db, "fertilizer_01", "Ph\u00E2n b\u00F3n", "Gi\u1EA3m 50% th\u1EDDi gian sinh tr\u01B0\u1EDFng.", "\ud83e\uddea", "items", 50, 25, true);
            AddItem(db, "vaccine_01", "V\u1EAFc-xin", "Ph\u00F2ng b\u1EC7nh 7 ng\u00E0y.", "\ud83d\udc89", "items", 80, 40, true);
            AddItem(db, "medicine_01", "Thu\u1ED1c tr\u1ECB", "Thu\u1ED1c \u0111i\u1EC1u tr\u1ECB v\u1EADt nu\u00F4i b\u1EC7nh.", "\ud83d\udc8a", "items", 100, 50, true);
            AddItem(db, "bait_01", "M\u1ED3i c\u00E2u", "T\u0103ng 20% c\u00E1 hi\u1EBFm.", "\ud83e\udeb1", "items", 20, 10, true);
            AddItem(db, "mine_ticket_01", "V\u00E9 \u0111\u00E0o m\u1ECF", "Th\u00EAm 5 l\u01B0\u1EE3t \u0111\u00E0o qu\u1EB7ng.", "\ud83c\udfab", "items", 100, 0, false);

            // Nông sản (8 loại tương ứng 8 seed)
            AddItem(db, "carrot_01", "C\u00E0 r\u1ED1t", "C\u00E0 r\u1ED1t t\u01B0\u01A1i.", "\ud83e\udd55", "food", 0, 22, true);
            AddItem(db, "cabbage_01", "B\u1EAFp c\u1EA3i", "B\u1EAFp c\u1EA3i xanh.", "\ud83e\udd6c", "food", 0, 27, true);
            AddItem(db, "watermelon_01", "D\u01B0a h\u1EA5u", "D\u01B0a h\u1EA5u ng\u1ECDt.", "\ud83c\udf49", "food", 0, 27, true);
            AddItem(db, "corn_01", "B\u1EAFp ng\u00F4", "B\u1EAFp ng\u00F4 v\u00E0ng \u01B0\u01A1m. D\u00F9ng cho \u0111\u1ED9ng v\u1EADt \u0103n.", "\ud83c\udf3d", "food", 0, 15, true);
            AddItem(db, "pumpkin_01", "B\u00ED ng\u00F4", "B\u00ED ng\u00F4 to tr\u00F2n.", "\ud83c\udf83", "food", 0, 4, true);
            AddItem(db, "morning_glory_01", "Rau mu\u1ED1ng", "Rau mu\u1ED1ng xanh.", "\ud83c\udf3e", "food", 0, 30, true);
            AddItem(db, "sweet_potato_01", "Khoai lang", "Khoai lang ng\u1ECDt b\u00F9i.", "\ud83c\udf60", "food", 0, 48, true);
            AddItem(db, "grass_01", "C\u1ECF Voi", "C\u1ECF voi t\u01B0\u01A1i l\u00E0m th\u1EE9c \u0103n.", "\ud83c\udf3f", "food", 0, 38, true);
            
            // Sản phẩm chăn nuôi
            // S\u1EA3n ph\u1EA9m ch\u00EDnh (Pro1) \u2014 gi\u00E1 b\u00E1n theo c\u1ED9t "Gi\u00E1 Product 1" trong VatNuoi.md
            AddItem(db, "egg_01", "Tr\u1EE9ng g\u00E0", "Tr\u1EE9ng g\u00E0 ta.", "\ud83e\udd5a", "products", 0, 28, true);
            AddItem(db, "milk_01", "S\u1EEFa b\u00F2", "S\u1EEFa b\u00F2 t\u01B0\u01A1i.", "\ud83e\udd5b", "products", 0, 233, true);
            AddItem(db, "pigskin_01", "Da heo", "Da heo thu\u1ED9c.", "\uD83D\uDC16", "products", 0, 25420, true);
            AddItem(db, "ostrich_egg_01", "Tr\u1EE9ng \u0111\u00E0 \u0111i\u1EC3u", "Tr\u1EE9ng \u0111\u00E0 \u0111i\u1EC3u kh\u1ED5ng l\u1ED3.", "\ud83e\udd5a", "products", 0, 1088, true);
            AddItem(db, "deer_velvet_01", "Nhung h\u01B0\u01A1u", "Nhung h\u01B0\u01A1u qu\u00FD.", "\ud83e\uDD8C", "products", 0, 99745, true);
            AddItem(db, "goat_milk_01", "S\u1EEFa d\u00EA", "S\u1EEFa d\u00EA t\u01B0\u01A1i.", "\ud83e\udd5b", "products", 0, 100, true);
            AddItem(db, "rabbit_fur_01", "L\u00F4ng th\u1ECF", "L\u00F4ng th\u1ECF m\u1EC1m.", "\uD83D\uDC07", "products", 0, 345, true);
            AddItem(db, "goose_egg_01", "Tr\u1EE9ng ng\u1ED7ng", "Tr\u1EE9ng ng\u1ED7ng.", "\ud83e\udd5a", "products", 0, 65, true);
            AddItem(db, "duck_egg_01", "Tr\u1EE9ng v\u1ECBt", "Tr\u1EE9ng v\u1ECBt.", "\ud83e\udd5a", "products", 0, 12, true);
            AddItem(db, "turtle_shell_01", "Mai r\u00F9a", "Mai r\u00F9a c\u1EE9ng.", "\uD83D\uDC22", "products", 0, 38860, true);

            // S\u1EA3n ph\u1EA9m th\u1ECBt (Pro2 \u2014 v\u1EE5 cu\u1ED1i) \u2014 gi\u00E1 b\u00E1n theo c\u1ED9t "Gi\u00E1 product 2" trong VatNuoi.md
            AddItem(db, "pork_01", "Th\u1ECBt heo", "Th\u1ECBt heo t\u01B0\u01A1i.", "\ud83e\udd69", "products", 0, 1056, true);
            AddItem(db, "chicken_meat_01", "Th\u1ECBt g\u00E0", "Th\u1ECBt g\u00E0 t\u01B0\u01A1i.", "\uD83C\uDF57", "products", 0, 816, true);
            AddItem(db, "beef_01", "Th\u1ECBt b\u00F2", "Th\u1ECBt b\u00F2 t\u01B0\u01A1i.", "\ud83e\udd69", "products", 0, 1510, true);
            AddItem(db, "ostrich_meat_01", "Th\u1ECBt \u0111\u00E0 \u0111i\u1EC3u", "Th\u1ECBt \u0111\u00E0 \u0111i\u1EC3u.", "\uD83C\uDF57", "products", 0, 2788, true);
            AddItem(db, "deer_meat_01", "Th\u1ECBt h\u01B0\u01A1u", "Th\u1ECBt h\u01B0\u01A1u.", "\uD83C\uDF56", "products", 0, 3762, true);
            AddItem(db, "goat_meat_01", "Th\u1ECBt d\u00EA", "Th\u1ECBt d\u00EA.", "\uD83C\uDF56", "products", 0, 495, true);
            AddItem(db, "rabbit_meat_01", "Th\u1ECBt th\u1ECF", "Th\u1ECBt th\u1ECF.", "\uD83C\uDF56", "products", 0, 577, true);
            AddItem(db, "goose_meat_01", "Th\u1ECBt ng\u1ED7ng", "Th\u1ECBt ng\u1ED7ng.", "\uD83C\uDF57", "products", 0, 1569, true);
            AddItem(db, "duck_meat_01", "Th\u1ECBt v\u1ECBt", "Th\u1ECBt v\u1ECBt.", "\uD83C\uDF57", "products", 0, 942, true);
            AddItem(db, "turtle_meat_01", "Th\u1ECBt r\u00F9a", "Th\u1ECBt r\u00F9a.", "\uD83C\uDF56", "products", 0, 3428, true);

            // \u2500\u2500 S\u1EA3n ph\u1EA9m C\u00C2Y L\u00C2U N\u0102M (10) \u2014 gi\u00E1 b\u00E1n: Sa Chi/S\u1EA7u Ri\u00EAng theo CayTrong.md, c\u00F2n l\u1EA1i DEMO.
            AddItem(db, "banana_01", "Bu\u1ED3ng chu\u1ED1i", "Bu\u1ED3ng chu\u1ED1i ch\u00EDn.", "\uD83C\uDF4C", "products", 0, 80, true);
            AddItem(db, "coconut_01", "D\u1EEBa", "Tr\u00E1i d\u1EEBa t\u01B0\u01A1i.", "\uD83E\uDD65", "products", 0, 120, true);
            AddItem(db, "areca_01", "Tr\u00E1i cau", "Tr\u00E1i cau.", "\uD83C\uDF34", "products", 0, 100, true);
            AddItem(db, "date_01", "H\u1ED9p Ch\u00E0 L\u00E0", "H\u1ED9p ch\u00E0 l\u00E0 kh\u00F4.", "\uD83C\uDF34", "products", 0, 600, true);
            AddItem(db, "sacha_01", "H\u1ED9p Sa Chi", "H\u1ED9p h\u1EA1t Sa Chi d\u01B0\u1EE3c li\u1EC7u.", "\uD83C\uDF30", "products", 0, 1118, true);
            AddItem(db, "tea_01", "T\u00FAi Tr\u00E0", "T\u00FAi l\u00E1 tr\u00E0.", "\uD83C\uDF75", "products", 0, 400, true);
            AddItem(db, "durian_01", "H\u1ED9p S\u1EA7u Ri\u00EAng", "H\u1ED9p s\u1EA7u ri\u00EAng cao c\u1EA5p.", "\uD83E\uDD6D", "products", 0, 3575, true);
            AddItem(db, "asparagus_01", "B\u00FAp M\u0103ng T\u00E2y", "B\u00FAp m\u0103ng t\u00E2y t\u01B0\u01A1i.", "\uD83C\uDF31", "products", 0, 300, true);
            AddItem(db, "red_ginseng_01", "H\u1ED9p h\u1ED3ng s\u00E2m", "H\u1ED9p h\u1ED3ng s\u00E2m qu\u00FD.", "\uD83C\uDF3F", "products", 0, 2000, true);
            AddItem(db, "royal_ginseng_01", "H\u1ED9p S\u00E2m Ti\u1EBFn Vua", "S\u1EA3n ph\u1EA9m s\u00E2m cao c\u1EA5p nh\u1EA5t.", "\uD83C\uDF3F", "products", 0, 5000, true);
            
            // Vật liệu
            AddItem(db, "wood_01", "G\u1ED7", "G\u1ED7 ch\u1EB7t t\u1EEB c\u00E2y.", "\ud83e\udeb5", "materials", 0, 8, true);
            AddItem(db, "stone_01", "\u0110\u00E1", "\u0110\u00E1 \u0111\u00E0o t\u1EEB m\u1ECF.", "\ud83e\udea8", "materials", 0, 12, true);
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

            // Vật nuôi (mua tại shop)
            AddItem(db, "chicken_01", "Gà", "Gà ta đẻ trứng.", "🐔", "animals", 500, 0, false);
            AddItem(db, "rabbit_01", "Thỏ", "Thỏ con dễ thương.", "🐰", "animals", 400, 0, false);
            AddItem(db, "ostrich_01", "Đà điểu", "Đà điểu cho trứng lớn.", "🦤", "animals", 800, 0, false);
            AddItem(db, "goat_01", "Dê", "Dê cho sữa.", "🐐", "animals", 900, 0, false);
            AddItem(db, "cow_01", "Bò", "Bò sữa.", "🐄", "animals", 1500, 0, false);
            AddItem(db, "deer_01", "Hươu", "Hươu cho nhung quý.", "🦌", "animals", 1800, 0, false);
            AddItem(db, "pig_01", "Heo", "Heo thịt.", "🐷", "animals", 1000, 0, false);

            // Câu cá
            AddItem(db, "fish_01", "Cá chép", "Cá chép tươi sống.", "🐟", "food", 0, 50, true);
            AddItem(db, "fish_02", "Cá hiếm", "Loài cá quý hiếm.", "🐡", "food", 0, 200, true);
            AddItem(db, "gift_box_01", "Hộp quà", "Hộp quà sự kiện từ đại dương.", "🎁", "items", 0, 500, true);

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

        [MenuItem("YWonderLand/Generate Animal Data")]
        public static void GenerateAnimalData()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Items"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Items");
            }

            // Thông tin chăn nuôi (giá/ô/thức ăn/sản phẩm) — nguồn: bảng VatNuoi khách gửi.
            // Chỉ set trường hiển thị + tên/giá; GIỮ nguyên field gameplay (cycle, produceItemId) nếu asset đã có.
            SetHusbandry("chicken_01", "Gà mái V2", 6, 1, "Bắp Ngô", 2, "Cám", 0, "Trứng gà", 1, "Thịt gà", 5);
            SetHusbandry("cow_01", "Bò sữa", 300, 9, "Cỏ Voi", 2, "Khoai Lang", 4, "Sữa bò", 10, "Thịt bò", 50);
            SetHusbandry("pig_01", "Heo con", 100, 9, "Khoai lang", 2, "Bí ngô", 2, "Da heo", 1, "Thịt heo", 50);
            SetHusbandry("ostrich_01", "Đà điểu V2", 170, 1, "Dưa Hấu", 4, "Cám", 0, "Trứng đà điểu", 1, "Thịt đà điểu", 20);
            SetHusbandry("deer_01", "Hươu", 400, 9, "Bắp Ngô", 5, "Cám", 0, "Nhung hươu", 2, "Thịt hươu", 40);
            SetHusbandry("goat_01", "Dê con V2", 50, 9, "Bí ngô", 2, "Cỏ voi", 2, "Sữa dê", 2, "Thịt dê", 20);
            SetHusbandry("rabbit_01", "Thỏ con V2", 5, 1, "Cà rốt", 1, "Bắp ngô", 1, "Lông thỏ", 8, "Thịt thỏ", 5);
            SetHusbandry("goose_01", "Ngỗng con V2", 10, 1, "Bắp Cải", 2, "Bắp Ngô", 3, "Trứng ngỗng", 2, "Thịt ngỗng", 5);
            SetHusbandry("duck_01", "Vịt V3", 8, 1, "Bắp Ngô", 1, "Cám", 0, "Trứng vịt", 1, "Thịt vịt", 5);
            SetHusbandry("turtle_01", "Rùa con", 90, 1, "Rau Muống", 7, "Dưa hấu", 12, "Mai rùa", 1, "Thịt rùa", 10);

            // Logic gameplay theo VatNuoi: produceId, SL sản phẩm/vụ (Pro1), TỔNG SỐ LẦN THU, thịt vụ cuối (Pro2), SL thịt.
            // produceCycle/feed để giây DEMO (số ngày thật chờ khách quy đổi).  (cycle 25s, feed 40s)
            // Chu kỳ thu / cho ăn = NGÀY THẬT theo VatNuoi (quy đổi qua Days()). Con chu kỳ dài (heo/hươu/rùa)
            // là vật nuôi "đầu tư dài hạn" — demo chỉnh nhanh bằng SecondsPerGameDay nếu cần.
            SetAnimalGameplay("chicken_01", "egg_01", 1, 45, "chicken_meat_01", 5, Days(2f), Days(1f));
            SetAnimalGameplay("cow_01", "milk_01", 10, 38, "beef_01", 50, Days(7f), Days(1f));
            SetAnimalGameplay("pig_01", "pigskin_01", 1, 1, "pork_01", 50, Days(180f), Days(1f));
            SetAnimalGameplay("ostrich_01", "ostrich_egg_01", 1, 30, "ostrich_meat_01", 20, Days(6f), Days(1f));
            SetAnimalGameplay("deer_01", "deer_velvet_01", 2, 2, "deer_meat_01", 40, Days(180f), Days(1f));
            SetAnimalGameplay("goat_01", "goat_milk_01", 2, 60, "goat_meat_01", 20, Days(3f), Days(1f));
            SetAnimalGameplay("rabbit_01", "rabbit_fur_01", 8, 2, "rabbit_meat_01", 5, Days(40f), Days(1f));
            SetAnimalGameplay("goose_01", "goose_egg_01", 2, 30, "goose_meat_01", 5, Days(3f), Days(1f));
            SetAnimalGameplay("duck_01", "duck_egg_01", 1, 45, "duck_meat_01", 5, Days(1f), Days(0.5f));
            SetAnimalGameplay("turtle_01", "turtle_shell_01", 1, 1, "turtle_meat_01", 10, Days(300f), Days(7f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AnimalData] Generated/updated 10 animal definitions (kèm thông tin chăn nuôi + logic VatNuoi)!");
        }

        // Set logic gameplay theo VatNuoi (giữ nguyên phần hiển thị do SetHusbandry set trước đó).
        private static void SetAnimalGameplay(string id, string produceId, int produceAmt, int maxHarv,
            string meatId, int meatAmt, float cycle, float feed)
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
            a.meatItemId = meatId;
            a.meatAmount = meatAmt;
            a.produceCycleTimeSec = cycle;
            a.feedIntervalSec = feed;
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
