using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controller for Build Mode Overlay.
/// Manages category browsing, item selection, placement feedback (mockup).
/// Toggle with B key or HUD button.
/// </summary>
public class BuildModeOverlayController : MonoBehaviour
{
    public static BuildModeOverlayController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private UIDocument buildDocument;

    // State
    private enum BuildState { Hidden, Browsing, Selected }
    private BuildState state = BuildState.Hidden;

    // UI Elements
    private VisualElement buildRoot;
    private VisualElement detailTooltip;
    private Label lblBuildBalance;
    private Label lblBuildStatus;
    private Label tooltipIcon;
    private Label tooltipName;
    private Label tooltipSize;
    private Label tooltipPrice;
    private Label tooltipDesc;
    private Button btnPlace;
    private Button btnExitBuild;
    private Button btnRotate;
    private Button btnDelete;
    private Button btnMove;
    private Button btnSave;
    private Button btnUndo;
    private ScrollView itemScrollView;

    // Category buttons
    private Button[] categoryButtons;
    private int activeCategoryIndex = 0;

    // Selected item
    private int selectedItemIndex = -1;
    private VisualElement selectedCardElement;

    // Mock balance
    private int currentBalance = 5000;

    // Rotation state (mockup)
    private int rotationDegrees = 0;

    // Active tool
    private enum BuildTool { Place, Delete, Move }
    private BuildTool activeTool = BuildTool.Place;

    // Raycast for delete/move tools
    [Header("Delete/Move Raycast")]
    [SerializeField] private LayerMask buildingRayMask = ~0;

    // ── Mock Data ──

    private struct BuildItemData
    {
        public string emoji;
        public string name;
        public string size;
        public int price;
        public string description;

        public BuildItemData(string emoji, string name, string size, int price, string description)
        {
            this.emoji = emoji;
            this.name = name;
            this.size = size;
            this.price = price;
            this.description = description;
        }
    }

    private static readonly Dictionary<int, List<BuildItemData>> categoryData = new Dictionary<int, List<BuildItemData>>()
    {
        { 0, new List<BuildItemData> // Nha cua
            {
                new BuildItemData("\U0001F3E0", "Nhà gỗ nhỏ", "2x2", 500, "Ngôi nhà gỗ ấm cúng cho gia đình nhỏ."),
                new BuildItemData("\U0001F3E1", "Nhà gạch", "3x3", 1200, "Ngôi nhà gạch vững chắc, rộng rãi."),
                new BuildItemData("\U0001F3EA", "Kho chứa", "2x3", 800, "Kho lưu trữ nông sản và dụng cụ."),
                new BuildItemData("\U0001F414", "Chuồng gà", "2x2", 600, "Nuôi gà đẻ trứng mỗi ngày."),
                new BuildItemData("\U0001F404", "Chuồng bò", "3x3", 1500, "Nuôi bò lấy sữa tươi."),
            }
        },
        { 1, new List<BuildItemData> // Nong trai
            {
                new BuildItemData("\U0001F33F", "Ruộng 2x2", "2x2", 100, "Ô đất nhỏ để gieo hạt."),
                new BuildItemData("\U0001F33E", "Ruộng 3x3", "3x3", 200, "Ô đất lớn, trồng được nhiều hơn."),
                new BuildItemData("\U0001F3E1", "Nhà kính", "4x4", 2000, "Trồng cây quanh năm không lo thời tiết."),
                new BuildItemData("\U0001F4A7", "Giếng nước", "1x1", 300, "Nguồn nước tưới tiêu gần ruộng."),
            }
        },
        { 2, new List<BuildItemData> // Hang rao
            {
                new BuildItemData("\U0001FAB5", "Rào gỗ", "1x1", 20, "Hàng rào gỗ đơn giản."),
                new BuildItemData("\U0001FAA8", "Rào đá", "1x1", 50, "Hàng rào đá chắc chắn."),
                new BuildItemData("\U0001F6AA", "Cổng gỗ", "1x1", 80, "Cổng ra vào nông trại."),
                new BuildItemData("\U0001F338", "Rào hoa", "1x1", 40, "Hàng rào trang trí bằng hoa."),
            }
        },
        { 3, new List<BuildItemData> // Trang tri
            {
                new BuildItemData("\U0001F332", "Cây cảnh", "1x1", 30, "Cây xanh trang trí sân vườn."),
                new BuildItemData("\U0001F3EE", "Đèn lồng", "1x1", 60, "Đèn lồng sáng lung linh ban đêm."),
                new BuildItemData("\U0001FA91", "Ghế đá", "1x1", 45, "Ghế nghỉ chân trong vườn."),
                new BuildItemData("\U0001F5FF", "Tượng vườn", "1x1", 120, "Bức tượng trang trí nghệ thuật."),
                new BuildItemData("\U0001F33A", "Bồn hoa", "1x1", 35, "Bồn hoa nhiều màu sắc."),
            }
        },
        { 4, new List<BuildItemData> // Duong di
            {
                new BuildItemData("\U0001F6E4", "Đường đất", "1x1", 10, "Con đường đất giản dị."),
                new BuildItemData("\U0001F9F1", "Đường gạch", "1x1", 25, "Đường lát gạch đỏ sạch sẽ."),
                new BuildItemData("\U0001FAA8", "Đường đá", "1x1", 40, "Đường đá tự nhiên bền bỉ."),
                new BuildItemData("\U0001F309", "Cầu gỗ nhỏ", "2x1", 150, "Cầu gỗ bắc qua suối nhỏ."),
            }
        }
    };

