using UnityEngine;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;

/// <summary>
/// UI Toolkit controller for Character Selection screen.
/// Handles gender selection, name input validation, and confirm dialog.
/// Vietnamese text is set from code (not UXML) to ensure proper diacritics.
/// </summary>
public class CharacterSelectController : MonoBehaviour
{
    /// <summary>
    /// Static property to access the chosen player name from any script.
    /// </summary>
    public static string PlayerName { get; private set; }

    /// <summary>
    /// Static property to access the chosen gender (0 = Male, 1 = Female) for default avatar.
    /// </summary>
    public static int PlayerGender { get; private set; }

    private UIDocument uiDocument;

    // Root screen
    private VisualElement screenRoot;

    // Gender cards
    private VisualElement maleCard;
    private VisualElement femaleCard;

    // Name input
    private VisualElement nameInputGroup;
    private TextField nameField;
    private Label nameStatus;

    // Start button
    private Button btnStart;

    // Confirm dialog
    private VisualElement confirmOverlay;
    private Button btnConfirmYes;
    private Button btnConfirmNo;

    // State
    private int selectedGender = 0; // 0 = Male, 1 = Female
    private bool isNameValid = false;

    // USS class names
    private const string SELECTED_CLASS = "charselect-card-selected";
    private const string HIDDEN_CLASS = "hidden";
    private const string INPUT_FOCUS_CLASS = "charselect-input-group-focus";
    private const string STATUS_SUCCESS = "status-success";
    private const string STATUS_ERROR = "status-error";

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("[CharacterSelect] UIDocument component not found!");
            return;
        }

        var root = uiDocument.rootVisualElement;
        QueryElements(root);
        SetVietnameseText(root);
        RegisterCallbacks();

        // Default state: hidden, male selected, button disabled
        SelectGender(0);
        UpdateStartButtonState();
    }

    private void QueryElements(VisualElement root)
    {
        screenRoot = root.Q<VisualElement>("CharacterSelectScreen");

        maleCard = root.Q<VisualElement>("MaleCard");
        femaleCard = root.Q<VisualElement>("FemaleCard");

        nameInputGroup = root.Q<VisualElement>("NameInputGroup");
        nameField = root.Q<TextField>("NameField");
        nameStatus = root.Q<Label>("NameStatus");

        btnStart = root.Q<Button>("BtnStart");

        confirmOverlay = root.Q<VisualElement>("ConfirmOverlay");
        btnConfirmYes = root.Q<Button>("BtnConfirmYes");
        btnConfirmNo = root.Q<Button>("BtnConfirmNo");
    }

    /// <summary>
    /// Set all Vietnamese text from C# code to ensure proper diacritics display.
    /// UXML uses placeholder dots to avoid encoding issues.
    /// </summary>
    private void SetVietnameseText(VisualElement root)
    {
        SetLabelText(root, "TitleLabel", "T\u1ea0O NH\u00c2N V\u1eacT");
        SetLabelText(root, "SubtitleLabel", "Ch\u1ecdn gi\u1edbi t\u00ednh v\u00e0 \u0111\u1eb7t t\u00ean cho nh\u00e2n v\u1eadt c\u1ee7a b\u1ea1n");
        SetLabelText(root, "MaleLabel", "NAM");
        SetLabelText(root, "FemaleLabel", "N\u1eee");
        SetLabelText(root, "InputTitle", "T\u00caN NH\u00c2N V\u1eacT");
        SetLabelText(root, "ConfirmTitle", "C\u1ea2NH B\u00c1O");
        SetLabelText(root, "ConfirmBody",
            "B\u1ea0N KH\u00d4NG TH\u1ec2 THAY \u0110\u1ed4I T\u00caN V\u00c0 GI\u1edaI T\u00cdNH SAU KHI X\u00c1C NH\u1eacN.\nB\u1ea1n ch\u1eafc ch\u1eafn mu\u1ed1n ti\u1ebfp t\u1ee5c?");

        // Gender icons: replaced by background images in UXML/USS

        // Buttons
        var btnStart = root.Q<Button>("BtnStart");
        if (btnStart != null) btnStart.text = "B\u1eaeT \u0110\u1ea6U PHI\u00caU L\u01afU";

        var btnNo = root.Q<Button>("BtnConfirmNo");
        if (btnNo != null) btnNo.text = "Quay l\u1ea1i";

        var btnYes = root.Q<Button>("BtnConfirmYes");
        if (btnYes != null) btnYes.text = "X\u00e1c nh\u1eadn";

        // Placeholder
        if (nameField != null)
        {
            nameField.textEdition.placeholder = "Nh\u1eadp t\u00ean nh\u00e2n v\u1eadt (2-20 k\u00fd t\u1ef1)";
        }
    }

    private void SetLabelText(VisualElement root, string name, string text)
    {
        Label label = root.Q<Label>(name);
        if (label != null) label.text = text;
    }

    private void RegisterCallbacks()
    {
        // Gender selection
        maleCard?.RegisterCallback<ClickEvent>(evt => SelectGender(0));
        femaleCard?.RegisterCallback<ClickEvent>(evt => SelectGender(1));

        // Name input validation
        nameField?.RegisterValueChangedCallback(evt => OnNameChanged(evt.newValue));

        // Focus styling for input group
        if (nameField != null && nameInputGroup != null)
        {
            nameField.RegisterCallback<FocusInEvent>(evt => nameInputGroup.AddToClassList(INPUT_FOCUS_CLASS));
            nameField.RegisterCallback<FocusOutEvent>(evt => nameInputGroup.RemoveFromClassList(INPUT_FOCUS_CLASS));
        }

        // Start button
        btnStart?.RegisterCallback<ClickEvent>(evt => OnStartClicked());

        // Confirm dialog
        btnConfirmYes?.RegisterCallback<ClickEvent>(evt => OnConfirmYes());
        btnConfirmNo?.RegisterCallback<ClickEvent>(evt => OnConfirmNo());
    }

    // ── Show / Hide ──

    /// <summary>
    /// Show the character selection screen.
    /// Called by GameManager when entering Menu state.
    /// </summary>
    public void Show()
    {
        if (screenRoot != null)
        {
            screenRoot.RemoveFromClassList(HIDDEN_CLASS);
        }
        Debug.Log("[CharacterSelect] Screen shown.");
    }

    /// <summary>
    /// Hide the character selection screen.
    /// Called by GameManager when leaving Menu state.
    /// </summary>
    public void Hide()
    {
        if (screenRoot != null)
        {
            screenRoot.AddToClassList(HIDDEN_CLASS);
        }
        Debug.Log("[CharacterSelect] Screen hidden.");
    }

    // ── Gender Selection ──

    private void SelectGender(int gender)
    {
        selectedGender = gender;

        // Update card styles
        if (gender == 0)
        {
            maleCard?.AddToClassList(SELECTED_CLASS);
            femaleCard?.RemoveFromClassList(SELECTED_CLASS);
        }
        else
        {
            femaleCard?.AddToClassList(SELECTED_CLASS);
            maleCard?.RemoveFromClassList(SELECTED_CLASS);
        }

        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SelectCharacter(gender);
        }

        Debug.Log($"[CharacterSelect] Selected gender: {(gender == 0 ? "Male" : "Female")}");
    }

    // ── Name Validation ──

    private void OnNameChanged(string value)
    {
        ValidateName(value);
        UpdateStartButtonState();
    }

    private bool ValidateName(string name)
    {
        name = name?.Trim() ?? "";

        if (string.IsNullOrEmpty(name))
        {
            ClearStatus();
            isNameValid = false;
            return false;
        }

        if (name.Length < 2)
        {
            ShowStatus("T\u00ean ph\u1ea3i c\u00f3 \u00edt nh\u1ea5t 2 k\u00fd t\u1ef1", false);
            isNameValid = false;
            return false;
        }

        if (name.Length > 20)
        {
            ShowStatus("T\u00ean kh\u00f4ng \u0111\u01b0\u1ee3c v\u01b0\u1ee3t qu\u00e1 20 k\u00fd t\u1ef1", false);
            isNameValid = false;
            return false;
        }

        if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_]+$"))
        {
            ShowStatus("T\u00ean ch\u1ec9 \u0111\u01b0\u1ee3c ch\u1ee9a ch\u1eef c\u00e1i, s\u1ed1 v\u00e0 d\u1ea5u g\u1ea1ch d\u01b0\u1edbi", false);
            isNameValid = false;
            return false;
        }

        // Valid!
        ShowStatus("T\u00ean h\u1ee3p l\u1ec7!", true);
        isNameValid = true;
        return true;
    }

    private void ShowStatus(string message, bool isSuccess)
    {
        if (nameStatus == null) return;

        nameStatus.text = message;
        nameStatus.RemoveFromClassList(STATUS_SUCCESS);
        nameStatus.RemoveFromClassList(STATUS_ERROR);
        nameStatus.AddToClassList(isSuccess ? STATUS_SUCCESS : STATUS_ERROR);
    }

    private void ClearStatus()
    {
        if (nameStatus == null) return;

        nameStatus.text = "";
        nameStatus.RemoveFromClassList(STATUS_SUCCESS);
        nameStatus.RemoveFromClassList(STATUS_ERROR);
        nameStatus.style.display = DisplayStyle.None;
    }

    private void UpdateStartButtonState()
    {
        if (btnStart != null)
        {
            btnStart.SetEnabled(isNameValid);
        }
    }

    // ── Start / Confirm Flow ──

    private void OnStartClicked()
    {
        if (!isNameValid) return;

        // Show confirm dialog
        if (confirmOverlay != null)
        {
            confirmOverlay.RemoveFromClassList(HIDDEN_CLASS);
        }
    }

    private void OnConfirmYes()
    {
        // Store player name
        PlayerName = nameField?.value?.Trim() ?? "Player";
        PlayerGender = selectedGender;
        Debug.Log($"[CharacterSelect] Confirmed! PlayerName: {PlayerName}, Gender: {(selectedGender == 0 ? "Male" : "Female")}");

        // Hide confirm dialog
        if (confirmOverlay != null)
        {
            confirmOverlay.AddToClassList(HIDDEN_CLASS);
        }

        // Hide this entire screen
        Hide();

        // Start game via GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }

    private void OnConfirmNo()
    {
        // Hide confirm dialog
        if (confirmOverlay != null)
        {
            confirmOverlay.AddToClassList(HIDDEN_CLASS);
        }
    }
}
