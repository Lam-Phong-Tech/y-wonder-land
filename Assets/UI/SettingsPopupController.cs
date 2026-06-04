using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Controller for the Settings Popup v3.
/// Horizontal layout: Audio+Camera (left), Graphics+General (right).
/// </summary>
public class SettingsPopupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument settingsDocument;
    [SerializeField] private ConfirmDialogController confirmDialog;

    // UI Elements
    private VisualElement overlay;
    private Button btnClose;

    // Localization Labels
    private Label lblSettingsTitle;
    private Label lblAudioSection;
    private Label lblMusicVolume;
    private Label lblSFXVolume;
    private Label lblCameraSection;
    private Label lblCameraSens;
    private Label lblCameraZoom;
    private Label lblGraphicsSection;
    private Label lblQuality;
    private Label lblShadow;
    private Label lblGeneralSection;
    private Label lblLanguage;

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

        // Query localization labels
        lblSettingsTitle = root.Q<Label>("LblSettingsTitle");
        lblAudioSection = root.Q<Label>("LblAudioSection");
        lblMusicVolume = root.Q<Label>("LblMusicVolume");
        lblSFXVolume = root.Q<Label>("LblSFXVolume");
        lblCameraSection = root.Q<Label>("LblCameraSection");
        lblCameraSens = root.Q<Label>("LblCameraSens");
        lblCameraZoom = root.Q<Label>("LblCameraZoom");
        lblGraphicsSection = root.Q<Label>("LblGraphicsSection");
        lblQuality = root.Q<Label>("LblQuality");
        lblShadow = root.Q<Label>("LblShadow");
        lblGeneralSection = root.Q<Label>("LblGeneralSection");
        lblLanguage = root.Q<Label>("LblLanguage");

        // Subscribe to locale change event
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        // Set initial values
        SetInitialValues();

        // Register all callbacks
        RegisterCallbacks();

        // Initial localization update
        UpdateLocalizedTexts();

        // Start hidden
        Hide();
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void SetInitialValues()
    {
        if (sliderMusic != null) sliderMusic.value = musicVolume * 100f;
        if (sliderSFX != null) sliderSFX.value = sfxVolume * 100f;
        if (sliderCameraSens != null) sliderCameraSens.value = cameraSensitivity * 100f;
        if (sliderCameraZoom != null) sliderCameraZoom.value = 50f + cameraZoom * 50f;
        if (sliderRenderQuality != null) sliderRenderQuality.value = renderQuality * 100f;
        if (toggleShadow != null) toggleShadow.value = shadowEnabled;

        UpdateDropdownLanguageValue();
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
            UpdateShadowStatusLabel();
            Debug.Log($"[Settings] Shadow: {(shadowEnabled ? "ON" : "OFF")}");
            // TODO: QualitySettings.shadows = shadowEnabled ? ShadowQuality.All : ShadowQuality.Disable;
        });

        // ── General ──
        dropdownLanguage?.RegisterValueChangedCallback(evt =>
        {
            currentLanguage = evt.newValue == "Tiếng Việt" ? "vi" : "en";
            Debug.Log($"[Settings] Language change requested to: {currentLanguage}");

            var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(currentLanguage));
            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
            }
            else
            {
                Debug.LogWarning($"[Settings] Locale '{currentLanguage}' not found in AvailableLocales!");
            }
        });

        // ── Bottom Buttons (tích hợp Confirm Dialog) ──
        btnDeleteAccount?.RegisterCallback<ClickEvent>(evt =>
        {
            if (confirmDialog != null)
            {
                confirmDialog.Show(
                    "XÓA TÀI KHOẢN",
                    "Hành động này sẽ xóa vĩnh viễn tài khoản của bạn. Tất cả dữ liệu sẽ bị mất và không thể khôi phục.",
                    "Xóa vĩnh viễn",
                    "Giữ tài khoản",
                    OnDeleteAccountConfirmed,
                    ConfirmDialogType.Danger
                );
            }
            else
            {
                Debug.LogWarning("[Settings] ConfirmDialogController chưa được gán! Kéo thả vào Inspector.");
            }
        });

        btnExitApp?.RegisterCallback<ClickEvent>(evt =>
        {
            if (confirmDialog != null)
            {
                confirmDialog.Show(
                    "THOÁT GAME",
                    "Bạn có chắc chắn muốn thoát game? Tiến trình đã được lưu tự động.",
                    "Thoát",
                    "Ở lại",
                    OnExitGameConfirmed,
                    ConfirmDialogType.Warning
                );
            }
            else
            {
                Debug.LogWarning("[Settings] ConfirmDialogController chưa được gán! Kéo thả vào Inspector.");
            }
        });
    }

    // ── Confirm Dialog Callbacks ──

    private void OnDeleteAccountConfirmed()
    {
        Debug.Log("[Settings] ✅ Xác nhận XÓA TÀI KHOẢN — đang thực hiện...");
        // TODO: Gọi UGS Authentication để xóa tài khoản
        // AuthenticationService.Instance.DeleteAccountAsync();
        Hide();
    }

    private void OnExitGameConfirmed()
    {
        Debug.Log("[Settings] ✅ Xác nhận THOÁT GAME — đang thoát...");
        // TODO: Save settings trước khi thoát
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
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

    // ── Localization Handlers ──

    private void OnLocaleChanged(Locale locale)
    {
        UpdateDropdownLanguageValue();
        UpdateLocalizedTexts();
    }

    private void UpdateDropdownLanguageValue()
    {
        if (dropdownLanguage == null) return;

        var activeLocale = LocalizationSettings.SelectedLocale;
        if (activeLocale != null)
        {
            string code = activeLocale.Identifier.Code;
            dropdownLanguage.value = code == "vi" ? "Tiếng Việt" : "English";
            currentLanguage = code;
        }
    }

    private void UpdateLocalizedTexts()
    {
        var table = LocalizationSettings.StringDatabase.GetTable("GameUI");
        if (table != null)
        {
            if (lblSettingsTitle != null) lblSettingsTitle.text = GetLocalizedString(table, "settings_title", "CÀI ĐẶT");
            if (lblAudioSection != null) lblAudioSection.text = GetLocalizedString(table, "audio_title", "ÂM THANH");
            if (lblMusicVolume != null) lblMusicVolume.text = GetLocalizedString(table, "music_volume", "Âm nhạc nền");
            if (lblSFXVolume != null) lblSFXVolume.text = GetLocalizedString(table, "sfx_volume", "Hiệu ứng");
            if (lblCameraSection != null) lblCameraSection.text = GetLocalizedString(table, "camera_title", "CAMERA");
            if (lblCameraSens != null) lblCameraSens.text = GetLocalizedString(table, "camera_sens", "Độ nhạy");
            if (lblCameraZoom != null) lblCameraZoom.text = GetLocalizedString(table, "camera_zoom", "Zoom");
            if (lblGraphicsSection != null) lblGraphicsSection.text = GetLocalizedString(table, "graphics_title", "ĐỒ HỌA");
            if (lblQuality != null) lblQuality.text = GetLocalizedString(table, "quality", "Chất lượng");
            if (lblShadow != null) lblShadow.text = GetLocalizedString(table, "shadow", "Bóng đổ");
            if (lblGeneralSection != null) lblGeneralSection.text = GetLocalizedString(table, "general_title", "CHUNG");
            if (lblLanguage != null) lblLanguage.text = GetLocalizedString(table, "language", "Ngôn ngữ");

            // Bottom buttons
            if (btnDeleteAccount != null) btnDeleteAccount.text = GetLocalizedString(table, "delete_account", "Xóa tài khoản");
            if (btnExitApp != null) btnExitApp.text = GetLocalizedString(table, "exit_game", "Thoát game");
        }
        else
        {
            LoadLocalizedTextsAsync();
        }

        UpdateShadowStatusLabel();
    }

    private string GetLocalizedString(UnityEngine.Localization.Tables.StringTable table, string key, string defaultValue)
    {
        var entry = table.GetEntry(key);
        return entry != null ? entry.LocalizedValue : defaultValue;
    }

    private async void LoadLocalizedTextsAsync()
    {
        // Wait until initialization completes
        await LocalizationSettings.InitializationOperation.Task;
        var table = await LocalizationSettings.StringDatabase.GetTableAsync("GameUI").Task;
        if (table != null)
        {
            UpdateLocalizedTexts();
        }
    }

    private void UpdateShadowStatusLabel()
    {
        if (lblShadowStatus == null) return;

        var table = LocalizationSettings.StringDatabase.GetTable("GameUI");
        string onStr = "BẬT";
        string offStr = "TẮT";

        if (table != null)
        {
            onStr = GetLocalizedString(table, "shadow_on", "BẬT");
            offStr = GetLocalizedString(table, "shadow_off", "TẮT");
        }

        lblShadowStatus.text = shadowEnabled ? onStr : offStr;
    }
}
