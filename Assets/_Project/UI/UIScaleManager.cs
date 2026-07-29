using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Phóng to/thu nhỏ TOÀN BỘ giao diện UI Toolkit (chữ, nút, popup) theo mức người chơi chọn trong Settings.
/// Khách chốt 29/07: cần kéo tới ~200% cho người lớn tuổi dễ nhìn, và ~50% cho người muốn UI gọn.
///
/// Cách làm: PanelSettings đang chạy chế độ ScaleWithScreenSize, nên UI to lên khi Reference Resolution
/// NHỎ đi. Ta chia độ phân giải tham chiếu gốc cho hệ số zoom.
///
/// Lưu ý: PanelSettings là ASSET dùng chung — sửa lúc Play trong Editor sẽ dính lại vào file .asset,
/// nên phải nhớ giá trị gốc và trả về khi thoát (Application.quitting).
/// </summary>
public static class UIScaleManager
{
    public const string PrefKey = "YW_UIScale";
    public const float MinScale = 0.5f;
    public const float MaxScale = 2.0f;

    private static readonly Dictionary<PanelSettings, Vector2Int> baseResolutions = new Dictionary<PanelSettings, Vector2Int>();
    private static bool quitHookRegistered;

    /// <summary>Hệ số đang áp (1 = 100%).</summary>
    public static float CurrentScale { get; private set; } = 1f;

    /// <summary>Đọc mức đã lưu và áp vào giao diện. Gọi lúc mở game/mở Settings.</summary>
    public static void ApplySaved()
    {
        Apply(PlayerPrefs.GetFloat(PrefKey, 1f));
    }

    // Tự chạy khi vào game để cỡ chữ đã chọn có hiệu lực ngay, không phải mở Settings mới áp.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        ApplySaved();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Scene mới có thể mang PanelSettings khác — áp lại cho chắc (đã cache nên gọi lại vô hại).
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply(CurrentScale);
    }

    /// <summary>Áp hệ số zoom UI (0.5 – 2.0). Không lưu PlayerPrefs — nơi gọi tự lưu.</summary>
    public static void Apply(float scale)
    {
        scale = Mathf.Clamp(scale, MinScale, MaxScale);
        CurrentScale = scale;

        RegisterQuitHook();

        var documents = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (documents == null) return;

        for (int i = 0; i < documents.Length; i++)
        {
            var doc = documents[i];
            var panel = doc != null ? doc.panelSettings : null;
            if (panel == null) continue;

            if (!baseResolutions.TryGetValue(panel, out var baseResolution))
            {
                baseResolution = panel.referenceResolution;
                if (baseResolution.x <= 0 || baseResolution.y <= 0) continue;
                baseResolutions[panel] = baseResolution;
            }

            panel.referenceResolution = new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(baseResolution.x / scale)),
                Mathf.Max(1, Mathf.RoundToInt(baseResolution.y / scale)));
        }
    }

    /// <summary>Trả PanelSettings về đúng độ phân giải tham chiếu gốc (chống dính giá trị vào asset).</summary>
    public static void RestoreBase()
    {
        foreach (var pair in baseResolutions)
        {
            if (pair.Key == null) continue;
            pair.Key.referenceResolution = pair.Value;
        }
        baseResolutions.Clear();
        CurrentScale = 1f;
    }

    private static void RegisterQuitHook()
    {
        if (quitHookRegistered) return;
        quitHookRegistered = true;
        Application.quitting += RestoreBase;
    }
}
