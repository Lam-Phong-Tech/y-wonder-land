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
        public static FarmInteractionController Instance { get; private set; }

        private const float DefaultInteractionRange = 1f;
        private const float DefaultGroundInteractRange = 1.35f;
        private const float DefaultTileInteractRange = 1.75f;
        private const float DefaultFishingInteractRange = 5f;
        private const float MaxFishingInteractRange = 5f;
        private const float SolidHitPassthroughTolerance = 0.75f;
        private const float ResourceExecuteRangePadding = 0.25f;
        private const float FarmTileAimFallbackRadius = 0.45f;
        private const float TreeCuttingClipDuration = 2.26f;
        private const float MiningClipDuration = 0.967f;
        private const float PlantingClipDuration = 4.13f;
        private const float WateringClipDuration = 5.6f;
        private const float FeedClipDuration = 9.1f;
        private const float FeedActionSpeed = 2f;
        private const float HoeingFallbackDuration = 3f;

        [Header("Interaction Settings")]
        [Tooltip("Khoảng cách tối đa để tương tác trực tiếp (đề xuất ~1m)")]
        [SerializeField] private float interactRange = DefaultInteractionRange;
        [Tooltip("Khoảng cách tương tác với tài nguyên (cây/đá)")]
        [SerializeField] private float resourceInteractRange = DefaultInteractionRange;
        [Tooltip("Khoảng cách tương tác với động vật")]
        [SerializeField] private float animalInteractRange = DefaultGroundInteractRange;
        [Tooltip("Khoảng cách tương tác với chuồng")]
        [SerializeField] private float enclosureInteractRange = DefaultGroundInteractRange;
        [Tooltip("Khoảng cách tương tác với ô đất")]
        [SerializeField] private float tileInteractRange = DefaultTileInteractRange;
        [Tooltip("Khoảng cách tương tác với NPC/merchant")]
        [SerializeField] private float merchantInteractRange = DefaultInteractionRange;
        [Tooltip("Khoảng cách tương tác với ao nước")]
        [SerializeField] private float waterInteractRange = DefaultGroundInteractRange;
        [Tooltip("Khoảng cách tương tác khi câu cá")]
        [SerializeField] private float fishingInteractRange = DefaultFishingInteractRange;

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
        private List<BuildSurfaceCell> pendingDemolishEnclosure;
        private float demolishConfirmTimer;
        private const float DemolishConfirmWindow = 1.25f;
        private BuildSurfaceCell hoverEnclosureSeed;     // cache: ô đang rê để khỏi flood-fill mỗi frame
        private List<BuildSurfaceCell> hoverEnclosure;   // cache: kết quả vùng quây của ô đang rê

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            if (interactRange <= 0f) interactRange = DefaultInteractionRange;
            if (resourceInteractRange <= 0f) resourceInteractRange = DefaultInteractionRange;
            if (animalInteractRange <= 0f) animalInteractRange = DefaultGroundInteractRange;
            if (enclosureInteractRange <= 0f) enclosureInteractRange = DefaultGroundInteractRange;
            if (tileInteractRange <= 0f) tileInteractRange = DefaultTileInteractRange;
            if (merchantInteractRange <= 0f) merchantInteractRange = DefaultInteractionRange;
            if (waterInteractRange <= 0f) waterInteractRange = DefaultGroundInteractRange;
            if (fishingInteractRange <= 0f) fishingInteractRange = DefaultFishingInteractRange;

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
            CancelTimedAction(null);
            if (Instance == this) Instance = null;

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
        private Coroutine timedActionRoutine;
        private int timedActionToken;
        private int timedActionStartFrame;
        private bool timedActionActive;
        private global::System.Action timedActionCancelRefund;
        private CursorLockMode timedPreviousCursorLockState;
        private bool timedPreviousCursorVisible;
        private bool timedHasSavedCursorState;

        public bool IsTimedActionActive => timedActionActive;

        public bool CancelTimedActionFromHUD()
        {
            if (!timedActionActive) return false;
            CancelTimedAction("Đã hủy thao tác.");
            return true;
        }

        private void BeginTimedActionCursorMode()
        {
            if (!timedHasSavedCursorState)
            {
                timedPreviousCursorLockState = UnityEngine.Cursor.lockState;
                timedPreviousCursorVisible = UnityEngine.Cursor.visible;
                timedHasSavedCursorState = true;
            }

            UIPopupTracker.SetOpen(this, true);
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        private void EndTimedActionCursorMode()
        {
            UIPopupTracker.SetOpen(this, false);

            if (!timedHasSavedCursorState) return;
            if (!UIPopupTracker.AnyOpen)
            {
                UnityEngine.Cursor.lockState = timedPreviousCursorLockState;
                UnityEngine.Cursor.visible = timedPreviousCursorVisible;
            }

            timedHasSavedCursorState = false;
        }

        private bool BeginTimedAction(
            string animName,
            float fallbackDuration,
            YWonderLand.Player.ToolType tool,
            Vector3 facePoint,
            global::System.Action onComplete,
            global::System.Action onCancelRefund = null,
            float speed = 1f)
        {
            var player = PlayerController.Instance;
            if (player == null) return false;
            if (timedActionActive)
            {
                Debug.LogWarning($"[FarmInteraction] Timed action '{animName}' blocked: another timed action is active.");
                return false;
            }
            if (player.IsBusy)
            {
                Debug.LogWarning($"[FarmInteraction] Timed action '{animName}' blocked: player is busy/action locked.");
                return false;
            }
            if (FishingOverlayController.Instance != null && FishingOverlayController.Instance.IsAutoFishing)
            {
                Debug.LogWarning($"[FarmInteraction] Timed action '{animName}' blocked: fishing overlay is active.");
                return false;
            }

            timedActionToken++;
            int token = timedActionToken;
            timedActionActive = true;
            timedActionCancelRefund = onCancelRefund;
            timedActionStartFrame = Time.frameCount;

            currentHoverObject = null;
            currentActions.Clear();
            GameHUDController.Instance?.HideInteractionPrompt();
            if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();

            player.FaceTowards(facePoint);
            float duration = player.PlayActionAnimation(animName, Mathf.Max(0.1f, fallbackDuration), tool, speed);
            if (duration <= 0f) duration = Mathf.Max(0.1f, fallbackDuration / Mathf.Max(0.1f, speed));

            BeginTimedActionCursorMode();
            GameHUDController.Instance?.ShowActionCancelProgress(0f);
            timedActionRoutine = StartCoroutine(RunTimedAction(token, duration, onComplete));
            return true;
        }

        private System.Collections.IEnumerator RunTimedAction(int token, float duration, global::System.Action onComplete)
        {
            float elapsed = 0f;
            duration = Mathf.Max(0.1f, duration);

            while (elapsed < duration)
            {
                if (token != timedActionToken) yield break;

                var keyboard = Keyboard.current;
                if (keyboard != null && Time.frameCount > timedActionStartFrame &&
                    (keyboard.escapeKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame))
                {
                    CancelTimedAction("Đã hủy thao tác.");
                    yield break;
                }

                elapsed += Time.deltaTime;
                GameHUDController.Instance?.SetActionCancelProgress(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            if (token != timedActionToken) yield break;

            FinishTimedAction();
            onComplete?.Invoke();
        }

        private void FinishTimedAction()
        {
            timedActionToken++;
            timedActionRoutine = null;
            timedActionActive = false;
            timedActionCancelRefund = null;
            GameHUDController.Instance?.HideActionCancelProgress();
            EndTimedActionCursorMode();
        }

        private void CancelTimedAction(string toast)
        {
            if (!timedActionActive && timedActionRoutine == null) return;

            timedActionToken++;
            if (timedActionRoutine != null)
            {
                StopCoroutine(timedActionRoutine);
                timedActionRoutine = null;
            }

            timedActionActive = false;
            GameHUDController.Instance?.HideActionCancelProgress();
            EndTimedActionCursorMode();

            if (currentHarvestTarget != null)
            {
                currentHarvestTarget.CancelHarvest();
                currentHarvestTarget = null;
            }
            _buttonHeldResource = null;
            _chopAnimTimer = 0f;

            timedActionCancelRefund?.Invoke();
            timedActionCancelRefund = null;

            if (PlayerController.Instance != null)
                PlayerController.Instance.CancelAction();

            if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();

            if (!string.IsNullOrEmpty(toast))
                ScreenToast.Show(toast);
        }

        private float HorizontalDistanceToPlayer(Vector3 worldPos)
        {
            Vector3 playerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
            float dx = playerPos.x - worldPos.x;
            float dz = playerPos.z - worldPos.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private float NormalizeRange(float range)
        {
            return range > 0f ? range : DefaultInteractionRange;
        }

        private float NormalizeGroundRange(float range)
        {
            return range > 0f ? range : DefaultGroundInteractRange;
        }

        private float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private bool IsInInteractRange(Vector3 worldPos, float range) =>
            HorizontalDistanceToPlayer(worldPos) <= NormalizeRange(range);

        private bool IsInInteractRangeAtPoint(Vector3 hitPoint, float range) =>
            HorizontalDistanceToPlayer(hitPoint) <= NormalizeRange(range);

        private bool IsInInteractRange(Vector3 worldPos) =>
            IsInInteractRange(worldPos, interactRange);

        private float ClampedRange(float targetRange, float fallbackRange) =>
            targetRange > 0f ? Mathf.Min(targetRange, NormalizeRange(fallbackRange)) : NormalizeRange(fallbackRange);

        private float HorizontalDistanceToClosestColliderPoint(GameObject root, Vector3 fallbackWorldPos)
        {
            Vector3 playerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
            float best = HorizontalDistance(playerPos, fallbackWorldPos);
            if (root == null) return best;

            var colliders = root.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                if (col == null || !col.enabled) continue;
                float dist = HorizontalDistance(playerPos, SafeClosestPoint(col, playerPos));
                if (dist < best) best = dist;
            }

            return best;
        }

        private Vector3 SafeClosestPoint(Collider col, Vector3 point)
        {
            if (col == null) return point;

            var mesh = col as MeshCollider;
            if (mesh != null && !mesh.convex)
                return col.bounds.ClosestPoint(point);

            if (col is BoxCollider || col is SphereCollider || col is CapsuleCollider || mesh != null)
                return col.ClosestPoint(point);

            return col.bounds.ClosestPoint(point);
        }

        private float GetResourceExecuteRange(HarvestableResource resource)
        {
            if (resource == null) return NormalizeRange(resourceInteractRange);
            return ClampedRange(resource.interactionRange, resourceInteractRange) + ResourceExecuteRangePadding;
        }

        private float GetResourceDistanceToPlayer(HarvestableResource resource)
        {
            Vector3 playerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
            if (resource == null) return float.PositiveInfinity;

            Collider resourceCollider = resource.GetComponent<Collider>();
            if (resourceCollider == null)
                resourceCollider = resource.GetComponentInChildren<Collider>();

            Vector3 targetPoint = resource.transform.position;
            if (resourceCollider != null)
                targetPoint = resourceCollider.ClosestPoint(playerPos);

            return HorizontalDistance(playerPos, targetPoint);
        }

        private BuildSurfaceCell ResolveBuildSurfaceCellFromHit(RaycastHit hit)
        {
            if (hit.collider == null) return null;

            var cell = hit.collider.GetComponentInParent<BuildSurfaceCell>();
            if (cell != null) return cell;

            var fence = hit.collider.GetComponentInParent<FenceAutoConnect>();
            if (fence == null) return null;

            Transform hitTransform = fence.transform;
            foreach (var candidate in BuildSurfaceCell.All)
            {
                if (candidate == null || candidate.Occupant == null) continue;
                if (hitTransform.IsChildOf(candidate.Occupant.transform))
                    return candidate;
            }

            return null;
        }

        private FarmTile ResolveFarmTileFromHit(RaycastHit hit)
        {
            if (hit.collider == null) return null;

            FarmTile tile = hit.collider.GetComponentInParent<FarmTile>();
            if (tile != null) return tile;

            if (hit.collider.TryGetComponent<FarmTile>(out tile))
                return tile;

            BuildSurfaceCell cell = ResolveBuildSurfaceCellFromHit(hit);
            if (cell == null || cell.Occupant == null) return null;

            tile = cell.Occupant.GetComponent<FarmTile>();
            if (tile != null) return tile;

            return cell.Occupant.GetComponentInChildren<FarmTile>();
        }

        private bool TryResolveFarmTileFromAim(Ray ray, out FarmTile tile)
        {
            tile = null;
            int hitCount = Physics.SphereCastNonAlloc(ray, FarmTileAimFallbackRadius, tileAimHitResults, 100f, InteractionLayerMask, QueryTriggerInteraction.Collide);
            if (hitCount <= 0) return false;

            System.Array.Sort(tileAimHitResults, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));
            for (int i = 0; i < hitCount; i++)
            {
                var candidate = ResolveFarmTileFromHit(tileAimHitResults[i]);
                if (candidate == null || !IsTileInRange(candidate)) continue;
                tile = candidate;
                return true;
            }

            return false;
        }

        private void AddTileAction(FarmTile tile, List<InteractionAction> actions)
        {
            if (tile == null || actions == null) return;

            string actName = "T\u01b0\u01a1ng t\u00e1c";
            switch (tile.currentState)
            {
                case FarmTile.TileState.Soil: actName = "Cu\u1ed1c \u0111\u1ea5t"; break;
                case FarmTile.TileState.Plowed: actName = "Gieo h\u1ea1t"; break;
                case FarmTile.TileState.Planted: actName = "T\u01b0\u1edbi n\u01b0\u1edbc"; break;
                case FarmTile.TileState.Watered: actName = "T\u01b0\u1edbi n\u01b0\u1edbc"; break;
                case FarmTile.TileState.Ripe: actName = "Thu ho\u1ea1ch"; break;
            }

            actions.Add(new InteractionAction { keyName = "Click", actionName = actName, onClick = () => PerformTileAction(tile) });
        }

        private bool IsPenSpawnerInRange(AnimalPenSpawner pen)
        {
            if (pen == null) return false;
            return HorizontalDistanceToClosestColliderPoint(pen.gameObject, pen.transform.position) <= NormalizeGroundRange(enclosureInteractRange);
        }

        private bool IsEnclosureInRange(List<BuildSurfaceCell> enclosure)
        {
            if (enclosure == null || enclosure.Count == 0) return false;

            float range = NormalizeGroundRange(enclosureInteractRange);
            foreach (var cell in enclosure)
            {
                if (cell == null) continue;
                if (HorizontalDistanceToClosestColliderPoint(cell.gameObject, cell.SurfaceCenter) <= range)
                    return true;
                if (cell.Occupant != null && HorizontalDistanceToClosestColliderPoint(cell.Occupant, cell.SurfaceCenter) <= range)
                    return true;
            }
            return false;
        }

        private bool IsTileInRange(FarmTile tile)
        {
            if (tile == null) return false;
            return HorizontalDistanceToClosestColliderPoint(tile.gameObject, tile.transform.position) <= GetTileInteractRange();
        }

        private float GetTileInteractRange()
        {
            return Mathf.Max(NormalizeGroundRange(tileInteractRange), DefaultTileInteractRange);
        }

        private int InteractionLayerMask => farmTileLayer.value != 0 ? farmTileLayer.value : ~0;

        private bool IsAnimalInRange(FarmAnimal animal, RaycastHit hit)
        {
            return IsAnimalInRange(animal);
        }

        private bool IsAnimalInRange(FarmAnimal animal)
        {
            if (animal == null) return false;
            float range = NormalizeRange(animalInteractRange);
            return HorizontalDistanceToClosestColliderPoint(animal.gameObject, animal.transform.position) <= range;
        }

        private bool IsWaterSourceInRange(WaterSource waterSource)
        {
            if (waterSource == null) return false;
            float range = ClampedRange(waterSource.interactRange, waterInteractRange);
            return HorizontalDistanceToClosestColliderPoint(waterSource.gameObject, waterSource.transform.position) <= range;
        }

        private float GetFishingRange(FishingSpot spot)
        {
            float configuredRange = spot != null && spot.interactionRange > 0f
                ? spot.interactionRange
                : Mathf.Max(NormalizeRange(fishingInteractRange), DefaultFishingInteractRange);
            configuredRange = Mathf.Max(configuredRange, DefaultFishingInteractRange);
            return Mathf.Clamp(configuredRange, 0.5f, MaxFishingInteractRange);
        }

        private bool IsFishingSpotInRange(FishingSpot spot, Vector3 hitPoint)
        {
            if (spot == null) return false;
            Vector3 playerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
            return HorizontalDistance(playerPos, hitPoint) <= GetFishingRange(spot);
        }

        private bool CanPriorityScanPassThrough(RaycastHit hit)
        {
            if (hit.collider == null) return true;
            if (hit.collider.gameObject.CompareTag("Player")) return true;
            if (hit.collider.isTrigger) return true;
            if (hit.collider.GetComponentInParent<FenceAutoConnect>() != null) return true;
            if (hit.collider.GetComponentInParent<BuildSurfaceCell>() != null) return true;
            return false;
        }

        private FarmAnimal FindPriorityAnimalTarget(RaycastHit[] hits, int hitCount)
        {
            float solidBlockDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                var hit = hits[i];
                if (hit.collider == null) continue;
                if (hit.collider.gameObject.CompareTag("Player")) continue;
                if (hit.distance > solidBlockDistance) break;

                var animal = hit.collider.GetComponentInParent<FarmAnimal>();
                if (animal != null)
                {
                    if (!IsAnimalInRange(animal, hit))
                        continue;

                    return animal;
                }

                if (!CanPriorityScanPassThrough(hit))
                    solidBlockDistance = Mathf.Min(solidBlockDistance, hit.distance + SolidHitPassthroughTolerance);
            }

            return null;
        }

        private void AddAnimalActions(FarmAnimal animal, List<InteractionAction> actions,
            out FarmAnimal.AnimalState animalState, out bool hasProduct)
        {
            animalState = FarmAnimal.AnimalState.Healthy;
            hasProduct = false;
            if (animal == null || actions == null) return;

            animalState = animal.currentState;
            hasProduct = animal.hasProductReady;

            // Cho an trong demo can hien ngay sau khi tha thu, ke ca luc thanh no con day.
            if (animal.currentState == FarmAnimal.AnimalState.Hungry || animal.currentState == FarmAnimal.AnimalState.Healthy)
                actions.Add(new InteractionAction { keyName = "F", actionName = "Cho ăn", onClick = () => FeedAnimal(animal) });
            if (animal.hasProductReady)
                actions.Add(new InteractionAction { keyName = "R", actionName = "Thu hoạch", onClick = () => HarvestAnimal(animal) });
            if (animal.currentState == FarmAnimal.AnimalState.Sick)
                actions.Add(new InteractionAction { keyName = "H", actionName = "Chữa bệnh", onClick = () => HealAnimal(animal) });

            actions.Add(new InteractionAction { keyName = "Click", actionName = "Thông tin", onClick = () => { if (AnimalInteractionPopupController.Instance != null) AnimalInteractionPopupController.Instance.Show(animal); } });
        }

        private bool TryGetAnimalEnclosure(FarmAnimal animal, out List<BuildSurfaceCell> enclosure)
        {
            enclosure = null;
            if (animal == null || animal.occupiedCells == null) return false;

            foreach (var cell in animal.occupiedCells)
            {
                if (cell == null || !cell.HasFence) continue;

                enclosure = PenEnclosure.FindPen(cell);
                return enclosure != null && enclosure.Count > 0;
            }

            return false;
        }

        private void AddEnclosureActions(List<BuildSurfaceCell> enclosure, List<InteractionAction> actions)
        {
            if (enclosure == null || actions == null) return;

            var viewEnclosure = new List<BuildSurfaceCell>(enclosure);
            actions.Add(new InteractionAction
            {
                keyName = "Click",
                actionName = "Xem chuồng",
                onClick = () =>
                {
                    if (AnimalInteractionPopupController.Instance != null)
                        AnimalInteractionPopupController.Instance.ShowEnclosure(viewEnclosure);
                }
            });

            if (PenEnclosure.AvailableCount(enclosure) > 0)
            {
                var addEnclosure = new List<BuildSurfaceCell>(enclosure);
                actions.Add(new InteractionAction { keyName = "E", actionName = "Thả thú", onClick = () => OpenEnclosurePicker(addEnclosure) });
            }

            var demolishEnclosure = new List<BuildSurfaceCell>(enclosure);
            actions.Add(new InteractionAction { keyName = "G", actionName = "Hủy chuồng", onClick = () => RequestDemolishEnclosure(demolishEnclosure) });
        }

        private bool TryShowAnimalEnclosurePopup(FarmAnimal animal)
        {
            if (!TryGetAnimalEnclosure(animal, out var enclosure)) return false;
            if (!IsEnclosureInRange(enclosure)) return false;

            if (AnimalInteractionPopupController.Instance != null)
                AnimalInteractionPopupController.Instance.ShowEnclosure(enclosure);
            return true;
        }

        private FarmAnimal FindAnimalInEnclosure(List<BuildSurfaceCell> enclosure)
        {
            if (enclosure == null || enclosure.Count == 0) return null;

            FarmAnimal best = null;
            float bestDistance = float.PositiveInfinity;
            bool enclosureInRange = IsEnclosureInRange(enclosure);

            foreach (var cell in enclosure)
            {
                if (cell == null || cell.AnimalObject == null) continue;

                var animal = cell.AnimalObject.GetComponent<FarmAnimal>();
                if (animal == null) animal = cell.AnimalObject.GetComponentInChildren<FarmAnimal>();
                if (animal == null || (!enclosureInRange && !IsAnimalInRange(animal))) continue;

                float distance = HorizontalDistanceToClosestColliderPoint(animal.gameObject, animal.transform.position);
                if (distance < bestDistance)
                {
                    best = animal;
                    bestDistance = distance;
                }
            }

            if (best != null) return best;

            var animals = Object.FindObjectsByType<FarmAnimal>(FindObjectsSortMode.None);
            foreach (var animal in animals)
            {
                if (animal == null || animal.occupiedCells == null || (!enclosureInRange && !IsAnimalInRange(animal))) continue;

                bool belongsToEnclosure = false;
                foreach (var cell in animal.occupiedCells)
                {
                    if (cell != null && enclosure.Contains(cell))
                    {
                        belongsToEnclosure = true;
                        break;
                    }
                }
                if (!belongsToEnclosure) continue;

                float distance = HorizontalDistanceToClosestColliderPoint(animal.gameObject, animal.transform.position);
                if (distance < bestDistance)
                {
                    best = animal;
                    bestDistance = distance;
                }
            }

            return best;
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Gameplay) return;
            if (IsBuildModeOpen())
            {
                ClearWorldInteractionState();
                return;
            }
            if (IsPlayerSwimming())
            {
                ClearWorldInteractionStateForSwimming();
                return;
            }
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

            // NGẮM THEO TÂM MÀN HÌNH (crosshair) cho ỔN ĐỊNH: dùng cả khi pointer đang nằm trên UI
            // (vd đang giữ joystick), để prompt thế giới vẫn cập nhật theo tâm ngắm.
            Vector2 aimPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            HandleHover(aimPos);

            bool pointerPressed = pointer.press.wasPressedThisFrame;
            bool pointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            // Khi chuột đang khóa/tâm ngắm hoặc mobile tap ngoài UI, click/tap phải kích hoạt action "Click"
            // đang hiện dưới tâm. Nếu chuột đã được nhả và đang bấm trực tiếp lên UI button thì để UI tự xử lý.
            if (pointerPressed && !timedActionActive && (!pointerOverUI || IsLockedMousePointer(pointer)) && TryInvokeCurrentClickAction())
                return;

            if (!timedActionActive && TryInvokeCurrentHotkeyAction())
                return;

            if (pointerOverUI)
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

            if (pointerPressed) HandleClick(aimPos);
            if (timedActionActive) return;

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

            // Timeout hủy chuồng: nhấn 2 lần trong thời gian ngắn
            if (demolishConfirmTimer > 0f)
            {
                demolishConfirmTimer -= Time.deltaTime;
                if (demolishConfirmTimer <= 0f)
                {
                    pendingDemolishEnclosure = null;
                    demolishConfirmTimer = 0f;
                }
            }

            // Xử lý Phím tắt Tương tác (PC)
            TryInvokeCurrentHotkeyAction();
        }

        private bool IsLockedMousePointer(Pointer pointer)
        {
            return Mouse.current != null &&
                object.ReferenceEquals(pointer, Mouse.current) &&
                (UnityEngine.Cursor.lockState == CursorLockMode.Locked || !UnityEngine.Cursor.visible);
        }

        private bool TryInvokeCurrentHotkeyAction()
        {
            if (currentActions == null || currentActions.Count == 0) return false;
            bool isTyping = ChatPanelController.Instance != null && ChatPanelController.Instance.IsTyping();
            var keyboard = Keyboard.current;
            if (isTyping || keyboard == null) return false;

            global::System.Action hotkeyClick = null;
            for (int i = 0; i < currentActions.Count; i++)
            {
                var action = currentActions[i];
                bool pressed =
                    (action.keyName == "F" && keyboard.fKey.wasPressedThisFrame) ||
                    (action.keyName == "E" && keyboard.eKey.wasPressedThisFrame) ||
                    (action.keyName == "R" && keyboard.rKey.wasPressedThisFrame) ||
                    (action.keyName == "H" && keyboard.hKey.wasPressedThisFrame) ||
                    (action.keyName == "G" && keyboard.gKey.wasPressedThisFrame);

                if (pressed)
                {
                    hotkeyClick = action.onClick;
                    break;
                }
            }

            if (hotkeyClick == null) return false;
            hotkeyClick.Invoke();
            return true;
        }

        private bool TryInvokeCurrentClickAction()
        {
            if (currentActions == null || currentActions.Count == 0) return false;

            global::System.Action clickAction = null;
            string clickActionName = null;
            string clickKeyName = null;
            for (int i = 0; i < currentActions.Count; i++)
            {
                var action = currentActions[i];
                if (action.keyName == "Click" && action.onClick != null)
                {
                    clickAction = action.onClick;
                    clickActionName = action.actionName;
                    clickKeyName = action.keyName;
                    break;
                }
            }

            if (clickAction == null) return false;
            Debug.Log($"[FarmInteraction] Crosshair click action: {clickKeyName} {clickActionName}");
            clickAction.Invoke();
            return true;
        }

        private bool IsBuildModeOpen()
        {
            return BuildModeOverlayController.Instance != null && BuildModeOverlayController.Instance.IsVisible();
        }

        private void ClearWorldInteractionState()
        {
            if (currentHarvestTarget != null)
            {
                currentHarvestTarget.CancelHarvest();
                currentHarvestTarget = null;
            }

            currentHoverObject = null;
            currentActions.Clear();

            if (GameHUDController.Instance != null)
                GameHUDController.Instance.HideInteractionPrompt();
            if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();
        }

        private void ClearWorldInteractionStateForSwimming()
        {
            if (timedActionActive || timedActionRoutine != null)
                CancelTimedAction(null);

            _buttonHeldResource = null;
            _chopAnimTimer = 0f;
            ClearWorldInteractionState();
        }

        private RaycastHit[] hoverHitResults = new RaycastHit[30];
        private RaycastHit[] tileAimHitResults = new RaycastHit[32];
        private GameObject currentHoverObject = null;
        private FarmAnimal.AnimalState lastAnimalState;
        private bool lastAnimalProductReady;
        private string lastActionSignature = "";
        private List<InteractionAction> currentActions = new List<InteractionAction>();

        private void HandleHover(Vector2 screenPos)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            int hitCount = Physics.RaycastNonAlloc(ray, hoverHitResults, 100f, InteractionLayerMask, QueryTriggerInteraction.Collide);
            System.Array.Sort(hoverHitResults, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));
            Vector3 playerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
            float solidPassthroughLimit = float.PositiveInfinity;

            List<InteractionAction> foundActions = new List<InteractionAction>();
            GameObject foundObj = null;
            FarmAnimal.AnimalState currentAnimalState = FarmAnimal.AnimalState.Healthy;
            bool currentHasProduct = false;

            var priorityAnimal = FindPriorityAnimalTarget(hoverHitResults, hitCount);
            if (priorityAnimal != null)
            {
                if (TryGetAnimalEnclosure(priorityAnimal, out var priorityEnclosure) && IsEnclosureInRange(priorityEnclosure))
                {
                    foundObj = priorityAnimal.gameObject;
                    AddEnclosureActions(priorityEnclosure, foundActions);
                }
                else
                {
                    foundObj = priorityAnimal.gameObject;
                    AddAnimalActions(priorityAnimal, foundActions, out currentAnimalState, out currentHasProduct);
                }
            }

            for (int i = 0; foundActions.Count == 0 && i < hitCount; i++)
            {
                var hit = hoverHitResults[i];
                if (hit.collider == null) continue;
                if (hit.collider.gameObject.CompareTag("Player")) continue;
                if (hit.distance > solidPassthroughLimit) break;

                var animal = hit.collider.GetComponentInParent<FarmAnimal>();
                if (animal != null)
                {
                    if (!IsAnimalInRange(animal, hit))
                        continue;

                    if (TryGetAnimalEnclosure(animal, out var animalEnclosure) && IsEnclosureInRange(animalEnclosure))
                    {
                        foundObj = animal.gameObject;
                        AddEnclosureActions(animalEnclosure, foundActions);
                    }
                    else
                    {
                        foundObj = animal.gameObject;
                        AddAnimalActions(animal, foundActions, out currentAnimalState, out currentHasProduct);
                    }
                    break;
                }
                else if (hit.collider.TryGetComponent<HarvestableResource>(out var resource) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<HarvestableResource>(out resource)))
                {
                    float resourceRange = ClampedRange(resource.interactionRange, resourceInteractRange);
                    if (HorizontalDistance(playerPos, hit.point) > resourceRange)
                        continue;

                    // Chỉ hiện nút khi TÂM NGẮM chạm bề mặt cây/đá trong tầm với (resource.interactionRange).
                    // Đo từ nhân vật tới ĐIỂM CHẠM (hit.point) cho trực quan, đúng cả với cây to.
                    // ĐÀO ĐÁ chỉ ở thành phố → ở đảo khác KHÔNG hiện nút "Đào khoáng" (chặt cây vẫn hiện).
                    bool resourceUsable = resource.type != HarvestableResource.ResourceType.Rock || IsOnCityIsland();
                    if (resourceUsable)
                    {
                        foundObj = resource.gameObject;
                        string actionStr = resource.type == HarvestableResource.ResourceType.Tree ? "Chặt cây" : "Đào khoáng";
                        var res = resource; // capture cho closure
                        foundActions.Add(new InteractionAction { keyName = "Click", actionName = actionStr, onClick = () => ClickHarvestResource(res) });
                    }
                    break;
                }
                else if (hit.collider.TryGetComponent<MerchantNPC>(out var merchant) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<MerchantNPC>(out merchant)))
                {
                    if (!IsInInteractRangeAtPoint(hit.point, merchantInteractRange))
                        continue;

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
                        foundActions.Add(new InteractionAction { keyName = "Click", actionName = "Thả thú", onClick = () => { if (IsPenSpawnerInRange(penS)) OpenPenAnimalPicker(penS); } });
                    }
                    break;
                }
                else if (ResolveFarmTileFromHit(hit) is FarmTile tile && tile != null)
                {
                    if (!IsTileInRange(tile))
                        continue;

                    foundObj = tile.gameObject;
                    AddTileAction(tile, foundActions);
                    break;
                }
                else if (hit.collider.TryGetComponent<FishingSpot>(out var spot) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<FishingSpot>(out spot)))
                {
                    Vector3 fishingHitPoint = hit.point;
                    if (IsFishingSpotInRange(spot, fishingHitPoint) && IsFishingAllowedHere())
                    {
                        foundObj = spot.gameObject;
                        foundActions.Add(new InteractionAction { keyName = "F", actionName = "Câu cá", onClick = () => StartFishing(spot, fishingHitPoint) });
                    }
                    break;
                }
                else if (hit.collider.GetComponentInParent<WaterSource>() is WaterSource waterSrc && waterSrc != null)
                {
                    // Vùng ao MÚC ĐƯỢC (không phải nước biển) → bấm "Múc nước" lấy xô nước về túi.
                    if (IsWaterSourceInRange(waterSrc))
                    {
                        foundObj = waterSrc.gameObject;
                        var ws = waterSrc;
                        foundActions.Add(new InteractionAction { keyName = "Click", actionName = "Múc nước", onClick = () => ScoopWater(ws) });
                    }
                    break;
                }
                else if (ResolveBuildSurfaceCellFromHit(hit) is BuildSurfaceCell penCell && penCell != null)
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
                            var encl = hoverEnclosure;
                            if (IsEnclosureInRange(encl))
                            {
                                foundObj = penCell.gameObject;
                                AddEnclosureActions(encl, foundActions);
                            }
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
                else if (!hit.collider.isTrigger)
                {
                    // Cho phép nhìn xuyên thêm một đoạn rất ngắn sau mặt đất/collider mỏng
                    // để bắt các object sát nền mà vẫn không quét xuyên quá sâu.
                    solidPassthroughLimit = Mathf.Min(solidPassthroughLimit, hit.distance + SolidHitPassthroughTolerance);
                    continue;
                }
            }

            if (foundActions.Count == 0 && TryResolveFarmTileFromAim(ray, out var aimTile))
            {
                foundObj = aimTile.gameObject;
                AddTileAction(aimTile, foundActions);
            }

            // Update UI if target or state changed
            if (foundActions.Count > 0)
            {
                string actionSignature = BuildActionSignature(foundActions);
                bool hadNoCurrentActions = currentActions == null || currentActions.Count == 0;
                bool shouldRefreshPrompt =
                    hadNoCurrentActions ||
                    foundObj != currentHoverObject ||
                    currentAnimalState != lastAnimalState ||
                    currentHasProduct != lastAnimalProductReady ||
                    actionSignature != lastActionSignature;

                currentHoverObject = foundObj;
                lastAnimalState = currentAnimalState;
                lastAnimalProductReady = currentHasProduct;
                lastActionSignature = actionSignature;
                currentActions = foundActions;

                if (shouldRefreshPrompt)
                {
                    if (GameHUDController.Instance != null) GameHUDController.Instance.ShowInteractionPrompts(foundActions);
                }
            }
            else
            {
                if (currentHoverObject != null)
                {
                    currentHoverObject = null;
                    lastActionSignature = "";
                    currentActions.Clear();
                    if (GameHUDController.Instance != null) GameHUDController.Instance.HideInteractionPrompt();
                }
            }
        }

        // --- CÁC HÀM XỬ LÝ SỰ KIỆN NÚT BẤM ---
        // Đang ở đảo Thành phố? (CÂU CÁ + ĐÀO ĐÁ chỉ ở thành phố — khách chốt 20/06).
        private static string BuildActionSignature(List<InteractionAction> actions)
        {
            if (actions == null || actions.Count == 0) return "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < actions.Count; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append(actions[i].keyName);
                sb.Append(':');
                sb.Append(actions[i].actionName);
            }
            return sb.ToString();
        }

        private bool IsOnCityIsland()
        {
            var itm = IslandTravelManager.Instance;
            return itm != null && itm.CurrentIslandId == "city";
        }
        private bool IsPlayerSwimming()
        {
            var player = PlayerController.Instance;
            return player != null && player.isSwimming;
        }

        private bool IsFishingAllowedHere() => IsOnCityIsland() && !IsPlayerSwimming();

        private void StartFishing(FishingSpot spot, Vector3 targetPoint)
        {
            if (!IsFishingAllowedHere())
            {
                ScreenToast.Show("Chỉ câu cá được ở Đảo Thành phố thôi!");
                return;
            }

            if (!IsFishingSpotInRange(spot, targetPoint))
            {
                ScreenToast.Show("Đứng gần bờ hơn để câu cá nhé!");
                return;
            }

            var fishingUI = Object.FindFirstObjectByType<FishingOverlayController>();
            if (fishingUI == null)
            {
                ScreenToast.Show("Hệ thống câu cá chưa sẵn sàng.");
                return;
            }

            if (!fishingUI.CanStartFishing()) return;

            currentHoverObject = null;
            currentActions.Clear();
            GameHUDController.Instance?.HideInteractionPrompt();

            PlayerController player = PlayerController.Instance;
            float duration = 8.7f;
            if (player != null)
            {
                player.FaceTowards(targetPoint);
                duration = player.PlayActionAnimation("Fishing", 8.7f, YWonderLand.Player.ToolType.FishingRod); // khớp độ dài clip Fishing ~8.7s
                if (duration <= 0f) duration = 8.7f;
            }

            // Chuẩn bị câu: lưu cao độ mặt nước, CHỜ Animation Event (frame vung cần) bắn dây ra.
            if (FishingLineController.Instance != null && spot != null)
                FishingLineController.Instance.PrepareCast(targetPoint.y);

            fishingUI.BeginAutoFishing(duration);
        }

        /// <summary>Cổng public cho UI (popup Thú nuôi) gọi luồng cho ăn qua túi đồ.</summary>
        public void BeginFeed(FarmAnimal animal) => FeedAnimal(animal);

        /// <summary>Cổng public cho popup chuồng gọi lại đúng luồng mở túi để thả thêm thú.</summary>
        public void BeginPlaceAnimalInEnclosure(List<BuildSurfaceCell> interior) => OpenEnclosurePicker(interior);

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
            bool feedReserved = inv != null && inv.RemoveItem(itemId, required);
            if (!feedReserved)
                return;

            pendingFeedAnimal = null;

            if (inventoryPopup != null) inventoryPopup.Hide();

            bool started = BeginTimedAction(
                "Feed",
                FeedClipDuration,
                YWonderLand.Player.ToolType.AnimalFeed,
                animal.transform.position,
                () =>
                {
                    if (animal != null)
                    {
                        animal.Feed();
                        ScreenToast.ShowInfo($"Đã cho {(def != null ? def.animalName : "thú")} ăn {required}x {matchedName}.");
                    }
                    else if (inv != null)
                    {
                        inv.AddItem(itemId, required);
                    }
                },
                () =>
                {
                    if (inv != null)
                        inv.AddItem(itemId, required);
                },
                FeedActionSpeed);

            if (!started && inv != null)
                inv.AddItem(itemId, required);
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
            // Luu data truoc khi thu, vi vu cuoi co the huy GameObject con vat.
            var animalData = animal != null ? animal.data : null;

            if (animal.HarvestProduct(out string itemId, out int amount))
            {
                Managers.InventoryManager.Instance?.AddItem(itemId, amount);

                // VatNuoi2: EXP chi cong o lan thu hoach cuoi.
                if (animalData != null && animal.LastHarvestWasFinal && animalData.expReward > 0)
                {
                    Managers.ExperienceManager.Instance?.AddEXP(animalData.expReward);
                }

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

            if (GetResourceDistanceToPlayer(resource) > GetResourceExecuteRange(resource))
                return;

            StartResourceTimedAction(resource);
        }

        private void StartResourceTimedAction(HarvestableResource resource)
        {
            if (resource == null || !resource.isHarvestable) return;

            currentHarvestTarget = resource;
            resource.CancelHarvest();

            bool isTree = resource.type == HarvestableResource.ResourceType.Tree;
            string anim = isTree ? "TreeCuttingV4" : "Mining";
            float duration = isTree ? TreeCuttingClipDuration : MiningClipDuration;
            var tool = isTree ? YWonderLand.Player.ToolType.Axe : YWonderLand.Player.ToolType.Pickaxe;

            bool started = BeginTimedAction(
                anim,
                duration,
                tool,
                resource.transform.position,
                () =>
                {
                    if (resource != null && resource.isHarvestable)
                        HarvestResourceTick(resource, Mathf.Max(resource.harvestDuration, 0.1f));
                    if (currentHarvestTarget == resource)
                        currentHarvestTarget = null;
                },
                () =>
                {
                    if (resource != null)
                        resource.CancelHarvest();
                    if (currentHarvestTarget == resource)
                        currentHarvestTarget = null;
                });

            if (!started)
                currentHarvestTarget = null;
        }

        // Chặt/đập LIÊN TỤC 1 tài nguyên trong lúc GIỮ nút HUD (mobile). Tăng tiến độ theo thời gian
        // thật + lặp animation mỗi ~0.9s -> giống hệt giữ chuột trên thế giới, nhưng kích hoạt từ nút.
        private void HoldChopResource(HarvestableResource resource)
        {
            if (resource == null || !resource.isHarvestable) { _buttonHeldResource = null; return; }

            PlayerController player = PlayerController.Instance;
            if (GetResourceDistanceToPlayer(resource) > GetResourceExecuteRange(resource)) return; // quá xa -> khựng

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
            if (!IsTileInRange(tile))
            {
                Debug.LogWarning($"[FarmInteraction] Tile action blocked by range: tile={(tile != null ? tile.name : "null")}, range={GetTileInteractRange():0.00}");
                return;
            }

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
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f, InteractionLayerMask, QueryTriggerInteraction.Collide);

            foreach (var hit in hits)
            {
                HarvestableResource resource = hit.collider.GetComponent<HarvestableResource>();
                if (resource == null)
                    resource = hit.collider.GetComponentInParent<HarvestableResource>();
                if (resource != null)
                {
                    // ĐÀO ĐÁ chỉ ở thành phố — bỏ qua tảng đá nếu đang ở đảo khác (chặt cây thì vẫn được).
                    if (resource.type == HarvestableResource.ResourceType.Rock && !IsOnCityIsland()) continue;

                    // Quá xa thì không cho chặt/đập (đo tới điểm chạm, khớp với lúc hiện gợi ý)
                    Vector3 holdPlayerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
                    if (HorizontalDistance(holdPlayerPos, hit.point) > ClampedRange(resource.interactionRange, resourceInteractRange))
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
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f, InteractionLayerMask, QueryTriggerInteraction.Collide);
            float solidPassthroughLimit = float.PositiveInfinity;

            Debug.Log($"[FarmInteraction] Bấm chuột! Tia laze trúng {hits.Length} vật thể.");

            // 1. Dùng tia quét đã sắp xếp
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            var priorityAnimal = FindPriorityAnimalTarget(hits, hits.Length);
            if (priorityAnimal != null)
            {
                if (!TryShowAnimalEnclosurePopup(priorityAnimal) && AnimalInteractionPopupController.Instance != null)
                    AnimalInteractionPopupController.Instance.Show(priorityAnimal);
                return;
            }

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.CompareTag("Player")) continue;
                if (hit.distance > solidPassthroughLimit) break;

                FarmAnimal animal = hit.collider.GetComponentInParent<FarmAnimal>();
                if (animal != null)
                {
                    if (!IsAnimalInRange(animal, hit))
                        return;

                    if (!TryShowAnimalEnclosurePopup(animal) && AnimalInteractionPopupController.Instance != null)
                        AnimalInteractionPopupController.Instance.Show(animal);
                    return;
                }

                HarvestableResource resource = hit.collider.GetComponent<HarvestableResource>();
                if (resource == null)
                    resource = hit.collider.GetComponentInParent<HarvestableResource>();
                if (resource != null)
                {
                    Vector3 resourcePlayerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
                    if (HorizontalDistance(resourcePlayerPos, hit.point) > ClampedRange(resource.interactionRange, resourceInteractRange))
                        return;

                    ClickHarvestResource(resource);
                    return;
                }

                // Múc nước bằng TÂM NGẮM (click) — giống chặt cây/đào, chạy cả PC lẫn mobile.
                var waterSrcClick = hit.collider.GetComponentInParent<WaterSource>();
                if (waterSrcClick != null)
                {
                    if (IsWaterSourceInRange(waterSrcClick)) ScoopWater(waterSrcClick);
                    return;
                }

                FarmTile tile = ResolveFarmTileFromHit(hit);
                if (tile != null)
                {
                    if (!IsTileInRange(tile))
                    {
                        Debug.Log("[FarmInteraction] Too far from tile.");
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
                var penCellClick = ResolveBuildSurfaceCellFromHit(hit);
                if (penCellClick != null && penCellClick.HasFence)
                {
                    var pen = PenEnclosure.FindPen(penCellClick);
                    if (pen != null)
                    {
                        if (IsEnclosureInRange(pen) && AnimalInteractionPopupController.Instance != null)
                            AnimalInteractionPopupController.Instance.ShowEnclosure(pen);
                    }
                    return;
                }

                // Chuồng thú (kiểu cũ): click để mở túi đồ (tab Thú nuôi) chọn con vật thả vào chuồng.
                var penSpawner = hit.collider.GetComponentInParent<YWonderLand.Environment.AnimalPenSpawner>();
                if (penSpawner != null)
                {
                    if (IsPenSpawnerInRange(penSpawner))
                        OpenPenAnimalPicker(penSpawner);
                    return;
                }

                MerchantNPC merchant = hit.collider.GetComponent<MerchantNPC>();
                if (merchant != null)
                {
                    if (!IsInInteractRangeAtPoint(hit.point, merchantInteractRange))
                        return;

                    Debug.Log($"[FarmInteraction] TÌM THẤY MERCHANT: {merchant.gameObject.name}! Mở Shop...");
                    merchant.Interact();
                    return;
                }

                // Bỏ qua collider HÀNG RÀO để tia tới được ô chuồng/đất phía sau (click thả thú).
                if (hit.collider.GetComponentInParent<FenceAutoConnect>() != null) continue;

                // Thêm kiểm tra chặn tia
                if (!hit.collider.isTrigger)
                {
                    solidPassthroughLimit = Mathf.Min(solidPassthroughLimit, hit.distance + SolidHitPassthroughTolerance);
                    continue;
                }
            }

            if (TryResolveFarmTileFromAim(ray, out var aimTile))
            {
                PerformTileAction(aimTile);
                return;
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

            BeginTimedAction(
                "Hoeing",
                HoeingFallbackDuration,
                YWonderLand.Player.ToolType.Hoe,
                tile.transform.position,
                () =>
                {
                    if (tile != null && tile.currentState == FarmTile.TileState.Soil && tile.InteractPlow())
                        Debug.Log("[FarmInteraction] Plowed tile!");
                });
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
                StartPlantTimedAction(tile, "carrot_seed_01", false);
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

        private void RequestDemolishEnclosure(List<BuildSurfaceCell> encl)
        {
            if (encl == null || encl.Count == 0) return;

            if (pendingDemolishEnclosure != null && IsSameEnclosure(pendingDemolishEnclosure, encl))
            {
                pendingDemolishEnclosure = null;
                demolishConfirmTimer = 0f;
                DemolishEnclosure(encl);
                return;
            }

            pendingDemolishEnclosure = new List<BuildSurfaceCell>(encl);
            demolishConfirmTimer = DemolishConfirmWindow;
            ScreenToast.Show("Nhấn hủy chuồng lần nữa để xác nhận.");
        }

        private bool IsSameEnclosure(List<BuildSurfaceCell> a, List<BuildSurfaceCell> b)
        {
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;

            var set = new HashSet<BuildSurfaceCell>(a);
            foreach (var c in b)
            {
                if (c == null || !set.Contains(c))
                    return false;
            }
            return true;
        }

        // Phá CẢ CỤM rào (chuồng): trả con giống về túi → gỡ hàng rào → hoàn 1 phần giá build.
        private string ResolveAnimalItemIdInEnclosure(FarmAnimal animal, List<BuildSurfaceCell> pen)
        {
            if (animal == null) return "";

            if (pen != null)
            {
                foreach (var cell in pen)
                {
                    if (cell == null || cell.AnimalObject == null) continue;

                    var cellAnimal = cell.AnimalObject.GetComponent<FarmAnimal>();
                    if (cellAnimal == null) cellAnimal = cell.AnimalObject.GetComponentInChildren<FarmAnimal>();
                    if (cellAnimal == animal && !string.IsNullOrEmpty(cell.AnimalItemId))
                        return cell.AnimalItemId;
                }
            }

            return animal.data != null ? animal.data.animalId : "";
        }

        private void DemolishEnclosure(List<BuildSurfaceCell> pen)
        {
            if (pen == null || pen.Count == 0) return;
            pendingDemolishEnclosure = null;
            demolishConfirmTimer = 0f;

            var inv = YWonderLand.Managers.InventoryManager.Instance;
            int refundWood = 0;
            int refundStone = 0;
            int returnedAnimals = 0;
            int removedFences = 0;
            var destroyedAnimalObjects = new HashSet<GameObject>();
            var animalsInPen = PenEnclosure.FindAnimals(pen);

            AnimalInteractionPopupController.Instance?.Hide();

            foreach (var animal in animalsInPen)
            {
                if (animal == null || animal.gameObject == null) continue;

                string animalItemId = ResolveAnimalItemIdInEnclosure(animal, pen);
                if (inv != null && !string.IsNullOrEmpty(animalItemId))
                {
                    inv.AddItem(animalItemId, 1);
                    returnedAnimals++;
                }

                if (pendingFeedAnimal == animal) pendingFeedAnimal = null;
                if (currentHoverObject == animal.gameObject) currentHoverObject = null;

                if (animal.occupiedCells != null)
                {
                    foreach (var occupiedCell in animal.occupiedCells)
                        if (occupiedCell != null) occupiedCell.ClearAnimal();
                    animal.occupiedCells = null;
                }

                destroyedAnimalObjects.Add(animal.gameObject);
                Destroy(animal.gameObject);
            }

            foreach (var c in pen)
            {
                if (c == null) continue;

                // 1) Trả con giống về túi (chỉ ô NEO mới giữ tham chiếu thật).
                if (c.AnimalObject != null && !destroyedAnimalObjects.Contains(c.AnimalObject))
                {
                    var fallbackAnimalObject = c.AnimalObject;
                    var fallbackAnimal = fallbackAnimalObject.GetComponent<FarmAnimal>();
                    if (fallbackAnimal == null) fallbackAnimal = fallbackAnimalObject.GetComponentInChildren<FarmAnimal>();
                    if (pendingFeedAnimal == fallbackAnimal) pendingFeedAnimal = null;
                    if (currentHoverObject == fallbackAnimalObject) currentHoverObject = null;

                    if (inv != null && !string.IsNullOrEmpty(c.AnimalItemId))
                    {
                        inv.AddItem(c.AnimalItemId, 1);
                        returnedAnimals++;
                    }
                    Destroy(fallbackAnimalObject);
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
            lastActionSignature = "";
            if (GameHUDController.Instance != null) GameHUDController.Instance.HideInteractionPrompt();
            if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();

            string msg = $"Đã phá chuồng ({removedFences} ô rào)";
            if (returnedAnimals > 0) msg += $", trả {returnedAnimals} con về túi";
            if (refundWood > 0) msg += $", +{refundWood} Gỗ";
            if (refundStone > 0) msg += $", +{refundStone} Đá";
            ScreenToast.ShowInfo(msg + ".");
            Debug.Log($"[FarmInteraction] Phá chuồng: {removedFences} rào, trả {returnedAnimals} con, hoàn {refundWood} gỗ + {refundStone} đá.");

            // Lưu ngay để tránh mất trạng thái chuồng khi thoát game ngay sau khi phá.
            var persistence = Object.FindFirstObjectByType<BuildPersistence>(FindObjectsInactive.Include);
            persistence?.SaveBuildings();
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
            bool seedConsumed = false;
            if (inv != null)
            {
                if (inv.GetItemQuantity(itemId) <= 0)
                {
                    Debug.Log($"[FarmInteraction] No {itemId} in inventory! Auto-giving 1 for demo.");
                    inv.AddItem(itemId, 1);
                }
                seedConsumed = inv.RemoveItem(itemId, 1);
            }

            // Đóng túi đồ, rồi MÚA động tác trồng TRƯỚC — gieo hạt SAU khi progress/clip chạy xong.
            if (inventoryPopup != null) inventoryPopup.Hide();

            if (!StartPlantTimedAction(pendingPlantTile, itemId, seedConsumed) && seedConsumed && inv != null)
                inv.AddItem(itemId, 1);

            pendingPlantTile = null;
            pendingSeedId = null;
        }

        // Múa động tác Planting xong MỚI thật sự gieo hạt xuống ô đất.
        private bool StartPlantTimedAction(FarmTile tile, string seedId, bool seedConsumed)
        {
            if (tile == null || string.IsNullOrEmpty(seedId)) return false;

            var inv = YWonderLand.Managers.InventoryManager.Instance;
            return BeginTimedAction(
                "Planting",
                PlantingClipDuration,
                YWonderLand.Player.ToolType.SeedBag,
                tile.transform.position,
                () =>
                {
                    if (tile != null && PlantWithSlots(tile, seedId))
                    {
                        Debug.Log($"[FarmInteraction] Gieo hạt {seedId} SAU khi múa xong!");
                    }
                    else if (seedConsumed && inv != null)
                    {
                        inv.AddItem(seedId, 1);
                    }
                },
                () =>
                {
                    if (seedConsumed && inv != null)
                        inv.AddItem(seedId, 1);
                });
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

            bool waterReserved = inv.RemoveItem("watering_water_01", 1); // giữ 1 xô nước; hủy thì trả lại.
            if (!waterReserved) return;

            bool started = BeginTimedAction(
                "Watering",
                WateringClipDuration,
                YWonderLand.Player.ToolType.WateringCan,
                tile.transform.position,
                () =>
                {
                    bool watered = false;
                    if (tile != null && tile.currentState == FarmTile.TileState.Planted) watered = tile.InteractWater();
                    else if (tile != null && tile.currentState == FarmTile.TileState.Watered) watered = tile.WaterAgain();

                    if (!watered)
                        inv.AddItem("watering_water_01", 1);
                },
                () => inv.AddItem("watering_water_01", 1));

            if (!started)
                inv.AddItem("watering_water_01", 1);
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

                    // CayTrong2/CayTrongLauNam2: EXP cong o lan thu ket thuc vong doi.
                    if (tile.LastHarvestWasFinal && crop.expReward > 0)
                    {
                        YWonderLand.Managers.ExperienceManager.Instance?.AddEXP(crop.expReward);
                    }
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
