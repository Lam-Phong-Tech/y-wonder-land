using UnityEngine;
using UnityEngine.InputSystem;
using YWonderLand.Data;
using System.Collections.Generic;

namespace YWonderLand.Environment
{
    /// <summary>
    /// FarmInteractionController: Gắn vào Player, xử lý tương tác với FarmTile.
    /// Click chuột trái / tap → tự động chọn hành động theo state của tile.
    /// Kết nối với InventoryManager, EconomyManager, CropDatabase.
    /// </summary>
    public class FarmInteractionController : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [Tooltip("Khoảng cách tối đa để tương tác với ô đất")]
        [SerializeField] private float interactRange = 10f;

        [Tooltip("Layer mask cho FarmTile raycasting")]
        [SerializeField] private LayerMask farmTileLayer = ~0; // Default: all layers

        [Header("Sản lượng tài nguyên (khách chốt)")]
        [Tooltip("Chặt xong 1 CÂY nhận bao nhiêu gỗ (khách: 10). Ép cứng nên không cần chỉnh từng cây trong scene.")]
        [SerializeField] private int treeYield = 10;
        [Tooltip("Đào xong 1 KHỐI ĐÁ nhận bao nhiêu đá (khách: 10).")]
        [SerializeField] private int rockYield = 10;
        [Tooltip("EXP nhận mỗi lần CHẶT CÂY xong (hệ Level tối giản).")]
        [SerializeField] private int resourceExp = 5;
        [Tooltip("EXP nhận mỗi lần ĐÀO KHOÁNG xong (khách chốt 22/06: 15).")]
        [SerializeField] private int mineExp = 15;


        [Header("References")]
        [SerializeField] private Camera mainCamera;

        private InventoryPopupController inventoryPopup;
        private string pendingSeedId; // Seed được chọn từ inventory, chờ gieo
        private FarmTile pendingPlantTile; // Tile đang chờ gieo hạt
        private YWonderLand.Environment.AnimalPenSpawner pendingPen; // Chuồng đang chờ chọn con vật từ túi
        private FarmAnimal pendingFeedAnimal; // Con vật đang chờ chọn thức ăn từ túi
        private List<BuildSurfaceCell> pendingEnclosure; // Vùng quây (rào) đang chờ thả thú
        private BuildSurfaceCell hoverEnclosureSeed;     // cache: ô đang rê để khỏi flood-fill mỗi frame
        private List<BuildSurfaceCell> hoverEnclosure;   // cache: kết quả vùng quây của ô đang rê

        void Start()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            inventoryPopup = Object.FindFirstObjectByType<InventoryPopupController>();

