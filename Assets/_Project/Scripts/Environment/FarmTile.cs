using UnityEngine;
using System;
using YWonderLand.Data;
using YWonderLand.Environment; // ScreenToast (toast báo cây chết)

/// <summary>
/// FarmTile nâng cấp: tích hợp CropDefinition, visual stages theo loại cây,
/// growth time từ CropDatabase, thu hoạch trả produce ID + yield từ data.
/// Tương thích ngược với TutorialManager (vẫn giữ tutorialGrowthTime fallback).
/// </summary>
public class FarmTile : MonoBehaviour
{
    private const string WorldBarMaterialResourcePath = "Materials/WorldBar_Unlit";
    private static Material worldBarMaterialTemplate;
    private static bool worldBarMaterialLogged;

    public enum TileState
    {
        Soil,        // Đất thường chưa cuốc
        Plowed,      // Đất đã cuốc, sẵn sàng gieo hạt
        Planted,     // Đất đã gieo hạt
        Watered,     // Đã gieo hạt & đã tưới nước (bắt đầu lớn)
        Ripe         // Cây chín, sẵn sàng thu hoạch
    }

    [Header("Tile Info")]
    public TileState currentState = TileState.Soil;
    public string plantedSeedId = "";

    [Header("Visual Models (Assign in Inspector)")]
    public GameObject soilVisual;
    public GameObject plowedVisual;
    public GameObject seedVisual;      // Visual showing small sprout
    public GameObject growingVisual;   // Visual showing half grown plant
    public GameObject ripeVisual;      // Visual showing final product

    [Header("Timing Configuration")]
    public float tutorialGrowthTime = 24f; // Fallback cho tutorial (tua nhanh 24s) — khớp override ở GetGrowthTime

    [Header("Model 3D")]
    [Tooltip("BẬT nếu ô đất này dùng prefab cây 3D (gán ở CropDefinition) thay khối primitive. Khi BẬT: KHÔNG tạo khối đất/cây mặc định — phần đất do model mảnh đất của bạn lo.")]
    [SerializeField] private bool useCustomCropModels = false;

    [Header("Crop Data (Auto-assigned)")]
    [SerializeField] private CropDefinition currentCrop;

    // Tile index for FarmManager tracking
    [HideInInspector] public int tileIndex = -1;

    // Events
    public Action<FarmTile> OnTilePlowed;
    public Action<FarmTile> OnTilePlanted;
    public Action<FarmTile> OnTileWatered;
    public Action<FarmTile> OnTileHarvested;

    // MỐC THỜI GIAN bắt đầu lớn (giây, theo đồng hồ TOÀN CỤC RealNow()).
    // % lớn = (giờ hiện tại − mốc này) / thời_gian_lớn → KHÔNG phụ thuộc Update mỗi frame,
    // nên đi thành phố / tắt ô đất cho nhẹ máy thì về farm cây vẫn "lớn bù" đúng thời gian đã trôi.
    private double growStartTime = 0.0;  // mốc bắt đầu LỚN = lần TƯỚI ĐẦU (24h tính từ đây, khách chốt)
    private bool isGrowing = false;

    // ── Thanh nước = THANH MÁU (đồng hồ sống). Khách chốt: gieo mà CHƯA tưới sống noWaterDeathSec (cây 8h);
    //    mỗi lần TƯỚI đổ đầy wateredLifeSec (cây 20h); cạn về 0 = CHẾT. Lớn theo thời gian thật, KHÔNG gate nước. ──
    private double plantedTime = 0.0;     // mốc GIEO HẠT (đếm chết khi chưa tưới lần nào)
    private double lastWaterTime = 0.0;   // mốc TƯỚI gần nhất (đổ đầy thanh máu)
    private float dryAccumSec = 0f;       // (cũ — chỉ còn đọc ở LastCareFactor, luôn 0)
    /// <summary>(CŨ) Hệ số chăm sóc — nay luôn = 1 (cơ chế phạt sản lượng thay bằng CHẾT). Giữ cho caller cũ.</summary>
    public float LastCareFactor { get; private set; } = 1f;

    // ── Cây LÂU NĂM: thu NHIỀU LẦN (theo CropDefinition.maxHarvests / reHarvestCycleSec) ──
    private int harvestsRemaining = 1;   // số lần thu còn lại
    private bool isReGrowing = false;    // đang RA QUẢ LẠI giữa các lần thu (dùng reHarvestCycleSec)
    /// <summary>Sản phẩm VỤ CUỐI thu ở lần harvest cuối (caller đọc sau InteractHarvest). Rỗng nếu không có.</summary>
    public string LastFinalProductId { get; private set; } = "";
    public int LastFinalProductAmount { get; private set; } = 0;
    public bool LastHarvestWasFinal { get; private set; } = false;

    // ── Cây NHIỀU Ô (giàn, vd chanh dây 20 ô): master giữ list slave; slave trỏ về master (bị khoá) ──
    [HideInInspector] public FarmTile masterTile = null;          // != null → ô này bị 1 giàn chiếm
    private System.Collections.Generic.List<FarmTile> slaveTiles; // != null → ô này là master nhiều ô
    /// <summary>Ô đã cuốc và CHƯA bị giàn nào chiếm (dùng để tìm ô cho cây nhiều ô).</summary>
    public bool IsPlowedFree => currentState == TileState.Plowed && masterTile == null;
    public void OccupyAsSlot(FarmTile master) { masterTile = master; }
    public void RegisterSlaves(System.Collections.Generic.List<FarmTile> slaves) { slaveTiles = slaves; }
    private void FreeSlaves()
    {
        if (slaveTiles == null) return;
        foreach (var s in slaveTiles) if (s != null) s.masterTile = null;
        slaveTiles = null;
    }

