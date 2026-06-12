using UnityEngine;
using UnityEditor;
using YWonderLand.Data;

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

            // 8 lo\u1ea1i c\u00e2y tr\u1ed3ng theo k\u1ecbch b\u1ea3n (demo: 30-60s growth)
            AddCrop(db, "carrot_seed_01", "carrot_01", 45f, 20f, 3, 20, 50, 3, new Color(1f, 0.55f, 0.1f), "\ud83e\udd55");
            AddCrop(db, "cabbage_seed_01", "cabbage_01", 40f, 18f, 3, 15, 40, 3, new Color(0.3f, 0.8f, 0.3f), "\ud83e\udd6c");
            AddCrop(db, "watermelon_seed_01", "watermelon_01", 60f, 25f, 5, 30, 80, 3, new Color(0.1f, 0.6f, 0.1f), "\ud83c\udf49");
            AddCrop(db, "corn_seed_01", "corn_01", 50f, 22f, 4, 25, 60, 3, new Color(1f, 0.85f, 0.2f), "\ud83c\udf3d");
            AddCrop(db, "pumpkin_seed_01", "pumpkin_01", 55f, 24f, 4, 25, 70, 3, new Color(0.9f, 0.5f, 0.1f), "\ud83c\udf83");
            AddCrop(db, "morning_glory_seed_01", "morning_glory_01", 30f, 15f, 2, 10, 25, 2, new Color(0.4f, 0.7f, 0.3f), "\ud83c\udf3e");
            AddCrop(db, "sweet_potato_seed_01", "sweet_potato_01", 50f, 22f, 3, 20, 45, 3, new Color(0.6f, 0.3f, 0.15f), "\ud83c\udf60");
            AddCrop(db, "grass_seed_01", "grass_01", 30f, 15f, 3, 5, 10, 2, new Color(0.2f, 0.6f, 0.2f), "\ud83c\udf3f");

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CropData] Generated CropDatabase with 8 crops!");
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
            crop.harvestYield = yield;
            crop.expReward = exp;
            crop.posReward = pos;
            crop.growthStages = stages;
            crop.cropColor = color;
            crop.iconEmoji = emoji;

            EditorUtility.SetDirty(crop);
            db.AddCropEntry(crop);
        }
    }
}
