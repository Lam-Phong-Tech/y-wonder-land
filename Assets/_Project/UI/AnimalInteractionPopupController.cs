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
    private Button btnClose;
    private Button btnFeed;
    private Button btnHarvest;
    private Button btnHeal;

    private FarmAnimal currentAnimal;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        if (document == null) document = GetComponent<UIDocument>();
        if (document == null || document.rootVisualElement == null) return;

        var root = document.rootVisualElement;

        container = root.Q<VisualElement>("AnimalPopupContainer");
        lblAnimalName = root.Q<Label>("LblAnimalName");
        lblStatus = root.Q<Label>("LblStatus");
        lblPrice = root.Q<Label>("LblPrice");
        lblSlots = root.Q<Label>("LblSlots");
        lblFoodMain = root.Q<Label>("LblFoodMain");
        lblFoodAlt = root.Q<Label>("LblFoodAlt");
        lblProducts = root.Q<Label>("LblProducts");

        btnClose = root.Q<Button>("BtnClose");
        btnFeed = root.Q<Button>("BtnFeed");
        btnHarvest = root.Q<Button>("BtnHarvest");
        btnHeal = root.Q<Button>("BtnHeal");

        if (btnClose != null) btnClose.clicked += Hide;
        if (btnFeed != null) btnFeed.clicked += OnFeedClicked;
        if (btnHarvest != null) btnHarvest.clicked += OnHarvestClicked;
        if (btnHeal != null) btnHeal.clicked += OnHealClicked;

        Hide();
    }

    // An toàn: nếu popup bị tắt/destroy khi đang mở (vd đổi đảo) mà chưa kịp gọi Hide(),
    // vẫn gỡ khỏi UIPopupTracker để chuột không bị kẹt + tương tác thế giới không chết.
    private void OnDisable()
    {
        UIPopupTracker.SetOpen(this, false);
    }

    public void Show(FarmAnimal animal)
    {
        if (animal == null || container == null) return;

        currentAnimal = animal;
        currentAnimal.OnAnimalStateChanged += RefreshUI; // Listen for changes

        container.style.display = DisplayStyle.Flex;
        UIPopupTracker.SetOpen(this, true);
        PopulateInfo(currentAnimal);
        RefreshUI(currentAnimal);
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
        string alt = FoodText(d.productAltName, d.productAltAmount);
        if (main != "—" && alt != "—") return $"{main}, {alt}";
        if (main != "—") return main;
        return alt;
    }

    public void Hide()
    {
        UIPopupTracker.SetOpen(this, false);
        if (container != null)
        {
            container.style.display = DisplayStyle.None;
        }
        if (currentAnimal != null)
        {
            currentAnimal.OnAnimalStateChanged -= RefreshUI;
            currentAnimal = null;
        }
    }

    private void RefreshUI(FarmAnimal animal)
    {
        if (animal != currentAnimal) return;

        lblAnimalName.text = animal.data != null ? animal.data.animalName : "Thú nuôi";

        // Status text
        string statusStr = "Khỏe mạnh";
        switch (animal.currentState)
        {
            case FarmAnimal.AnimalState.Hungry: statusStr = "<color=#FFA500>Đang đói</color>"; break;
            case FarmAnimal.AnimalState.Sick: statusStr = "<color=#FF0000>Đang bệnh</color>"; break;
            case FarmAnimal.AnimalState.Dead: statusStr = "<color=#000000>Đã chết</color>"; break;
        }
        if (animal.hasProductReady)
        {
            statusStr += " - <color=#00AA00>Có sản phẩm!</color>";
        }
        lblStatus.text = "Trạng thái: " + statusStr;

        // Button visibility
        bool isDead = animal.currentState == FarmAnimal.AnimalState.Dead;
        
        btnFeed.style.display = (animal.currentState == FarmAnimal.AnimalState.Hungry || animal.currentState == FarmAnimal.AnimalState.Healthy) && !isDead ? DisplayStyle.Flex : DisplayStyle.None;
        btnHarvest.style.display = animal.hasProductReady && !isDead ? DisplayStyle.Flex : DisplayStyle.None;
        btnHeal.style.display = animal.currentState == FarmAnimal.AnimalState.Sick && !isDead ? DisplayStyle.Flex : DisplayStyle.None;

        // Dim buttons if they don't apply right now
        btnFeed.SetEnabled(animal.currentState == FarmAnimal.AnimalState.Hungry);
    }

    private void OnFeedClicked()
    {
        if (currentAnimal == null) return;

        // Đi CÙNG luồng với phím F: đóng popup -> mở túi đồ chọn thức ăn -> animation cho ăn.
        var animal = currentAnimal;
        Hide();

        var fic = Object.FindFirstObjectByType<FarmInteractionController>();
        if (fic != null)
        {
            fic.BeginFeed(animal);
        }
        else
        {
            Debug.LogWarning("[AnimalPopup] Không tìm thấy FarmInteractionController để mở luồng cho ăn.");
        }
    }

    private void OnHarvestClicked()
    {
        if (currentAnimal == null) return;

        if (currentAnimal.HarvestProduct(out string itemId, out int amount))
        {
            Debug.Log($"[AnimalPopup] Harvested {amount} of {itemId}");
            InventoryManager.Instance?.AddItem(itemId, amount);
            // Optionally hide after harvest
            Hide();
        }
    }

    private void OnHealClicked()
    {
        if (currentAnimal == null) return;

        // TODO: Check inventory for vaccine
        if (currentAnimal.Heal())
        {
            Debug.Log("[AnimalPopup] Healed the animal.");
        }
    }
}