    // Thanh nước nổi trên cây — billboard ĐỘC LẬP (KHÔNG parent vào ô đất để né scale lệch của Dirt).
    private Transform waterBarRoot;
    private Transform waterFillPivot;
    private Renderer waterFillRenderer;
    private Camera waterBarCam;
    private const float WBAR_W = 0.8f;
    private const float WBAR_H = 0.12f;

    // Model 3D thật của cây (khi useCustomCropModels = true)
    private GameObject cropModelInstance;
    private Vector3 cropModelBaseScale = Vector3.one;
    // Scale THẾ GIỚI của ô đất (Dirt = 0.15,1,0.15) — bù lại để cây không bị bóp dẹp khi làm con của ô.
    private Vector3 cropParentLossy = Vector3.one;

    // Crop-specific color (used for primitive fallback visuals)
    private Color cropColor = Color.green;

    // Nhãn chữ NỔI trên cây (billboard) — hiện % lớn / chín sau ~Xs / số lần thu (cho người chơi xem trực quan, đếm ngược sống).
    private Transform cropInfoRoot;
    private TextMesh cropInfoTM;
    private MeshFilter cropInfoMF;   // để đo bề rộng chữ → CO nhãn cho vừa thanh nước (WBAR_W), né chữ to tràn màn hình

    void Start()
    {
        if (!useCustomCropModels) CreateFallbackVisuals();
        UpdateVisuals();
    }

    void Update()
    {
        // CHẾT vì THIẾU NƯỚC (khách chốt): thanh nước = THANH MÁU. Áp cho CẢ lúc chưa tưới (Planted, đồng hồ 8h)
        // lẫn đang lớn (Watered, đồng hồ 20h từ lần tưới). Cạn về 0 = chết. Bỏ qua trong Tutorial (người mới khỏi nản).
        if (currentCrop != null
            && (currentState == TileState.Planted || currentState == TileState.Watered)
            && GetWaterFraction() <= 0f
            && !IsTutorialActive())
        {
            DieFromDrought();
            return;
        }

        if (isGrowing && currentState == TileState.Watered)
        {
            // % lớn theo THỜI GIAN THẬT từ lần tưới đầu (không gate nước; thiếu nước thì CHẾT, không đứng hình).
            float pct = GetGrowthPercentage();

            // Cây đang lớn mà chưa có thanh nước (vd load từ save) → tạo cho chắc.
            if (waterBarRoot == null) CreateWaterBar();

            if (useCustomCropModels)
            {
                // Model 3D phóng to dần theo % trưởng thành
                ApplyCropGrowthScale();
            }
            else if (pct >= 0.4f && seedVisual != null && seedVisual.activeSelf)
            {
                // Switch from seed Visual to growing Visual at 40% growth (primitive)
                if (seedVisual != null) seedVisual.SetActive(false);
                if (growingVisual != null)
                {
                    growingVisual.SetActive(true);
                    // Update growing visual color to match crop
                    UpdateCropColor(growingVisual);
                }
            }

            if (pct >= 1f)
            {
                // Ripe state reached
                isGrowing = false;
                currentState = TileState.Ripe;
                if (useCustomCropModels) ApplyCropGrowthScale();
                DestroyWaterBar(); // cây chín → khỏi cần nước nữa
                UpdateVisuals();

                OnTileWatered?.Invoke(this); // Trigger update to show progress text at 100%
            }
        }
    }

    // ── Thanh nước nổi trên cây (billboard độc lập) ──

    void LateUpdate()
    {
        UpdateCropInfoLabel(); // chữ nổi trên cây — chạy độc lập với thanh nước (hiện cả lúc cây đã chín)

        // Thanh máu hiện CẢ khi chưa tưới (Planted — để nhắc tưới) lẫn đang lớn (Watered). Tạo lười nếu chưa có.
        bool show = (currentState == TileState.Watered)
                 || (currentState == TileState.Planted && currentCrop != null && currentCrop.noWaterDeathSec > 0f);
        if (show && waterBarRoot == null) CreateWaterBar();
        if (waterBarRoot == null) return;
        if (waterBarCam == null) waterBarCam = Camera.main;

        if (waterBarRoot.gameObject.activeSelf != show) waterBarRoot.gameObject.SetActive(show);
        if (!show) return;

        // Vị trí: ngay trên ĐỈNH cây (theo bounds model nếu có, để bám theo lúc cây lớn dần).
        float topY = transform.position.y + 1.0f;
        if (cropModelInstance != null)
        {
            float maxY = float.MinValue; bool found = false;
            foreach (var r in cropModelInstance.GetComponentsInChildren<Renderer>())
            {
                if (r == null) continue;
                maxY = Mathf.Max(maxY, r.bounds.max.y);
                found = true;
            }
            if (found) topY = maxY + 0.3f;
        }
        waterBarRoot.position = new Vector3(transform.position.x, topY, transform.position.z);
        if (waterBarCam != null)
            waterBarRoot.rotation = Quaternion.LookRotation(waterBarCam.transform.forward, waterBarCam.transform.up);

        // Fill + màu: xanh dương (đầy) → đỏ (khát).
        float frac = GetWaterFraction();
        if (waterFillPivot != null) waterFillPivot.localScale = new Vector3(Mathf.Max(0.0001f, frac), 1f, 1f);
        if (waterFillRenderer != null)
        {
            Color c = Color.Lerp(new Color(0.9f, 0.3f, 0.2f), new Color(0.25f, 0.6f, 1f), frac);
            ApplyWorldBarColor(waterFillRenderer.material, c);
        }
    }

