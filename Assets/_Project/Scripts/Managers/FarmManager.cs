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
        [Tooltip("BẬT = tự spawn lưới ô. TẮT (mặc định) = KHÔNG tự xây, chỉ track ô có sẵn + lo persistence.")]
        [SerializeField] private bool autoSpawnTiles = false;

        [Tooltip("Số ô đất tự spawn (CHỈ dùng khi Auto Spawn Tiles BẬT)")]
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
            StartCoroutine(LoadAfterTilesReady());
        }

        // Hoãn 1 frame để FarmTile.Start() chạy xong (visual sẵn sàng) rồi mới khôi phục.
        private System.Collections.IEnumerator LoadAfterTilesReady()
        {
            yield return null;
            LoadFarmState();
        }

        /// <summary>
        /// Spawn N ô đất theo grid pattern.
        /// </summary>
        private void SpawnFarmTiles()
        {
            // Pick up ô CÓ SẴN trong scene (ô đặt sẵn) — BỎ QUA ô của TilePlacementSystem (Stage 2 tự lo ô gõ-búa).
            FarmTile[] existingTiles = FindObjectsByType<FarmTile>(FindObjectsSortMode.None);
            foreach (var existing in existingTiles)
            {
                if (existing == null) continue;
                if (existing.GetComponentInParent<YWonderLand.Environment.TilePlacementSystem>() != null) continue; // ô gõ-búa → Stage 2 lo
                if (existing.tileIndex < 0) existing.tileIndex = farmTiles.Count;
                farmTiles.Add(existing);
            }

            // Anh chốt: KHÔNG tự xây lưới ô nữa. Chỉ spawn khi BẬT autoSpawnTiles.
            if (!autoSpawnTiles)
            {
                Debug.Log($"[FarmManager] Auto-spawn TẮT — track {farmTiles.Count} ô có sẵn (persistence vẫn chạy).");
                return;
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
            var saveData = new FarmSaveData { tiles = new List<FarmTile.CropSave>() };

            foreach (var tile in farmTiles)
            {
                if (tile == null) continue;
                var cs = tile.ExportSaveOrNull();   // null nếu đất trống / ô slave / cây nhiều ô
                if (cs == null) continue;
                cs.posKey = PosKey(tile.transform.position);
                saveData.tiles.Add(cs);
            }

            PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();
            Debug.Log($"[FarmManager] Saved {saveData.tiles.Count} crop tiles.");
        }

        public void LoadFarmState()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

            FarmSaveData saveData = JsonUtility.FromJson<FarmSaveData>(PlayerPrefs.GetString(SAVE_KEY));
            if (saveData == null || saveData.tiles == null) return;

            // Khớp ô theo VỊ TRÍ (ổn định hơn tileIndex — không phụ thuộc thứ tự tạo).
            var byPos = new Dictionary<string, FarmTile>();
            foreach (var tile in farmTiles)
                if (tile != null) byPos[PosKey(tile.transform.position)] = tile;

            int restored = 0;
            foreach (var cs in saveData.tiles)
            {
                if (cs == null || string.IsNullOrEmpty(cs.posKey)) continue;
                if (byPos.TryGetValue(cs.posKey, out FarmTile tile) && tile != null)
                {
                    tile.RestoreSave(cs, cropDatabase);   // tự lớn-bù / chết-bù theo thời gian thực
                    restored++;
                }
            }
            Debug.Log($"[FarmManager] Restored {restored} crop tiles.");
        }

        /// <summary>Key vị trí ô (làm tròn 0.1m) — khóa lưu/khớp ô, ổn định qua các phiên.</summary>
        private static string PosKey(Vector3 p)
            => $"{Mathf.RoundToInt(p.x * 10f)}_{Mathf.RoundToInt(p.z * 10f)}";

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
            public List<FarmTile.CropSave> tiles;
        }
    }
}
