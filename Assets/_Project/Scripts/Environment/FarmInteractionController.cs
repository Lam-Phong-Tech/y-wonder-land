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

        [Header("References")]
        [SerializeField] private Camera mainCamera;

        private InventoryPopupController inventoryPopup;
        private string pendingSeedId; // Seed được chọn từ inventory, chờ gieo
        private FarmTile pendingPlantTile; // Tile đang chờ gieo hạt

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

        private HarvestableResource currentHarvestTarget;
        private float _chopAnimTimer = 0f; // Đếm giờ để lặp animation chặt/đập khi đang giữ chuột

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Gameplay) return;
            if (UIPopupTracker.AnyOpen) return; // Đang mở popup -> ngừng tương tác thế giới (tránh click xuyên qua UI)

            var mouse = Mouse.current;
            if (mouse == null || mainCamera == null) return;

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

            if (mouse.leftButton.wasPressedThisFrame) HandleClick(mouse.position.ReadValue());
            if (mouse.leftButton.isPressed) HandleHold(mouse.position.ReadValue());
            else if (mouse.leftButton.wasReleasedThisFrame)
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
            HandleHover(mouse.position.ReadValue());

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

                if (hit.collider.TryGetComponent<FarmAnimal>(out var animal) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<FarmAnimal>(out animal)))
                {
                    foundObj = animal.gameObject;
                    currentAnimalState = animal.currentState;
                    currentHasProduct = animal.hasProductReady;

                    foundActions.Add(new InteractionAction { keyName = "E", actionName = "Vuốt ve", onClick = () => PetAnimal(animal) });
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
                    if (Vector3.Distance(hoverPlayerPos, hit.point) <= resource.interactionRange)
                    {
                        foundObj = resource.gameObject;
                        string actionStr = resource.type == HarvestableResource.ResourceType.Tree ? "Chặt cây" : "Đập đá";
                        foundActions.Add(new InteractionAction { keyName = "Click", actionName = actionStr, onClick = () => ClickHarvestResource(resource) });
                    }
                    break;
                }
                else if (hit.collider.TryGetComponent<MerchantNPC>(out var merchant) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<MerchantNPC>(out merchant)))
                {
                    foundObj = merchant.gameObject;
                    foundActions.Add(new InteractionAction { keyName = "Click", actionName = "Giao thương", onClick = () => merchant.Interact() });
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
                        case FarmTile.TileState.Watered: actName = "Theo dõi"; break;
                        case FarmTile.TileState.Ripe: actName = "Thu hoạch"; break;
                    }
                    foundActions.Add(new InteractionAction { keyName = "Click", actionName = actName, onClick = () => PerformTileAction(tile) });
                    break;
                }
                else if (hit.collider.TryGetComponent<FishingSpot>(out var spot) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<FishingSpot>(out spot)))
                {
                    if (hit.distance <= 15f)
                    {
                        foundObj = spot.gameObject;
                        foundActions.Add(new InteractionAction { keyName = "F", actionName = "Câu cá", onClick = () => StartFishing(spot) });
                    }
                    break;
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
        private void StartFishing(FishingSpot spot)
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null) player.PlayActionAnimation("Fishing", 8.5f, YWonderLand.Player.ToolType.FishingRod);
            
            var fishingUI = Object.FindFirstObjectByType<FishingOverlayController>();
            if (fishingUI != null) fishingUI.Show();
        }

        private void PetAnimal(FarmAnimal animal)
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null) player.PlayActionAnimation("Petting", 0f); // 0 = tự lấy đúng độ dài clip
            animal.Pet();
        }

        private void FeedAnimal(FarmAnimal animal)
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null) player.PlayActionAnimation("Feed", 0f); // 0 = tự lấy đúng độ dài clip (~6s)
            animal.Feed();
        }

        private void HealAnimal(FarmAnimal animal)
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            // Chưa có animation "tiêm/chữa bệnh" riêng -> tạm dùng "Feed" (động tác đưa tay) cho đỡ trống
            if (player != null) player.PlayActionAnimation("Feed", 0f);
            animal.Heal();
        }

        private void HarvestAnimal(FarmAnimal animal)
        {
            if (animal.HarvestProduct(out string itemId, out int amount))
            {
                Managers.InventoryManager.Instance?.AddItem(itemId, amount);
            }
        }

        private void ClickHarvestResource(HarvestableResource resource)
        {
            Vector3 clickPlayerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
            if (Vector3.Distance(clickPlayerPos, resource.transform.position) > resource.interactionRange + 1.5f)
                return;

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            
            // Ép nhân vật múa hoạt ảnh và cầm rìu/cúp
            if (player != null) 
            {
                if (resource.type == HarvestableResource.ResourceType.Tree)
                    player.PlayActionAnimation("TreeCutting", 1f, YWonderLand.Player.ToolType.Axe);
                else
                    player.PlayActionAnimation("TreeCutting", 1f, YWonderLand.Player.ToolType.None); // Chưa có Cúp nên tạm dùng tay không
            }

            // Hiện thanh tiến trình
            if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                YWonderLand.UI.ResourceInteractionUIController.Instance.Show(resource);

            // Tăng tiến độ chặt (1 giây / click). (Ví dụ cây 3 máu thì click 3 phát là gãy)
            if (resource.Harvest(1.0f))
            {
                // Nếu gãy rồi thì ẩn thanh tiến trình
                if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                    YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();
            }
        }

        private void PerformTileAction(FarmTile tile)
        {
            float dist = Vector3.Distance(Object.FindFirstObjectByType<PlayerController>().transform.position, tile.transform.position);
            if (dist > interactRange) return;

            switch (tile.currentState)
            {
                case FarmTile.TileState.Soil: HandlePlow(tile); break;
                case FarmTile.TileState.Plowed: HandleOpenSeedSelection(tile); break;
                case FarmTile.TileState.Planted: HandleWater(tile); break;
                case FarmTile.TileState.Watered: break;
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
                            ? YWonderLand.Player.ToolType.Axe : YWonderLand.Player.ToolType.None;
                        PlayerController.Instance.PlayActionAnimation("TreeCutting", 1.0f, chopTool);
                        _chopAnimTimer = 0.9f;
                    }

                    // Show progress bar
                    if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                    {
                        YWonderLand.UI.ResourceInteractionUIController.Instance.Show(resource);
                    }
                    
                    if (resource.Harvest(Time.deltaTime))
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
                            // Show growth progress
                            float pct = tile.GetGrowthPercentage();
                            Debug.Log($"[FarmInteraction] Growing... {pct * 100:F0}%");
                            break;

                        case FarmTile.TileState.Ripe:
                            HandleHarvest(tile);
                            break;
                    }
                    return; // Handled tile, stop here
                }

                MerchantNPC merchant = hit.collider.GetComponent<MerchantNPC>();
                if (merchant != null)
                {
                    Debug.Log($"[FarmInteraction] TÌM THẤY MERCHANT: {merchant.gameObject.name}! Mở Shop...");
                    merchant.Interact();
                    return; 
                }

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
                
                // Múa động tác Cuốc đất
                if (PlayerController.Instance != null)
                {
                    PlayerController.Instance.PlayActionAnimation("TreeCutting", 3.0f, YWonderLand.Player.ToolType.Axe);
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

        private void OnInventoryItemSelected(string itemId)
        {
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

            if (tile != null && tile.InteractPlant(seedId))
                Debug.Log($"[FarmInteraction] Gieo hạt {seedId} SAU khi múa xong!");
        }

        private void HandleWater(FarmTile tile)
        {
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv != null && inv.GetItemQuantity("watering_can_01") <= 0)
            {
                Debug.Log("[FarmInteraction] Auto-giving watering_can_01 for demo purposes.");
                inv.AddItem("watering_can_01", 1);
            }

            if (tile.InteractWater())
            {
                Debug.Log("[FarmInteraction] Watered tile! Growth timer started.");
            }
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
                    // Add POS reward
                    if (YWonderLand.Managers.EconomyManager.Instance != null)
                    {
                        YWonderLand.Managers.EconomyManager.Instance.AddPOS(crop.posReward);
                        Debug.Log($"[FarmInteraction] +{crop.posReward} POS");
                    }

                    // TODO: Add EXP when level system is implemented
                    Debug.Log($"[FarmInteraction] +{crop.expReward} EXP (not yet implemented)");
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
