using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using YWonderLand.Data;
using YWonderLand.Managers;

public class WorkshopPopupController : MonoBehaviour
{
    public static WorkshopPopupController Instance { get; private set; }

    [SerializeField] private UIDocument uiDocument;
    private VisualElement root;
    
    // UI Elements
    private VisualElement overlay;
    private Button btnClose;
    
    private VisualElement toolListContainer;
    private VisualElement detailPanel;
    private Label lblDetailEmpty;
    private VisualElement detailContent;
    
    // Detail Elements
    private Label lblToolIcon;
    private Label lblToolName;
    private Label lblUpgradeEffect;
    
    private Label lblReqPOS;
    private Label lblReqWood;
    private Label lblReqStone;
    private Label lblReqIron;
    
    private VisualElement rowReqWood;
    private VisualElement rowReqStone;
    private VisualElement rowReqIron;
    
    private Button btnUpgrade;
    
    private ItemDatabase itemDatabase;
    private string selectedToolId = "";
    
    private List<VisualElement> toolItemVisuals = new List<VisualElement>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Don't destroy on load might be needed if it's a global popup, but if it's attached to HUD, it's fine.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private Label lblReqTitle;

    private void Start()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase");
        
        root = uiDocument.rootVisualElement;
        
        overlay = root.Q<VisualElement>("WorkshopOverlay");
        btnClose = root.Q<Button>("BtnCloseWorkshop");
        
        toolListContainer = root.Q<VisualElement>("ToolListContainer");
        detailPanel = root.Q<VisualElement>("WorkshopDetailPanel");
        lblDetailEmpty = root.Q<Label>("WorkshopDetailEmpty");
        detailContent = root.Q<VisualElement>("WorkshopDetailContent");
        
        lblToolIcon = root.Q<Label>("LblToolIcon");
        lblToolName = root.Q<Label>("LblToolName");
        lblUpgradeEffect = root.Q<Label>("LblUpgradeEffect");
        lblReqTitle = root.Q<Label>("LblReqTitle");
        
        lblReqPOS = root.Q<Label>("LblReqPOS");
        lblReqWood = root.Q<Label>("LblReqWood");
        lblReqStone = root.Q<Label>("LblReqStone");
        lblReqIron = root.Q<Label>("LblReqIron");
        
        rowReqWood = root.Q<VisualElement>("RowReqWood");
        rowReqStone = root.Q<VisualElement>("RowReqStone");
        rowReqIron = root.Q<VisualElement>("RowReqIron");
        
        btnUpgrade = root.Q<Button>("BtnUpgrade");
        
        // Set Vietnamese text from C# (Memory #50)
        root.Q<Label>("WorkshopTitle").text = "TI\u1EC6M R\u00C8N";
        root.Q<Label>(className: "workshop-sidebar-section").text = "D\u1EE4NG C\u1EE4";
        lblDetailEmpty.text = "Ch\u1ECDn m\u1ED9t d\u1EE5ng c\u1EE5 \u0111\u1EC3 xem chi ti\u1EBFt...";
        btnClose.text = "\u2715";
        if (lblReqTitle != null) lblReqTitle.text = "Y\u00CAU C\u1EA6U:";
        
        // Set req row icons and names from C#
        SetReqRowContent("RowReqWood", "\ud83e\udeb5", "G\u1ED7");
        SetReqRowContent("RowReqStone", "\ud83e\udea8", "\u0110\u00E1");
        SetReqRowContent("RowReqIron", "\u26D3", "S\u1EAFt");
        // POS row icon
        var posRow = root.Q<VisualElement>("ReqContainer").Children().GetEnumerator();
        if (posRow.MoveNext())
        {
            var firstRow = posRow.Current;
            var labels = firstRow.Query<Label>().ToList();
            if (labels.Count >= 1) labels[0].text = "\ud83e\ude99";
        }
        
        btnClose.clicked += Hide;
        btnUpgrade.clicked += OnUpgradeClicked;
        
