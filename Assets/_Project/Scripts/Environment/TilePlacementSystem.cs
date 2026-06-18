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

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
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
    }
}
