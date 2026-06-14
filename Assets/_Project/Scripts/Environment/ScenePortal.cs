using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class ScenePortal : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("Tọa độ dịch chuyển nhân vật tới")]
    public Vector3 targetPosition;
    
    [Tooltip("Góc quay của nhân vật sau khi dịch chuyển")]
    public float targetYRotation;

    [Header("Scene Settings (Tuỳ chọn)")]
    [Tooltip("Tên Scene mới muốn load đè (Additive) nếu có. Bỏ trống nếu dịch chuyển cùng Scene.")]
    public string additiveSceneToLoad;
    
    [Tooltip("Tên Scene cũ muốn Unload (nếu cần)")]
    public string additiveSceneToUnload;

    private bool isTeleporting = false;

    private async void OnTriggerEnter(Collider other)
    {
        if (isTeleporting) return;

        if (other.CompareTag("Player"))
        {
            isTeleporting = true;
            await TeleportPlayerAsync(other.gameObject);
        }
    }

    private async Task TeleportPlayerAsync(GameObject player)
    {
        // 1. Hiện màn hình chờ
        if (LoadingScreenController.Instance == null)
        {
            // Tự động tạo LoadingScreen nếu quên chưa gắn vào Scene
            GameObject loadingObj = new GameObject("LoadingScreenController");
            loadingObj.AddComponent<LoadingScreenController>();
        }

        if (LoadingScreenController.Instance != null)
        {
            await LoadingScreenController.Instance.ShowLoadingAsync("Đang di chuyển...");
        }

        // Tắt CharacterController tạm thời để không bị giật khi dịch chuyển
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        MonoBehaviour pc = player.GetComponent("PlayerController") as MonoBehaviour;
        if (pc != null) pc.enabled = false;

        // 2. Load Scene đè (Nếu có)
        if (!string.IsNullOrEmpty(additiveSceneToLoad))
        {
            // Kiểm tra xem scene đã load chưa
            bool isLoaded = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == additiveSceneToLoad)
                {
                    isLoaded = true;
                    break;
                }
            }

            if (!isLoaded)
            {
                AsyncOperation loadOp = SceneManager.LoadSceneAsync(additiveSceneToLoad, LoadSceneMode.Additive);
                while (!loadOp.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }
                Debug.Log($"[ScenePortal] Đã load Additive Scene: {additiveSceneToLoad}");
            }
        }

        // 3. Unload Scene cũ (Nếu có)
        if (!string.IsNullOrEmpty(additiveSceneToUnload))
        {
            Scene sceneToUnload = SceneManager.GetSceneByName(additiveSceneToUnload);
            if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(additiveSceneToUnload);
                while (!unloadOp.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }
                Debug.Log($"[ScenePortal] Đã Unload Scene: {additiveSceneToUnload}");
            }
        }

        // 4. Dịch chuyển Player
        player.transform.position = targetPosition;
        player.transform.rotation = Quaternion.Euler(0, targetYRotation, 0);

        // Đợi 1 frame để Physics update
        await Awaitable.NextFrameAsync();

        // Bật lại
        if (cc != null) cc.enabled = true;
        if (pc != null) pc.enabled = true;

        // 5. Ẩn màn hình chờ
        if (LoadingScreenController.Instance != null)
        {
            await LoadingScreenController.Instance.HideLoadingAsync();
        }

        // Reset trạng thái (sau 2 giây để tránh kẹt va chạm)
        await Awaitable.WaitForSecondsAsync(2f);
        isTeleporting = false;
    }
}
