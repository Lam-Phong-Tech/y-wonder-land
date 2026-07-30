using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using YWonderLand.Data;
using YWonderLand.Environment;
using YWonderLand.Managers;

public class AnimalInteractionPopupController : MonoBehaviour
{
    public static AnimalInteractionPopupController Instance { get; private set; }

    [SerializeField] private UIDocument document;

    private VisualElement container;
    private Label lblAnimalName;
    private Label lblStatus;
    private Label lblPrice;
    private Label lblSlots;
    private Label lblFoodMain;
    private Label lblFoodAlt;
    private Label lblProducts;
    private Label lblHunger;
    private Label lblHarvest;
    private VisualElement feedLogPanel;   // khối nhật ký — các hàng dựng động, xem ShowLogPanel
    private string logPanelSignature;     // chỉ dựng lại hàng khi đổi đối tượng/đổi bộ hàng
    private readonly List<Label> logValueLabels = new List<Label>();
    private VisualElement enclosurePanel;
    private VisualElement infoPanel;
    private VisualElement actionsPanel;
    private Label lblEnclosureSummary;
    private Button btnAddAnimal;
    private VisualElement animalCardList;
    private ScrollView animalCardScroll;   // nới cao hơn ở chế độ Xem ruộng (ruộng nhiều cây hơn chuồng)
    private Button btnClose;
    private Button btnFeed;
    private Button btnHarvest;
    private Button btnHeal;
    private Button btnVaccine;
    // Việc của CẢ CỤM (dời chuồng / dời ruộng) — dùng chung một nút, đổi chữ theo chế độ.
    private Button btnMoveGroup;
    // Việc của CÂY đang chọn ở chế độ Xem ruộng.
    private VisualElement plotActionsPanel;
    private Button btnWaterCrop;
    private Button btnHarvestCrop;
    private Button btnFertilizeCrop;
    private ItemDatabase itemDatabase;

    private FarmAnimal currentAnimal;
    private List<BuildSurfaceCell> currentEnclosure;
    // Chế độ XEM RUỘNG (khách chốt 30/07): dùng CHUNG khung popup này với chuồng thú —
    // cùng là "một cụm ô liền nhau, liệt kê từng thứ bên trong". Khỏi dựng popup mới, khỏi sửa scene.
    private List<FarmTile> currentPlot;
    private FarmTile selectedPlotTile;   // cây đang chọn trong popup ruộng — để hiện nhật ký của nó
    private readonly List<FarmAnimal> enclosureAnimals = new List<FarmAnimal>();
    private readonly Dictionary<FarmAnimal, VisualElement> animalCards = new Dictionary<FarmAnimal, VisualElement>();

    private float refreshTimer;

    // Popup tự vẽ lại 4 lần/giây; quét scene tìm controller mỗi lần là đúng cái bẫy giật đã gặp ở
    // nhãn nổi. Giữ lại một tham chiếu, chỉ quét khi chưa có (hoặc scene cũ đã hủy).
    private FarmInteractionController cachedFarm;
    private FarmInteractionController Farm
    {
        get
        {
            if (cachedFarm == null) cachedFarm = Object.FindFirstObjectByType<FarmInteractionController>();
            return cachedFarm;
        }
    }

    private bool IsEnclosureMode => currentEnclosure != null;
    private bool IsPlotMode => currentPlot != null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        if (document == null) document = GetComponent<UIDocument>();
        if (document == null || document.rootVisualElement == null) return;

        if (itemDatabase == null)
            itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase");

        var root = document.rootVisualElement;

        container = root.Q<VisualElement>("AnimalPopupContainer");
        lblAnimalName = root.Q<Label>("LblAnimalName");
        lblStatus = root.Q<Label>("LblStatus");
        lblPrice = root.Q<Label>("LblPrice");
        lblSlots = root.Q<Label>("LblSlots");
        lblFoodMain = root.Q<Label>("LblFoodMain");
        lblFoodAlt = root.Q<Label>("LblFoodAlt");
        lblProducts = root.Q<Label>("LblProducts");
        lblHunger = root.Q<Label>("LblHunger");
        lblHarvest = root.Q<Label>("LblHarvest");
        enclosurePanel = root.Q<VisualElement>("EnclosurePanel");
        infoPanel = root.Q<VisualElement>(className: "ap-info");
        actionsPanel = root.Q<VisualElement>(className: "ap-actions");
        lblEnclosureSummary = root.Q<Label>("LblEnclosureSummary");
        btnAddAnimal = root.Q<Button>("BtnAddAnimal");
        animalCardList = root.Q<VisualElement>("AnimalCardList");
        animalCardScroll = root.Q<ScrollView>("AnimalCardScroll");

        feedLogPanel = root.Q<VisualElement>("FeedLogPanel");

        // Cho phép xuống dòng nếu chữ dài, tránh tràn ra ngoài panel.
        if (lblHarvest != null) lblHarvest.style.whiteSpace = WhiteSpace.Normal;
        if (lblStatus != null) lblStatus.style.whiteSpace = WhiteSpace.Normal;

