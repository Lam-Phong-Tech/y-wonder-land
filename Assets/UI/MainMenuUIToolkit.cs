using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuUIToolkit : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement maleCard;
    private VisualElement femaleCard;
    private Button playButton;

    // USS class names
    private const string SELECTED_CLASS = "charselect-card-selected";

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

        // 4. Set default selection (Male)
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
}
