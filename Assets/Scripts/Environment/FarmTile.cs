using UnityEngine;
using System;

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
    public GameObject ripeVisual;      // Visual showing final product (e.g. Carrot ready)

    [Header("Timing Configuration")]
    public float tutorialGrowthTime = 60f; // 60s for tutorial

    // Events
    public Action<FarmTile> OnTilePlowed;
    public Action<FarmTile> OnTilePlanted;
    public Action<FarmTile> OnTileWatered;
    public Action<FarmTile> OnTileHarvested;

    private float growthTimer = 0f;
    private bool isGrowing = false;

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
            
            // Switch from seed Visual to growing Visual at 40% growth
            if (growthTimer >= tutorialGrowthTime * 0.4f && seedVisual != null && seedVisual.activeSelf)
            {
                if (seedVisual != null) seedVisual.SetActive(false);
                if (growingVisual != null) growingVisual.SetActive(true);
            }

            if (growthTimer >= tutorialGrowthTime)
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

        harvestedItemId = plantedSeedId; // Return the carrot or whatever was planted
        amount = 5; // e.g. yields 5 carrots
        
        // Reset tile to Soil
        currentState = TileState.Soil;
        plantedSeedId = "";
        growthTimer = 0f;
        isGrowing = false;

        UpdateVisuals();
        OnTileHarvested?.Invoke(this);
        return true;
    }

    // ── Helper Visual Updates ──

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
                if (ripeVisual != null) ripeVisual.SetActive(true);
                break;
        }
    }

    public float GetGrowthPercentage()
    {
        if (currentState == TileState.Ripe) return 1f;
        if (!isGrowing) return 0f;
        return Mathf.Clamp01(growthTimer / tutorialGrowthTime);
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
        if (seedVisual == null)
        {
            seedVisual = new GameObject("SeedVisual");
            seedVisual.transform.SetParent(this.transform, false);
            
            // Add ground plowed representation
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.SetParent(seedVisual.transform, false);
            ground.transform.localScale = new Vector3(2f, 0.15f, 2f);
            SetColor(ground, new Color(0.35f, 0.22f, 0.1f));
            Destroy(ground.GetComponent<Collider>());
            
            // Add tiny sprout
            GameObject sprout = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sprout.transform.SetParent(seedVisual.transform, false);
            sprout.transform.localPosition = new Vector3(0, 0.2f, 0);
            sprout.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            SetColor(sprout, Color.green);
            Destroy(sprout.GetComponent<Collider>());
        }
        if (growingVisual == null)
        {
            growingVisual = new GameObject("GrowingVisual");
            growingVisual.transform.SetParent(this.transform, false);
            
            // Add ground plowed representation
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.SetParent(growingVisual.transform, false);
            ground.transform.localScale = new Vector3(2f, 0.15f, 2f);
            SetColor(ground, new Color(0.35f, 0.22f, 0.1f));
            Destroy(ground.GetComponent<Collider>());
            
            // Add medium sprout
            GameObject sprout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sprout.transform.SetParent(growingVisual.transform, false);
            sprout.transform.localPosition = new Vector3(0, 0.4f, 0);
            sprout.transform.localScale = new Vector3(0.3f, 0.4f, 0.3f);
            SetColor(sprout, Color.green);
            Destroy(sprout.GetComponent<Collider>());
        }
        if (ripeVisual == null)
        {
            ripeVisual = new GameObject("RipeVisual");
            ripeVisual.transform.SetParent(this.transform, false);
            
            // Add ground plowed representation
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.SetParent(ripeVisual.transform, false);
            ground.transform.localScale = new Vector3(2f, 0.15f, 2f);
            SetColor(ground, new Color(0.35f, 0.22f, 0.1f));
            Destroy(ground.GetComponent<Collider>());
            
            // Add ripe carrot
            GameObject carrot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            carrot.transform.SetParent(ripeVisual.transform, false);
            carrot.transform.localPosition = new Vector3(0, 0.6f, 0);
            carrot.transform.localScale = new Vector3(0.4f, 0.5f, 0.4f);
            SetColor(carrot, new Color(1f, 0.5f, 0f)); // Orange!
            Destroy(carrot.GetComponent<Collider>());
        }
    }

    private void SetColor(GameObject go, Color color)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Standard"));
            r.material.color = color;
        }
    }
}
