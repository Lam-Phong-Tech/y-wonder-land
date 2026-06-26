using UnityEngine;
using UnityEditor;
using YWonderLand.Data;
using static YWonderLand.Core.GameTimeConfig; // Days()/Hours() — quy đổi thời gian thật

namespace YWonderLand.EditorScripts
{
    /// <summary>
    /// T\u1ef1 \u0111\u1ed9ng t\u1ea1o CropDatabase + CropDefinition assets cho demo.
    /// Ch\u1ea1y qua menu: Y WONDER GREEN FARM \u2192 Tools \u2192 Generate Crop Data
    /// </summary>
    public class CropDataGenerator
    {
        [MenuItem("Y WONDER GREEN FARM/Tools/Generate Crop Data")]
        public static void GenerateCropData()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Items"))
                AssetDatabase.CreateFolder("Assets/Resources", "Items");

            // T\u1ea1o ho\u1eb7c load CropDatabase
            CropDatabase db = AssetDatabase.LoadAssetAtPath<CropDatabase>("Assets/Resources/CropDatabase.asset");
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<CropDatabase>();
                AssetDatabase.CreateAsset(db, "Assets/Resources/CropDatabase.asset");
            }

            db.ClearCrops();

            // 8 lo\u1ea1i c\u00e2y NG\u1eaeN NG\u00c0Y \u2014 BA ch\u1ed1t: ch\u00edn trong 24h = Days(1f) cho T\u1ea4T C\u1ea2 (demo 60s/c\u00e2y). Tutorial tua nhanh 24s (\u00e9p \u1edf FarmTile).
            // Cây NGẮN NGÀY = THỨC ĂN chăn nuôi, KHÔNG bán → posReward = 0 (kiếm tiền qua vòng vật nuôi, khách chốt 22/06).
            AddCrop(db, "carrot_seed_01", "carrot_01", Days(1f), Hours(10f), 1, 25, 0, 3, new Color(1f, 0.55f, 0.1f), "\ud83e\udd55");
            AddCrop(db, "cabbage_seed_01", "cabbage_01", Days(1f), Hours(10f), 1, 35, 0, 3, new Color(0.3f, 0.8f, 0.3f), "\ud83e\udd6c");
            AddCrop(db, "watermelon_seed_01", "watermelon_01", Days(1f), Hours(10f), 1, 50, 0, 3, new Color(0.1f, 0.6f, 0.1f), "\ud83c\udf49");
            AddCrop(db, "corn_seed_01", "corn_01", Days(1f), Hours(10f), 3, 100, 0, 3, new Color(1f, 0.85f, 0.2f), "\ud83c\udf3d");
            AddCrop(db, "pumpkin_seed_01", "pumpkin_01", Days(1f), Hours(10f), 11, 125, 0, 3, new Color(0.9f, 0.5f, 0.1f), "\ud83c\udf83");
            AddCrop(db, "morning_glory_seed_01", "morning_glory_01", Days(1f), Hours(6f), 1, 60, 0, 2, new Color(0.4f, 0.7f, 0.3f), "\ud83c\udf3e");
            AddCrop(db, "sweet_potato_seed_01", "sweet_potato_01", Days(1f), Hours(10f), 1, 100, 0, 3, new Color(0.6f, 0.3f, 0.15f), "\ud83c\udf60");
            AddCrop(db, "grass_seed_01", "grass_01", Days(1f), Hours(6f), 2, 175, 0, 2, new Color(0.2f, 0.6f, 0.2f), "\ud83c\udf3f");

            // \u2500\u2500 C\u00c2Y L\u00c2U N\u0102M (10 c\u00e2y) \u2014 khung C\u01a0 B\u1ea2N: t\u1ea1m 1-L\u1ea6N-THU nh\u01b0 c\u00e2y ng\u1eafn ng\u00e0y (thu-nhi\u1ec1u-l\u1ea7n = Phase 2).
            // growthTime/water = gi\u00e2y DEMO. cropPrefab \u0111\u1ec3 TR\u1ed0NG \u2192 anh k\u00e9o model v\u00e0o t\u1eebng Crop_<seed>.asset.
            AddCrop(db, "banana_seed_01", "banana_01", Days(2f), Hours(10f), 3, 150, 0, 5, new Color(0.85f, 0.8f, 0.2f), "\ud83c\udf4c");
            AddCrop(db, "coconut_seed_01", "coconut_01", Days(2f), Hours(10f), 2, 150, 0, 5, new Color(0.6f, 0.45f, 0.25f), "\ud83e\udd65");
            AddCrop(db, "areca_seed_01", "areca_01", Days(2f), Hours(10f), 3, 150, 0, 5, new Color(0.55f, 0.7f, 0.3f), "\ud83c\udf34");
            AddCrop(db, "date_seed_01", "date_01", Days(2f), Hours(10f), 4, 200, 0, 5, new Color(0.6f, 0.35f, 0.15f), "\ud83c\udf34");
            AddCrop(db, "sacha_seed_01", "sacha_01", Days(28f), Days(14f), 5, 6800, 0, 5, new Color(0.3f, 0.6f, 0.3f), "\ud83c\udf30");
            AddCrop(db, "tea_seed_01", "tea_01", Days(2f), Hours(10f), 5, 180, 0, 5, new Color(0.2f, 0.55f, 0.25f), "\ud83c\udf75");
            AddCrop(db, "durian_seed_01", "durian_01", Days(28f), Days(14f), 5, 16800, 0, 5, new Color(0.7f, 0.6f, 0.2f), "\ud83e\udd6d");
            AddCrop(db, "asparagus_seed_01", "asparagus_01", Days(2f), Hours(10f), 4, 150, 0, 4, new Color(0.4f, 0.7f, 0.3f), "\ud83c\udf31");
            AddCrop(db, "red_ginseng_seed_01", "red_ginseng_01", Days(2f), Hours(10f), 1, 250, 0, 5, new Color(0.7f, 0.2f, 0.2f), "\ud83c\udf3f");
            AddCrop(db, "royal_ginseng_seed_01", "royal_ginseng_01", Days(2f), Hours(10f), 1, 400, 0, 5, new Color(0.8f, 0.15f, 0.2f), "\ud83c\udf3f");
            // Chanh leo/chanh day theo CayTrongLauNam2: 20 o, thu 2 lan, chu ky 90 ngay, gia ban 57.
            AddCrop(db, "passion_fruit_seed_01", "passion_fruit_01", Days(90f), Days(14f), 10, 3000, 0, 5, new Color(0.5f, 0.2f, 0.6f), "\ud83c\udf47");

