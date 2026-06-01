using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Settings Popup v3.
/// Horizontal layout: Audio+Camera (left), Graphics+General (right).
/// </summary>
public class SettingsPopupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument settingsDocument;

    // UI Elements
    private VisualElement overlay;
    private Button btnClose;

    // Audio
    private Slider sliderMusic;
    private Slider sliderSFX;
    private Label lblMusicValue;
    private Label lblSFXValue;

    // Camera
    private Slider sliderCameraSens;
    private Slider sliderCameraZoom;
    private Label lblCameraSensValue;
    private Label lblCameraZoomValue;

    // Graphics
    private Slider sliderRenderQuality;
    private Label lblRenderQualityValue;
    private Toggle toggleShadow;
    private Label lblShadowStatus;

    // General
    private DropdownField dropdownLanguage;

    // Bottom buttons
    private Button btnDeleteAccount;
    private Button btnExitApp;

    // Current settings (mockup values)
    private float musicVolume = 0.8f;
    private float sfxVolume = 1.0f;
    private float cameraSensitivity = 0.5f;
    private float cameraZoom = 0.75f;
    private float renderQuality = 1.0f;
    private bool shadowEnabled = true;
    private string currentLanguage = "vi";

    private void Awake()
    {
        if (settingsDocument == null)
            settingsDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        var root = settingsDocument.rootVisualElement;

        // Query all elements
        overlay = root.Q<VisualElement>("SettingsOverlay");
        btnClose = root.Q<Button>("BtnCloseSettings");

        // Audio
        sliderMusic = root.Q<Slider>("SliderMusic");
        sliderSFX = root.Q<Slider>("SliderSFX");
        lblMusicValue = root.Q<Label>("LblMusicValue");
        lblSFXValue = root.Q<Label>("LblSFXValue");

        // Camera
        sliderCameraSens = root.Q<Slider>("SliderCameraSens");
        sliderCameraZoom = root.Q<Slider>("SliderCameraZoom");
        lblCameraSensValue = root.Q<Label>("LblCameraSensValue");
        lblCameraZoomValue = root.Q<Label>("LblCameraZoomValue");

        // Graphics
        sliderRenderQuality = root.Q<Slider>("SliderRenderQuality");
        lblRenderQualityValue = root.Q<Label>("LblRenderQualityValue");
        toggleShadow = root.Q<Toggle>("ToggleShadow");
        lblShadowStatus = root.Q<Label>("LblShadowStatus");

        // General
        dropdownLanguage = root.Q<DropdownField>("DropdownLanguage");

        // Bottom buttons
        btnDeleteAccount = root.Q<Button>("BtnDeleteAccount");
        btnExitApp = root.Q<Button>("BtnExitApp");

        // Set initial values
        SetInitialValues();

        // Register all callbacks
        RegisterCallbacks();

        // Start hidden
        Hide();
    }

    private void SetInitialValues()
    {
        if (sliderMusic != null) sliderMusic.value = musicVolume * 100f;
        if (sliderSFX != null) sliderSFX.value = sfxVolume * 100f;
        if (sliderCameraSens != null) sliderCameraSens.value = cameraSensitivity * 100f;
        if (sliderCameraZoom != null) sliderCameraZoom.value = 50f + cameraZoom * 50f;
        if (sliderRenderQuality != null) sliderRenderQuality.value = renderQuality * 100f;
        if (toggleShadow != null) toggleShadow.value = shadowEnabled;
        UpdateAllLabels();
    }

    private void RegisterCallbacks()
    {
        // Close button
        btnClose?.RegisterCallback<ClickEvent>(evt =>
        {
            Hide();
        });

        // Click overlay to close
        overlay?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == overlay)
                Hide();
        });

        // ── Audio ──
        sliderMusic?.RegisterValueChangedCallback(evt =>
        {
            musicVolume = evt.newValue / 100f;
            UpdateLabel(lblMusicValue, evt.newValue);
            Debug.Log($"[Settings] Music volume: {musicVolume:F2}");
            // TODO: AudioManager.Instance.SetMusicVolume(musicVolume);
        });

        sliderSFX?.RegisterValueChangedCallback(evt =>
        {
            sfxVolume = evt.newValue / 100f;
            UpdateLabel(lblSFXValue, evt.newValue);
            Debug.Log($"[Settings] SFX volume: {sfxVolume:F2}");
            // TODO: AudioManager.Instance.SetSFXVolume(sfxVolume);
        });

        // ── Camera ──
        sliderCameraSens?.RegisterValueChangedCallback(evt =>
        {
            cameraSensitivity = evt.newValue / 100f;
            UpdateLabel(lblCameraSensValue, evt.newValue);
            Debug.Log($"[Settings] Camera sensitivity: {cameraSensitivity:F2}");
            // TODO: CameraController.Instance.SetSensitivity(cameraSensitivity);
        });

        sliderCameraZoom?.RegisterValueChangedCallback(evt =>
        {
            cameraZoom = (evt.newValue - 50f) / 50f;
            UpdateLabel(lblCameraZoomValue, evt.newValue);
            Debug.Log($"[Settings] Camera zoom: {cameraZoom:F2}");
            // TODO: CameraController.Instance.SetZoom(cameraZoom);
        });

        // ── Graphics ──
        sliderRenderQuality?.RegisterValueChangedCallback(evt =>
        {
            renderQuality = evt.newValue / 100f;
            UpdateLabel(lblRenderQualityValue, evt.newValue);
            Debug.Log($"[Settings] Render quality: {renderQuality:F2}");
            // TODO: QualitySettings.SetQualityLevel(...)
        });

        toggleShadow?.RegisterValueChangedCallback(evt =>
        {
            shadowEnabled = evt.newValue;
            if (lblShadowStatus != null) lblShadowStatus.text = shadowEnabled ? "BẬT" : "TẮT";
            Debug.Log($"[Settings] Shadow: {(shadowEnabled ? "ON" : "OFF")}");
            // TODO: QualitySettings.shadows = shadowEnabled ? ShadowQuality.All : ShadowQuality.Disable;
        });

        // ── General ──
        dropdownLanguage?.RegisterValueChangedCallback(evt =>
        {
            currentLanguage = evt.newValue == "Tiếng Việt" ? "vi" : "en";
            Debug.Log($"[Settings] Language: {currentLanguage}");
            // TODO: LocalizationManager.Instance.SetLanguage(currentLanguage);
        });

        // ── Bottom Buttons ──
        btnDeleteAccount?.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("[Settings] Delete Account clicked — show confirmation dialog");
            // TODO: Show confirmation popup before deleting
        });

        btnExitApp?.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("[Settings] Exit App clicked");
            // TODO: Save settings before quitting
            // Application.Quit();
        });
    }

    private void UpdateLabel(Label label, float value)
    {
        if (label != null) label.text = $"{Mathf.RoundToInt(value)}%";
    }

    private void UpdateAllLabels()
    {
        UpdateLabel(lblMusicValue, musicVolume * 100f);
        UpdateLabel(lblSFXValue, sfxVolume * 100f);
        UpdateLabel(lblCameraSensValue, cameraSensitivity * 100f);
        UpdateLabel(lblCameraZoomValue, 50f + cameraZoom * 50f);
        UpdateLabel(lblRenderQualityValue, renderQuality * 100f);
    }

    // ── Public API ──

    /// <summary>
    /// Show the settings popup.
    /// </summary>
    public void Show()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.Flex;
            Debug.Log("[Settings] Popup opened");
        }
    }

    /// <summary>
    /// Hide the settings popup.
    /// </summary>
    public void Hide()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
            Debug.Log("[Settings] Popup closed");
        }
    }

    /// <summary>
    /// Check if settings popup is currently visible.
    /// </summary>
    public bool IsVisible()
    {
        return overlay != null && overlay.style.display == DisplayStyle.Flex;
    }

    /// <summary>
    /// Set music volume programmatically (0.0 - 1.0).
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (sliderMusic != null) sliderMusic.value = musicVolume * 100f;
        UpdateLabel(lblMusicValue, musicVolume * 100f);
    }

    /// <summary>
    /// Set SFX volume programmatically (0.0 - 1.0).
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sliderSFX != null) sliderSFX.value = sfxVolume * 100f;
        UpdateLabel(lblSFXValue, sfxVolume * 100f);
    }
}
