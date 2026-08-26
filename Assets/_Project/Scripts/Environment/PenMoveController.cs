using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// DỜI NGUYÊN MỘT CỤM Ô ĐẤT: nhấc cả cụm — giữ đúng hình dạng và mang theo thứ đang đứng trên đó
    /// — rồi đặt xuống chỗ mới. KHÔNG tốn/hoàn vật liệu vì không có gì bị phá.
    ///
    /// Ban đầu chỉ làm cho CHUỒNG (khách chốt 29/07), nay dùng chung cho cả RUỘNG và ĐƯỜNG LÁT ĐÁ
    /// (khách chốt 30/07) — vì cả ba đều là "một cụm BuildSurfaceCell liền nhau có vật trên đầu".
    /// Tên lớp giữ nguyên để khỏi lệch với tài liệu cũ; <see cref="SubjectLabel"/> quyết định chữ hiển thị.
    ///
    /// Cách dùng (FarmInteractionController gọi):
    ///   Begin(cells, label)   -> vào chế độ dời, chụp lại hình dạng + thú + khoá nhật ký của cây
    ///   UpdatePreview()       -> mỗi frame: rê cả cụm theo bước chân người chơi
    ///   CanPlace()            -> chỗ mới có đủ ô đất trống không
    ///   Confirm()             -> chốt: ghi lại dữ liệu ô + ô của thú + đổi khoá nhật ký cây
    ///   Cancel()              -> trả về đúng chỗ cũ
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
            public bool hadAnimal;                 // ô này ĐANG bị thú chiếm chỗ (kể cả ô "mượn chỗ" của con nhiều ô)
            public string historyKeyBefore;        // khoá nhật ký của cây trên ô này TRƯỚC khi dời (ruộng)
        }

        private static readonly List<Entry> entries = new List<Entry>();
        private static readonly HashSet<BuildSurfaceCell> capturedCells = new HashSet<BuildSurfaceCell>();
        private static readonly Dictionary<Vector2Int, BuildSurfaceCell> grid = new Dictionary<Vector2Int, BuildSurfaceCell>();

        private static BuildSurfaceCell anchorCell;
        private static float cellSize = 0.8f;
        private static Vector3 appliedDelta;

        // Chuồng ĐI THEO NGƯỜI CHƠI: giữ nguyên khoảng cách lúc bắt đầu dời, snap theo ô.
        // Nhờ vậy người chơi đứng ngoài chuồng thì luôn ở ngoài, chuồng không trùm lên nhân vật.
        private static Transform playerTransform;
        private static Vector3 playerStartPos;

        // Tắt va chạm khi rê (rào dịch vào người sẽ đẩy/nhấc nhân vật lên nóc) — bật lại khi xong.
        private static readonly List<Collider> disabledColliders = new List<Collider>();

        // Ghost màu: xanh = đặt được, đỏ = không. Dùng MaterialPropertyBlock nên không tạo material rác.
        private static readonly List<Renderer> tintedRenderers = new List<Renderer>();
        private static MaterialPropertyBlock tintBlock;
        private static int tintState = -1; // -1 = chưa tô, 0 = đỏ, 1 = xanh
        private static readonly Color ValidColor = new Color(0.35f, 1f, 0.45f);
        private static readonly Color InvalidColor = new Color(1f, 0.35f, 0.35f);

        public static bool IsActive { get; private set; }

        /// <summary>Số ô rào đang dời (để hiện thông báo).</summary>
        public static int CellCount => entries.Count;

        /// <summary>Chữ gọi thứ đang dời trong gợi ý và thông báo: "chuồng" / "ruộng" / "đường".</summary>
        public static string SubjectLabel { get; private set; } = DefaultSubject;

        /// <summary>
        /// Vị trí của vật thể TRƯỚC khi bắt đầu rê. Chỉ có nghĩa khi đang dời dở.
        ///
        /// Dùng cho lúc LƯU: xem trước chỉ dịch transform chứ chưa đổi dữ liệu ô, nên nếu có một lần
        /// lưu chen vào giữa (bấm nút Home, thú chết đói, thú đổ bệnh) thì bản lưu sẽ ghi VỊ TRÍ ĐÃ RÊ
        /// mà vẫn gắn với Ô CŨ — mở lại game là hàng rào nằm lệch hẳn khỏi ô của nó và không tự nắn lại.
        /// Hỏi hàm này để lưu đúng vị trí gốc.
        /// </summary>
        public static bool TryGetStartPosition(GameObject occupant, out Vector3 position)
        {
            position = default;
            if (!IsActive || occupant == null) return false;

            foreach (var e in entries)
            {
                if (e == null) continue;
                if (e.fence == occupant) { position = e.fenceStartPos; return true; }
                if (e.animalObject == occupant) { position = e.animalStartPos; return true; }
            }
            return false;
        }

        private const string DefaultSubject = "chuồng";

        /// <summary>Vào chế độ dời. Trả false nếu cụm ô không hợp lệ.</summary>
        public static bool Begin(List<BuildSurfaceCell> pen) => Begin(pen, DefaultSubject);

        /// <summary>Vào chế độ dời với chữ hiển thị riêng ("ruộng", "đường"…).</summary>
        public static bool Begin(List<BuildSurfaceCell> pen, string subjectLabel)
        {
            Reset();
            SubjectLabel = string.IsNullOrEmpty(subjectLabel) ? DefaultSubject : subjectLabel;

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
                    animalItemId = cell.AnimalItemId,
                    hadAnimal = cell.HasAnimal,
                    historyKeyBefore = HistoryKeyOf(cell.Occupant)
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

            // Mốc người chơi để chuồng đi theo đúng khoảng cách ban đầu.
            playerTransform = global::PlayerController.Instance != null
                ? global::PlayerController.Instance.transform
                : null;
            playerStartPos = playerTransform != null ? playerTransform.position : Vector3.zero;

            CaptureVisuals();

            appliedDelta = Vector3.zero;
            IsActive = true;
            return true;
        }

        // Gom collider (tắt để không đẩy nhân vật) + renderer (để tô xanh/đỏ) của rào và thú đang dời.
        private static void CaptureVisuals()
        {
            disabledColliders.Clear();
            tintedRenderers.Clear();
            tintState = -1;

            foreach (var e in entries)
            {
                if (e.fence != null)
                {
                    foreach (var col in e.fence.GetComponentsInChildren<Collider>(true))
                        if (col != null && col.enabled) { col.enabled = false; disabledColliders.Add(col); }

                    foreach (var rend in e.fence.GetComponentsInChildren<Renderer>(true))
                        if (rend != null) tintedRenderers.Add(rend);
                }

                if (e.animalObject != null)
                {
                    foreach (var col in e.animalObject.GetComponentsInChildren<Collider>(true))
                        if (col != null && col.enabled) { col.enabled = false; disabledColliders.Add(col); }
                }
            }
        }

        // Trả lại va chạm + màu gốc.
        private static void RestoreVisuals()
        {
            foreach (var col in disabledColliders)
                if (col != null) col.enabled = true;
            disabledColliders.Clear();

            if (tintState >= 0)
            {
                foreach (var rend in tintedRenderers)
                    if (rend != null) rend.SetPropertyBlock(null);
            }
            tintedRenderers.Clear();
            tintState = -1;
        }

        private static void ApplyTint(bool valid)
        {
            int wanted = valid ? 1 : 0;
            if (tintState == wanted) return;
            tintState = wanted;

            tintBlock ??= new MaterialPropertyBlock();
            Color color = valid ? ValidColor : InvalidColor;

            foreach (var rend in tintedRenderers)
            {
                if (rend == null) continue;
                rend.GetPropertyBlock(tintBlock);
                tintBlock.SetColor("_BaseColor", color);
                tintBlock.SetColor("_Color", color);
                rend.SetPropertyBlock(tintBlock);
            }
        }

        /// <summary>Rê cả cụm chuồng theo bước chân người chơi (snap theo ô) + tô xanh/đỏ báo hợp lệ.
        /// Gọi mỗi frame khi đang ở chế độ dời.</summary>
        public static void UpdatePreview()
        {
            if (!IsActive) return;

            Vector3 delta = CurrentDelta();
            if ((delta - appliedDelta).sqrMagnitude > 0.0001f)
            {
                appliedDelta = delta;
                foreach (var e in entries)
                {
                    if (e.fence != null) e.fence.transform.position = e.fenceStartPos + delta;
                    if (e.animalObject != null) e.animalObject.transform.position = e.animalStartPos + delta;
                }
            }

            ApplyTint(CanPlace());
        }

        /// <summary>Chỗ ĐANG XEM TRƯỚC có đủ ô đất trống cho cả chuồng không.</summary>
        public static bool CanPlace()
        {
            if (!IsActive) return false;

            foreach (var e in entries)
            {
                if (e.cell == null) return false;
                if (!TryGetTargetCell(e.cell, appliedDelta, out var target)) return false;

                // Trùng lên chính chuồng đang dời thì được; đè lên vật khác/thú khác thì không.
                if (capturedCells.Contains(target)) continue;
                if (target.IsOccupied || target.AnimalObject != null) return false;
            }
            return true;
        }

        /// <summary>Chốt ĐÚNG vị trí đang xem trước (không nhận ô ngoài vào, tránh đặt nhầm về chỗ cũ).</summary>
        public static bool Confirm()
        {
            if (!IsActive || !CanPlace()) return false;

            Vector3 delta = appliedDelta;

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
                // Con nhiều ô: CHỈ ô neo giữ AnimalObject, 8 ô còn lại chỉ "mượn chỗ" (BuildPersistence:266).
                // Bước 1 vừa xoá cờ của cả 9 ô, mà nhánh trên chỉ trả cờ cho ô neo — thiếu dòng này thì
                // 8 ô kia thành trống ảo: chuồng báo còn chỗ ngay trên lưng con vật, thả thú vào là
                // máy chủ từ chối. Trả lại đúng cờ đã chụp lúc nhấc lên.
                else if (e.hadAnimal) target.SetAnimal(true);
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

            // 5) DỜI RUỘNG: khoá nhật ký của cây tính theo VỊ TRÍ ô, nên đổi chỗ là phải đổi khoá,
            //    không thì lịch sử tưới/bón/thu của cây thành mồ côi. Lúc này vật thể đã nằm ở chỗ
            //    mới (xem trước đã dời transform), nên khoá mới đọc thẳng từ FarmTile là đúng.
            RemapHistoryKeys();

            RestoreVisuals();
            Reset();

            var persistence = Object.FindFirstObjectByType<BuildPersistence>(FindObjectsInactive.Include);
            persistence?.SaveBuildings();
            return true;
        }

        /// <summary>Khoá nhật ký của cây đang đứng trên occupant (rỗng nếu không phải ô ruộng).</summary>
        private static string HistoryKeyOf(GameObject occupant)
        {
            if (occupant == null) return "";
            var tile = occupant.GetComponentInChildren<FarmTile>(true);
            return tile != null ? tile.HistoryKey : "";
        }

        private static void RemapHistoryKeys()
        {
            Dictionary<string, string> map = null;

            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.historyKeyBefore)) continue;

                string after = HistoryKeyOf(e.fence);
                if (string.IsNullOrEmpty(after) || after == e.historyKeyBefore) continue;

                map ??= new Dictionary<string, string>();
                map[e.historyKeyBefore] = after; // ô con của giàn mượn khoá ô chính -> trùng cặp, ghi đè vô hại
            }

            if (map != null) FarmActivityLog.RemapOwners(map);
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
            RestoreVisuals();
            Reset();
        }

        private static void Reset()
        {
            entries.Clear();
            capturedCells.Clear();
            grid.Clear();
            anchorCell = null;
            playerTransform = null;
            appliedDelta = Vector3.zero;
            SubjectLabel = DefaultSubject;
            IsActive = false;
        }

        /// <summary>Độ dời hiện tại = quãng người chơi đã đi kể từ lúc bấm "Dời chuồng", làm tròn về ô.
        /// Cao độ lấy theo ô đích để chuồng nằm đúng mặt đất chỗ mới.</summary>
        private static Vector3 CurrentDelta()
        {
            if (playerTransform == null || anchorCell == null) return appliedDelta;

            Vector3 walked = playerTransform.position - playerStartPos;
            Vector3 delta = new Vector3(
                Mathf.RoundToInt(walked.x / cellSize) * cellSize,
                0f,
                Mathf.RoundToInt(walked.z / cellSize) * cellSize);

            if (TryGetTargetCell(anchorCell, delta, out var targetAnchor))
                delta.y = targetAnchor.transform.position.y - anchorCell.transform.position.y;

            return delta;
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
