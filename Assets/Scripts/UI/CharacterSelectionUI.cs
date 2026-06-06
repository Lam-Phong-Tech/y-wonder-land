using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class CharacterSelectionUI : MonoBehaviour
{
    public static string PlayerName { get; private set; }

    [Header("UI Buttons")]
    public Button maleButton;
    public Button femaleButton;
    public Button playButton;

    [Header("Name Input")]
    public InputField nameInputField;
    public Text nameErrorText;

    [Header("Confirm Dialog")]
    public GameObject confirmDialog;
    public Text confirmDialogText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("Selection Styling")]
    public Color selectedColor = Color.green;
    public Color normalColor = Color.white;

    private bool isNameValid = false;

    void Start()
    {
        // Add listeners to buttons
        if (maleButton != null) maleButton.onClick.AddListener(OnSelectMale);
        if (femaleButton != null) femaleButton.onClick.AddListener(OnSelectFemale);
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);

        // Name input validation
        if (nameInputField != null)
        {
            nameInputField.onValueChanged.AddListener(OnNameChanged);
            nameInputField.characterLimit = 16;
        }

        // Confirm dialog buttons
        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(OnConfirmYes);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(OnConfirmNo);

        // Hide confirm dialog and error text at start
        if (confirmDialog != null) confirmDialog.SetActive(false);
        if (nameErrorText != null) nameErrorText.text = "";

        // Highlight male by default
        HighlightSelection(0);

        // Disable play button until name is valid
        UpdatePlayButtonState();
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

    void OnPlayClicked()
    {
        if (!ValidateName()) return;

        // Show confirm dialog instead of starting game directly
        if (confirmDialog != null)
        {
            if (confirmDialogText != null)
            {
                confirmDialogText.text = "BẠN KHÔNG THỂ THAY ĐỔI TÊN VÀ GIỚI TÍNH SAU KHI XÁC NHẬN. Bạn chắc chắn muốn tiếp tục?";
            }
            confirmDialog.SetActive(true);
        }
    }

    private void OnConfirmYes()
    {
        // Store player name
        PlayerName = nameInputField != null ? nameInputField.text.Trim() : "Player";

        if (confirmDialog != null) confirmDialog.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }

    private void OnConfirmNo()
    {
        if (confirmDialog != null) confirmDialog.SetActive(false);
    }

    private void OnNameChanged(string value)
    {
        ValidateName();
        UpdatePlayButtonState();
    }

    private bool ValidateName()
    {
        if (nameInputField == null)
        {
            isNameValid = false;
            UpdatePlayButtonState();
            return false;
        }

        string name = nameInputField.text.Trim();

        if (name.Length < 2)
        {
            ShowNameError("Tên phải có ít nhất 2 ký tự");
            isNameValid = false;
            UpdatePlayButtonState();
            return false;
        }

        if (name.Length > 16)
        {
            ShowNameError("Tên không được vượt quá 16 ký tự");
            isNameValid = false;
            UpdatePlayButtonState();
            return false;
        }

        if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_]+$"))
        {
            ShowNameError("Tên chỉ được chứa chữ cái, số và dấu gạch dưới");
            isNameValid = false;
            UpdatePlayButtonState();
            return false;
        }

        // Valid
        if (nameErrorText != null) nameErrorText.text = "";
        isNameValid = true;
        UpdatePlayButtonState();
        return true;
    }

    private void ShowNameError(string message)
    {
        if (nameErrorText != null) nameErrorText.text = message;
    }

    private void UpdatePlayButtonState()
    {
        if (playButton != null) playButton.interactable = isNameValid;
    }

    private void HighlightSelection(int index)
    {
        // Change colors of selection buttons to indicate choice
        if (maleButton != null)
        {
            ColorBlock cb = maleButton.colors;
            cb.normalColor = (index == 0) ? selectedColor : normalColor;
            cb.selectedColor = (index == 0) ? selectedColor : normalColor;
            maleButton.colors = cb;
        }

        if (femaleButton != null)
        {
            ColorBlock cb = femaleButton.colors;
            cb.normalColor = (index == 1) ? selectedColor : normalColor;
            cb.selectedColor = (index == 1) ? selectedColor : normalColor;
            femaleButton.colors = cb;
        }
    }
}