    private void CreateWaterBar()
    {
        if (waterBarRoot != null) return;
        var rootGo = new GameObject("WaterBar");
        waterBarRoot = rootGo.transform; // KHÔNG parent (né scale ô đất lệch) — FarmTile tự quản vòng đời.

        CreateWaterQuad(waterBarRoot, "BarBG", Vector3.zero,
            new Vector3(WBAR_W, WBAR_H, 1f), new Color(0.12f, 0.12f, 0.14f, 1f));

        var pivotGo = new GameObject("FillPivot");
        waterFillPivot = pivotGo.transform;
        waterFillPivot.SetParent(waterBarRoot, false);
        waterFillPivot.localPosition = new Vector3(-WBAR_W * 0.5f, 0f, -0.01f);

        var fill = CreateWaterQuad(waterFillPivot, "Fill", new Vector3(WBAR_W * 0.5f, 0f, 0f),
            new Vector3(WBAR_W, WBAR_H * 0.78f, 1f), new Color(0.25f, 0.6f, 1f, 1f));
        waterFillRenderer = fill.GetComponent<Renderer>();

        waterBarCam = Camera.main;
    }

    private GameObject CreateWaterQuad(Transform parent, string nameStr, Vector3 localPos, Vector3 localScale, Color color)
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = nameStr;
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = localPos;
        quad.transform.localScale = localScale;

