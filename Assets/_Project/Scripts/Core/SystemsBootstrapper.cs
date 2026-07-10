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
#if UNITY_EDITOR || UNITY_STANDALONE
            Application.runInBackground = true;
#endif
            QualitySettings.vSyncCount = 0;

            GameObject systemsGo = new GameObject("[Persistent Systems]");
            Object.DontDestroyOnLoad(systemsGo);

            // Auth must load its cached identity before gameplay managers resolve
            // player-scoped PlayerPrefs keys.
            systemsGo.AddComponent<AuthService>();
            systemsGo.AddComponent<EconomyManager>();
            systemsGo.AddComponent<InventoryManager>();
            systemsGo.AddComponent<ToolManager>();

            // Backend (REST) — hồ sơ + bootstrap sau khi core managers tồn tại.
            systemsGo.AddComponent<PlayerProfileService>();
            systemsGo.AddComponent<PlayerBootstrapService>();

            Debug.Log("[Bootstrapper] Persistent Systems initialized (Economy, Inventory, Tools, Auth, Profile, Bootstrap).");
        }
    }
}
