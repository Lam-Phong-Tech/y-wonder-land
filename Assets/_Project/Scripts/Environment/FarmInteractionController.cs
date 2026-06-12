using UnityEngine;
using UnityEngine.InputSystem;
using YWonderLand.Data;

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

        void Update()
        {
            // Chỉ cho phép tương tác khi đang trong Gameplay
            if (GameManager.Instance != null && 
                GameManager.Instance.currentState != GameManager.GameState.Gameplay)
            {
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null || mainCamera == null) return;

            // Ngăn click xuyên qua UI (cho cả nhấp và đè)
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
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

            // Left click (just pressed this frame) - Dành cho Tile, Animal
            if (mouse.leftButton.wasPressedThisFrame)
            {
                HandleClick(mouse.position.ReadValue());
            }

            // Left click (held down) - Dành cho HarvestableResource
            if (mouse.leftButton.isPressed)
            {
                HandleHold(mouse.position.ReadValue());
            }
            // Released
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (currentHarvestTarget != null)
                {
                    currentHarvestTarget.CancelHarvest();
                    currentHarvestTarget = null;
                    if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                        YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();
                }
            }

            // Quét liên tục (Hover) tại vị trí con trỏ chuột
            HandleHover(mouse.position.ReadValue());

            // --- XỬ LÝ PHÍM TẮT DÀNH CHO TƯƠNG TÁC (Ví dụ: F để câu cá) ---
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                if (currentHoverObject != null)
                {
                    FishingSpot spot = currentHoverObject.GetComponent<FishingSpot>();
                    if (spot == null && currentHoverObject.transform.parent != null) spot = currentHoverObject.transform.parent.GetComponent<FishingSpot>();
                    
                    if (spot != null)
                    {
                        PlayerController player = GetComponent<PlayerController>();
                        if (player != null) player.PlayActionAnimation("Fishing", 8.5f, YWonderLand.Player.ToolType.FishingRod);
                        
                        var fishingUI = Object.FindFirstObjectByType<FishingOverlayController>();
                        if (fishingUI != null) fishingUI.Show();
                    }
                }
            }
        }

        private RaycastHit[] hoverHitResults = new RaycastHit[30];
        private GameObject currentHoverObject = null;
        private string currentHoverPrompt = "";

        private void HandleHover(Vector2 screenPos)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            int hitCount = Physics.RaycastNonAlloc(ray, hoverHitResults, 100f);

            // Sắp xếp kết quả quét từ gần đến xa
            System.Array.Sort(hoverHitResults, 0, hitCount, System.Collections.Generic.Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));

            string foundPrompt = null;
            GameObject foundObj = null;

            for (int i = 0; i < hitCount; i++)
            {
                var hit = hoverHitResults[i];
                if (hit.collider == null) continue;
                if (hit.collider.gameObject.CompareTag("Player")) continue;

                if (hit.collider.TryGetComponent<FarmAnimal>(out var animal) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<FarmAnimal>(out animal)))
                {
                    foundPrompt = "[Chuột Trái] Nựng thú cưng";
                    foundObj = animal.gameObject;
                    break;
                }
                else if (hit.collider.TryGetComponent<MerchantNPC>(out var merchant) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<MerchantNPC>(out merchant)))
                {
                    foundPrompt = "[Chuột Trái] Giao thương";
                    foundObj = merchant.gameObject;
                    break;
                }
                else if (hit.collider.TryGetComponent<FarmTile>(out var tile) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<FarmTile>(out tile)))
                {
                    foundObj = tile.gameObject;
                    switch (tile.currentState)
                    {
                        case FarmTile.TileState.Soil: foundPrompt = "[Chuột Trái] Cuốc đất"; break;
                        case FarmTile.TileState.Plowed: foundPrompt = "[Chuột Trái] Gieo hạt"; break;
                        case FarmTile.TileState.Planted: foundPrompt = "[Chuột Trái] Tưới nước"; break;
                        case FarmTile.TileState.Watered: foundPrompt = "[Chuột Trái] Theo dõi lớn lên"; break;
                        case FarmTile.TileState.Ripe: foundPrompt = "[Chuột Trái] Thu hoạch"; break;
                    }
                    break;
                }
                else if (hit.collider.TryGetComponent<HarvestableResource>(out var resource) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<HarvestableResource>(out resource)))
                {
                    foundPrompt = "[Chuột Trái / Giữ] Chặt cây";
                    foundObj = resource.gameObject;
                    break;
                }
                else if (hit.collider.TryGetComponent<FishingSpot>(out var spot) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<FishingSpot>(out spot)))
                {
                    if (hit.distance <= 15f) // Ngăn cản bấm tít mù khơi
                    {
                        foundPrompt = "[F] Câu cá";
                        foundObj = spot.gameObject;
                    }
                    break;
                }
                else if (!hit.collider.isTrigger)
                {
                    // Tia quét đâm trúng đất hoặc vật cản cứng -> Bị che khuất tầm nhìn, DỪNG QUÉT ngay lập tức!
                    break;
                }
            }

            // Cập nhật UI
            if (foundPrompt != null)
            {
                if (foundObj != currentHoverObject || foundPrompt != currentHoverPrompt)
                {
                    currentHoverObject = foundObj;
                    currentHoverPrompt = foundPrompt;
                    if (GameHUDController.Instance != null)
                    {
                        GameHUDController.Instance.ShowInteractionPrompt(foundPrompt);
                    }
                }
            }
            else
            {
                if (currentHoverObject != null)
                {
                    currentHoverObject = null;
                    currentHoverPrompt = "";
                    if (GameHUDController.Instance != null)
                    {
                        GameHUDController.Instance.HideInteractionPrompt();
                    }
                }
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
                    if (currentHarvestTarget != null && currentHarvestTarget != resource)
                    {
                        currentHarvestTarget.CancelHarvest();
                    }
                    
                    currentHarvestTarget = resource;
                    
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
                PlayerController player = GetComponent<PlayerController>();
                if (player != null)
                {
                    player.PlayActionAnimation("TreeCutting", 3.0f, YWonderLand.Player.ToolType.Axe);
                }
            }
        }

        private void HandleOpenSeedSelection(FarmTile tile)
        {
            // CHỮA CHÁY NHANH CHO SẾP XEM: Tự động gieo hạt cà rốt luôn, không thèm mở túi đồ!
            Debug.Log("[FarmInteraction] CHỮA CHÁY DEMO: Tự động gieo hạt cà rốt!");
            
            if (tile.InteractPlant("carrot_seed_01"))
            {
                // Múa động tác gieo hạt
                PlayerController player = GetComponent<PlayerController>();
                if (player != null)
                {
                    player.PlayActionAnimation("Planting", 1.5f, YWonderLand.Player.ToolType.SeedBag); // "Planting" thay vì "Plant" để khớp với sơ đồ của user
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

            // Plant on the pending tile
            if (pendingPlantTile.InteractPlant(itemId))
            {
                Debug.Log($"[FarmInteraction] Planted {itemId}!");
                
                // Múa động tác gieo hạt
                PlayerController player = GetComponent<PlayerController>();
                if (player != null)
                {
                    player.PlayActionAnimation("Planting", 1.5f, YWonderLand.Player.ToolType.SeedBag);
                }
            }

            // Close inventory
            if (inventoryPopup != null)
                inventoryPopup.Hide();

            pendingPlantTile = null;
            pendingSeedId = null;
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
