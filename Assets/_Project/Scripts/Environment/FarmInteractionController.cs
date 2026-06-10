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

            // 1. Try to find an Animal first among all hits
            foreach (var hit in hits)
            {
                Debug.Log($"[FarmInteraction] Tia đụng trúng: {hit.collider.gameObject.name} (Layer: {hit.collider.gameObject.layer})");

                FarmAnimal animal = hit.collider.GetComponent<FarmAnimal>();
                if (animal != null)
                {
                    Debug.Log($"[FarmInteraction] TÌM THẤY CON THÚ: {animal.data.animalId}! Mở UI...");
                    if (AnimalInteractionPopupController.Instance != null)
                    {
                        AnimalInteractionPopupController.Instance.Show(animal);
                        Debug.Log("[FarmInteraction] Đã gọi lệnh mở UI thành công!");
                    }
                    else
                    {
                        Debug.LogError("[FarmInteraction] LỖI: AnimalInteractionPopupController.Instance bị NULL! Bảng UI chưa được kích hoạt đúng cách.");
                    }
                    return; // Handled animal, stop here
                }
            }

            // 2. If no animal, try to find a FarmTile
            foreach (var hit in hits)
            {
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
            }

            // 3. Try to find a MerchantNPC
            foreach (var hit in hits)
            {
                MerchantNPC merchant = hit.collider.GetComponent<MerchantNPC>();
                if (merchant != null)
                {
                    Debug.Log($"[FarmInteraction] TÌM THẤY MERCHANT: {hit.collider.gameObject.name}! Mở Shop...");
                    merchant.Interact();
                    return; // Handled merchant, stop here
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
                
                // Múa động tác Cuốc đất (Dùng tạm animation Chop/Attack)
                Animator anim = GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetTrigger("Chop");
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
                Animator anim = GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetTrigger("Plant");
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
                Animator anim = GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetTrigger("Plant");
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
