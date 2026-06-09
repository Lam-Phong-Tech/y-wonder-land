using UnityEngine;
using UnityEditor;
using YWonderLand.Data;

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

        [MenuItem("Y Wonder Land/Tools/Generate Mock Items")]
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
            AddItem(db, "carrot_seed_01", "H\u1EA1t c\u00E0 r\u1ED1t", "H\u1EA1t gi\u1ED1ng c\u00E0 r\u1ED1t. Thu ho\u1EA1ch sau 24h.", "\ud83e\udd55", "seeds", 10, 5, true);
            AddItem(db, "cabbage_seed_01", "H\u1EA1t c\u1EA3i", "H\u1EA1t gi\u1ED1ng rau c\u1EA3i xanh t\u01B0\u01A1i.", "\ud83e\udd6c", "seeds", 15, 7, true);
            AddItem(db, "watermelon_seed_01", "H\u1EA1t d\u01B0a h\u1EA5u", "H\u1EA1t gi\u1ED1ng d\u01B0a h\u1EA5u ng\u1ECDt.", "\ud83c\udf49", "seeds", 30, 15, true);
            AddItem(db, "corn_seed_01", "H\u1EA1t b\u1EAFp", "H\u1EA1t gi\u1ED1ng b\u1EAFp ng\u00F4 v\u00E0ng.", "\ud83c\udf3d", "seeds", 20, 10, true);
            AddItem(db, "pumpkin_seed_01", "H\u1EA1t b\u00ED ng\u00F4", "H\u1EA1t gi\u1ED1ng b\u00ED ng\u00F4 m\u00F9a event.", "\ud83c\udf83", "seeds", 25, 12, true);
            AddItem(db, "grass_seed_01", "C\u1ECF voi", "C\u1ECF voi l\u00E0m th\u1EE9c \u0103n cho v\u1EADt nu\u00F4i.", "\ud83c\udf3f", "seeds", 5, 2, true);
            AddItem(db, "morning_glory_seed_01", "H\u1EA1t rau mu\u1ED1ng", "H\u1EA1t gi\u1ED1ng rau mu\u1ED1ng (chu k\u1EF3 ng\u1EAFn).", "\ud83c\udf3e", "seeds", 8, 4, true);
            AddItem(db, "sweet_potato_seed_01", "D\u00E2y khoai lang", "D\u00E2y khoai lang gi\u1ED1ng.", "\ud83c\udf60", "seeds", 12, 6, true);
            
            // Vật phẩm tiêu hao
            AddItem(db, "fertilizer_01", "Ph\u00E2n b\u00F3n", "Gi\u1EA3m 50% th\u1EDDi gian sinh tr\u01B0\u1EDFng.", "\ud83e\uddea", "items", 50, 25, true);
            AddItem(db, "vaccine_01", "V\u1EAFc-xin", "Ph\u00F2ng b\u1EC7nh 7 ng\u00E0y.", "\ud83d\udc89", "items", 80, 40, true);
            AddItem(db, "medicine_01", "Thu\u1ED1c tr\u1ECB", "Thu\u1ED1c \u0111i\u1EC1u tr\u1ECB v\u1EADt nu\u00F4i b\u1EC7nh.", "\ud83d\udc8a", "items", 100, 50, true);
            AddItem(db, "bait_01", "M\u1ED3i c\u00E2u", "T\u0103ng 20% c\u00E1 hi\u1EBFm.", "\ud83e\udeb1", "items", 20, 10, true);
            AddItem(db, "mine_ticket_01", "V\u00E9 \u0111\u00E0o m\u1ECF", "Th\u00EAm 5 l\u01B0\u1EE3t \u0111\u00E0o qu\u1EB7ng.", "\ud83c\udfab", "items", 100, 0, false);

            // Nông sản (8 loại tương ứng 8 seed)
            AddItem(db, "carrot_01", "C\u00E0 r\u1ED1t", "C\u00E0 r\u1ED1t t\u01B0\u01A1i.", "\ud83e\udd55", "items", 0, 15, true);
            AddItem(db, "cabbage_01", "Rau c\u1EA3i", "Rau c\u1EA3i xanh.", "\ud83e\udd6c", "items", 0, 20, true);
            AddItem(db, "watermelon_01", "D\u01B0a h\u1EA5u", "D\u01B0a h\u1EA5u ng\u1ECDt.", "\ud83c\udf49", "items", 0, 50, true);
            AddItem(db, "corn_01", "B\u1EAFp ng\u00F4", "B\u1EAFp ng\u00F4 v\u00E0ng \u01B0\u01A1m.", "\ud83c\udf3d", "items", 0, 30, true);
            AddItem(db, "pumpkin_01", "B\u00ED ng\u00F4", "B\u00ED ng\u00F4 to tr\u00F2n.", "\ud83c\udf83", "items", 0, 35, true);
            AddItem(db, "morning_glory_01", "Rau mu\u1ED1ng", "Rau mu\u1ED1ng xanh.", "\ud83c\udf3e", "items", 0, 10, true);
            AddItem(db, "sweet_potato_01", "Khoai lang", "Khoai lang ng\u1ECDt b\u00F9i.", "\ud83c\udf60", "items", 0, 18, true);
            AddItem(db, "grass_01", "C\u1ECF kh\u00F4", "C\u1ECF kh\u00F4 l\u00E0m th\u1EE9c \u0103n.", "\ud83c\udf3f", "items", 0, 5, true);
            
            // Sản phẩm chăn nuôi
            AddItem(db, "egg_01", "Tr\u1EE9ng g\u00E0", "Tr\u1EE9ng g\u00E0 ta.", "\ud83e\udd5a", "animals", 0, 25, true);
            AddItem(db, "milk_01", "S\u1EEFa b\u00F2", "S\u1EEFa b\u00F2 t\u01B0\u01A1i.", "\ud83e\udd5b", "animals", 0, 40, true);
            AddItem(db, "pork_01", "Th\u1ECBt heo", "Th\u1ECBt heo t\u01B0\u01A1i.", "\ud83e\udd69", "animals", 0, 35, true);
            
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
            
            // Thực phẩm
            AddItem(db, "bread_01", "B\u00E1nh m\u00EC", "B\u00E1nh m\u00EC th\u01A1m ngon.", "\ud83c\udf5e", "food", 20, 5, true);
            AddItem(db, "apple_01", "T\u00E1o \u0111\u1ECF", "T\u00E1o ch\u00EDn \u0111\u1ECF ng\u1ECDt l\u1ECBm.", "\ud83c\udf4e", "food", 10, 2, true);

            // Vật nuôi (mua tại shop)
            AddItem(db, "chicken_01", "Gà", "Gà ta đẻ trứng.", "🐔", "animals", 500, 0, false);
            AddItem(db, "cow_01", "Bò", "Bò sữa.", "🐄", "animals", 1500, 0, false);
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

            CreateAnimalEntry("chicken_01", "Gà", 500, 30f, "egg_01", 1, 10, 45f, true);
            CreateAnimalEntry("cow_01", "Bò sữa", 1500, 45f, "milk_01", 1, 20, 60f, true);
            CreateAnimalEntry("pig_01", "Heo", 1000, 40f, "pork_01", 2, 1, 55f, true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AnimalData] Generated Animal definitions!");
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