        var col = quad.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var r = quad.GetComponent<Renderer>();
        if (r != null)
        {
            var mat = CreateWorldBarMaterial(color);
            if (mat != null)
            {
                mat.SetFloat("_Cull", 0f); // 2 mặt
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

        LogWorldBarMaterial("FarmTile", mat);
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

    private void DestroyWaterBar()
    {
        if (waterBarRoot != null) { Destroy(waterBarRoot.gameObject); waterBarRoot = null; }
        waterFillPivot = null;
        waterFillRenderer = null;
    }

    void OnDisable()
    {
        // Tile bị tắt (đổi đảo cho nhẹ máy) → ẩn thanh nước độc lập theo.
        if (waterBarRoot != null) waterBarRoot.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        DestroyWaterBar();
    }

    // ── Interaction Methods ──

    public bool InteractPlow()
    {
        if (currentState != TileState.Soil) return false;

        currentState = TileState.Plowed;
        UpdateVisuals();
        OnTilePlowed?.Invoke(this);
        return true;
    }

    public bool InteractPlant(string seedId)
    {
        if (currentState != TileState.Plowed || masterTile != null) return false;

        plantedSeedId = seedId;
        currentState = TileState.Planted;
        plantedTime = RealNow(); // mốc gieo — đồng hồ CHẾT khi chưa tưới (noWaterDeathSec)
        harvestsRemaining = 1;
        isReGrowing = false;

        // Lookup CropDefinition from CropDatabase
        var cropDb = Resources.Load<CropDatabase>("CropDatabase");
        if (cropDb != null)
        {
            currentCrop = cropDb.GetCropBySeedId(seedId);
            if (currentCrop != null)
            {
                cropColor = currentCrop.cropColor;
                harvestsRemaining = Mathf.Max(1, currentCrop.maxHarvests);
                Debug.Log($"[FarmTile] Planted {seedId} → crop found, growth={currentCrop.growthTimeSec}s, yield={currentCrop.harvestYield}, lanThu={harvestsRemaining}");
            }
        }

        // Hiện cây: dùng model 3D thật hoặc primitive tùy cấu hình
        if (useCustomCropModels)
        {
            SpawnCropModel();
        }
        else
        {
            DestroySeedAndGrowingVisuals();
            CreateCropVisuals();
        }
        UpdateVisuals();

        OnTilePlanted?.Invoke(this);
        return true;
    }

    public bool InteractWater()
    {
        if (currentState != TileState.Planted || masterTile != null) return false;

        currentState = TileState.Watered;
        growStartTime = RealNow(); // mốc bắt đầu LỚN (24h tính từ lần tưới đầu)
        isGrowing = true;
        lastWaterTime = RealNow(); // đổ đầy thanh máu (wateredLifeSec)
        dryAccumSec = 0f;
        CreateWaterBar();
        UpdateVisuals();
        OnTileWatered?.Invoke(this);
        return true;
    }

    /// <summary>Tưới LẠI khi cây đang lớn → ĐỔ ĐẦY thanh máu (wateredLifeSec), lùi mốc chết. Lớn vẫn chạy theo thời gian thật.</summary>
    public bool WaterAgain()
    {
        if (currentState != TileState.Watered || !isGrowing) return false;
        lastWaterTime = RealNow(); // đổ đầy lại thanh máu
        return true;
    }

    private float GetWaterInterval()
    {
        if (currentCrop != null && currentCrop.waterIntervalSec > 0f) return currentCrop.waterIntervalSec;
        return 20f;
    }

    /// <summary>Thanh MÁU 0..1. Planted (chưa tưới): tụt theo noWaterDeathSec từ lúc gieo (cây 8h).
    /// Watered: tụt theo wateredLifeSec từ lần tưới gần nhất (cây 20h). 0 = chết (xem DieFromDrought).
    /// Field = 0 → trả 1 (loại đó KHÔNG chết vì thiếu nước).</summary>
    public float GetWaterFraction()
    {
        if (currentCrop == null) return 1f;
        if (currentState == TileState.Planted)
        {
            float win = currentCrop.noWaterDeathSec;
            if (win <= 0f) return 1f;
            return Mathf.Clamp01(1f - (float)((RealNow() - plantedTime) / win));
        }
        if (currentState == TileState.Watered)
        {
            float win = currentCrop.wateredLifeSec;
            if (win <= 0f) return 1f;
            return Mathf.Clamp01(1f - (float)((RealNow() - lastWaterTime) / win));
        }
        return 1f;
    }

    /// <summary>Số giây còn lại trước khi CHẾT vì thiếu nước (theo cửa sổ hiện tại). float.MaxValue nếu không chết.</summary>
    private float GetLifeRemainingSec()
    {
        if (currentCrop == null) return float.MaxValue;
        if (currentState == TileState.Planted)
        {
            if (currentCrop.noWaterDeathSec <= 0f) return float.MaxValue;
            return Mathf.Max(0f, currentCrop.noWaterDeathSec - (float)(RealNow() - plantedTime));
        }
        if (currentState == TileState.Watered)
        {
            if (currentCrop.wateredLifeSec <= 0f) return float.MaxValue;
            return Mathf.Max(0f, currentCrop.wateredLifeSec - (float)(RealNow() - lastWaterTime));
        }
        return float.MaxValue;
    }

    /// <summary>Cây CHẾT vì thiếu nước (thanh máu cạn): ô về Đất trống, MẤT giống, dọn model/thanh nước/nhãn,
    /// thả ô giàn (cây nhiều ô). KHÔNG trao sản phẩm, KHÔNG tính là thu hoạch (không bắn OnTileHarvested).</summary>
    private void DieFromDrought()
    {
        currentState = TileState.Soil;
        plantedSeedId = "";
        currentCrop = null;
        cropColor = Color.green;
        growStartTime = 0.0;
        plantedTime = 0.0;
        isGrowing = false;
        isReGrowing = false;
        harvestsRemaining = 1;
        dryAccumSec = 0f;
        FreeSlaves(); // cây nhiều ô chết → thả các ô slave cho trồng lại
        DestroyWaterBar();
        if (cropInfoRoot != null) { Destroy(cropInfoRoot.gameObject); cropInfoRoot = null; cropInfoTM = null; cropInfoMF = null; }
        if (cropModelInstance != null) { Destroy(cropModelInstance); cropModelInstance = null; }
        UpdateVisuals();

        ScreenToast.Show("Cây đã héo chết vì thiếu nước! Nhớ tưới đúng giờ nhé.");
    }

    /// <summary>Có đang trong Tutorial không (tutorial ép lớn nhanh → KHÔNG cho cây chết, người mới khỏi nản).</summary>
    private bool IsTutorialActive()
    {
        TutorialManager tm = FindFirstObjectByType<TutorialManager>();
        return tm != null && tm.IsActive();
    }

    public bool InteractHarvest(out string harvestedItemId, out int amount)
    {
        harvestedItemId = "";
        amount = 0;
        LastHarvestWasFinal = false;

        if (currentState != TileState.Ripe) return false;

        // Use CropDefinition data if available
        if (currentCrop != null)
        {
            harvestedItemId = currentCrop.harvestItemId;
            amount = currentCrop.harvestYield;
        }
        else
        {
            // Fallback: return seed id as harvest (legacy tutorial behavior)
            harvestedItemId = plantedSeedId;
            amount = 5;
        }

        // Behavior B: phạt theo thời gian KHÁT — sản lượng giảm dần tới tối thiểu 50%.
        float totalGrow = GetGrowthTime();
        float dryRatio = totalGrow > 0f ? Mathf.Clamp01(dryAccumSec / totalGrow) : 0f;
        LastCareFactor = Mathf.Lerp(1f, 0.5f, dryRatio);
        amount = Mathf.Max(1, Mathf.RoundToInt(amount * LastCareFactor));

        // Mặc định chưa có sản phẩm vụ cuối (caller đọc 2 field này sau khi gọi).
        LastFinalProductId = "";
        LastFinalProductAmount = 0;

        harvestsRemaining = Mathf.Max(0, harvestsRemaining - 1);

        // CÂY LÂU NĂM còn lần thu → quay lại RA QUẢ tiếp (KHÔNG về đất trống, GIỮ model + lặp chu kỳ).
        if (currentCrop != null && currentCrop.maxHarvests > 1 && harvestsRemaining > 0)
        {
            isReGrowing = true;
            currentState = TileState.Watered;
            isGrowing = true;
            growStartTime = RealNow();
            lastWaterTime = RealNow();
            dryAccumSec = 0f;
            CreateWaterBar();
            UpdateVisuals();
            OnTileHarvested?.Invoke(this);
            return true;
        }

        // LẦN CUỐI (hoặc cây ngắn ngày 1-lần): thu thêm sản phẩm VỤ CUỐI (nếu có) rồi cây biến mất.
        LastHarvestWasFinal = true;

        if (currentCrop != null && !string.IsNullOrEmpty(currentCrop.finalProductItemId) && currentCrop.finalProductAmount > 0)
        {
            LastFinalProductId = currentCrop.finalProductItemId;
            LastFinalProductAmount = currentCrop.finalProductAmount;
        }

        // Reset tile to Soil
        currentState = TileState.Soil;
        plantedSeedId = "";
        currentCrop = null;
        cropColor = Color.green;
        growStartTime = 0.0;
        isGrowing = false;
        isReGrowing = false;
        harvestsRemaining = 1;
        dryAccumSec = 0f;
        FreeSlaves(); // cây nhiều ô hết đời → thả các ô slave cho trồng lại
        DestroyWaterBar();
        if (cropInfoRoot != null) { Destroy(cropInfoRoot.gameObject); cropInfoRoot = null; cropInfoTM = null; cropInfoMF = null; }

        if (cropModelInstance != null) { Destroy(cropModelInstance); cropModelInstance = null; }

        UpdateVisuals();
        OnTileHarvested?.Invoke(this);
        return true;
    }

    /// <summary>
    /// Get the CropDefinition for the currently planted crop (if any).
    /// </summary>
    public CropDefinition GetCurrentCrop() => currentCrop;

    // ── Model 3D thật cho cây ──

    private void SpawnCropModel()
    {
        if (cropModelInstance != null) { Destroy(cropModelInstance); cropModelInstance = null; }
        if (currentCrop == null || currentCrop.cropPrefab == null)
        {
            Debug.LogWarning($"[FarmTile] useCustomCropModels BẬT nhưng CropDefinition của '{plantedSeedId}' chưa gán cropPrefab.");
            return;
        }

        cropModelInstance = Instantiate(currentCrop.cropPrefab);
        // SetParent(false): GIỮ NGUYÊN transform local của prefab (gồm góc xoay Blender ~ -90° trục X).
        cropModelInstance.transform.SetParent(transform, false);
        // Chỉ dời vị trí — KHÔNG đụng góc xoay/scale, để model đứng đúng như artist đã dựng.
        cropModelInstance.transform.localPosition = new Vector3(0f, currentCrop.modelGroundOffset, 0f);
        cropModelBaseScale = cropModelInstance.transform.localScale;
        cropParentLossy = transform.lossyScale; // nhớ scale ô đất để BÙ lại (chống bóp dẹp)

        // Bỏ collider của model để tia click vẫn trúng FarmTile (không bị model chắn)
        foreach (var col in cropModelInstance.GetComponentsInChildren<Collider>())
            Destroy(col);

        ApplyCropGrowthScale();
    }

    private void ApplyCropGrowthScale()
    {
        if (cropModelInstance == null) return;
        float minScale = currentCrop != null ? currentCrop.seedlingScale : 0.25f;
        // Lúc mới gieo (chưa tưới) để nhỏ nhất; tưới xong thì phóng to theo %.
        float t = (currentState == TileState.Watered || currentState == TileState.Ripe) ? GetGrowthPercentage() : 0f;
        float s = Mathf.Lerp(minScale, 1f, t);

        // Kích thước THẾ GIỚI mong muốn = base × % lớn. Chia cho scale ô đất (lossy) để cây
        // hiện ĐÚNG tỉ lệ 3D thật, KHÔNG bị ô đất (0.15,1,0.15) bóp dẹp.
        Vector3 world = cropModelBaseScale * s;
        Vector3 L = cropParentLossy;
        cropModelInstance.transform.localScale = new Vector3(
            Mathf.Approximately(L.x, 0f) ? world.x : world.x / L.x,
            Mathf.Approximately(L.y, 0f) ? world.y : world.y / L.y,
            Mathf.Approximately(L.z, 0f) ? world.z : world.z / L.z);

        // Neo ĐÁY cây xuống mặt ô → cây mọc TỪ DƯỚI LÊN (không phình 2 đầu); pivot ở đâu cũng đúng.
        AnchorBaseToGround();
    }

    /// <summary>Dời model theo trục Y sao cho ĐÁY (bounds.min.y của model) luôn nằm trên mặt ô đất,
    /// để khi scale lớn dần thì chỉ NGỌN vươn lên, gốc đứng yên (mọc thật, không dãn 2 đầu).</summary>
    private void AnchorBaseToGround()
    {
        if (cropModelInstance == null) return;
        var rends = cropModelInstance.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0) return;

        float minY = float.MaxValue;
        foreach (var r in rends)
        {
            if (r == null) continue;
            minY = Mathf.Min(minY, r.bounds.min.y);
        }
        if (minY >= float.MaxValue) return;

        // Mặt ô = vị trí ô + tinh chỉnh modelGroundOffset (anh dùng số này để nâng/hạ cao độ chân cây).
        float groundY = transform.position.y + (currentCrop != null ? currentCrop.modelGroundOffset : 0f);
        float delta = groundY - minY;
        cropModelInstance.transform.position += new Vector3(0f, delta, 0f);
    }

    // ── Helper Visual Updates ──

    private float GetGrowthTime()
    {
        // Ép TUA NHANH nếu đang trong Tutorial (khách chốt 24s). Ngoài tutorial dùng growthTimeSec thật (24h cây ngắn ngày).
        TutorialManager tm = FindFirstObjectByType<TutorialManager>();
        if (tm != null && tm.IsActive()) return 24f; // Tutorial = 24s (tua nhanh, bỏ qua growthTimeSec thật)

        if (currentCrop != null)
        {
            // Cây lâu năm đang RA QUẢ LẠI → dùng chu kỳ tái sinh thay vì thời gian lớn ban đầu.
            if (isReGrowing && currentCrop.reHarvestCycleSec > 0f) return currentCrop.reHarvestCycleSec;
            return currentCrop.growthTimeSec;
        }
        return tutorialGrowthTime;
    }

    private void UpdateVisuals()
    {
        // Deactivate all first
        if (soilVisual != null) soilVisual.SetActive(false);
        if (plowedVisual != null) plowedVisual.SetActive(false);
        if (seedVisual != null) seedVisual.SetActive(false);
        if (growingVisual != null) growingVisual.SetActive(false);
        if (ripeVisual != null) ripeVisual.SetActive(false);

        switch (currentState)
        {
            case TileState.Soil:
                if (soilVisual != null) soilVisual.SetActive(true);
                break;
            case TileState.Plowed:
                if (plowedVisual != null) plowedVisual.SetActive(true);
                break;
            case TileState.Planted:
                if (seedVisual != null) seedVisual.SetActive(true);
                break;
            case TileState.Watered:
                // Seed starts growing
                if (seedVisual != null) seedVisual.SetActive(true);
                break;
            case TileState.Ripe:
                if (ripeVisual != null)
                {
                    ripeVisual.SetActive(true);
                    UpdateCropColor(ripeVisual);
                }
                break;
        }
    }

    // ── Nhãn chữ nổi trên cây (đếm ngược + số lần thu) ──
    private void CreateCropInfo()
    {
        if (cropInfoRoot != null) return;
        var go = new GameObject("CropInfo");
        cropInfoRoot = go.transform;
        cropInfoTM = go.AddComponent<TextMesh>();
        cropInfoMF = go.GetComponent<MeshFilter>(); // TextMesh tự thêm MeshFilter — dùng đo bề rộng để co nhãn
        cropInfoTM.text = "";
        cropInfoTM.characterSize = 0.035f; // NHỎ (tránh chèn UI khác); còn co thêm cho không vượt thanh nước ở FitCropInfoToWidth
        cropInfoTM.fontSize = 70;
        cropInfoTM.anchor = TextAnchor.LowerCenter;
        cropInfoTM.alignment = TextAlignment.Center;
        cropInfoTM.color = Color.white;
        cropInfoTM.richText = true;
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
    }

    private void UpdateCropInfoLabel()
    {
        bool show = currentCrop != null &&
            (currentState == TileState.Watered || currentState == TileState.Ripe || currentState == TileState.Planted);
        if (!show)
        {
            if (cropInfoRoot != null) cropInfoRoot.gameObject.SetActive(false);
            return;
        }
        if (cropInfoRoot == null) CreateCropInfo();
        cropInfoRoot.gameObject.SetActive(true);

        // Vị trí trên đỉnh cây + xoay mặt về camera (billboard).
        float topY = transform.position.y + 1.35f;
        if (cropModelInstance != null)
        {
            float maxY = float.MinValue; bool found = false;
            foreach (var r in cropModelInstance.GetComponentsInChildren<Renderer>())
            { if (r == null) continue; maxY = Mathf.Max(maxY, r.bounds.max.y); found = true; }
            if (found) topY = maxY + 0.6f;
        }
        cropInfoRoot.position = new Vector3(transform.position.x, topY, transform.position.z);
        var cam = Camera.main;
        if (cam != null) cropInfoRoot.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);

        if (cropInfoTM != null) cropInfoTM.text = GetStatusText();
        FitCropInfoToWidth(); // co nhãn cho VỪA thanh nước, không tràn màn hình
    }

