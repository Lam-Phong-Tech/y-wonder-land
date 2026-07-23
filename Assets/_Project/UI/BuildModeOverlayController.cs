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

    [Header("Character-Facing Build Flow")]
    [SerializeField] private bool useTopDownBuildCamera = false;
    [SerializeField] private bool hideGameHudWhileOpen = false;
    [SerializeField] private bool useFrontCellGhostPlacement = true;

    // State
    private enum BuildState { Hidden, Browsing, Placing }
    private BuildState state = BuildState.Hidden;

    // UI Elements — Header
    private VisualElement buildRoot;
    private VisualElement buildBalancePill;
    private Label lblBuildBalance;
    private Label lblBuildWood;
    private Label lblBuildStone;
    private Label lblBuildStatus;
    private Button btnExitBuild;

    // UI Elements — Item Bar
    private ScrollView itemScrollView;

    // UI Elements — Placement Controls (confirm/rotate/cancel)
    private VisualElement placementControls;
    private Button btnConfirmPlace;
    private Button btnCancelPlace;

    // UI Elements — Contextual Menu (for placed buildings)
    private VisualElement contextMenu;
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

    // Context-selected building
    private GameObject contextSelectedBuilding;
    private bool lastPointerDownWasTouch;

    // ── Dữ liệu menu Build (chi phí lấy từ SerializeField bên dưới, không phải số giả) ──

    private struct BuildItemData
    {
        public string iconClass;
        public string name;
        public string size;
        public string materialId;   // "" = miễn phí
        public int materialAmount;  // số lượng vật liệu cần
        public string description;

        public BuildItemData(string iconClass, string name, string size, string materialId, int materialAmount, string description)
        {
            this.iconClass = iconClass;
            this.name = name;
            this.size = size;
            this.materialId = materialId;
            this.materialAmount = materialAmount;
            this.description = description;
        }

        // Chữ chi phí hiển thị trên menu Build (thay tiền bằng vật liệu).
        public string CostText()
        {
            if (materialAmount <= 0 || string.IsNullOrEmpty(materialId)) return "Miễn phí";
            string mat = materialId == "wood_01" ? "Gỗ" : materialId == "stone_01" ? "Đá" : materialId == "iron_01" ? "Sắt" : materialId;
            return $"{materialAmount} {mat}";
        }
    }

    [Header("Chi ph\u00ed x\u00e2y (t\u00f9y ch\u1ec9nh trong Inspector)")]
    [Tooltip("S\u1ed1 G\u1ed6 t\u1ed1n khi x\u00e2y 1 \u00f4 r\u00e0o (Chu\u1ed3ng).")]
    [SerializeField] private int penWoodCost = 4;
    [Tooltip("S\u1ed1 \u0110\u00c1 t\u1ed1n khi l\u00e1t 1 \u00f4 \u0110\u01b0\u1eddng \u0111\u00e1.")]
    [SerializeField] private int pathStoneCost = 4;

    // Menu Build r\u00fat g\u1ecdn \u2014 3 m\u1ee5c: Ru\u1ed9ng (free) / \u0110\u01b0\u1eddng \u0111\u00e1 / Chu\u1ed3ng. D\u1ef1ng RUNTIME \u0111\u1ec3 l\u1ea5y chi ph\u00ed t\u1eeb SerializeField.
    private Dictionary<int, List<BuildItemData>> categoryData;

    private void BuildCategoryData()
    {
        categoryData = new Dictionary<int, List<BuildItemData>>()
        {
            { 0, new List<BuildItemData>
                {
                    new BuildItemData("build-icon-farm-plot", "Ru\u1ed9ng", "1x1", "", 0, "\u00d4 \u0111\u1ea5t canh t\u00e1c \u2014 \u0111\u1eb7t t\u1eebng \u00f4 \u0111\u1ec3 tr\u1ed3ng c\u00e2y. (Mi\u1ec5n ph\u00ed)"),
                    new BuildItemData("build-icon-stone-path", "\u0110\u01b0\u1eddng \u0111\u00e1", "1x1", "stone_01", pathStoneCost, $"M\u1eb7t \u0111\u01b0\u1eddng \u0111\u00e1 l\u00e1t l\u1ed1i \u0111i. ({pathStoneCost} \u0110\u00e1)"),
                    new BuildItemData("build-icon-pen", "Chu\u1ed3ng", "1x1", "wood_01", penWoodCost, $"H\u00e0ng r\u00e0o qu\u00e2y chu\u1ed3ng \u2014 gh\u00e9p nhi\u1ec1u \u00f4 th\u00e0nh chu\u1ed3ng to. ({penWoodCost} G\u1ed7/\u00f4 r\u00e0o)"),
                }
            }
        };
    }

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

        BuildCategoryData(); // dựng danh mục build từ chi phí trong Inspector
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

        // Escape = Exit build mode or cancel placement
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
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
                if (useFrontCellGhostPlacement)
                    return;

                if (ghost.IsPinned)
                {
                    UpdatePlacementControlsPosition(ghost.GhostPosition);
                }

                if (TryGetPointerDownPosition(out Vector2 pointerPos) && !IsPointerOverUI(pointerPos))
                {
                    ghost.RefreshPlacementAtScreenPosition(pointerPos, lastPointerDownWasTouch);

                    if (!ghost.IsPinned)
                    {
                        if (!ghost.IsPlacementValid || ghost.IsScreenPositionBlockedForPlacement(pointerPos, lastPointerDownWasTouch))
                        {
                            ShowStatusMessage("V\u1ecb tr\u00ed kh\u00f4ng h\u1ee3p l\u1ec7 ho\u1eb7c qu\u00e1 s\u00e1t r\u00eca m\u00e0n h\u00ecnh.", false);
                            return;
                        }

                        ghost.SetPinned(true);
                        ShowPlacementControls();
                        ShowStatusMessage("\u0110\u00e3 ghim v\u1ecb tr\u00ed. B\u1ea5m OK \u0111\u1ec3 x\u00e2y.", true);
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
            if (TryGetPointerDownPosition(out Vector2 pointerPos) && !IsPointerOverUI(pointerPos))
            {
                HandleContextualClick(pointerPos);
            }
        }
    }

    private bool IsPointerOverUI()
    {
        if (!TryGetCurrentPointerPosition(out Vector2 pointerPos)) return false;
        return IsPointerOverUI(pointerPos);
    }

    private bool IsPointerOverUI(Vector2 screenPos)
    {
        if (buildDocument == null || buildDocument.rootVisualElement == null || buildDocument.rootVisualElement.panel == null) return false;
        Vector2 invertedY = new Vector2(screenPos.x, Screen.height - screenPos.y);
        var picked = buildDocument.rootVisualElement.panel.Pick(RuntimePanelUtils.ScreenToPanel(buildDocument.rootVisualElement.panel, invertedY));
        return picked != null && picked != buildRoot;
    }

    private bool TryGetPointerDownPosition(out Vector2 screenPos)
    {
        var touch = Touchscreen.current;
        if (touch != null)
        {
            var primary = touch.primaryTouch;
            if (primary.press.wasPressedThisFrame)
            {
                screenPos = primary.position.ReadValue();
                lastPointerDownWasTouch = true;
                return true;
            }

            for (int i = 0; i < touch.touches.Count; i++)
            {
                var finger = touch.touches[i];
                if (finger.press.wasPressedThisFrame)
                {
                    screenPos = finger.position.ReadValue();
                    lastPointerDownWasTouch = true;
                    return true;
                }
            }
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPos = mouse.position.ReadValue();
            lastPointerDownWasTouch = false;
            return true;
        }

        screenPos = default;
        lastPointerDownWasTouch = false;
        return false;
    }

    private bool TryGetCurrentPointerPosition(out Vector2 screenPos)
    {
        var touch = Touchscreen.current;
        if (touch != null)
        {
            var primary = touch.primaryTouch;
            if (primary.press.isPressed)
            {
                screenPos = primary.position.ReadValue();
                return true;
            }
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            screenPos = mouse.position.ReadValue();
            return true;
        }

        screenPos = default;
        return false;
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
        buildBalancePill = root.Q<VisualElement>(className: "build-balance-pill");
        lblBuildBalance = root.Q<Label>("lblBuildBalance");
        lblBuildStatus = root.Q<Label>("lblBuildStatus");
        btnExitBuild = root.Q<Button>("BtnExitBuild");
        itemScrollView = root.Q<ScrollView>("ItemScrollView");

        // Placement controls
        placementControls = root.Q<VisualElement>("PlacementControls");
        btnConfirmPlace = root.Q<Button>("BtnConfirmPlace");
        btnCancelPlace = root.Q<Button>("BtnCancelPlace");

        // Contextual menu
        contextMenu = root.Q<VisualElement>("ContextMenu");
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

        ConfigureBuildBalancePill();
    }

    private void ConfigureBuildBalancePill()
    {
        if (buildBalancePill == null) return;

        buildBalancePill.Clear();
        lblBuildWood = AddBuildBalanceEntry("wood_01", "Gỗ");
        lblBuildStone = AddBuildBalanceEntry("stone_01", "Đá");
    }

    private Label AddBuildBalanceEntry(string materialId, string displayName)
    {
        var entry = new VisualElement();
        entry.AddToClassList("build-balance-material");

        entry.Add(CreateMaterialIcon(materialId, "build-balance-material-icon"));

        var label = new Label($"0 {displayName}");
        label.AddToClassList("build-balance-material-text");
        entry.Add(label);

        buildBalancePill.Add(entry);
        return label;
    }

    private VisualElement CreateMaterialIcon(string materialId, string baseClass)
    {
        var icon = new VisualElement();
        icon.AddToClassList(baseClass);

        if (materialId == "wood_01")
            icon.AddToClassList("build-material-icon-wood");
        else if (materialId == "stone_01")
            icon.AddToClassList("build-material-icon-stone");
        else
            icon.AddToClassList("build-material-icon-generic");

        return icon;
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
        btnCancelPlace?.RegisterCallback<ClickEvent>(evt => OnCancelPlacement());

        // Contextual menu
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

        // Optional legacy top-down camera. The mobile flow now builds from the character-facing view.
        if (useTopDownBuildCamera && BuildCameraController.Instance != null)
            BuildCameraController.Instance.Activate();

        // Lưới hiển thị: ĐÃ TẮT theo yêu cầu khách (giữ logic snap ô, chỉ bỏ phần vẽ lưới).
        var gridRenderer = FindFirstObjectByType<BuildGridRenderer>();
        if (gridRenderer != null)
            gridRenderer.Hide();

        // Subscribe to building placed event for balance deduction
        GhostPlacementController.OnBuildingPlaced -= OnBuildingPlacedHandler;
        GhostPlacementController.OnBuildingPlaced += OnBuildingPlacedHandler;

        // Optional legacy behavior. Keep the HUD visible so the player can move and aim at the front cell.
        if (hideGameHudWhileOpen)
            SetGameHUDVisible(false);

        // Compact build popup sits near the right HUD buttons, so chat no longer needs to move.

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

        // Deactivate ghost and optional legacy camera
        if (GhostPlacementController.Instance != null)
            GhostPlacementController.Instance.Deactivate();
        if (useTopDownBuildCamera && BuildCameraController.Instance != null)
            BuildCameraController.Instance.Deactivate();

        // Stop grid following + hide
        if (BuildGridManager.Instance != null)
            BuildGridManager.Instance.StopFollowing();
        var gridRenderer = FindFirstObjectByType<BuildGridRenderer>();
        if (gridRenderer != null)
            gridRenderer.Hide();

        // Restore GameHUD only if this controller hid it on open.
        if (hideGameHudWhileOpen)
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

            var icon = new VisualElement();
            icon.AddToClassList("build-item-icon");
            if (!string.IsNullOrEmpty(item.iconClass))
                icon.AddToClassList(item.iconClass);
            card.Add(icon);

            var nameLabel = new Label(item.name);
            nameLabel.AddToClassList("build-item-name");
            card.Add(nameLabel);

            var priceRow = new VisualElement();
            priceRow.AddToClassList("build-item-price-row");
            if (!string.IsNullOrEmpty(item.materialId) && item.materialAmount > 0)
                priceRow.Add(CreateMaterialIcon(item.materialId, "build-item-price-icon"));

            var priceLabel = new Label(item.CostText());
            priceLabel.AddToClassList("build-item-price");
            priceRow.Add(priceLabel);
            card.Add(priceRow);

            var actionsRow = new VisualElement();
            actionsRow.AddToClassList("build-card-actions");

            var confirmButton = new Button { text = "\u2713" };
            confirmButton.AddToClassList("build-card-action-btn");
            confirmButton.AddToClassList("build-card-confirm");
            confirmButton.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                OnConfirmPlacement();
            });
            actionsRow.Add(confirmButton);

            var cancelButton = new Button { text = "X" };
            cancelButton.AddToClassList("build-card-action-btn");
            cancelButton.AddToClassList("build-card-cancel");
            cancelButton.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                OnCancelPlacement();
            });
            actionsRow.Add(cancelButton);
            card.Add(actionsRow);

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

        var ghost = GhostPlacementController.Instance;
        if (ghost == null)
        {
            ShowStatusMessage("Build ghost ch\u01b0a s\u1eb5n s\u00e0ng.", false);
            ClearSelectedItem();
            return;
        }

        Vector2Int size = ParseSize(item.size);
        ghost.Activate(item.name, size, item.materialId, item.materialAmount);

        state = BuildState.Placing;
        HideContextMenu();
        HideInfoTooltip();

        if (useFrontCellGhostPlacement)
        {
            if (!TryPinGhostToFrontCell())
            {
                ghost.Deactivate();
                state = BuildState.Browsing;
                HidePlacementControls();
                ClearSelectedItem();
                ShowStatusMessage("Kh\u00f4ng c\u00f3 \u00f4 \u0111\u1ea5t h\u1ee3p l\u1ec7 tr\u01b0\u1edbc m\u1eb7t nh\u00e2n v\u1eadt.", false);
                return;
            }

            HidePlacementControls();
            ShowStatusMessage(
                ghost.IsPlacementValid
                    ? $"B\u1ea5m d\u1ea5u t\u00edch tr\u00ean th\u1ebb \u0111\u1ec3 x\u00e2y {item.name}."
                    : "\u00d4 tr\u01b0\u1edbc m\u1eb7t \u0111ang b\u1ecb chi\u1ebfm, kh\u00f4ng th\u1ec3 x\u00e2y.",
                ghost.IsPlacementValid);
        }
        else
        {
            HidePlacementControls(); // Wait for user to pin before showing
            ShowStatusMessage($"Ch\u1ea1m \u0111\u1ec3 ghim {item.name}", true);
        }

        Debug.Log($"[BuildMode] Item selected & ghost activated: {item.name}");
    }

    private bool TryPinGhostToFrontCell()
    {
        var ghost = GhostPlacementController.Instance;
        var selector = YWonderLand.Environment.FrontBuildCellSelector.Instance;
        var cell = selector != null ? selector.CurrentCell : null;
        return ghost != null && cell != null && ghost.SnapToCell(cell, true);
    }

    private void ClearSelectedItem()
    {
        if (selectedCardElement != null)
        {
            selectedCardElement.RemoveFromClassList("build-item-selected");
            selectedCardElement = null;
        }
        selectedItemIndex = -1;
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
        if (placementControls == null) return;
        placementControls.RemoveFromClassList("hidden");
        // Set thẳng inline để cùng "ngôn ngữ" với UpdatePlacementControlsPosition (nó cũng set inline).
        placementControls.style.display = DisplayStyle.Flex;
    }

    private void HidePlacementControls()
    {
        if (placementControls == null) return;
        placementControls.AddToClassList("hidden");
        // BẮT BUỘC set inline None: UpdatePlacementControlsPosition đã set inline Flex (đè class .hidden)
        // -> chỉ add class sẽ KHÔNG ẩn được, khiến nút Tích/X "đứng lì" sau khi xây/hủy.
        placementControls.style.display = DisplayStyle.None;
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

        if (!ghost.ConfirmPlacement())
            return;

        if (useFrontCellGhostPlacement)
        {
            ghost.Deactivate();
            state = BuildState.Browsing;
            ClearSelectedItem();
        }
        else
        {
            // Unpin so the next one follows pointer immediately.
            ghost.SetPinned(false);
        }

        HidePlacementControls();
    }

    private void OnCancelPlacement()
    {
        if (GhostPlacementController.Instance != null)
            GhostPlacementController.Instance.Deactivate();

        state = BuildState.Browsing;
        HidePlacementControls();

        ClearSelectedItem();
    }

    // ── Contextual Menu (for placed buildings) ──

    private void HandleContextualClick(Vector2 mousePos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));

        GameObject building = FindContextBuilding(ray);
        if (building != null)
        {
            contextSelectedBuilding = building;
            ShowContextMenu(mousePos);
            return;
        }

        // Clicked empty area — hide context menu
        HideContextMenu();
    }

    private GameObject FindContextBuilding(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, 200f, buildingRayMask, QueryTriggerInteraction.Collide);
        GameObject best = null;
        float bestDistance = float.PositiveInfinity;

        foreach (var hit in hits)
        {
            GameObject building = ResolveContextBuilding(hit);
            if (building == null || hit.distance >= bestDistance) continue;

            best = building;
            bestDistance = hit.distance;
        }

        return best;
    }

    private GameObject ResolveContextBuilding(RaycastHit hit)
    {
        var placed = hit.collider.GetComponentInParent<YWonderLand.Environment.PlacedBuilding>();
        if (placed != null) return placed.gameObject;

        Transform current = hit.collider.transform;
        while (current != null)
        {
            if (current.CompareTag("PlacedBuilding"))
                return current.gameObject;
            current = current.parent;
        }

        return null;
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
        bool wasFarmTile = building != null && building.GetComponentInChildren<FarmTile>(true) != null;
        string buildingName = building != null ? building.name : "(null)";

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

        // Ho\u00e0n v\u1eadt li\u1ec7u \u0111\u00e3 t\u1ed1n khi ph\u00e1 (\u0111\u1ecdc t\u1eeb \u00f4 TR\u01af\u1edaC khi ClearOccupant x\u00f3a d\u1eef li\u1ec7u v\u1eadt li\u1ec7u).
        // Ru\u1ed9ng mi\u1ec5n ph\u00ed c\u00f3 BuildMaterialId r\u1ed7ng n\u00ean ho\u00e0n 0. \u0110\u1ed3ng nh\u1ea5t v\u1edbi ph\u00e1 chu\u1ed3ng.
        YWonderLand.Environment.BuildSurfaceCell.SumRefund(building, out int refundWood, out int refundStone);
        YWonderLand.Environment.BuildSurfaceCell.ClearOccupant(building);
        var inv = YWonderLand.Managers.InventoryManager.Instance;
        if (inv != null)
        {
            if (refundWood > 0) inv.AddItem("wood_01", refundWood, "build_refund");
            if (refundStone > 0) inv.AddItem("stone_01", refundStone, "build_refund");
        }
        Destroy(building);
        SaveBuildState();
        ShowStatusMessage(wasFarmTile ? "\u0110\u00e3 h\u1ee7y \u00f4 tr\u1ed3ng!" : "\u0110\u00e3 x\u00f3a!", true);
        Debug.Log($"[BuildMode] Deleted building: {buildingName}, refund wood={refundWood}, stone={refundStone}");
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

        YWonderLand.Environment.BuildSurfaceCell.ClearOccupant(building);
        Destroy(building);

        if (GhostPlacementController.Instance != null)
        {
            string itemName = buildingName;
            if (buildingName.StartsWith("Building_"))
            {
                string[] parts = buildingName.Split('_');
                if (parts.Length >= 2) itemName = parts[1];
            }

            GhostPlacementController.Instance.Activate(itemName, size, "", 0); // đặt lại công trình đã có → miễn phí (không trừ lại vật liệu)
            state = BuildState.Placing;
            ShowPlacementControls();
        }

        ShowStatusMessage("\u0110\u00e3 nh\u1ea5c c\u00f4ng tr\u00ecnh \u2014 ch\u1ecdn v\u1ecb tr\u00ed m\u1edbi", true);
        Debug.Log($"[BuildMode] Picked up building: {buildingName}");
    }

    private void SaveBuildState()
    {
        var persistence = Object.FindFirstObjectByType<YWonderLand.Environment.BuildPersistence>(FindObjectsInactive.Include);
        persistence?.SaveBuildings();
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
        if (infoTooltipPrice != null) infoTooltipPrice.text = item.CostText();
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
        // Build mode dùng VẬT LIỆU → hiện số gỗ/đá người chơi đang có.
        var inv = YWonderLand.Managers.InventoryManager.Instance;
        if (inv != null)
        {
            int wood = inv.GetItemQuantity("wood_01");
            int stone = inv.GetItemQuantity("stone_01");
            if (lblBuildWood != null) lblBuildWood.text = $"{wood} Gỗ";
            if (lblBuildStone != null) lblBuildStone.text = $"{stone} Đá";
            if (lblBuildBalance != null) lblBuildBalance.text = $"{wood} Gỗ   {stone} Đá";
        }
        else
        {
            if (lblBuildWood != null) lblBuildWood.text = "0 Gỗ";
            if (lblBuildStone != null) lblBuildStone.text = "0 Đá";
            if (lblBuildBalance != null) lblBuildBalance.text = "0 Gỗ   0 Đá";
        }
    }

    private void OnBuildingPlacedHandler(string itemName, int price)
    {
        // V\u1EADt li\u1EC7u \u0111\u00E3 \u0111\u01B0\u1EE3c KI\u1EC2M + TR\u1EEA \u1EDF GhostPlacementController.ConfirmPlacement (n\u1EBFu thi\u1EBFu th\u00EC kh\u00F4ng \u0111\u1EB7t \u0111\u01B0\u1EE3c).
        // \u0110\u00E2y ch\u1EC9 b\u00E1o th\u00E0nh c\u00F4ng + m\u00FAa ho\u1EA1t \u1EA3nh.
        ShowStatusMessage("\u0110\u1EB7t th\u00E0nh c\u00F4ng!", true);
        UpdateBalance(); // c\u1EADp nh\u1EADt l\u1EA1i s\u1ED1 g\u1ED7/\u0111\u00E1 c\u00F2n l\u1EA1i

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
