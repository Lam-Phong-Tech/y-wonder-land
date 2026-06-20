using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controller for Build Mode Overlay — v2 Redesign.
/// Clean header (exit only), contextual placement controls,
/// contextual menu for editing placed buildings.
/// Toggle with B key or HUD button.
/// </summary>
public class BuildModeOverlayController : MonoBehaviour
{
    public static BuildModeOverlayController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private UIDocument buildDocument;

    [Header("Delete/Move Raycast")]
    [SerializeField] private LayerMask buildingRayMask = ~0;

    // State
    private enum BuildState { Hidden, Browsing, Placing }
    private BuildState state = BuildState.Hidden;

    // UI Elements — Header
    private VisualElement buildRoot;
    private Label lblBuildBalance;
    private Label lblBuildStatus;
    private Button btnExitBuild;

    // UI Elements — Item Bar
    private ScrollView itemScrollView;

    // UI Elements — Placement Controls (confirm/rotate/cancel)
    private VisualElement placementControls;
    private Button btnConfirmPlace;
    private Button btnRotatePlace;
    private Button btnCancelPlace;

    // UI Elements — Contextual Menu (for placed buildings)
    private VisualElement contextMenu;
    private Button btnCtxRotate;
    private Button btnCtxMove;
    private Button btnCtxDelete;

    // UI Elements — Info Tooltip
    private VisualElement infoTooltip;
    private Label infoTooltipName;
    private Label infoTooltipSize;
    private Label infoTooltipPrice;
    private Label infoTooltipDesc;

    // Category buttons
    private Button[] categoryButtons;
    private int activeCategoryIndex = 0;

    // Selected item
    private int selectedItemIndex = -1;
    private VisualElement selectedCardElement;

    // Mock balance
    private int currentBalance = 5000;

    // Context-selected building
    private GameObject contextSelectedBuilding;

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

