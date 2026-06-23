using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Lưới ô ĐA DỤNG toàn cục (kiểu Minecraft): người chơi gõ búa lát từng ô.
    /// Mỗi ô lát ra tự gắn FarmTile -> vừa TRỒNG được, vừa tính vào số ô để XÂY chuồng.
    /// Đếm ô để bước sau kiểm "đủ ô mới xây được chuồng".
    /// </summary>
    public class TilePlacementSystem : MonoBehaviour
    {
        public static TilePlacementSystem Instance { get; private set; }

        [Tooltip("Kích thước 1 ô (mét). Khớp khối đất hiện tại = 2.")]
        public float cellSize = 2f;

        private readonly Dictionary<Vector2Int, GameObject> _tiles = new();

        public int TileCount => _tiles.Count;

        private const string SAVE_KEY = "YW_PlacedTiles";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            LoadTiles(); // khôi phục ô tự xây + cây (real-time lớn-bù/chết-bù)
        }

        public Vector2Int WorldToCell(Vector3 p)
            => new(Mathf.RoundToInt(p.x / cellSize), Mathf.RoundToInt(p.z / cellSize));

        public Vector3 CellToWorldCenter(Vector2Int c, float y)
            => new(c.x * cellSize, y, c.y * cellSize);

        public bool HasTile(Vector2Int c) => _tiles.ContainsKey(c);

        /// <summary>Lát 1 ô tại cell (nếu trống). Trả GameObject ô, hoặc null nếu đã có ô.</summary>
        public GameObject PlaceTile(Vector2Int c, float groundY)
        {
            if (HasTile(c)) return null;

            var go = new GameObject($"PlacedTile_{c.x}_{c.y}");
            go.transform.SetParent(transform, true);
            go.transform.position = CellToWorldCenter(c, groundY);

            // Collider để click trồng/tương tác trúng ô
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(cellSize, 0.2f, cellSize);
            col.center = new Vector3(0, 0.05f, 0);

            // Đa dụng: gắn FarmTile -> trồng được ngay; FarmTileMarker tự gắn -> có viền ô.
            go.AddComponent<FarmTile>();

            _tiles[c] = go;
            return go;
        }

        // ─────────── PERSISTENCE (Stage 2): lưu ô TỰ XÂY + cây trên đó, độc lập FarmManager ───────────

        /// <summary>Lưu mọi ô đã lát (toạ độ + cao độ) + trạng thái cây (tái dùng FarmTile.ExportSaveOrNull).</summary>
        public void SaveTiles()
        {
            var data = new PlacedTilesSave { tiles = new List<PlacedTileSave>() };
            foreach (var kv in _tiles)
            {
                if (kv.Value == null) continue;
                var ft = kv.Value.GetComponent<FarmTile>();
                data.tiles.Add(new PlacedTileSave
                {
                    cx = kv.Key.x,
                    cy = kv.Key.y,
                    groundY = kv.Value.transform.position.y,
                    crop = ft != null ? ft.ExportSaveOrNull() : null   // null nếu ô trống
                });
            }
            PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            Debug.Log($"[TilePlacement] Saved {data.tiles.Count} placed tiles.");
        }

        private void LoadTiles()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) return;
            var data = JsonUtility.FromJson<PlacedTilesSave>(PlayerPrefs.GetString(SAVE_KEY));
            if (data == null || data.tiles == null) return;

            var pending = new List<PendingCrop>();
            foreach (var e in data.tiles)
            {
                if (e == null) continue;
                var go = PlaceTile(new Vector2Int(e.cx, e.cy), e.groundY); // dựng lại ô (đăng ký _tiles, KHÔNG trừ vật liệu)
                if (go == null) continue;
                // chỉ gắn cây cho ô có cây (state > Soil); ô trống chỉ cần dựng lại ô.
                if (e.crop != null && e.crop.state > (int)FarmTile.TileState.Soil)
                {
                    var ft = go.GetComponent<FarmTile>();
                    if (ft != null) pending.Add(new PendingCrop { tile = ft, crop = e.crop });
                }
            }
            if (pending.Count > 0) StartCoroutine(RestoreCropsNextFrame(pending));
            Debug.Log($"[TilePlacement] Recreated {data.tiles.Count} tiles ({pending.Count} có cây).");
        }

        // Chờ 1 frame cho FarmTile.Start() chạy (visual sẵn sàng) rồi mới gắn cây.
        private System.Collections.IEnumerator RestoreCropsNextFrame(List<PendingCrop> pending)
        {
            var db = Resources.Load<YWonderLand.Data.CropDatabase>("CropDatabase");
            yield return null;
            foreach (var p in pending)
                if (p.tile != null) p.tile.RestoreSave(p.crop, db);
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveTiles(); }
        private void OnApplicationQuit() { SaveTiles(); }

        private class PendingCrop { public FarmTile tile; public FarmTile.CropSave crop; }

        [System.Serializable]
        private class PlacedTileSave
        {
            public int cx;
            public int cy;
            public float groundY;
            public FarmTile.CropSave crop;
        }

        [System.Serializable]
        private class PlacedTilesSave
        {
            public List<PlacedTileSave> tiles;
        }
    }
}