    /// <summary>Co nhãn nổi cho bề rộng KHÔNG vượt thanh nước (WBAR_W): chữ dài tự thu nhỏ, chữ ngắn giữ cỡ gốc.
    /// Đo bằng mesh local bounds (không phụ thuộc góc billboard) nên ổn định mọi hướng camera.</summary>
    private void FitCropInfoToWidth()
    {
        if (cropInfoRoot == null || cropInfoTM == null) return;
        if (cropInfoMF == null) cropInfoMF = cropInfoTM.GetComponent<MeshFilter>();
        var mesh = cropInfoMF != null ? cropInfoMF.sharedMesh : null;
        if (mesh == null) return;
        float localW = mesh.bounds.size.x;
        if (localW <= 0.0001f) return; // chưa dựng mesh (text rỗng/đầu frame) → giữ nguyên
        float scale = Mathf.Min(1f, WBAR_W / localW); // chỉ THU NHỎ, không phóng to quá thanh nước
        cropInfoRoot.localScale = new Vector3(scale, scale, scale);
    }

    /// <summary>Dòng trạng thái cây: % lớn / chín sau ~Xs / số lần thu / cần tưới.</summary>
    public string GetStatusText()
    {
        if (currentCrop == null) return "";
        switch (currentState)
        {
            case TileState.Ripe:
            {
                string s = "<color=#5BD66B>Đã chín</color>";
                if (currentCrop.maxHarvests > 1) s += $"\nCòn {harvestsRemaining}/{currentCrop.maxHarvests} lần";
                return s;
            }
            case TileState.Watered:
            {
                float pct = GetGrowthPercentage();
                float remain = Mathf.Max(0f, GetGrowthTime() - GetGrownSeconds());
                string s = $"Lớn {Mathf.RoundToInt(pct * 100f)}%";
                if (remain > 0.4f) s += $" · chín ~{FormatSec(remain)}";
                // Thanh máu xuống thấp → báo còn bao lâu CHẾT nếu không tưới.
                if (GetWaterFraction() < 0.4f)
                    s += $"\n<color=#E06666>cần tưới · héo ~{FormatSec(GetLifeRemainingSec())}</color>";
                if (currentCrop.maxHarvests > 1) s += $"\nlần {(currentCrop.maxHarvests - harvestsRemaining) + 1}/{currentCrop.maxHarvests}";
                return s;
            }
            case TileState.Planted:
            {
                string s = "<color=#FFD54F>Chưa tưới</color>";
                if (currentCrop.noWaterDeathSec > 0f)
                    s += $"\n<color=#E06666>héo ~{FormatSec(GetLifeRemainingSec())}</color>";
                return s;
            }
            default:
                return "";
        }
    }