    // Menu Build r\u00fat g\u1ecdn \u2014 ch\u1ec9 3 m\u1ee5c: Ru\u1ed9ng / \u0110\u01b0\u1eddng \u0111\u00e1 / Chu\u1ed3ng (r\u00e0o x\u1ecbn). 1 category duy nh\u1ea5t.
    private static readonly Dictionary<int, List<BuildItemData>> categoryData = new Dictionary<int, List<BuildItemData>>()
    {
        { 0, new List<BuildItemData> // X\u00e2y d\u1ef1ng
            {
                new BuildItemData("\U0001F33E", "Ru\u1ed9ng", "1x1", 50, "\u00d4 \u0111\u1ea5t canh t\u00e1c \u2014 \u0111\u1eb7t t\u1eebng \u00f4 \u0111\u1ec3 tr\u1ed3ng c\u00e2y."),
                new BuildItemData("\U0001FAA8", "\u0110\u01b0\u1eddng \u0111\u00e1", "1x1", 40, "M\u1eb7t \u0111\u01b0\u1eddng \u0111\u00e1 l\u00e1t l\u1ed1i \u0111i."),
                new BuildItemData("\U0001F404", "Chu\u1ed3ng", "1x1", 50, "H\u00e0ng r\u00e0o qu\u00e2y chu\u1ed3ng \u2014 gh\u00e9p nhi\u1ec1u \u00f4 th\u00e0nh chu\u1ed3ng to."),
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
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        // R = Rotate (when placing)
        if (keyboard.rKey.wasPressedThisFrame && state == BuildState.Placing)
        {
            OnRotatePlacement();
        }

        // Escape = Exit build mode or cancel placement
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (state == BuildState.Placing)
            {
                OnCancelPlacement();
            }
            else
            {
                Hide();
            }
        }

        if (state == BuildState.Placing)
        {
            var ghost = GhostPlacementController.Instance;
            if (ghost != null && ghost.IsActive)
            {
                if (ghost.IsPinned)
                {
                    UpdatePlacementControlsPosition(ghost.GhostPosition);
                }

                if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI())
                {
                    if (!ghost.IsPinned)
                    {
                        ghost.SetPinned(true);
                        ShowPlacementControls();
                        ShowStatusMessage("\u0110\u00e3 ghim v\u1ecb tr\u00ed. B\u1ea5m \u2714 \u0111\u1ec3 x\u00e2y.", true); // "Đã ghim vị trí. Bấm ✔ để xây."
                    }
                    else
                    {
                        // Unpin to move again
                        ghost.SetPinned(false);
                        HidePlacementControls();
                    }
                }
            }
        }
        else if (state == BuildState.Browsing)
        {
            if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI())
            {
                HandleContextualClick(mouse.position.ReadValue());
            }
        }
    }

    private bool IsPointerOverUI()
    {
        if (buildDocument == null || buildDocument.rootVisualElement == null || buildDocument.rootVisualElement.panel == null) return false;
        var mousePos = Mouse.current.position.ReadValue();
        Vector2 invertedY = new Vector2(mousePos.x, Screen.height - mousePos.y);
        var picked = buildDocument.rootVisualElement.panel.Pick(RuntimePanelUtils.ScreenToPanel(buildDocument.rootVisualElement.panel, invertedY));
        return picked != null && picked != buildRoot;
    }

    private void UpdatePlacementControlsPosition(Vector3 worldPos)
    {
        if (placementControls == null || Camera.main == null) return;
        
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        if (screenPos.z < 0) 
        {
            placementControls.style.display = DisplayStyle.None;
            return;
        }
        
        placementControls.style.display = DisplayStyle.Flex;
        var panelPos = RuntimePanelUtils.ScreenToPanel(
            buildDocument.rootVisualElement.panel,
            new Vector2(screenPos.x, Screen.height - screenPos.y)
        );

        placementControls.style.left = panelPos.x - 96f; // half of 192px width
        placementControls.style.top = panelPos.y + 40f; // slightly below the building
    }

    // ── Query & Register ──

    private void QueryElements(VisualElement root)
    {
        buildRoot = root.Q<VisualElement>("BuildRoot");
        lblBuildBalance = root.Q<Label>("lblBuildBalance");
        lblBuildStatus = root.Q<Label>("lblBuildStatus");
        btnExitBuild = root.Q<Button>("BtnExitBuild");
        itemScrollView = root.Q<ScrollView>("ItemScrollView");

        // Placement controls
        placementControls = root.Q<VisualElement>("PlacementControls");
        btnConfirmPlace = root.Q<Button>("BtnConfirmPlace");
        btnRotatePlace = root.Q<Button>("BtnRotatePlace");
        btnCancelPlace = root.Q<Button>("BtnCancelPlace");

        // Contextual menu
        contextMenu = root.Q<VisualElement>("ContextMenu");
        btnCtxRotate = root.Q<Button>("BtnCtxRotate");
        btnCtxMove = root.Q<Button>("BtnCtxMove");
        btnCtxDelete = root.Q<Button>("BtnCtxDelete");

        // Info tooltip
        infoTooltip = root.Q<VisualElement>("InfoTooltip");
        infoTooltipName = root.Q<Label>("InfoTooltipName");
        infoTooltipSize = root.Q<Label>("InfoTooltipSize");
        infoTooltipPrice = root.Q<Label>("InfoTooltipPrice");
        infoTooltipDesc = root.Q<Label>("InfoTooltipDesc");

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

        // Header
        btnExitBuild?.RegisterCallback<ClickEvent>(evt => Hide());

        // Placement controls
        btnConfirmPlace?.RegisterCallback<ClickEvent>(evt => OnConfirmPlacement());
        btnRotatePlace?.RegisterCallback<ClickEvent>(evt => OnRotatePlacement());
        btnCancelPlace?.RegisterCallback<ClickEvent>(evt => OnCancelPlacement());

        // Contextual menu
        btnCtxRotate?.RegisterCallback<ClickEvent>(evt => OnCtxRotate());
        btnCtxMove?.RegisterCallback<ClickEvent>(evt => OnCtxMove());
        btnCtxDelete?.RegisterCallback<ClickEvent>(evt => OnCtxDelete());
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

        UpdateBalance();
        UpdateCategoryTabs();
        RebuildItemGrid();
        HidePlacementControls();
        HideContextMenu();
        HideInfoTooltip();

        // Grid follows player continuously
        if (BuildGridManager.Instance != null)
        {
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

        // Lưới hiển thị: ĐÃ TẮT theo yêu cầu khách (giữ logic snap ô, chỉ bỏ phần vẽ lưới).
        var gridRenderer = FindFirstObjectByType<BuildGridRenderer>();
        if (gridRenderer != null)
            gridRenderer.Hide();

        // Subscribe to building placed event for balance deduction
        GhostPlacementController.OnBuildingPlaced -= OnBuildingPlacedHandler;
        GhostPlacementController.OnBuildingPlaced += OnBuildingPlacedHandler;

        // Hide GameHUD to prevent UI click overlaps
        SetGameHUDVisible(false);

        // Shift Chat up to avoid overlapping build items
        if (ChatPanelController.Instance != null)
        {
            ChatPanelController.Instance.ShiftForBuildMode(true);
        }

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
        GhostPlacementController.OnBuildingPlaced -= OnBuildingPlacedHandler;

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

        // Show GameHUD again
        SetGameHUDVisible(true);

        // Reset Chat position
        if (ChatPanelController.Instance != null)
        {
            ChatPanelController.Instance.ShiftForBuildMode(false);
        }

        Debug.Log("[BuildMode] Build Mode closed");
    }

    public bool IsVisible()
    {
        return state != BuildState.Hidden;
    }

    private void SetGameHUDVisible(bool visible)
    {
        GameObject hudGo = GameObject.Find("GameHUD") ?? GameObject.Find("HUD");
        if (hudGo != null)
        {
            var hudDoc = hudGo.GetComponent<UIDocument>();
            if (hudDoc != null && hudDoc.rootVisualElement != null)
            {
                hudDoc.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }

    // ── Category Selection ──

    private void SelectCategory(int index)
    {
        activeCategoryIndex = index;
        selectedItemIndex = -1;
        state = BuildState.Browsing;

        // Cancel any active ghost
        if (GhostPlacementController.Instance != null && GhostPlacementController.Instance.IsActive)
            GhostPlacementController.Instance.Deactivate();

        UpdateCategoryTabs();
        RebuildItemGrid();
        HidePlacementControls();
        HideContextMenu();
        HideInfoTooltip();

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

            // Info badge (i button)
            var infoBadge = new VisualElement();
            infoBadge.AddToClassList("build-item-info-badge");
            var infoIcon = new Label("i");
            infoIcon.AddToClassList("build-item-info-icon");
            infoBadge.Add(infoIcon);
            infoBadge.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                ShowInfoTooltip(index);
            });
            card.Add(infoBadge);

            // Click handler = select item and activate ghost
            card.RegisterCallback<ClickEvent>(evt =>
            {
                SelectAndActivateItem(index, card);
            });

            container.Add(card);
        }
    }

    // ── Item Selection & Ghost Activation ──

    private void SelectAndActivateItem(int index, VisualElement cardElement)
    {
        // Deselect previous
        if (selectedCardElement != null)
        {
            selectedCardElement.RemoveFromClassList("build-item-selected");
        }

        selectedItemIndex = index;
        selectedCardElement = cardElement;

        // Highlight selected
        cardElement.AddToClassList("build-item-selected");

        // Get item data
        if (!categoryData.ContainsKey(activeCategoryIndex)) return;
        var items = categoryData[activeCategoryIndex];
        if (index < 0 || index >= items.Count) return;
        var item = items[index];

        // Activate ghost placement immediately
        if (GhostPlacementController.Instance != null)
        {
            Vector2Int size = ParseSize(item.size);
            GhostPlacementController.Instance.Activate(item.name, size, item.price);
        }

        // Show placement controls (confirm/rotate/cancel)
        state = BuildState.Placing;
        HidePlacementControls(); // Wait for user to pin before showing
        HideContextMenu();
        HideInfoTooltip();

        ShowStatusMessage($"Click chu\u1ed9t \u0111\u1ec3 ghim {item.name}", true); // "Click chuột để ghim [name]"
        Debug.Log($"[BuildMode] Item selected & ghost activated: {item.name}");
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

    // ── Placement Controls ──

    private void ShowPlacementControls()
    {
        placementControls?.RemoveFromClassList("hidden");
    }

    private void HidePlacementControls()
    {
        placementControls?.AddToClassList("hidden");
    }

    private void OnConfirmPlacement()
    {
        var ghost = GhostPlacementController.Instance;
        if (ghost == null || !ghost.IsActive) return;

        if (!ghost.IsPlacementValid)
        {
            ShowStatusMessage("V\u1ecb tr\u00ed kh\u00f4ng h\u1ee3p l\u1ec7! H\u00e3y ch\u1ecdn ch\u1ed7 kh\u00e1c.", false);
            return;
        }

        ghost.ConfirmPlacement();
        // Unpin so the next one follows mouse immediately
        ghost.SetPinned(false);
        HidePlacementControls();
    }

    private void OnRotatePlacement()
    {
        var ghost = GhostPlacementController.Instance;
        if (ghost != null && ghost.IsActive)
        {
            ghost.Rotate();
        }
    }

    private void OnCancelPlacement()
    {
        if (GhostPlacementController.Instance != null)
            GhostPlacementController.Instance.Deactivate();

        state = BuildState.Browsing;
        HidePlacementControls();

        // Deselect item
        if (selectedCardElement != null)
        {
            selectedCardElement.RemoveFromClassList("build-item-selected");
            selectedCardElement = null;
        }
        selectedItemIndex = -1;
    }

    // ── Contextual Menu (for placed buildings) ──

    private void HandleContextualClick(Vector2 mousePos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, buildingRayMask))
        {
            GameObject hitObj = hit.collider.gameObject;
            if (hitObj.CompareTag("PlacedBuilding"))
            {
                contextSelectedBuilding = hitObj;
                ShowContextMenu(mousePos);
                return;
            }
        }

        // Clicked empty area — hide context menu
        HideContextMenu();
    }

    private void ShowContextMenu(Vector2 screenPos)
    {
        if (contextMenu == null) return;

        contextMenu.RemoveFromClassList("hidden");

        // Position context menu near the click
        // Convert screen pos to UI Toolkit panel coords
        var panelPos = RuntimePanelUtils.ScreenToPanel(
            buildDocument.rootVisualElement.panel, 
            new Vector2(screenPos.x, Screen.height - screenPos.y)
        );

        contextMenu.style.left = panelPos.x - 60;
        contextMenu.style.top = panelPos.y - 60;
    }

    private void HideContextMenu()
    {
        contextMenu?.AddToClassList("hidden");
        contextSelectedBuilding = null;
    }

    private void OnCtxRotate()
    {
        if (contextSelectedBuilding == null) return;

        contextSelectedBuilding.transform.Rotate(0, 90, 0);
        ShowStatusMessage("\\u0110\\u00e3 xoay!", true);
        HideContextMenu();
    }

    private void OnCtxMove()
    {
        if (contextSelectedBuilding == null) return;
        PickUpBuilding(contextSelectedBuilding);
        HideContextMenu();
    }

    private void OnCtxDelete()
    {
        if (contextSelectedBuilding == null) return;
        DeleteBuildingAt(contextSelectedBuilding);
        HideContextMenu();
    }

    // ── Delete & Move Logic ──

    private void DeleteBuildingAt(GameObject building)
    {
        if (BuildGridManager.Instance != null)
        {
            Vector2Int gridCell = BuildGridManager.Instance.WorldToGrid(building.transform.position);
            float cellSize = BuildGridManager.Instance.CellSize;
            int sizeX = Mathf.Max(1, Mathf.RoundToInt(building.transform.localScale.x / (cellSize * 0.95f)));
            int sizeZ = Mathf.Max(1, Mathf.RoundToInt(building.transform.localScale.z / (cellSize * 0.95f)));
            Vector2Int size = new Vector2Int(sizeX, sizeZ);

            Vector2Int originCell = new Vector2Int(
                gridCell.x - (sizeX / 2),
                gridCell.y - (sizeZ / 2)
            );
            BuildGridManager.Instance.FreeCells(originCell, size);
        }

        Destroy(building);
        ShowStatusMessage("\u0110\u00e3 x\u00f3a!", true);
        Debug.Log($"[BuildMode] Deleted building: {building.name}");
    }

    private void PickUpBuilding(GameObject building)
    {
        Vector3 buildingPos = building.transform.position;
        Vector3 buildingScale = building.transform.localScale;
        string buildingName = building.name;

        float cellSize = BuildGridManager.Instance != null ? BuildGridManager.Instance.CellSize : 1f;
        int sizeX = Mathf.Max(1, Mathf.RoundToInt(buildingScale.x / (cellSize * 0.95f)));
        int sizeZ = Mathf.Max(1, Mathf.RoundToInt(buildingScale.z / (cellSize * 0.95f)));
        Vector2Int size = new Vector2Int(sizeX, sizeZ);

        if (BuildGridManager.Instance != null)
        {
            Vector2Int gridCell = BuildGridManager.Instance.WorldToGrid(buildingPos);
            Vector2Int originCell = new Vector2Int(
                gridCell.x - (sizeX / 2),
                gridCell.y - (sizeZ / 2)
            );
            BuildGridManager.Instance.FreeCells(originCell, size);
        }

        Destroy(building);

        if (GhostPlacementController.Instance != null)
        {
            string itemName = buildingName;
            if (buildingName.StartsWith("Building_"))
            {
                string[] parts = buildingName.Split('_');
                if (parts.Length >= 2) itemName = parts[1];
            }

            GhostPlacementController.Instance.Activate(itemName, size, 0);
            state = BuildState.Placing;
            ShowPlacementControls();
        }

        ShowStatusMessage("\u0110\u00e3 nh\u1ea5c c\u00f4ng tr\u00ecnh \u2014 ch\u1ecdn v\u1ecb tr\u00ed m\u1edbi", true);
        Debug.Log($"[BuildMode] Picked up building: {buildingName}");
    }

    // ── Info Tooltip ──

    private void ShowInfoTooltip(int index)
    {
        if (!categoryData.ContainsKey(activeCategoryIndex)) return;
        var items = categoryData[activeCategoryIndex];
        if (index < 0 || index >= items.Count) return;

        var item = items[index];

        if (infoTooltipName != null) infoTooltipName.text = item.name;
        if (infoTooltipSize != null) infoTooltipSize.text = item.size;
        if (infoTooltipPrice != null) infoTooltipPrice.text = $"{item.price} POS";
        if (infoTooltipDesc != null) infoTooltipDesc.text = item.description;

        infoTooltip?.RemoveFromClassList("hidden");
    }

    private void HideInfoTooltip()
    {
        infoTooltip?.AddToClassList("hidden");
    }

    // ── Balance ──

    private void UpdateBalance()
    {
        if (lblBuildBalance != null)
        {
            lblBuildBalance.text = $"{currentBalance:N0} POS";
        }
    }

    private void OnBuildingPlacedHandler(string itemName, int price)
    {
        if (currentBalance < price)
        {
            ShowStatusMessage($"Kh\u00F4ng \u0111\u1EE7 POS! C\u1EA7n {price} POS.", false);
            return;
        }

        currentBalance -= price;
        UpdateBalance();
        ShowStatusMessage($"\u0110\u1EB7t th\u00E0nh c\u00F4ng! (-{price} POS)", true);

        // Kích hoạt Animation cho Player
        if (PlayerController.Instance != null)
        {
            bool isFarmPlot = !string.IsNullOrEmpty(itemName) && (itemName.ToLower().Contains("ruộng") || itemName.ToLower().Contains("farm"));
            // Tên phải khớp STATE trong Animator (clip gõ búa của anh tên "Hammering2").
            string animName = isFarmPlot ? "Hoeing" : "Hammering2";
            var tool = isFarmPlot ? YWonderLand.Player.ToolType.Hoe : YWonderLand.Player.ToolType.Hammer;
            // duration = 0 -> tự đo trọn độ dài clip (gõ búa phát hết, không bị cắt giữa chừng).
            PlayerController.Instance.PlayActionAnimation(animName, 0f, tool);
        }
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
