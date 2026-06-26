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
    private VisualElement enclosurePanel;
    private VisualElement infoPanel;
    private VisualElement actionsPanel;
    private Label lblEnclosureSummary;
    private Button btnAddAnimal;
    private VisualElement animalCardList;
    private Button btnClose;
    private Button btnFeed;
    private Button btnHarvest;
    private Button btnHeal;
    private Button btnVaccine;
    private ItemDatabase itemDatabase;

    private FarmAnimal currentAnimal;
    private List<BuildSurfaceCell> currentEnclosure;
    private readonly List<FarmAnimal> enclosureAnimals = new List<FarmAnimal>();
    private readonly Dictionary<FarmAnimal, VisualElement> animalCards = new Dictionary<FarmAnimal, VisualElement>();

    private float refreshTimer;

    private bool IsEnclosureMode => currentEnclosure != null;

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

        // Cho phép xuống dòng nếu chữ dài, tránh tràn ra ngoài panel.
        if (lblHarvest != null) lblHarvest.style.whiteSpace = WhiteSpace.Normal;
        if (lblStatus != null) lblStatus.style.whiteSpace = WhiteSpace.Normal;

        btnClose = root.Q<Button>("BtnClose");
        btnFeed = root.Q<Button>("BtnFeed");
        btnHarvest = root.Q<Button>("BtnHarvest");
        btnHeal = root.Q<Button>("BtnHeal");
        btnVaccine = root.Q<Button>("BtnVaccine");

        if (btnClose != null) btnClose.clicked += Hide;
        if (btnFeed != null) btnFeed.clicked += OnFeedClicked;
        if (btnHarvest != null) btnHarvest.clicked += OnHarvestClicked;
        if (btnHeal != null) btnHeal.clicked += OnHealClicked;
        if (btnAddAnimal != null) btnAddAnimal.clicked += OnAddAnimalClicked;

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
        if (enclosurePanel != null) enclosurePanel.style.display = DisplayStyle.None;
        ClearAnimalCards();

        container.style.display = DisplayStyle.Flex;
        UIPopupTracker.SetOpen(this, true);
        SelectAnimal(animal);
    }

    public void ShowEnclosure(List<BuildSurfaceCell> enclosure)
    {
        if (enclosure == null || container == null) return;

        currentEnclosure = new List<BuildSurfaceCell>(enclosure);
        container.style.display = DisplayStyle.Flex;
        UIPopupTracker.SetOpen(this, true);
        if (enclosurePanel != null) enclosurePanel.style.display = DisplayStyle.Flex;

        RefreshEnclosureUI();
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

        var fallback = new Label(AnimalIconText(animal));
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
        if (lblPrice != null) lblPrice.text = d != null ? $"{d.buyPrice} POS" : "—";
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

    // Đếm ngược vụ thu là số "sống" nên cập nhật định kỳ khi popup đang mở.
    void Update()
    {
        if (container == null || container.style.display == DisplayStyle.None) return;

        refreshTimer += Time.deltaTime;
        if (refreshTimer < 0.25f) return;

        refreshTimer = 0f;
        if (IsEnclosureMode)
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

    private static string FormatDuration(float sec)
    {
        if (sec < 60f) return Mathf.CeilToInt(sec) + "s";
        if (sec < 3600f) return Mathf.FloorToInt(sec / 60f) + "m" + Mathf.CeilToInt(sec % 60f) + "s";
        return Mathf.FloorToInt(sec / 3600f) + "h" + Mathf.FloorToInt((sec % 3600f) / 60f) + "m";
    }

    public void Hide()
    {
        UIPopupTracker.SetOpen(this, false);
        if (container != null)
            container.style.display = DisplayStyle.None;

        UnsubscribeCurrentAnimal();
        currentAnimal = null;
        currentEnclosure = null;
        enclosureAnimals.Clear();
        if (enclosurePanel != null) enclosurePanel.style.display = DisplayStyle.None;
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
        if (btnHeal != null)
        {
            btnHeal.style.display = DisplayStyle.Flex;
            btnHeal.SetEnabled(false);
        }
        if (btnVaccine != null)
        {
            btnVaccine.style.display = DisplayStyle.Flex;
            btnVaccine.SetEnabled(false);
        }
    }

    private void OnAddAnimalClicked()
    {
        if (!IsEnclosureMode || currentEnclosure == null) return;

        var enclosure = new List<BuildSurfaceCell>(currentEnclosure);
        Hide();

        var fic = Object.FindFirstObjectByType<FarmInteractionController>();
        if (fic != null)
            fic.BeginPlaceAnimalInEnclosure(enclosure);
        else
            Debug.LogWarning("[AnimalPopup] Không tìm thấy FarmInteractionController để mở túi thả thú.");
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
            ScreenToast.ShowInfo($"Thu hoạch: +{amount} {itemId}");

            if (wasEnclosureMode)
                RefreshEnclosureUI();
            else
                Hide();
        }
    }

    private void OnHealClicked()
    {
        if (currentAnimal == null) return;

        // TODO: Chờ khách chốt vaccine/thuốc chữa bệnh để trừ item đúng thiết kế.
        if (currentAnimal.Heal())
        {
            Debug.Log("[AnimalPopup] Healed the animal.");
            RefreshUI(currentAnimal);
            if (IsEnclosureMode) RefreshEnclosureUI();
        }
    }
}
