using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuUIToolkit : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement maleCard;
    private VisualElement femaleCard;
    private Button playButton;

    // Name Input Elements
    private TextField nameField;
    private Label nameStatus;
    private VisualElement nameInputGroup;

    // Confirm Dialog Elements
    private VisualElement confirmOverlay;
    private Button btnCancelConfirm;
    private Button btnAcceptConfirm;

    // USS class names
    private const string SELECTED_CLASS = "charselect-card-selected";
    private const string INPUT_FOCUS_CLASS = "charselect-input-group-focus";
    private const string STATUS_SUCCESS = "status-success";
    private const string STATUS_ERROR = "status-error";
    private const string HIDDEN_CLASS = "hidden";

    void OnEnable()
    {
        // 1. Get UIDocument component
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("MainMenuUIToolkit requires a UIDocument component on the same GameObject!");
            return;
        }

        // 2. Query Visual Elements from UXML
        var root = uiDocument.rootVisualElement;
        
        maleCard = root.Q<VisualElement>("MaleCard");
        femaleCard = root.Q<VisualElement>("FemaleCard");
        playButton = root.Q<Button>("PlayButton");

        // Name input query
        nameField = root.Q<TextField>("NameField");
        nameStatus = root.Q<Label>("NameStatus");
        nameInputGroup = root.Q<VisualElement>("NameInputGroup");

        // Confirm popup query
        confirmOverlay = root.Q<VisualElement>("ConfirmOverlay");
        btnCancelConfirm = root.Q<Button>("BtnCancelConfirm");
        btnAcceptConfirm = root.Q<Button>("BtnAcceptConfirm");

        // 3. Register Callbacks (Events)
        if (maleCard != null)
        {
            maleCard.RegisterCallback<ClickEvent>(evt => OnSelectMale());
        }
        else
        {
            Debug.LogError("Could not find MaleCard in UXML!");
        }

        if (femaleCard != null)
        {
            femaleCard.RegisterCallback<ClickEvent>(evt => OnSelectFemale());
        }
        else
        {
            Debug.LogError("Could not find FemaleCard in UXML!");
        }

        if (playButton != null)
        {
            playButton.clicked += OnPlayClicked;
        }
        else
        {
            Debug.LogError("Could not find PlayButton in UXML!");
        }

        // Confirm buttons callbacks
        if (btnCancelConfirm != null)
        {
            btnCancelConfirm.clicked += OnCancelConfirmClicked;
        }
        if (btnAcceptConfirm != null)
        {
            btnAcceptConfirm.clicked += OnAcceptConfirmClicked;
        }

        // Real-time name validate feedback (optional, provides nice UX)
        nameField?.RegisterValueChangedCallback(evt => OnNameChanged());

        // Name input focus styling
        if (nameField != null && nameInputGroup != null)
        {
            nameField.RegisterCallback<FocusInEvent>(evt => nameInputGroup.AddToClassList(INPUT_FOCUS_CLASS));
            nameField.RegisterCallback<FocusOutEvent>(evt => nameInputGroup.RemoveFromClassList(INPUT_FOCUS_CLASS));
        }

        // Set placeholder
        if (nameField != null)
        {
            nameField.textEdition.placeholder = "Nhập tên của bạn...";
        }

        // 4. Set default selection (Male)
        HighlightSelection(0);
        
        // Ensure confirm popup is hidden at start
        confirmOverlay?.AddToClassList(HIDDEN_CLASS);
    }

    void OnSelectMale()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SelectCharacter(0);
            HighlightSelection(0);
        }
    }

    void OnSelectFemale()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SelectCharacter(1);
            HighlightSelection(1);
        }
    }

    private void OnNameChanged()
    {
        string errMsg;
        bool isValid = ValidateName(out errMsg);

        if (nameStatus != null)
        {
            if (!isValid && !string.IsNullOrEmpty(nameField?.value))
            {
                ShowStatus(nameStatus, errMsg, false);
            }
            else
            {
                ClearStatus(nameStatus);
            }
        }
    }

    private bool ValidateName(out string errorMessage)
    {
        errorMessage = "";
        string name = nameField?.value ?? "";
        name = name.Trim();

        if (string.IsNullOrEmpty(name))
        {
            errorMessage = "Vui lòng nhập tên nhân vật";
            return false;
        }

        if (name.Length < 2 || name.Length > 16)
        {
            errorMessage = "Tên nhân vật phải từ 2 đến 16 ký tự";
            return false;
        }

        // Only allow letters (Unicode supported for Vietnamese), digits and spaces
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z0-9\s\p{L}]+$"))
        {
            errorMessage = "Tên không được chứa các ký tự đặc biệt";
            return false;
        }

        return true;
    }

    void OnPlayClicked()
    {
        string errMsg;
        if (!ValidateName(out errMsg))
        {
            if (nameStatus != null)
            {
                ShowStatus(nameStatus, errMsg, false);
            }
            return;
        }

        ClearStatus(nameStatus);
        
        // Show confirmation popup warning
        confirmOverlay?.RemoveFromClassList(HIDDEN_CLASS);
        Debug.Log("[MainMenuUI] Verification warning overlay shown.");
    }

    private void OnCancelConfirmClicked()
    {
        confirmOverlay?.AddToClassList(HIDDEN_CLASS);
        Debug.Log("[MainMenuUI] Cancelled confirmation.");
    }

    private void OnAcceptConfirmClicked()
    {
        string name = nameField?.value ?? "";
        name = name.Trim();

        // Save character configuration in PlayerPrefs (Mock database)
        PlayerPrefs.SetString("PlayerName", name);
        if (GameManager.Instance != null)
        {
            PlayerPrefs.SetInt("PlayerGender", GameManager.Instance.selectedCharacterIndex);
            
            confirmOverlay?.AddToClassList(HIDDEN_CLASS);
            Debug.Log($"[MainMenuUI] Configuration Saved: Name = '{name}', Gender = {(GameManager.Instance.selectedCharacterIndex == 0 ? "Male" : "Female")}. Starting game...");
            
            GameManager.Instance.StartGame();
        }
    }

    private void HighlightSelection(int index)
    {
        if (maleCard == null || femaleCard == null) return;

        if (index == 0)
        {
            maleCard.AddToClassList(SELECTED_CLASS);
            femaleCard.RemoveFromClassList(SELECTED_CLASS);
        }
        else
        {
            femaleCard.AddToClassList(SELECTED_CLASS);
            maleCard.RemoveFromClassList(SELECTED_CLASS);
        }
    }

    // ── Status Message Helpers ──

    private void ShowStatus(Label label, string message, bool isSuccess)
    {
        if (label == null) return;

        label.text = message;
        label.RemoveFromClassList(STATUS_SUCCESS);
        label.RemoveFromClassList(STATUS_ERROR);
        label.AddToClassList(isSuccess ? STATUS_SUCCESS : STATUS_ERROR);
        label.style.display = DisplayStyle.Flex;
    }

    private void ClearStatus(Label label)
    {
        if (label == null) return;

        label.text = "";
        label.RemoveFromClassList(STATUS_SUCCESS);
        label.RemoveFromClassList(STATUS_ERROR);
        label.style.display = DisplayStyle.None;
    }
}