            // Subscribe to inventory item used event
            if (inventoryPopup != null)
            {
                inventoryPopup.OnItemUsed += OnInventoryItemSelected;
            }
        }

        void OnDestroy()
        {
            if (inventoryPopup != null)
            {
                inventoryPopup.OnItemUsed -= OnInventoryItemSelected;
            }
        }

        // Chặt/đào 1 nhịp rồi TỰ bắn toast khi HOÀN TẤT. Gọi trực tiếp (không qua event tĩnh
        // OnResourceHarvested dùng chung với Tutorial — vì 1 subscriber ném exception sẽ làm
        // các handler sau bị skip → mất toast). Lấy số lượng nhận được qua chênh lệch túi đồ.
        private bool HarvestResourceTick(HarvestableResource resource, float delta)
        {
            if (resource == null) return false;

            // Ép sản lượng cố định theo yêu cầu khách (10 gỗ / 10 đá) — set field public của resource,
            // KHÔNG sửa file QC HarvestableResource.cs. Áp mỗi nhịp cho chắc (idempotent).
            int forced = resource.type == HarvestableResource.ResourceType.Tree ? treeYield : rockYield;
            if (forced > 0)
            {
                resource.minYield = forced;
                resource.maxYield = forced;
            }

            // Nhiều cây/đá trong scene để TRỐNG yieldItemId -> không có đồ vào túi + toast không ra số.
            // Bù id mặc định theo loại (gỗ/đá) để đồ vào túi đúng và đếm được số lượng.
            if (string.IsNullOrEmpty(resource.yieldItemId))
                resource.yieldItemId = resource.type == HarvestableResource.ResourceType.Tree ? "wood_01" : "stone_01";

            string yieldId = resource.yieldItemId;
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            int before = (inv != null && !string.IsNullOrEmpty(yieldId)) ? inv.GetItemQuantity(yieldId) : 0;

            bool done = resource.Harvest(delta);
            if (done)
            {
                int gained = (inv != null && !string.IsNullOrEmpty(yieldId)) ? inv.GetItemQuantity(yieldId) - before : 0;
                string verb = resource.type == HarvestableResource.ResourceType.Tree ? "Chặt cây" : "Đào khoáng";
                if (gained > 0)
                    ScreenToast.ShowInfo($"{verb}: +{gained} {GetItemDisplayName(yieldId)}");
                else
                    ScreenToast.ShowInfo($"{verb} xong!");
                int rexp = resource.type == HarvestableResource.ResourceType.Rock ? mineExp : resourceExp;
                YWonderLand.Managers.ExperienceManager.Instance?.AddEXP(rexp);
                YWonderLand.Managers.AudioManager.Instance?.PlaySFX("chop");
            }
            return done;
        }

        private HarvestableResource currentHarvestTarget;
        private HarvestableResource _buttonHeldResource; // tài nguyên đang GIỮ nút "Chặt cây" trên HUD (mobile)
        private float _chopAnimTimer = 0f; // Đếm giờ để lặp animation chặt/đập khi đang giữ chuột

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Gameplay) return;
            if (UIPopupTracker.AnyOpen) return; // Đang mở popup -> ngừng tương tác thế giới (tránh click xuyên qua UI)

            // Pointer = lớp CHUNG cho Mouse (PC) lẫn Touchscreen (mobile) -> chạm tay cũng chạy.
            var pointer = Pointer.current;
            if (pointer == null || mainCamera == null) return;

            // GIỮ nút "Chặt cây"/"Đào khoáng" trên HUD: ngón đang trên UI nên xử lý chặt liên tục TRƯỚC
            // khi đoạn dưới bỏ qua tương tác (vì con trỏ trên UI). Thả nút -> onHoldEnd gỡ cờ này.
            if (_buttonHeldResource != null)
            {
                HoldChopResource(_buttonHeldResource);
                return;
            }

            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                if (currentHarvestTarget != null)
                {
                    currentHarvestTarget.CancelHarvest();
                    currentHarvestTarget = null;
                    if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                        YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();
                }
                return;
            }

            // NGẮM THEO TÂM MÀN HÌNH (crosshair) cho ỔN ĐỊNH: giữ chuột là chặt đúng vật ở tâm,
            // KHÔNG nhấp nháy/giật như khi raycast theo con trỏ (khi trúng khi trật). Khi con trỏ được
            // mở khóa để bấm chữ gợi ý "Chặt cây", mục tiêu ở tâm KHÔNG đổi -> gợi ý đứng yên, bấm được.
            Vector2 aimPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            if (pointer.press.wasPressedThisFrame) HandleClick(aimPos);
            if (pointer.press.isPressed) HandleHold(aimPos);
            else if (pointer.press.wasReleasedThisFrame)
            {
                _chopAnimTimer = 0f;
                if (currentHarvestTarget != null)
                {
                    currentHarvestTarget.CancelHarvest();
                    currentHarvestTarget = null;
                    if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                        YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();
                }
            }

            // Quét và vẽ Menu Nổi
            HandleHover(aimPos);

            // Xử lý Phím tắt Tương tác (PC)
            bool isTyping = ChatPanelController.Instance != null && ChatPanelController.Instance.IsTyping();
            if (!isTyping && Keyboard.current != null && currentActions != null)
            {
                foreach (var action in currentActions)
                {
                    if (action.keyName == "F" && Keyboard.current.fKey.wasPressedThisFrame) action.onClick?.Invoke();
                    if (action.keyName == "E" && Keyboard.current.eKey.wasPressedThisFrame) action.onClick?.Invoke();
                    if (action.keyName == "R" && Keyboard.current.rKey.wasPressedThisFrame) action.onClick?.Invoke();
                    if (action.keyName == "H" && Keyboard.current.hKey.wasPressedThisFrame) action.onClick?.Invoke();
                    if (action.keyName == "G" && Keyboard.current.gKey.wasPressedThisFrame) action.onClick?.Invoke();
                }
            }
        }

        private RaycastHit[] hoverHitResults = new RaycastHit[30];
        private GameObject currentHoverObject = null;
        private FarmAnimal.AnimalState lastAnimalState;
        private bool lastAnimalProductReady;
        private List<InteractionAction> currentActions = new List<InteractionAction>();

        private void HandleHover(Vector2 screenPos)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            int hitCount = Physics.RaycastNonAlloc(ray, hoverHitResults, 100f);
            System.Array.Sort(hoverHitResults, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));

            List<InteractionAction> foundActions = new List<InteractionAction>();
            GameObject foundObj = null;
            FarmAnimal.AnimalState currentAnimalState = FarmAnimal.AnimalState.Healthy;
            bool currentHasProduct = false;

            for (int i = 0; i < hitCount; i++)
            {
                var hit = hoverHitResults[i];
                if (hit.collider == null) continue;
                if (hit.collider.gameObject.CompareTag("Player")) continue;

                var animal = hit.collider.GetComponentInParent<FarmAnimal>();
                if (animal != null)
                {
                    foundObj = animal.gameObject;
                    currentAnimalState = animal.currentState;
                    currentHasProduct = animal.hasProductReady;

                    // [19/06] Đã bỏ tính năng "Vuốt ve" theo yêu cầu — chỉ còn Cho ăn / Thu hoạch / Chữa bệnh / Thông tin.
                    if (animal.currentState == FarmAnimal.AnimalState.Hungry)
                        foundActions.Add(new InteractionAction { keyName = "F", actionName = "Cho ăn", onClick = () => FeedAnimal(animal) });
                    if (animal.hasProductReady)
                        foundActions.Add(new InteractionAction { keyName = "R", actionName = "Thu hoạch", onClick = () => HarvestAnimal(animal) });
                    if (animal.currentState == FarmAnimal.AnimalState.Sick)
                        foundActions.Add(new InteractionAction { keyName = "H", actionName = "Chữa bệnh", onClick = () => HealAnimal(animal) });

                    foundActions.Add(new InteractionAction { keyName = "Click", actionName = "Thông tin", onClick = () => { if (AnimalInteractionPopupController.Instance != null) AnimalInteractionPopupController.Instance.Show(animal); } });
                    break;
                }
                else if (hit.collider.TryGetComponent<HarvestableResource>(out var resource) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<HarvestableResource>(out resource)))
                {
                    // Chỉ hiện nút khi TÂM NGẮM chạm bề mặt cây/đá trong tầm với (resource.interactionRange).
                    // Đo từ nhân vật tới ĐIỂM CHẠM (hit.point) cho trực quan, đúng cả với cây to.
                    Vector3 hoverPlayerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
                    // ĐÀO ĐÁ chỉ ở thành phố → ở đảo khác KHÔNG hiện nút "Đào khoáng" (chặt cây vẫn hiện).
                    bool resourceUsable = resource.type != HarvestableResource.ResourceType.Rock || IsOnCityIsland();
                    if (resourceUsable && Vector3.Distance(hoverPlayerPos, hit.point) <= resource.interactionRange)
                    {
                        foundObj = resource.gameObject;
                        string actionStr = resource.type == HarvestableResource.ResourceType.Tree ? "Chặt cây" : "Đào khoáng";
                        var res = resource; // capture cho closure
                        foundActions.Add(new InteractionAction
                        {
                            keyName = "Click",
                            actionName = actionStr,
                            // GIỮ nút = chặt liên tục (mobile). Giữ -> đặt cờ; thả -> gỡ cờ + ẩn thanh tiến trình.
                            onHoldStart = () => { _buttonHeldResource = res; },
                            onHoldEnd = () =>
                            {
                                _buttonHeldResource = null;
                                _chopAnimTimer = 0f;
                                if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                                    YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();
                            }
                        });
                    }
                    break;
                }
                else if (hit.collider.TryGetComponent<MerchantNPC>(out var merchant) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<MerchantNPC>(out merchant)))
                {
                    foundObj = merchant.gameObject;
                    foundActions.Add(new InteractionAction { keyName = "Click", actionName = merchant.GetInteractionLabel(), onClick = () => merchant.Interact() });
                    break;
                }
                else if (hit.collider.GetComponentInParent<YWonderLand.Environment.AnimalPenSpawner>() != null)
                {
                    var penS = hit.collider.GetComponentInParent<YWonderLand.Environment.AnimalPenSpawner>();
                    if (penS.HasSpace)
                    {
                        foundObj = penS.gameObject;
                        foundActions.Add(new InteractionAction { keyName = "Click", actionName = "Thả thú", onClick = () => OpenPenAnimalPicker(penS) });
                    }
                    break;
                }
                else if (hit.collider.TryGetComponent<FarmTile>(out var tile) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<FarmTile>(out tile)))
                {
                    foundObj = tile.gameObject;
                    string actName = "Tương tác";
                    switch (tile.currentState)
                    {
                        case FarmTile.TileState.Soil: actName = "Cuốc đất"; break;
                        case FarmTile.TileState.Plowed: actName = "Gieo hạt"; break;
                        case FarmTile.TileState.Planted: actName = "Tưới nước"; break;
                        case FarmTile.TileState.Watered: actName = "Tưới nước"; break;
                        case FarmTile.TileState.Ripe: actName = "Thu hoạch"; break;
                    }
                    foundActions.Add(new InteractionAction { keyName = "Click", actionName = actName, onClick = () => PerformTileAction(tile) });
                    break;
                }
                else if (hit.collider.TryGetComponent<FishingSpot>(out var spot) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<FishingSpot>(out spot)))
                {
                    if (hit.distance <= 15f && IsFishingAllowedHere())
                    {
                        foundObj = spot.gameObject;
                        foundActions.Add(new InteractionAction { keyName = "F", actionName = "Câu cá", onClick = () => StartFishing(spot) });
                    }
                    break;
                }
                else if (hit.collider.GetComponentInParent<WaterSource>() is WaterSource waterSrc && waterSrc != null)
                {
                    // Vùng ao MÚC ĐƯỢC (không phải nước biển) → bấm "Múc nước" lấy xô nước về túi.
                    Vector3 wPlayerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
                    if (Vector3.Distance(wPlayerPos, hit.point) <= waterSrc.interactRange)
                    {
                        foundObj = waterSrc.gameObject;
                        var ws = waterSrc;
                        foundActions.Add(new InteractionAction { keyName = "Click", actionName = "Múc nước", onClick = () => ScoopWater(ws) });
                    }
                    break;
                }
                else if (hit.collider.GetComponentInParent<BuildSurfaceCell>() is BuildSurfaceCell penCell && penCell != null)
                {
                    // Ô CÓ RÀO = 1 ô chuồng -> "Thả thú".
                    if (penCell.HasFence)
                    {
                        if (penCell != hoverEnclosureSeed)
                        {
                            hoverEnclosureSeed = penCell;
                            hoverEnclosure = PenEnclosure.FindPen(penCell);
                        }
                        if (hoverEnclosure != null)
                        {
                            foundObj = penCell.gameObject;
                            var encl = hoverEnclosure;
                            foundActions.Add(new InteractionAction { keyName = "Click", actionName = "Thả thú", onClick = () => OpenEnclosurePicker(encl) });
                            foundActions.Add(new InteractionAction { keyName = "G", actionName = "Hủy chuồng", onClick = () => DemolishEnclosure(encl) });
                        }
                        break;
                    }
                    if (!penCell.IsFree) continue; // ô bị chiếm bởi công trình khác -> xuyên qua
                    break; // ô đất trống thường (không phải chuồng) -> bỏ
                }
                else if (hit.collider.GetComponentInParent<FenceAutoConnect>() != null)
                {
                    continue; // Xuyên qua HÀNG RÀO để thấy nền đất bên trong chuồng (tương tác thả thú)
                }
                else if (!hit.collider.isTrigger) break; // Bị che khuất
            }

            // Update UI if target or state changed
            if (foundActions.Count > 0)
            {
                if (foundObj != currentHoverObject || currentAnimalState != lastAnimalState || currentHasProduct != lastAnimalProductReady)
                {
                    currentHoverObject = foundObj;
                    lastAnimalState = currentAnimalState;
                    lastAnimalProductReady = currentHasProduct;
                    currentActions = foundActions;
                    if (GameHUDController.Instance != null) GameHUDController.Instance.ShowInteractionPrompts(foundActions);
                }
            }
            else
            {
                if (currentHoverObject != null)
                {
                    currentHoverObject = null;
                    currentActions.Clear();
                    if (GameHUDController.Instance != null) GameHUDController.Instance.HideInteractionPrompt();
                }
            }
        }

        // --- CÁC HÀM XỬ LÝ SỰ KIỆN NÚT BẤM ---
        // Đang ở đảo Thành phố? (CÂU CÁ + ĐÀO ĐÁ chỉ ở thành phố — khách chốt 20/06).
        private bool IsOnCityIsland()
        {
            var itm = IslandTravelManager.Instance;
            return itm != null && itm.CurrentIslandId == "city";
        }
        private bool IsFishingAllowedHere() => IsOnCityIsland();

        private void StartFishing(FishingSpot spot)
        {
            if (!IsFishingAllowedHere())
            {
                ScreenToast.Show("Chỉ câu cá được ở Đảo Thành phố thôi!");
                return;
            }

            PlayerController player = PlayerController.Instance;
            if (player != null) player.PlayActionAnimation("Fishing", 8.7f, YWonderLand.Player.ToolType.FishingRod); // khớp cửa sổ căn cá 8.7s

            // Chuẩn bị câu: lưu cao độ mặt nước, CHỜ Animation Event (frame vung cần) bắn dây ra.
            if (FishingLineController.Instance != null && spot != null)
                FishingLineController.Instance.PrepareCast(spot.transform.position.y);

            var fishingUI = Object.FindFirstObjectByType<FishingOverlayController>();
            if (fishingUI != null) fishingUI.Show();
        }

        /// <summary>Cổng public cho UI (popup Thú nuôi) gọi luồng cho ăn qua túi đồ.</summary>
        public void BeginFeed(FarmAnimal animal) => FeedAnimal(animal);

        // Cho ăn = mở túi (tab Thực phẩm) chọn thức ăn (tạm dùng Bắp ngô) -> animation Feed.
        private void FeedAnimal(FarmAnimal animal)
        {
            if (animal == null) return;
            pendingFeedAnimal = animal;
            pendingPen = null;
            pendingPlantTile = null;
            pendingEnclosure = null;
            if (PlayerController.Instance != null) PlayerController.Instance.FaceTowards(animal.transform.position);

            EnsureStarterFeed(animal); // demo: cấp ĐÚNG thức ăn của loài (theo tài liệu) để chọn

            if (inventoryPopup == null)
                inventoryPopup = Object.FindFirstObjectByType<InventoryPopupController>();
            if (inventoryPopup != null)
            {
                inventoryPopup.ShowAtTab("food");
                Debug.Log("[FarmInteraction] Mở Túi (Thực phẩm) để chọn thức ăn cho động vật.");
            }
        }

        // Người chơi chọn 1 thức ăn trong túi khi đang cho ăn -> VALIDATE đúng thức ăn của loài
        // (theo tài liệu: Thức ăn chính / phụ + số lượng) -> trừ đúng số -> animation Feed.
        private void HandleFeedSelected(string itemId)
        {
            var animal = pendingFeedAnimal;
            if (animal == null) return;
            var def = animal.data;

            // Tên hiển thị của thức ăn vừa chọn (để so với foodMainName/foodAltName của loài).
            string selName = GetItemDisplayName(itemId);
            int required = 0;
            string matchedName = null;
            if (def != null && NameMatches(selName, def.foodMainName)) { required = Mathf.Max(1, def.foodMainAmount); matchedName = def.foodMainName; }
            else if (def != null && NameMatches(selName, def.foodAltName)) { required = Mathf.Max(1, def.foodAltAmount); matchedName = def.foodAltName; }

            if (required <= 0)
            {
                // Sai thức ăn: KHÔNG trừ đồ, giữ túi mở để chọn lại.
                ScreenToast.Show($"{(def != null ? def.animalName : "Thú")} không ăn '{selName}'! Cần: {FoodOptionsText(def)}.");
                return;
            }

            var inv = YWonderLand.Managers.InventoryManager.Instance;
            int have = inv != null ? inv.GetItemQuantity(itemId) : 0;
            if (have < required)
            {
                ScreenToast.Show($"Cần {required}x {matchedName}, túi chỉ có {have}.");
                return;
            }
            if (inv != null) inv.RemoveItem(itemId, required);

            pendingFeedAnimal = null;

            var player = PlayerController.Instance;
            if (player != null)
            {
                player.FaceTowards(animal.transform.position);
                player.PlayActionAnimation("Feed", 0f, YWonderLand.Player.ToolType.AnimalFeed); // cầm nắm cám rải xuống
            }
            animal.Feed();
            ScreenToast.ShowInfo($"Đã cho {(def != null ? def.animalName : "thú")} ăn {required}x {matchedName}.");

            if (inventoryPopup != null) inventoryPopup.Hide();
        }

        // Demo helper: cấp ĐÚNG thức ăn của loài (chính + phụ) để test, thay vì luôn dùng ngô.
        // Production: người chơi tự trồng/mua thức ăn đúng loại.
        private void EnsureStarterFeed(FarmAnimal animal)
        {
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv == null || animal == null || animal.data == null) return;
            GiveFoodForDemo(inv, animal.data.foodMainName, Mathf.Max(1, animal.data.foodMainAmount) * 3);
            // Bỏ qua thức ăn phụ amount 0 (vd 'Cám' — không phải item trong game) → tránh cảnh báo thừa.
            if (animal.data.foodAltAmount > 0)
                GiveFoodForDemo(inv, animal.data.foodAltName, animal.data.foodAltAmount * 3);
        }

        private void GiveFoodForDemo(YWonderLand.Managers.InventoryManager inv, string foodName, int amount)
        {
            string id = ResolveItemIdByName(foodName);
            if (string.IsNullOrEmpty(id))
            {
                if (!string.IsNullOrEmpty(foodName))
                    Debug.LogWarning($"[FarmInteraction] Không tìm thấy item khớp tên thức ăn '{foodName}' trong ItemDatabase (kiểm tra lại tên trong AnimalDefinition vs ItemDatabase).");
                return;
            }
            if (inv.GetItemQuantity(id) <= 0) inv.AddItem(id, amount);
        }

        // ── Tra cứu tên ↔ id thức ăn qua ItemDatabase ──
        private YWonderLand.Data.ItemDatabase _foodDb;
        private YWonderLand.Data.ItemDatabase FoodDb
        {
            get
            {
                if (_foodDb == null) _foodDb = Resources.Load<YWonderLand.Data.ItemDatabase>("ItemDatabase");
                return _foodDb;
            }
        }

        private string GetItemDisplayName(string itemId)
        {
            var def = FoodDb != null ? FoodDb.GetItem(itemId) : null;
            return def != null && !string.IsNullOrEmpty(def.itemName) ? def.itemName : itemId;
        }

        private string ResolveItemIdByName(string displayName)
        {
            var db = FoodDb;
            if (db == null || db.items == null || string.IsNullOrEmpty(displayName)) return null;
            foreach (var it in db.items)
                if (it != null && NameMatches(it.itemName, displayName)) return it.id;
            return null;
        }

        private static bool NameMatches(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return string.Equals(a.Trim(), b.Trim(), System.StringComparison.OrdinalIgnoreCase);
        }

        private static string FoodOptionsText(YWonderLand.Data.AnimalDefinition d)
        {
            if (d == null) return "—";
            string m = !string.IsNullOrEmpty(d.foodMainName) ? $"{Mathf.Max(1, d.foodMainAmount)}x {d.foodMainName}" : null;
            string a = !string.IsNullOrEmpty(d.foodAltName) ? $"{Mathf.Max(1, d.foodAltAmount)}x {d.foodAltName}" : null;
            if (m != null && a != null) return $"{m} hoặc {a}";
            return m ?? a ?? "—";
        }

        private void HealAnimal(FarmAnimal animal)
        {
            PlayerController player = PlayerController.Instance;
            // Chưa có animation "tiêm/chữa bệnh" riêng -> tạm dùng "Feed" (động tác đưa tay) cho đỡ trống
            if (player != null) player.PlayActionAnimation("Feed", 0f);
            animal.Heal();
        }

        private void HarvestAnimal(FarmAnimal animal)
        {
            // Lấy chu kỳ ra sản phẩm TRƯỚC khi harvest (vụ cuối có thể huỷ con vật).
            float animalDays = (animal != null && animal.data != null)
                ? animal.data.produceCycleTimeSec / YWonderLand.Core.GameTimeConfig.SecondsPerGameDay : 0f;

            if (animal.HarvestProduct(out string itemId, out int amount))
            {
                Managers.InventoryManager.Instance?.AddItem(itemId, amount);

                // EXP (khách chốt 22/06: số NGÀY 1 chu kỳ ra sản phẩm × 10).
                int aexp = Mathf.Max(1, Mathf.RoundToInt(animalDays * 10f));
                Managers.ExperienceManager.Instance?.AddEXP(aexp);

                // Vụ cuối: HarvestProduct tự huỷ con vật (làm thịt) + đã có toast "Làm thịt..." riêng.
                // Chỉ báo thu sản phẩm khi con vật CÒN SỐNG để khỏi đè toast làm thịt.
                if (animal != null && !string.IsNullOrEmpty(itemId) && amount > 0)
                    ScreenToast.ShowInfo($"Thu hoạch: +{amount} {GetItemDisplayName(itemId)}");
            }
        }

        private void ClickHarvestResource(HarvestableResource resource)
        {
            // ĐÀO ĐÁ chỉ ở thành phố — ở đảo khác thì chặn (chặt cây vẫn bình thường).
            if (resource != null && resource.type == HarvestableResource.ResourceType.Rock && !IsOnCityIsland())
            {
                ScreenToast.Show("Chỉ đào đá được ở Đảo Thành phố thôi!");
                return;
            }

            Vector3 clickPlayerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
            if (Vector3.Distance(clickPlayerPos, resource.transform.position) > resource.interactionRange + 1.5f)
                return;

            PlayerController player = PlayerController.Instance;
            
            // Ép nhân vật múa hoạt ảnh và cầm rìu/cúp
            if (player != null) 
            {
                if (resource.type == HarvestableResource.ResourceType.Tree)
                    player.PlayActionAnimation("TreeCuttingV4", 1f, YWonderLand.Player.ToolType.Axe);
                else
                    player.PlayActionAnimation("Mining", 1f, YWonderLand.Player.ToolType.Pickaxe); // đập đá cầm Cúp
            }

            // Hiện thanh tiến trình
            if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                YWonderLand.UI.ResourceInteractionUIController.Instance.Show(resource);

            // Tăng tiến độ chặt (1 giây / click). (Ví dụ cây 3 máu thì click 3 phát là gãy)
            if (HarvestResourceTick(resource, 1.0f))
            {
                // Nếu gãy rồi thì ẩn thanh tiến trình
                if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                    YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();
            }
        }

        // Chặt/đập LIÊN TỤC 1 tài nguyên trong lúc GIỮ nút HUD (mobile). Tăng tiến độ theo thời gian
        // thật + lặp animation mỗi ~0.9s -> giống hệt giữ chuột trên thế giới, nhưng kích hoạt từ nút.
        private void HoldChopResource(HarvestableResource resource)
        {
            if (resource == null || !resource.isHarvestable) { _buttonHeldResource = null; return; }

            PlayerController player = PlayerController.Instance;
            Vector3 ppos = player != null ? player.transform.position : transform.position;
            if (Vector3.Distance(ppos, resource.transform.position) > resource.interactionRange + 1.5f) return; // quá xa -> khựng

            _chopAnimTimer -= Time.deltaTime;
            if (_chopAnimTimer <= 0f && player != null)
            {
                var tool = resource.type == HarvestableResource.ResourceType.Tree
                    ? YWonderLand.Player.ToolType.Axe : YWonderLand.Player.ToolType.Pickaxe;
                string anim = resource.type == HarvestableResource.ResourceType.Tree ? "TreeCuttingV4" : "Mining";
                player.PlayActionAnimation(anim, 1.0f, tool);
                _chopAnimTimer = 0.9f;
            }

            if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                YWonderLand.UI.ResourceInteractionUIController.Instance.Show(resource);

            if (HarvestResourceTick(resource, Time.deltaTime))
            {
                if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                    YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();
                _buttonHeldResource = null; // gãy rồi -> dừng
            }
        }

        private void PerformTileAction(FarmTile tile)
        {
            if (PlayerController.Instance == null) return; // chống NullReferenceException khi player chưa spawn / đang teleport
            float dist = Vector3.Distance(PlayerController.Instance.transform.position, tile.transform.position);
            if (dist > interactRange) return;

            // Xoay nhân vật mặt THẲNG về ô đất trước khi cuốc/gieo/tưới — khớp đúng hướng,
            // tránh lệch do camera lệch vai (GTA-style). Action lock sẽ giữ nguyên hướng này.
            PlayerController.Instance.FaceTowards(tile.transform.position);

            switch (tile.currentState)
            {
                case FarmTile.TileState.Soil: HandlePlow(tile); break;
                case FarmTile.TileState.Plowed: HandleOpenSeedSelection(tile); break;
                case FarmTile.TileState.Planted: HandleWater(tile); break;
                case FarmTile.TileState.Watered: HandleWater(tile); break;
                case FarmTile.TileState.Ripe: HandleHarvest(tile); break;
            }
        }

        private void HandleHold(Vector2 screenPos)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

            foreach (var hit in hits)
            {
                HarvestableResource resource = hit.collider.GetComponent<HarvestableResource>();
                if (resource != null)
                {
                    // ĐÀO ĐÁ chỉ ở thành phố — bỏ qua tảng đá nếu đang ở đảo khác (chặt cây thì vẫn được).
                    if (resource.type == HarvestableResource.ResourceType.Rock && !IsOnCityIsland()) continue;

                    // Quá xa thì không cho chặt/đập (đo tới điểm chạm, khớp với lúc hiện gợi ý)
                    Vector3 holdPlayerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
                    if (Vector3.Distance(holdPlayerPos, hit.point) > resource.interactionRange)
                        continue;

                    if (currentHarvestTarget != null && currentHarvestTarget != resource)
                    {
                        currentHarvestTarget.CancelHarvest();
                    }
                    
                    currentHarvestTarget = resource;

                    // Lặp animation chặt/đập cho nhân vật trong lúc đang giữ chuột
                    _chopAnimTimer -= Time.deltaTime;
                    if (_chopAnimTimer <= 0f && PlayerController.Instance != null)
                    {
                        var chopTool = resource.type == HarvestableResource.ResourceType.Tree
                            ? YWonderLand.Player.ToolType.Axe : YWonderLand.Player.ToolType.Pickaxe;
                        string animName = resource.type == HarvestableResource.ResourceType.Tree ? "TreeCuttingV4" : "Mining";
                        PlayerController.Instance.PlayActionAnimation(animName, 1.0f, chopTool);
                        _chopAnimTimer = 0.9f;
                    }

                    // Show progress bar
                    if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                    {
                        YWonderLand.UI.ResourceInteractionUIController.Instance.Show(resource);
                    }
                    
                    if (HarvestResourceTick(resource, Time.deltaTime))
                    {
                        // Completed
                        currentHarvestTarget = null;
                        if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                            YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();
                    }
                    return;
                }
            }

            // Mất mục tiêu trong lúc đang đè
            if (currentHarvestTarget != null)
            {
                currentHarvestTarget.CancelHarvest();
                currentHarvestTarget = null;
                if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                    YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();
            }
        }

        private void HandleClick(Vector2 screenPos)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            
            // Use RaycastAll to ensure we don't get blocked by invisible colliders or the Player
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

            Debug.Log($"[FarmInteraction] Bấm chuột! Tia laze trúng {hits.Length} vật thể.");

            // 1. Dùng tia quét đã sắp xếp
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.CompareTag("Player")) continue;

                FarmAnimal animal = hit.collider.GetComponent<FarmAnimal>();
                if (animal != null)
                {
                    if (AnimalInteractionPopupController.Instance != null) AnimalInteractionPopupController.Instance.Show(animal);
                    return;
                }

                // Múc nước bằng TÂM NGẮM (click) — giống chặt cây/đào, chạy cả PC lẫn mobile.
                var waterSrcClick = hit.collider.GetComponentInParent<WaterSource>();
                if (waterSrcClick != null)
                {
                    Vector3 wpos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
                    if (Vector3.Distance(wpos, hit.point) <= waterSrcClick.interactRange) ScoopWater(waterSrcClick);
                    return;
                }

                FarmTile tile = hit.collider.GetComponentInParent<FarmTile>() ?? hit.collider.GetComponent<FarmTile>();
                if (tile != null)
                {
                    float dist = 0f;
                    GameObject player = GameObject.FindWithTag("Player");
                    if (player != null)
                    {
                        dist = Vector3.Distance(player.transform.position, tile.transform.position);
                    }
                    else
                    {
                        dist = Vector3.Distance(transform.position, tile.transform.position);
                    }

                    if (dist > interactRange)
                    {
                        Debug.Log($"[FarmInteraction] Too far from tile! dist={dist:F1} > max={interactRange}");
                        return;
                    }

                    // Auto-select action based on tile state
                    switch (tile.currentState)
                    {
                        case FarmTile.TileState.Soil:
                            HandlePlow(tile);
                            break;

                        case FarmTile.TileState.Plowed:
                            HandleOpenSeedSelection(tile);
                            break;

                        case FarmTile.TileState.Planted:
                            HandleWater(tile);
                            break;

                        case FarmTile.TileState.Watered:
                            HandleWater(tile); // tưới LẠI bằng tâm ngắm (giống nút gợi ý)
                            break;

                        case FarmTile.TileState.Ripe:
                            HandleHarvest(tile);
                            break;
                    }
                    return; // Handled tile, stop here
                }

                // Chuồng từ HÀNG RÀO: click thẳng vào ô rào -> mở túi chọn thú thả (PC click trực tiếp).
                var penCellClick = hit.collider.GetComponentInParent<BuildSurfaceCell>();
                if (penCellClick != null && penCellClick.HasFence)
                {
                    var pen = PenEnclosure.FindPen(penCellClick);
                    if (pen != null) OpenEnclosurePicker(pen);
                    return;
                }

                // Chuồng thú (kiểu cũ): click để mở túi đồ (tab Thú nuôi) chọn con vật thả vào chuồng.
                var penSpawner = hit.collider.GetComponentInParent<YWonderLand.Environment.AnimalPenSpawner>();
                if (penSpawner != null)
                {
                    Vector3 ppos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
                    if (Vector3.Distance(ppos, penSpawner.transform.position) <= interactRange)
                        OpenPenAnimalPicker(penSpawner);
                    return;
                }

                MerchantNPC merchant = hit.collider.GetComponent<MerchantNPC>();
                if (merchant != null)
                {
                    Debug.Log($"[FarmInteraction] TÌM THẤY MERCHANT: {merchant.gameObject.name}! Mở Shop...");
                    merchant.Interact();
                    return;
                }

                // Bỏ qua collider HÀNG RÀO để tia tới được ô chuồng/đất phía sau (click thả thú).
                if (hit.collider.GetComponentInParent<FenceAutoConnect>() != null) continue;

                // Thêm kiểm tra chặn tia
                if (!hit.collider.isTrigger)
                {
                    // Chạm đất hoặc tường, hủy quét xuyên
                    break;
                }
            }
        }

        // ── Action Handlers ──

        private void HandlePlow(FarmTile tile)
        {
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv != null && inv.GetItemQuantity("hoe_01") <= 0)
            {
                Debug.Log("[FarmInteraction] Auto-giving hoe_01 for demo purposes.");
                inv.AddItem("hoe_01", 1);
            }

            if (tile.InteractPlow())
            {
                Debug.Log("[FarmInteraction] Plowed tile!");
                
                // Múa động tác Cuốc đất -> cầm CUỐC (Hoe), không phải rìu
                if (PlayerController.Instance != null)
                {
                    PlayerController.Instance.PlayActionAnimation("Hoeing", 3.0f, YWonderLand.Player.ToolType.Hoe);
                }
            }
        }

        private void HandleOpenSeedSelection(FarmTile tile)
        {
            // Ghi nhớ ô đất đang chờ gieo, rồi mở Túi đồ ở tab Hạt giống để người chơi CHỌN loại cây.
            pendingPlantTile = tile;

            // Demo helper: nếu chưa có hạt nào thì tặng gói hạt khởi đầu để có cái mà chọn.
            EnsureStarterSeeds();

            if (inventoryPopup == null)
                inventoryPopup = Object.FindFirstObjectByType<InventoryPopupController>();

            if (inventoryPopup != null)
            {
                inventoryPopup.ShowAtTab("seeds");
                Debug.Log("[FarmInteraction] Mở Túi đồ (Hạt giống) để chọn cây trồng cho ô đất.");
            }
            else
            {
                // Dự phòng: không có túi đồ -> gieo tạm cà rốt để không kẹt demo
                Debug.LogWarning("[FarmInteraction] Không tìm thấy InventoryPopup -> tạm gieo cà rốt.");
                if (tile.InteractPlant("carrot_seed_01") && PlayerController.Instance != null)
                    PlayerController.Instance.PlayActionAnimation("Planting", 0f, YWonderLand.Player.ToolType.SeedBag, 2f); // x2 cho nhanh
            }
        }

        // Đảm bảo túi đồ LUÔN có đủ 3 loại hạt (đã có model 3D) để người chơi chọn trồng.
        private void EnsureStarterSeeds()
        {
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv == null) return;

            string[] starterSeeds = { "carrot_seed_01", "cabbage_seed_01", "corn_seed_01" };
            foreach (var s in starterSeeds)
            {
                if (inv.GetItemQuantity(s) <= 0)
                {
                    inv.AddItem(s, 3);
                    Debug.Log($"[FarmInteraction] Bổ sung hạt giống cho demo: {s} +3");
                }
            }
        }

        // Mở túi đồ (tab Thú nuôi) để chọn con vật thả vào chuồng đang đứng.
        private void OpenPenAnimalPicker(YWonderLand.Environment.AnimalPenSpawner pen)
        {
            if (pen == null) return;
            pendingPen = pen;
            pendingPlantTile = null; // tránh nhầm với luồng gieo hạt
            pendingEnclosure = null;
            if (PlayerController.Instance != null) PlayerController.Instance.FaceTowards(pen.transform.position);

            EnsureStarterAnimals(); // demo: chắc chắn có vài con vật để test (production: mua từ shop)

            if (inventoryPopup == null)
                inventoryPopup = Object.FindFirstObjectByType<InventoryPopupController>();
            if (inventoryPopup != null)
            {
                inventoryPopup.ShowAtTab("animals");
                Debug.Log("[FarmInteraction] Mở Túi đồ (Thú nuôi) để chọn con vật thả vào chuồng.");
            }
        }

        // Mở túi đồ (tab Thú nuôi) để chọn con vật thả vào VÙNG QUÂY (chuồng từ hàng rào).
        private void OpenEnclosurePicker(List<BuildSurfaceCell> interior)
        {
            if (interior == null) return;
            pendingEnclosure = interior;
            pendingPen = null;
            pendingFeedAnimal = null;
            pendingPlantTile = null;

            EnsureStarterAnimals(); // demo: có sẵn vài con để test

            if (inventoryPopup == null)
                inventoryPopup = Object.FindFirstObjectByType<InventoryPopupController>();
            if (inventoryPopup != null)
            {
                inventoryPopup.ShowAtTab("animals");
                int free = PenEnclosure.AvailableCount(interior);
                Debug.Log($"[FarmInteraction] Mở Túi (Thú nuôi) — chuồng còn {free} ô trống.");
            }
        }

        // Người chơi chọn con vật từ túi cho VÙNG QUÂY -> validate số ô rồi thả hoặc báo lỗi.
        private void HandleEnclosureAnimalSelected(string itemId)
        {
            var interior = pendingEnclosure;
            pendingEnclosure = null;
            if (interior == null) return;

            var def = YWonderLand.Managers.AnimalManager.LookupDefinition(itemId);
            int need = def != null ? Mathf.Max(1, def.penSlots) : 1;
            int free = PenEnclosure.AvailableCount(interior);

            if (free < need)
            {
                ScreenToast.Show($"Chuồng không đủ chỗ! Cần {need} ô, còn {free} ô.");
                if (inventoryPopup != null) inventoryPopup.Hide();
                return;
            }

            // Gom 'need' ô chuồng còn trống cho con vật đứng + đánh dấu đã có thú.
            var cells = new List<BuildSurfaceCell>();
            foreach (var c in interior)
            {
                if (c != null && !c.HasAnimal) { cells.Add(c); if (cells.Count >= need) break; }
            }
            if (cells.Count < need) { ScreenToast.Show("Chuồng không đủ chỗ!"); return; }

            Vector3 pos = cells[0].SurfaceCenter;
            if (AnimalPrefabLibrary.Instance != null)
                pos.y += AnimalPrefabLibrary.Instance.GetSpawnHeightOffset(itemId); // chỉnh cao theo pivot từng loài
            GameObject prefab = AnimalPrefabLibrary.Instance != null ? AnimalPrefabLibrary.Instance.GetPrefab(itemId) : null;
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, pos, Quaternion.identity);
                go.name = prefab.name;
                // Gắn logic vật nuôi (đói/sản phẩm theo mốc thời gian) lên CHÍNH model thật.
                // false = không sinh khối primitive, chỉ thêm thanh HP nổi trên đầu.
                var fa = go.GetComponent<FarmAnimal>();
                if (fa == null) fa = go.AddComponent<FarmAnimal>();
                if (def != null) fa.Initialize(def, false);
            }
            else
            {
                go = new GameObject($"Animal_{itemId}");
                go.transform.position = pos;
                var faNew = go.AddComponent<FarmAnimal>();
                if (def != null) faNew.Initialize(def);
            }

            // Ô ĐẦU = ô neo: giữ tham chiếu con vật + itemId (để trả con giống khi phá chuồng).
            // Các ô còn lại chỉ "mượn chỗ" (HasAnimal=true, không giữ GameObject).
            cells[0].SetAnimalOccupant(go, itemId);
            for (int i = 1; i < cells.Count; i++) cells[i].SetAnimal(true);

            // Cho con vật biết nó chiếm ô nào → tự trả ô về trống khi làm thịt vụ cuối.
            var spawned = go.GetComponent<FarmAnimal>();
            if (spawned != null)
            {
                spawned.occupiedCells = new List<BuildSurfaceCell>(cells);
                FarmAnimal.RaiseSpawned(spawned); // báo tutorial: đã thả thú (flow mới)
            }

            ScreenToast.ShowInfo($"Đã thả {(def != null ? def.animalName : itemId)} ({need} ô).");
            if (inventoryPopup != null) inventoryPopup.Hide();
        }

        // Phá CẢ CỤM rào (chuồng): trả con giống về túi → gỡ hàng rào → hoàn 1 phần giá build.
        private void DemolishEnclosure(List<BuildSurfaceCell> pen)
        {
            if (pen == null || pen.Count == 0) return;

            var inv = YWonderLand.Managers.InventoryManager.Instance;
            int refundWood = 0;
            int refundStone = 0;
            int returnedAnimals = 0;
            int removedFences = 0;

            foreach (var c in pen)
            {
                if (c == null) continue;

                // 1) Trả con giống về túi (chỉ ô NEO mới giữ tham chiếu thật).
                if (c.AnimalObject != null)
                {
                    if (inv != null && !string.IsNullOrEmpty(c.AnimalItemId))
                    {
                        inv.AddItem(c.AnimalItemId, 1);
                        returnedAnimals++;
                    }
                    Destroy(c.AnimalObject);
                }
                c.ClearAnimal();

                // 2) Cộng dồn VẬT LIỆU đã tốn để hoàn (rào lưu vật liệu lúc đặt).
                if (c.BuildMaterialId == "wood_01") refundWood += c.BuildCost;
                else if (c.BuildMaterialId == "stone_01") refundStone += c.BuildCost;
                c.SetBuildMaterial("", 0);

                // 3) Gỡ hàng rào khỏi ô (FenceAutoConnect.OnDisable tự refresh các cạnh còn lại).
                if (c.Occupant != null) { Destroy(c.Occupant); removedFences++; }
                c.Clear();
            }

            // 4) Hoàn lại VẬT LIỆU đã tốn (đầy đủ — phá đồ của mình thì trả lại đúng loại).
            if (inv != null)
            {
                if (refundWood > 0) inv.AddItem("wood_01", refundWood);
                if (refundStone > 0) inv.AddItem("stone_01", refundStone);
            }

            // 5) Dọn cache + ẩn gợi ý (ô không còn là chuồng nữa).
            hoverEnclosureSeed = null;
            hoverEnclosure = null;
            pendingEnclosure = null;
            currentHoverObject = null;
            currentActions.Clear();
            if (GameHUDController.Instance != null) GameHUDController.Instance.HideInteractionPrompt();

            string msg = $"Đã phá chuồng ({removedFences} ô rào)";
            if (returnedAnimals > 0) msg += $", trả {returnedAnimals} con về túi";
            if (refundWood > 0) msg += $", +{refundWood} Gỗ";
            if (refundStone > 0) msg += $", +{refundStone} Đá";
            ScreenToast.ShowInfo(msg + ".");
            Debug.Log($"[FarmInteraction] Phá chuồng: {removedFences} rào, trả {returnedAnimals} con, hoàn {refundWood} gỗ + {refundStone} đá.");
        }

        // Demo helper: đảm bảo túi có sẵn vài con vật để test thả chuồng (production: mua từ shop).
        private void EnsureStarterAnimals()
        {
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv == null) return;

            string[] starterAnimals = { "chicken_01", "rabbit_01", "ostrich_01", "goat_01", "cow_01", "deer_01" };
            foreach (var a in starterAnimals)
            {
                if (inv.GetItemQuantity(a) <= 0)
                {
                    inv.AddItem(a, 2);
                    Debug.Log($"[FarmInteraction] Bổ sung con vật cho demo: {a} +2");
                }
            }
        }

        // Người chơi chọn 1 con vật trong túi khi đang đứng ở chuồng -> kiểm tra + thả.
        private void HandlePenAnimalSelected(string itemId)
        {
            var pen = pendingPen;
            pendingPen = null; // dùng 1 lần — chọn sai loài thì bấm "Thả thú" lại
            if (pen == null) return;

            if (!pen.CanAccept(itemId))
            {
                Debug.Log($"[FarmInteraction] Chuồng '{pen.name}' KHÔNG nhận '{itemId}'. Danh sách cho phép: {pen.AllowedIdsText()} | Còn chỗ: {pen.HasSpace}");
                return;
            }

            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv != null && !inv.RemoveItem(itemId, 1))
            {
                Debug.Log($"[FarmInteraction] Không có '{itemId}' trong túi để thả.");
                return;
            }

            if (PlayerController.Instance != null) PlayerController.Instance.FaceTowards(pen.transform.position);
            pen.SpawnByItem(itemId);

            if (inventoryPopup != null) inventoryPopup.Hide();
        }

        private void OnInventoryItemSelected(string itemId)
        {
            // Ưu tiên: đang chờ chọn thức ăn để cho động vật ăn.
            if (pendingFeedAnimal != null)
            {
                HandleFeedSelected(itemId);
                return;
            }

            // Ưu tiên: đang chờ thả thú vào VÙNG QUÂY (chuồng từ hàng rào).
            if (pendingEnclosure != null)
            {
                HandleEnclosureAnimalSelected(itemId);
                return;
            }

            // Ưu tiên: đang chờ chọn con vật cho 1 chuồng (kiểu cũ) -> xử lý thả thú.
            if (pendingPen != null)
            {
                HandlePenAnimalSelected(itemId);
                return;
            }

            // Only handle seed selection when we have a pending tile
            if (pendingPlantTile == null) return;
            if (!itemId.Contains("seed")) return; // Only accept seed items

            pendingSeedId = itemId;

            // Remove seed from inventory
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv != null)
            {
                if (inv.GetItemQuantity(itemId) <= 0)
                {
                    Debug.Log($"[FarmInteraction] No {itemId} in inventory! Auto-giving 1 for demo.");
                    inv.AddItem(itemId, 1);
                }
                inv.RemoveItem(itemId, 1);
            }

            // Đóng túi đồ, rồi MÚA động tác trồng TRƯỚC — gieo hạt SAU khi múa xong
            if (inventoryPopup != null) inventoryPopup.Hide();

            StartCoroutine(PlantAfterAnimation(pendingPlantTile, itemId));

            pendingPlantTile = null;
            pendingSeedId = null;
        }

        // Múa động tác Planting xong MỚI thật sự gieo hạt xuống ô đất.
        private System.Collections.IEnumerator PlantAfterAnimation(FarmTile tile, string seedId)
        {
            float dur = 2f;
            if (PlayerController.Instance != null)
                dur = PlayerController.Instance.PlayActionAnimation("Planting", 0f, YWonderLand.Player.ToolType.SeedBag, 2f); // x2 cho nhanh
            if (dur <= 0f) dur = 2f;

            yield return new WaitForSeconds(dur);

            if (tile != null && PlantWithSlots(tile, seedId))
                Debug.Log($"[FarmInteraction] Gieo hạt {seedId} SAU khi múa xong!");
        }

        // Trồng cây có thể CHIẾM NHIỀU Ô (giàn): cây nhiều ô (vd chanh dây 20 ô) cần thêm ô trống gần nhất.
        private bool PlantWithSlots(FarmTile master, string seedId)
        {
            int slots = 1;
            var cropDb = Resources.Load<CropDatabase>("CropDatabase");
            var crop = cropDb != null ? cropDb.GetCropBySeedId(seedId) : null;
            if (crop != null) slots = Mathf.Max(1, crop.plotSlots);

            if (slots <= 1) return master.InteractPlant(seedId);

            // Cây nhiều ô: cần thêm (slots-1) ô ĐÃ CUỐC & còn trống gần nhất.
            var extras = FindNearbyPlowedTiles(master, slots - 1);
            if (extras.Count < slots - 1)
            {
                ScreenToast.Show($"Cần {slots} ô đất đã cuốc để trồng {GetItemDisplayName(seedId)} (giàn) — còn thiếu {slots - 1 - extras.Count} ô.");
                return false;
            }

            if (!master.InteractPlant(seedId)) return false;
            foreach (var t in extras) t.OccupyAsSlot(master);
            master.RegisterSlaves(extras);
            Debug.Log($"[FarmInteraction] Trồng {seedId} chiếm {slots} ô (1 master + {extras.Count} ô giàn).");
            return true;
        }

        // Tìm 'count' ô đã cuốc & còn trống GẦN NHẤT quanh master (theo khoảng cách thế giới).
        private System.Collections.Generic.List<FarmTile> FindNearbyPlowedTiles(FarmTile master, int count)
        {
            var all = new System.Collections.Generic.List<FarmTile>(
                FindObjectsByType<FarmTile>(FindObjectsSortMode.None));
            Vector3 origin = master.transform.position;
            all.Sort((a, b) =>
                (a.transform.position - origin).sqrMagnitude.CompareTo((b.transform.position - origin).sqrMagnitude));

            var result = new System.Collections.Generic.List<FarmTile>();
            foreach (var t in all)
            {
                if (t == master) continue;
                if (t.IsPlowedFree)
                {
                    result.Add(t);
                    if (result.Count >= count) break;
                }
            }
            return result;
        }

        private void HandleWater(FarmTile tile)
        {
            // CHẶN SPAM: đang múa động tác (tưới/cuốc...) thì bỏ qua click mới -> không tưới chồng
            // nhiều lần + không tốn nước thừa + không tăng tiến độ ô theo số lần click.
            if (PlayerController.Instance != null && PlayerController.Instance.IsBusy) return;

            var inv = YWonderLand.Managers.InventoryManager.Instance;

            // CẦN nước tưới (múc từ ao trên đảo). Hết → báo, không tưới được.
            if (inv == null || inv.GetItemQuantity("watering_water_01") <= 0)
            {
                ScreenToast.Show("Hết nước tưới! Ra ao trên đảo bấm \"Múc nước\" trước đã.");
                return;
            }

            // Đảm bảo có xô (dụng cụ) để cầm khi múa tưới.
            if (inv.GetItemQuantity("watering_can_01") <= 0) inv.AddItem("watering_can_01", 1);

            // Tưới LẦN ĐẦU (Planted → bắt đầu lớn) hoặc TƯỚI LẠI (đang lớn → đổ đầy nước chống khát).
            bool watered = false;
            if (tile.currentState == FarmTile.TileState.Planted) watered = tile.InteractWater();
            else if (tile.currentState == FarmTile.TileState.Watered) watered = tile.WaterAgain();

            if (watered)
            {
                inv.RemoveItem("watering_water_01", 1); // TỐN 1 xô nước mỗi lần tưới
                // Múa động tác TƯỚI bằng clip riêng "Watering" (duration 0 = tự đo độ dài clip) -> cầm BÌNH TƯỚI/XÔ
                if (PlayerController.Instance != null)
                    PlayerController.Instance.PlayActionAnimation("Watering", 0f, YWonderLand.Player.ToolType.WateringCan);
            }
        }

        // Múc nước ở ao (vùng WaterSource) → +xô nước vào túi. KHÔNG animation (khách không yêu cầu).
        private void ScoopWater(WaterSource src)
        {
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv == null) return;
            int amt = src != null ? Mathf.Max(1, src.amountPerScoop) : 5;
            inv.AddItem("watering_water_01", amt);
            int total = inv.GetItemQuantity("watering_water_01");
            ScreenToast.ShowInfo($"Múc được {amt} xô nước! (Tổng: {total})");
        }

        private void HandleHarvest(FarmTile tile)
        {
            if (tile.InteractHarvest(out string harvestId, out int amount))
            {
                // Add produce to inventory
                var inv = YWonderLand.Managers.InventoryManager.Instance;
                if (inv != null)
                {
                    inv.AddItem(harvestId, amount);
                    Debug.Log($"[FarmInteraction] Harvested {amount}x {harvestId}!");
                }

                // Cây LÂU NĂM — vụ cuối: thu thêm sản phẩm Product2 (FarmTile đã set sẵn LastFinalProduct).
                if (inv != null && !string.IsNullOrEmpty(tile.LastFinalProductId) && tile.LastFinalProductAmount > 0)
                {
                    inv.AddItem(tile.LastFinalProductId, tile.LastFinalProductAmount);
                    ScreenToast.ShowInfo($"Vụ cuối: +{tile.LastFinalProductAmount} {GetItemDisplayName(tile.LastFinalProductId)}");
                }

                // Add rewards from CropDefinition
                CropDefinition crop = null;
                var cropDb = Resources.Load<CropDatabase>("CropDatabase");
                if (cropDb != null)
                {
                    // Lookup by harvest item to get rewards
                    crop = cropDb.GetCropByHarvestId(harvestId);
                }

                if (crop != null)
                {
                    float care = tile.LastCareFactor; // <1 nếu cây từng bị khát (behavior B)

                    // Add POS reward (giảm theo độ chăm sóc)
                    if (YWonderLand.Managers.EconomyManager.Instance != null)
                    {
                        int pos = Mathf.RoundToInt(crop.posReward * care);
                        YWonderLand.Managers.EconomyManager.Instance.AddPOS(pos);
                        Debug.Log($"[FarmInteraction] +{pos} POS (care {care:0.00})");
                    }

                    // Cộng EXP thật (khách chốt 22/06: số NGÀY trồng × 10).
                    float cropDays = crop.growthTimeSec / YWonderLand.Core.GameTimeConfig.SecondsPerGameDay;
                    int exp = Mathf.Max(1, Mathf.RoundToInt(cropDays * 10f));
                    YWonderLand.Managers.ExperienceManager.Instance?.AddEXP(exp);
                    YWonderLand.Managers.AudioManager.Instance?.PlaySFX("harvest");

                    if (care < 0.99f)
                    {
                        // Thiếu nước → toast đỏ kèm số % giảm (gộp với báo thu hoạch).
                        ScreenToast.Show($"Thu hoạch: +{amount} {GetItemDisplayName(harvestId)} (thiếu nước -{Mathf.RoundToInt((1f - care) * 100f)}%)");
                    }
                    else
                    {
                        ScreenToast.ShowInfo($"Thu hoạch: +{amount} {GetItemDisplayName(harvestId)}");
                    }
                }
                else
                {
                    // Cây không có CropDefinition (vd cây lâu năm khung tạm) — vẫn báo thu hoạch.
                    ScreenToast.ShowInfo($"Thu hoạch: +{amount} {GetItemDisplayName(harvestId)}");
                }

                // Save farm state
                if (YWonderLand.Managers.FarmManager.Instance != null)
                {
                    YWonderLand.Managers.FarmManager.Instance.SaveFarmState();
                }
            }
        }
    }
}
