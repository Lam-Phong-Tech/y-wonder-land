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
}
