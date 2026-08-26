using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YWonderLand.Backend;

namespace YWonderLand.Environment
{
    /// <summary>
    /// LƯU / KHÔI PHỤC công trình đặt qua Build Mode (Ruộng/Chuồng/Đường) + cây trên ô ruộng.
    /// Gắn lên 1 GameObject ĐANG BẬT trong scene (vd object [BuildPrefabLibrary]).
    ///
    /// - Lưu: lúc đóng app (quit/pause) + ngay sau mỗi lần đặt (OnBuildingPlaced).
    /// - Nạp: lúc vào game — tìm BuildSurfaceCell theo vị trí, dựng lại prefab qua GhostPlacementController.PlaceFromSave,
    ///        rồi khôi phục cây (FarmTile.RestoreSave) với lớn-bù / chết-bù theo thời gian thực.
    ///
    /// Con vật trong chuồng cũng được lưu/khôi phục: đói-bù/chết-bù theo thời gian thực (re-spawn trên ô chuồng).
    /// </summary>
    public class BuildPersistence : MonoBehaviour
    {
        private const string SAVE_KEY = "YW_BuildState";
        private bool loadComplete;

        private void OnEnable()
        {
            GhostPlacementController.OnBuildingPlaced += HandleBuildingPlaced;
            FarmStateSync.AuthoritativeSnapshotApplied += HandleAuthoritativeSnapshotApplied;
            if (AuthService.Instance == null) return;
            AuthService.Instance.IdentityChanging += HandleIdentityChanging;
            AuthService.Instance.IdentityChanged += HandleIdentityChanged;
        }

        private void OnDisable()
        {
            GhostPlacementController.OnBuildingPlaced -= HandleBuildingPlaced;
            FarmStateSync.AuthoritativeSnapshotApplied -= HandleAuthoritativeSnapshotApplied;
            if (AuthService.Instance == null) return;
            AuthService.Instance.IdentityChanging -= HandleIdentityChanging;
            AuthService.Instance.IdentityChanged -= HandleIdentityChanged;
        }

        private void Start() => StartCoroutine(LoadAfterCellsReady());

        private IEnumerator LoadAfterCellsReady()
        {
            yield return null; // chờ BuildSurfaceCell.OnEnable đăng ký + các Instance (GPC/Library) sẵn sàng
            LoadBuildings();
        }

        private void HandleBuildingPlaced(string itemName, int price) => SaveBuildings(); // lưu ngay khi đặt

        private void HandleIdentityChanging(string previousScopeId, string nextScopeId)
        {
            SaveBuildings();
        }

        private void HandleIdentityChanged(string previousScopeId, string nextScopeId)
        {
            StopAllCoroutines();
            loadComplete = false;
            ClearRuntimeState();
            StartCoroutine(ReloadAfterIdentityChange());
        }

        private IEnumerator ReloadAfterIdentityChange()
        {
            yield return null;
            LoadBuildings();
        }

        private void HandleAuthoritativeSnapshotApplied()
        {
            // Đang rê chuồng/ruộng dở mà máy chủ ập ảnh chụp xuống thì ClearRuntimeState() ngay bên dưới
            // sẽ HUỶ đúng những GameObject mà PenMoveController đang cầm. Người chơi bấm "Đặt" sau đó:
            // bước 1 của Confirm() xoá sạch ô cũ (nơi hàng rào + thú vừa được nạp lại), bước 2 thì trượt
            // im lặng vì tham chiếu đã chết — chuồng và con vật biến mất khỏi bản lưu rồi được đẩy luôn
            // lên máy chủ. Mất thật, không tự lành. Nên phải bỏ dở việc dời TRƯỚC, lúc vật thể còn sống.
            if (PenMoveController.IsActive)
            {
                PenMoveController.Cancel();
                Debug.LogWarning("[BuildPersistence] Máy chủ gửi ảnh chụp mới khi đang dời — đã bỏ dở việc dời để khỏi mất chuồng/thú.");
            }

            StopAllCoroutines();
            loadComplete = false;
            ClearRuntimeState();
            StartCoroutine(ReloadAfterIdentityChange());
        }

