using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// DỜI NGUYÊN CỤM CHUỒNG (khách chốt 29/07): nhấc cả chuồng — giữ đúng hình dạng và mang theo
    /// vật nuôi đang ở trong — rồi đặt xuống chỗ mới. KHÔNG tốn/hoàn vật liệu vì chuồng không bị phá.
    ///
    /// Cách dùng (FarmInteractionController gọi):
    ///   Begin(pen)                  -> vào chế độ dời, chụp lại hình dạng + thú
    ///   UpdatePreview(frontCell)    -> mỗi frame: rê cả chuồng theo ô trước mặt nhân vật
    ///   CanPlaceAt(frontCell)       -> chỗ mới có đủ ô đất trống không
    ///   Confirm(frontCell)          -> chốt: ghi lại dữ liệu ô + ô của thú
    ///   Cancel()                    -> trả chuồng về đúng chỗ cũ
    ///
    /// Lưu ý: chỉ DI CHUYỂN vật thể lúc xem trước; dữ liệu ô chỉ đổi khi Confirm, nên bỏ dở giữa chừng
    /// (hủy / thoát scene) không làm hỏng trạng thái nông trại.
    /// </summary>
    public static class PenMoveController
    {
        private class Entry
        {
            public BuildSurfaceCell cell;          // ô cũ
            public GameObject fence;               // hàng rào đang chiếm ô
            public Vector3 fenceStartPos;
            public string materialId;
            public int cost;
            public GameObject animalObject;        // ô neo mới giữ thú
            public string animalItemId;
            public Vector3 animalStartPos;
        }

        private static readonly List<Entry> entries = new List<Entry>();
        private static readonly HashSet<BuildSurfaceCell> capturedCells = new HashSet<BuildSurfaceCell>();
        private static readonly Dictionary<Vector2Int, BuildSurfaceCell> grid = new Dictionary<Vector2Int, BuildSurfaceCell>();

        private static BuildSurfaceCell anchorCell;
        private static float cellSize = 0.8f;
        private static Vector3 appliedDelta;

        public static bool IsActive { get; private set; }

        /// <summary>Số ô rào đang dời (để hiện thông báo).</summary>
        public static int CellCount => entries.Count;

        /// <summary>Vào chế độ dời. Trả false nếu chuồng không hợp lệ.</summary>
        public static bool Begin(List<BuildSurfaceCell> pen)
        {
            Reset();

            if (pen == null || pen.Count == 0) return false;

            foreach (var cell in pen)
            {
                if (cell == null) continue;

                var entry = new Entry
                {
                    cell = cell,
                    fence = cell.Occupant,
                    materialId = cell.BuildMaterialId,
                    cost = cell.BuildCost,
                    animalObject = cell.AnimalObject,
                    animalItemId = cell.AnimalItemId
                };
                if (entry.fence != null) entry.fenceStartPos = entry.fence.transform.position;
                if (entry.animalObject != null) entry.animalStartPos = entry.animalObject.transform.position;

                entries.Add(entry);
                capturedCells.Add(cell);
            }

            if (entries.Count == 0) return false;

            anchorCell = entries[0].cell;
            cellSize = anchorCell.FootprintSize.x;
            if (cellSize < 0.01f) cellSize = 0.8f;

            // Bảng tra ô theo lưới XZ để kiểm tra chỗ đặt mới nhanh (không quét lại toàn scene mỗi frame).
            grid.Clear();
            var all = BuildSurfaceCell.All;
            if (all != null)
            {
                foreach (var c in all)
                {
                    if (c == null) continue;
                    grid[GridKey(c.transform.position)] = c;
                }
            }

            appliedDelta = Vector3.zero;
            IsActive = true;
            return true;
        }

        /// <summary>Rê cả cụm chuồng theo ô trước mặt nhân vật (chỉ đổi vị trí hiển thị).</summary>
        public static void UpdatePreview(BuildSurfaceCell frontCell)
        {
            if (!IsActive) return;

            Vector3 delta = DeltaFor(frontCell);
            if ((delta - appliedDelta).sqrMagnitude < 0.0001f) return;

            appliedDelta = delta;
            foreach (var e in entries)
            {
                if (e.fence != null) e.fence.transform.position = e.fenceStartPos + delta;
                if (e.animalObject != null) e.animalObject.transform.position = e.animalStartPos + delta;
            }
        }

        /// <summary>Chỗ mới có đủ ô đất và còn trống cho cả chuồng không.</summary>
        public static bool CanPlaceAt(BuildSurfaceCell frontCell)
        {
            if (!IsActive || frontCell == null) return false;

            Vector3 delta = DeltaFor(frontCell);
            foreach (var e in entries)
            {
                if (e.cell == null) return false;
                if (!TryGetTargetCell(e.cell, delta, out var target)) return false;

                // Trùng lên chính chuồng đang dời thì được; đè lên vật khác/thú khác thì không.
                if (capturedCells.Contains(target)) continue;
                if (target.IsOccupied || target.AnimalObject != null) return false;
            }
            return true;
        }

        /// <summary>Chốt vị trí mới: chuyển dữ liệu ô (rào, vật liệu, thú) sang các ô đích.</summary>
        public static bool Confirm(BuildSurfaceCell frontCell)
        {
            if (!IsActive || !CanPlaceAt(frontCell)) return false;

            Vector3 delta = DeltaFor(frontCell);
            UpdatePreview(frontCell); // đảm bảo vật thể đã nằm đúng chỗ mới

            // Map ô cũ -> ô mới, dùng để cập nhật lại occupiedCells của thú.
            var remap = new Dictionary<BuildSurfaceCell, BuildSurfaceCell>();
            foreach (var e in entries)
            {
                if (e.cell == null || !TryGetTargetCell(e.cell, delta, out var target)) { Cancel(); return false; }
                remap[e.cell] = target;
            }

            // 1) Trả TẤT CẢ ô cũ về trống trước (ô cũ và ô mới có thể chồng nhau).
            foreach (var e in entries)
            {
                if (e.cell == null) continue;
                e.cell.ClearAnimal();
                e.cell.SetBuildMaterial("", 0);
                e.cell.Clear();
            }

            // 2) Ghi sang ô mới: hàng rào + vật liệu đã tốn + thú đang đứng.
            foreach (var e in entries)
            {
                var target = remap[e.cell];
                if (e.fence != null) target.SetOccupant(e.fence);
                target.SetBuildMaterial(e.materialId, e.cost);
                if (e.animalObject != null) target.SetAnimalOccupant(e.animalObject, e.animalItemId);
            }

            // 3) Cập nhật ô mà mỗi con thú đang chiếm (thú nhiều ô cũng đúng).
            var animals = Object.FindObjectsByType<FarmAnimal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var animal in animals)
            {
                if (animal == null || animal.occupiedCells == null) continue;

                for (int i = 0; i < animal.occupiedCells.Count; i++)
                {
                    var oldCell = animal.occupiedCells[i];
                    if (oldCell != null && remap.TryGetValue(oldCell, out var newCell))
                        animal.occupiedCells[i] = newCell;
                }
            }

            // 4) Rào tự nối lại theo hàng xóm ở chỗ MỚI (bật/tắt để chạy lại logic nối cạnh).
            foreach (var e in entries)
            {
                if (e.fence == null) continue;
                var auto = e.fence.GetComponentInChildren<FenceAutoConnect>(true);
                if (auto == null) continue;
                auto.gameObject.SetActive(false);
                auto.gameObject.SetActive(true);
            }

            Reset();

            var persistence = Object.FindFirstObjectByType<BuildPersistence>(FindObjectsInactive.Include);
            persistence?.SaveBuildings();
            return true;
        }

        /// <summary>Bỏ dở: trả chuồng và thú về đúng chỗ cũ, dữ liệu ô không hề đổi.</summary>
        public static void Cancel()
        {
            if (!IsActive) { Reset(); return; }

            foreach (var e in entries)
            {
                if (e.fence != null) e.fence.transform.position = e.fenceStartPos;
                if (e.animalObject != null) e.animalObject.transform.position = e.animalStartPos;
            }
            Reset();
        }

        private static void Reset()
        {
            entries.Clear();
            capturedCells.Clear();
            grid.Clear();
            anchorCell = null;
            appliedDelta = Vector3.zero;
            IsActive = false;
        }

        private static Vector3 DeltaFor(BuildSurfaceCell frontCell)
        {
            if (frontCell == null || anchorCell == null) return appliedDelta;
            return frontCell.transform.position - anchorCell.transform.position;
        }

        private static bool TryGetTargetCell(BuildSurfaceCell source, Vector3 delta, out BuildSurfaceCell target)
        {
            target = null;
            if (source == null) return false;
            return grid.TryGetValue(GridKey(source.transform.position + delta), out target) && target != null;
        }

        private static Vector2Int GridKey(Vector3 position)
        {
            return new Vector2Int(
                Mathf.RoundToInt(position.x / cellSize),
                Mathf.RoundToInt(position.z / cellSize));
        }
    }
}
