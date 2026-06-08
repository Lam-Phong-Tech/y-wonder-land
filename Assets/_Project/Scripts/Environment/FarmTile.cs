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

    [Header("Crop Data (Auto-assigned)")]
    [SerializeField] private CropDefinition currentCrop;

    // Tile index for FarmManager tracking
    [HideInInspector] public int tileIndex = -1;

    // Events
    public Action<FarmTile> OnTilePlowed;
    public Action<FarmTile> OnTilePlanted;
    public Action<FarmTile> OnTileWatered;
    public Action<FarmTile> OnTileHarvested;

    private float growthTimer = 0f;
    private bool isGrowing = false;

    // Crop-specific color (used for primitive fallback visuals)
    private Color cropColor = Color.green;

    void Start()
    {
        CreateFallbackVisuals();
        UpdateVisuals();
    }

    void Update()
    {
        if (isGrowing && currentState == TileState.Watered)
        {
            growthTimer += Time.deltaTime;

            float totalGrowthTime = GetGrowthTime();

            // Switch from seed Visual to growing Visual at 40% growth
            if (growthTimer >= totalGrowthTime * 0.4f && seedVisual != null && seedVisual.activeSelf)
            {
                if (seedVisual != null) seedVisual.SetActive(false);
                if (growingVisual != null)
                {
                    growingVisual.SetActive(true);
                    // Update growing visual color to match crop
                    UpdateCropColor(growingVisual);
                }
            }

            if (growthTimer >= totalGrowthTime)
            {
                // Ripe state reached
                isGrowing = false;
                currentState = TileState.Ripe;
                UpdateVisuals();

                OnTileWatered?.Invoke(this); // Trigger update to show progress text at 100%
            }
        }
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

        // Recreate visuals with crop-specific color
        DestroySeedAndGrowingVisuals();
        CreateCropVisuals();
        UpdateVisuals();

        OnTilePlanted?.Invoke(this);
        return true;
    }

    public bool InteractWater()
    {
        if (currentState != TileState.Planted) return false;

        currentState = TileState.Watered;
        growthTimer = 0f;
        isGrowing = true;
        UpdateVisuals();
        OnTileWatered?.Invoke(this);
        return true;
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

        // Reset tile to Soil
        currentState = TileState.Soil;
        plantedSeedId = "";
        currentCrop = null;
        cropColor = Color.green;
        growthTimer = 0f;
        isGrowing = false;

        UpdateVisuals();
        OnTileHarvested?.Invoke(this);
        return true;
    }

    /// <summary>
    /// Get the CropDefinition for the currently planted crop (if any).
    /// </summary>
    public CropDefinition GetCurrentCrop() => currentCrop;

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
        return Mathf.Clamp01(growthTimer / GetGrowthTime());
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
