using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Piggy Bank (Heo Đất) Popup.
/// Allows players to deposit POS into 3 savings packages (12/30/180 days).
/// Only 1 active deposit at a time. Cannot withdraw early.
/// When matured, principal + interest auto-sent to Mailbox.
/// </summary>
public class PiggyBankPopupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument piggyDocument;

    // ── Elements ──
    private VisualElement overlay;
    private Button btnClose;
    private Label lblBalance;

    // Tabs
    private Button tabDeposit;
    private Button tabHistory;
    private VisualElement panelDeposit;
    private VisualElement panelHistory;

    // Packages
    private VisualElement pkg12;
    private VisualElement pkg30;
    private VisualElement pkg180;

    // Deposit form
    private VisualElement depositForm;
    private Label lblFormPkg;
    private TextField txtAmount;
    private Label lblPreviewPrincipal;
    private Label lblPreviewInterest;
    private Label lblPreviewTotal;
    private Label lblFormError;
    private Button btnDeposit;

    // Active deposit
    private VisualElement activeDeposit;
    private Label lblActivePkg;
    private Label lblActivePrincipal;
    private Label lblActiveInterest;
    private Label lblCountdown;

    // History
    private VisualElement historyList;

    // ── State ──
    private float playerBalance = 5000f;
    private int selectedPackageIndex = 0; // 0=12d, 1=30d, 2=180d
    private bool hasActiveDeposit = false;

    // Active deposit data
    private int activePkgIndex;
    private float activePrincipal;
    private float activeInterestAmount;
    private DateTime activeMaturityDate;
    private Coroutine countdownCoroutine;

    // History data
    private List<HistoryEntry> historyEntries = new List<HistoryEntry>();

    // ── Package Data ──

    private struct PackageInfo
    {
        public int days;
        public float rate; // e.g. 0.02 = 2%
        public string label;
    }

    private PackageInfo[] packages = new PackageInfo[]
    {
        new PackageInfo { days = 12, rate = 0.02f, label = "12 ngày" },
        new PackageInfo { days = 30, rate = 0.06f, label = "30 ngày" },
        new PackageInfo { days = 180, rate = 0.45f, label = "180 ngày" },
    };

    private struct HistoryEntry
    {
        public string pkgName;
        public float principal;
        public float interest;
        public string status; // "Đã nhận", "Đang gửi"
        public string date;
    }

    // ── Lifecycle ──

    private void Awake()
    {
        if (piggyDocument == null)
            piggyDocument = GetComponent<UIDocument>();

        // Add mock history
        historyEntries.Add(new HistoryEntry
        {
            pkgName = "Gói 12 ngày (+2%)",
            principal = 200, interest = 4,
            status = "Đã nhận", date = "28/05/2026"
        });
        historyEntries.Add(new HistoryEntry
        {
            pkgName = "Gói 30 ngày (+6%)",
            principal = 500, interest = 30,
            status = "Đã nhận", date = "15/05/2026"
        });
    }

    private void OnEnable()
    {
        var root = piggyDocument.rootVisualElement;
        QueryElements(root);
        RegisterCallbacks();
        Hide();
    }

    private void QueryElements(VisualElement root)
    {
        overlay = root.Q<VisualElement>("PiggyOverlay");
        btnClose = root.Q<Button>("BtnClosePiggy");
        lblBalance = root.Q<Label>("LblPiggyBalance");

        // Tabs
        tabDeposit = root.Q<Button>("TabDeposit");
        tabHistory = root.Q<Button>("TabHistory");
        panelDeposit = root.Q<VisualElement>("PanelDeposit");
        panelHistory = root.Q<VisualElement>("PanelHistory");

        // Packages
        pkg12 = root.Q<VisualElement>("Pkg12");
        pkg30 = root.Q<VisualElement>("Pkg30");
        pkg180 = root.Q<VisualElement>("Pkg180");

        // Form
        depositForm = root.Q<VisualElement>("DepositForm");
        lblFormPkg = root.Q<Label>("LblFormPkg");
        txtAmount = root.Q<TextField>("TxtAmount");
        lblPreviewPrincipal = root.Q<Label>("LblPreviewPrincipal");
        lblPreviewInterest = root.Q<Label>("LblPreviewInterest");
        lblPreviewTotal = root.Q<Label>("LblPreviewTotal");
        lblFormError = root.Q<Label>("LblFormError");
        btnDeposit = root.Q<Button>("BtnDeposit");

        // Active
        activeDeposit = root.Q<VisualElement>("ActiveDeposit");
        lblActivePkg = root.Q<Label>("LblActivePkg");
        lblActivePrincipal = root.Q<Label>("LblActivePrincipal");
        lblActiveInterest = root.Q<Label>("LblActiveInterest");
        lblCountdown = root.Q<Label>("LblCountdown");

        // History
        historyList = root.Q<VisualElement>("HistoryList");
    }

    private void RegisterCallbacks()
    {
        btnClose?.RegisterCallback<ClickEvent>(evt => Hide());
        overlay?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == overlay) Hide();
        });

        // Tabs
        tabDeposit?.RegisterCallback<ClickEvent>(evt => SwitchTab(0));
        tabHistory?.RegisterCallback<ClickEvent>(evt => SwitchTab(1));

        // Package selection
        RegisterPkgClick(pkg12, 0);
        RegisterPkgClick(pkg30, 1);
        RegisterPkgClick(pkg180, 2);

        // Amount change
        txtAmount?.RegisterValueChangedCallback(evt => UpdatePreview());

        // Deposit
        btnDeposit?.RegisterCallback<ClickEvent>(evt => OnDeposit());
    }

    private void RegisterPkgClick(VisualElement pkg, int index)
    {
        if (pkg == null) return;
        pkg.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            SelectPackage(index);
        });
    }

    // ── Public API ──

    public void Show()
    {
        if (overlay == null) return;

        selectedPackageIndex = 0;
        SwitchTab(0);
        SelectPackage(0);
        UpdateBalance();
        ClearError();

        if (txtAmount != null) txtAmount.value = "100";
        UpdatePreview();

        RefreshDepositView();
        RefreshHistory();

        overlay.style.display = DisplayStyle.Flex;
        UIPopupTracker.SetOpen(this, true); // trả chuột để bấm nút gửi tiết kiệm
        Debug.Log("[PiggyBank] Opened Heo Đất");
    }

    public void Hide()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
            UIPopupTracker.SetOpen(this, false);
            Debug.Log("[PiggyBank] Closed Heo Đất");
        }
    }

    // An toàn: nếu popup bị tắt/destroy lúc đang mở mà chưa kịp Hide() -> gỡ tracker
    // để chuột không bị kẹt.
    private void OnDisable()
    {
        UIPopupTracker.SetOpen(this, false);
    }

    public bool IsVisible()
    {
        return overlay != null && overlay.style.display == DisplayStyle.Flex;
    }

    // ── Tabs ──

    private void SwitchTab(int tabIndex)
    {
        if (tabDeposit != null)
        {
            tabDeposit.RemoveFromClassList("piggy-tab--active");
            if (tabIndex == 0) tabDeposit.AddToClassList("piggy-tab--active");
        }
        if (tabHistory != null)
        {
            tabHistory.RemoveFromClassList("piggy-tab--active");
            if (tabIndex == 1) tabHistory.AddToClassList("piggy-tab--active");
        }

        if (panelDeposit != null)
            panelDeposit.style.display = tabIndex == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        if (panelHistory != null)
            panelHistory.style.display = tabIndex == 1 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ── Package Selection ──

    private void SelectPackage(int index)
    {
        if (hasActiveDeposit) return; // can't change while active

        selectedPackageIndex = index;

        // Update card styles
        UpdatePkgStyle(pkg12, 0);
        UpdatePkgStyle(pkg30, 1);
        UpdatePkgStyle(pkg180, 2);

        // Update form summary
        var pkg = packages[index];
        if (lblFormPkg != null)
            lblFormPkg.text = $"Gói {pkg.label} • Lãi +{(pkg.rate * 100):0}%";

        UpdatePreview();
    }

    private void UpdatePkgStyle(VisualElement pkgShadow, int index)
    {
        if (pkgShadow == null) return;
        var card = pkgShadow.Q(className: "piggy-pkg");
        if (card == null) return;

        if (index == selectedPackageIndex)
            card.AddToClassList("piggy-pkg--selected");
        else
            card.RemoveFromClassList("piggy-pkg--selected");
    }

    // ── Preview ──

    private void UpdatePreview()
    {
        float amount = ParseAmount();
        var pkg = packages[selectedPackageIndex];
        float interest = amount * pkg.rate;
        float total = amount + interest;

        if (lblPreviewPrincipal != null)
            lblPreviewPrincipal.text = $"{amount:N0} POS";
        if (lblPreviewInterest != null)
            lblPreviewInterest.text = $"+{interest:N0} POS";
        if (lblPreviewTotal != null)
            lblPreviewTotal.text = $"{total:N0} POS";
    }

    private float ParseAmount()
    {
        if (txtAmount == null) return 0;
        if (float.TryParse(txtAmount.value, out float result))
            return Mathf.Max(0, result);
        return 0;
    }

    // ── Deposit ──

    private void OnDeposit()
    {
        if (hasActiveDeposit)
        {
            ShowError("Đã có gói đang gửi! Không thể gửi thêm.");
            return;
        }

        float amount = ParseAmount();
        if (amount <= 0)
        {
            ShowError("Vui lòng nhập số tiền hợp lệ.");
            return;
        }
        if (amount > playerBalance)
        {
            ShowError($"Số dư không đủ! Hiện có: {playerBalance:N0} POS");
            return;
        }

        // Execute deposit
        var pkg = packages[selectedPackageIndex];
        activePkgIndex = selectedPackageIndex;
        activePrincipal = amount;
        activeInterestAmount = amount * pkg.rate;

        // For testing: use seconds instead of days (1 day = 5 seconds in test mode)
        float testSeconds = pkg.days * 5f; // 12d=60s, 30d=150s, 180d=900s
        activeMaturityDate = DateTime.Now.AddSeconds(testSeconds);

        playerBalance -= amount;
        hasActiveDeposit = true;

        UpdateBalance();
        ClearError();
        RefreshDepositView();

        Debug.Log($"[PiggyBank] Gửi {amount:N0} POS vào gói {pkg.label} (+{pkg.rate * 100}%). Đáo hạn test: {testSeconds}s");
    }

    private void RefreshDepositView()
    {
        if (depositForm != null)
            depositForm.style.display = hasActiveDeposit ? DisplayStyle.None : DisplayStyle.Flex;
        if (activeDeposit != null)
            activeDeposit.style.display = hasActiveDeposit ? DisplayStyle.Flex : DisplayStyle.None;

        if (hasActiveDeposit)
        {
            var pkg = packages[activePkgIndex];

            if (lblActivePkg != null)
                lblActivePkg.text = $"{pkg.label} (+{pkg.rate * 100:0}%)";
            if (lblActivePrincipal != null)
                lblActivePrincipal.text = $"{activePrincipal:N0} POS";
            if (lblActiveInterest != null)
                lblActiveInterest.text = $"+{activeInterestAmount:N0} POS";

            // Start countdown
            if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
            countdownCoroutine = StartCoroutine(CountdownRoutine());
        }
    }

    // ── Countdown ──

    private IEnumerator CountdownRoutine()
    {
        while (DateTime.Now < activeMaturityDate)
        {
            TimeSpan remaining = activeMaturityDate - DateTime.Now;

            string text;
            if (remaining.TotalDays >= 1)
                text = $"{(int)remaining.TotalDays}d {remaining.Hours}h {remaining.Minutes}m";
            else if (remaining.TotalHours >= 1)
                text = $"{(int)remaining.TotalHours}h {remaining.Minutes}m {remaining.Seconds}s";
            else
                text = $"{remaining.Minutes}m {remaining.Seconds}s";

            if (lblCountdown != null)
                lblCountdown.text = text;

            yield return new WaitForSeconds(1f);
        }

        // Matured!
        OnMatured();
    }

    private void OnMatured()
    {
        float total = activePrincipal + activeInterestAmount;
        playerBalance += total;

        // Add to history
        var pkg = packages[activePkgIndex];
        historyEntries.Insert(0, new HistoryEntry
        {
            pkgName = $"Gói {pkg.label} (+{pkg.rate * 100:0}%)",
            principal = activePrincipal,
            interest = activeInterestAmount,
            status = "Đã nhận",
            date = DateTime.Now.ToString("dd/MM/yyyy")
        });

        hasActiveDeposit = false;
        countdownCoroutine = null;

        UpdateBalance();
        RefreshDepositView();
        RefreshHistory();

        Debug.Log($"[PiggyBank] 🎉 ĐÁO HẠN! Nhận {total:N0} POS (gốc {activePrincipal:N0} + lãi {activeInterestAmount:N0}). Đã gửi về hộp thư.");
    }

    // ── History ──

    private void RefreshHistory()
    {
        if (historyList == null) return;
        historyList.Clear();

        if (historyEntries.Count == 0)
        {
            var empty = new Label("Chưa có giao dịch nào...");
            empty.AddToClassList("piggy-history-empty");
            historyList.Add(empty);
            return;
        }

        foreach (var entry in historyEntries)
        {
            var row = new VisualElement();
            row.AddToClassList("piggy-history-row");

            var icon = new VisualElement();
            icon.AddToClassList("piggy-history-icon");
            icon.AddToClassList("piggy-piggy-icon");

            var info = new VisualElement();
            info.AddToClassList("piggy-history-info");

            var pkgName = new Label(entry.pkgName);
            pkgName.AddToClassList("piggy-history-pkg-name");

            var detail = new Label($"Gốc: {entry.principal:N0} POS • {entry.date}");
            detail.AddToClassList("piggy-history-detail");

            info.Add(pkgName);
            info.Add(detail);

            var result = new VisualElement();
            result.AddToClassList("piggy-history-result");

            var amount = new Label($"+{entry.interest:N0} POS");
            amount.AddToClassList("piggy-history-amount");

            var status = new Label(entry.status);
            status.AddToClassList("piggy-history-status");

            result.Add(amount);
            result.Add(status);

            row.Add(icon);
            row.Add(info);
            row.Add(result);

            historyList.Add(row);
        }
    }

    // ── Helpers ──

    private void UpdateBalance()
    {
        if (lblBalance != null)
            lblBalance.text = $"{playerBalance:N0} POS";
    }

    private void ShowError(string msg)
    {
        if (lblFormError != null)
            lblFormError.text = msg;
    }

    private void ClearError()
    {
        if (lblFormError != null)
            lblFormError.text = "";
    }
}
