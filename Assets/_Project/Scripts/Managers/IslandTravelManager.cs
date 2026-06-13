using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// IslandTravelManager: Trung tâm điều phối di chuyển giữa các đảo (map).
/// Tái sử dụng cơ chế "Additive Scene" giống ScenePortal nhưng gọi được từ UI (Bản đồ / phím M).
///
/// MÔ HÌNH KIẾN TRÚC (quan trọng):
///  - Scene NỀN (Scene 0 / Nông trại) LUÔN được giữ tải -> chứa UI, Manager, Player.
///    => UI KHÔNG bao giờ mất khi đổi đảo, KHÔNG cần đóng gói UI thành prefab.
///  - Mỗi đảo phụ (Thành phố, Mỏ...) là 1 Scene riêng, Load đè (Additive) khi tới
///    và Unload khi rời đi. Scene nền không bao giờ bị Unload.
///
/// Cách dùng: IslandTravelManager.Instance.TravelToIsland("city");
/// </summary>
public class IslandTravelManager : MonoBehaviour
{
    public static IslandTravelManager Instance { get; private set; }

    [System.Serializable]
    public class IslandDef
    {
        [Tooltip("ID đảo — PHẢI khớp id trong MapPopupController: farm, city, mine, haiphu, mocnhi")]
        public string id;

        [Tooltip("Tên hiển thị (chỉ để dễ đọc + hiện trên màn hình chờ)")]
        public string displayName;

        [Tooltip("Tên Scene cần Load đè (Additive). ĐỂ TRỐNG nếu đảo này nằm sẵn trong Scene nền.")]
        public string sceneName;

        [Tooltip("Tích nếu đảo nằm sẵn trong Scene nền (Scene 0) -> sẽ KHÔNG bao giờ bị Unload.")]
        public bool isBaseScene;

        [Tooltip("Toạ độ thả nhân vật khi tới đảo này (phải nằm trên mặt đất/plane của đảo đó)")]
        public Vector3 spawnPosition;

        [Tooltip("Góc quay Y của nhân vật sau khi tới")]
        public float spawnYRotation;
    }

    [Header("Danh sách đảo (cấu hình trong Inspector)")]
    [SerializeField] private List<IslandDef> islands = new List<IslandDef>();

    [Tooltip("ID đảo nhân vật đang đứng lúc bắt đầu game")]
    [SerializeField] private string startingIslandId = "farm";

    /// <summary>ID đảo hiện tại nhân vật đang đứng.</summary>
    public string CurrentIslandId { get; private set; }

    private bool _isTraveling = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CurrentIslandId = startingIslandId;
    }

    /// <summary>Gọi từ UI (Bản đồ). Fire-and-forget — không cần await.</summary>
    public void TravelToIsland(string islandId)
    {
        _ = TravelToAsync(islandId);
    }

    private async Awaitable TravelToAsync(string targetId)
    {
        if (_isTraveling)
        {
            Debug.Log("[IslandTravel] Đang di chuyển, bỏ qua yêu cầu mới.");
            return;
        }

        IslandDef target = islands.Find(i => i.id == targetId);
        if (target == null)
        {
            Debug.LogWarning($"[IslandTravel] Không tìm thấy cấu hình đảo id='{targetId}'. Hãy thêm vào danh sách trong Inspector.");
            return;
        }

        if (targetId == CurrentIslandId)
        {
            Debug.Log($"[IslandTravel] Đang ở sẵn đảo '{targetId}' rồi.");
            return;
        }

        _isTraveling = true;
        IslandDef previous = islands.Find(i => i.id == CurrentIslandId);

        // 1. Hiện màn hình chờ (tự tạo nếu thiếu — giống ScenePortal)
        if (LoadingScreenController.Instance == null)
        {
            var loadingObj = new GameObject("LoadingScreenController");
            loadingObj.AddComponent<LoadingScreenController>();
        }
        if (LoadingScreenController.Instance != null)
        {
            await LoadingScreenController.Instance.ShowLoadingAsync($"Đang tới {target.displayName}...");
        }

        // 2. Tạm khoá điều khiển để teleport không bị giật
        PlayerController player = PlayerController.Instance;
        CharacterController cc = player != null ? player.GetComponent<CharacterController>() : null;
        if (cc != null) cc.enabled = false;
        if (player != null) player.enabled = false;

        // 3. Load đảo đích (nếu là scene phụ và chưa được load)
        if (!target.isBaseScene && !string.IsNullOrEmpty(target.sceneName) && !IsSceneLoaded(target.sceneName))
        {
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(target.sceneName, LoadSceneMode.Additive);
            while (loadOp != null && !loadOp.isDone) await Awaitable.NextFrameAsync();
            Debug.Log($"[IslandTravel] Đã load đảo: {target.sceneName}");
        }

        // 4. Unload đảo cũ (chỉ unload scene phụ — KHÔNG bao giờ unload scene nền)
        if (previous != null && !previous.isBaseScene && !string.IsNullOrEmpty(previous.sceneName)
            && previous.id != target.id && IsSceneLoaded(previous.sceneName))
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(previous.sceneName);
            while (unloadOp != null && !unloadOp.isDone) await Awaitable.NextFrameAsync();
            Debug.Log($"[IslandTravel] Đã unload đảo cũ: {previous.sceneName}");
        }

        // 5. Thả nhân vật tới điểm spawn của đảo đích
        if (player != null)
        {
            player.transform.position = target.spawnPosition;
            player.transform.rotation = Quaternion.Euler(0, target.spawnYRotation, 0);
            player.isSwimming = false; // reset trạng thái bơi phòng khi teleport ra khỏi vùng nước
        }

        // Đợi 1 frame cho Physics cập nhật trước khi bật lại CharacterController
        await Awaitable.NextFrameAsync();

        if (cc != null) cc.enabled = true;
        if (player != null) player.enabled = true;

        CurrentIslandId = target.id;
        Debug.Log($"[IslandTravel] Đã tới đảo: {target.displayName} ({target.id})");

        // 6. Ẩn màn hình chờ
        if (LoadingScreenController.Instance != null)
        {
            await LoadingScreenController.Instance.HideLoadingAsync();
        }

        _isTraveling = false;
    }

    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == sceneName) return true;
        }
        return false;
    }

#if UNITY_EDITOR
    // Tự điền sẵn cấu hình mẫu khi mới Add component cho đỡ phải gõ tay.
    // Anh chỉ cần chỉnh lại spawnPosition cho khớp đảo thật.
    private void Reset()
    {
        islands = new List<IslandDef>
        {
            new IslandDef { id = "farm",  displayName = "Nông trại",   sceneName = "",          isBaseScene = true,  spawnPosition = new Vector3(0, 2, 0),    spawnYRotation = 0 },
            new IslandDef { id = "city",  displayName = "Thành phố",   sceneName = "CityScene", isBaseScene = false, spawnPosition = new Vector3(1000, 2, 0), spawnYRotation = 0 },
            new IslandDef { id = "mine",  displayName = "Khai thác mỏ", sceneName = "MineMap",   isBaseScene = false, spawnPosition = new Vector3(0, 2, 500),  spawnYRotation = 0 },
        };
    }
#endif
}
