using UnityEngine;
using System.Collections.Generic;
using YWonderLand.Data;

namespace YWonderLand.Managers
{
    /// <summary>
    /// FarmManager: Qu\u1ea3n l\u00fd to\u00e0n b\u1ed9 n\u00f4ng tr\u1ea1i.
    /// Spawn 10 \u00f4 \u0111\u1ea5t (FarmTile) theo grid pattern 2x5.
    /// L\u01b0u/load tr\u1ea1ng th\u00e1i m\u1ed7i tile v\u00e0o PlayerPrefs.
    /// </summary>
    [RequireComponent(typeof(YWonderLand.Environment.FarmInteractionController))]
    public class FarmManager : MonoBehaviour
    {
        public static FarmManager Instance { get; private set; }

        [Header("Farm Layout")]
        [Tooltip("Số ô đất (demo: 10)")]
        [SerializeField] private int totalTiles = 10;

        [Tooltip("Số cột trong grid")]
        [SerializeField] private int columns = 5;

        [Tooltip("Khoảng cách giữa các ô")]
        [SerializeField] private float tileSpacing = 2.5f;

        [Tooltip("Vị trí gốc của nông trại")]
        [SerializeField] private Vector3 farmOrigin = new Vector3(5f, 0.05f, 5f);

        [Header("References")]
        [SerializeField] private CropDatabase cropDatabase;

        private List<FarmTile> farmTiles = new List<FarmTile>();

        private const string SAVE_KEY = "YW_FarmState";

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Load CropDatabase from Resources if not assigned
            if (cropDatabase == null)
            {
                cropDatabase = Resources.Load<CropDatabase>("CropDatabase");
            }
        }

        void Start()
        {
            SpawnFarmTiles();
            LoadFarmState();
        }

        /// <summary>
        /// Spawn N ô đất theo grid pattern.
        /// </summary>
        private void SpawnFarmTiles()
        {
            // Check if tiles already exist in scene (e.g. tutorial tile)
            FarmTile[] existingTiles = FindObjectsByType<FarmTile>(FindObjectsSortMode.None);
            foreach (var existing in existingTiles)
            {
                if (existing.tileIndex < 0)
                {
                    existing.tileIndex = farmTiles.Count;
                }
                farmTiles.Add(existing);
            }

            int tilesToSpawn = totalTiles - farmTiles.Count;
            int startIndex = farmTiles.Count;

            for (int i = 0; i < tilesToSpawn; i++)
            {
                int globalIndex = startIndex + i;
                int row = globalIndex / columns;
                int col = globalIndex % columns;

                Vector3 position = farmOrigin + new Vector3(
                    col * tileSpacing,
                    0f,
                    row * tileSpacing
                );

                GameObject tileGo = new GameObject($"FarmTile_{globalIndex}");
                tileGo.transform.position = position;
                tileGo.transform.SetParent(this.transform);

                // Add BoxCollider for raycasting
                BoxCollider collider = tileGo.AddComponent<BoxCollider>();
                collider.size = new Vector3(2f, 0.2f, 2f);

                FarmTile tile = tileGo.AddComponent<FarmTile>();
                tile.tileIndex = globalIndex;

                farmTiles.Add(tile);
            }

            Debug.Log($"[FarmManager] Farm ready: {farmTiles.Count} tiles ({existingTiles.Length} existing + {tilesToSpawn} spawned)");
        }

        // ── Public API ──

        /// <summary>
        /// Lấy danh sách tất cả FarmTile.
        /// </summary>
        public List<FarmTile> GetAllTiles() => farmTiles;

        /// <summary>
        /// Lấy CropDatabase (dùng cho FarmInteractionController).
        /// </summary>
        public CropDatabase GetCropDatabase() => cropDatabase;

        /// <summary>
        /// Tìm FarmTile gần vị trí nhất (trong khoảng maxDistance).
        /// </summary>
        public FarmTile GetTileAt(Vector3 worldPos, float maxDistance = 2f)
        {
            FarmTile closest = null;
            float closestDist = maxDistance;

            foreach (var tile in farmTiles)
            {
                float dist = Vector3.Distance(tile.transform.position, worldPos);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = tile;
                }
            }
            return closest;
        }

        // ── Save / Load ──

        public void SaveFarmState()
        {
            FarmSaveData saveData = new FarmSaveData();
            saveData.tiles = new List<TileSaveData>();

            foreach (var tile in farmTiles)
            {
                saveData.tiles.Add(new TileSaveData
                {
                    index = tile.tileIndex,
                    state = (int)tile.currentState,
                    seedId = tile.plantedSeedId,
                    growthPercent = tile.GetGrowthPercentage()
                });
            }

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            Debug.Log("[FarmManager] Farm state saved.");
        }

        public void LoadFarmState()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

            string json = PlayerPrefs.GetString(SAVE_KEY);
            FarmSaveData saveData = JsonUtility.FromJson<FarmSaveData>(json);

            if (saveData == null || saveData.tiles == null) return;

            foreach (var tileData in saveData.tiles)
            {
                if (tileData.index >= 0 && tileData.index < farmTiles.Count)
                {
                    FarmTile tile = farmTiles[tileData.index];

                    // Only restore tiles that were actively growing
                    if (tileData.state > (int)FarmTile.TileState.Soil && !string.IsNullOrEmpty(tileData.seedId))
                    {
                        // Restore planted state and let player continue
                        if (tileData.state >= (int)FarmTile.TileState.Planted)
                        {
                            tile.InteractPlow();
                            tile.InteractPlant(tileData.seedId);

                            if (tileData.state >= (int)FarmTile.TileState.Watered)
                            {
                                tile.InteractWater();
                            }
                        }
                        else if (tileData.state == (int)FarmTile.TileState.Plowed)
                        {
                            tile.InteractPlow();
                        }
                    }
                }
            }

            Debug.Log("[FarmManager] Farm state loaded.");
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveFarmState();
        }

        void OnApplicationQuit()
        {
            SaveFarmState();
        }

        // ── Save Data Structures ──

        [System.Serializable]
        private class FarmSaveData
        {
            public List<TileSaveData> tiles;
        }

        [System.Serializable]
        private class TileSaveData
        {
            public int index;
            public int state;
            public string seedId;
            public float growthPercent;
        }
    }
}
