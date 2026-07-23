using UnityEngine;
using System;
using YWonderLand.Data;
using YWonderLand.Backend;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Logic vật nuôi: ĐÓI và RA SẢN PHẨM tính theo MỐC THỜI GIAN (đồng hồ toàn cục
    /// RealNow()) GIỐNG CÂY (FarmTile.growStartTime) — đi đảo thành phố về vẫn
    /// "chạy bù" đúng thời gian đã trôi, không phụ thuộc Update mỗi frame.
    ///
    /// Gắn được lên CẢ prefab model thật (gọi Initialize(def, false) để KHÔNG sinh khối
    /// primitive — chỉ thêm thanh HP nổi trên đầu) lẫn fallback không có prefab
    /// (Initialize(def) sinh khối primitive như cũ).
    /// </summary>
    public class FarmAnimal : MonoBehaviour
    {
        private const string WorldBarMaterialResourcePath = "Materials/WorldBar_Unlit";
        private static Material worldBarMaterialTemplate;
        private static bool worldBarMaterialLogged;

        public enum AnimalState
        {
            Healthy = 0,
            Hungry = 1,
            Sick = 2,
            Dead = 3
        }

        [Header("Identity")]
        public string animalInstanceId; // For saving
        public AnimalDefinition data;

        [Header("State")]
        public AnimalState currentState = AnimalState.Healthy;
        // feedTimer / produceTimer GIỮ LẠI cho tương thích AnimalManager save/load (hệ chuồng cũ).
        // Logic mới KHÔNG cộng dồn 2 trường này mỗi frame — tính trực tiếp từ mốc thời gian bên dưới.
        public float feedTimer = 0f;
        public float produceTimer = 0f;
        public int harvestsRemaining;
        public bool hasProductReady = false;
        public bool isVaccinated = false;
        public bool LastHarvestWasFinal { get; private set; }

        // ── BỆNH / VẮC-XIN — theo CÔNG THỨC VatNuoi2 + CachTinh ──
        // Mốc phát bệnh = "Thời điểm phát bệnh" (hệ số 0.15–0.5) × "Số ngày nuôi".
        // Tới mốc thì GIEO XÁC SUẤT "Tỉ lệ phát bệnh" (0.3–0.6) — MỘT lần cho cả vòng nuôi,
        // khớp công thức chi phí của CachTinh (thuốc chỉ tính 1 lần, nhân với tỉ lệ phát bệnh).
        // Một mũi vắc-xin phòng được = Số ngày nuôi ÷ "Số lượng Vắc-xin cần".
        // 2 field dưới chỉ để ÉP số khi test — >0 là đè lên số của loài.
        [Header("Bệnh / Vắc-xin — ÉP SỐ để test (0 = theo VatNuoi2)")]
        [Tooltip("ÉP mốc phát bệnh (giây). 0 = tính theo loài: hệ số × số ngày nuôi.")]
        [SerializeField] private float sicknessOnsetSec = 0f;
        [Tooltip("ÉP thời gian một mũi vắc-xin (giây). 0 = tính theo loài: số ngày nuôi ÷ số mũi.")]
        [SerializeField] private float vaccineProtectSec = 0f;

        /// <summary>Mốc HẾT HẠN vắc-xin (unix giây). now &lt; mốc này = đang được bảo vệ.</summary>
        private double vaccineUntilUnix = 0.0;
        /// <summary>Mốc bắt đầu vòng nuôi (để đếm tới thời điểm phát bệnh). Đặt khi thả, KHÔNG reset khi tiêm.</summary>
        private double sickRefUnix = 0.0;
        /// <summary>Đã gieo xác suất phát bệnh cho vòng nuôi này chưa (mỗi vòng chỉ gieo MỘT lần).</summary>
        private bool sicknessRolled = false;

        // Số ngày nuôi cả vòng đời. Loài chưa đổ số (asset cũ) → 90 ngày cho khỏi chia 0.
        private float RaisingDays => (data != null && data.raisingDays > 0f) ? data.raisingDays : 90f;

        private float SicknessOnsetSec
        {
            get
            {
                if (sicknessOnsetSec > 0f) return sicknessOnsetSec;
                float ratio = (data != null && data.sicknessOnsetRatio > 0f) ? data.sicknessOnsetRatio : 0.3f;
                return YWonderLand.Core.GameTimeConfig.Days(RaisingDays * ratio);
            }
        }

        private float VaccineProtectSec
        {
            get
            {
                if (vaccineProtectSec > 0f) return vaccineProtectSec;
                int doses = (data != null && data.vaccineDosesPerCycle > 0) ? data.vaccineDosesPerCycle : 4;
                return YWonderLand.Core.GameTimeConfig.Days(RaisingDays / doses);
            }
        }

        /// <summary>Xác suất phát bệnh khi tới mốc (VatNuoi2 "Tỉ lệ phát bệnh").</summary>
        private float SicknessChance => (data != null && data.sicknessChance > 0f) ? data.sicknessChance : 0.4f;

        /// <summary>Số LIỀU thuốc tốn để chữa khỏi con này (VatNuoi2: bò 10, vịt 2...).</summary>
        public int MedicineDosesPerCure => Mathf.Max(1, data != null ? data.medicineDosesPerCure : 1);

        /// <summary>Vắc-xin còn hiệu lực (đang được phòng bệnh).</summary>
        public bool IsVaccineActive => RealNow() < vaccineUntilUnix;
        /// <summary>Số giây vắc-xin còn hiệu lực (0 = hết/chưa tiêm).</summary>
        public float VaccineRemainingSec => Mathf.Max(0f, (float)(vaccineUntilUnix - RealNow()));
        /// <summary>Số giây còn lại tới mốc gieo bệnh (float.MaxValue = đã qua mốc / loài không bệnh).</summary>
        public float SicknessRemainingSec
        {
            get
            {
                if (data == null || !data.canGetSick) return float.MaxValue;
                if (currentState == AnimalState.Sick || currentState == AnimalState.Dead) return 0f;
                if (sicknessRolled) return float.MaxValue; // đã qua mốc, cả vòng nuôi không bệnh nữa
                return Mathf.Max(0f, SicknessOnsetSec - (float)(RealNow() - sickRefUnix));
            }
        }

        // ── MỐC THỜI GIAN (đồng hồ toàn cục) ──
        // No đầy lại từ lúc này; đói dần tới khi (now - feedRefTime) >= cửa sổ sống (noFeedDeathSec chưa ăn / fedLifeSec đã ăn).
        private double feedRefTime;
        // Bắt đầu chu kỳ sản phẩm hiện tại từ lúc này.
        private double produceRefTime;
        // Đã cho ăn lần nào chưa: false → dùng noFeedDeathSec (vd 24h); true → fedLifeSec (vd 48h). Khách chốt thanh-máu.
        private bool hasBeenFed = false;

        [Header("Thanh HP (no/đói) nổi trên đầu")]
        [Tooltip("Chiều cao thanh HP so với gốc con vật (m). 0 = tự đo theo model.")]
        public float statusBarHeight = 0f;

        // Ô chuồng con vật đang chiếm (FarmInteractionController gán lúc thả) — để GIẢI PHÓNG khi làm thịt vụ cuối.
        [HideInInspector] public System.Collections.Generic.List<BuildSurfaceCell> occupiedCells;

        private AnimalPen currentPen;

        // Visuals
        private GameObject visualObject;     // chỉ tạo cho fallback primitive
        private bool ownsPrimitiveBody = false;

        // Thanh HP nổi
        private Transform barRoot;           // billboard quay về camera
        private Transform hungerFillPivot;   // pivot trái để fill mọc từ trái
        private Renderer hungerFillRenderer;
        private GameObject productIndicator;  // dấu "có sản phẩm"
        private Camera barCamera;

        private const float BAR_W = 0.8f;
        private const float BAR_H = 0.12f;

        public event Action<FarmAnimal> OnAnimalStateChanged;

        /// <summary>Bắn khi con vật vừa được CHO ĂN (khác OnAnimalStateChanged bắn cho mọi thay đổi). Dùng cho tutorial.</summary>
        public static event Action<FarmAnimal> OnAnimalFed;

        /// <summary>Bắn khi con vật vừa được THẢ vào chuồng (flow mới BuildSurfaceCell). Dùng cho tutorial bước "Thả thú".</summary>
        public static event Action<FarmAnimal> OnAnimalSpawned;
        public static void RaiseSpawned(FarmAnimal a) => OnAnimalSpawned?.Invoke(a);

        void Awake()
        {
            if (string.IsNullOrEmpty(animalInstanceId))
            {
                animalInstanceId = Guid.NewGuid().ToString();
            }
        }

        public void Initialize(AnimalDefinition def)
        {
            Initialize(def, true);
        }

        /// <param name="createPrimitiveBody">
        /// true: tự sinh khối primitive (fallback khi chưa có model). false: đã có prefab model
        /// thật rồi — chỉ thêm thanh HP, KHÔNG sinh khối.
        /// </param>
        public void Initialize(AnimalDefinition def, bool createPrimitiveBody)
        {
            data = def;
            harvestsRemaining = def != null ? def.maxHarvests : 0;
            currentState = AnimalState.Healthy;
            feedTimer = 0f;
            produceTimer = 0f;
            hasProductReady = false;
            isVaccinated = false;

            feedRefTime = RealNow();
            produceRefTime = RealNow();
            hasBeenFed = false;
            vaccineUntilUnix = 0.0;   // thả ra là chưa tiêm
            sickRefUnix = RealNow();  // mốc bắt đầu vòng nuôi (gốc tính thời điểm phát bệnh)
            sicknessRolled = false;   // chưa gieo xác suất bệnh cho vòng nuôi này

            ownsPrimitiveBody = createPrimitiveBody;
            if (createPrimitiveBody) CreatePrimitiveBody();
            EnsureCollider();
            CreateStatusBar();
            UpdateVisuals();
        }

        public void SetPen(AnimalPen pen)
        {
            currentPen = pen;
        }

        void Update()
        {
            if (data == null || currentState == AnimalState.Dead) return;

            double now = RealNow();
            float window = CurrentHungerWindow(); // chưa ăn → noFeedDeathSec; đã ăn → fedLifeSec
            feedTimer = (float)(now - feedRefTime); // cache cho save cũ

            // ── CHẾT ĐÓI khi thanh máu cạn (khách chốt thanh-máu). 'Bệnh' nay là hệ RIÊNG, KHÔNG set từ đói. ──
            if (window > 0f && (now - feedRefTime) >= window)
            {
                DieFromHunger(); // khách chốt: chết là BIẾN MẤT + trả ô chuồng, không để xác
                return;
            }

            // Cảnh báo ĐÓI (thanh máu < 50%) — giữ trạng thái Bệnh nếu đang bệnh (hệ bệnh lo riêng).
            if (currentState != AnimalState.Sick)
            {
                AnimalState hungerState = (window > 0f && GetHungerFraction() < 0.5f)
                    ? AnimalState.Hungry : AnimalState.Healthy;
                if (hungerState != currentState)
                {
                    currentState = hungerState;
                    UpdateVisuals();
                    OnAnimalStateChanged?.Invoke(this);
                }
            }

            // ── MỐC PHÁT BỆNH (VatNuoi2): tới "hệ số × số ngày nuôi" thì GIEO XÁC SUẤT một lần.
            // Đang có vắc-xin che ở đúng mốc này = thoát bệnh cả vòng nuôi (đó là công dụng của vắc-xin).
            if (!sicknessRolled
                && currentState != AnimalState.Sick
                && data.canGetSick
                && !IsTutorialActive()
                && (now - sickRefUnix) >= SicknessOnsetSec)
            {
                sicknessRolled = true; // mỗi vòng nuôi chỉ gieo MỘT lần, khớp công thức chi phí CachTinh
                if (!IsVaccineActive && UnityEngine.Random.value < SicknessChance)
                    BecomeSick();
            }

            // ── RA SẢN PHẨM theo mốc thời gian (độc lập với đói; chỉ dừng khi Bệnh/Chết) ──
            if (CanProduce() && !hasProductReady)
            {
                double produceElapsed = now - produceRefTime;
                produceTimer = (float)produceElapsed;
                if (produceElapsed >= Mathf.Max(0.1f, data.produceCycleTimeSec))
                {
                    hasProductReady = true;
                    UpdateVisuals();
                    OnAnimalStateChanged?.Invoke(this);
                }
            }
        }

        void LateUpdate()
        {
            BillboardStatusBar();
        }

        // ── Truy vấn cho UI (popup hiện số) ──

        /// <summary>Độ NO 0..1 (1 = vừa ăn no, 0 = đói). Dùng cho thanh HP.</summary>
        public float GetHungerFraction()
        {
            if (data == null) return 1f;
            float window = CurrentHungerWindow();
            if (window <= 0f) return 1f;
            double elapsed = RealNow() - feedRefTime;
            return Mathf.Clamp01(1f - (float)(elapsed / window));
        }

        /// <summary>Cửa sổ SỐNG hiện tại (giây): chưa cho ăn → noFeedDeathSec; đã cho ăn → fedLifeSec. 0 = không chết đói.</summary>
        private float CurrentHungerWindow()
        {
            if (data == null) return 0f;
            return hasBeenFed ? data.fedLifeSec : data.noFeedDeathSec;
        }

        /// <summary>Đang trong Tutorial? (ép KHÔNG chết đói để người mới khỏi nản — giống cây).</summary>
        private bool IsTutorialActive()
        {
            var tm = FindFirstObjectByType<TutorialManager>();
            return tm != null && tm.IsActive();
        }

        public bool IsInfiniteHarvest => data != null && data.maxHarvests <= 0;

        public int MaxHarvests => data != null ? data.maxHarvests : 0;

        /// <summary>Còn bao nhiêu giây tới vụ sản phẩm kế. 0 = đã chín; -1 = hết vụ / không sản xuất.</summary>
        public float GetTimeToNextProduceSec()
        {
            if (!CanProduce()) return -1f;
            if (hasProductReady) return 0f;
            float cycle = Mathf.Max(0.1f, data.produceCycleTimeSec);
            double elapsed = RealNow() - produceRefTime;
            return Mathf.Max(0f, cycle - (float)elapsed);
        }

        private bool CanProduce()
        {
            if (data == null) return false;
            if (currentState == AnimalState.Sick || currentState == AnimalState.Dead) return false;
            return IsInfiniteHarvest || harvestsRemaining > 0;
        }

        // ── Interactions ──

        public bool Feed()
        {
            if (currentState == AnimalState.Dead) return false;

            feedRefTime = RealNow(); // no đầy lại (cửa sổ fedLifeSec)
            feedTimer = 0f;
            hasBeenFed = true;               // từ giờ dùng cửa sổ fedLifeSec (dài hơn noFeedDeathSec)
            if (currentState == AnimalState.Hungry)
            {
                currentState = AnimalState.Healthy;
            }
            UpdateVisuals();
            OnAnimalStateChanged?.Invoke(this);
            OnAnimalFed?.Invoke(this);
            return true;
        }

        public bool HarvestProduct(out string itemId, out int amount)
        {
            itemId = "";
            amount = 0;
            LastHarvestWasFinal = false;

            if (!hasProductReady || currentState == AnimalState.Dead) return false;

            itemId = data.produceItemId;
            amount = data.produceAmount;

            hasProductReady = false;
            produceRefTime = RealNow(); // bắt đầu chu kỳ kế từ bây giờ
            produceTimer = 0f;
            if (!IsInfiniteHarvest) harvestsRemaining--;
            LastHarvestWasFinal = !IsInfiniteHarvest && harvestsRemaining <= 0;

            UpdateVisuals();
            OnAnimalStateChanged?.Invoke(this);

            // VỤ CUỐI: hết số lần thu → cho THỊT (Pro2) + làm thịt con vật (biến mất, trả ô chuồng).
            if (LastHarvestWasFinal)
            {
                SlaughterForMeat();
            }
            return true;
        }

        /// <summary>Vụ cuối: cộng thịt vào túi, giải phóng ô chuồng, xoá con vật.</summary>
        private void SlaughterForMeat()
        {
            if (data != null && !string.IsNullOrEmpty(data.meatItemId) && data.meatAmount > 0)
            {
                var inv = YWonderLand.Managers.InventoryManager.Instance;
                if (inv != null) inv.AddItem(data.meatItemId, data.meatAmount);

                string meatName = !string.IsNullOrEmpty(data.productAltName) ? data.productAltName : "thịt";
                ScreenToast.ShowInfoForItem(
                    data.meatItemId,
                    $"Làm thịt {data.animalName}: +{data.meatAmount} {meatName}",
                    fallbackText: "Meat");
            }

            // Trả ô chuồng về trống (rào vẫn còn) để thả con mới.
            if (occupiedCells != null)
            {
                foreach (var c in occupiedCells)
                    if (c != null) c.ClearAnimal();
                occupiedCells = null;
            }

            Destroy(gameObject);
        }

        /// <summary>Chết ĐÓI (khách chốt): báo toast, TRẢ ô chuồng về trống, rồi XOÁ con vật — KHÔNG để lại xác.</summary>
        private void DieFromHunger()
        {
            if (data != null)
                ScreenToast.Show($"{data.animalName} đã chết đói! Nhớ cho ăn đúng giờ.");

            if (currentPen != null)
            {
                currentPen.RemoveAnimal(this);
                currentPen = null;
            }

            // Trả ô chuồng về trống (rào vẫn còn) để thả con mới — không để xác kẹt ô.
            if (occupiedCells != null)
            {
                foreach (var c in occupiedCells)
                    if (c != null) c.ClearAnimal();
                occupiedCells = null;
            }

            currentState = AnimalState.Dead;
            OnAnimalStateChanged?.Invoke(this); // báo popup/listener cập nhật trước khi xoá
            FarmStateSync.SaveRuntimeState();
            Destroy(gameObject);
        }

        public bool Pet()
        {
            if (currentState == AnimalState.Dead) return false;
            return true;
        }

        /// <summary>Thú đổ bệnh: ngừng ra sản phẩm (CanProduce chặn) cho tới khi chữa bằng thuốc.</summary>
        private void BecomeSick()
        {
            if (currentState == AnimalState.Sick || currentState == AnimalState.Dead) return;

            currentState = AnimalState.Sick;
            UpdateVisuals();
            OnAnimalStateChanged?.Invoke(this);
            if (data != null)
                ScreenToast.Show($"{data.animalName} bị bệnh! Dùng Thuốc để chữa, rồi tiêm Vắc-xin phòng tiếp.");
            FarmStateSync.SaveRuntimeState();
        }

        /// <summary>TIÊM VẮC-XIN (phòng bệnh) — KHÔNG chữa được thú đang bệnh (phải dùng Thuốc trước).
        /// Caller (UI) chịu trách nhiệm trừ item vaccine_01 khi hàm này trả true.</summary>
        public bool Vaccinate()
        {
            if (currentState == AnimalState.Dead) return false;
            if (currentState == AnimalState.Sick) return false; // đang bệnh thì phải chữa trước

            // CHỈ dời hạn bảo vệ. KHÔNG reset sickRefUnix — mốc phát bệnh là điểm CỐ ĐỊNH trong vòng nuôi
            // (hệ số × số ngày nuôi); reset đi thì 1 mũi tiêm né được bệnh vĩnh viễn.
            vaccineUntilUnix = RealNow() + VaccineProtectSec;
            isVaccinated = true;

            UpdateVisuals();
            OnAnimalStateChanged?.Invoke(this);
            FarmStateSync.SaveRuntimeState();
            return true;
        }

        /// <summary>CHỮA BỆNH bằng thuốc — chỉ có tác dụng khi đang Bệnh. KHÔNG tạo miễn dịch
        /// (muốn phòng tiếp thì tiêm vắc-xin). Caller (UI) trừ item medicine_01 khi trả true.</summary>
        public bool Heal()
        {
            if (currentState != AnimalState.Sick) return false;

            currentState = AnimalState.Healthy;
            sicknessRolled = true; // đã tiêu tiền thuốc cho vòng nuôi này → không gieo bệnh lại (đúng CachTinh)

            UpdateVisuals();
            OnAnimalStateChanged?.Invoke(this);
            FarmStateSync.SaveRuntimeState();
            return true;
        }

        // ── Thanh HP (no/đói) nổi trên đầu — billboard, không cần artist ──

        private void CreateStatusBar()
        {
            if (barRoot != null) return;

            float h = statusBarHeight > 0f ? statusBarHeight : ComputeAutoBarHeight();

            var rootGo = new GameObject("StatusBar");
            barRoot = rootGo.transform;
            barRoot.SetParent(transform, false);
            barRoot.localPosition = new Vector3(0f, h, 0f);

            // Nền thanh (xám đậm)
            CreateBarQuad(barRoot, "BarBG", new Vector3(0f, 0f, 0f),
                new Vector3(BAR_W, BAR_H, 1f), new Color(0.12f, 0.12f, 0.14f, 1f));

            // Pivot trái để fill mọc từ trái sang phải
            var pivotGo = new GameObject("FillPivot");
            hungerFillPivot = pivotGo.transform;
            hungerFillPivot.SetParent(barRoot, false);
            hungerFillPivot.localPosition = new Vector3(-BAR_W * 0.5f, 0f, -0.01f);

            var fill = CreateBarQuad(hungerFillPivot, "Fill", new Vector3(BAR_W * 0.5f, 0f, 0f),
                new Vector3(BAR_W, BAR_H * 0.78f, 1f), new Color(0.3f, 0.85f, 0.3f, 1f));
            hungerFillRenderer = fill.GetComponent<Renderer>();

            // Dấu "có sản phẩm" (chấm vàng nhỏ trên thanh)
            productIndicator = CreateBarQuad(barRoot, "ProductReady",
                new Vector3(0f, BAR_H * 1.4f, -0.01f), new Vector3(0.22f, 0.22f, 1f),
                new Color(1f, 0.85f, 0.1f, 1f));
            productIndicator.SetActive(false);

            barCamera = Camera.main;
        }

        private float ComputeAutoBarHeight()
        {
            float top = 1.2f;
            var renderers = GetComponentsInChildren<Renderer>();
            bool found = false;
            float maxY = float.MinValue;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (barRoot != null && r.transform.IsChildOf(barRoot)) continue;
                maxY = Mathf.Max(maxY, r.bounds.max.y);
                found = true;
            }
            if (found) top = (maxY - transform.position.y) + 0.3f;
            return Mathf.Clamp(top, 0.6f, 4f);
        }

        private GameObject CreateBarQuad(Transform parent, string name, Vector3 localPos, Vector3 localScale, Color color)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = localPos;
            quad.transform.localScale = localScale;

            var col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col); // không chắn tia ngắm

            var r = quad.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = CreateWorldBarMaterial(color);
                if (mat != null)
                {
                    mat.SetFloat("_Cull", 0f); // 2 mặt — luôn thấy dù quay hướng nào
                    r.material = mat;
                }
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            return quad;
        }

        private static Material CreateWorldBarMaterial(Color color)
        {
            if (worldBarMaterialTemplate == null)
                worldBarMaterialTemplate = Resources.Load<Material>(WorldBarMaterialResourcePath);

            Material mat = null;
            if (worldBarMaterialTemplate != null)
            {
                mat = new Material(worldBarMaterialTemplate);
            }
            else
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader != null) mat = new Material(shader);
            }

            if (mat == null)
            {
                Shader fallbackShader = Shader.Find("Sprites/Default");
                if (fallbackShader == null) fallbackShader = Shader.Find("Hidden/Internal-Colored");
                if (fallbackShader != null) mat = new Material(fallbackShader);
            }

            LogWorldBarMaterial("FarmAnimal", mat);
            ApplyWorldBarColor(mat, color);
            return mat;
        }

        private static void LogWorldBarMaterial(string owner, Material mat)
        {
            if (worldBarMaterialLogged) return;
            worldBarMaterialLogged = true;

            string shaderName = mat != null && mat.shader != null ? mat.shader.name : "null";
            bool loadedTemplate = worldBarMaterialTemplate != null;
            Debug.Log($"[WorldBar] {owner}: templateLoaded={loadedTemplate}, resource='{WorldBarMaterialResourcePath}', shader='{shaderName}'");

            if (!loadedTemplate || string.IsNullOrEmpty(shaderName) || !shaderName.Contains("Unlit"))
                Debug.LogWarning($"[WorldBar] {owner}: material template/shader may be wrong. Expected Resources/{WorldBarMaterialResourcePath} using an Unlit shader.");
        }

        private static void ApplyWorldBarColor(Material mat, Color color)
        {
            if (mat == null) return;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        }

        private void BillboardStatusBar()
        {
            if (barRoot == null) return;
            if (barCamera == null) barCamera = Camera.main;
            if (barCamera == null) return;

            // Ẩn thanh khi chết
            bool show = currentState != AnimalState.Dead;
            if (barRoot.gameObject.activeSelf != show) barRoot.gameObject.SetActive(show);
            if (!show) return;

            // Quay mặt thanh theo hướng camera (billboard phẳng).
            barRoot.rotation = Quaternion.LookRotation(barCamera.transform.forward, barCamera.transform.up);

            RefreshHungerFill(); // thanh đói tụt LIÊN TỤC mỗi frame, không chỉ lúc đổi trạng thái

            // Pulse nhẹ dấu sản phẩm
            if (productIndicator != null && productIndicator.activeSelf)
            {
                float p = 0.9f + 0.2f * Mathf.PingPong(Time.time * 1.6f, 1f);
                productIndicator.transform.localScale = new Vector3(0.22f * p, 0.22f * p, 1f);
            }
        }

        // ── Visuals ──

        public void UpdateVisuals()
        {
            // Thanh HP fill: cập nhật LIÊN TỤC ở LateUpdate (RefreshHungerFill) — đây chỉ refresh 1 phát lúc đổi trạng thái.
            RefreshHungerFill();

            if (productIndicator != null)
                productIndicator.SetActive(hasProductReady && currentState != AnimalState.Dead);

            // Fallback primitive: nằm nghiêng khi chết
            if (ownsPrimitiveBody && visualObject != null)
            {
                if (currentState == AnimalState.Dead)
                {
                    visualObject.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                    visualObject.transform.localPosition = new Vector3(0.5f, -0.4f, 0);
                }
                else
                {
                    visualObject.transform.localRotation = Quaternion.identity;
                    visualObject.transform.localPosition = Vector3.zero;
                }
            }
        }

        /// <summary>Cập nhật fill + màu thanh đói theo độ no hiện tại. Gọi MỖI FRAME (đói tụt liên tục, khớp popup).</summary>
        private void RefreshHungerFill()
        {
            if (hungerFillPivot == null) return;
            float frac = GetHungerFraction();
            hungerFillPivot.localScale = new Vector3(Mathf.Max(0.0001f, frac), 1f, 1f);
            if (hungerFillRenderer != null)
            {
                Color c = Color.Lerp(new Color(0.9f, 0.25f, 0.2f), new Color(0.3f, 0.85f, 0.3f), frac);
                if (currentState == AnimalState.Sick) c = new Color(0.6f, 0.4f, 0.85f); // tím = bệnh
                ApplyWorldBarColor(hungerFillRenderer.material, c);
            }
        }

        private void EnsureCollider()
        {
            // Để tia ngắm bắt được FarmAnimal: cần ÍT NHẤT 1 collider trong cây con.
            if (GetComponentInChildren<Collider>() != null) return;
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.5f, 0);
            col.size = new Vector3(1f, 1f, 1f);
        }

        private void CreatePrimitiveBody()
        {
            if (visualObject != null) return;

            visualObject = new GameObject("Visuals");
            visualObject.transform.SetParent(this.transform, false);

            GameObject body;
            Color bodyColor = Color.white;

            string id = data != null ? data.animalId : "";
            if (id.Contains("chicken"))
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                bodyColor = Color.white;
            }
            else if (id.Contains("cow"))
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.transform.localScale = new Vector3(1.5f, 1f, 1f);
                bodyColor = new Color(0.9f, 0.9f, 0.9f);
            }
            else if (id.Contains("pig"))
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.transform.localScale = new Vector3(1f, 0.8f, 1f);
                bodyColor = new Color(1f, 0.7f, 0.8f);
            }
            else
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }

            body.transform.SetParent(visualObject.transform, false);
            body.transform.localPosition = new Vector3(0, 0.5f, 0);

            Renderer r = body.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                r.material.color = bodyColor;
            }

            // Collider raycast trên root (xoá collider của body để tia trúng root).
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.5f, 0);
            col.size = new Vector3(1.5f, 1.5f, 1.5f);
            var bodyCol = body.GetComponent<Collider>();
            if (bodyCol != null) Destroy(bodyCol);
        }

        // ── Load state (hệ chuồng cũ AnimalManager) ──
        public void LoadState(AnimalState state, float fTimer, float pTimer, int harvests, bool ready, bool vacc)
        {
            currentState = state;
            feedTimer = fTimer;
            produceTimer = pTimer;
            harvestsRemaining = harvests;
            hasProductReady = ready;
            isVaccinated = vacc;
            hasBeenFed = true; // con vật load lại → coi như đã cho ăn (dùng fedLifeSec)
            // Save kiểu CŨ chỉ có cờ bool: cờ bật → cấp lại 1 kỳ bảo vệ; đếm ủ bệnh lại từ bây giờ
            // (tránh vừa load đã đổ bệnh oan).
            vaccineUntilUnix = vacc ? RealNow() + VaccineProtectSec : 0.0;
            sickRefUnix = RealNow();
            sicknessRolled = false;

            // Quy đổi tiến trình đã lưu (giây) về mốc thời gian tương đương để chạy tiếp mượt.
            feedRefTime = RealNow() - fTimer;
            produceRefTime = RealNow() - pTimer;

            if (ownsPrimitiveBody && visualObject == null) CreatePrimitiveBody();
            if (barRoot == null) CreateStatusBar();
            UpdateVisuals();
        }

        // ───────────────── PERSISTENCE (real-time, đói-bù / chết-bù) ─────────────────

        /// <summary>Mốc thời gian ĐỜI THỰC (Unix giây). Thay Time.timeAsDouble (reset khi đóng app) để thú
        /// đói-bù + chết-bù đúng qua các phiên. ⚠️ Chỉnh giờ máy có thể tua (chống gian lận = server-time, Phase sau).</summary>
        private static double RealNow() => System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

        // Accessor cho persistence đọc mốc thời gian (private) — lưu ra đĩa.
        public double FeedRefUnix => feedRefTime;
        public double ProduceRefUnix => produceRefTime;
        public bool HasBeenFed => hasBeenFed;
        public double VaccineUntilUnix => vaccineUntilUnix;
        public double SickRefUnix => sickRefUnix;
        public bool SicknessRolled => sicknessRolled;

        /// <summary>Khôi phục trạng thái thú từ save (persistence): set thẳng mốc Unix → Update tự đói-bù/chết-bù
        /// theo thời gian thực đã trôi (gọi SAU Initialize, đè lại mốc no/sản phẩm).</summary>
        public void RestoreAnimalState(double feedRefUnix, double produceRefUnix, bool fed, int harvests, bool product, bool vacc,
            double vaccineUntil = 0.0, double sickRef = 0.0, int stateInt = -1, bool rolled = false)
        {
            feedRefTime = feedRefUnix;
            produceRefTime = produceRefUnix;
            hasBeenFed = fed;
            harvestsRemaining = harvests;
            hasProductReady = product;
            isVaccinated = vacc;
            // Save CŨ chưa có 2 mốc này → mốc vòng nuôi tính từ bây giờ để không giết oan thú đang nuôi.
            vaccineUntilUnix = vaccineUntil;
            sickRefUnix = sickRef > 0.0 ? sickRef : RealNow();
            sicknessRolled = rolled;
            if (stateInt >= 0 && stateInt <= (int)AnimalState.Dead)
            {
                var saved = (AnimalState)stateInt;
                if (saved == AnimalState.Sick) currentState = AnimalState.Sick; // giữ nguyên đang bệnh qua các phiên
            }
            feedTimer = (float)(RealNow() - feedRefTime);
            produceTimer = (float)(RealNow() - produceRefTime);
            UpdateVisuals();
        }
    }
}