    private static string FormatSec(float s)
    {
        if (s >= 60f) return $"{Mathf.FloorToInt(s / 60f)}m{Mathf.RoundToInt(s % 60f):00}s";
        return $"{Mathf.RoundToInt(s)}s";
    }

    public float GetGrowthPercentage()
    {
        if (currentState == TileState.Ripe) return 1f;
        if (!isGrowing) return 0f;
        float total = GetGrowthTime();
        if (total <= 0f) return 1f;
        // Lớn theo THỜI GIAN THẬT từ lần tưới đầu (khách chốt 24h cây ngắn ngày). KHÔNG gate nước nữa —
        // thiếu nước thì CHẾT chứ không "đứng hình". Đi đảo/tắt ô vẫn lớn bù đúng (mốc RealNow()).
        return Mathf.Clamp01(GetGrownSeconds() / total);
    }

    /// <summary>Giây đã lớn = thời gian thật trôi từ lần TƯỚI ĐẦU (growStartTime).</summary>
    private float GetGrownSeconds()
    {
        if (!isGrowing) return 0f;
        return Mathf.Max(0f, (float)(RealNow() - growStartTime));
    }

    private void UpdateCropColor(GameObject visual)
    {
        if (visual == null) return;
        // Update the non-ground child (the plant/fruit shape)
        foreach (Transform child in visual.transform)
        {
            if (child.name != "Ground")
            {
                Renderer r = child.GetComponent<Renderer>();
                if (r != null) r.material.color = cropColor;
            }
        }
    }

