using UnityEngine;
using YWonderLand.Managers;
using YWonderLand.Backend;

namespace YWonderLand.Core
{
    /// <summary>
    /// Tự động khởi tạo các hệ thống Singleton Core (Economy, Inventory) ngay khi game vừa bật.
    /// Giúp không phải kéo thả script vào Scene thủ công và giữ GameManager sạch sẽ.
    /// </summary>
    public static class SystemsBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            // Cheap mobile win: cap FPS + tắt vsync để máy yếu không giật/nóng vô ích.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            GameObject systemsGo = new GameObject("[Persistent Systems]");
            Object.DontDestroyOnLoad(systemsGo);

            systemsGo.AddComponent<EconomyManager>();
            systemsGo.AddComponent<InventoryManager>();
            systemsGo.AddComponent<ToolManager>();

            // Backend (REST) — đăng nhập + hồ sơ người chơi. Offline-first.
            systemsGo.AddComponent<AuthService>();
            systemsGo.AddComponent<PlayerProfileService>();

            Debug.Log("[Bootstrapper] Persistent Systems initialized (Economy, Inventory, Tools, Auth, Profile).");
        }
    }
}