        // Click overlay to dismiss (DESIGN.md 6.3)
        overlay.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == overlay) Hide();
        });
        
        Hide();
    }

    private void SetReqRowContent(string rowName, string icon, string name)
    {
        var row = root.Q<VisualElement>(rowName);
        if (row == null) return;
        var labels = row.Query<Label>().ToList();
        if (labels.Count >= 2)
        {
            labels[0].text = icon;
            labels[1].text = name;
        }
    }

    public void Show()
    {
        if (overlay == null) return;
        overlay.style.display = DisplayStyle.Flex;
        
        selectedToolId = "";
        RefreshToolList();
        UpdateDetailPanel();
    }

    public void Hide()
    {
        if (overlay == null) return;
        overlay.style.display = DisplayStyle.None;
    }

    private void RefreshToolList()
    {
        toolListContainer.Clear();
        toolItemVisuals.Clear();
        
        foreach (var toolId in ToolManager.BaseTools)
        {
            ItemDefinition itemDef = itemDatabase?.GetItem(toolId);
            if (itemDef == null) continue;
            
            VisualElement toolVisual = new VisualElement();
            toolVisual.AddToClassList("workshop-tool-item");
            
            // Icon
            Label iconLabel = new Label(itemDef.iconEmoji);
            iconLabel.AddToClassList("workshop-tool-item-icon");
            toolVisual.Add(iconLabel);
            
            // Name with Level
            string displayName = ToolManager.Instance.GetToolDisplayName(toolId, itemDef.itemName);
            // Bỏ " Lv1" của tên gốc nếu có để build lại tên với cấp độ
            if (itemDef.itemName.Contains(" Lv1")) 
            {
                string baseName = itemDef.itemName.Replace(" Lv1", "");
                displayName = ToolManager.Instance.GetToolDisplayName(toolId, baseName);
            }

            Label nameLabel = new Label(displayName);
            nameLabel.AddToClassList("workshop-tool-item-name");
            toolVisual.Add(nameLabel);
            
            // Interaction
            string currentToolId = toolId; // Capture for lambda
            toolVisual.RegisterCallback<ClickEvent>(ev => {
                SelectTool(currentToolId, toolVisual);
            });
            
            toolListContainer.Add(toolVisual);
            toolItemVisuals.Add(toolVisual);
            
            if (string.IsNullOrEmpty(selectedToolId))
            {
                SelectTool(toolId, toolVisual);
            }
        }
    }

    private void SelectTool(string toolId, VisualElement selectedVisual)
    {
        selectedToolId = toolId;
        
        foreach (var v in toolItemVisuals)
        {
            v.RemoveFromClassList("workshop-tool-item--active");
        }
        selectedVisual.AddToClassList("workshop-tool-item--active");
        
        UpdateDetailPanel();
    }

    private void UpdateDetailPanel()
    {
        if (string.IsNullOrEmpty(selectedToolId))
        {
            lblDetailEmpty.style.display = DisplayStyle.Flex;
            detailContent.style.display = DisplayStyle.None;
            return;
        }

        lblDetailEmpty.style.display = DisplayStyle.None;
        detailContent.style.display = DisplayStyle.Flex;

        ItemDefinition itemDef = itemDatabase?.GetItem(selectedToolId);
        int currentLevel = ToolManager.Instance.GetToolLevel(selectedToolId);
        
        string baseName = itemDef != null ? itemDef.itemName.Replace(" Lv1", "") : "D\u1EE5ng c\u1EE5";
        
        lblToolIcon.text = itemDef != null ? itemDef.iconEmoji : "\ud83d\udd27";
        
        if (currentLevel >= ToolManager.MAX_TOOL_LEVEL)
        {
            lblToolName.text = $"{baseName} Lv{currentLevel}";
            lblUpgradeEffect.text = "\u0110\u00E3 \u0111\u1EA1t c\u1EA5p \u0111\u1ED9 t\u1ED1i \u0111a.";
            btnUpgrade.SetEnabled(false);
            btnUpgrade.AddToClassList("workshop-btn-action--disabled");
            btnUpgrade.text = "\u0110\u00C3 T\u1ED0I \u0110A";
            
            // Hide reqs
            root.Q<VisualElement>("ReqContainer").style.display = DisplayStyle.None;
            if (lblReqTitle != null) lblReqTitle.style.display = DisplayStyle.None;
            return;
        }
        else
        {
            root.Q<VisualElement>("ReqContainer").style.display = DisplayStyle.Flex;
            if (lblReqTitle != null) lblReqTitle.style.display = DisplayStyle.Flex;
            btnUpgrade.text = "N\u00C2NG C\u1EA4P";
        }

        int nextLevel = currentLevel + 1;
        lblToolName.text = $"{baseName} Lv{currentLevel} \u2192 Lv{nextLevel}";
        
        // Effect text with real newline
        lblUpgradeEffect.text = $"S\u1EA3n l\u01B0\u1EE3ng: +{currentLevel} \u2192 +{nextLevel}\nT\u1ED1c \u0111\u1ED9: +{currentLevel * 10}% \u2192 +{nextLevel * 10}%";

        var req = ToolManager.Instance.GetUpgradeRequirement(selectedToolId, nextLevel);
        
        int woodOwned = InventoryManager.Instance.GetItemQuantity("wood_01");
        int stoneOwned = InventoryManager.Instance.GetItemQuantity("stone_01");
        int ironOwned = InventoryManager.Instance.GetItemQuantity("iron_01");
        long posOwned = EconomyManager.Instance.GetPOS();

        bool canUpgrade = true;

        // POS
        UpdateReqLabel(lblReqPOS, posOwned, req.posCost);
        if (posOwned < req.posCost) canUpgrade = false;

        // Wood
        if (req.woodCost > 0) {
            rowReqWood.style.display = DisplayStyle.Flex;
            UpdateReqLabel(lblReqWood, woodOwned, req.woodCost);
            if (woodOwned < req.woodCost) canUpgrade = false;
        } else {
            rowReqWood.style.display = DisplayStyle.None;
        }

        // Stone
        if (req.stoneCost > 0) {
            rowReqStone.style.display = DisplayStyle.Flex;
            UpdateReqLabel(lblReqStone, stoneOwned, req.stoneCost);
            if (stoneOwned < req.stoneCost) canUpgrade = false;
        } else {
            rowReqStone.style.display = DisplayStyle.None;
        }

        // Iron
        if (req.ironCost > 0) {
            rowReqIron.style.display = DisplayStyle.Flex;
            UpdateReqLabel(lblReqIron, ironOwned, req.ironCost);
            if (ironOwned < req.ironCost) canUpgrade = false;
        } else {
            rowReqIron.style.display = DisplayStyle.None;
        }

        // Button State
        if (canUpgrade)
        {
            btnUpgrade.SetEnabled(true);
            btnUpgrade.RemoveFromClassList("workshop-btn-action--disabled");
        }
        else
        {
            btnUpgrade.SetEnabled(false);
            btnUpgrade.AddToClassList("workshop-btn-action--disabled");
        }
    }

    private void UpdateReqLabel(Label lbl, long owned, long required)
    {
        lbl.text = $"{owned} / {required}";
        if (owned >= required)
        {
            lbl.RemoveFromClassList("workshop-req-value--not-enough");
            lbl.AddToClassList("workshop-req-value--enough");
        }
        else
        {
            lbl.RemoveFromClassList("workshop-req-value--enough");
            lbl.AddToClassList("workshop-req-value--not-enough");
        }
    }

    private void OnUpgradeClicked()
    {
        if (string.IsNullOrEmpty(selectedToolId)) return;
        
        bool success = ToolManager.Instance.UpgradeTool(selectedToolId);
        if (success)
        {
            // Cập nhật lại UI sau khi nâng cấp
            RefreshToolList(); // To update names in sidebar
            UpdateDetailPanel(); // To show next level
            
            // Optionally: Play sound or VFX
            Debug.Log("[Workshop] Upgrade success UI updated.");
        }
    }
}
