using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Shared UI Toolkit helper for keeping focused text inputs above the mobile soft keyboard.
/// </summary>
public static class MobileKeyboardAvoidance
{
    private const float DefaultFallbackKeyboardRatio = 0.45f;

    public static bool IsTextInputFocused(VisualElement input)
    {
        return input != null &&
               input.focusController != null &&
               input.focusController.focusedElement == input;
    }

    public static bool ShouldAvoidKeyboard(VisualElement input)
    {
        if (!IsTextInputFocused(input)) return false;
        return TouchScreenKeyboard.visible || Application.isMobilePlatform;
    }

    public static float GetKeyboardHeightInPanel(VisualElement root, float fallbackScreenRatio = DefaultFallbackKeyboardRatio)
    {
        float panelHeight = GetPanelHeight(root);
        float screenHeight = Mathf.Max(1f, Screen.height);
        float keyboardScreenHeight = TouchScreenKeyboard.area.height;

        if (keyboardScreenHeight < 1f)
            keyboardScreenHeight = screenHeight * Mathf.Clamp01(fallbackScreenRatio);

        return keyboardScreenHeight / screenHeight * panelHeight;
    }

    public static float CalculateRequiredUpShift(
        VisualElement root,
        VisualElement input,
        float currentUpShift,
        float marginAboveKeyboard = 24f,
        float fallbackScreenRatio = DefaultFallbackKeyboardRatio)
    {
        if (root == null || input == null) return 0f;

        float panelHeight = GetPanelHeight(root);
        if (panelHeight <= 1f) return 0f;

        float keyboardHeight = GetKeyboardHeightInPanel(root, fallbackScreenRatio);
        float keyboardTop = panelHeight - keyboardHeight;
        float inputBottomWithoutShift = input.worldBound.yMax + Mathf.Max(0f, currentUpShift);
        float requiredShift = inputBottomWithoutShift + marginAboveKeyboard - keyboardTop;

        return Mathf.Max(0f, requiredShift);
    }

    private static float GetPanelHeight(VisualElement root)
    {
        if (root != null && root.panel != null)
        {
            float panelHeight = root.panel.visualTree.layout.height;
            if (panelHeight > 1f) return panelHeight;
        }

        return Mathf.Max(1f, Screen.height);
    }
}