    private void DestroySeedAndGrowingVisuals()
    {
        // Destroy existing dynamic visuals to recreate with new crop color
        if (seedVisual != null && seedVisual.name == "SeedVisual")
        {
            Destroy(seedVisual);
            seedVisual = null;
        }
        if (growingVisual != null && growingVisual.name == "GrowingVisual")
        {
            Destroy(growingVisual);
            growingVisual = null;
        }
        if (ripeVisual != null && ripeVisual.name == "RipeVisual")
        {
            Destroy(ripeVisual);
            ripeVisual = null;
        }
    }

    private void CreateCropVisuals()
    {
        // Recreate seed, growing, ripe visuals with crop-specific color
        if (seedVisual == null)
        {
            seedVisual = new GameObject("SeedVisual");
            seedVisual.transform.SetParent(this.transform, false);

            GameObject ground = CreateGround(seedVisual.transform);
            GameObject sprout = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sprout.name = "Sprout";
            sprout.transform.SetParent(seedVisual.transform, false);
            sprout.transform.localPosition = new Vector3(0, 0.08f, 0); // Sát mặt đất
            sprout.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f); // Bẹp bẹp giống hạt giống
            SetColor(sprout, new Color(0.9f, 0.7f, 0.2f)); // Màu vàng nhạt nổi bật
            Destroy(sprout.GetComponent<Collider>());
        }
        if (growingVisual == null)
        {
            growingVisual = new GameObject("GrowingVisual");
            growingVisual.transform.SetParent(this.transform, false);

            GameObject ground = CreateGround(growingVisual.transform);
            GameObject plant = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plant.name = "Plant";
            plant.transform.SetParent(growingVisual.transform, false);
            plant.transform.localPosition = new Vector3(0, 0.4f, 0);
            plant.transform.localScale = new Vector3(0.3f, 0.4f, 0.3f);
            SetColor(plant, cropColor);
            Destroy(plant.GetComponent<Collider>());
        }
        if (ripeVisual == null)
        {
            ripeVisual = new GameObject("RipeVisual");
            ripeVisual.transform.SetParent(this.transform, false);

            GameObject ground = CreateGround(ripeVisual.transform);
            GameObject fruit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fruit.name = "Fruit";
            fruit.transform.SetParent(ripeVisual.transform, false);
            fruit.transform.localPosition = new Vector3(0, 0.6f, 0);
            fruit.transform.localScale = new Vector3(0.4f, 0.5f, 0.4f);
            SetColor(fruit, cropColor);
            Destroy(fruit.GetComponent<Collider>());
        }
    }

    private GameObject CreateGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(parent, false);
        ground.transform.localScale = new Vector3(2f, 0.15f, 2f);
        SetColor(ground, new Color(0.35f, 0.22f, 0.1f));
        Destroy(ground.GetComponent<Collider>());
        return ground;
    }

    private void CreateFallbackVisuals()
    {
        if (soilVisual == null)
        {
            soilVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            soilVisual.name = "SoilVisual";
            soilVisual.transform.SetParent(this.transform, false);
            soilVisual.transform.localScale = new Vector3(2f, 0.1f, 2f);
            SetColor(soilVisual, new Color(0.5f, 0.35f, 0.2f)); // Brown
            // Remove collider so it doesn't block raycasting on the parent FarmTile
            Destroy(soilVisual.GetComponent<Collider>());
        }
        if (plowedVisual == null)
        {
            plowedVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plowedVisual.name = "PlowedVisual";
            plowedVisual.transform.SetParent(this.transform, false);
            plowedVisual.transform.localScale = new Vector3(2f, 0.15f, 2f);
            SetColor(plowedVisual, new Color(0.35f, 0.22f, 0.1f)); // Dark brown
            Destroy(plowedVisual.GetComponent<Collider>());
        }
        // Default seed/growing/ripe visuals (will be recreated with crop color when planted)
        if (seedVisual == null)
        {
            seedVisual = new GameObject("SeedVisual");
            seedVisual.transform.SetParent(this.transform, false);

            CreateGround(seedVisual.transform);
            GameObject sprout = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sprout.name = "Sprout";
            sprout.transform.SetParent(seedVisual.transform, false);
            sprout.transform.localPosition = new Vector3(0, 0.08f, 0); // Sát mặt đất
            sprout.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f); // Bẹp giống hạt giống
            SetColor(sprout, new Color(0.9f, 0.7f, 0.2f)); // Vàng nhạt
            Destroy(sprout.GetComponent<Collider>());
        }
        if (growingVisual == null)
        {
            growingVisual = new GameObject("GrowingVisual");
            growingVisual.transform.SetParent(this.transform, false);

            CreateGround(growingVisual.transform);
            GameObject plant = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plant.name = "Plant";
            plant.transform.SetParent(growingVisual.transform, false);
            plant.transform.localPosition = new Vector3(0, 0.4f, 0);
            plant.transform.localScale = new Vector3(0.3f, 0.4f, 0.3f);
            SetColor(plant, Color.green);
            Destroy(plant.GetComponent<Collider>());
        }
        if (ripeVisual == null)
        {
            ripeVisual = new GameObject("RipeVisual");
            ripeVisual.transform.SetParent(this.transform, false);

            CreateGround(ripeVisual.transform);
            GameObject fruit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fruit.name = "Fruit";
            fruit.transform.SetParent(ripeVisual.transform, false);
            fruit.transform.localPosition = new Vector3(0, 0.6f, 0);
            fruit.transform.localScale = new Vector3(0.4f, 0.5f, 0.4f);
            SetColor(fruit, new Color(1f, 0.5f, 0f)); // Orange fallback
            Destroy(fruit.GetComponent<Collider>());
        }
    }

    private void SetColor(GameObject go, Color color)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            r.material.color = color;
        }
    }

    // ───────────────────────── PERSISTENCE (real-time, lớn-bù / chết-bù) ─────────────────────────

    /// <summary>Mốc thời gian ĐỜI THỰC (Unix giây). Thay Time.timeAsDouble (reset khi đóng app) để cây
    /// lớn-bù + chết-bù đúng qua các phiên. ⚠️ Chỉnh giờ máy có thể tua — chống gian lận = server-time (Phase sau).</summary>
    private static double RealNow() => System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

    /// <summary>Gói lưu trạng thái 1 ô (FarmManager ghi posKey + serialize ra đĩa).</summary>
    [System.Serializable]
    public class CropSave
    {
        public string posKey;
        public int state;
        public string seedId;
        public double plantedUnix;
        public double lastWaterUnix;
        public double growStartUnix;
        public int harvestsRemaining;
        public bool isReGrowing;
    }

    /// <summary>Xuất trạng thái để LƯU. null nếu không cần (đất trống, ô slave của giàn, cây nhiều-ô chưa hỗ trợ).</summary>
    public CropSave ExportSaveOrNull()
    {
        if (currentState == TileState.Soil) return null;
        if (masterTile != null) return null;                                // ô slave → master lo
        if (currentCrop != null && currentCrop.plotSlots > 1) return null;  // cây nhiều ô (giàn): chưa hỗ trợ lưu
        return new CropSave
        {
            state = (int)currentState,
            seedId = plantedSeedId,
            plantedUnix = plantedTime,
            lastWaterUnix = lastWaterTime,
            growStartUnix = growStartTime,
            harvestsRemaining = harvestsRemaining,
            isReGrowing = isReGrowing,
        };
    }

    /// <summary>Khôi phục trạng thái cây từ save + GIẢI QUYẾT thời gian offline đã trôi (lớn-bù / chết-bù).
    /// Dùng mốc thực nên đóng app vài tiếng/ngày → mở lại cây tự chín hoặc đã héo chết đúng.</summary>
    public void RestoreSave(CropSave s, CropDatabase db)
    {
        if (s == null) return;

        currentState = (TileState)s.state;
        plantedSeedId = s.seedId;
        plantedTime = s.plantedUnix;
        lastWaterTime = s.lastWaterUnix;
        growStartTime = s.growStartUnix;
        harvestsRemaining = Mathf.Max(1, s.harvestsRemaining);
        isReGrowing = s.isReGrowing;

        if (currentState == TileState.Soil || currentState == TileState.Plowed) { UpdateVisuals(); return; }

        // Tra cứu crop + dựng cây.
        if (db != null && !string.IsNullOrEmpty(plantedSeedId))
        {
            currentCrop = db.GetCropBySeedId(plantedSeedId);
            if (currentCrop != null) cropColor = currentCrop.cropColor;
        }
        if (currentCrop == null) // không tra được → để đất trống cho an toàn
        {
            currentState = TileState.Soil; plantedSeedId = ""; UpdateVisuals(); return;
        }

        if (useCustomCropModels) SpawnCropModel();
        else { DestroySeedAndGrowingVisuals(); CreateCropVisuals(); }

        double now = RealNow();

        // CHƯA tưới (Planted): chết nếu offline vượt noWaterDeathSec.
        if (currentState == TileState.Planted)
        {
            if (currentCrop.noWaterDeathSec > 0f && now >= plantedTime + currentCrop.noWaterDeathSec)
            { DieFromDrought(); return; }
            UpdateVisuals();
            return;
        }

        // ĐÃ chín khi lưu (Ripe): giữ nguyên chín, chờ thu hoạch (không đánh giá chết).
        if (currentState == TileState.Ripe)
        {
            isGrowing = false;
            if (useCustomCropModels) ApplyCropGrowthScale();
            UpdateVisuals();
            return;
        }

        // ĐÃ tưới, đang lớn (Watered): so MỐC chín vs MỐC chết để biết offline đã chín hay đã chết.
        isGrowing = true;
        double ripeAt = growStartTime + GetGrowthTime();
        double deathAt = (currentCrop.wateredLifeSec > 0f)
                         ? lastWaterTime + currentCrop.wateredLifeSec
                         : double.PositiveInfinity;

        if (deathAt < ripeAt && now >= deathAt) { DieFromDrought(); return; }   // hết nước trước khi chín → đã chết
        if (ripeAt <= deathAt && now >= ripeAt)                                  // kịp chín → đã chín
        {
            isGrowing = false;
            currentState = TileState.Ripe;
            if (useCustomCropModels) ApplyCropGrowthScale();
            DestroyWaterBar();
            UpdateVisuals();
            return;
        }

        // Vẫn đang lớn → dựng thanh nước, để Update sống tiếp bình thường.
        CreateWaterBar();
        if (useCustomCropModels) ApplyCropGrowthScale();
        UpdateVisuals();
    }
}