        btnClose = root.Q<Button>("BtnClose");
        btnFeed = root.Q<Button>("BtnFeed");
        btnHarvest = root.Q<Button>("BtnHarvest");
        btnHeal = root.Q<Button>("BtnHeal");
        btnVaccine = root.Q<Button>("BtnVaccine");
        btnMoveGroup = root.Q<Button>("BtnMoveGroup");
        plotActionsPanel = root.Q<VisualElement>("PlotActionsPanel");
        btnWaterCrop = root.Q<Button>("BtnWaterCrop");
        btnHarvestCrop = root.Q<Button>("BtnHarvestCrop");
        btnFertilizeCrop = root.Q<Button>("BtnFertilizeCrop");

        if (btnClose != null) btnClose.clicked += Hide;
        if (btnFeed != null) btnFeed.clicked += OnFeedClicked;
        if (btnHarvest != null) btnHarvest.clicked += OnHarvestClicked;
        if (btnHeal != null) btnHeal.clicked += OnHealClicked;
        if (btnVaccine != null) btnVaccine.clicked += OnVaccineClicked;
        if (btnAddAnimal != null) btnAddAnimal.clicked += OnAddAnimalClicked;
        if (btnMoveGroup != null) btnMoveGroup.clicked += OnMoveGroupClicked;
        if (btnWaterCrop != null) btnWaterCrop.clicked += OnWaterCropClicked;
        if (btnHarvestCrop != null) btnHarvestCrop.clicked += OnHarvestCropClicked;
        if (btnFertilizeCrop != null) btnFertilizeCrop.clicked += OnFertilizeCropClicked;

