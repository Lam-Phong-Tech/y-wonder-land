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
    private Label lblShowChat;

    // Audio
    private Slider sliderMusic;
    private Slider sliderSFX;
    private Label lblMusicValue;
    private Label lblSFXValue;

    // Camera
    private Slider sliderCameraSens;
    private Slider sliderCameraZoom;
    private Slider sliderUIScale;
    private Label lblUIScale;
    private Label lblUIScaleValue;
    private Label lblCameraSensValue;
    private Label lblCameraZoomValue;

    // Graphics
    private Slider sliderRenderQuality;
    private Label lblRenderQualityValue;
    private Toggle toggleShadow;
    private Label lblShadowStatus;
    private Toggle toggleShowChat;
    private Label lblShowChatStatus;
    private Toggle toggleCropLabels;      // chữ nổi trên cây/thú — mặc định TẮT, có cảnh báo giật
    private Label lblCropLabelsStatus;

    // General
    private DropdownField dropdownLanguage;

    // Bottom buttons
    private Button btnDeleteAccount;
    private Button btnExitApp;

    // Mức mặc định khi chưa có gì lưu; SetInitialValues() sẽ nạp đè bằng giá trị đã lưu.
    private float musicVolume = 0.8f;
    private float sfxVolume = 1.0f;
    private float cameraSensitivity = 0.5f;
    private float cameraZoom = 0f; // 0 = ĐÚNG mức camera trong Inspector. (Cũ để 0.75 -> lùi xa 1.65x.)
    private float uiScale = 1f;    // 1 = 100% cỡ chữ/nút gốc; khách chốt 29/07 cho kéo 50% – 200%.
    private float renderQuality = 1.0f;
    private bool shadowEnabled = true;
    private bool showChatEnabled = true;
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
        sliderUIScale = root.Q<Slider>("SliderUIScale");
        lblUIScaleValue = root.Q<Label>("LblUIScaleValue");
        lblUIScale = root.Q<Label>("LblUIScale");
        toggleShadow = root.Q<Toggle>("ToggleShadow");
        lblShadowStatus = root.Q<Label>("LblShadowStatus");
        toggleShowChat = root.Q<Toggle>("ToggleShowChat");
        lblShowChatStatus = root.Q<Label>("LblShowChatStatus");
        toggleCropLabels = root.Q<Toggle>("ToggleCropLabels");
        lblCropLabelsStatus = root.Q<Label>("LblCropLabelsStatus");

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
        lblShowChat = root.Q<Label>("LblShowChat");

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

    // ĐỘ PHÂN GIẢI dựng hình: kéo xuống thì URP render ở khung nhỏ hơn rồi phóng lên (nhẹ máy yếu),
    // kèm hạ mip texture + LOD cho đồng bộ. KHÔNG đổi độ phân giải cửa sổ (hại trên mobile).
    private static void ApplyRenderQuality(float t01)
    {
        t01 = Mathf.Clamp01(t01);

        var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline
            as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        if (urp != null)
            urp.renderScale = Mathf.Lerp(0.6f, 1f, t01); // dưới 0.6x thì vỡ hình, không cho thấp hơn

        QualitySettings.globalTextureMipmapLimit = t01 >= 0.75f ? 0 : (t01 >= 0.5f ? 1 : 2);
        QualitySettings.lodBias = Mathf.Lerp(0.6f, 1.2f, t01);
    }

    private static void ApplyShadow(bool on)
    {
        QualitySettings.shadows = on ? ShadowQuality.All : ShadowQuality.Disable;
        QualitySettings.shadowDistance = on ? 50f : 0f;
    }

    private void SetInitialValues()
    {
        // Nạp lại các mức đã lưu để thanh trượt khớp với trạng thái thật của game.
        var audio = YWonderLand.Managers.AudioManager.Instance;
        if (audio != null) { musicVolume = audio.MusicVolume; sfxVolume = audio.SfxVolume; }
        cameraZoom = PlayerPrefs.GetFloat("YW_CamZoom", cameraZoom);
        renderQuality = PlayerPrefs.GetFloat("YW_RenderQuality", renderQuality);
        shadowEnabled = PlayerPrefs.GetInt("YW_Shadow", shadowEnabled ? 1 : 0) == 1;
        cameraSensitivity = PlayerPrefs.GetFloat("YW_CamSensitivity", cameraSensitivity); // nạp độ nhạy đã lưu
        uiScale = PlayerPrefs.GetFloat(UIScaleManager.PrefKey, uiScale);
        if (sliderMusic != null) sliderMusic.value = musicVolume * 100f;
        if (sliderSFX != null) sliderSFX.value = sfxVolume * 100f;
        if (sliderCameraSens != null) sliderCameraSens.value = cameraSensitivity * 100f;
        if (sliderCameraZoom != null) sliderCameraZoom.value = CameraZoomToPercent(cameraZoom);
        if (sliderUIScale != null) sliderUIScale.value = uiScale * 100f;
        if (sliderRenderQuality != null) sliderRenderQuality.value = renderQuality * 100f;
        if (toggleShadow != null) toggleShadow.value = shadowEnabled;
        if (toggleShowChat != null) toggleShowChat.value = showChatEnabled;
        if (toggleCropLabels != null) toggleCropLabels.value = YWonderLand.Environment.FarmLabelVisibility.Show;
        UpdateCropLabelsStatusLabel();

        // ÁP các mức đã lưu vào game luôn — không chỉ dựng lại thanh trượt.
        ApplyRenderQuality(renderQuality);
        ApplyShadow(shadowEnabled);
        UIScaleManager.Apply(uiScale);
        if (ThirdPersonCamera.Instance != null)
        {
            ThirdPersonCamera.Instance.SetUserSensitivity(cameraSensitivity);
            ThirdPersonCamera.Instance.SetUserZoom(cameraZoom);
        }

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
            // AudioManager tự lưu vào PlayerPrefs.
            YWonderLand.Managers.AudioManager.Instance?.SetMusicVolume(musicVolume);
        });

        sliderSFX?.RegisterValueChangedCallback(evt =>
        {
            sfxVolume = evt.newValue / 100f;
            UpdateLabel(lblSFXValue, evt.newValue);
            var audio = YWonderLand.Managers.AudioManager.Instance;
            if (audio != null)
            {
                audio.SetSFXVolume(sfxVolume);
                audio.PlaySFX("click"); // nghe thử ngay mức vừa chỉnh
            }
        });

        // ── Camera ──
        sliderCameraSens?.RegisterValueChangedCallback(evt =>
        {
            cameraSensitivity = evt.newValue / 100f;
            UpdateLabel(lblCameraSensValue, evt.newValue);
            PlayerPrefs.SetFloat("YW_CamSensitivity", cameraSensitivity); // lưu lại
            if (ThirdPersonCamera.Instance != null)
                ThirdPersonCamera.Instance.SetUserSensitivity(cameraSensitivity); // nối camera (cả chuột PC + chạm mobile)
        });

        sliderCameraZoom?.RegisterValueChangedCallback(evt =>
        {
            cameraZoom = PercentToCameraZoom(evt.newValue);
            UpdateLabel(lblCameraZoomValue, evt.newValue);
            PlayerPrefs.SetFloat("YW_CamZoom", cameraZoom);
            if (ThirdPersonCamera.Instance != null)
                ThirdPersonCamera.Instance.SetUserZoom(cameraZoom);
        });

        // Cỡ chữ & nút (UI Toolkit): 50% – 200%. Khách chốt 29/07 để người lớn tuổi nhìn rõ.
        sliderUIScale?.RegisterValueChangedCallback(evt =>
        {
            uiScale = Mathf.Clamp(evt.newValue / 100f, UIScaleManager.MinScale, UIScaleManager.MaxScale);
            UpdateLabel(lblUIScaleValue, evt.newValue);
            PlayerPrefs.SetFloat(UIScaleManager.PrefKey, uiScale);
            UIScaleManager.Apply(uiScale);
        });

        // ── Graphics ──
        sliderRenderQuality?.RegisterValueChangedCallback(evt =>
        {
            renderQuality = evt.newValue / 100f;
            UpdateLabel(lblRenderQualityValue, evt.newValue);
            PlayerPrefs.SetFloat("YW_RenderQuality", renderQuality);
            ApplyRenderQuality(renderQuality);
        });

        toggleShadow?.RegisterValueChangedCallback(evt =>
        {
            shadowEnabled = evt.newValue;
            UpdateShadowStatusLabel();
            PlayerPrefs.SetInt("YW_Shadow", shadowEnabled ? 1 : 0);
            ApplyShadow(shadowEnabled);
        });

        // Chữ nổi trên cây/thú. FarmLabelVisibility tự lưu PlayerPrefs; FarmTile/FarmAnimal đọc cờ
        // mỗi khung hình nên bật/tắt là thấy ngay, không cần báo cho ai.
        toggleCropLabels?.RegisterValueChangedCallback(evt =>
        {
            YWonderLand.Environment.FarmLabelVisibility.Show = evt.newValue;
            UpdateCropLabelsStatusLabel();
        });

        toggleShowChat?.RegisterValueChangedCallback(evt =>
        {
            showChatEnabled = evt.newValue;
            UpdateChatStatusLabel();
            Debug.Log($"[Settings] Show Chat: {(showChatEnabled ? "ON" : "OFF")}");
            if (ChatPanelController.Instance != null)
            {
                ChatPanelController.Instance.SetChatVisible(showChatEnabled);
            }
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
                    "Bạn có chắc muốn thoát về màn hình chính? Tiến trình đã được lưu tự động.",
                    "Thoát",
                    "Ở lại",
                    OnExitGameConfirmed,
                    ConfirmDialogType.Warning
                );
            }
            else
            {
                // ConfirmDialog chưa gán Inspector → vẫn thoát được (khỏi kẹt nút "không dùng được").
                Debug.LogWarning("[Settings] ConfirmDialog chưa gán — thoát thẳng về menu.");
                OnExitGameConfirmed();
            }
        });
    }

    // ── Confirm Dialog Callbacks ──

    [System.Serializable]
    private class DeleteAccountRequest { public bool confirm; }

    [System.Serializable]
    private class DeleteAccountResponse { public bool ok; public bool deleted; }

    /// <summary>
    /// Xoá tài khoản NGAY TRONG APP — Apple Guideline 5.1.1(v) bắt buộc, và họ đã
    /// nhắc trong thư từ chối 16/08/2026. Trước đây hàm này chỉ là stub gọi UGS
    /// (dự án không dùng UGS) nên nút bấm xong không xoá được gì.
    ///
    /// Server xoá dữ liệu chơi, ẩn danh thông tin cá nhân và khoá đăng nhập; sổ cái
    /// giao dịch cùng số dư Point được giữ để còn đối soát (xem softDeleteAccount
    /// trong server/postgresStore.js). Tài khoản web và ví USDT không bị đụng tới.
    /// </summary>
    private async void OnDeleteAccountConfirmed()
    {
        Debug.Log("[Settings] Xác nhận XÓA TÀI KHOẢN — đang gửi yêu cầu lên máy chủ...");

        var auth = YWonderLand.Backend.AuthService.Instance;
        if (auth == null || !auth.IsSignedIn)
        {
            ScreenToast.Show("Bạn cần đăng nhập trước khi xoá tài khoản.");
            return;
        }

        if (btnDeleteAccount != null) btnDeleteAccount.SetEnabled(false);

        var res = await YWonderLand.Backend.ApiClient.PostAsync<DeleteAccountResponse>(
            "/player/account/delete",
            new DeleteAccountRequest { confirm = true },
            auth.Token);

        if (btnDeleteAccount != null) btnDeleteAccount.SetEnabled(true);

        if (!res.ok)
        {
            string reason = res.status == 0
                ? "Không kết nối được máy chủ. Kiểm tra mạng rồi thử lại."
                : (string.IsNullOrEmpty(res.error) ? "Lỗi không xác định." : res.error);
            Debug.LogWarning($"[Settings] Xoá tài khoản THẤT BẠI (status={res.status}): {reason}");
            ScreenToast.Show("Không xoá được tài khoản. " + reason);
            return;
        }

        Debug.Log("[Settings] Đã xoá tài khoản trên máy chủ. Đăng xuất và về màn hình đăng nhập.");
        ScreenToast.Show("Tài khoản của bạn đã được xoá.");

        Hide();

        // Dùng bản KHÔNG lưu: dữ liệu vừa bị xoá trên máy chủ, đẩy state cũ lên
        // nữa là tạo lại đúng thứ vừa xoá. LogoutToLoginAsync(false) lo cả việc
        // đăng xuất lẫn đưa về màn hình Login.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LogoutToLoginWithoutSaving();
        }
        else
        {
            Debug.LogWarning("[Settings] Không thấy GameManager — tự đăng xuất tại chỗ.");
            auth.SignOutWithoutSavingGameplay();
        }
    }

    private void OnExitGameConfirmed()
    {
        Debug.Log("[Settings] Thoát game → về màn hình chính (Login).");
        Hide();
        // Khách chốt: KHÔNG kill app (iOS không khuyến khích) — về Menu/Login. Save tự chạy lúc Quit/Pause.
        if (GameManager.Instance != null)
            GameManager.Instance.LogoutToLogin();
        else
            Debug.LogWarning("[Settings] Không thấy GameManager để về menu.");
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
        UpdateLabel(lblCameraZoomValue, CameraZoomToPercent(cameraZoom));
        UpdateLabel(lblUIScaleValue, uiScale * 100f);
        UpdateLabel(lblRenderQualityValue, renderQuality * 100f);
    }

    // ── Quy đổi thanh Zoom camera (khách chốt 29/07: dải 50% – 200%) ──
    // 100% = đúng mức camera gốc; 200% = zoom sát nhất (cảnh to nhất); 50% = lùi xa nhất.
    // ThirdPersonCamera.SetUserZoom nhận t ∈ [-1, 1] với -1 = sát vai, +1 = lùi xa.
    private static float PercentToCameraZoom(float percent)
    {
        if (percent >= 100f) return -Mathf.Clamp01((percent - 100f) / 100f);
        return Mathf.Clamp01((100f - percent) / 50f);
    }

    private static float CameraZoomToPercent(float t)
    {
        t = Mathf.Clamp(t, -1f, 1f);
        return t <= 0f ? 100f + (-t) * 100f : 100f - t * 50f;
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
        if (settingsDocument != null) settingsDocument.sortingOrder = 100; // render TRÊN HUD + thanh chat
        UIPopupTracker.SetOpen(this, true); // khoá camera/tương tác + để chat tự ẩn
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
        UIPopupTracker.SetOpen(this, false);
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
            if (lblUIScale != null) lblUIScale.text = GetLocalizedString(table, "ui_scale", "Cỡ chữ & nút");
            if (lblShadow != null) lblShadow.text = GetLocalizedString(table, "shadow", "Bóng đổ");
            if (lblGeneralSection != null) lblGeneralSection.text = GetLocalizedString(table, "general_title", "CHUNG");
            if (lblLanguage != null) lblLanguage.text = GetLocalizedString(table, "language", "Ngôn ngữ");
            if (lblShowChat != null) lblShowChat.text = GetLocalizedString(table, "show_chat", "Hiện chat");

            // Bottom buttons
            if (btnDeleteAccount != null) btnDeleteAccount.text = GetLocalizedString(table, "delete_account", "Xóa tài khoản");
            if (btnExitApp != null) btnExitApp.text = GetLocalizedString(table, "exit_game", "Thoát game");
        }
        else
        {
            LoadLocalizedTextsAsync();
        }

        UpdateShadowStatusLabel();
        UpdateChatStatusLabel();
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

    private void UpdateCropLabelsStatusLabel()
    {
        if (lblCropLabelsStatus == null) return;

        var table = LocalizationSettings.StringDatabase.GetTable("GameUI");
        string onStr = "BẬT";
        string offStr = "TẮT";

        if (table != null)
        {
            onStr = GetLocalizedString(table, "shadow_on", "BẬT");
            offStr = GetLocalizedString(table, "shadow_off", "TẮT");
        }

        lblCropLabelsStatus.text = YWonderLand.Environment.FarmLabelVisibility.Show ? onStr : offStr;
    }

    private void UpdateChatStatusLabel()
    {
        if (lblShowChatStatus == null || toggleShowChat == null) return;

        var table = LocalizationSettings.StringDatabase.GetTable("GameUI");
        string onStr = "BẬT";
        string offStr = "TẮT";

        if (table != null)
        {
            onStr = GetLocalizedString(table, "shadow_on", "BẬT");
            offStr = GetLocalizedString(table, "shadow_off", "TẮT");
        }

        lblShowChatStatus.text = showChatEnabled ? onStr : offStr;
    }
}
