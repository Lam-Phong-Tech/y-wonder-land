using UnityEngine;
using UnityEngine.InputSystem;
using YWonderLand.Data;
using YWonderLand.Backend;
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
        // Khách chốt 23/07: câu cá phải đứng SÁT mép nước như mọi tương tác khác, không
        // đứng xa 5m quăng cần. Đặt Max = 1.5 để trần cứng: giá trị 5 đã lỡ lưu trong
        // scene/asset FishingSpot cũng bị kẹp xuống, khỏi phải sửa tay từng chỗ.
        private const float DefaultFishingInteractRange = 1.5f;
        private const float MaxFishingInteractRange = 1.5f;
        private const float SolidHitPassthroughTolerance = 0.75f;
        private const float DirectTapSurfaceTolerance = 0.05f;
        private const float DefaultFarmSlotSpacing = 0.8f;
        private static readonly Vector2Int[] FarmSlotDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };
        private const float ResourceExecuteRangePadding = 0.25f;
        private const float FarmTileAimFallbackRadius = 0.45f;
        private const float TreeCuttingClipDuration = 2.26f;
        private const float MiningClipDuration = 0.967f;
        private const float PlantingClipDuration = 4.13f;   // độ dài thật của clip Planting
        private const float PlantingActionDuration = 2f;    // thời gian TRỒNG mong muốn (phát nhanh clip cho vừa)
        private const float WateringClipDuration = 5.6f;   // độ dài thật của clip Watering
        private const float WateringActionDuration = 3f;   // thời gian TƯỚI MONG MUỐN (phát nhanh clip cho vừa)
        private const float FeedClipDuration = 9.1f;
        private const float ScoopWaterClipDuration = 6.57f;   // độ dài thật của clip ScoopWater2
        private const float ScoopWaterActionDuration = 1.5f;  // thời gian MÚC MONG MUỐN (phát nhanh clip cho vừa)
        private const float FeedActionDuration = 1.5f;  // thời gian CHO ĂN mong muốn (phát nhanh clip cho vừa)
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
        [Tooltip("Khoang cach diem mui chan dung de tu hien nut muc nuoc.")]
        [SerializeField, Range(0.2f, 2f)] private float waterFootProbeForward = 0.9f;
        [Tooltip("Ban kinh quet quanh diem mui chan de bat vung ho nuoc, khong hien vien trang.")]
        [SerializeField, Range(0.2f, 2f)] private float waterFootProbeRadius = 0.9f;
        [Tooltip("Khoang cach diem mui chan chieu ra de tu hien nut Chat cay / Dao khoang (tang hinh giong nuoc).")]
        [SerializeField, Range(0.2f, 3f)] private float resourceFootProbeForward = 1.1f;
        [Tooltip("Ban kinh quet quanh diem mui chan de bat cay/da gan, khong hien vien trang.")]
        [SerializeField, Range(0.2f, 3f)] private float resourceFootProbeRadius = 1.1f;
        [Tooltip("Khoảng cách tương tác khi câu cá (đo tới mép nước gần nhất). Trần cứng 1.5m.")]
        [SerializeField, Range(0.5f, MaxFishingInteractRange)] private float fishingInteractRange = DefaultFishingInteractRange;
        [Tooltip("Khoang cach diem mui chan chieu ra de tu hien nut Cau ca (giong nuoc/cay da).")]
        [SerializeField, Range(0.2f, 2f)] private float fishingFootProbeForward = 0.9f;
        [Tooltip("Ban kinh quet quanh diem mui chan de bat ho cau ca, khong hien vien trang.")]
        [SerializeField, Range(0.2f, 2f)] private float fishingFootProbeRadius = 0.9f;
        [Tooltip("Flow moi: tap/click truc tiep len vat the de hien UI tuong tac, khong quet theo tam man hinh.")]
        [SerializeField] private bool useDirectTapInteraction = true;
        [Tooltip("Tam click truc tiep len cay, da, nuoc, chuong. Khach yeu cau khoang 3.5m.")]
        [SerializeField, Range(1f, 3.5f)] private float directTapMaxRange = 3.5f;
        [Tooltip("Ban kinh ho tro tap truc tiep trong world-space de bu collider nho/le chuan tren mobile.")]
        [SerializeField, Range(0f, 1.25f)] private float directTapAssistWorldRadius = 0.45f;

        [Tooltip("Layer mask cho FarmTile raycasting")]
        [SerializeField] private LayerMask farmTileLayer = ~0; // Default: all layers

        [System.Serializable]
        private class GemstoneMiningReward
        {
            public string itemId;
            public string displayName;
            public int amount;
            public float chancePercent;

            public GemstoneMiningReward(string itemId, string displayName, int amount, float chancePercent)
            {
                this.itemId = itemId;
                this.displayName = displayName;
                this.amount = amount;
                this.chancePercent = chancePercent;
            }
        }

        [Header("Sản lượng tài nguyên (khách chốt)")]
        [Tooltip("Chặt xong 1 CÂY nhận bao nhiêu gỗ (khách: 10). Ép cứng nên không cần chỉnh từng cây trong scene.")]
        [SerializeField] private int treeYield = 10;
        [Tooltip("Đào xong 1 KHỐI ĐÁ nhận bao nhiêu đá thường (giữ theo gameplay hiện tại: 10 rock, 100%).")]
        [SerializeField] private int rockYield = 10;
        [Tooltip("EXP nhận mỗi lần CHẶT CÂY xong (hệ Level tối giản).")]
        [SerializeField] private int resourceExp = 5;
        [Tooltip("EXP nhận mỗi lần ĐÀO KHOÁNG xong (khách chốt 22/06: 15).")]
        [SerializeField] private int mineExp = 15;
        [Tooltip("Số lượt đào khoáng miễn phí mỗi ngày (khách chốt: 10).")]
        [SerializeField] private int dailyMiningTurns = 10;

        [Header("Dao da quy (khach chot 29/06)")]
        [SerializeField] private List<GemstoneMiningReward> gemstoneRewards = new List<GemstoneMiningReward>
        {
            new GemstoneMiningReward("gem_ruby_01", "Ruby qu\u00FD hi\u1EBFm", 1, 1f),
            new GemstoneMiningReward("gem_amethyst_01", "Amethyst", 1, 2f),
            new GemstoneMiningReward("gem_fire_quartz_01", "Fire Quartz", 2, 5f),
            new GemstoneMiningReward("gem_green_calcite_01", "Green Calcite", 3, 12f),
            new GemstoneMiningReward("gem_orange_calcite_01", "Orange Calcite", 4, 30f),
            new GemstoneMiningReward("gem_kyanite_01", "Kyanite", 4, 50f),
        };


        [Header("References")]
        [SerializeField] private Camera mainCamera;

        private InventoryPopupController inventoryPopup;
        private string pendingSeedId; // Seed được chọn từ inventory, chờ gieo
        private FarmTile pendingPlantTile; // Tile đang chờ gieo hạt
        private YWonderLand.Environment.AnimalPenSpawner pendingPen; // Chuồng đang chờ chọn con vật từ túi
        private FarmAnimal pendingFeedAnimal; // Con vật đang chờ chọn thức ăn từ túi
        private List<BuildSurfaceCell> pendingEnclosure; // Vùng quây (rào) đang chờ thả thú
        private bool animalPlacementInFlight;
        private List<BuildSurfaceCell> pendingDemolishEnclosure;
        private FarmTile pendingDemolishTile;
        private GameObject pendingDemolishPath; // công trình trang trí (vd Đường đá) đang chờ xác nhận hủy
        private float demolishConfirmTimer;
        private const float DemolishConfirmWindow = 1.25f;
        private const string FertilizerItemId = "fertilizer_01";

        [Header("Bón phân (khách chốt 30/07)")]
        [Tooltip("Mỗi lần bón rút thẳng bao nhiêu GIỜ thời gian chờ. Cố định, KHÔNG theo phần trăm " +
                 "— khách chốt vậy để cây dài ngày không bị lợi dụng. Mặc định 3.6 giờ = đúng 15% " +
                 "của cây ngắn ngày 24 giờ (con số khách đưa ban đầu). ĐỔI SỐ Ở ĐÂY, không sửa code.")]
        [SerializeField] private float fertilizerBonusHours = 3.6f;

        [Tooltip("CHỈ bón được cây có vòng lớn không quá số ngày này (khách chốt: chỉ cây ngắn ngày). " +
                 "1 = chỉ 8 cây ngắn ngày (24 giờ); nhóm kế tiếp là 2 ngày nên để 1 là tách sạch.")]
        [SerializeField] private float fertilizerMaxCropDays = 1f;

        private float FertilizerBonusSec => YWonderLand.Core.GameTimeConfig.Hours(fertilizerBonusHours);
        private float FertilizerMaxGrowthSec => YWonderLand.Core.GameTimeConfig.Days(fertilizerMaxCropDays);

        private const string MiningLastDateKey = "YW_MiningLastDate";
        private const string MiningTurnsLeftKey = "YW_MiningTurnsLeft";
        private int miningTurnsLeft = -1;
        private float nextMiningLimitToastAt;
        private BuildSurfaceCell hoverEnclosureSeed;     // cache: ô đang rê để khỏi flood-fill mỗi frame
        private List<BuildSurfaceCell> hoverEnclosure;   // cache: kết quả vùng quây của ô đang rê

        void Awake()
        {
            // Tree.prefab still carries a legacy copy of this controller. It must not
            // replace the FarmManager controller that owns timed-action coroutines.
            if (GetComponent<HarvestableResource>() != null)
            {
                enabled = false;
                return;
            }

            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }

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
            EnsureMiningDailyTurns();

            if (AuthService.Instance != null)
                AuthService.Instance.IdentityChanged += HandleIdentityChanged;

            if (mainCamera == null)
                mainCamera = Camera.main;

            // Tìm túi đồ + đăng ký sự kiện "Sử dụng". Nếu lúc này popup chưa sẵn sàng,
            // các chỗ mở túi (cho ăn/gieo/thả thú) sẽ gọi lại helper này để đăng ký cho chắc.
            EnsureInventoryPopupSubscribed();
        }

        // Đảm bảo đã tìm thấy túi đồ VÀ đã đăng ký nghe sự kiện "Sử dụng" (idempotent, không sợ trùng).
        // BẮT BUỘC gọi trước mọi lần mở túi — vì nếu đăng ký lúc Start bị hụt (popup chưa tạo/chưa active)
        // thì bấm "Sử dụng" sẽ không phản hồi (không cho ăn được, thú chết đói).
        private void EnsureInventoryPopupSubscribed()
        {
            if (inventoryPopup == null)
                inventoryPopup = Object.FindFirstObjectByType<InventoryPopupController>();
            if (inventoryPopup != null)
            {
                inventoryPopup.OnItemUsed -= OnInventoryItemSelected; // gỡ trước để tránh đăng ký trùng
                inventoryPopup.OnItemUsed += OnInventoryItemSelected;
            }
        }

        void OnDestroy()
        {
            CancelTimedAction(null);
            // Đổi scene / thoát khi đang dời (chuồng/ruộng/đường): trả về chỗ cũ để không lưu nhầm vị trí.
            if (PenMoveController.IsActive) PenMoveController.Cancel();
            if (Instance == this) Instance = null;

            if (AuthService.Instance != null)
                AuthService.Instance.IdentityChanged -= HandleIdentityChanged;

            if (inventoryPopup != null)
            {
                inventoryPopup.OnItemUsed -= OnInventoryItemSelected;
            }
        }

        private void HandleIdentityChanged(string previousScopeId, string nextScopeId)
        {
            miningTurnsLeft = -1;
            EnsureMiningDailyTurns();
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

            if (RequiresServerResourceSync())
                return HarvestSharedResourceTick(resource, delta);

            string yieldId = resource.yieldItemId;
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            int before = (inv != null && !string.IsNullOrEmpty(yieldId)) ? inv.GetItemQuantity(yieldId) : 0;

            bool done = resource.Harvest(delta);
            if (done)
            {
                bool minedRock = resource.type == HarvestableResource.ResourceType.Rock;
                if (minedRock && !ConsumeMiningTurn())
                {
                    ScreenToast.Show("Hết lượt đào hôm nay rồi! Mai quay lại nhé.");
                    return true;
                }

                int gained = (inv != null && !string.IsNullOrEmpty(yieldId)) ? inv.GetItemQuantity(yieldId) - before : 0;
                GemstoneMiningReward gemstoneReward = null;
                if (minedRock && inv != null)
                {
                    gemstoneReward = RollGemstoneReward();
                    if (gemstoneReward != null)
                    {
                        inv.AddItem(gemstoneReward.itemId, Mathf.Max(1, gemstoneReward.amount));
                    }
                }

                ShowResourceHarvestToast(resource.type, yieldId, gained, gemstoneReward, minedRock ? miningTurnsLeft : -1);
                int rexp = minedRock ? mineExp : resourceExp;
                YWonderLand.Managers.ExperienceManager.Instance?.AddEXP(rexp);
                YWonderLand.Managers.AudioManager.Instance?.PlaySFX("chop");
            }
            return done;
        }

        private bool HarvestSharedResourceTick(HarvestableResource resource, float delta)
        {
            if (resource == null || !resource.isHarvestable) return false;

            resource.currentProgress += Mathf.Max(0f, delta);
            if (resource.currentProgress < Mathf.Max(0.1f, resource.harvestDuration)) return false;

            resource.currentProgress = 0f;
            var realtime = YWonderLand.Realtime.RealtimeClient.Instance;
            bool queued = realtime != null && realtime.TryRequestResourceHarvest(
                resource,
                result => HandleSharedResourceHarvestResult(resource, result));
            if (!queued)
                ScreenToast.Show("Mất kết nối máy chủ. Chưa thể khai thác tài nguyên.");
            return true;
        }

        private void HandleSharedResourceHarvestResult(
            HarvestableResource resource,
            YWonderLand.Realtime.RealtimeClient.ResourceHarvestResult result)
        {
            if (result == null || !result.accepted)
            {
                string code = result != null ? result.code : "RESOURCE_REWARD_FAILED";
                if (code == "RESOURCE_UNAVAILABLE")
                    ScreenToast.Show("Tài nguyên vừa được người chơi khác khai thác.");
                else if (code == "DAILY_LIMIT_EXCEEDED")
                {
                    SetServerMiningTurns(0);
                    // TỰ dùng Vé đào mỏ (nếu có) để đào tiếp — không cần bấm "Sử dụng", giống vé vòng quay.
                    if (IsMiningServerAuthoritative() && !_autoRedeemInFlight && HasMineTicket())
                    {
                        _autoRedeemInFlight = true;
                        AutoRedeemMineTicketThenRetry(resource);
                    }
                    else
                    {
                        ScreenToast.Show("Hết lượt đào hôm nay rồi! Mua Vé đào mỏ để đào thêm nhé.");
                    }
                }
                else if (code == "RESOURCE_TOO_FAR")
                    ScreenToast.Show("Hãy đứng gần tài nguyên hơn để khai thác.");
                else
                    ScreenToast.Show("Mất kết nối máy chủ. Chưa nhận tài nguyên.");
                return;
            }

            // Đào thành công -> mở lại chốt auto-đổi-vé cho lần hết lượt kế tiếp.
            _autoRedeemInFlight = false;

            bool minedRock = resource != null
                ? resource.type == HarvestableResource.ResourceType.Rock
                : result.resourceType == "rock";
            var resourceType = minedRock
                ? HarvestableResource.ResourceType.Rock
                : HarvestableResource.ResourceType.Tree;
            string yieldId = minedRock ? "stone_01" : "wood_01";
            int gained = 0;
            GemstoneMiningReward gemstoneReward = null;

            // Server phát lại kết quả cũ khi cùng người chơi request trùng (retry/tap trùng): túi đồ đã được
            // RealtimeClient áp snapshot authoritative, nhưng EXP là client-side không idempotent. Nếu vẫn xử lý
            // như lần đầu sẽ cộng EXP + toast thưởng ẢO nhiều lần. Với bản phát lại: bỏ EXP/toast, chỉ đồng bộ lượt đào.
            if (result.duplicate)
            {
                if (minedRock && result.miningTurnsRemaining >= 0)
                    SetServerMiningTurns(result.miningTurnsRemaining);
                Debug.Log($"[FarmInteraction] Bỏ qua thưởng trùng (duplicate) cho tài nguyên '{result.resourceId}'.");
                return;
            }

            foreach (var reward in result.rewards)
            {
                if (reward == null || reward.quantity <= 0) continue;
                if (reward.kind == "bonus")
                {
                    gemstoneReward = new GemstoneMiningReward(
                        reward.itemId,
                        reward.displayName,
                        reward.quantity,
                        1f);
                }
                else
                {
                    yieldId = reward.itemId;
                    gained += reward.quantity;
                }
            }

            if (minedRock && result.miningTurnsRemaining >= 0)
                SetServerMiningTurns(result.miningTurnsRemaining);

            ShowResourceHarvestToast(
                resourceType,
                yieldId,
                gained,
                gemstoneReward,
                minedRock ? result.miningTurnsRemaining : -1);
            YWonderLand.Managers.ExperienceManager.Instance?.AddEXP(minedRock ? mineExp : resourceExp);
            YWonderLand.Managers.AudioManager.Instance?.PlaySFX("chop");
        }

        private void SetServerMiningTurns(int remaining)
        {
            miningTurnsLeft = Mathf.Clamp(remaining, 0, Mathf.Max(0, dailyMiningTurns));
            PlayerScopedPrefs.SetString(MiningLastDateKey, System.DateTime.Now.ToString("yyyy-MM-dd"));
            PlayerScopedPrefs.SetInt(MiningTurnsLeftKey, miningTurnsLeft);
            PlayerScopedPrefs.Save();
        }

        private static bool RequiresServerResourceSync()
        {
            var realtime = YWonderLand.Realtime.RealtimeClient.Instance;
            return realtime != null && realtime.RequiresServerResourceSync;
        }

        private static bool IsServerResourceSyncReady(bool showToast)
        {
            var realtime = YWonderLand.Realtime.RealtimeClient.Instance;
            if (realtime == null || !realtime.RequiresServerResourceSync) return true;
            if (realtime.CanUseServerResourceSync) return true;

            if (showToast)
                ScreenToast.Show("Đang kết nối máy chủ. Vui lòng thử lại sau.");
            return false;
        }

        public void OnSharedResourceUnavailable(HarvestableResource resource)
        {
            if (resource == null) return;

            bool wasCurrent = currentHarvestTarget == resource;
            bool wasHeld = _buttonHeldResource == resource;
            if (wasCurrent)
            {
                currentHarvestTarget = null;
                CancelTimedAction(null);
            }
            if (wasHeld) _buttonHeldResource = null;
            if (wasCurrent || wasHeld)
                YWonderLand.UI.ResourceInteractionUIController.Instance?.Hide();
        }

        private GemstoneMiningReward RollGemstoneReward()
        {
            if (gemstoneRewards == null || gemstoneRewards.Count == 0) return null;

            float totalChance = 0f;
            GemstoneMiningReward lastValidReward = null;
            foreach (var reward in gemstoneRewards)
            {
                if (!IsValidGemstoneReward(reward)) continue;
                totalChance += reward.chancePercent;
                lastValidReward = reward;
            }

            if (totalChance <= 0f) return null;

            float roll = Random.Range(0f, totalChance);
            foreach (var reward in gemstoneRewards)
            {
                if (!IsValidGemstoneReward(reward)) continue;

                if (roll < reward.chancePercent) return reward;
                roll -= reward.chancePercent;
            }

            return lastValidReward;
        }

        private static bool IsValidGemstoneReward(GemstoneMiningReward reward)
        {
            return reward != null
                && !string.IsNullOrEmpty(reward.itemId)
                && reward.amount > 0
                && reward.chancePercent > 0f;
        }

        private void ShowResourceHarvestToast(
            HarvestableResource.ResourceType resourceType,
            string yieldId,
            int gained,
            GemstoneMiningReward gemstoneReward,
            int miningTurnsRemaining = -1)
        {
            string verb = resourceType == HarvestableResource.ResourceType.Tree ? "Chặt cây" : "Đào khoáng";
            string miningTurnsSuffix = resourceType == HarvestableResource.ResourceType.Rock && miningTurnsRemaining >= 0
                ? $"(còn {miningTurnsRemaining}/{Mathf.Max(0, dailyMiningTurns)} lượt hôm nay)"
                : null;
            if (resourceType == HarvestableResource.ResourceType.Rock && gemstoneReward != null)
            {
                ItemDefinition gemstoneDef = FoodDb != null ? FoodDb.GetItem(gemstoneReward.itemId) : null;
                string gemstoneName = gemstoneDef != null && !string.IsNullOrEmpty(gemstoneDef.itemName)
                    ? gemstoneDef.itemName
                    : (!string.IsNullOrEmpty(gemstoneReward.displayName) ? gemstoneReward.displayName : gemstoneReward.itemId);
                int gemstoneAmount = Mathf.Max(1, gemstoneReward.amount);
                string rockText = gained > 0 ? $"+{gained} {GetItemDisplayName(yieldId)}, " : string.Empty;

                ScreenToast.ShowInfoForItem(
                    gemstoneReward.itemId,
                    $"Đào trúng: {rockText}+{gemstoneAmount} {gemstoneName} {miningTurnsSuffix}",
                    fallbackText: "Gem");
                Debug.Log($"[Mining] Gem reward: +{gemstoneAmount} {gemstoneReward.itemId}");
                return;
            }

            if (gained > 0)
                ScreenToast.ShowItemReward(yieldId, gained, verb, miningTurnsSuffix);
            else
                ScreenToast.ShowInfo($"{verb} xong!");
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
            if (!timedActionActive && timedActionRoutine == null) return false;
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

            // The controller that starts the coroutine must receive the HUD cancel.
            Instance = this;
            timedActionToken++;
            int token = timedActionToken;
            timedActionActive = true;
            timedActionCancelRefund = onCancelRefund;
            timedActionStartFrame = Time.frameCount;

            // Nhớ mục tiêu để DỰNG LẠI bảng nút sau khi múa xong (anh báo 30/07: tưới/cuốc xong là
            // bảng nút biến mất, phải chạm lại ô mới hiện). Trong lúc múa vẫn ẩn để nhường thanh Hủy.
            promptTargetBeforeTimedAction = currentHoverObject;

            currentHoverObject = null;
            currentActions.Clear();
            currentPromptFromFrontCell = false;
            currentPromptFromFootWater = false;
            currentPromptFromFootResource = false;
            currentPromptFromFootFishing = false;
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

            // THỨ TỰ QUAN TRỌNG (anh báo 31/07: cuốc xong nút vẫn ghi "Cuốc đất", bấm lại thì
            // nhảy sang gieo hạt). onComplete MỚI là chỗ đổi trạng thái ô đất — dựng bảng nút
            // trước nó là dựng theo trạng thái CŨ, nên bảng luôn trễ đúng một bước.
            var promptTarget = promptTargetBeforeTimedAction;
            FinishTimedAction(restorePrompt: false);
            onComplete?.Invoke();

            // onComplete có thể mở màn múa mới (chuỗi thao tác) — lúc đó để màn mới tự lo bảng nút,
            // không bày nút đè lên thanh Hủy.
            if (!timedActionActive) RebuildPromptFor(promptTarget);
        }

        /// <param name="restorePrompt">
        /// Dựng LẠI bảng nút cho đúng thứ vừa thao tác xong. Chỉ đúng khi trạng thái đã đổi xong —
        /// luồng làm-xong tự dựng SAU onComplete nên truyền false.
        /// </param>
        private void FinishTimedAction(bool restorePrompt = true)
        {
            timedActionToken++;
            timedActionRoutine = null;
            timedActionActive = false;
            timedActionCancelRefund = null;
            GameHUDController.Instance?.HideActionCancelProgress();
            EndTimedActionCursorMode();

            var target = promptTargetBeforeTimedAction;
            promptTargetBeforeTimedAction = null;
            if (restorePrompt) RebuildPromptFor(target);
        }

        /// <summary>Dựng lại bảng nút cho đúng vật thể này theo trạng thái HIỆN TẠI của nó.</summary>
        private void RebuildPromptFor(GameObject target)
        {
            if (target == null || !useDirectTapInteraction) return;
            if (PenMoveController.IsActive || UIPopupTracker.AnyOpen) return;
            if (!IsDirectTapTargetStillInRange(target)) return;

            var actions = new List<InteractionAction>();

            var animal = target.GetComponentInParent<FarmAnimal>();
            if (animal != null)
            {
                AddAnimalActions(animal, actions, out _, out _);
            }
            else
            {
                var tile = target.GetComponentInParent<FarmTile>();
                if (tile != null) AddTileAction(tile, actions);
            }

            if (actions.Count == 0) return;

            currentHoverObject = target;
            currentActions = actions;
            lastActionSignature = BuildActionSignature(actions);
            currentPromptFromFrontCell = false;
            currentPromptFromFootWater = false;
            currentPromptFromFootResource = false;
            currentPromptFromFootFishing = false;

            GameHUDController.Instance?.ShowInteractionPrompts(actions);
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
            // Bỏ dở là người chơi CHỦ ĐỘNG thoát -> không dựng lại bảng nút (khác lúc làm xong).
            promptTargetBeforeTimedAction = null;
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

        private float GetDirectTapRange(float configuredRange, float fallbackRange)
        {
            return Mathf.Max(1f, directTapMaxRange);
        }

        private bool IsInDirectTapRangeAtPoint(Vector3 hitPoint, float configuredRange, float fallbackRange) =>
            HorizontalDistanceToPlayer(hitPoint) <= GetDirectTapRange(configuredRange, fallbackRange);

        private bool IsDirectTapObjectInRange(GameObject root, Vector3 fallbackWorldPos, float configuredRange, float fallbackRange)
        {
            return HorizontalDistanceToClosestColliderPoint(root, fallbackWorldPos) <= GetDirectTapRange(configuredRange, fallbackRange);
        }

        private bool IsInInteractRange(Vector3 worldPos) =>
            IsInInteractRange(worldPos, interactRange);

        private float ClampedRange(float targetRange, float fallbackRange) =>
            targetRange > 0f ? Mathf.Min(targetRange, NormalizeRange(fallbackRange)) : NormalizeRange(fallbackRange);

        private float HorizontalDistanceToClosestColliderPoint(GameObject root, Vector3 fallbackWorldPos)
        {
            Vector3 playerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
            float best = HorizontalDistance(playerPos, fallbackWorldPos);
            if (root == null) return best;

            colliderDistanceBuffer.Clear();
            root.GetComponentsInChildren(false, colliderDistanceBuffer);
            foreach (var col in colliderDistanceBuffer)
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

        private float GetResourceActionRange(HarvestableResource resource)
        {
            return useDirectTapInteraction
                ? GetDirectTapRange(resource != null ? resource.interactionRange : resourceInteractRange, resourceInteractRange)
                : GetResourceExecuteRange(resource);
        }

        private float GetResourceDistanceToPlayer(HarvestableResource resource)
        {
            if (resource == null) return float.PositiveInfinity;
            return HorizontalDistanceToClosestColliderPoint(resource.gameObject, resource.transform.position);
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

        private bool TryResolveFarmTileFromAim(Ray ray, out FarmTile tile, bool directTap = false)
        {
            tile = null;
            int hitCount = Physics.SphereCastNonAlloc(ray, FarmTileAimFallbackRadius, tileAimHitResults, 100f, InteractionLayerMask, QueryTriggerInteraction.Collide);
            if (hitCount <= 0) return false;

            System.Array.Sort(tileAimHitResults, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));
            for (int i = 0; i < hitCount; i++)
            {
                var candidate = ResolveFarmTileFromHit(tileAimHitResults[i]);
                if (candidate == null || !IsTileInRange(candidate, directTap)) continue;
                tile = candidate;
                return true;
            }

            return false;
        }

        // Tra công trình "đường" từ 1 hit: trúng chính công trình HOẶC trúng ô đất bên dưới (đọc occupant).
        // Đối xứng với ResolveFarmTileFromHit để đường đá cũng bắt được dù prefab không có collider riêng.
        private GameObject ResolveDemolishablePathFromHit(RaycastHit hit)
        {
            if (hit.collider == null) return null;

            var direct = ResolveDemolishablePath(hit.collider.gameObject);
            if (direct != null) return direct;

            var cell = ResolveBuildSurfaceCellFromHit(hit);
            if (cell != null && cell.Occupant != null)
                return ResolveDemolishablePath(cell.Occupant);

            return null;
        }

        // Fallback dò theo tâm ngắm (sphere-cast) giống ruộng — bắt cả khi ngắm HƠI LỆCH khỏi mặt đường nhỏ.
        private bool TryResolvePathFromAim(Ray ray, out GameObject path, bool directTap = false)
        {
            path = null;
            int hitCount = Physics.SphereCastNonAlloc(ray, FarmTileAimFallbackRadius, tileAimHitResults, 100f, InteractionLayerMask, QueryTriggerInteraction.Collide);
            if (hitCount <= 0) return false;

            System.Array.Sort(tileAimHitResults, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));
            for (int i = 0; i < hitCount; i++)
            {
                var candidate = ResolveDemolishablePathFromHit(tileAimHitResults[i]);
                if (candidate == null || !IsPlacedBuildingInRange(candidate, directTap)) continue;
                path = candidate;
                return true;
            }

            return false;
        }

        private void AddTileAction(FarmTile tile, List<InteractionAction> actions)
        {
            if (tile == null || actions == null) return;

            string actName = tile.masterTile != null ? "\u00d4 thu\u1ed9c gi\u00e0n" : "T\u01b0\u01a1ng t\u00e1c";
            if (tile.masterTile == null)
            {
                switch (tile.currentState)
                {
                    case FarmTile.TileState.Soil: actName = "Cu\u1ed1c \u0111\u1ea5t"; break;
                    case FarmTile.TileState.Plowed: actName = "Gieo h\u1ea1t"; break;
                    case FarmTile.TileState.Planted: actName = "T\u01b0\u1edbi n\u01b0\u1edbc"; break;
                    case FarmTile.TileState.Watered: actName = "T\u01b0\u1edbi n\u01b0\u1edbc"; break;
                    case FarmTile.TileState.Ripe: actName = "Thu ho\u1ea1ch"; break;
                }
            }

            actions.Add(new InteractionAction { keyName = "Click", actionName = actName, onClick = () => PerformTileAction(tile) });

            // Khách chốt 30/07: xem CẢ MẢNH RUỘNG trong một popup (giống "Xem chuồng" của thú),
            // thay cho việc đọc chữ nổi lởm chởm trên đầu từng cây ("nhìn như đám rừng").
            // Giữ nguyên Click = làm việc luôn, không bắt mở popup mới thao tác được.
            FarmTile plotSeed = tile.masterTile != null ? tile.masterTile : tile;
            actions.Add(new InteractionAction
            {
                keyName = "Q",
                actionName = "Xem ruộng",
                onClick = () =>
                {
                    if (AnimalInteractionPopupController.Instance != null)
                        AnimalInteractionPopupController.Instance.ShowPlot(FindPlotTiles(plotSeed));
                }
            });

            // BÓN PHÂN (khách chốt 30/07): chỉ hiện với CÂY NGẮN NGÀY đang lớn — chưa tưới thì chưa
            // có gì để rút, cây đã chín thì bón vô nghĩa, cây dài ngày thì khách không cho bón.
            FarmTile fertilizeTile = tile.masterTile != null ? tile.masterTile : tile;
            if (fertilizeTile != null && fertilizeTile.IsFertilizable(FertilizerMaxGrowthSec))
            {
                actions.Add(new InteractionAction
                {
                    keyName = "B",
                    actionName = "Bón phân",
                    // Bón thẳng rồi dựng lại bảng nút: bón xong cây có thể chín, nút "Bón phân"
                    // phải biến mất chứ không đứng ì đó.
                    onClick = () =>
                    {
                        if (BeginFertilize(fertilizeTile)) RebuildPromptFor(currentHoverObject);
                    }
                });
            }

            // DỜI RUỘNG (khách chốt 30/07): làm y như "Dời chuồng" — nhấc cả mảnh ruộng liền nhau,
            // cây đang trồng đi theo. Chỉ hiện với ruộng dựng bằng Chế độ Xây (có ô nền để đặt lại).
            if (CanDemolishFarmTile(plotSeed))
            {
                actions.Add(new InteractionAction
                {
                    keyName = "M",
                    actionName = "Dời ruộng",
                    onClick = () => { if (IsTileInRange(plotSeed, useDirectTapInteraction)) BeginMovePlot(plotSeed); }
                });
            }

            FarmTile demolishTile = tile.masterTile != null ? tile.masterTile : tile;
            // Khách chốt 29/07: ô ĐÃ TRỒNG cây thì KHÔNG hiện nút hủy — chỉ hủy được ô còn trống.
            if (CanDemolishFarmTile(demolishTile) && !HasCropOnFarmTile(demolishTile))
            {
                actions.Add(new InteractionAction
                {
                    keyName = "G",
                    actionName = "H\u1ee7y \u00f4 tr\u1ed3ng",
                    onClick = () => RequestDemolishFarmTile(demolishTile)
                });
            }
        }

        private bool CanDemolishFarmTile(FarmTile tile)
        {
            return ResolvePlacedBuildingRoot(tile) != null;
        }

        /// <summary>Ô đang có cây (đã gieo / đang lớn / chín) — dùng để KHÔNG cho hủy ô trồng.
        /// Quét mọi FarmTile của cùng công trình để cây nhiều ô (giàn) cũng chặn đúng.</summary>
        private bool HasCropOnFarmTile(FarmTile tile)
        {
            if (tile == null) return false;
            if (IsCropState(tile.currentState)) return true;

            var building = ResolvePlacedBuildingRoot(tile);
            if (building == null) return false;

            var tiles = building.GetComponentsInChildren<FarmTile>(true);
            for (int i = 0; i < tiles.Length; i++)
                if (tiles[i] != null && IsCropState(tiles[i].currentState)) return true;

            return false;
        }

        private static bool IsCropState(FarmTile.TileState state)
        {
            return state == FarmTile.TileState.Planted ||
                   state == FarmTile.TileState.Watered ||
                   state == FarmTile.TileState.Ripe;
        }

        /// <summary>Chuồng còn vật nuôi hay không. Đọc AnimalObject (tham chiếu thật) thay vì cờ HasAnimal
        /// để thú đã chết/bị hủy không làm ô "kẹt" không hủy được.</summary>
        private static bool EnclosureHasAnimal(List<BuildSurfaceCell> pen)
        {
            if (pen == null) return false;
            for (int i = 0; i < pen.Count; i++)
                if (pen[i] != null && pen[i].AnimalObject != null) return true;
            return false;
        }

        private GameObject ResolvePlacedBuildingRoot(FarmTile tile)
        {
            if (tile == null) return null;

            var placed = tile.GetComponentInParent<PlacedBuilding>();
            return placed != null ? placed.gameObject : null;
        }

        // Công trình đặt qua Build Mode nhưng KHÔNG phải ô trồng (FarmTile) và KHÔNG phải hàng rào (chuồng)
        // — hiện chỉ có "Đường đá". Nhóm này chưa có nút hủy riêng nên gom về đây để cho "Hủy đường".
        private GameObject ResolveDemolishablePath(GameObject candidate)
        {
            if (candidate == null) return null;

            var placed = candidate.GetComponentInParent<PlacedBuilding>();
            if (placed == null) return null;

            var go = placed.gameObject;
            if (go.GetComponentInChildren<FarmTile>(true) != null) return null;        // ruộng: đã có "Hủy ô trồng"
            if (go.GetComponentInChildren<FenceAutoConnect>(true) != null) return null; // chuồng: đã có "Hủy chuồng"
            return go;
        }

        private bool IsPlacedBuildingInRange(GameObject building, bool directTap)
        {
            if (building == null) return false;
            float range = directTap
                ? GetDirectTapRange(tileInteractRange, DefaultTileInteractRange)
                : GetTileInteractRange();
            return HorizontalDistanceToClosestColliderPoint(building, building.transform.position) <= range;
        }

        private void AddPathDemolishAction(GameObject building, List<InteractionAction> actions)
        {
            if (building == null || actions == null) return;

            var target = building;

            // DỜI ĐƯỜNG (khách chốt 30/07): nhấc cả đoạn đường liền nhau sang chỗ khác, không tốn đá.
            actions.Add(new InteractionAction
            {
                keyName = "M",
                actionName = "Dời đường",
                onClick = () => { if (IsPlacedBuildingInRange(target, useDirectTapInteraction)) BeginMovePath(target); }
            });

            actions.Add(new InteractionAction
            {
                keyName = "G",
                actionName = "Hủy đường",
                onClick = () => { if (IsPlacedBuildingInRange(target, useDirectTapInteraction)) RequestDemolishPathBuilding(target); }
            });
        }

        private bool IsPenSpawnerInRange(AnimalPenSpawner pen)
        {
            if (pen == null) return false;
            return HorizontalDistanceToClosestColliderPoint(pen.gameObject, pen.transform.position) <= NormalizeGroundRange(enclosureInteractRange);
        }

        private bool IsPenSpawnerInRange(AnimalPenSpawner pen, bool directTap)
        {
            if (!directTap) return IsPenSpawnerInRange(pen);
            if (pen == null) return false;
            float range = GetDirectTapRange(enclosureInteractRange, DefaultGroundInteractRange);
            return HorizontalDistanceToClosestColliderPoint(pen.gameObject, pen.transform.position) <= range;
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

        private bool IsEnclosureInRange(List<BuildSurfaceCell> enclosure, bool directTap)
        {
            if (!directTap) return IsEnclosureInRange(enclosure);
            if (enclosure == null || enclosure.Count == 0) return false;

            float range = GetDirectTapRange(enclosureInteractRange, DefaultGroundInteractRange);
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

        private bool IsTileInRange(FarmTile tile, bool directTap)
        {
            if (!directTap) return IsTileInRange(tile);
            if (tile == null) return false;
            float range = GetDirectTapRange(tileInteractRange, DefaultTileInteractRange);
            return HorizontalDistanceToClosestColliderPoint(tile.gameObject, tile.transform.position) <= range;
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

        private bool IsAnimalInRange(FarmAnimal animal, RaycastHit hit, bool directTap)
        {
            return IsAnimalInRange(animal, directTap);
        }

        private bool IsAnimalInRange(FarmAnimal animal)
        {
            if (animal == null) return false;
            float range = NormalizeRange(animalInteractRange);
            return HorizontalDistanceToClosestColliderPoint(animal.gameObject, animal.transform.position) <= range;
        }

        private bool IsAnimalInRange(FarmAnimal animal, bool directTap)
        {
            if (!directTap) return IsAnimalInRange(animal);
            if (animal == null) return false;
            float range = GetDirectTapRange(animalInteractRange, DefaultGroundInteractRange);
            return HorizontalDistanceToClosestColliderPoint(animal.gameObject, animal.transform.position) <= range;
        }

        private bool IsWaterSourceInRange(WaterSource waterSource)
        {
            if (waterSource == null) return false;
            float range = ClampedRange(waterSource.interactRange, waterInteractRange);
            return HorizontalDistanceToClosestColliderPoint(waterSource.gameObject, waterSource.transform.position) <= range;
        }

        private bool IsWaterSourceInRange(WaterSource waterSource, bool directTap)
        {
            if (!directTap) return IsWaterSourceInRange(waterSource);
            if (waterSource == null) return false;
            float range = GetDirectTapRange(waterSource.interactRange, waterInteractRange);
            return HorizontalDistanceToClosestColliderPoint(waterSource.gameObject, waterSource.transform.position) <= range;
        }

        private float GetFishingRange(FishingSpot spot)
        {
            float configuredRange = spot != null && spot.interactionRange > 0f
                ? spot.interactionRange
                : NormalizeRange(fishingInteractRange);
            return Mathf.Clamp(configuredRange, 0.5f, MaxFishingInteractRange);
        }

        private bool IsFishingSpotInRange(FishingSpot spot, Vector3 hitPoint)
        {
            if (spot == null) return false;
            // Đo tới MÉP nước gần nhất, giống mọi tương tác khác — không đo tới chỗ vừa
            // chạm. Mặt hồ rất rộng: đo tới điểm chạm thì đứng sát bờ mà chạm ra giữa hồ
            // vẫn bị tính là đứng xa.
            return HorizontalDistanceToClosestColliderPoint(spot.gameObject, hitPoint) <= GetFishingRange(spot);
        }

        private bool IsFishingSpotInRange(FishingSpot spot, Vector3 hitPoint, bool directTap)
        {
            // CỐ Ý không đi qua GetDirectTapRange: hàm đó bỏ qua tầm riêng của từng loại và
            // luôn trả directTapMaxRange (3.5m). Câu cá phải giữ đúng 1.5m khách yêu cầu.
            return IsFishingSpotInRange(spot, hitPoint);
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
            // Tiêm phòng: chỉ hiện với loài có thể bệnh, đang KHÔNG bệnh và vắc-xin cũ đã hết hạn.
            else if (animal.currentState != FarmAnimal.AnimalState.Dead
                     && animal.data != null && animal.data.canGetSick
                     && !animal.IsVaccineActive)
                actions.Add(new InteractionAction { keyName = "V", actionName = "Tiêm vắc-xin", onClick = () => VaccinateAnimal(animal) });

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
                    if (IsEnclosureInRange(viewEnclosure, useDirectTapInteraction) && AnimalInteractionPopupController.Instance != null)
                        AnimalInteractionPopupController.Instance.ShowEnclosure(viewEnclosure);
                }
            });

            if (PenEnclosure.AvailableCount(enclosure) > 0)
            {
                var addEnclosure = new List<BuildSurfaceCell>(enclosure);
                actions.Add(new InteractionAction { keyName = "E", actionName = "Thả thú", onClick = () => { if (IsEnclosureInRange(addEnclosure, useDirectTapInteraction)) OpenEnclosurePicker(addEnclosure); } });
            }

            // Khách chốt 29/07: cho DỜI nguyên cụm chuồng sang chỗ khác (thú đi theo, không tốn vật liệu).
            var moveEnclosure = new List<BuildSurfaceCell>(enclosure);
            actions.Add(new InteractionAction
            {
                keyName = "M",
                actionName = "Dời chuồng",
                onClick = () =>
                {
                    if (!IsEnclosureInRange(moveEnclosure, useDirectTapInteraction)) return;
                    BeginMovePen(moveEnclosure);
                }
            });

            // Khách chốt 29/07: chuồng CÒN THÚ thì KHÔNG hiện nút hủy — chỉ hủy được chuồng trống.
            if (!EnclosureHasAnimal(enclosure))
            {
                var demolishEnclosure = new List<BuildSurfaceCell>(enclosure);
                actions.Add(new InteractionAction { keyName = "G", actionName = "Hủy chuồng", onClick = () => { if (IsEnclosureInRange(demolishEnclosure, useDirectTapInteraction)) RequestDemolishEnclosure(demolishEnclosure); } });
            }
        }

        private bool TryShowAnimalEnclosurePopup(FarmAnimal animal)
        {
            if (!TryGetAnimalEnclosure(animal, out var enclosure)) return false;
            if (!IsEnclosureInRange(enclosure, useDirectTapInteraction)) return false;

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

            ClearPendingItemPickIfBagClosed();

            if (IsBuildModeOpen())
            {
                // Mở Build Mode giữa chừng thì bỏ dở việc dời, trả cụm về chỗ cũ.
                if (PenMoveController.IsActive) PenMoveController.Cancel();
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
            bool pointerPressed = pointer.press.wasPressedThisFrame;
            bool pointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            ClearDirectTapPromptIfOutOfRange();

            // Khi chuột đang khóa/tâm ngắm hoặc mobile tap ngoài UI, click/tap phải kích hoạt action "Click"
            // đang hiện dưới tâm. Nếu chuột đã được nhả và đang bấm trực tiếp lên UI button thì để UI tự xử lý.
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
                if (!timedActionActive && useDirectTapInteraction)
                    RefreshFacingInteractionPrompts();
                return;
            }

            // Đang DỜI CHUỒNG: chỉ dùng 2 nút Đặt/Hủy dời, không cho chạm vật khác trong thế giới
            // (kẻo prompt bị thay và người chơi kẹt trong chế độ dời).
            if (PenMoveController.IsActive)
            {
                RefreshFacingInteractionPrompts();
                return;
            }

            if (pointerPressed && !timedActionActive)
            {
                Vector2 tapPos = pointer.position.ReadValue();
                if (useDirectTapInteraction)
                {
                    HandleHover(tapPos, true);
                    return;
                }

                Vector2 aimPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                HandleHover(aimPos);
                if (TryInvokeCurrentClickAction())
                    return;
                HandleClick(aimPos);
            }
            if (timedActionActive) return;

            if (useDirectTapInteraction)
            {
                RefreshFacingInteractionPrompts();
            }
            else
            {
                Vector2 aimPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                HandleHover(aimPos);
                if (pointer.press.isPressed) HandleHold(aimPos);
            }

            if (pointer.press.wasReleasedThisFrame)
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
                    pendingDemolishTile = null;
                    pendingDemolishPath = null;
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
            promptTargetBeforeTimedAction = null;
            currentActions.Clear();
            currentPromptFromFrontCell = false;
            currentPromptFromFootWater = false;
            currentPromptFromFootResource = false;
            currentPromptFromFootFishing = false;
            pendingDemolishEnclosure = null;
            pendingDemolishTile = null;
            pendingDemolishPath = null;
            demolishConfirmTimer = 0f;

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

        private void ClearDirectTapPromptIfOutOfRange()
        {
            if (!useDirectTapInteraction || currentHoverObject == null || currentActions == null || currentActions.Count == 0)
                return;

            if (!IsDirectTapTargetStillInRange(currentHoverObject))
                ClearWorldInteractionState();
        }

        private void RefreshFacingInteractionPrompts()
        {
            // Đang dời: chỉ hiện đúng 2 lựa chọn Đặt / Hủy dời, không cho tương tác thứ khác.
            if (PenMoveController.IsActive)
            {
                RefreshPenMovePrompt();
                return;
            }

            RefreshFrontCellInteractionPrompt();
            if (currentPromptFromFrontCell)
            {
                currentPromptFromFootWater = false;
                currentPromptFromFootResource = false;
                currentPromptFromFootFishing = false;
                return;
            }

            RefreshFootFishingInteractionPrompt();
            if (currentPromptFromFootFishing)
            {
                currentPromptFromFootWater = false;
                currentPromptFromFootResource = false;
                return;
            }

            RefreshFootWaterInteractionPrompt();
            if (currentPromptFromFootWater)
            {
                currentPromptFromFootResource = false;
                currentPromptFromFootFishing = false;
                return;
            }

            RefreshFootResourceInteractionPrompt();
        }

        private bool HasDirectTapPrompt()
        {
            return !currentPromptFromFrontCell &&
                   !currentPromptFromFootWater &&
                   !currentPromptFromFootResource &&
                   !currentPromptFromFootFishing &&
                   currentHoverObject != null &&
                   currentActions != null &&
                   currentActions.Count > 0;
        }

        /// <summary>Gợi ý khi đang DỜI (chuồng / ruộng / đường): rê cả cụm theo bước chân, hiện nút Đặt / Hủy dời.</summary>
        private void RefreshPenMovePrompt()
        {
            // Cụm bám theo bước chân người chơi; nút Đặt luôn chốt ĐÚNG vị trí đang xem trước
            // (không giữ ô cũ trong closure — trước đây HUD không dựng lại nên đặt nhầm về chỗ ban đầu).
            PenMoveController.UpdatePreview();
            bool canPlace = PenMoveController.CanPlace();
            string subject = PenMoveController.SubjectLabel;

            var actions = new List<InteractionAction>();
            if (canPlace)
            {
                actions.Add(new InteractionAction
                {
                    keyName = "Click",
                    actionName = $"Đặt {subject} ở đây",
                    onClick = () =>
                    {
                        if (PenMoveController.Confirm()) ScreenToast.ShowInfo($"Đã dời {subject} sang chỗ mới.");
                        else ScreenToast.Show($"Chỗ này không đặt được {subject}.");
                        ClearWorldInteractionState();
                    }
                });
            }
            else
            {
                actions.Add(new InteractionAction
                {
                    keyName = "Click",
                    actionName = "Chưa đặt được — cần đủ ô trống",
                    onClick = () => ScreenToast.Show($"Cần đủ ô đất trống bằng số ô của {subject}.")
                });
            }

            actions.Add(new InteractionAction
            {
                keyName = "G",
                actionName = "Hủy dời",
                onClick = () =>
                {
                    PenMoveController.Cancel();
                    ScreenToast.Show($"Đã hủy dời — {subject} về chỗ cũ.");
                    ClearWorldInteractionState();
                }
            });

            string signature = BuildActionSignature(actions);
            bool shouldRefreshPrompt = signature != lastActionSignature || currentActions == null || currentActions.Count == 0;

            var frontCell = FrontBuildCellSelector.Instance != null ? FrontBuildCellSelector.Instance.CurrentCell : null;
            currentHoverObject = frontCell != null ? frontCell.gameObject : null;
            lastActionSignature = signature;
            currentActions = actions;
            currentPromptFromFrontCell = true;
            currentPromptFromFootWater = false;
            currentPromptFromFootResource = false;
            currentPromptFromFootFishing = false;

            if (shouldRefreshPrompt && GameHUDController.Instance != null)
                GameHUDController.Instance.ShowInteractionPrompts(actions);
        }

        private void RefreshFrontCellInteractionPrompt()
        {
            if (!useDirectTapInteraction || timedActionActive)
                return;

            // A direct-tapped tree/rock/NPC prompt should stay until it leaves range.
            if (HasDirectTapPrompt())
                return;

            var selector = FrontBuildCellSelector.Instance;
            var cell = selector != null ? selector.CurrentCell : null;
            if (cell == null)
            {
                ClearFrontCellInteractionPrompt();
                return;
            }

            var foundActions = new List<InteractionAction>();
            GameObject foundObj = null;

            if (!TryBuildFrontCellEnclosurePrompt(cell, foundActions, out foundObj) &&
                !TryBuildFrontCellTilePrompt(cell, foundActions, out foundObj) &&
                !TryBuildFrontCellPathPrompt(cell, foundActions, out foundObj))
            {
                ClearFrontCellInteractionPrompt();
                return;
            }

            if (foundActions.Count == 0 || foundObj == null)
            {
                ClearFrontCellInteractionPrompt();
                return;
            }

            string actionSignature = BuildActionSignature(foundActions);
            bool hadNoCurrentActions = currentActions == null || currentActions.Count == 0;
            bool shouldRefreshPrompt =
                hadNoCurrentActions ||
                !currentPromptFromFrontCell ||
                foundObj != currentHoverObject ||
                actionSignature != lastActionSignature;

            currentHoverObject = foundObj;
            lastAnimalState = FarmAnimal.AnimalState.Healthy;
            lastAnimalProductReady = false;
            lastActionSignature = actionSignature;
            currentActions = foundActions;
            currentPromptFromFrontCell = true;
            currentPromptFromFootWater = false;
            currentPromptFromFootResource = false;
            currentPromptFromFootFishing = false;

            if (shouldRefreshPrompt && GameHUDController.Instance != null)
                GameHUDController.Instance.ShowInteractionPrompts(foundActions);
        }

        private bool TryBuildFrontCellEnclosurePrompt(BuildSurfaceCell cell, List<InteractionAction> actions, out GameObject foundObj)
        {
            foundObj = null;
            if (cell == null || !cell.HasFence)
                return false;

            if (cell != hoverEnclosureSeed || hoverEnclosure == null)
            {
                hoverEnclosureSeed = cell;
                hoverEnclosure = PenEnclosure.FindPen(cell);
            }

            if (hoverEnclosure == null || !IsEnclosureInRange(hoverEnclosure, true))
                return false;

            foundObj = cell.gameObject;
            AddEnclosureActions(hoverEnclosure, actions);
            return actions != null && actions.Count > 0;
        }

        private bool TryBuildFrontCellTilePrompt(BuildSurfaceCell cell, List<InteractionAction> actions, out GameObject foundObj)
        {
            foundObj = null;
            FarmTile tile = ResolveFarmTileFromCell(cell);
            if (tile == null || !IsTileInRange(tile, true))
                return false;

            foundObj = tile.gameObject;
            AddTileAction(tile, actions);
            return actions != null && actions.Count > 0;
        }

        // Ghost đè lên ô chứa ĐƯỜNG ĐÁ (occupant không phải ruộng/chuồng) → hiện nút "Hủy đường".
        // Nhờ vậy đường đá có UI khi ghost chạm vào, đồng nhất với ruộng/chuồng (không cần click chuột).
        private bool TryBuildFrontCellPathPrompt(BuildSurfaceCell cell, List<InteractionAction> actions, out GameObject foundObj)
        {
            foundObj = null;
            if (cell == null || cell.Occupant == null)
                return false;

            GameObject path = ResolveDemolishablePath(cell.Occupant);
            if (path == null || !IsPlacedBuildingInRange(path, true))
                return false;

            foundObj = path;
            AddPathDemolishAction(path, actions);
            return actions != null && actions.Count > 0;
        }

        private void RefreshFootWaterInteractionPrompt()
        {
            if (!useDirectTapInteraction || timedActionActive)
                return;

            if (HasDirectTapPrompt() || currentPromptFromFrontCell)
                return;

            WaterSource waterSource = FindWaterSourceNearFoot();
            if (waterSource == null)
            {
                ClearFootWaterInteractionPrompt();
                return;
            }

            var foundActions = new List<InteractionAction>();
            AddWaterSourceAction(waterSource, foundActions);

            string actionSignature = BuildActionSignature(foundActions);
            bool hadNoCurrentActions = currentActions == null || currentActions.Count == 0;
            bool shouldRefreshPrompt =
                hadNoCurrentActions ||
                !currentPromptFromFootWater ||
                waterSource.gameObject != currentHoverObject ||
                actionSignature != lastActionSignature;

            currentHoverObject = waterSource.gameObject;
            lastAnimalState = FarmAnimal.AnimalState.Healthy;
            lastAnimalProductReady = false;
            lastActionSignature = actionSignature;
            currentActions = foundActions;
            currentPromptFromFrontCell = false;
            currentPromptFromFootWater = true;
            currentPromptFromFootResource = false;
            currentPromptFromFootFishing = false;

            if (shouldRefreshPrompt && GameHUDController.Instance != null)
                GameHUDController.Instance.ShowInteractionPrompts(foundActions);
        }

        private void AddWaterSourceAction(WaterSource waterSource, List<InteractionAction> actions)
        {
            if (waterSource == null || actions == null) return;

            var ws = waterSource;
            actions.Add(new InteractionAction { keyName = "Click", actionName = "M\u00fac n\u01b0\u1edbc", onClick = () => ScoopWater(ws) });
        }

        private FarmTile ResolveFarmTileFromCell(BuildSurfaceCell cell)
        {
            if (cell == null) return null;

            FarmTile tile = cell.GetComponent<FarmTile>();
            if (tile != null) return tile;

            tile = cell.GetComponentInParent<FarmTile>();
            if (tile != null) return tile;

            tile = cell.GetComponentInChildren<FarmTile>();
            if (tile != null) return tile;

            if (cell.Occupant == null) return null;

            tile = cell.Occupant.GetComponent<FarmTile>();
            if (tile != null) return tile;

            return cell.Occupant.GetComponentInChildren<FarmTile>();
        }

        private WaterSource FindWaterSourceNearFoot()
        {
            Transform player = PlayerController.Instance != null ? PlayerController.Instance.transform : transform;
            Vector3 forward = player.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = transform.forward;
            forward.Normalize();

            Vector3 center = player.position + forward * Mathf.Max(0.1f, waterFootProbeForward) + Vector3.up * 0.2f;
            float radius = Mathf.Max(0.2f, waterFootProbeRadius);

            int hitCount = Physics.OverlapSphereNonAlloc(
                center,
                radius,
                frontCellOverlapResults,
                InteractionLayerMask,
                QueryTriggerInteraction.Collide);

            WaterSource best = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = frontCellOverlapResults[i];
                if (col == null) continue;

                WaterSource water = col.GetComponentInParent<WaterSource>();
                if (water == null || !IsWaterSourceInRange(water, true))
                    continue;

                float distance = HorizontalDistance(center, SafeClosestPoint(col, center));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = water;
                }
            }

            return best;
        }

        // Đến GẦN mép nước câu được là tự hiện nút Câu cá, y như Múc nước / Chặt cây.
        // Đặt TRƯỚC nhánh múc nước: hồ câu ở thành phố có thể trùng luôn vùng WaterSource,
        // để sau thì "Múc nước" giành mất nút và không bao giờ câu được. Nhánh này tự tắt
        // ở nông trại vì IsFishingAllowedHere() chỉ đúng ở thành phố.
        private void RefreshFootFishingInteractionPrompt()
        {
            if (!useDirectTapInteraction || timedActionActive)
                return;

            if (HasDirectTapPrompt() || currentPromptFromFrontCell)
                return;

            FishingSpot spot = IsFishingAllowedHere() ? FindFishingSpotNearFoot() : null;
            if (spot == null)
            {
                ClearFootFishingInteractionPrompt();
                return;
            }

            var foundActions = new List<InteractionAction>();
            var target = spot;
            Vector3 castPoint = ClosestFishingPoint(spot);
            foundActions.Add(new InteractionAction
            {
                keyName = "F",
                actionName = "Câu cá",
                onClick = () => StartFishing(target, castPoint)
            });

            string actionSignature = BuildActionSignature(foundActions);
            bool hadNoCurrentActions = currentActions == null || currentActions.Count == 0;
            bool shouldRefreshPrompt =
                hadNoCurrentActions ||
                !currentPromptFromFootFishing ||
                spot.gameObject != currentHoverObject ||
                actionSignature != lastActionSignature;

            currentHoverObject = spot.gameObject;
            lastAnimalState = FarmAnimal.AnimalState.Healthy;
            lastAnimalProductReady = false;
            lastActionSignature = actionSignature;
            currentActions = foundActions;
            currentPromptFromFrontCell = false;
            currentPromptFromFootWater = false;
            currentPromptFromFootResource = false;
            currentPromptFromFootFishing = true;

            if (shouldRefreshPrompt && GameHUDController.Instance != null)
                GameHUDController.Instance.ShowInteractionPrompts(foundActions);
        }

        /// <summary>Điểm trên mặt nước gần người chơi nhất — chỗ để quăng cần.</summary>
        private Vector3 ClosestFishingPoint(FishingSpot spot)
        {
            if (spot == null) return transform.position;
            Vector3 playerPos = PlayerController.Instance != null
                ? PlayerController.Instance.transform.position
                : transform.position;

            Vector3 best = spot.transform.position;
            float bestDistance = HorizontalDistance(playerPos, best);

            colliderDistanceBuffer.Clear();
            spot.gameObject.GetComponentsInChildren(false, colliderDistanceBuffer);
            foreach (var col in colliderDistanceBuffer)
            {
                if (col == null || !col.enabled) continue;
                Vector3 point = SafeClosestPoint(col, playerPos);
                float distance = HorizontalDistance(playerPos, point);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = point;
                }
            }
            colliderDistanceBuffer.Clear();
            return best;
        }

        private FishingSpot FindFishingSpotNearFoot()
        {
            Transform player = PlayerController.Instance != null ? PlayerController.Instance.transform : transform;
            Vector3 forward = player.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = transform.forward;
            forward.Normalize();

            Vector3 center = player.position + forward * Mathf.Max(0.1f, fishingFootProbeForward) + Vector3.up * 0.2f;
            float radius = Mathf.Max(0.2f, fishingFootProbeRadius);

            int hitCount = Physics.OverlapSphereNonAlloc(
                center,
                radius,
                frontCellOverlapResults,
                InteractionLayerMask,
                QueryTriggerInteraction.Collide);

            FishingSpot best = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = frontCellOverlapResults[i];
                if (col == null) continue;

                FishingSpot spot = col.GetComponentInParent<FishingSpot>();
                if (spot == null || !IsFishingSpotInRange(spot, spot.transform.position))
                    continue;

                float distance = HorizontalDistance(center, SafeClosestPoint(col, center));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = spot;
                }
            }

            return best;
        }

        private void ClearFootFishingInteractionPrompt()
        {
            if (!currentPromptFromFootFishing)
                return;

            currentHoverObject = null;
            lastActionSignature = "";
            currentActions.Clear();
            currentPromptFromFootFishing = false;
            if (GameHUDController.Instance != null)
                GameHUDController.Instance.HideInteractionPrompt();
        }

        private void ClearFootWaterInteractionPrompt()
        {
            if (!currentPromptFromFootWater)
                return;

            currentHoverObject = null;
            lastActionSignature = "";
            currentActions.Clear();
            currentPromptFromFootWater = false;
            if (GameHUDController.Instance != null)
                GameHUDController.Instance.HideInteractionPrompt();
        }

        private void ClearFrontCellInteractionPrompt()
        {
            if (!currentPromptFromFrontCell)
                return;

            currentHoverObject = null;
            lastActionSignature = "";
            currentActions.Clear();
            currentPromptFromFrontCell = false;
            if (GameHUDController.Instance != null)
                GameHUDController.Instance.HideInteractionPrompt();
        }

        // Đến GẦN cây/đá là tự hiện nút Chặt cây / Đào khoáng — tia bắn từ mũi chân (tàng hình, không cần ngắm tâm),
        // y như nút múc nước. Ưu tiên đứng sau ruộng/chuồng và ao nước để không tranh nút với chúng.
        private void RefreshFootResourceInteractionPrompt()
        {
            if (!useDirectTapInteraction || timedActionActive)
                return;

            if (HasDirectTapPrompt() || currentPromptFromFrontCell || currentPromptFromFootWater)
                return;

            HarvestableResource resource = FindHarvestableNearFoot();
            if (resource == null)
            {
                ClearFootResourceInteractionPrompt();
                return;
            }

            var foundActions = new List<InteractionAction>();
            AddFootResourceAction(resource, foundActions);

            string actionSignature = BuildActionSignature(foundActions);
            bool hadNoCurrentActions = currentActions == null || currentActions.Count == 0;
            bool shouldRefreshPrompt =
                hadNoCurrentActions ||
                !currentPromptFromFootResource ||
                resource.gameObject != currentHoverObject ||
                actionSignature != lastActionSignature;

            currentHoverObject = resource.gameObject;
            lastAnimalState = FarmAnimal.AnimalState.Healthy;
            lastAnimalProductReady = false;
            lastActionSignature = actionSignature;
            currentActions = foundActions;
            currentPromptFromFrontCell = false;
            currentPromptFromFootWater = false;
            currentPromptFromFootResource = true;
            currentPromptFromFootFishing = false;

            if (shouldRefreshPrompt && GameHUDController.Instance != null)
                GameHUDController.Instance.ShowInteractionPrompts(foundActions);
        }

        private void AddFootResourceAction(HarvestableResource resource, List<InteractionAction> actions)
        {
            if (resource == null || actions == null) return;

            var res = resource;
            string actionStr = resource.type == HarvestableResource.ResourceType.Tree ? "Chặt cây" : "Đào khoáng";
            actions.Add(new InteractionAction { keyName = "Click", actionName = actionStr, onClick = () => ClickHarvestResource(res) });
        }

        // Quét OverlapSphere ngay trước mũi chân, trả cây/đá GẦN NHẤT còn khai thác được và trong tầm hành động.
        // Đá chỉ tính ở nơi cho đào (City/Mine). Không hiện viền trắng — probe tàng hình như nước.
        private HarvestableResource FindHarvestableNearFoot()
        {
            Transform player = PlayerController.Instance != null ? PlayerController.Instance.transform : transform;
            Vector3 forward = player.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = transform.forward;
            forward.Normalize();

            Vector3 center = player.position + forward * Mathf.Max(0.1f, resourceFootProbeForward) + Vector3.up * 0.2f;
            float radius = Mathf.Max(0.2f, resourceFootProbeRadius);

            int hitCount = Physics.OverlapSphereNonAlloc(
                center,
                radius,
                frontCellOverlapResults,
                InteractionLayerMask,
                QueryTriggerInteraction.Collide);

            HarvestableResource best = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = frontCellOverlapResults[i];
                if (col == null) continue;

                HarvestableResource res = col.GetComponentInParent<HarvestableResource>();
                if (res == null || !res.isHarvestable)
                    continue;
                if (res.type == HarvestableResource.ResourceType.Rock && !IsMiningAllowedHere())
                    continue;
                if (GetResourceDistanceToPlayer(res) > GetResourceActionRange(res))
                    continue;

                float distance = HorizontalDistance(center, SafeClosestPoint(col, center));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = res;
                }
            }

            return best;
        }

        private void ClearFootResourceInteractionPrompt()
        {
            if (!currentPromptFromFootResource)
                return;

            currentHoverObject = null;
            lastActionSignature = "";
            currentActions.Clear();
            currentPromptFromFootResource = false;
            currentPromptFromFootFishing = false;
            if (GameHUDController.Instance != null)
                GameHUDController.Instance.HideInteractionPrompt();
        }

        private bool IsDirectTapTargetStillInRange(GameObject target)
        {
            if (target == null) return false;

            var resource = target.GetComponentInParent<HarvestableResource>();
            if (resource != null)
                return GetResourceDistanceToPlayer(resource) <= GetDirectTapRange(resource.interactionRange, resourceInteractRange);

            var animal = target.GetComponentInParent<FarmAnimal>();
            if (animal != null)
                return IsAnimalInRange(animal, true);

            var waterSource = target.GetComponentInParent<WaterSource>();
            if (waterSource != null)
                return IsWaterSourceInRange(waterSource, true);

            var fishingSpot = target.GetComponentInParent<FishingSpot>();
            if (fishingSpot != null)
                // Dùng thẳng tầm câu cá (1.5m), không dùng tầm chạm chung 3.5m.
                return IsFishingSpotInRange(fishingSpot, fishingSpot.transform.position);

            var merchant = target.GetComponentInParent<MerchantNPC>();
            if (merchant != null)
                return HorizontalDistanceToClosestColliderPoint(merchant.gameObject, merchant.transform.position) <= GetDirectTapRange(merchantInteractRange, merchantInteractRange);

            var penSpawner = target.GetComponentInParent<AnimalPenSpawner>();
            if (penSpawner != null)
                return IsPenSpawnerInRange(penSpawner, true);

            var tile = target.GetComponentInParent<FarmTile>();
            if (tile != null)
                return IsTileInRange(tile, true);

            var cell = target.GetComponentInParent<BuildSurfaceCell>();
            if (cell != null)
            {
                if (cell.HasFence)
                {
                    var enclosure = PenEnclosure.FindPen(cell);
                    return IsEnclosureInRange(enclosure, true);
                }

                return HorizontalDistanceToClosestColliderPoint(cell.gameObject, cell.SurfaceCenter) <= GetDirectTapRange(tileInteractRange, DefaultTileInteractRange);
            }

            return HorizontalDistanceToClosestColliderPoint(target, target.transform.position) <= GetDirectTapRange(interactRange, interactRange);
        }

        private RaycastHit[] hoverHitResults = new RaycastHit[64];
        private RaycastHit[] directTapAssistHitResults = new RaycastHit[48];
        private RaycastHit[] tileAimHitResults = new RaycastHit[32];
        private Collider[] frontCellOverlapResults = new Collider[32];
        private readonly List<Collider> colliderDistanceBuffer = new List<Collider>(16);
        private GameObject currentHoverObject = null;
        /// <summary>Thứ đang chỉ ngay trước khi vào màn múa động tác — để dựng lại bảng nút khi múa xong.</summary>
        private GameObject promptTargetBeforeTimedAction;
        private bool currentPromptFromFrontCell;
        private bool currentPromptFromFootWater;
        private bool currentPromptFromFootResource;
        private bool currentPromptFromFootFishing;
        private FarmAnimal.AnimalState lastAnimalState;
        private bool lastAnimalProductReady;
        private string lastActionSignature = "";
        private List<InteractionAction> currentActions = new List<InteractionAction>();

        private int CollectInteractionHits(Ray ray, bool directTap)
        {
            int hitCount = Physics.RaycastNonAlloc(ray, hoverHitResults, 100f, InteractionLayerMask, QueryTriggerInteraction.Collide);

            if (directTap && directTapAssistWorldRadius > 0f)
            {
                int assistCount = Physics.SphereCastNonAlloc(
                    ray,
                    directTapAssistWorldRadius,
                    directTapAssistHitResults,
                    100f,
                    InteractionLayerMask,
                    QueryTriggerInteraction.Collide);

                for (int i = 0; i < assistCount && hitCount < hoverHitResults.Length; i++)
                {
                    var assistHit = directTapAssistHitResults[i];
                    if (assistHit.collider == null) continue;

                    bool duplicate = false;
                    for (int j = 0; j < hitCount; j++)
                    {
                        if (hoverHitResults[j].collider == assistHit.collider)
                        {
                            duplicate = true;
                            break;
                        }
                    }

                    if (!duplicate)
                        hoverHitResults[hitCount++] = assistHit;
                }
            }

            System.Array.Sort(hoverHitResults, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));
            return hitCount;
        }

        private void HandleHover(Vector2 screenPos, bool directTap = false)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            int hitCount = CollectInteractionHits(ray, directTap);
            Vector3 playerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;
            float solidPassthroughLimit = float.PositiveInfinity;
            float passthroughTolerance = directTap
                ? DirectTapSurfaceTolerance
                : SolidHitPassthroughTolerance;

            List<InteractionAction> foundActions = new List<InteractionAction>();
            GameObject foundObj = null;
            FarmAnimal.AnimalState currentAnimalState = FarmAnimal.AnimalState.Healthy;
            bool currentHasProduct = false;

            var priorityAnimal = directTap ? null : FindPriorityAnimalTarget(hoverHitResults, hitCount);
            if (priorityAnimal != null)
            {
                if (TryGetAnimalEnclosure(priorityAnimal, out var priorityEnclosure) && IsEnclosureInRange(priorityEnclosure, directTap))
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
                    if (!IsAnimalInRange(animal, hit, directTap))
                        continue;

                    if (TryGetAnimalEnclosure(animal, out var animalEnclosure) && IsEnclosureInRange(animalEnclosure, directTap))
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
                    float resourceRange = directTap
                        ? GetDirectTapRange(resource.interactionRange, resourceInteractRange)
                        : ClampedRange(resource.interactionRange, resourceInteractRange);
                    float resourceDistance = directTap
                        ? GetResourceDistanceToPlayer(resource)
                        : HorizontalDistance(playerPos, hit.point);
                    if (resourceDistance > resourceRange)
                        continue;

                    // Chỉ hiện nút khi TÂM NGẮM chạm bề mặt cây/đá trong tầm với (resource.interactionRange).
                    // Đo từ nhân vật tới ĐIỂM CHẠM (hit.point) cho trực quan, đúng cả với cây to.
                    // Đào đá chỉ hiện ở City hoặc Mine; chặt cây vẫn hiện ở nơi có tài nguyên.
                    bool resourceUsable = resource.type != HarvestableResource.ResourceType.Rock || IsMiningAllowedHere();
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
                    if (directTap
                        ? !IsDirectTapObjectInRange(merchant.gameObject, hit.point, merchantInteractRange, merchantInteractRange)
                        : !IsInInteractRangeAtPoint(hit.point, merchantInteractRange))
                        continue;

                    foundObj = merchant.gameObject;
                    foundActions.Add(new InteractionAction { keyName = "Click", actionName = merchant.GetInteractionLabel(), onClick = () => merchant.Interact() });
                    break;
                }
                else if (ResolveBuildSurfaceCellFromHit(hit) is BuildSurfaceCell directPenCell && directPenCell != null && directPenCell.HasFence)
                {
                    if (directPenCell != hoverEnclosureSeed)
                    {
                        hoverEnclosureSeed = directPenCell;
                        hoverEnclosure = PenEnclosure.FindPen(directPenCell);
                    }
                    if (hoverEnclosure != null)
                    {
                        var encl = hoverEnclosure;
                        if (IsEnclosureInRange(encl, directTap))
                        {
                            foundObj = directPenCell.gameObject;
                            AddEnclosureActions(encl, foundActions);
                        }
                    }
                    break;
                }
                else if (hit.collider.GetComponentInParent<YWonderLand.Environment.AnimalPenSpawner>() != null)
                {
                    // Legacy static pens have no BuildSurfaceCell snapshot. Keep this fallback
                    // after the authoritative enclosure path so placed fence prefabs cannot
                    // bypass the atomic inventory + farm transaction.
                    var penS = hit.collider.GetComponentInParent<YWonderLand.Environment.AnimalPenSpawner>();
                    if (penS.HasSpace && IsPenSpawnerInRange(penS, directTap))
                    {
                        foundObj = penS.gameObject;
                        foundActions.Add(new InteractionAction { keyName = "Click", actionName = "Thả thú", onClick = () => { if (IsPenSpawnerInRange(penS, useDirectTapInteraction)) OpenPenAnimalPicker(penS); } });
                    }
                    break;
                }
                else if (ResolveFarmTileFromHit(hit) is FarmTile tile && tile != null)
                {
                    if (!IsTileInRange(tile, directTap))
                        continue;

                    foundObj = tile.gameObject;
                    AddTileAction(tile, foundActions);
                    break;
                }
                else if (ResolveDemolishablePath(hit.collider.gameObject) is GameObject pathBuilding && pathBuilding != null)
                {
                    // Đường đá (hoặc trang trí khác) — ngắm trúng collider của chính công trình.
                    if (!IsPlacedBuildingInRange(pathBuilding, directTap))
                        continue;

                    foundObj = pathBuilding;
                    AddPathDemolishAction(pathBuilding, foundActions);
                    break;
                }
                else if (hit.collider.TryGetComponent<FishingSpot>(out var spot) || (hit.collider.transform.parent != null && hit.collider.transform.parent.TryGetComponent<FishingSpot>(out spot)))
                {
                    Vector3 fishingHitPoint = hit.point;
                    if (IsFishingSpotInRange(spot, fishingHitPoint, directTap) && IsFishingAllowedHere())
                    {
                        foundObj = spot.gameObject;
                        foundActions.Add(new InteractionAction { keyName = "F", actionName = "Câu cá", onClick = () => StartFishing(spot, fishingHitPoint) });
                    }
                    break;
                }
                else if (hit.collider.GetComponentInParent<WaterSource>() is WaterSource waterSrc && waterSrc != null)
                {
                    // Vùng ao MÚC ĐƯỢC (không phải nước biển) → bấm "Múc nước" lấy xô nước về túi.
                    if (IsWaterSourceInRange(waterSrc, directTap))
                    {
                        foundObj = waterSrc.gameObject;
                        AddWaterSourceAction(waterSrc, foundActions);
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
                            if (IsEnclosureInRange(encl, directTap))
                            {
                                foundObj = penCell.gameObject;
                                AddEnclosureActions(encl, foundActions);
                            }
                        }
                        break;
                    }
                    if (!penCell.IsFree)
                    {
                        // Ô bị chiếm bởi đường đá (collider mỏng/không có nên tia trúng ô bên dưới) -> cho "Hủy đường".
                        var occPath = ResolveDemolishablePath(penCell.Occupant);
                        if (occPath != null && IsPlacedBuildingInRange(occPath, directTap))
                        {
                            foundObj = occPath;
                            AddPathDemolishAction(occPath, foundActions);
                            break;
                        }
                        continue; // công trình khác -> xuyên qua
                    }
                    break; // ô đất trống thường (không phải chuồng) -> bỏ
                }
                else if (hit.collider.GetComponentInParent<FenceAutoConnect>() != null)
                {
                    continue; // Xuyên qua HÀNG RÀO để thấy nền đất bên trong chuồng (tương tác thả thú)
                }
                else if (!hit.collider.isTrigger)
                {
                    // Direct tap chỉ chừa sai số bề mặt rất nhỏ: nền đất phải che nước/điểm câu bên dưới.
                    // Luồng aim cũ vẫn giữ tolerance lớn hơn để tương thích collider mỏng.
                    solidPassthroughLimit = Mathf.Min(solidPassthroughLimit, hit.distance + passthroughTolerance);
                    continue;
                }
            }

            if (foundActions.Count == 0 && TryResolveFarmTileFromAim(ray, out var aimTile, directTap))
            {
                foundObj = aimTile.gameObject;
                AddTileAction(aimTile, foundActions);
            }
            else if (foundActions.Count == 0 && TryResolvePathFromAim(ray, out var aimPath, directTap))
            {
                foundObj = aimPath;
                AddPathDemolishAction(aimPath, foundActions);
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
                currentPromptFromFrontCell = false;
                currentPromptFromFootWater = false;
                currentPromptFromFootResource = false;
            currentPromptFromFootFishing = false;

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
                    currentPromptFromFrontCell = false;
                    currentPromptFromFootWater = false;
                    currentPromptFromFootResource = false;
            currentPromptFromFootFishing = false;
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

        private bool IsMiningAllowedHere()
        {
            var itm = IslandTravelManager.Instance;
            return itm != null && (itm.CurrentIslandId == "city" || itm.CurrentIslandId == "mine");
        }

        private void EnsureMiningDailyTurns()
        {
            int maxTurns = Mathf.Max(0, dailyMiningTurns);
            string today = System.DateTime.Now.ToString("yyyy-MM-dd");
            string lastDate = PlayerScopedPrefs.GetString(MiningLastDateKey, "");

            if (lastDate != today)
            {
                miningTurnsLeft = maxTurns;
                PlayerScopedPrefs.SetString(MiningLastDateKey, today);
                PlayerScopedPrefs.SetInt(MiningTurnsLeftKey, miningTurnsLeft);
                PlayerScopedPrefs.Save();
                return;
            }

            if (miningTurnsLeft < 0)
                miningTurnsLeft = Mathf.Clamp(PlayerScopedPrefs.GetInt(MiningTurnsLeftKey, maxTurns), 0, maxTurns);
        }

        private bool HasMiningTurnsRemaining(bool showToast)
        {
            EnsureMiningDailyTurns();
            if (miningTurnsLeft > 0) return true;

            if (showToast && Time.unscaledTime >= nextMiningLimitToastAt)
            {
                ScreenToast.Show("Hết lượt đào hôm nay rồi! Mai quay lại nhé.");
                nextMiningLimitToastAt = Time.unscaledTime + 1.5f;
            }
            return false;
        }

        private bool ConsumeMiningTurn()
        {
            EnsureMiningDailyTurns();
            if (miningTurnsLeft <= 0) return false;

            miningTurnsLeft--;
            PlayerScopedPrefs.SetInt(MiningTurnsLeftKey, miningTurnsLeft);
            PlayerScopedPrefs.Save();
            return true;
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

            if (!IsFishingSpotInRange(spot, targetPoint, useDirectTapInteraction))
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
            currentPromptFromFrontCell = false;
            currentPromptFromFootWater = false;
            currentPromptFromFootResource = false;
            currentPromptFromFootFishing = false;
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

        // ── Cổng public cho popup "Xem ruộng" (khách chốt 30/07) ────────────────────────────────
        // Popup gom việc lại một chỗ giống popup chuồng: bấm cây trong danh sách rồi tưới / thu /
        // bón / dời ngay tại đó. Mọi nút đều gọi ĐÚNG luồng cũ ngoài ruộng — không có nhánh logic
        // thứ hai để lệch số liệu hay lách kiểm tra.

        /// <summary>
        /// Tưới cây đang chọn trong popup. Popup phải đóng vì tưới có màn múa động tác (che thì
        /// không thấy gì), nhưng <paramref name="onWatered"/> cho phép nó TỰ MỞ LẠI khi múa xong —
        /// anh chốt 31/07: tưới nhiều cây liên tiếp mà phải đi bấm lại "Xem ruộng" thì mệt.
        /// Chỉ gọi khi tưới THÀNH CÔNG; bỏ dở giữa chừng thì không mở lại.
        /// </summary>
        public void BeginWaterTile(FarmTile tile, System.Action onWatered = null)
        {
            if (tile == null) return;
            if (tile.currentState != FarmTile.TileState.Planted && tile.currentState != FarmTile.TileState.Watered)
            {
                ScreenToast.Show("Cây này chưa cần tưới.");
                return;
            }
            if (PlayerController.Instance != null) PlayerController.Instance.FaceTowards(tile.transform.position);
            HandleWater(tile, onWatered);
        }

        /// <summary>Thu hoạch cây đang chọn. KHÔNG có màn múa nên để popup mở, thu liền tay nhiều cây.</summary>
        public void BeginHarvestTile(FarmTile tile)
        {
            if (tile == null) return;
            if (tile.currentState != FarmTile.TileState.Ripe)
            {
                ScreenToast.Show("Cây chưa chín.");
                return;
            }
            if (PlayerController.Instance != null) PlayerController.Instance.FaceTowards(tile.transform.position);
            HandleHarvest(tile);
        }

        /// <summary>Bón phân cho cây đang chọn (mở túi ở tab Đồ dùng).</summary>
        public bool BeginFertilizeTile(FarmTile tile) => BeginFertilize(tile);

        /// <summary>Cây này bón phân được không — popup hỏi để bật/tắt nút, khỏi lộ ngưỡng ra ngoài.</summary>
        public bool CanFertilizeTile(FarmTile tile) => tile != null && tile.IsFertilizable(FertilizerMaxGrowthSec);

        // ── DỜI CỤM: chuồng / ruộng / đường lát đá dùng chung PenMoveController ──────────────────

        /// <summary>Cổng public cho popup chuồng bấm "Dời chuồng".</summary>
        public bool BeginMovePen(List<BuildSurfaceCell> pen)
        {
            if (pen == null || pen.Count == 0) return false;
            return StartGroupMove(new List<BuildSurfaceCell>(pen), "chuồng");
        }

        /// <summary>
        /// DỜI CẢ MẢNH RUỘNG (khách chốt 30/07): nhấc mọi ô đất liền nhau — cây đang trồng đi theo
        /// vì model cây là con của ô đất — rồi đặt xuống chỗ mới. Không tốn/hoàn vật liệu (ruộng vốn free).
        /// Chỉ dời được ruộng dựng bằng Chế độ Xây (nằm trên BuildSurfaceCell); ruộng cũ của
        /// TilePlacementSystem không có ô nền nên từ chối, thà không cho dời còn hơn dời xong mất cây.
        /// </summary>
        public bool BeginMovePlot(FarmTile seed)
        {
            if (seed == null) return false;

            var tiles = FindPlotTiles(seed.masterTile != null ? seed.masterTile : seed);
            var cells = new List<BuildSurfaceCell>();
            var seen = new HashSet<BuildSurfaceCell>();

            foreach (var tile in tiles)
            {
                if (tile == null) continue;
                var building = ResolvePlacedBuildingRoot(tile);
                var cell = building != null ? BuildSurfaceCell.FindByOccupant(building) : null;
                if (cell == null)
                {
                    ScreenToast.Show("Ruộng này không dời được (ruộng đời cũ, chưa gắn ô nền).");
                    return false;
                }
                if (seen.Add(cell)) cells.Add(cell);
            }

            if (cells.Count == 0) return false;
            return StartGroupMove(cells, "ruộng");
        }

        /// <summary>
        /// DỜI ĐƯỜNG LÁT ĐÁ: nhấc ĐÚNG VIÊN đang chỉ, không nhấc cả đoạn (anh chốt 30/07 —
        /// bốc cả lối đi lên thì khó dùng, chỉ muốn nắn lại một viên đặt lệch).
        /// Viên nào chiếm nhiều ô thì đi trọn bộ ô của nó.
        /// </summary>
        public bool BeginMovePath(GameObject pathBuilding)
        {
            if (pathBuilding == null) return false;

            var cells = BuildSurfaceCell.FindAllByOccupant(pathBuilding);
            if (cells.Count == 0)
            {
                ScreenToast.Show("Đường này không dời được (chưa gắn ô nền).");
                return false;
            }

            return StartGroupMove(cells, "đường");
        }

        private bool StartGroupMove(List<BuildSurfaceCell> cells, string subjectLabel)
        {
            if (!PenMoveController.Begin(cells, subjectLabel))
            {
                ScreenToast.Show($"Không dời được {subjectLabel} này.");
                return false;
            }

            ClearWorldInteractionState();
            ScreenToast.ShowInfo($"Đang dời {subjectLabel} ({PenMoveController.CellCount} ô) — đi tới chỗ mới rồi bấm Đặt.");
            return true;
        }

        // Khung hình lúc bật một việc "chờ chọn đồ trong túi" — để đừng dọn nhầm ngay khung vừa mở túi.
        private int pendingItemPickFrame = -1;

        private void MarkPendingItemPick() => pendingItemPickFrame = Time.frameCount;

        /// <summary>
        /// Đóng túi mà KHÔNG chọn gì thì bỏ luôn việc đang chờ. Trước đây cờ chờ nằm lại mãi, nên lần
        /// sau mở túi bấm món bất kỳ vẫn bị hiểu là "đang bón phân" / "đang cho ăn" — anh gặp lúc làm
        /// việc khác trên ruộng mà game bắn toast về phân bón.
        ///
        /// Cố ý KHÔNG đụng pendingPen / pendingEnclosure: hai cái đó do luồng thả thú BẤT ĐỒNG BỘ
        /// (chờ server) giữ, dọn ngang sẽ làm rơi kết quả trả về.
        /// </summary>
        private void ClearPendingItemPickIfBagClosed()
        {
            if (pendingFeedAnimal == null && pendingPlantTile == null) return;
            if (Time.frameCount <= pendingItemPickFrame + 1) return;              // vừa bấm, túi chưa kịp hiện
            if (inventoryPopup != null && inventoryPopup.IsVisible()) return;     // túi còn mở -> vẫn đang chọn

            pendingFeedAnimal = null;
            pendingPlantTile = null;
        }

        /// <summary>
        /// BÓN PHÂN — bón THẲNG, không mở túi (anh chốt 31/07).
        ///
        /// Trước đây bấm "Bón phân" thì mở túi cho người chơi chọn món. Bước đó THỪA vì phân bón chỉ
        /// có ĐÚNG MỘT loại; đổi lại nó gây hai phiền: popup "Xem ruộng" phải đóng nên không bón liên
        /// tiếp được, và cờ "đang chờ chọn phân" bị treo khi đóng túi giữa chừng (sinh ra toast phân
        /// bón lúc đang làm việc khác). Bón thẳng là hết cả hai.
        ///
        /// Trả về true nếu bón được — popup dùng để biết có cần vẽ lại không.
        /// </summary>
        private bool BeginFertilize(FarmTile tile)
        {
            if (tile == null) return false;

            if (!tile.IsFertilizable(FertilizerMaxGrowthSec))
            {
                ScreenToast.Show("Chỉ bón được CÂY NGẮN NGÀY đang lớn (đã tưới, chưa chín).");
                return false;
            }

            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv == null || inv.GetItemQuantity(FertilizerItemId) <= 0)
            {
                ScreenToast.Show("Trong túi không còn phân bón — mua thêm ở Cửa hàng Vật phẩm hoặc Đại lý Hai Lúa.");
                return false;
            }

            if (PlayerController.Instance != null) PlayerController.Instance.FaceTowards(tile.transform.position);
            if (!inv.RemoveItem(FertilizerItemId, 1)) return false;

            float bonusSec = FertilizerBonusSec;
            // Đọc giống TRƯỚC khi bón để tính ra phần trăm; bón xong cây có thể chín và mất crop.
            var fertilizedCrop = tile.GetCurrentCrop();

            if (!tile.ApplyFertilizer(bonusSec, FertilizerMaxGrowthSec))
            {
                inv.AddItem(FertilizerItemId, 1); // bón hụt thì HOÀN phân, không nuốt đồ của người chơi
                ScreenToast.Show("Bón không được — cây chưa tưới hoặc đã chín.");
                return false;
            }

            string saved = FertilizerSavingText(fertilizedCrop, bonusSec);

            // Bón đúng túi CUỐI thì gộp luôn lời nhắc vào câu báo thành công. Trước đây bắn 2 toast
            // liền nhau ("đã bón" rồi "hết phân bón"), anh báo là người chơi tưởng bón hụt.
            string note = inv.GetItemQuantity(FertilizerItemId) <= 0
                ? " Đó là túi phân cuối — mua thêm ở Cửa hàng Vật phẩm hoặc Đại lý Hai Lúa."
                : "";

            ScreenToast.ShowInfoForItem(FertilizerItemId, $"Đã bón phân: {saved}.{note}", fallbackText: "Phân");
            FarmActivityLog.RecordEvent(tile.HistoryKey, FarmActivityLog.KindFertilize, saved);
            return true;
        }

        /// <summary>
        /// Chữ báo hiệu quả bón phân. Khách muốn nói theo PHẦN TRĂM ("giảm 15% thời gian") chứ không
        /// theo số giờ. Phần trăm TÍNH RA từ chính giống cây đang bón chứ không gõ cứng — đổi
        /// <c>fertilizerBonusHours</c> trong Inspector là câu chữ tự đúng theo, khỏi lệch với số thật.
        /// </summary>
        private static string FertilizerSavingText(CropDefinition crop, float bonusSec)
        {
            if (crop != null && crop.growthTimeSec > 0f)
            {
                int percent = Mathf.RoundToInt(bonusSec / crop.growthTimeSec * 100f);
                if (percent > 0) return $"giảm {percent}% thời gian chín";
            }

            // Không tra được giống thì lùi về nói số giờ — vẫn đúng, chỉ kém gọn.
            return $"chín sớm hơn {YWonderLand.Core.GameTimeConfig.FormatDuration(bonusSec)}";
        }

        // Cho ăn = mở túi (tab Thực phẩm) chọn thức ăn (tạm dùng Bắp ngô) -> animation Feed.
        private void FeedAnimal(FarmAnimal animal)
        {
            if (animal == null) return;
            pendingFeedAnimal = animal;
            MarkPendingItemPick();
            pendingPen = null;
            pendingPlantTile = null;
            pendingEnclosure = null;
            if (PlayerController.Instance != null) PlayerController.Instance.FaceTowards(animal.transform.position);

            WarnIfNoFeed(animal); // hết thức ăn thì nhắc, không cấp thêm

            EnsureInventoryPopupSubscribed();
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

            // Phát clip nhanh hơn để múa TRỌN trong ~FeedActionDuration (khóa = clip/speed).
            float feedSpeed = FeedClipDuration / Mathf.Max(0.1f, FeedActionDuration);
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
                        // Ghi nhật ký cho ăn — hiện lại trong popup của chính con này (khách chốt 30/07).
                        FarmActivityLog.RecordFeed(animal.animalInstanceId, $"{required}x {matchedName}");
                        FarmStateSync.SaveBuildState();
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
                feedSpeed);

            if (!started && inv != null)
                inv.AddItem(itemId, required);
        }

        // Hết thức ăn thì NHẮC người chơi trồng/mua, không phát không.
        // (Bản demo trước tự cấp thức ăn mỗi khi về 0 — cùng lỗi với hạt giống.)
        private void WarnIfNoFeed(FarmAnimal animal)
        {
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv == null || animal == null || animal.data == null) return;

            string main = animal.data.foodMainName;
            // Thức ăn phụ ghi amount 0 (vd 'Cám') là không dùng thật -> bỏ qua.
            string alt = animal.data.foodAltAmount > 0 ? animal.data.foodAltName : null;

            if (HasFood(inv, main) || HasFood(inv, alt)) return;

            string names = string.IsNullOrEmpty(alt) ? main : $"{main} hoặc {alt}";
            if (string.IsNullOrEmpty(names)) return;

            string who = !string.IsNullOrEmpty(animal.data.animalName) ? animal.data.animalName : "Con vật";
            ScreenToast.Show($"{who} cần {names} — trồng thêm hoặc mua ở Farm Shop.");
        }

        private bool HasFood(YWonderLand.Managers.InventoryManager inv, string foodName)
        {
            if (string.IsNullOrEmpty(foodName)) return false;
            string id = ResolveItemIdByName(foodName);
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[FarmInteraction] Không tìm thấy item khớp tên thức ăn '{foodName}' trong ItemDatabase (kiểm tra lại tên trong AnimalDefinition vs ItemDatabase).");
                return false;
            }
            return inv.GetItemQuantity(id) > 0;
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

        // Chữa bệnh TỐN 1 Thuốc (medicine_01) — trước đây chữa miễn phí, thành lỗ hổng kinh tế.
        private void HealAnimal(FarmAnimal animal)
        {
            if (animal == null) return;
            if (animal.currentState != FarmAnimal.AnimalState.Sick)
            {
                ScreenToast.Show("Con vật không bị bệnh, chưa cần dùng thuốc.");
                return;
            }

            // Số LIỀU thuốc theo loài (VatNuoi2: bò 10 · heo 9 · vịt 2...), không còn cứng 1 liều.
            int doses = animal.MedicineDosesPerCure;
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv == null || inv.GetItemQuantity("medicine_01") < doses)
            {
                ScreenToast.Show($"Cần {doses} Thuốc để chữa {animal.data?.animalName}! Mua thêm ở Cửa hàng vật phẩm.");
                return;
            }
            if (!inv.RemoveItem("medicine_01", doses)) return;

            PlayerController player = PlayerController.Instance;
            // Chưa có animation "tiêm/chữa bệnh" riêng -> tạm dùng "Feed" (động tác đưa tay) cho đỡ trống
            if (player != null) player.PlayActionAnimation("Feed", 0f);
            if (animal.Heal())
            {
                ScreenToast.Show($"Đã chữa khỏi bệnh (tốn {doses} Thuốc)!");
                FarmStateSync.SaveBuildState();
            }
            else
            {
                inv.AddItem("medicine_01", doses); // không chữa được thì hoàn đủ số thuốc
            }
        }

        // Tiêm vắc-xin PHÒNG bệnh — tốn 1 vaccine_01. Không chữa được thú đang bệnh.
        private void VaccinateAnimal(FarmAnimal animal)
        {
            if (animal == null) return;
            if (animal.currentState == FarmAnimal.AnimalState.Sick)
            {
                ScreenToast.Show("Con vật đang bệnh — phải dùng Thuốc chữa trước, rồi mới tiêm phòng.");
                return;
            }
            if (animal.IsVaccineActive)
            {
                ScreenToast.Show("Vắc-xin còn hiệu lực, chưa cần tiêm lại.");
                return;
            }

            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv == null || inv.GetItemQuantity("vaccine_01") <= 0)
            {
                ScreenToast.Show("Hết Vắc-xin! Mua thêm ở Cửa hàng vật phẩm.");
                return;
            }
            if (!inv.RemoveItem("vaccine_01", 1)) return;

            PlayerController player = PlayerController.Instance;
            if (player != null) player.PlayActionAnimation("Feed", 0f); // chưa có animation tiêm riêng
            if (animal.Vaccinate())
            {
                ScreenToast.Show("Đã tiêm vắc-xin — con vật được phòng bệnh.");
                FarmStateSync.SaveBuildState();
            }
            else
            {
                inv.AddItem("vaccine_01", 1); // tiêm hụt thì hoàn lại
            }
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
                    ScreenToast.ShowItemReward(itemId, amount, "Thu hoạch");

                FarmStateSync.SaveBuildState();
            }
        }

        private void ClickHarvestResource(HarvestableResource resource)
        {
            // Đào đá chỉ ở các đảo có khu khai thác; chặt cây vẫn bình thường ở nơi có tài nguyên.
            if (resource != null && resource.type == HarvestableResource.ResourceType.Rock && !IsMiningAllowedHere())
            {
                ScreenToast.Show("Chỉ đào đá được ở Thành phố hoặc Đảo mỏ thôi!");
                return;
            }
            if (!IsServerResourceSyncReady(true)) return;
            if (resource != null
                && resource.type == HarvestableResource.ResourceType.Rock
                && !RequiresServerResourceSync()
                && !HasMiningTurnsRemaining(true))
                return;

            if (GetResourceDistanceToPlayer(resource) > GetResourceActionRange(resource))
                return;

            StartResourceTimedAction(resource);
        }

        private void StartResourceTimedAction(HarvestableResource resource)
        {
            if (resource == null || !resource.isHarvestable) return;
            if (!IsServerResourceSyncReady(true)) return;
            if (resource.type == HarvestableResource.ResourceType.Rock
                && !RequiresServerResourceSync()
                && !HasMiningTurnsRemaining(true)) return;

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
            if (!IsServerResourceSyncReady(true)) { _buttonHeldResource = null; return; }

            PlayerController player = PlayerController.Instance;
            if (GetResourceDistanceToPlayer(resource) > GetResourceActionRange(resource)) return; // quá xa -> khựng
            if (resource.type == HarvestableResource.ResourceType.Rock
                && !RequiresServerResourceSync()
                && !HasMiningTurnsRemaining(true))
            {
                _buttonHeldResource = null;
                return;
            }

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
            if (!IsTileInRange(tile, useDirectTapInteraction))
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
                    // Đào đá chỉ ở City hoặc Mine; bỏ qua tảng đá nếu đang ở đảo khác.
                    if (resource.type == HarvestableResource.ResourceType.Rock && !IsMiningAllowedHere()) continue;
                    if (!IsServerResourceSyncReady(true)) return;
                    if (resource.type == HarvestableResource.ResourceType.Rock
                        && !RequiresServerResourceSync()
                        && !HasMiningTurnsRemaining(true)) return;

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

                // Fallback: vẫn cho click trực tiếp vào WaterSource nếu người chơi bấm đúng collider ao.
                var waterSrcClick = hit.collider.GetComponentInParent<WaterSource>();
                if (waterSrcClick != null)
                {
                    if (IsWaterSourceInRange(waterSrcClick)) ScoopWater(waterSrcClick);
                    return;
                }

                // Chuồng từ HÀNG RÀO: ưu tiên trước FarmTile để collider đất bên dưới không ăn mất tap/click.
                var penCellBeforeTile = ResolveBuildSurfaceCellFromHit(hit);
                if (penCellBeforeTile != null && penCellBeforeTile.HasFence)
                {
                    var pen = PenEnclosure.FindPen(penCellBeforeTile);
                    if (pen != null && IsEnclosureInRange(pen) && AnimalInteractionPopupController.Instance != null)
                        AnimalInteractionPopupController.Instance.ShowEnclosure(pen);
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
                    {
                        FarmStateSync.SaveTileState(tile);
                        Debug.Log("[FarmInteraction] Plowed tile!");
                    }
                });
        }

        private void HandleOpenSeedSelection(FarmTile tile)
        {
            if (tile == null || tile.masterTile != null)
            {
                pendingPlantTile = null;
                ScreenToast.Show("\u00d4 \u0111\u1ea5t n\u00e0y \u0111ang thu\u1ed9c m\u1ed9t gi\u00e0n c\u00e2y. H\u00e3y ch\u1ecdn \u00f4 \u0111\u1ea5t tr\u1ed1ng kh\u00e1c.");
                return;
            }
            if (tile.currentState != FarmTile.TileState.Plowed)
            {
                pendingPlantTile = null;
                ScreenToast.Show("\u00d4 \u0111\u1ea5t kh\u00f4ng c\u00f2n s\u1eb5n s\u00e0ng \u0111\u1ec3 gieo h\u1ea1t.");
                return;
            }

            // Ghi nhớ ô đất đang chờ gieo, rồi mở Túi đồ ở tab Hạt giống để người chơi CHỌN loại cây.
            pendingPlantTile = tile;
            MarkPendingItemPick();

            // Hết hạt thì NHẮC ra shop mua, không tặng thêm.
            WarnIfNoSeeds();

            EnsureInventoryPopupSubscribed();

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

        // Hết hạt là phải MUA, không phát không.
        // (Bản demo trước tự cộng 3 hạt cà rốt/cải/bắp mỗi lần về 0 -> hạt vô hạn, hỏng kinh tế
        //  và còn đẩy delta +3 lên server nên số hạt bên server cũng phồng theo.)
        private void WarnIfNoSeeds()
        {
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv == null || HasAnySeed(inv)) return;

            ScreenToast.Show("Hết hạt giống rồi — ra Farm Shop mua thêm để trồng.");
        }

        private bool HasAnySeed(YWonderLand.Managers.InventoryManager inv)
        {
            var db = FoodDb; // dùng chung ItemDatabase
            if (db == null) return true; // không tra được thì thôi, đừng báo nhầm

            foreach (var slot in inv.GetAllSlots())
            {
                if (slot == null || slot.quantity <= 0) continue;
                var def = db.GetItem(slot.itemId);
                if (def != null && def.category == "seeds") return true;
            }
            return false;
        }

        // Mở túi đồ (tab Thú nuôi) để chọn con vật thả vào chuồng đang đứng.
        private void OpenPenAnimalPicker(YWonderLand.Environment.AnimalPenSpawner pen)
        {
            if (pen == null) return;
            pendingPen = null;
            pendingPlantTile = null; // tránh nhầm với luồng gieo hạt
            pendingEnclosure = null;
            pendingFeedAnimal = null;
            ScreenToast.Show("Chuồng kiểu cũ chưa hỗ trợ đồng bộ. Hãy dùng chuồng hàng rào.");
            Debug.LogWarning($"[AnimalPlacement] Blocked legacy local pen '{pen.name}'.");
        }

        // Mở túi đồ (tab Thú nuôi) để chọn con vật thả vào VÙNG QUÂY (chuồng từ hàng rào).
        private void OpenEnclosurePicker(List<BuildSurfaceCell> interior)
        {
            if (interior == null) return;
            pendingEnclosure = interior;
            pendingPen = null;
            pendingFeedAnimal = null;
            pendingPlantTile = null;

            EnsureInventoryPopupSubscribed();
            if (inventoryPopup != null)
            {
                inventoryPopup.ShowAtTab("animals");
                int free = PenEnclosure.AvailableCount(interior);
                Debug.Log($"[FarmInteraction] Mở Túi (Thú nuôi) — chuồng còn {free} ô trống.");
            }
        }

        // Server atomically consumes the inventory item and appends the animal to the farm snapshot.
        private async Awaitable HandleEnclosureAnimalSelectedAsync(string itemId)
        {
            var interior = pendingEnclosure;
            pendingEnclosure = null;
            if (interior == null || animalPlacementInFlight) return;

            var def = YWonderLand.Managers.AnimalManager.LookupDefinition(itemId);
            var inventory = YWonderLand.Managers.InventoryManager.Instance;
            if (def == null || inventory == null || inventory.GetItemQuantity(itemId) < 1)
            {
                ScreenToast.Show("Không có con giống này trong túi.");
                if (inventoryPopup != null) inventoryPopup.Hide();
                return;
            }

            int quantityBefore = inventory.GetItemQuantity(itemId);

            int need = Mathf.Max(1, def.penSlots);
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
            if (cells.Count < need)
            {
                ScreenToast.Show("Chuồng không đủ chỗ!");
                if (inventoryPopup != null) inventoryPopup.Hide();
                return;
            }

            var cellKeys = new List<string>(cells.Count);
            foreach (var cell in cells)
                cellKeys.Add(FarmStateSync.CellKey(cell.transform.position));

            if (inventoryPopup != null) inventoryPopup.Hide();

            animalPlacementInFlight = true;
            try
            {
                var result = await FarmStateSync.PlaceAnimalAsync(itemId, cellKeys);
                if (!result.ok)
                {
                    string message;
                    switch (result.errorCode)
                    {
                        case "INSUFFICIENT_ITEM":
                            message = "Con giống đã hết trong túi.";
                            break;
                        case "PEN_CELL_OCCUPIED":
                        case "FARM_STATE_CONFLICT":
                            message = "Chuồng vừa thay đổi. Hãy chọn lại vị trí thả thú.";
                            break;
                        case "INVALID_PEN_CELL":
                        case "INVALID_PEN_SLOT_COUNT":
                            message = "Vị trí chuồng không hợp lệ.";
                            break;
                        default:
                            message = "Không thể thả thú khi chưa kết nối máy chủ. Hãy thử lại.";
                            break;
                    }
                    ScreenToast.Show(message);
                    return;
                }

                int appliedQuantity = inventory.GetItemQuantity(itemId);
                if (appliedQuantity != result.remainingQuantity)
                {
                    Debug.LogWarning(
                        $"[AnimalPlacement] Inventory snapshot mismatch for '{itemId}': " +
                        $"response={result.remainingQuantity}, applied={appliedQuantity}.");
                }
                if (result.remainingQuantity >= quantityBefore)
                {
                    Debug.LogWarning(
                        $"[AnimalPlacement] Accepted placement did not reduce the visible quantity for '{itemId}': " +
                        $"before={quantityBefore}, remaining={result.remainingQuantity}, duplicate={result.duplicate}.");
                }

                // The authoritative snapshot rebuilds the enclosure on the following frames.
                await Awaitable.NextFrameAsync();
                await Awaitable.NextFrameAsync();
                var spawned = cells[0] != null && cells[0].AnimalObject != null
                    ? cells[0].AnimalObject.GetComponent<FarmAnimal>()
                    : null;
                if (spawned != null) FarmAnimal.RaiseSpawned(spawned);
                ScreenToast.ShowInfo($"Đã thả {def.animalName}. Còn {result.remainingQuantity} trong túi.");
            }
            finally
            {
                animalPlacementInFlight = false;
            }
        }

        private void RequestDemolishEnclosure(List<BuildSurfaceCell> encl)
        {
            if (encl == null || encl.Count == 0) return;

            if (pendingDemolishEnclosure != null && IsSameEnclosure(pendingDemolishEnclosure, encl))
            {
                pendingDemolishEnclosure = null;
                pendingDemolishTile = null;
                demolishConfirmTimer = 0f;
                DemolishEnclosure(encl);
                return;
            }

            pendingDemolishTile = null;
            pendingDemolishEnclosure = new List<BuildSurfaceCell>(encl);
            demolishConfirmTimer = DemolishConfirmWindow;
            ScreenToast.Show("Nhấn hủy chuồng lần nữa để xác nhận.");
        }

        private void RequestDemolishFarmTile(FarmTile tile)
        {
            if (tile == null) return;

            GameObject building = ResolvePlacedBuildingRoot(tile);
            if (building == null)
            {
                ScreenToast.Show("Ch\u1ec9 h\u1ee7y \u0111\u01b0\u1ee3c \u00f4 tr\u1ed3ng \u0111\u00e3 x\u00e2y b\u1eb1ng Build Mode.");
                return;
            }

            if (ResolvePlacedBuildingRoot(pendingDemolishTile) == building)
            {
                pendingDemolishTile = null;
                pendingDemolishEnclosure = null;
                demolishConfirmTimer = 0f;
                DemolishFarmTile(tile, building);
                return;
            }

            pendingDemolishEnclosure = null;
            pendingDemolishTile = tile;
            demolishConfirmTimer = DemolishConfirmWindow;
            ScreenToast.Show("Nh\u1ea5n h\u1ee7y \u00f4 tr\u1ed3ng l\u1ea7n n\u1eefa \u0111\u1ec3 x\u00e1c nh\u1eadn.");
        }

        private void DemolishFarmTile(FarmTile tile, GameObject building = null)
        {
            if (tile == null) return;
            if (!IsTileInRange(tile, useDirectTapInteraction))
            {
                ScreenToast.Show("\u0110\u1ee9ng g\u1ea7n \u00f4 tr\u1ed3ng h\u01a1n \u0111\u1ec3 h\u1ee7y.");
                return;
            }

            building ??= ResolvePlacedBuildingRoot(tile);
            if (building == null) return;

            // Chốt 29/07: chỉ hủy ô TRỐNG. Chặn ở đây phòng khi lời gọi tới từ đường khác (phím tắt, prompt cũ).
            if (HasCropOnFarmTile(tile))
            {
                ScreenToast.Show("Ô đang có cây — thu hoạch xong mới hủy được.");
                return;
            }

            string buildingName = building.name;

            bool hadCrop = tile.currentState == FarmTile.TileState.Planted ||
                           tile.currentState == FarmTile.TileState.Watered ||
                           tile.currentState == FarmTile.TileState.Ripe;

            if (pendingPlantTile != null && ResolvePlacedBuildingRoot(pendingPlantTile) == building)
                pendingPlantTile = null;

            pendingDemolishTile = null;
            pendingDemolishEnclosure = null;
            demolishConfirmTimer = 0f;

            BuildSurfaceCell.ClearOccupant(building);
            Destroy(building);

            hoverEnclosureSeed = null;
            hoverEnclosure = null;
            pendingEnclosure = null;
            currentHoverObject = null;
            currentActions.Clear();
            lastActionSignature = "";
            currentPromptFromFrontCell = false;
            currentPromptFromFootWater = false;
            currentPromptFromFootResource = false;
            currentPromptFromFootFishing = false;
            GameHUDController.Instance?.HideInteractionPrompt();
            if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();

            var persistence = Object.FindFirstObjectByType<BuildPersistence>(FindObjectsInactive.Include);
            persistence?.SaveBuildings();

            ScreenToast.ShowInfo(hadCrop ? "\u0110\u00e3 h\u1ee7y \u00f4 tr\u1ed3ng v\u00e0 c\u00e2y tr\u00ean \u00f4." : "\u0110\u00e3 h\u1ee7y \u00f4 tr\u1ed3ng.");
            Debug.Log($"[FarmInteraction] Huy o trong: {buildingName}, hadCrop={hadCrop}.");
        }

        // Hủy đường đá (hoặc trang trí khác đặt qua Build Mode): nhấn 2 lần trong DemolishConfirmWindow để xác nhận,
        // rồi hoàn ĐÚNG vật liệu đã tốn và trả ô về trống — đồng nhất với hủy ruộng/chuồng.
        private void RequestDemolishPathBuilding(GameObject building)
        {
            if (building == null) return;

            if (pendingDemolishPath == building)
            {
                pendingDemolishPath = null;
                pendingDemolishTile = null;
                pendingDemolishEnclosure = null;
                demolishConfirmTimer = 0f;
                DemolishPathBuilding(building);
                return;
            }

            pendingDemolishTile = null;
            pendingDemolishEnclosure = null;
            pendingDemolishPath = building;
            demolishConfirmTimer = DemolishConfirmWindow;
            ScreenToast.Show("Nhấn hủy đường lần nữa để xác nhận.");
        }

        private void DemolishPathBuilding(GameObject building)
        {
            if (building == null) return;
            if (!IsPlacedBuildingInRange(building, useDirectTapInteraction))
            {
                ScreenToast.Show("Đứng gần đường hơn để hủy.");
                return;
            }

            string buildingName = building.name;

            pendingDemolishTile = null;
            pendingDemolishEnclosure = null;
            pendingDemolishPath = null;
            demolishConfirmTimer = 0f;

            // Hoàn vật liệu đã tốn — đọc từ ô TRƯỚC khi ClearOccupant xóa dữ liệu vật liệu. Đồng nhất với hủy chuồng.
            BuildSurfaceCell.SumRefund(building, out int refundWood, out int refundStone);
            BuildSurfaceCell.ClearOccupant(building);
            Destroy(building);

            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv != null)
            {
                if (refundWood > 0) inv.AddItem("wood_01", refundWood, "build_refund");
                if (refundStone > 0) inv.AddItem("stone_01", refundStone, "build_refund");
            }

            hoverEnclosureSeed = null;
            hoverEnclosure = null;
            pendingEnclosure = null;
            currentHoverObject = null;
            currentActions.Clear();
            lastActionSignature = "";
            currentPromptFromFrontCell = false;
            currentPromptFromFootWater = false;
            currentPromptFromFootResource = false;
            currentPromptFromFootFishing = false;
            GameHUDController.Instance?.HideInteractionPrompt();
            if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
                YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();

            var persistence = Object.FindFirstObjectByType<BuildPersistence>(FindObjectsInactive.Include);
            persistence?.SaveBuildings();

            string msg = "Đã hủy đường đá";
            if (refundStone > 0) msg += $", +{refundStone} Đá";
            if (refundWood > 0) msg += $", +{refundWood} Gỗ";
            ScreenToast.ShowInfo(msg);
            Debug.Log($"[FarmInteraction] Huy duong da: {buildingName}, refund wood={refundWood}, stone={refundStone}.");
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

            // Chốt 29/07: chuồng còn thú thì không cho hủy (nút đã ẩn; chặn thêm ở đây cho mọi đường gọi).
            if (EnclosureHasAnimal(pen))
            {
                ScreenToast.Show("Chuồng còn vật nuôi — hãy bán hoặc dời thú trước khi hủy.");
                return;
            }

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
            currentPromptFromFrontCell = false;
            currentPromptFromFootWater = false;
            currentPromptFromFootResource = false;
            currentPromptFromFootFishing = false;
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

        // Người chơi chọn 1 con vật trong túi khi đang đứng ở chuồng -> kiểm tra + thả.
        private void HandlePenAnimalSelected(string itemId)
        {
            var pen = pendingPen;
            pendingPen = null;
            if (pen == null) return;
            if (inventoryPopup != null) inventoryPopup.Hide();
            ScreenToast.Show("Chuồng kiểu cũ chưa hỗ trợ đồng bộ. Hãy dùng chuồng hàng rào.");
            Debug.LogWarning($"[AnimalPlacement] Rejected legacy selection '{itemId}' for pen '{pen.name}'.");
        }

        private void OnInventoryItemSelected(string itemId)
        {
            // MarkHandled() = "bấm nút đã ra việc". Nhánh nào không gọi thì túi đồ tự bắn toast
            // giải thích công dụng (ItemUsageHint) thay vì im lặng như trước.
            void MarkHandled()
            {
                if (inventoryPopup != null) inventoryPopup.LastItemUseHandled = true;
            }

            // Ưu tiên: đang chờ chọn thức ăn để cho động vật ăn.
            if (pendingFeedAnimal != null)
            {
                MarkHandled();
                HandleFeedSelected(itemId);
                return;
            }

            // (Bón phân KHÔNG còn đi qua đây: từ 31/07 bấm "Bón phân" là bón thẳng, không mở túi.
            //  Bấm Phân bón trong túi giờ rơi xuống ItemUsageHint, chỉ đường ra ruộng — đúng ý.)

            // Ưu tiên: đang chờ thả thú vào VÙNG QUÂY (chuồng từ hàng rào).
            if (pendingEnclosure != null)
            {
                MarkHandled();
                _ = HandleEnclosureAnimalSelectedAsync(itemId);
                return;
            }

            // Ưu tiên: đang chờ chọn con vật cho 1 chuồng (kiểu cũ) -> xử lý thả thú.
            if (pendingPen != null)
            {
                MarkHandled();
                HandlePenAnimalSelected(itemId);
                return;
            }

            // Bấm "Sử dụng" VÉ trong túi (khi KHÔNG ở chế độ chờ chọn cho ăn/thả/gieo).
            if (itemId == "mine_ticket_01") { MarkHandled(); UseMineTicket(); return; }
            if (itemId == "spin_ticket_01") { MarkHandled(); UseSpinTicket(); return; }

            // Đang chờ chọn hạt để gieo: nhắc rõ khi người chơi bấm nhầm món không phải hạt,
            // thay vì bỏ qua lặng lẽ khiến họ tưởng nút hỏng.
            if (pendingPlantTile != null && !itemId.Contains("seed"))
            {
                MarkHandled();
                ScreenToast.Show("Đang chọn hạt để gieo — hãy chọn một HẠT GIỐNG trong tab Hạt giống.");
                return;
            }

            // Ngoài các trường hợp trên: KHÔNG MarkHandled -> túi đồ giải thích công dụng.
            if (pendingPlantTile == null) return;

            MarkHandled();

            if (!TryValidatePlanting(pendingPlantTile, itemId, out string plantingError))
            {
                ScreenToast.Show(plantingError);
                return;
            }

            pendingSeedId = itemId;

            // Remove seed from inventory
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            bool seedConsumed = false;
            int seedCost = GetSeedCostForPlanting(itemId);
            if (inv != null)
            {
                int owned = inv.GetItemQuantity(itemId);
                if (owned < seedCost)
                {
                    ScreenToast.Show($"Cần {seedCost} {GetItemDisplayName(itemId)} để trồng.");
                    return;
                }
                seedConsumed = inv.RemoveItem(itemId, seedCost);
                if (!seedConsumed) return;
            }

            // Đóng túi đồ, rồi MÚA động tác trồng TRƯỚC — gieo hạt SAU khi progress/clip chạy xong.
            if (inventoryPopup != null) inventoryPopup.Hide();

            if (!StartPlantTimedAction(pendingPlantTile, itemId, seedConsumed, seedCost) && seedConsumed && inv != null)
                inv.AddItem(itemId, seedCost);

            pendingPlantTile = null;
            pendingSeedId = null;
        }

        /// <summary>
        /// "Sử dụng" Vé đào mỏ trong túi: trừ 1 vé -> +1 lượt đào (LOCAL).
        /// LƯU Ý: ở Thành phố/Hầm mỏ khi ONLINE, lượt đào do SERVER quản (giới hạn ngày server-side);
        /// vé cộng lượt local chỉ có tác dụng ở bản OFFLINE. Muốn vé cộng lượt cả khi online thì phải
        /// làm 1 endpoint server cấp lượt đào theo vé (chưa làm ở đây).
        /// </summary>
        // Online: server nắm daily-limit "mining" (đào realtime kiểm), nên vé phải đổi qua server.
        private static bool IsMiningServerAuthoritative()
        {
            var auth = YWonderLand.Backend.AuthService.Instance;
            return auth != null && auth.IsSignedIn && !string.IsNullOrWhiteSpace(auth.Token);
        }

        private void UseMineTicket()
        {
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv == null || inv.GetItemQuantity("mine_ticket_01") <= 0)
            {
                ScreenToast.Show("Bạn không có Vé đào mỏ.");
                return;
            }

            // ONLINE: server tự trừ vé + +1 lượt daily-limit "mining"; client KHÔNG tự trừ/cộng.
            if (IsMiningServerAuthoritative())
            {
                RedeemMineTicketServerAsync();
                return;
            }

            // OFFLINE (demo): cộng lượt local.
            EnsureMiningDailyTurns();
            if (!inv.RemoveItem("mine_ticket_01", 1, "use_mine_ticket")) return;

            miningTurnsLeft = Mathf.Max(0, miningTurnsLeft) + 1;
            PlayerScopedPrefs.SetInt(MiningTurnsLeftKey, miningTurnsLeft);
            PlayerScopedPrefs.Save();
            ScreenToast.Show($"Đã dùng 1 Vé đào mỏ (+1 lượt đào, còn {miningTurnsLeft} lượt).");
        }

        private async void RedeemMineTicketServerAsync()
        {
            var result = await YWonderLand.Backend.MiningService.RedeemTicketAsync();
            if (result.ok)
            {
                SetServerMiningTurns(result.miningTurnsRemaining);
                ScreenToast.Show($"Đã dùng 1 Vé đào mỏ (+1 lượt, còn {result.miningTurnsRemaining} lượt hôm nay).");
            }
            else if (result.errorCode == "NO_MINE_TICKET")
            {
                ScreenToast.Show("Bạn không có Vé đào mỏ.");
            }
            else
            {
                ScreenToast.Show("Mất kết nối, chưa dùng được vé. Thử lại nhé.");
            }
        }

        // Chốt tránh auto-đổi vé lặp vô hạn (mỗi lần đào-lại chỉ đổi tối đa 1 vé; reset khi đào thành công).
        private bool _autoRedeemInFlight = false;

        private static bool HasMineTicket()
        {
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            return inv != null && inv.GetItemQuantity("mine_ticket_01") > 0;
        }

        // Online hết lượt + còn vé -> tự đổi 1 vé (server) rồi đào lại chính tài nguyên đó. Không cần bấm "Sử dụng".
        private async void AutoRedeemMineTicketThenRetry(HarvestableResource resource)
        {
            var result = await YWonderLand.Backend.MiningService.RedeemTicketAsync();
            if (!result.ok)
            {
                _autoRedeemInFlight = false;
                ScreenToast.Show(result.errorCode == "NO_MINE_TICKET"
                    ? "Hết lượt đào hôm nay rồi! Mua Vé đào mỏ để đào thêm nhé."
                    : "Mất kết nối, chưa dùng được vé. Thử lại nhé.");
                return;
            }

            SetServerMiningTurns(result.miningTurnsRemaining);
            ScreenToast.Show($"Tự dùng 1 Vé đào mỏ (+1 lượt, còn {result.miningTurnsRemaining}). Đào lại...");

            // Đào lại tài nguyên đó bằng lượt vừa cấp. _autoRedeemInFlight giữ true tới khi đào thành công
            // (reset trong HandleSharedResourceHarvestResult) -> nếu vẫn hết lượt thì KHÔNG đổi vé lần nữa.
            var realtime = YWonderLand.Realtime.RealtimeClient.Instance;
            if (resource != null && resource.isHarvestable && realtime != null)
                realtime.TryRequestResourceHarvest(resource, r => HandleSharedResourceHarvestResult(resource, r));
            else
                _autoRedeemInFlight = false;
        }

        /// <summary>
        /// "Sử dụng" Vé vòng quay trong túi: mở Vòng quay may mắn. Vé sẽ bị trừ KHI quay quá lượt
        /// free trong ngày (logic trừ nằm ở EventPopupController.OnSpin), không trừ lúc chỉ mở ra.
        /// </summary>
        private void UseSpinTicket()
        {
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv == null || inv.GetItemQuantity("spin_ticket_01") <= 0)
            {
                ScreenToast.Show("Bạn không có Vé vòng quay.");
                return;
            }

            // Chỉ lấy bản ĐANG ACTIVE (đã bind UI) để ShowLuckyWheel chạy được; nếu không có thì báo mở qua Sự kiện.
            var eventPopup = Object.FindFirstObjectByType<EventPopupController>();
            if (eventPopup == null)
            {
                ScreenToast.Show("Chưa mở được Vòng quay. Vào mục Sự kiện để quay nhé.");
                return;
            }

            if (inventoryPopup != null) inventoryPopup.Hide();
            eventPopup.ShowLuckyWheel();
        }

        // Múa động tác Planting xong MỚI thật sự gieo hạt xuống ô đất.
        private bool StartPlantTimedAction(FarmTile tile, string seedId, bool seedConsumed, int seedConsumedAmount = 1)
        {
            if (!TryValidatePlanting(tile, seedId, out string plantingError))
            {
                ScreenToast.Show(plantingError);
                return false;
            }

            var inv = YWonderLand.Managers.InventoryManager.Instance;
            // Phát clip nhanh hơn để múa TRỌN trong ~PlantingActionDuration (khóa = clip/speed).
            float plantSpeed = PlantingClipDuration / Mathf.Max(0.1f, PlantingActionDuration);
            bool started = BeginTimedAction(
                "Planting",
                PlantingClipDuration,
                YWonderLand.Player.ToolType.SeedBag,
                tile.transform.position,
                () =>
                {
                    if (tile != null && PlantWithSlots(tile, seedId))
                    {
                        FarmStateSync.SaveTileState(tile);
                        Debug.Log($"[FarmInteraction] Gieo hạt {seedId} SAU khi múa xong!");
                    }
                    else if (seedConsumed && inv != null)
                    {
                        inv.AddItem(seedId, Mathf.Max(1, seedConsumedAmount));
                    }
                },
                () =>
                {
                    if (seedConsumed && inv != null)
                        inv.AddItem(seedId, Mathf.Max(1, seedConsumedAmount));
                },
                plantSpeed);

            if (!started)
                ScreenToast.Show("Ch\u01b0a th\u1ec3 gieo h\u1ea1t l\u00fac n\u00e0y. H\u00e3y ch\u1edd thao t\u00e1c hi\u1ec7n t\u1ea1i k\u1ebft th\u00fac.");
            return started;
        }

        private int GetSeedCostForPlanting(string seedId)
        {
            if (string.IsNullOrEmpty(seedId)) return 1;

            var cropDb = Resources.Load<CropDatabase>("CropDatabase");
            var crop = cropDb != null ? cropDb.GetCropBySeedId(seedId) : null;
            return crop != null ? Mathf.Max(1, crop.seedItemCost) : 1;
        }

        // Trồng cây có thể CHIẾM NHIỀU Ô (giàn): cây nhiều ô (vd chanh dây 20 ô) cần thêm ô trống gần nhất.
        private bool PlantWithSlots(FarmTile master, string seedId)
        {
            if (!TryValidatePlanting(master, seedId, out string plantingError))
            {
                ScreenToast.Show(plantingError);
                return false;
            }

            int slots = 1;
            var cropDb = Resources.Load<CropDatabase>("CropDatabase");
            var crop = cropDb != null ? cropDb.GetCropBySeedId(seedId) : null;
            if (crop != null) slots = Mathf.Max(1, crop.plotSlots);

            if (slots <= 1)
            {
                if (master.InteractPlant(seedId)) return true;
                ScreenToast.Show("\u00d4 \u0111\u1ea5t v\u1eeba thay \u0111\u1ed5i. H\u00e3y ch\u1ecdn l\u1ea1i \u00f4 gieo h\u1ea1t.");
                return false;
            }

            // Cây nhiều ô: cần thêm (slots-1) ô ĐÃ CUỐC & còn trống gần nhất.
            var extras = FindNearbyPlowedTiles(master, slots - 1);
            if (extras.Count < slots - 1)
            {
                ScreenToast.Show($"Cần {slots} ô đất đã cuốc để trồng {GetItemDisplayName(seedId)} (giàn) — còn thiếu {slots - 1 - extras.Count} ô.");
                return false;
            }

            if (!master.InteractPlant(seedId))
            {
                ScreenToast.Show("\u00d4 \u0111\u1ea5t v\u1eeba thay \u0111\u1ed5i. H\u00e3y ch\u1ecdn l\u1ea1i \u00f4 gieo h\u1ea1t.");
                return false;
            }
            foreach (var t in extras) t.OccupyAsSlot(master);
            master.RegisterSlaves(extras);
            Debug.Log($"[FarmInteraction] Trồng {seedId} chiếm {slots} ô (1 master + {extras.Count} ô giàn).");
            return true;
        }

        private bool TryValidatePlanting(FarmTile tile, string seedId, out string error)
        {
            error = "";
            if (tile == null || string.IsNullOrWhiteSpace(seedId))
            {
                error = "Kh\u00f4ng x\u00e1c \u0111\u1ecbnh \u0111\u01b0\u1ee3c \u00f4 \u0111\u1ea5t ho\u1eb7c h\u1ea1t gi\u1ed1ng.";
                return false;
            }
            if (tile.masterTile != null)
            {
                error = "\u00d4 \u0111\u1ea5t n\u00e0y \u0111ang thu\u1ed9c m\u1ed9t gi\u00e0n c\u00e2y. H\u00e3y ch\u1ecdn \u00f4 \u0111\u1ea5t tr\u1ed1ng kh\u00e1c.";
                return false;
            }
            if (tile.currentState != FarmTile.TileState.Plowed)
            {
                error = "\u00d4 \u0111\u1ea5t kh\u00f4ng c\u00f2n s\u1eb5n s\u00e0ng \u0111\u1ec3 gieo h\u1ea1t.";
                return false;
            }

            var cropDb = Resources.Load<CropDatabase>("CropDatabase");
            var crop = cropDb != null ? cropDb.GetCropBySeedId(seedId) : null;
            int slots = crop != null ? Mathf.Max(1, crop.plotSlots) : 1;
            if (slots <= 1) return true;

            int availableExtras = FindNearbyPlowedTiles(tile, slots - 1).Count;
            if (availableExtras >= slots - 1) return true;

            error = $"C\u1ea7n {slots} \u00f4 \u0111\u1ea5t \u0111\u00e3 cu\u1ed1c li\u1ec1n nhau \u0111\u1ec3 tr\u1ed3ng {GetItemDisplayName(seedId)}; " +
                    $"c\u1ee5m hi\u1ec7n t\u1ea1i c\u00f2n thi\u1ebfu {slots - 1 - availableExtras} \u00f4.";
            return false;
        }

        // Only connected, cardinally adjacent plowed tiles may belong to one multi-slot crop.
        /// <summary>
        /// Gom MỌI ô đất liền nhau với ô này thành một "mảnh ruộng" — kể cả ô đang trồng
        /// (khác FindNearbyPlowedTiles chỉ nhặt ô trống để đặt giàn). Dùng cho "Xem ruộng".
        /// Cùng thuật toán loang 4 hướng, có chặn lệch lưới và chặn khác tầng/khác đảo.
        /// </summary>
        private List<FarmTile> FindPlotTiles(FarmTile seed, int maxTiles = 400)
        {
            var result = new List<FarmTile>();
            if (seed == null) return result;

            FarmTile master = seed.masterTile != null ? seed.masterTile : seed;
            result.Add(master); // ô đang đứng cũng thuộc ruộng

            Vector2 spacing = GetFarmSlotSpacing(master);
            Vector3 origin = master.transform.position;
            var map = new Dictionary<Vector2Int, FarmTile>();
            foreach (var tile in FindObjectsByType<FarmTile>(FindObjectsSortMode.None))
            {
                if (tile == null || tile == master) continue;

                Vector3 position = tile.transform.position;
                float gridX = (position.x - origin.x) / spacing.x;
                float gridZ = (position.z - origin.z) / spacing.y;
                int x = Mathf.RoundToInt(gridX);
                int z = Mathf.RoundToInt(gridZ);
                if (Mathf.Abs(gridX - x) > 0.2f || Mathf.Abs(gridZ - z) > 0.2f) continue;
                if (Mathf.Abs(position.y - origin.y) > Mathf.Max(spacing.x, spacing.y)) continue;

                var key = new Vector2Int(x, z);
                if (key == Vector2Int.zero || map.ContainsKey(key)) continue;
                map[key] = tile;
            }

            var visited = new HashSet<Vector2Int> { Vector2Int.zero };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(Vector2Int.zero);
            while (queue.Count > 0 && result.Count < maxTiles)
            {
                Vector2Int current = queue.Dequeue();
                foreach (Vector2Int direction in FarmSlotDirections)
                {
                    Vector2Int next = current + direction;
                    if (!visited.Add(next) || !map.TryGetValue(next, out FarmTile tile)) continue;
                    result.Add(tile);
                    queue.Enqueue(next);
                    if (result.Count >= maxTiles) break;
                }
            }

            return result;
        }

        private List<FarmTile> FindNearbyPlowedTiles(FarmTile master, int count)
        {
            var result = new List<FarmTile>();
            if (master == null || count <= 0) return result;

            Vector2 spacing = GetFarmSlotSpacing(master);
            Vector3 origin = master.transform.position;
            var map = new Dictionary<Vector2Int, FarmTile>();
            foreach (var tile in FindObjectsByType<FarmTile>(FindObjectsSortMode.None))
            {
                if (tile == null || tile == master || !tile.IsPlowedFree) continue;

                Vector3 position = tile.transform.position;
                float gridX = (position.x - origin.x) / spacing.x;
                float gridZ = (position.z - origin.z) / spacing.y;
                int x = Mathf.RoundToInt(gridX);
                int z = Mathf.RoundToInt(gridZ);
                if (Mathf.Abs(gridX - x) > 0.2f || Mathf.Abs(gridZ - z) > 0.2f) continue;
                if (Mathf.Abs(position.y - origin.y) > Mathf.Max(spacing.x, spacing.y)) continue;

                var key = new Vector2Int(x, z);
                if (key == Vector2Int.zero || map.ContainsKey(key)) continue;
                map[key] = tile;
            }

            var visited = new HashSet<Vector2Int> { Vector2Int.zero };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(Vector2Int.zero);
            while (queue.Count > 0 && result.Count < count)
            {
                Vector2Int current = queue.Dequeue();
                foreach (Vector2Int direction in FarmSlotDirections)
                {
                    Vector2Int next = current + direction;
                    if (!visited.Add(next) || !map.TryGetValue(next, out FarmTile tile)) continue;
                    result.Add(tile);
                    queue.Enqueue(next);
                    if (result.Count >= count) break;
                }
            }

            return result;
        }

        private Vector2 GetFarmSlotSpacing(FarmTile tile)
        {
            GameObject building = ResolvePlacedBuildingRoot(tile);
            BuildSurfaceCell cell = BuildSurfaceCell.FindByOccupant(building != null ? building : tile.gameObject);
            Vector2 spacing = cell != null
                ? cell.FootprintSize
                : new Vector2(DefaultFarmSlotSpacing, DefaultFarmSlotSpacing);
            if (spacing.x < 0.05f) spacing.x = DefaultFarmSlotSpacing;
            if (spacing.y < 0.05f) spacing.y = DefaultFarmSlotSpacing;
            return spacing;
        }

        private void HandleWater(FarmTile tile, System.Action onWatered = null)
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

            // Phát clip nhanh hơn để múa TRỌN trong ~WateringActionDuration (khóa = clip/speed).
            float wateringSpeed = WateringClipDuration / Mathf.Max(0.1f, WateringActionDuration);
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

                    if (watered)
                    {
                        FarmStateSync.SaveTileState(tile);
                        onWatered?.Invoke();   // popup "Xem ruộng" tự mở lại để tưới tiếp cây khác
                    }
                    else
                    {
                        inv.AddItem("watering_water_01", 1);
                    }
                },
                () => inv.AddItem("watering_water_01", 1),
                wateringSpeed);

            if (!started)
                inv.AddItem("watering_water_01", 1);
        }

        // Múc nước ở ao (vùng WaterSource) → +xô nước vào túi.
        // Có animation "ScoopWater2" + khóa hành động + nút hủy (X) giống chặt/đào/tưới/cho ăn.
        // Nước chỉ vào túi KHI MÚA XONG — hủy giữa chừng thì không nhận.
        private void ScoopWater(WaterSource src)
        {
            if (src != null && !IsWaterSourceInRange(src, useDirectTapInteraction))
            {
                ScreenToast.Show("Äá»©ng gáº§n há»“ hÆ¡n Ä‘á»ƒ mÃºc nÆ°á»›c.");
                return;
            }

            // CHẶN SPAM: đang múa động tác khác thì bỏ qua (BeginTimedAction cũng chặn, guard sớm cho gọn).
            if (PlayerController.Instance != null && PlayerController.Instance.IsBusy) return;

            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv == null) return;
            int amt = src != null ? Mathf.Max(10, src.amountPerScoop) : 10;

            Vector3 facePoint = src != null ? src.transform.position : transform.position;
            // Phát clip nhanh hơn để múa TRỌN trong ~ScoopWaterActionDuration (khóa = clip/speed).
            float scoopSpeed = ScoopWaterClipDuration / Mathf.Max(0.1f, ScoopWaterActionDuration);
            BeginTimedAction(
                "ScoopWater2",
                ScoopWaterClipDuration,
                YWonderLand.Player.ToolType.WaterBucket, // XÔ tay TRÁI (khác bình tưới tay phải)
                facePoint,
                () =>
                {
                    inv.AddItem("watering_water_01", amt);
                    int total = inv.GetItemQuantity("watering_water_01");
                    ScreenToast.ShowItemReward("watering_water_01", amt, "Múc nước", $"(Tổng: {total})");
                },
                null,
                scoopSpeed);
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
                    ScreenToast.ShowItemReward(tile.LastFinalProductId, tile.LastFinalProductAmount, "Vụ cuối");
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

                    // Add Point reward (giảm theo độ chăm sóc)
                    if (YWonderLand.Managers.EconomyManager.Instance != null)
                    {
                        int pos = Mathf.RoundToInt(crop.posReward * care);
                        YWonderLand.Managers.EconomyManager.Instance.AddPOS(pos);
                        Debug.Log($"[FarmInteraction] +{pos} Point (care {care:0.00})");
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
                        ScreenToast.ShowInfoForItem(
                            harvestId,
                            $"Thu hoạch: +{amount} {GetItemDisplayName(harvestId)} (thiếu nước -{Mathf.RoundToInt((1f - care) * 100f)}%)");
                    }
                    else
                    {
                        ScreenToast.ShowItemReward(harvestId, amount, "Thu hoạch");
                    }
                }
                else
                {
                    // Cây không có CropDefinition (vd cây lâu năm khung tạm) — vẫn báo thu hoạch.
                    ScreenToast.ShowItemReward(harvestId, amount, "Thu hoạch");
                }

                FarmStateSync.SaveTileState(tile);
            }
        }
    }
}