        Hide();
    }

    // An toàn: nếu popup bị tắt/destroy khi đang mở (vd đổi đảo) mà chưa kịp gọi Hide(),
    // vẫn gỡ khỏi UIPopupTracker để chuột không bị kẹt và tương tác thế giới không chết.
    private void OnDisable()
    {
        UIPopupTracker.SetOpen(this, false);
        UnsubscribeCurrentAnimal();
        ClearAnimalCards();
    }

    public void Show(FarmAnimal animal)
    {
        if (animal == null || container == null) return;

        currentEnclosure = null;
        currentPlot = null;
        if (enclosurePanel != null) enclosurePanel.style.display = DisplayStyle.None;
        if (plotActionsPanel != null) plotActionsPanel.style.display = DisplayStyle.None;
        ClearAnimalCards();

        container.style.display = DisplayStyle.Flex;
        UIPopupTracker.SetOpen(this, true);
        SelectAnimal(animal);
    }

    public void ShowEnclosure(List<BuildSurfaceCell> enclosure)
    {
        if (enclosure == null || container == null) return;

        currentPlot = null;
        currentEnclosure = new List<BuildSurfaceCell>(enclosure);
        container.style.display = DisplayStyle.Flex;
        UIPopupTracker.SetOpen(this, true);
        if (enclosurePanel != null) enclosurePanel.style.display = DisplayStyle.Flex;
        if (plotActionsPanel != null) plotActionsPanel.style.display = DisplayStyle.None;
        if (btnMoveGroup != null)
        {
            btnMoveGroup.style.display = DisplayStyle.Flex;
            btnMoveGroup.text = "Dời chuồng";
        }

        RefreshEnclosureUI();
    }

    /// <summary>
    /// XEM RUỘNG (khách chốt 30/07): liệt kê mọi cây trong cụm ô đất liền nhau, thay cho việc
    /// bắt người chơi đọc chữ nổi lởm chởm trên đầu từng cây ("nhìn như đám rừng").
    ///
    /// Gom luôn việc vào đây giống popup chuồng: chọn cây rồi Tưới / Thu hoạch / Bón phân ngay,
    /// và "Dời ruộng" cho cả mảnh. Cuốc đất và gieo hạt vẫn bấm ngoài ruộng — chúng cần ngắm
    /// đúng ô trống, đưa vào danh sách "các cây đang có" thì không có gì để chọn.
    /// </summary>
    public void ShowPlot(List<FarmTile> tiles)
    {
        if (tiles == null || container == null) return;

        UnsubscribeCurrentAnimal();
        currentAnimal = null;
        currentEnclosure = null;
        currentPlot = new List<FarmTile>(tiles);
        selectedPlotTile = null;
        logPanelSignature = null; // ép dựng lại hàng nhật ký cho đối tượng mới

        container.style.display = DisplayStyle.Flex;
        UIPopupTracker.SetOpen(this, true);
        if (enclosurePanel != null) enclosurePanel.style.display = DisplayStyle.Flex;

        // Ruộng không có mấy thứ của thú: bảng thông tin, nhật ký cho ăn, nút cho ăn/chữa bệnh, nút thả thú.
        if (infoPanel != null) infoPanel.style.display = DisplayStyle.None;
        if (actionsPanel != null) actionsPanel.style.display = DisplayStyle.None;
        if (feedLogPanel != null) feedLogPanel.style.display = DisplayStyle.None;
        if (btnAddAnimal != null) btnAddAnimal.style.display = DisplayStyle.None;

        // ...bù lại bằng hàng nút của cây + nút dời cả mảnh ruộng.
        if (plotActionsPanel != null) plotActionsPanel.style.display = DisplayStyle.Flex;
        if (btnMoveGroup != null)
        {
            btnMoveGroup.style.display = DisplayStyle.Flex;
            btnMoveGroup.text = "Dời ruộng";
        }

        // Khung 160px của chuồng vừa cho vài con; ruộng có thể mấy chục cây nên nới ra.
        // Chỗ vừa trống do ẩn bảng thông tin + hàng nút bên dưới.
        if (animalCardScroll != null) animalCardScroll.style.height = PlotScrollHeight;

        RefreshPlotUI();
    }

    private void RefreshPlotUI()
    {
        if (!IsPlotMode) return;

        // Ô bị phá/thu hoạch xong có thể biến mất giữa chừng -> lọc lại mỗi lần vẽ.
        currentPlot.RemoveAll(t => t == null);

        int total = currentPlot.Count;
        int planted = 0, ripe = 0, thirsty = 0;
        foreach (var t in currentPlot)
        {
            if (t == null || t.masterTile != null) continue; // ô con của giàn — tính theo ô chính thôi
            if (t.GetCurrentCrop() == null) continue;
            planted++;
            if (t.currentState == FarmTile.TileState.Ripe) ripe++;
            else if (NeedsWater(t)) thirsty++;
        }

        if (lblAnimalName != null) lblAnimalName.text = "Ruộng";
        if (lblStatus != null)
            lblStatus.text = planted == 0
                ? "Ruộng trống — chưa gieo hạt nào."
                : $"Có <color=#5BD66B>{ripe} chín</color> · <color=#FFD54F>{thirsty} cần tưới</color>";

        if (lblEnclosureSummary != null)
            lblEnclosureSummary.text = planted == 0
                ? $"Ruộng: {total} ô"
                : $"Ruộng: {total} ô · {planted} cây — bấm vào cây để làm việc";

        // Cây đang chọn có thể vừa chết/vừa thu xong -> bỏ chọn cho khỏi hiện nhật ký ma.
        if (selectedPlotTile == null || selectedPlotTile.GetCurrentCrop() == null)
            selectedPlotTile = null;

        RebuildPlotCards();
        RefreshCropLog(selectedPlotTile);
        RefreshPlotButtons(selectedPlotTile);
    }

    /// <summary>
    /// Bật/tắt hàng nút theo ĐÚNG trạng thái cây đang chọn — chưa chọn cây thì tắt hết, khỏi bấm hụt.
    /// Điều kiện bón phân hỏi thẳng FarmInteractionController để chỉ có MỘT nơi giữ luật (cây ngắn ngày,
    /// đã tưới, chưa chín); popup không tự đoán lại.
    /// </summary>
    private void RefreshPlotButtons(FarmTile tile)
    {
        if (plotActionsPanel == null) return;

        bool hasCrop = tile != null && tile.GetCurrentCrop() != null;
        bool ripe = hasCrop && tile.currentState == FarmTile.TileState.Ripe;
        bool waterable = hasCrop && (tile.currentState == FarmTile.TileState.Planted
                                     || tile.currentState == FarmTile.TileState.Watered);

        var farm = hasCrop ? Farm : null;
        bool fertilizable = farm != null && farm.CanFertilizeTile(tile);

        if (btnWaterCrop != null) btnWaterCrop.SetEnabled(waterable);
        if (btnHarvestCrop != null) btnHarvestCrop.SetEnabled(ripe);
        if (btnFertilizeCrop != null) btnFertilizeCrop.SetEnabled(fertilizable);
    }

    // Cần tưới = chưa tưới lần nào, hoặc nước đã tụt thấp (cùng ngưỡng với cảnh báo trên nhãn nổi).
    private static bool NeedsWater(FarmTile tile)
    {
        if (tile == null || tile.GetCurrentCrop() == null) return false;
        if (tile.currentState == FarmTile.TileState.Planted) return true;
        return tile.currentState == FarmTile.TileState.Watered && tile.GetWaterFraction() < 0.4f;
    }

    private void RebuildPlotCards()
    {
        if (animalCardList == null) return;

        ClearAnimalCards();

        int index = 0;
        foreach (var tile in currentPlot)
        {
            if (tile == null || tile.masterTile != null) continue; // bỏ ô con của giàn
            var crop = tile.GetCurrentCrop();
            if (crop == null) continue; // ô trống thì không cần thẻ

            index++;
            var captured = tile;
            var card = new VisualElement();
            card.pickingMode = PickingMode.Position;
            card.AddToClassList("ap-animal-card");
            if (captured == selectedPlotTile) card.AddToClassList("ap-animal-card-selected");

            // Đăng ký CẢ ClickEvent lẫn PointerUp (TrickleDown) — giống thẻ thú, cần cho cảm ứng.
            card.RegisterCallback<ClickEvent>(evt => { SelectPlotTile(captured); evt.StopPropagation(); });
            card.RegisterCallback<PointerUpEvent>(evt => { SelectPlotTile(captured); evt.StopPropagation(); },
                TrickleDown.TrickleDown);

            var icon = CreateItemIconElement(crop.harvestItemId, index.ToString());
            icon.AddToClassList("ap-card-icon");
            card.Add(icon);

            var name = new Label(CropDisplayName(crop, index));
            name.AddToClassList("ap-card-name");
            card.Add(name);

            var meta = new Label(FlattenStatus(tile.GetStatusText()));
            meta.AddToClassList("ap-card-meta"); // lớp này đã có white-space: normal
            card.Add(meta);

            animalCardList.Add(card);
        }

        if (index == 0)
        {
            var empty = new Label("Ruộng này chưa gieo hạt nào.");
            empty.AddToClassList("ap-empty");
            animalCardList.Add(empty);
        }
    }

    private void SelectPlotTile(FarmTile tile)
    {
        selectedPlotTile = tile;
        RefreshPlotUI();
    }

    private string CropDisplayName(CropDefinition crop, int index)
    {
        if (crop == null) return $"Ô {index}";
        var def = !string.IsNullOrEmpty(crop.harvestItemId) && itemDatabase != null
            ? itemDatabase.GetItem(crop.harvestItemId) : null;
        string cropName = def != null && !string.IsNullOrEmpty(def.itemName) ? def.itemName : "Cây";
        return $"{index}. {cropName}";
    }

    // GetStatusText xuống dòng cho nhãn nổi 3D; trong thẻ thì gộp lại một dòng cho gọn.
    private static string FlattenStatus(string status)
    {
        if (string.IsNullOrEmpty(status)) return "—";
        return status.Replace("\n", " · ");
    }

    private void SelectAnimal(FarmAnimal animal)
    {
        if (animal == null)
        {
            UnsubscribeCurrentAnimal();
            currentAnimal = null;
            ShowEmptyAnimalDetails();
            RefreshSelectedCardStyles();
            return;
        }

        if (currentAnimal == animal)
        {
            PopulateInfo(currentAnimal);
            RefreshUI(currentAnimal);
            RefreshSelectedCardStyles();
            return;
        }

        UnsubscribeCurrentAnimal();
        currentAnimal = animal;

        if (currentAnimal != null)
        {
            currentAnimal.OnAnimalStateChanged += OnAnimalStateChanged;
            PopulateInfo(currentAnimal);
            RefreshUI(currentAnimal);
        }
        else
        {
            ShowEmptyAnimalDetails();
        }

        RefreshSelectedCardStyles();
    }

    private void UnsubscribeCurrentAnimal()
    {
        if (currentAnimal != null)
            currentAnimal.OnAnimalStateChanged -= OnAnimalStateChanged;
    }

    private void OnAnimalStateChanged(FarmAnimal animal)
    {
        if (animal == currentAnimal)
        {
            PopulateInfo(animal);
            RefreshUI(animal);
        }

        if (IsEnclosureMode)
            RefreshEnclosureUI();
    }

    private void RefreshEnclosureUI()
    {
        if (!IsEnclosureMode)
            return;

        RefreshEnclosureAnimals();

        if (currentAnimal == null || !enclosureAnimals.Contains(currentAnimal))
            SelectAnimal(enclosureAnimals.Count > 0 ? enclosureAnimals[0] : null);
        else
            RefreshUI(currentAnimal);

        int free = PenEnclosure.AvailableCount(currentEnclosure);
        if (lblEnclosureSummary != null)
            lblEnclosureSummary.text = $"Chuồng: {enclosureAnimals.Count} thú · còn {free} ô";

        if (btnAddAnimal != null)
        {
            btnAddAnimal.style.display = DisplayStyle.Flex;
            btnAddAnimal.SetEnabled(free > 0);
            btnAddAnimal.text = free > 0 ? "+ Thả thú" : "Chuồng đầy";
        }

        RebuildAnimalCards();
    }

    private void RefreshEnclosureAnimals()
    {
        enclosureAnimals.Clear();
        var animals = PenEnclosure.FindAnimals(currentEnclosure);
        foreach (var animal in animals)
        {
            if (animal != null && animal.currentState != FarmAnimal.AnimalState.Dead)
                enclosureAnimals.Add(animal);
        }
    }

    private void RebuildAnimalCards()
    {
        if (animalCardList == null) return;

        ClearAnimalCards();

        if (enclosureAnimals.Count == 0)
        {
            var empty = new Label("Chuồng chưa có thú. Bấm Thả thú để thêm.");
            empty.AddToClassList("ap-empty");
            animalCardList.Add(empty);
            return;
        }

        foreach (var animal in enclosureAnimals)
        {
            var captured = animal;
            var card = new VisualElement();
            card.pickingMode = PickingMode.Position;
            card.AddToClassList("ap-animal-card");
            if (captured == currentAnimal)
                card.AddToClassList("ap-animal-card-selected");

            var icon = CreateAnimalIconElement(captured);
            icon.AddToClassList("ap-card-icon");
            card.Add(icon);

            var name = new Label(captured.data != null ? captured.data.animalName : "Thú nuôi");
            name.AddToClassList("ap-card-name");
            card.Add(name);

            var meta = new Label(CardStatusText(captured));
            meta.AddToClassList("ap-card-meta");
            card.Add(meta);

            card.RegisterCallback<ClickEvent>(evt =>
            {
                SelectAnimal(captured);
                evt.StopPropagation();
            });
            card.RegisterCallback<PointerUpEvent>(evt =>
            {
                SelectAnimal(captured);
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);

            animalCards[captured] = card;
            animalCardList.Add(card);
        }
    }

    private void ClearAnimalCards()
    {
        animalCards.Clear();
        if (animalCardList != null)
            animalCardList.Clear();
    }

    private void RefreshSelectedCardStyles()
    {
        foreach (var pair in animalCards)
        {
            if (pair.Value == null) continue;
            if (pair.Key == currentAnimal)
                pair.Value.AddToClassList("ap-animal-card-selected");
            else
                pair.Value.RemoveFromClassList("ap-animal-card-selected");
        }
    }

    private VisualElement CreateAnimalIconElement(FarmAnimal animal)
    {
        string itemId = animal != null && animal.data != null ? animal.data.animalId : "";
        return CreateItemIconElement(itemId, AnimalIconText(animal));
    }

    /// <summary>Ảnh vật phẩm theo id; không có ảnh thì trả về chữ thay thế. Dùng chung cho thẻ thú và thẻ cây.</summary>
    private VisualElement CreateItemIconElement(string itemId, string fallbackText)
    {
        ItemDefinition def = !string.IsNullOrEmpty(itemId) && itemDatabase != null ? itemDatabase.GetItem(itemId) : null;

        if (def != null && (def.iconTexture != null || def.iconSprite != null))
        {
            var image = new Image { scaleMode = ScaleMode.ScaleToFit };
            if (def.iconTexture != null)
                image.image = def.iconTexture;
            else
                image.sprite = def.iconSprite;
            return image;
        }

        var fallback = new Label(fallbackText);
        fallback.AddToClassList("ap-card-icon-fallback");
        return fallback;
    }

    private static string AnimalIconText(FarmAnimal animal)
    {
        string name = animal != null && animal.data != null ? animal.data.animalName : "";
        if (string.IsNullOrEmpty(name)) return "T";
        return name.Substring(0, 1).ToUpperInvariant();
    }

    private static string CardStatusText(FarmAnimal animal)
    {
        if (animal == null) return "";
        if (animal.currentState == FarmAnimal.AnimalState.Sick) return "Đang bệnh";
        if (animal.currentState == FarmAnimal.AnimalState.Hungry) return "Đang đói";
        if (animal.hasProductReady) return "Có sản phẩm";
        return "No " + Mathf.RoundToInt(animal.GetHungerFraction() * 100f) + "%";
    }

    // Nạp bảng thông tin tĩnh của con vật (giá / ô chuồng / thức ăn / sản phẩm) từ AnimalDefinition.
    private void PopulateInfo(FarmAnimal animal)
    {
        AnimalDefinition d = animal != null ? animal.data : null;
        if (lblPrice != null) lblPrice.text = d != null ? $"{d.buyPrice} Point" : "—";
        if (lblSlots != null) lblSlots.text = d != null ? $"{d.penSlots} ô" : "—";
        if (lblFoodMain != null) lblFoodMain.text = d != null ? FoodText(d.foodMainName, d.foodMainAmount) : "—";
        if (lblFoodAlt != null) lblFoodAlt.text = d != null ? FoodText(d.foodAltName, d.foodAltAmount) : "—";
        if (lblProducts != null) lblProducts.text = d != null ? ProductText(d) : "—";
    }

    private static string FoodText(string name, int amount)
    {
        if (string.IsNullOrEmpty(name)) return "—";
        return amount > 0 ? $"{amount}x {name}" : name;
    }

    private static string ProductText(AnimalDefinition d)
    {
        string main = FoodText(d.productMainName, d.productMainAmount);
        // Chỉ hiện sản phẩm phụ (thịt) nếu con vật thật sự ra thịt.
        string alt = string.IsNullOrEmpty(d.meatItemId) ? "—" : FoodText(d.productAltName, d.productAltAmount);
        if (main != "—" && alt != "—") return $"{main}, {alt}";
        if (main != "—") return main;
        return alt;
    }

    /// <summary>Số mốc cho ăn gần nhất hiện trong popup (cũ hơn vẫn nằm trong nhật ký, chỉ không hiện).</summary>
    private const int FeedLogLinesShown = 5;

    /// <summary>Chiều cao khung danh sách ở chế độ Xem ruộng (chuồng thú giữ 160px theo USS).</summary>
    private const float PlotScrollHeight = 320f;

    // Một hàng nhật ký trong popup: tiêu đề + loại thao tác + chữ hiện khi chưa có mốc nào.
    private struct LogRow
    {
        public string title;
        public string kind;
        public string emptyText;

        public LogRow(string title, string kind, string emptyText)
        {
            this.title = title; this.kind = kind; this.emptyText = emptyText;
        }
    }

    // Nhật ký của CHÍNH con vật đang xem (khách chốt 30/07).
    private void RefreshFeedLog(FarmAnimal animal)
    {
        if (animal == null)
        {
            if (feedLogPanel != null) feedLogPanel.style.display = DisplayStyle.None;
            return;
        }

        ShowLogPanel(animal.animalInstanceId,
            new LogRow("Lịch sử cho ăn", FarmActivityLog.KindFeed, "Chưa cho ăn lần nào"),
            new LogRow("Lịch sử thu hoạch", FarmActivityLog.KindHarvest, "Chưa thu hoạch lần nào"),
            new LogRow("Lịch sử chữa bệnh", FarmActivityLog.KindHeal, "Chưa phải chữa lần nào"),
            new LogRow("Lịch sử tiêm vắc-xin", FarmActivityLog.KindVaccine, "Chưa tiêm lần nào"));
    }

    // Nhật ký của CHÍNH cây đang chọn trong popup Xem ruộng.
    private void RefreshCropLog(FarmTile tile)
    {
        if (tile == null || tile.GetCurrentCrop() == null)
        {
            if (feedLogPanel != null) feedLogPanel.style.display = DisplayStyle.None;
            return;
        }

        ShowLogPanel(tile.HistoryKey,
            new LogRow("Lịch sử tưới nước", FarmActivityLog.KindWater, "Chưa tưới lần nào"),
            new LogRow("Lịch sử bón phân", FarmActivityLog.KindFertilize, "Chưa bón lần nào"),
            new LogRow("Lịch sử thu hoạch", FarmActivityLog.KindHarvest, "Chưa thu hoạch lần nào"));
    }

    /// <summary>
    /// Dựng khối nhật ký cho một đối tượng. Popup tự làm mới 4 lần/giây nên CHỈ dựng lại hàng khi
    /// đổi đối tượng hoặc đổi bộ hàng; còn lại chỉ thay chữ, khỏi sinh rác mỗi lần làm mới.
    /// </summary>
    private void ShowLogPanel(string ownerId, params LogRow[] rows)
    {
        if (feedLogPanel == null || rows == null || rows.Length == 0) return;
        feedLogPanel.style.display = DisplayStyle.Flex;

        string signature = ownerId + "|" + rows.Length + "|" + rows[0].kind;
        if (signature != logPanelSignature)
        {
            logPanelSignature = signature;
            feedLogPanel.Clear();
            logValueLabels.Clear();

            for (int i = 0; i < rows.Length; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("ap-row");
                if (i == rows.Length - 1) row.AddToClassList("ap-row-last");

                var key = new Label(rows[i].title);
                key.AddToClassList("ap-key");
                row.Add(key);

                var val = new Label();
                val.AddToClassList("ap-val");
                val.style.whiteSpace = WhiteSpace.Normal; // nhật ký nhiều dòng
                row.Add(val);

                logValueLabels.Add(val);
                feedLogPanel.Add(row);
            }
        }

        for (int i = 0; i < rows.Length && i < logValueLabels.Count; i++)
        {
            logValueLabels[i].text = FarmActivityLog.FormatHistoryLines(
                FarmActivityLog.GetHistory(ownerId, rows[i].kind, FeedLogLinesShown), rows[i].emptyText);
        }
    }

    // Đếm ngược vụ thu là số "sống" nên cập nhật định kỳ khi popup đang mở.
    void Update()
    {
        if (container == null || container.style.display == DisplayStyle.None) return;

        refreshTimer += Time.deltaTime;
        if (refreshTimer < 0.25f) return;

        refreshTimer = 0f;
        if (IsPlotMode)
            RefreshPlotUI();
        else if (IsEnclosureMode)
            RefreshEnclosureUI();
        else if (currentAnimal != null)
            RefreshUI(currentAnimal);
    }

    // "Vụ tới 12s · 20/20 lần" - gộp đếm ngược + tổng số lần thu.
    private static string HarvestInfoText(FarmAnimal animal)
    {
        if (animal == null || animal.data == null) return "—";

        float t = animal.GetTimeToNextProduceSec();
        string when;
        if (t < 0f) when = "Hết vụ";
        else if (t <= 0.5f) when = "<color=#5BD66B>Sẵn sàng</color>";
        else when = "Vụ tới " + FormatDuration(t);

        string count = animal.IsInfiniteHarvest
            ? "∞ lần"
            : $"{Mathf.Max(0, animal.harvestsRemaining)}/{animal.MaxHarvests} lần";

        return when + " · " + count;
    }

    // Dùng CHUNG bộ format với cây trồng: "x ngày x giờ" / "x giờ x phút" / "x phút x giây" / "x giây".
    private static string FormatDuration(float sec) => YWonderLand.Core.GameTimeConfig.FormatDuration(sec);

    public void Hide()
    {
        UIPopupTracker.SetOpen(this, false);
        if (container != null)
            container.style.display = DisplayStyle.None;

        UnsubscribeCurrentAnimal();
        currentAnimal = null;
        currentEnclosure = null;
        currentPlot = null;
        selectedPlotTile = null;
        logPanelSignature = null;
        enclosureAnimals.Clear();
        if (enclosurePanel != null) enclosurePanel.style.display = DisplayStyle.None;

        // Chế độ Xem ruộng có ẩn bớt mấy khối của thú + nới khung danh sách —
        // trả lại hết để lần mở sau (chuồng thú) hiện đúng như cũ.
        if (btnAddAnimal != null) btnAddAnimal.style.display = DisplayStyle.Flex;
        if (animalCardScroll != null) animalCardScroll.style.height = StyleKeyword.Null;
        if (plotActionsPanel != null) plotActionsPanel.style.display = DisplayStyle.None;
        if (btnMoveGroup != null) btnMoveGroup.style.display = DisplayStyle.None;

        ClearAnimalCards();
    }

    private void ShowEmptyAnimalDetails()
    {
        if (lblAnimalName != null) lblAnimalName.text = "Chuồng thú";
        if (lblStatus != null) lblStatus.text = "Chưa chọn thú.";
        if (lblPrice != null) lblPrice.text = "—";
        if (lblSlots != null) lblSlots.text = "—";
        if (lblFoodMain != null) lblFoodMain.text = "—";
        if (lblFoodAlt != null) lblFoodAlt.text = "—";
        if (lblProducts != null) lblProducts.text = "—";
        if (lblHunger != null) lblHunger.text = "—";
        if (lblHarvest != null) lblHarvest.text = "—";
        if (infoPanel != null) infoPanel.style.display = DisplayStyle.None;
        if (actionsPanel != null) actionsPanel.style.display = DisplayStyle.None;
        if (feedLogPanel != null) feedLogPanel.style.display = DisplayStyle.None; // chưa chọn con nào thì không có nhật ký
        if (btnFeed != null) btnFeed.style.display = DisplayStyle.None;
        if (btnHarvest != null) btnHarvest.style.display = DisplayStyle.None;
        if (btnHeal != null) btnHeal.style.display = DisplayStyle.None;
        if (btnVaccine != null) btnVaccine.style.display = DisplayStyle.None;
    }

    private void RefreshUI(FarmAnimal animal)
    {
        if (animal == null || animal != currentAnimal)
        {
            if (currentAnimal == null) ShowEmptyAnimalDetails();
            return;
        }

        if (infoPanel != null) infoPanel.style.display = DisplayStyle.Flex;
        if (actionsPanel != null) actionsPanel.style.display = DisplayStyle.Flex;

        if (lblAnimalName != null) lblAnimalName.text = animal.data != null ? animal.data.animalName : "Thú nuôi";

        string statusStr = "Khỏe mạnh";
        switch (animal.currentState)
        {
            case FarmAnimal.AnimalState.Hungry: statusStr = "<color=#FFA500>Đang đói</color>"; break;
            case FarmAnimal.AnimalState.Sick: statusStr = "<color=#FF0000>Đang bệnh</color>"; break;
            case FarmAnimal.AnimalState.Dead: statusStr = "<color=#000000>Đã chết</color>"; break;
        }
        if (animal.hasProductReady)
            statusStr += " - <color=#00AA00>Có sản phẩm!</color>";

        if (lblStatus != null) lblStatus.text = "Trạng thái: " + statusStr;
        if (lblHunger != null) lblHunger.text = Mathf.RoundToInt(animal.GetHungerFraction() * 100f) + "%";
        if (lblHarvest != null) lblHarvest.text = HarvestInfoText(animal);
        RefreshFeedLog(animal);

        bool isDead = animal.currentState == FarmAnimal.AnimalState.Dead;
        bool canFeed = (animal.currentState == FarmAnimal.AnimalState.Hungry || animal.currentState == FarmAnimal.AnimalState.Healthy) && !isDead;

        if (btnFeed != null)
        {
            btnFeed.style.display = DisplayStyle.Flex;
            btnFeed.SetEnabled(canFeed);
        }
        if (btnHarvest != null)
        {
            btnHarvest.style.display = DisplayStyle.Flex;
            btnHarvest.SetEnabled(animal.hasProductReady && !isDead);
        }
        // CHỮA BỆNH: chỉ bật khi con vật ĐANG BỆNH (tốn 1 Thuốc).
        if (btnHeal != null)
        {
            btnHeal.style.display = DisplayStyle.Flex;
            btnHeal.SetEnabled(!isDead && animal.currentState == FarmAnimal.AnimalState.Sick);
        }
        // TIÊM VẮC-XIN: phòng bệnh — bật khi KHÔNG bệnh và vắc-xin cũ đã hết hạn (tốn 1 Vắc-xin).
        if (btnVaccine != null)
        {
            btnVaccine.style.display = DisplayStyle.Flex;
            btnVaccine.SetEnabled(!isDead
                && animal.currentState != FarmAnimal.AnimalState.Sick
                && !animal.IsVaccineActive);
        }
    }

    private void OnAddAnimalClicked()
    {
        if (!IsEnclosureMode || currentEnclosure == null) return;

        var enclosure = new List<BuildSurfaceCell>(currentEnclosure);
        Hide();

        var fic = Farm;
        if (fic != null)
            fic.BeginPlaceAnimalInEnclosure(enclosure);
        else
            Debug.LogWarning("[AnimalPopup] Không tìm thấy FarmInteractionController để mở túi thả thú.");
    }

    // ── Việc của CẢ CỤM: dời chuồng / dời ruộng ─────────────────────────────────────────────
    // Luôn ĐÓNG popup trước: người chơi phải nhìn thấy cụm đang rê theo bước chân mình mới đặt được.

    private void OnMoveGroupClicked()
    {
        var fic = Farm;
        if (fic == null)
        {
            Debug.LogWarning("[AnimalPopup] Không tìm thấy FarmInteractionController để dời.");
            return;
        }

        if (IsPlotMode)
        {
            FarmTile seed = FirstPlotTile();
            if (seed == null) return;
            Hide();
            fic.BeginMovePlot(seed);
        }
        else if (IsEnclosureMode && currentEnclosure != null)
        {
            var pen = new List<BuildSurfaceCell>(currentEnclosure);
            Hide();
            fic.BeginMovePen(pen);
        }
    }

    /// <summary>Một ô bất kỳ của mảnh ruộng — BeginMovePlot tự loang lại ra cả mảnh từ ô này.</summary>
    private FarmTile FirstPlotTile()
    {
        if (currentPlot == null) return null;
        foreach (var t in currentPlot)
            if (t != null) return t;
        return null;
    }

    // ── Việc của CÂY đang chọn ──────────────────────────────────────────────────────────────

    private void OnWaterCropClicked()
    {
        var tile = selectedPlotTile;
        var fic = Farm;
        if (tile == null || fic == null) return;

        Hide();                  // tưới có màn múa động tác — che popup thì không thấy gì
        fic.BeginWaterTile(tile);
    }

    private void OnHarvestCropClicked()
    {
        var tile = selectedPlotTile;
        var fic = Farm;
        if (tile == null || fic == null) return;

        // Thu hoạch không có màn múa: GIỮ popup mở để thu liền tay nhiều cây trong cùng mảnh ruộng.
        fic.BeginHarvestTile(tile);
        RefreshPlotUI();
    }

    private void OnFertilizeCropClicked()
    {
        var tile = selectedPlotTile;
        var fic = Farm;
        if (tile == null || fic == null) return;

        Hide();                  // bón phân mở túi đồ để chọn phân
        fic.BeginFertilizeTile(tile);
    }

    private void OnFeedClicked()
    {
        if (currentAnimal == null) return;

        // Đi cùng luồng phím F: đóng popup -> mở túi đồ chọn thức ăn -> animation cho ăn.
        var animal = currentAnimal;
        Hide();

        var fic = Object.FindFirstObjectByType<FarmInteractionController>();
        if (fic != null)
            fic.BeginFeed(animal);
        else
            Debug.LogWarning("[AnimalPopup] Không tìm thấy FarmInteractionController để mở luồng cho ăn.");
    }

    private void OnHarvestClicked()
    {
        if (currentAnimal == null) return;

        var animal = currentAnimal;
        bool wasEnclosureMode = IsEnclosureMode;

        float animalDays = (animal.data != null)
            ? animal.data.produceCycleTimeSec / YWonderLand.Core.GameTimeConfig.SecondsPerGameDay : 0f;

        if (animal.HarvestProduct(out string itemId, out int amount))
        {
            Debug.Log($"[AnimalPopup] Harvested {amount} of {itemId}");
            InventoryManager.Instance?.AddItem(itemId, amount);

            int aexp = Mathf.Max(1, Mathf.RoundToInt(animalDays * 10f));
            ExperienceManager.Instance?.AddEXP(aexp);
            ScreenToast.ShowItemReward(itemId, amount, "Thu hoạch");

            if (wasEnclosureMode)
                RefreshEnclosureUI();
            else
                Hide();
        }
    }

    // ID item dùng cho chữa bệnh / phòng bệnh (đã có sẵn trong ItemDatabase + bán ở shop).
    private const string MedicineItemId = "medicine_01";
    private const string VaccineItemId = "vaccine_01";

    /// <summary>CHỮA BỆNH: tốn số Thuốc theo loài (VatNuoi2), chỉ dùng được khi con vật đang bệnh.</summary>
    private void OnHealClicked()
    {
        if (currentAnimal == null) return;

        if (currentAnimal.currentState != FarmAnimal.AnimalState.Sick)
        {
            ScreenToast.Show("Con vật không bị bệnh, chưa cần dùng thuốc.");
            return;
        }
        // Số LIỀU thuốc theo loài (VatNuoi2: bò 10 · heo 9 · vịt 2...), không còn cứng 1 liều.
        int doses = currentAnimal.MedicineDosesPerCure;
        if (!ConsumeCareItem(MedicineItemId, "Thuốc", doses)) return;

        if (currentAnimal.Heal())
        {
            ScreenToast.Show($"Đã chữa khỏi bệnh (tốn {doses} Thuốc)!");
            RefreshUI(currentAnimal);
            if (IsEnclosureMode) RefreshEnclosureUI();
        }
        else
        {
            InventoryManager.Instance?.AddItem(MedicineItemId, doses); // không chữa được thì hoàn đủ
        }
    }

    /// <summary>TIÊM VẮC-XIN: tốn 1 Vắc-xin, phòng bệnh một thời gian. Không chữa được thú đang bệnh.</summary>
    private void OnVaccineClicked()
    {
        if (currentAnimal == null) return;

        if (currentAnimal.currentState == FarmAnimal.AnimalState.Sick)
        {
            ScreenToast.Show("Con vật đang bệnh — phải dùng Thuốc chữa trước, rồi mới tiêm phòng.");
            return;
        }
        if (currentAnimal.IsVaccineActive)
        {
            ScreenToast.Show("Vắc-xin còn hiệu lực, chưa cần tiêm lại.");
            return;
        }
        if (!ConsumeCareItem(VaccineItemId, "Vắc-xin")) return;

        if (currentAnimal.Vaccinate())
        {
            ScreenToast.Show("Đã tiêm vắc-xin — con vật được phòng bệnh.");
            RefreshUI(currentAnimal);
            if (IsEnclosureMode) RefreshEnclosureUI();
        }
        else
        {
            InventoryManager.Instance?.AddItem(VaccineItemId, 1); // tiêm hụt thì hoàn lại
        }
    }

    /// <summary>Trừ item chăm sóc theo SỐ LIỀU; thiếu thì báo và trả false (KHÔNG trừ).</summary>
    private bool ConsumeCareItem(string itemId, string displayName, int amount = 1)
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return false;
        if (inv.GetItemQuantity(itemId) < amount)
        {
            ScreenToast.Show($"Cần {amount} {displayName}! Mua thêm ở Cửa hàng vật phẩm.");
            return false;
        }
        return inv.RemoveItem(itemId, amount);
    }
}
