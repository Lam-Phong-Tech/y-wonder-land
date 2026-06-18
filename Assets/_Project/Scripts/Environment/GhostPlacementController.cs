using UnityEngine;

/// <summary>
/// Handles the ghost preview object during Build Mode.
/// Creates a Cube placeholder that follows the mouse cursor,
/// snaps to grid, and changes color based on placement validity.
/// </summary>
public class GhostPlacementController : MonoBehaviour
{
    public static GhostPlacementController Instance { get; private set; }

    /// <summary>
    /// Fired when a building is placed. Parameter is the item name and price.
    /// </summary>
    public static event System.Action<string, int> OnBuildingPlaced;

    [Header("Ghost Settings")]
    [SerializeField] private Color validColor = new Color(0.2f, 0.9f, 0.3f, 0.4f);
    [SerializeField] private Color invalidColor = new Color(0.9f, 0.2f, 0.2f, 0.4f);
    [SerializeField] private float ghostHeight = 0.6f;

    [Header("Raycast")]
    [SerializeField] private LayerMask groundMask = ~0;

    // Ghost object
    private GameObject ghostObject;
    private Renderer ghostRenderer;
    private Material ghostMaterial;

    // Current placement state
    private bool isActive = false;
    private Vector2Int currentItemSize = new Vector2Int(1, 1);
    private string currentItemName = "";
    private int currentItemPrice = 0;
    private int rotationAngle = 0;
    private Vector2Int currentGridCell;
    private bool currentPlacementValid = false;
    public bool IsPinned { get; private set; } = false;
    public Vector3 GhostPosition => ghostObject != null ? ghostObject.transform.position : Vector3.zero;

