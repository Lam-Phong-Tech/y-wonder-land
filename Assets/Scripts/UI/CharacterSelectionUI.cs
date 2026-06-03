using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionUI : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button maleButton;
    public Button femaleButton;
    public Button playButton;

    [Header("Selection Styling")]
    public Color selectedColor = Color.green;
    public Color normalColor = Color.white;

    void Start()
    {
        // Add listeners to buttons
        if (maleButton != null) maleButton.onClick.AddListener(OnSelectMale);
        if (femaleButton != null) femaleButton.onClick.AddListener(OnSelectFemale);
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);

        // Highlight male by default
        HighlightSelection(0);
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
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