            // Cây LÂU NĂM: số ô + thu NHIỀU LẦN + sản phẩm vụ cuối (theo CayTrongLauNam2.xlsx).
            // (seedId, số ô, số lần thu, chu kỳ ra quả, idSP vụ cuối, SL vụ cuối)
            SetPerennial("sacha_seed_01", 1, 9, Days(28f), Days(14f), 6800, "sacha_01", 46);
            SetPerennial("durian_seed_01", 1, 12, Days(28f), Days(14f), 16800, "durian_01", 29);
            SetPerennial("passion_fruit_seed_01", 20, 2, Days(90f), Days(14f), 3000, "passion_fruit_01", 35);

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CropData] Generated CropDatabase with 19 crops (8 ng\u1eafn ng\u00e0y + 11 l\u00e2u n\u0103m)!");
        }

        private static void AddCrop(CropDatabase db, string seedId, string harvestId,
            float growthTime, float waterInterval, int yield,
            int exp, int pos, int stages, Color color, string emoji)
        {
            string path = "Assets/Resources/Items/Crop_" + seedId + ".asset";
            CropDefinition crop = AssetDatabase.LoadAssetAtPath<CropDefinition>(path);

            if (crop == null)
            {
                crop = ScriptableObject.CreateInstance<CropDefinition>();
                AssetDatabase.CreateAsset(crop, path);
            }

            crop.seedItemId = seedId;
            crop.harvestItemId = harvestId;
            crop.growthTimeSec = growthTime;
            crop.waterIntervalSec = waterInterval;
            // Đồng hồ CHẾT (khách chốt cây ngắn ngày): chưa tưới sống 8h, mỗi lần tưới đầy 20h.
            // Cây lâu năm tạm dùng chung số này (số tạm — chỉnh riêng từng cây khi khách cho số).
            crop.noWaterDeathSec = Hours(8f);
            crop.wateredLifeSec = Hours(20f);
            crop.harvestYield = yield;
            crop.expReward = exp;
            crop.posReward = pos;
            crop.growthStages = stages;
            crop.cropColor = color;
            crop.iconEmoji = emoji;

            EditorUtility.SetDirty(crop);
            db.AddCropEntry(crop);
        }

        // Set thông số CÂY LÂU NĂM lên Crop_<seedId>.asset (chạy SAU AddCrop). Cây ngắn ngày giữ default (1 ô, 1 lần thu).
        private static void SetPerennial(string seedId, int plotSlots, int maxHarvests, float reHarvestCycleSec,
            float waterCycleSec, int expReward, string finalProductId, int finalProductAmount)
        {
            string path = "Assets/Resources/Items/Crop_" + seedId + ".asset";
            var crop = AssetDatabase.LoadAssetAtPath<CropDefinition>(path);
            if (crop == null) { Debug.LogWarning($"[CropData] Chưa có Crop_{seedId} để set cây lâu năm (AddCrop phải chạy trước)."); return; }
            crop.plotSlots = plotSlots;
            crop.maxHarvests = maxHarvests;
            crop.growthTimeSec = reHarvestCycleSec;
            crop.waterIntervalSec = waterCycleSec;
            crop.noWaterDeathSec = waterCycleSec;
            crop.wateredLifeSec = waterCycleSec;
            crop.expReward = expReward;
            crop.reHarvestCycleSec = reHarvestCycleSec;
            crop.finalProductItemId = finalProductId;
            crop.finalProductAmount = finalProductAmount;
            EditorUtility.SetDirty(crop);
        }
    }
}