    // Reference
    private Camera mainCamera;
    private BuildGridManager gridManager;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
        gridManager = BuildGridManager.Instance;
    }

    void Update()
    {
        if (!isActive || ghostObject == null || gridManager == null) return;
        
        // Skip raycast if pinned
        if (IsPinned) return;

        // Update camera reference if needed
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        // Raycast from mouse to ground
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundMask))
        {
            // Snap to grid
            Vector2Int gridCell = gridManager.WorldToGrid(hit.point);
            currentGridCell = gridCell;

            // Get effective size (swapped if rotated 90/270)
            Vector2Int effectiveSize = GetEffectiveSize();

            // Position ghost at grid-snapped location
            Vector3 worldPos = gridManager.GridToWorldCentered(gridCell, effectiveSize);
            worldPos.y = hit.point.y + ghostHeight * 0.5f;
            ghostObject.transform.position = worldPos;

            // Check validity
            currentPlacementValid = gridManager.CanPlace(gridCell, effectiveSize);

            // Update color
            UpdateGhostColor(currentPlacementValid);
        }

        // Right click to cancel
        if (mouse.rightButton.wasPressedThisFrame)
        {
            Deactivate();
        }
    }

    // ── Activation ──

    /// <summary>
    /// Activate ghost preview for a given item.
    /// </summary>
    public void Activate(string itemName, Vector2Int size, int price)
    {
        currentItemName = itemName;
        currentItemSize = size;
        currentItemPrice = price;
        rotationAngle = 0;
        isActive = true;
        IsPinned = false;

        // Ensure grid manager reference
        if (gridManager == null) gridManager = BuildGridManager.Instance;

        // Create or update ghost object
        CreateGhostObject();

        Debug.Log($"[GhostPlacement] Activated for '{itemName}' size ({size.x},{size.y})");
    }

    /// <summary>
    /// Deactivate and hide ghost.
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        if (ghostObject != null)
        {
            ghostObject.SetActive(false);
        }
        Debug.Log("[GhostPlacement] Deactivated");
    }

    public bool IsActive => isActive;

    public void SetPinned(bool pinned)
    {
        IsPinned = pinned;
    }

    // ── Rotation ──

    /// <summary>
    /// Rotate ghost 90 degrees clockwise.
    /// </summary>
    public void Rotate()
    {
        rotationAngle = (rotationAngle + 90) % 360;

        if (ghostObject != null)
        {
            ghostObject.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);

            // Update scale for swapped dimensions
            Vector2Int effectiveSize = GetEffectiveSize();
            float cellSize = gridManager != null ? gridManager.CellSize : 1f;
            ghostObject.transform.localScale = new Vector3(
                effectiveSize.x * cellSize * 0.95f,
                ghostHeight,
                effectiveSize.y * cellSize * 0.95f
            );

            // Re-check validity if pinned
            if (gridManager != null)
            {
                currentPlacementValid = gridManager.CanPlace(currentGridCell, effectiveSize);
                UpdateGhostColor(currentPlacementValid);
            }
        }

        Debug.Log($"[GhostPlacement] Rotated to {rotationAngle} degrees");
    }

    private Vector2Int GetEffectiveSize()
    {
        if (rotationAngle == 90 || rotationAngle == 270)
            return new Vector2Int(currentItemSize.y, currentItemSize.x);
        return currentItemSize;
    }

    // ── Ghost Object ──

    private void CreateGhostObject()
    {
        if (ghostObject == null)
        {
            ghostObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghostObject.name = "BuildGhost";

            // Disable collider so it doesn't block raycasts
            var col = ghostObject.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Create transparent material
            ghostRenderer = ghostObject.GetComponent<Renderer>();
            ghostMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            // URP Lit transparency settings
            ghostMaterial.SetFloat("_Surface", 1); // 0=Opaque, 1=Transparent
            ghostMaterial.SetFloat("_Blend", 0);   // 0=Alpha, 1=Premultiply
            ghostMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            ghostMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            ghostMaterial.SetFloat("_ZWrite", 0);
            ghostMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            ghostMaterial.renderQueue = 3000;
            ghostRenderer.material = ghostMaterial;

            // Add a small indicator to show the "Front" so 1x1 items visibly rotate
            GameObject forwardIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            forwardIndicator.name = "FrontIndicator";
            forwardIndicator.transform.SetParent(ghostObject.transform);
            // Local pos: at the front face (Z = +0.5)
            forwardIndicator.transform.localPosition = new Vector3(0f, 0.5f, 0.5f); 
            // Local scale: small box
            forwardIndicator.transform.localScale = new Vector3(0.2f, 0.2f, 0.1f);
            
            var indicatorCol = forwardIndicator.GetComponent<Collider>();
            if (indicatorCol != null) Destroy(indicatorCol);
            
            var indicatorRenderer = forwardIndicator.GetComponent<Renderer>();
            if (indicatorRenderer != null)
            {
                Material indMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                indMat.color = Color.yellow;
                indicatorRenderer.material = indMat;
            }
        }

        ghostObject.SetActive(true);

        // Set size based on item
        float cellSize = gridManager != null ? gridManager.CellSize : 1f;
        ghostObject.transform.localScale = new Vector3(
            currentItemSize.x * cellSize * 0.95f,
            ghostHeight,
            currentItemSize.y * cellSize * 0.95f
        );
        ghostObject.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);

        UpdateGhostColor(false);
    }

    private void UpdateGhostColor(bool valid)
    {
        if (ghostMaterial != null)
        {
            ghostMaterial.color = valid ? validColor : invalidColor;
        }
    }

    public bool IsPlacementValid => currentPlacementValid;

    // ── Placement ──

    public void ConfirmPlacement()
    {
        if (gridManager == null) return;

        Vector2Int effectiveSize = GetEffectiveSize();

        // Mark cells as occupied
        gridManager.OccupyCells(currentGridCell, effectiveSize);

        // Spawn placed building (Cube placeholder)
        Vector3 worldPos = gridManager.GridToWorldCentered(currentGridCell, effectiveSize);
        if (ghostObject != null)
        {
            worldPos.y = ghostObject.transform.position.y;
        }
        SpawnPlacedBuilding(worldPos, effectiveSize);

        // Notify listeners (UI controller handles balance deduction)
        OnBuildingPlaced?.Invoke(currentItemName, currentItemPrice);

        Debug.Log($"[GhostPlacement] Placed '{currentItemName}' at grid ({currentGridCell.x},{currentGridCell.y}) rotation {rotationAngle}");

        // Keep ghost active for continued placement
        // Player can place multiple of the same item
    }

    private void SpawnPlacedBuilding(Vector3 position, Vector2Int size)
    {
        float cellSize = gridManager != null ? gridManager.CellSize : 1f;

        // ƯU TIÊN: nếu có prefab THẬT khớp tên item (đất/hàng rào...) thì sinh prefab đó.
        var lib = YWonderLand.Environment.BuildPrefabLibrary.Instance;
        var entry = lib != null ? lib.Find(currentItemName) : null;
        if (entry != null)
        {
            SpawnPrefabBuilding(entry, position, size, cellSize);
            return;
        }

        // FALLBACK: chưa khai báo prefab -> khối Cube placeholder như cũ.
        GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building.name = $"Building_{currentItemName}_{currentGridCell.x}_{currentGridCell.y}";
        building.transform.position = position;
        building.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
        building.transform.localScale = new Vector3(
            size.x * cellSize * 0.95f,
            ghostHeight * 1.5f,
            size.y * cellSize * 0.95f
        );

        // Give it a solid color based on category
        Renderer renderer = building.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            // Ensure spawned building is opaque (URP Lit defaults)
            mat.SetFloat("_Surface", 0); // 0=Opaque
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetFloat("_ZWrite", 1);
            mat.renderQueue = -1; // Use shader default queue
            mat.color = GetBuildingColor();
            renderer.material = mat;
        }

        // Add a small indicator to show the "Front" so 1x1 items visibly rotate
        GameObject forwardIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        forwardIndicator.name = "FrontIndicator";
        forwardIndicator.transform.SetParent(building.transform);
        forwardIndicator.transform.localPosition = new Vector3(0f, 0.5f, 0.5f); 
        forwardIndicator.transform.localScale = new Vector3(0.2f, 0.2f, 0.1f);
        
        var indicatorRenderer = forwardIndicator.GetComponent<Renderer>();
        if (indicatorRenderer != null)
        {
            Material indMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            indMat.color = Color.yellow;
            indicatorRenderer.material = indMat;
        }

        // Tag for identification (used by Delete/Move tools)
        building.tag = "PlacedBuilding";

        Debug.Log($"[GhostPlacement] Spawned building '{building.name}' at {position}");
    }

    private Color GetBuildingColor()
    {
        // Simple color based on item name hash for variety
        int hash = currentItemName.GetHashCode();
        float hue = Mathf.Abs(hash % 360) / 360f;
        return Color.HSVToRGB(hue, 0.5f, 0.8f);
    }

    // ── Sinh PREFAB thật (đất có FarmTile, hàng rào...) ──

    private void SpawnPrefabBuilding(YWonderLand.Environment.BuildPrefabLibrary.Entry entry, Vector3 position, Vector2Int size, float cellSize)
    {
        // Hạ về sát mặt đất (ghost được nâng lên ghostHeight*0.5 để xem preview) + offset chỉnh tay theo prefab.
        Vector3 groundPos = position;
        groundPos.y = position.y - ghostHeight * 0.5f + entry.yOffset;

        // GIỮ rotation gốc của prefab (vd chuồng đã dựng nằm ngang sẵn), chỉ XOAY THÊM yaw khi người chơi bấm R.
        // Tránh ghi đè rotation làm prefab bị dựng đứng/lệch hướng.
        Quaternion finalRot = Quaternion.Euler(0, rotationAngle, 0) * entry.prefab.transform.rotation;
        GameObject go = Instantiate(entry.prefab, groundPos, finalRot);
        go.name = $"Building_{currentItemName}_{currentGridCell.x}_{currentGridCell.y}";

        // Đất: co giãn cho vừa ô lưới. Hàng rào/trang trí: giữ nguyên prefab.
        if (entry.stretchToFootprint)
        {
            StretchToFootprint(go, size.x * cellSize * 0.95f, size.y * cellSize * 0.95f);
        }

        // Cho phép Xóa/Di chuyển trong Build Mode (chỉ gán khi prefab chưa có tag riêng).
        // FarmTile nhận diện qua component nên tag KHÔNG ảnh hưởng việc trồng cây.
        if (go.CompareTag("Untagged")) go.tag = "PlacedBuilding";

        Debug.Log($"[GhostPlacement] Item '{currentItemName}' → khớp '{entry.nameContains}' → prefab '{entry.prefab.name}' (stretch={entry.stretchToFootprint})");
    }

    /// <summary>Co giãn prefab theo trục X/Z (world) cho vừa footprint ô lưới, giữ nguyên chiều cao.</summary>
    private void StretchToFootprint(GameObject go, float targetX, float targetZ)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        float curX = b.size.x, curZ = b.size.z;
        if (curX < 0.0001f || curZ < 0.0001f) return;

        Vector3 s = go.transform.localScale;
        s.x *= targetX / curX;
        s.z *= targetZ / curZ;
        go.transform.localScale = s;
    }
}