    // ── Unity Lifecycle ──

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

    void OnEnable()
    {
        if (buildDocument == null)
            buildDocument = GetComponent<UIDocument>();

        if (buildDocument == null)
        {
            Debug.LogError("[BuildMode] UIDocument component not found!");
            return;
        }

        var root = buildDocument.rootVisualElement;
        QueryElements(root);
        RegisterCallbacks();
        Hide();
    }

    void Update()
    {
        if (state == BuildState.Hidden) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // R = Rotate
        if (keyboard.rKey.wasPressedThisFrame)
        {
            OnRotateClicked();
        }

        // Delete = Delete mode
        if (keyboard.deleteKey.wasPressedThisFrame)
        {
            OnDeleteClicked();
        }

        // Handle Delete/Move tool raycasting on left-click
        HandleDeleteAndMoveTool();

        // Escape = Exit
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Hide();
        }
    }

    // ── Query & Register ──

    private void QueryElements(VisualElement root)
    {
        buildRoot = root.Q<VisualElement>("BuildRoot");
        detailTooltip = root.Q<VisualElement>("DetailTooltip");
        lblBuildBalance = root.Q<Label>("lblBuildBalance");
        lblBuildStatus = root.Q<Label>("lblBuildStatus");
        tooltipIcon = root.Q<Label>("TooltipIcon");
        tooltipName = root.Q<Label>("TooltipName");
        tooltipSize = root.Q<Label>("TooltipSize");
        tooltipPrice = root.Q<Label>("TooltipPrice");
        tooltipDesc = root.Q<Label>("TooltipDesc");
        btnPlace = root.Q<Button>("BtnPlace");
        btnExitBuild = root.Q<Button>("BtnExitBuild");
        btnRotate = root.Q<Button>("BtnRotate");
        btnDelete = root.Q<Button>("BtnDelete");
        btnMove = root.Q<Button>("BtnMove");
        btnSave = root.Q<Button>("BtnSave");
        btnUndo = root.Q<Button>("BtnUndo");
        itemScrollView = root.Q<ScrollView>("ItemScrollView");

        // Category buttons
        categoryButtons = new Button[]
        {
            root.Q<Button>("CatBuildings"),
            root.Q<Button>("CatFarm"),
            root.Q<Button>("CatFence"),
            root.Q<Button>("CatDecor"),
            root.Q<Button>("CatPath")
        };
    }

    private void RegisterCallbacks()
    {
        // Category tabs
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            int index = i;
            categoryButtons[i]?.RegisterCallback<ClickEvent>(evt => SelectCategory(index));
        }

        // Tool buttons
        btnExitBuild?.RegisterCallback<ClickEvent>(evt => Hide());
        btnRotate?.RegisterCallback<ClickEvent>(evt => OnRotateClicked());
        btnDelete?.RegisterCallback<ClickEvent>(evt => OnDeleteClicked());
        btnMove?.RegisterCallback<ClickEvent>(evt => OnMoveClicked());
        btnSave?.RegisterCallback<ClickEvent>(evt => OnSaveClicked());
        btnUndo?.RegisterCallback<ClickEvent>(evt => OnUndoClicked());
        btnPlace?.RegisterCallback<ClickEvent>(evt => OnPlaceClicked());
    }

    // ── Show / Hide ──

    public void Show()
    {
        if (buildDocument != null)
        {
            buildDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        }
        state = BuildState.Browsing;
        activeCategoryIndex = 0;
        selectedItemIndex = -1;
        rotationDegrees = 0;
        activeTool = BuildTool.Place;

        UpdateBalance();
        UpdateCategoryTabs();
        RebuildItemGrid();
        HideTooltip();

        // Grid follows player continuously
        if (BuildGridManager.Instance != null)
        {
            // Find player transform via camera target
            Transform playerTransform = null;
            var tpCam = FindFirstObjectByType<ThirdPersonCamera>();
            if (tpCam != null && tpCam.target != null)
            {
                playerTransform = tpCam.target;
            }
            BuildGridManager.Instance.SetFollowTarget(playerTransform);
        }

        // Activate Top-Down camera
        if (BuildCameraController.Instance != null)
            BuildCameraController.Instance.Activate();

        // Show grid overlay
        var gridRenderer = FindFirstObjectByType<BuildGridRenderer>();
        if (gridRenderer != null)
            gridRenderer.Show();

        // Subscribe to building placed event for balance deduction
        if (GhostPlacementController.Instance != null)
            GhostPlacementController.Instance.OnBuildingPlaced += OnBuildingPlacedHandler;

        Debug.Log("[BuildMode] Build Mode opened");
    }

    public void Hide()
    {
        if (buildDocument != null)
        {
            buildDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
        state = BuildState.Hidden;

        // Unsubscribe from building placed event
        if (GhostPlacementController.Instance != null)
            GhostPlacementController.Instance.OnBuildingPlaced -= OnBuildingPlacedHandler;

        // Deactivate ghost and camera
        if (GhostPlacementController.Instance != null)
            GhostPlacementController.Instance.Deactivate();
        if (BuildCameraController.Instance != null)
            BuildCameraController.Instance.Deactivate();

        // Stop grid following + hide
        if (BuildGridManager.Instance != null)
            BuildGridManager.Instance.StopFollowing();
        var gridRenderer = FindFirstObjectByType<BuildGridRenderer>();
        if (gridRenderer != null)
            gridRenderer.Hide();

        Debug.Log("[BuildMode] Build Mode closed");
    }

    public bool IsVisible()
    {
        return state != BuildState.Hidden;
    }

    // ── Category Selection ──

    private void SelectCategory(int index)
    {
        activeCategoryIndex = index;
        selectedItemIndex = -1;
        state = BuildState.Browsing;

        UpdateCategoryTabs();
        RebuildItemGrid();
        HideTooltip();

        Debug.Log($"[BuildMode] Category selected: {index}");
    }

    private void UpdateCategoryTabs()
    {
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            if (categoryButtons[i] == null) continue;

            if (i == activeCategoryIndex)
            {
                categoryButtons[i].AddToClassList("build-cat-active");
            }
            else
            {
                categoryButtons[i].RemoveFromClassList("build-cat-active");
            }
        }
    }

    // ── Item Grid ──

    private void RebuildItemGrid()
    {
        if (itemScrollView == null) return;

        // Clear existing items
        var container = itemScrollView.contentContainer;
        container.Clear();

        if (!categoryData.ContainsKey(activeCategoryIndex)) return;

        var items = categoryData[activeCategoryIndex];
        for (int i = 0; i < items.Count; i++)
        {
            int index = i;
            var item = items[i];

            // Create item card
            var card = new VisualElement();
            card.AddToClassList("build-item-card");

            var emojiLabel = new Label(item.emoji);
            emojiLabel.AddToClassList("build-item-emoji");
            card.Add(emojiLabel);

            var nameLabel = new Label(item.name);
            nameLabel.AddToClassList("build-item-name");
            card.Add(nameLabel);

            var priceLabel = new Label($"{item.price} POS");
            priceLabel.AddToClassList("build-item-price");
            card.Add(priceLabel);

            // Click handler
            card.RegisterCallback<ClickEvent>(evt =>
            {
                SelectItem(index, card);
            });

            container.Add(card);
        }
    }

    // ── Item Selection ──

    private void SelectItem(int index, VisualElement cardElement)
    {
        // Deselect previous
        if (selectedCardElement != null)
        {
            selectedCardElement.RemoveFromClassList("build-item-selected");
        }

        selectedItemIndex = index;
        selectedCardElement = cardElement;
        state = BuildState.Selected;

        // Highlight selected
        cardElement.AddToClassList("build-item-selected");

        // Show tooltip
        ShowTooltip(index);

        Debug.Log($"[BuildMode] Item selected: {index}");
    }

    private void ShowTooltip(int index)
    {
        if (!categoryData.ContainsKey(activeCategoryIndex)) return;

        var items = categoryData[activeCategoryIndex];
        if (index < 0 || index >= items.Count) return;

        var item = items[index];

        if (tooltipIcon != null) tooltipIcon.text = item.emoji;
        if (tooltipName != null) tooltipName.text = item.name;
        if (tooltipSize != null) tooltipSize.text = item.size;
        if (tooltipPrice != null) tooltipPrice.text = $"{item.price} POS";
        if (tooltipDesc != null) tooltipDesc.text = item.description;

        detailTooltip?.RemoveFromClassList("hidden");
    }

    private void HideTooltip()
    {
        detailTooltip?.AddToClassList("hidden");
    }

    // ── Tool Actions (Mockup) ──

    private void OnPlaceClicked()
    {
        if (selectedItemIndex < 0 || !categoryData.ContainsKey(activeCategoryIndex)) return;

        var items = categoryData[activeCategoryIndex];
        if (selectedItemIndex >= items.Count) return;

        var item = items[selectedItemIndex];

        // Activate ghost placement
        if (GhostPlacementController.Instance != null)
        {
            Vector2Int size = ParseSize(item.size);
            GhostPlacementController.Instance.Activate(item.name, size, item.price);
            ShowStatusMessage($"Chọn vị trí để đặt {item.name}...", true);
        }
        else
        {
            // Fallback: mockup mode (no ghost system)
            if (currentBalance < item.price)
            {
                ShowStatusMessage($"Không đủ POS! Cần {item.price} POS.", false);
                return;
            }
            currentBalance -= item.price;
            UpdateBalance();
            ShowStatusMessage($"Đặt {item.name} thành công! (-{item.price} POS)", true);
        }

        Debug.Log($"[BuildMode] Place requested: {item.name} at rotation {rotationDegrees} degrees");
    }

    private Vector2Int ParseSize(string sizeStr)
    {
        string[] parts = sizeStr.Split('x');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out int w) &&
            int.TryParse(parts[1], out int h))
        {
            return new Vector2Int(w, h);
        }
        return new Vector2Int(1, 1);
    }

    private void OnRotateClicked()
    {
        rotationDegrees = (rotationDegrees + 90) % 360;

        // Rotate ghost if active
        if (GhostPlacementController.Instance != null && GhostPlacementController.Instance.IsActive)
            GhostPlacementController.Instance.Rotate();

        ShowStatusMessage($"Xoay {rotationDegrees}°", true);
        Debug.Log($"[BuildMode] Rotate: {rotationDegrees} degrees");
    }

    private void OnDeleteClicked()
    {
        activeTool = BuildTool.Delete;

        // Deactivate ghost preview when switching to delete
        if (GhostPlacementController.Instance != null && GhostPlacementController.Instance.IsActive)
            GhostPlacementController.Instance.Deactivate();

        ShowStatusMessage("Ch\u1ebf \u0111\u1ed9 x\u00f3a \u2014 b\u1ea5m v\u00e0o c\u00f4ng tr\u00ecnh \u0111\u1ec3 x\u00f3a", true);
        Debug.Log("[BuildMode] Delete mode activated");
    }

    private void OnMoveClicked()
    {
        activeTool = BuildTool.Move;

        // Deactivate ghost preview when switching to move
        if (GhostPlacementController.Instance != null && GhostPlacementController.Instance.IsActive)
            GhostPlacementController.Instance.Deactivate();

        ShowStatusMessage("Ch\u1ebf \u0111\u1ed9 di chuy\u1ec3n \u2014 b\u1ea5m v\u00e0o c\u00f4ng tr\u00ecnh \u0111\u1ec3 nh\u1ea5c", true);
        Debug.Log("[BuildMode] Move mode activated");
    }

    private void OnSaveClicked()
    {
        ShowStatusMessage("Đã lưu bố cục nông trại!", true);
        Debug.Log("[BuildMode] Layout saved (mockup)");
    }

    private void OnUndoClicked()
    {
        ShowStatusMessage("Ho\u00e0n t\u00e1c thao t\u00e1c tr\u01b0\u1edbc!", true);
        Debug.Log("[BuildMode] Undo last action (mockup)");
    }

    // ── Delete / Move Tool Logic ──

    /// <summary>
    /// Called every frame from Update(). When Delete or Move tool is active,
    /// raycasts on left-click to find "PlacedBuilding" objects.
    /// </summary>
    private void HandleDeleteAndMoveTool()
    {
        if (activeTool != BuildTool.Delete && activeTool != BuildTool.Move) return;

        var mouse = Mouse.current;
        if (mouse == null) return;
        if (!mouse.leftButton.wasPressedThisFrame) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector2 mousePos = mouse.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, buildingRayMask)) return;

        GameObject hitObj = hit.collider.gameObject;
        if (!hitObj.CompareTag("PlacedBuilding")) return;

        if (activeTool == BuildTool.Delete)
        {
            DeleteBuildingAt(hitObj);
        }
        else if (activeTool == BuildTool.Move)
        {
            PickUpBuilding(hitObj);
        }
    }

    /// <summary>
    /// Destroy a placed building and free its grid cells.
    /// </summary>
    private void DeleteBuildingAt(GameObject building)
    {
        // Free grid cells occupied by this building
        if (BuildGridManager.Instance != null)
        {
            Vector2Int gridCell = BuildGridManager.Instance.WorldToGrid(building.transform.position);
            // Estimate size from the object's local scale and grid cell size
            float cellSize = BuildGridManager.Instance.CellSize;
            int sizeX = Mathf.Max(1, Mathf.RoundToInt(building.transform.localScale.x / (cellSize * 0.95f)));
            int sizeZ = Mathf.Max(1, Mathf.RoundToInt(building.transform.localScale.z / (cellSize * 0.95f)));
            Vector2Int size = new Vector2Int(sizeX, sizeZ);

            // Recalculate the origin cell (WorldToGrid gives center, need corner)
            Vector2Int originCell = new Vector2Int(
                gridCell.x - (sizeX / 2),
                gridCell.y - (sizeZ / 2)
            );
            BuildGridManager.Instance.FreeCells(originCell, size);
        }

        Destroy(building);
        ShowStatusMessage("\u0110\u00e3 x\u00f3a!", true); // "Đã xóa!"
        Debug.Log($"[BuildMode] Deleted building: {building.name}");
    }

    /// <summary>
    /// Pick up a placed building (destroy it, free cells, activate ghost at same position).
    /// Player can then re-place it.
    /// </summary>
    private void PickUpBuilding(GameObject building)
    {
        // Read building info before destroying
        Vector3 buildingPos = building.transform.position;
        Vector3 buildingScale = building.transform.localScale;
        string buildingName = building.name;

        // Estimate size from scale
        float cellSize = BuildGridManager.Instance != null ? BuildGridManager.Instance.CellSize : 1f;
        int sizeX = Mathf.Max(1, Mathf.RoundToInt(buildingScale.x / (cellSize * 0.95f)));
        int sizeZ = Mathf.Max(1, Mathf.RoundToInt(buildingScale.z / (cellSize * 0.95f)));
        Vector2Int size = new Vector2Int(sizeX, sizeZ);

        // Free grid cells
        if (BuildGridManager.Instance != null)
        {
            Vector2Int gridCell = BuildGridManager.Instance.WorldToGrid(buildingPos);
            Vector2Int originCell = new Vector2Int(
                gridCell.x - (sizeX / 2),
                gridCell.y - (sizeZ / 2)
            );
            BuildGridManager.Instance.FreeCells(originCell, size);
        }

        // Destroy the original
        Destroy(building);

        // Activate ghost at the same position for re-placement
        if (GhostPlacementController.Instance != null)
        {
            // Extract item name from building name (format: Building_ItemName_X_Y)
            string itemName = buildingName;
            if (buildingName.StartsWith("Building_"))
            {
                string[] parts = buildingName.Split('_');
                if (parts.Length >= 2) itemName = parts[1];
            }

            GhostPlacementController.Instance.Activate(itemName, size, 0);
            activeTool = BuildTool.Place;
        }

        ShowStatusMessage("\u0110\u00e3 nh\u1ea5c c\u00f4ng tr\u00ecnh \u2014 ch\u1ecdn v\u1ecb tr\u00ed m\u1edbi", true); // "Đã nhấc công trình — chọn vị trí mới"
        Debug.Log($"[BuildMode] Picked up building: {buildingName}");
    }

    // ── Balance ──

    private void UpdateBalance()
    {
        if (lblBuildBalance != null)
        {
            lblBuildBalance.text = $"{currentBalance:N0} POS";
        }
    }

    /// <summary>
    /// Called when a building is placed via the ghost system.
    /// Deducts the item price from mock balance and updates the UI.
    /// </summary>
    private void OnBuildingPlacedHandler(int price)
    {
        if (currentBalance < price)
        {
            ShowStatusMessage($"Kh\u00F4ng \u0111\u1EE7 POS! C\u1EA7n {price} POS.", false);
            return;
        }

        currentBalance -= price;
        UpdateBalance();
        ShowStatusMessage($"\u0110\u1EB7t th\u00E0nh c\u00F4ng! (-{price} POS)", true);
    }

    // ── Status Message (Fade-out) ──

    private Coroutine statusCoroutine;

    private void ShowStatusMessage(string message, bool isSuccess)
    {
        if (lblBuildStatus == null) return;

        if (statusCoroutine != null)
        {
            StopCoroutine(statusCoroutine);
        }

        lblBuildStatus.text = message;
        lblBuildStatus.RemoveFromClassList("hidden");
        lblBuildStatus.RemoveFromClassList("build-status-success");
        lblBuildStatus.RemoveFromClassList("build-status-error");
        lblBuildStatus.AddToClassList(isSuccess ? "build-status-success" : "build-status-error");
        lblBuildStatus.style.opacity = 1f;

        statusCoroutine = StartCoroutine(FadeOutStatus());
    }

    private IEnumerator FadeOutStatus()
    {
        yield return new WaitForSeconds(1.5f);

        // Fade out
        if (lblBuildStatus != null)
        {
            lblBuildStatus.style.opacity = 0f;
        }

        yield return new WaitForSeconds(0.5f);

        if (lblBuildStatus != null)
        {
            lblBuildStatus.AddToClassList("hidden");
            lblBuildStatus.style.opacity = 1f;
        }
    }
}
