using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Tự gắn <see cref="UISafeArea"/> cho MỌI UIDocument trong scene (HUD + tất cả popup) để UI
/// không bị TAI THỎ / ĐỤC LỖ / bo góc che — trên mọi loại màn hình phổ biến.
///
/// Chạy tự động (không cần thao tác Editor) sau khi scene load, và quét lại mỗi lần load thêm
/// scene (vd CityScene additive). Popup đặt sẵn trong scene (kể cả đang tắt) đều được phủ vì
/// quét cả object inactive.
///
/// Lưu ý: UISafeArea mặc định chỉ áp trong BUILD. Installer bật luôn ApplyInEditor cho các
/// component nó thêm để anh TEST được trong Device Simulator (ở Game view thường safeArea =
/// full màn nên padding = 0 → vô hại).
/// </summary>
public static class SafeAreaInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        InstallAll();
        // Quét lại khi có scene mới load thêm (đổi đảo Farm <-> City).
        SceneManager.sceneLoaded -= OnSceneLoaded; // tránh đăng ký trùng khi domain reload tắt
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => InstallAll();

    private static void InstallAll()
    {
        // Include inactive: popup thường tắt sẵn trong scene.
        var docs = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var doc in docs)
        {
            if (doc == null) continue;
            var safe = doc.GetComponent<UISafeArea>();
            if (safe == null)
                safe = doc.gameObject.AddComponent<UISafeArea>();
            // Bật cả cho component CŨ (gắn tay) để Device Simulator luôn áp safe area.
            safe.ApplyInEditor = true;
        }
    }
}
