using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Mô hình chuồng cho loại rào "1 hàng rào = 1 hộp vuông trên 1 ô".
    /// Ô CÓ RÀO chính là ô chuồng (nơi thả thú). Nhiều ô-rào kề nhau (rào tự nối, bỏ vách giữa)
    /// = 1 chuồng to. Chuồng = thành phần liên thông của các ô-rào (4-kề). Sức chứa = số ô-rào
    /// chưa có thú.
    /// </summary>
    public static class PenEnclosure
    {
        private static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        /// <summary>Từ 1 ô CÓ RÀO, trả về cụm ô-rào liền nhau (= các ô của chuồng). Null nếu seed không phải ô rào.</summary>
        public static List<BuildSurfaceCell> FindPen(BuildSurfaceCell seed, int maxCells = 400)
        {
            if (seed == null || !seed.HasFence) return null;

            var all = BuildSurfaceCell.All;
            if (all == null || all.Count == 0) return null;

            float cell = seed.FootprintSize.x;
            if (cell < 0.01f) cell = 0.8f;
            Vector3 o = seed.SurfaceCenter; // seed = gốc lưới -> (0,0)

            // Chỉ map các ô CÓ RÀO.
            var map = new Dictionary<Vector2Int, BuildSurfaceCell>();
            foreach (var c in all)
            {
                if (c == null || !c.HasFence) continue;
                Vector3 p = c.SurfaceCenter;
                map[new Vector2Int(
                    Mathf.RoundToInt((p.x - o.x) / cell),
                    Mathf.RoundToInt((p.z - o.z) / cell))] = c;
            }

            var visited = new HashSet<Vector2Int> { Vector2Int.zero };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(Vector2Int.zero);
            var pen = new List<BuildSurfaceCell>();

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (!map.TryGetValue(cur, out var bsc)) continue;
                pen.Add(bsc);
                if (pen.Count > maxCells) break;

                foreach (var d in Dirs)
                {
                    var n = cur + d;
                    if (visited.Add(n)) queue.Enqueue(n);
                }
            }

            return pen.Count > 0 ? pen : null;
        }

        /// <summary>Số ô chuồng CHƯA có thú = sức chứa còn lại.</summary>
        public static int AvailableCount(List<BuildSurfaceCell> pen)
        {
            if (pen == null) return 0;
            int n = 0;
            foreach (var c in pen) if (c != null && !c.HasAnimal) n++;
            return n;
        }

        /// <summary>
        /// Lấy toàn bộ vật nuôi thuộc cụm chuồng. Một con nhiều ô chỉ có ô neo giữ AnimalObject,
        /// nên phải quét cả occupiedCells để không bỏ sót.
        /// </summary>
        public static List<FarmAnimal> FindAnimals(List<BuildSurfaceCell> pen)
        {
            var result = new List<FarmAnimal>();
            if (pen == null || pen.Count == 0) return result;

            var seen = new HashSet<FarmAnimal>();
            foreach (var cell in pen)
            {
                if (cell == null || cell.AnimalObject == null) continue;

                var animal = cell.AnimalObject.GetComponent<FarmAnimal>();
                if (animal == null) animal = cell.AnimalObject.GetComponentInChildren<FarmAnimal>();
                if (animal != null && seen.Add(animal))
                    result.Add(animal);
            }

            var animals = Object.FindObjectsByType<FarmAnimal>(FindObjectsSortMode.None);
            foreach (var animal in animals)
            {
                if (animal == null || animal.occupiedCells == null || seen.Contains(animal)) continue;

                foreach (var cell in animal.occupiedCells)
                {
                    if (cell == null || !pen.Contains(cell)) continue;

                    if (seen.Add(animal))
                        result.Add(animal);
                    break;
                }
            }

            return result;
        }
    }
}