        private static void ClearRuntimeState()
        {
            var animals = new HashSet<GameObject>();
            foreach (var cell in BuildSurfaceCell.All)
            {
                if (cell == null) continue;
                if (cell.AnimalObject != null) animals.Add(cell.AnimalObject);
                cell.ClearAnimal();
            }
            foreach (var animal in animals)
                if (animal != null) Destroy(animal);

            var buildings = FindObjectsByType<PlacedBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var building in buildings)
            {
                if (building == null) continue;
                BuildSurfaceCell.ClearOccupant(building.gameObject);
                Destroy(building.gameObject);
            }
        }

        // ── SAVE ──
        public void SaveBuildings()
        {
            if (!loadComplete)
            {
                Debug.LogWarning("[BuildPersistence] Skip save before load completes to avoid overwriting existing build state.");
                return;
            }

            var data = new BuildSave { items = new List<BuildItem>() };
            foreach (var cell in BuildSurfaceCell.All)
            {
                if (cell == null || !cell.IsOccupied) continue;
                var occ = cell.Occupant;
                if (occ == null) continue;
                var tag = occ.GetComponent<PlacedBuilding>();
                if (tag == null) continue; // chỉ lưu công trình đặt qua Build Mode (có tag); bỏ vật đặt-sẵn trong scene

                var ft = occ.GetComponentInChildren<FarmTile>(); // FarmTile nằm ở prefab con (vd Dirt)

                // cellKey lấy theo Ô, còn px/py/pz lấy theo VẬT THỂ — hai nguồn này lệch nhau trong lúc
                // đang rê, vì xem trước chỉ dịch transform mà chưa đổi dữ liệu ô. Lưu thẳng sẽ ghi vị trí
                // đã rê kèm ô cũ, mở lại game là rào nằm lệch hẳn khỏi ô và không tự nắn về. Đang dời dở
                // thì lấy vị trí gốc; dời xong Confirm() ghi dữ liệu ô rồi nên hai nguồn lại khớp.
                Vector3 occPos = occ.transform.position;
                if (PenMoveController.TryGetStartPosition(occ, out var startPos)) occPos = startPos;

                data.items.Add(new BuildItem
                {
                    cellKey = PosKey(cell.transform.position),
                    itemName = tag.itemName,
                    fx = tag.footprintX,
                    fy = tag.footprintY,
                    px = occPos.x,
                    py = occPos.y,
                    pz = occPos.z,
                    rotY = occ.transform.eulerAngles.y,
                    materialId = tag.materialId,
                    cost = tag.cost,
                    crop = ft != null ? ft.ExportSaveOrNull() : null
                });
            }

            // ── Lưu CON VẬT (thú đứng trên ô chuồng BuildSurfaceCell) ──
            data.animals = new List<AnimalSave>();
            foreach (var cell in BuildSurfaceCell.All)
            {
                if (cell == null || cell.AnimalObject == null) continue; // chỉ ô NEO (giữ GameObject thú)
                var fa = cell.AnimalObject.GetComponent<FarmAnimal>();
                if (fa == null) continue;

                var cellKeys = new List<string>();
                if (fa.occupiedCells != null)
                    foreach (var c in fa.occupiedCells) { if (c != null) cellKeys.Add(PosKey(c.transform.position)); }
                if (cellKeys.Count == 0) cellKeys.Add(PosKey(cell.transform.position));

                data.animals.Add(new AnimalSave
                {
                    instanceId = fa.animalInstanceId,
                    itemId = cell.AnimalItemId,
                    cellKeys = cellKeys,
                    feedRefUnix = fa.FeedRefUnix,
                    produceRefUnix = fa.ProduceRefUnix,
                    hasBeenFed = fa.HasBeenFed,
                    harvestsRemaining = fa.harvestsRemaining,
                    hasProductReady = fa.hasProductReady,
                    pendingProduct = fa.pendingProduct,
                    isVaccinated = fa.isVaccinated,
                    vaccineUntilUnix = fa.VaccineUntilUnix,
                    sickRefUnix = fa.SickRefUnix,
                    sicknessRolled = fa.SicknessRolled,
                    state = (int)fa.currentState
                });
            }

            PlayerScopedPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(data));
            PlayerScopedPrefs.Save();
            FarmStateSync.NotifyLocalStateSaved();
            Debug.Log($"[BuildPersistence] Saved {data.items.Count} buildings + {data.animals.Count} animals.");
        }

        // ── LOAD ──
        private void LoadBuildings()
        {
            if (!PlayerScopedPrefs.HasKey(SAVE_KEY))
            {
                loadComplete = true;
                return;
            }
            var data = JsonUtility.FromJson<BuildSave>(PlayerScopedPrefs.GetString(SAVE_KEY));
            if (data == null || data.items == null)
            {
                loadComplete = true;
                return;
            }

            var gpc = GhostPlacementController.Instance;
            if (gpc == null)
            {
                Debug.LogWarning("[BuildPersistence] Không thấy GhostPlacementController → không dựng lại công trình.");
                return;
            }

            // Map ô đất theo vị trí (ô đã có sẵn trong scene).
            var byCell = new Dictionary<string, BuildSurfaceCell>();
            foreach (var cell in BuildSurfaceCell.All)
                if (cell != null) byCell[PosKey(cell.transform.position)] = cell;

            var pendingCrops = new List<PendingCrop>();
            int n = 0;
            foreach (var it in data.items)
            {
                if (it == null) continue;
                if (!byCell.TryGetValue(it.cellKey, out var cell) || cell == null) continue;
                if (cell.IsOccupied) continue; // ô đã có gì đó (vd vật đặt-sẵn) → bỏ qua, không chồng

                var go = gpc.PlaceFromSave(it.itemName, new Vector2Int(it.fx, it.fy),
                    new Vector3(it.px, it.py, it.pz), Quaternion.Euler(0f, it.rotY, 0f),
                    cell, it.materialId, it.cost);
                if (go == null) continue;
                n++;

                if (it.crop != null && it.crop.state > (int)FarmTile.TileState.Soil)
                {
                    var ft = go.GetComponentInChildren<FarmTile>();
                    if (ft != null) pendingCrops.Add(new PendingCrop { tile = ft, crop = it.crop });
                }
            }
            if (pendingCrops.Count > 0) StartCoroutine(RestoreCropsNextFrame(pendingCrops));
            else loadComplete = true;

            // ── Khôi phục CON VẬT (sau khi rào đã dựng) — re-spawn trên ô chuồng + đói-bù/chết-bù ──
            int na = 0;
            if (data.animals != null)
                foreach (var a in data.animals)
                    if (RestoreAnimal(a, byCell)) na++;

            Debug.Log($"[BuildPersistence] Restored {n} buildings ({pendingCrops.Count} có cây) + {na} animals.");
        }

        /// <summary>Re-spawn 1 con vật từ save trên các ô chuồng (BuildSurfaceCell) + khôi phục mốc đói/sản phẩm.</summary>
        private bool RestoreAnimal(AnimalSave a, Dictionary<string, BuildSurfaceCell> byCell)
        {
            if (a == null || a.cellKeys == null || a.cellKeys.Count == 0) return false;

            var cells = new List<BuildSurfaceCell>();
            foreach (var key in a.cellKeys)
                if (byCell.TryGetValue(key, out var c) && c != null && !c.HasAnimal) cells.Add(c);
            if (cells.Count == 0) return false;

            var def = YWonderLand.Managers.AnimalManager.LookupDefinition(a.itemId);
            if (def == null) return false;

            Vector3 pos = cells[0].SurfaceCenter;
            if (AnimalPrefabLibrary.Instance != null)
                pos.y += AnimalPrefabLibrary.Instance.GetSpawnHeightOffset(a.itemId);
            GameObject prefab = AnimalPrefabLibrary.Instance != null ? AnimalPrefabLibrary.Instance.GetPrefab(a.itemId) : null;

            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, pos, Quaternion.identity);
                go.name = prefab.name;
                var fa = go.GetComponent<FarmAnimal>();
                if (fa == null) fa = go.AddComponent<FarmAnimal>();
                fa.Initialize(def, false);
            }
            else
            {
                go = new GameObject($"Animal_{a.itemId}");
                go.transform.position = pos;
                go.AddComponent<FarmAnimal>().Initialize(def);
            }

            cells[0].SetAnimalOccupant(go, a.itemId);
            for (int i = 1; i < cells.Count; i++) cells[i].SetAnimal(true);

            var spawned = go.GetComponent<FarmAnimal>();
            if (spawned != null)
            {
                if (!string.IsNullOrWhiteSpace(a.instanceId))
                    spawned.animalInstanceId = a.instanceId;
                spawned.occupiedCells = new List<BuildSurfaceCell>(cells);
                spawned.RestoreAnimalState(a.feedRefUnix, a.produceRefUnix, a.hasBeenFed,
                    a.harvestsRemaining, a.hasProductReady, a.isVaccinated,
                    a.vaccineUntilUnix, a.sickRefUnix, a.state, a.sicknessRolled, a.pendingProduct);
            }
            return true;
        }

        // Chờ 1 frame cho FarmTile.Start() của prefab vừa dựng (visual sẵn sàng) rồi mới gắn cây.
        private IEnumerator RestoreCropsNextFrame(List<PendingCrop> pending)
        {
            var db = Resources.Load<YWonderLand.Data.CropDatabase>("CropDatabase");
            yield return null;
            foreach (var p in pending)
                if (p.tile != null) p.tile.RestoreSave(p.crop, db);
            loadComplete = true;
        }

        private static string PosKey(Vector3 p)
            => $"{Mathf.RoundToInt(p.x * 10f)}_{Mathf.RoundToInt(p.z * 10f)}";

        private void OnApplicationPause(bool paused) { if (paused) SaveBuildings(); }
        private void OnApplicationQuit() { SaveBuildings(); }

        private class PendingCrop { public FarmTile tile; public FarmTile.CropSave crop; }

        [System.Serializable]
        private class BuildSave { public List<BuildItem> items; public List<AnimalSave> animals; }

        [System.Serializable]
        private class BuildItem
        {
            public string cellKey;
            public string itemName;
            public int fx, fy;
            public float px, py, pz;
            public float rotY;
            public string materialId;
            public int cost;
            public FarmTile.CropSave crop;
        }

        [System.Serializable]
        private class AnimalSave
        {
            public string instanceId;
            public string itemId;
            public List<string> cellKeys; // ô chuồng con vật chiếm (ô đầu = ô neo)
            public double feedRefUnix;
            public double produceRefUnix;
            public bool hasBeenFed;
            public int harvestsRemaining;
            public bool hasProductReady;
            // -1 = save CŨ chưa có trường này → RestoreAnimalState suy từ hasProductReady (giữ nguyên
            // "1 sản phẩm chờ" cho người chơi cũ). Save MỚI ghi số vòng đã cộng dồn thật (>=0).
            public int pendingProduct = -1;
            public bool isVaccinated;
            public double vaccineUntilUnix; // mốc hết hạn vắc-xin
            public double sickRefUnix;      // mốc bắt đầu vòng nuôi (gốc tính thời điểm phát bệnh)
            public bool sicknessRolled;     // đã gieo xác suất phát bệnh cho vòng nuôi này chưa
            public int state;               // giữ trạng thái Bệnh qua các phiên
        }
    }
}
