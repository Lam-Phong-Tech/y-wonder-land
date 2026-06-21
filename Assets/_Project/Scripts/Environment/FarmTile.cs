using UnityEngine;
using System;
using YWonderLand.Data;

/// <summary>
/// FarmTile nâng cấp: tích hợp CropDefinition, visual stages theo loại cây,
/// growth time từ CropDatabase, thu hoạch trả produce ID + yield từ data.
/// Tương thích ngược với TutorialManager (vẫn giữ tutorialGrowthTime fallback).
/// </summary>
public class FarmTile : MonoBehaviour
{
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
    public float tutorialGrowthTime = 5f; // Fallback for tutorial

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

    // MỐC THỜI GIAN bắt đầu lớn (giây, theo đồng hồ TOÀN CỤC Time.timeAsDouble).
    // % lớn = (giờ hiện tại − mốc này) / thời_gian_lớn → KHÔNG phụ thuộc Update mỗi frame,
    // nên đi thành phố / tắt ô đất cho nhẹ máy thì về farm cây vẫn "lớn bù" đúng thời gian đã trôi.
    private double growStartTime = 0.0;
    private bool isGrowing = false;

    // ── Nước/khát (behavior B: hết nước → cây héo, GIẢM sản lượng/POS, vẫn lớn & thu được) ──
    private double lastWaterTime = 0.0;   // mốc tưới gần nhất (đầy nước)
    private float growthAccrued = 0f;     // GIÂY tăng trưởng đã "ngấm nước" tích luỹ (tưới-gate-lớn)
    private float dryAccumSec = 0f;       // (giữ cho tương thích; cơ chế mới = hết nước thì NGỪNG lớn, không phạt)
    /// <summary>Hệ số chăm sóc 0.5..1 lúc thu (1 = không để khát; 0.5 = khát suốt). Nhân vào sản lượng/POS/EXP.</summary>
    public float LastCareFactor { get; private set; } = 1f;

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

    void Start()
    {
        if (!useCustomCropModels) CreateFallbackVisuals();
        UpdateVisuals();
    }

    void Update()
    {
        if (isGrowing && currentState == TileState.Watered)
        {
            // % lớn tính TỪ MỐC THỜI GIAN (đồng hồ toàn cục) — không cộng dồn mỗi frame.
            float pct = GetGrowthPercentage();

            // Cây đang lớn mà chưa có thanh nước (vd load từ save) → tạo cho chắc.
            if (waterBarRoot == null) CreateWaterBar();

            // Cơ chế MỚI (khách chốt): hết nước → cây NGỪNG lớn (GetGrownSeconds tự dừng cộng), phải tưới lại.

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
        if (waterBarRoot == null) return;
        if (waterBarCam == null) waterBarCam = Camera.main;

        bool show = isGrowing && currentState == TileState.Watered;
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
            waterFillRenderer.material.SetColor("_BaseColor", c);
            waterFillRenderer.material.color = c;
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
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            mat.SetFloat("_Cull", 0f); // 2 mặt
            r.material = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        return quad;
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
        if (currentState != TileState.Plowed) return false;

        plantedSeedId = seedId;
        currentState = TileState.Planted;

        // Lookup CropDefinition from CropDatabase
        var cropDb = Resources.Load<CropDatabase>("CropDatabase");
        if (cropDb != null)
        {
            currentCrop = cropDb.GetCropBySeedId(seedId);
            if (currentCrop != null)
            {
                cropColor = currentCrop.cropColor;
                Debug.Log($"[FarmTile] Planted {seedId} → crop found, growth={currentCrop.growthTimeSec}s, yield={currentCrop.harvestYield}");
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
        if (currentState != TileState.Planted) return false;

        currentState = TileState.Watered;
        growStartTime = Time.timeAsDouble; // mốc bắt đầu lớn
        isGrowing = true;
        lastWaterTime = Time.timeAsDouble; // đầy nước
        growthAccrued = 0f;
        dryAccumSec = 0f;
        CreateWaterBar();
        UpdateVisuals();
        OnTileWatered?.Invoke(this);
        return true;
    }

    /// <summary>Tưới LẠI khi cây đang lớn → dồn phần đã ngấm vào kho rồi đổ đầy nước. Trả true nếu tưới được.</summary>
    public bool WaterAgain()
    {
        if (currentState != TileState.Watered || !isGrowing) return false;
        // Chốt phần tăng trưởng của lần tưới trước (tối đa 1 chu kỳ nước) vào kho, rồi reset đồng hồ nước.
        growthAccrued += Mathf.Clamp((float)(Time.timeAsDouble - lastWaterTime), 0f, GetWaterInterval());
        lastWaterTime = Time.timeAsDouble;
        return true;
    }

    private float GetWaterInterval()
    {
        if (currentCrop != null && currentCrop.waterIntervalSec > 0f) return currentCrop.waterIntervalSec;
        return 20f;
    }

    /// <summary>Mức nước còn lại 0..1 (1 = vừa tưới, 0 = khát). Dùng cho thanh nước + tính phạt.</summary>
    public float GetWaterFraction()
    {
        if (currentCrop == null) return 1f;
        float interval = Mathf.Max(1f, currentCrop.waterIntervalSec);
        double elapsed = Time.timeAsDouble - lastWaterTime;
        return Mathf.Clamp01(1f - (float)(elapsed / interval));
    }

    public bool InteractHarvest(out string harvestedItemId, out int amount)
    {
        harvestedItemId = "";
        amount = 0;

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

        // Reset tile to Soil
        currentState = TileState.Soil;
        plantedSeedId = "";
        currentCrop = null;
        cropColor = Color.green;
        growStartTime = 0.0;
        isGrowing = false;
        dryAccumSec = 0f;
        DestroyWaterBar();

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
        // Ép dùng thời gian ngắn nếu đang trong Tutorial
        TutorialManager tm = FindFirstObjectByType<TutorialManager>();
        if (tm != null && tm.IsActive()) return 5f; // Trực tiếp trả về 5s để bỏ qua giá trị lưu trong Inspector

        if (currentCrop != null) return currentCrop.growthTimeSec;
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

    public float GetGrowthPercentage()
    {
        if (currentState == TileState.Ripe) return 1f;
        if (!isGrowing) return 0f;
        float total = GetGrowthTime();
        if (total <= 0f) return 1f;
        // TƯỚI-GATE-LỚN: chỉ tính giây đã "ngấm nước". Hết nước → ngừng cộng → cây không lớn tới khi tưới lại.
        return Mathf.Clamp01(GetGrownSeconds() / total);
    }

    /// <summary>Giây tăng trưởng đã ngấm nước = kho tích luỹ + phần lần tưới hiện tại (CAP = 1 chu kỳ nước).</summary>
    private float GetGrownSeconds()
    {
        float interval = GetWaterInterval();
        float sinceWater = (float)(Time.timeAsDouble - lastWaterTime);
        return growthAccrued + Mathf.Clamp(sinceWater, 0f, interval);
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
}
