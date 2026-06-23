using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Login,
        Menu,
        Cutscene,
        Gameplay
    }

    [Header("Game State")]
    public GameState currentState = GameState.Login;

    [Header("Characters Prefabs")]
    public GameObject malePrefab;
    public GameObject femalePrefab;

    [Header("Boat Settings")]
    public GameObject boatObject;
    public Transform boatCharacterSpawnPoint; // Position on the boat where character stands
    public BoatCutscene boatCutscene; // Script that drives the boat and camera

    [Header("Cameras")]
    public GameObject menuCamera;
    public GameObject cutsceneCamera;
    public GameObject gameplayCamera;

    [Header("UI Panels")]
    public GameObject loginUIPanel;
    public GameObject menuUIPanel;

    [HideInInspector]
    public int selectedCharacterIndex = 0; // 0 = Male, 1 = Female
    public string playerName;
    private GameObject spawnedCharacter;

    [Header("Resume / Save (người chơi quay lại)")]
    [Tooltip("BẬT để LUÔN chạy Login + Cutscene từ đầu (test phần mở đầu), bỏ qua save người chơi cũ.")]
    [SerializeField] private bool alwaysStartFresh = false;

    // Khoá PlayerPrefs cho luồng resume (người chơi cũ vào thẳng game)
    private const string K_HasSave = "YW_HasSave";
    private const string K_CharIdx = "YW_CharIndex";
    private const string K_Name = "YW_PlayerName";
    private const string K_PosX = "YW_PosX";
    private const string K_PosY = "YW_PosY";
    private const string K_PosZ = "YW_PosZ";
    private const string K_Yaw = "YW_PosYaw";

    void Awake()
    {
        // Singleton CHỐNG LỖI: chỉ huỷ nếu có 1 GameManager KHÁC đang sống (Instance != this).
        // Sửa lỗi TỰ HUỶ khi "Enter Play Mode" tắt Domain/Scene Reload — static Instance từ lần Play
        // trước vẫn trỏ vào CHÍNH object này, khiến code cũ (else Destroy) huỷ nhầm chính mình.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[GameManager] Đã có GameManager khác đang chạy — huỷ bản trùng trên '{gameObject.name}'.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // QUAN TRỌNG (khuyến nghị từ Unity AI): khi "Enter Play Mode" TẮT Domain Reload, static Instance
    // KHÔNG tự reset giữa các lần Play → có thể còn trỏ vào object đã huỷ (fake-null) làm Awake nhầm.
    // Reset về null THẬT trước khi scene load để Awake luôn gán đúng. (Build luôn reload nên không cần.)
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticInstance()
    {
        Instance = null;
    }
#endif

    void Start()
    {
        // Auto-discover any missing camera references at startup
        DiscoverCameras();

        // Người chơi CŨ (đã có save) -> BỎ QUA Login + Cutscene, vào thẳng game ở vị trí cũ.
        // Người chơi MỚI (hoặc bật alwaysStartFresh để test) -> chạy Login như bình thường.
        if (!alwaysStartFresh && PlayerPrefs.GetInt(K_HasSave, 0) == 1)
            ResumeGame();
        else
            SetGameState(GameState.Login);
    }

    private void DiscoverCameras()
    {
        // 1. Discover gameplay camera (Main Camera with ThirdPersonCamera script or tagged MainCamera)
        if (gameplayCamera == null)
        {
            // Look for ThirdPersonCamera script first
            ThirdPersonCamera tpc = FindFirstObjectByType<ThirdPersonCamera>(FindObjectsInactive.Include);
            if (tpc != null)
            {
                gameplayCamera = tpc.gameObject;
                Debug.Log("[GameManager] Auto-discovered ThirdPersonCamera as gameplay camera: " + gameplayCamera.name);
            }
            else
            {
                // Fallback to tagged MainCamera
                GameObject go = GameObject.FindWithTag("MainCamera");
                if (go != null)
                {
                    gameplayCamera = go;
                    Debug.Log("[GameManager] Auto-discovered MainCamera as gameplay camera: " + go.name);
                }
            }
        }

        // 2. Discover Menu Camera
        if (menuCamera == null)
        {
            GameObject go = GameObject.Find("MenuCamera") ?? GameObject.Find("Menu Camera");
            if (go != null)
            {
                menuCamera = go;
                Debug.Log("[GameManager] Auto-discovered Menu Camera: " + go.name);
            }
        }

        // 3. Discover Cutscene Camera
        if (cutsceneCamera == null)
        {
            GameObject go = GameObject.Find("CutsceneCamera") ?? GameObject.Find("Cutscene Camera") ?? GameObject.Find("CinematicCamera");
            if (go != null)
            {
                cutsceneCamera = go;
                Debug.Log("[GameManager] Auto-discovered Cutscene Camera: " + go.name);
            }
        }

        // 4. Default discovery for gameplay camera if still null
        if (gameplayCamera == null)
        {
            GameObject go = GameObject.Find("GameplayCamera") ?? GameObject.Find("Gameplay Camera") ?? GameObject.FindWithTag("MainCamera");
            if (go != null)
            {
                gameplayCamera = go;
                Debug.Log("[GameManager] Auto-discovered fallback Gameplay Camera: " + go.name);
            }
        }
    }

    public void SetGameState(GameState newState)
    {
        Debug.Log($"[GameManager] SetGameState called. Old state: {currentState}, New state: {newState}");
        currentState = newState;

        // Auto-discover cameras again to ensure dynamic changes are caught
        DiscoverCameras();

        // Toggle Cameras depending on state (conflict-free logic if cameras share the same GameObject)
        GameObject activeCam = null;
        if (currentState == GameState.Login || currentState == GameState.Menu)
        {
            activeCam = menuCamera;
        }
        else if (currentState == GameState.Cutscene)
        {
            activeCam = cutsceneCamera;
        }
        else if (currentState == GameState.Gameplay)
        {
            activeCam = gameplayCamera;
        }

        // Deactivate other cameras (ensure we don't deactivate the activeCam if they share the same GameObject)
        if (menuCamera != null && menuCamera != activeCam)
        {
            menuCamera.SetActive(false);
        }
        if (cutsceneCamera != null && cutsceneCamera != activeCam)
        {
            cutsceneCamera.SetActive(false);
        }
        if (gameplayCamera != null && gameplayCamera != activeCam)
        {
            gameplayCamera.SetActive(false);
        }

        // Activate the designated camera
        if (activeCam != null)
        {
            activeCam.SetActive(true);
            Camera cam = activeCam.GetComponentInChildren<Camera>(true);
            if (cam != null) cam.enabled = true;

            // Disable CinemachineBrain to prevent it from overriding camera position
            var brain = activeCam.GetComponentInChildren<Unity.Cinemachine.CinemachineBrain>(true);
            if (brain != null)
            {
                brain.enabled = false;
            }

            // Enable ThirdPersonCamera script only during Gameplay
            var thirdPersonCam = activeCam.GetComponentInChildren<ThirdPersonCamera>(true);
            if (thirdPersonCam != null)
            {
                thirdPersonCam.enabled = (currentState == GameState.Gameplay);
            }
        }

        if (loginUIPanel != null) loginUIPanel.SetActive(currentState == GameState.Login);
        if (menuUIPanel != null) menuUIPanel.SetActive(currentState == GameState.Menu);

        // Toggle CharacterSelect UI Toolkit screen
        CharacterSelectController charSelect = FindFirstObjectByType<CharacterSelectController>();
        if (charSelect != null)
        {
            if (currentState == GameState.Menu)
                charSelect.Show();
            else
                charSelect.Hide();
        }

        // Toggle GameHUD visibility based on gameplay state
        GameHUDController hud = FindFirstObjectByType<GameHUDController>();
        if (hud != null)
        {
            hud.SetHUDVisible(currentState == GameState.Gameplay);
        }

        // Toggle ChatPanel visibility based on gameplay state
        ChatPanelController chat = FindFirstObjectByType<ChatPanelController>();
        if (chat != null)
        {
            chat.SetChatVisible(currentState == GameState.Gameplay);
        }

        switch (currentState)
        {
            case GameState.Login:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (boatCutscene != null) boatCutscene.enabled = false;
                break;

            case GameState.Menu:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (boatCutscene != null) boatCutscene.enabled = false;
                break;

            case GameState.Cutscene:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // Spawn character on the boat
                SpawnCharacterOnBoat();
                
                // Start the boat cutscene
                if (boatCutscene != null)
                {
                    // Force bind the cutscene camera to prevent inspector drag-and-drop mistakes!
                    if (cutsceneCamera != null)
                    {
                        boatCutscene.mainCameraTransform = cutsceneCamera.transform;
                    }
                    boatCutscene.enabled = true;
                    boatCutscene.StartCutscene(spawnedCharacter);
                }
                break;

            case GameState.Gameplay:
                // Activate character controls and physics
                if (spawnedCharacter != null)
                {
                    PlayerController controller = spawnedCharacter.GetComponent<PlayerController>();
                    if (controller != null)
                    {
                        controller.enabled = true;
                        Debug.Log("[GameManager] PlayerController script enabled for gameplay.");
                    }

                    CharacterController cc = spawnedCharacter.GetComponent<CharacterController>();
                    if (cc != null)
                    {
                        cc.enabled = true;
                        Debug.Log("[GameManager] CharacterController component enabled for gameplay.");
                    }

                    // Bind gameplay camera to the spawned character (using GTA-style ThirdPersonCamera)
                    ThirdPersonCamera thirdPersonCam = null;

                    // 1. Try to find ThirdPersonCamera on gameplayCamera or its children
                    if (gameplayCamera != null)
                    {
                        thirdPersonCam = gameplayCamera.GetComponentInChildren<ThirdPersonCamera>(true);
                    }

                    // 2. If not found, look globally in the scene
                    if (thirdPersonCam == null)
                    {
                        thirdPersonCam = FindFirstObjectByType<ThirdPersonCamera>(FindObjectsInactive.Include);
                    }

                    // 3. Perform the binding
                    if (thirdPersonCam != null)
                    {
                        thirdPersonCam.SetTarget(spawnedCharacter.transform);
                        Debug.Log($"[GameManager] Successfully bound ThirdPersonCamera to character: {spawnedCharacter.name}");
                    }
                    else
                    {
                        Debug.LogWarning("[GameManager] Could not find ThirdPersonCamera in the scene to bind to the player!");
                    }

                    // Start onboarding tutorial if TutorialManager exists in scene
                    if (TutorialManager.Instance != null)
                    {
                        TutorialManager.Instance.StartTutorial();
                    }
                }
                else
                {
                    Debug.LogError("[GameManager] CRITICAL: Transitioned to Gameplay but spawnedCharacter is NULL!");
                }
                break;
        }
    }

    public void SelectCharacter(int index)
    {
        selectedCharacterIndex = index;
        Debug.Log("Selected Character: " + (index == 0 ? "Male" : "Female"));
    }

    public void StartGame()
    {
        playerName = CharacterSelectController.PlayerName ?? "Player";

        // Đăng nhập + nạp hồ sơ người chơi (REST). Chạy NỀN song song với cutscene
        // (cutscene dài ~15s >> timeout 5s) -> profile sẵn sàng trước khi Tutorial bắt đầu.
        // Offline thì tự fallback dữ liệu local, KHÔNG chặn luồng vào game.
        _ = SignInAndLoadProfileAsync();

        SetGameState(GameState.Cutscene);
    }

    private async Awaitable SignInAndLoadProfileAsync()
    {
        var auth = YWonderLand.Backend.AuthService.Instance;
        var profile = YWonderLand.Backend.PlayerProfileService.Instance;
        if (auth == null || profile == null) return;

        // Đợt 1 CHƯA nối UI Login: dùng tên nhân vật làm username, mật khẩu sinh & lưu local.
        string username = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
        string password = GetOrCreateLocalPassword(username);

        await auth.EnsureSignedInAsync(username, password);
        await profile.LoadProfileAsync();
        profile.ApplyCharacterInfo(playerName, selectedCharacterIndex == 0 ? "male" : "female");
    }

    private string GetOrCreateLocalPassword(string username)
    {
        string key = "YW_LocalPwd_" + username;
        string pwd = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(pwd))
        {
            pwd = System.Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(key, pwd);
            PlayerPrefs.Save();
        }
        return pwd;
    }

    private void SpawnCharacterOnBoat()
    {
        Debug.Log("[GameManager] SpawnCharacterOnBoat started!");
        if (spawnedCharacter != null)
        {
            Debug.Log("[GameManager] Destroying previous spawned character: " + spawnedCharacter.name);
            Destroy(spawnedCharacter);
        }

        GameObject prefabToSpawn = (selectedCharacterIndex == 0) ? malePrefab : femalePrefab;
        Debug.Log("[GameManager] Prefab selected to spawn: " + (prefabToSpawn != null ? prefabToSpawn.name : "NULL"));

        // BULLETPROOF: Dynamically find the active BoatCutscene (BoatGroup) in the Scene!
        BoatCutscene activeCutscene = GameObject.FindFirstObjectByType<BoatCutscene>();
        if (activeCutscene != null)
        {
            boatObject = activeCutscene.gameObject;
            boatCutscene = activeCutscene;
            Debug.Log("[GameManager] Found active BoatCutscene in Scene: " + boatObject.name + " at position: " + boatObject.transform.position);
        }
        else
        {
            Debug.LogError("[GameManager] FAILED to find any active BoatCutscene in the scene! Make sure BoatGroup exists in the Hierarchy.");
            if (boatObject != null && !boatObject.scene.IsValid())
            {
                Debug.LogWarning("[GameManager] Fallback: GameManager has a boatObject Prefab Asset assigned, trying to Find by name...");
                boatObject = GameObject.Find(boatObject.name);
                Debug.Log("[GameManager] Find by name result: " + (boatObject != null ? "Found " + boatObject.name : "NOT FOUND"));
            }
        }

        // BULLETPROOF: Recursively search for any child containing "spawn" in the active Scene boat
        Transform actualSpawnPoint = null;
        if (boatObject != null)
        {
            actualSpawnPoint = FindChildRecursive(boatObject.transform, "spawn");
            if (actualSpawnPoint != null)
            {
                Debug.Log("[GameManager] Found SpawnPoint child recursively: " + actualSpawnPoint.name + " at local position: " + actualSpawnPoint.localPosition + ", world position: " + actualSpawnPoint.position);
            }
            else
            {
                actualSpawnPoint = boatObject.transform;
                Debug.LogWarning("[GameManager] Did not find any child named 'SpawnPoint' in " + boatObject.name + ". Falling back to boat root transform at position: " + actualSpawnPoint.position);
            }
        }
        else if (boatCharacterSpawnPoint != null)
        {
            actualSpawnPoint = boatCharacterSpawnPoint;
            Debug.Log("[GameManager] Using Inspector assigned spawn point: " + actualSpawnPoint.name + " at position: " + actualSpawnPoint.position);
        }

        // Resolve spawn point if it is still a Prefab Asset
        if (actualSpawnPoint != null && !actualSpawnPoint.gameObject.scene.IsValid())
        {
            Debug.LogWarning("[GameManager] Detected actualSpawnPoint is a Prefab Asset (scene is invalid)! Attempting to fallback to active boat transform in Scene.");
            if (boatObject != null)
            {
                actualSpawnPoint = boatObject.transform;
                Debug.Log("[GameManager] Fallback successful. Using active Scene boat transform: " + actualSpawnPoint.position);
            }
            else
            {
                Debug.LogError("[GameManager] Critical: Spawn point is a Prefab Asset and no active scene boat is found!");
            }
        }

        if (prefabToSpawn != null && actualSpawnPoint != null)
        {
            // Calculate a clean upright world rotation (standing up, facing the boat's Y heading)
            float boatYaw = boatObject != null ? boatObject.transform.eulerAngles.y : actualSpawnPoint.eulerAngles.y;
            Quaternion uprightRotation = Quaternion.Euler(0f, boatYaw, 0f);

            // Spawn character in the Scene standing upright!
            spawnedCharacter = Instantiate(prefabToSpawn, actualSpawnPoint.position, uprightRotation);
            Debug.Log("[GameManager] SUCCESSFULLY INSTANTIATED character: " + spawnedCharacter.name + " in Scene at position: " + spawnedCharacter.transform.position);
            
            // Programmatically force tag the spawned character as "Player"
            spawnedCharacter.tag = "Player";
            Debug.Log("[GameManager] Programmatically set spawned character tag to 'Player'.");

            // Parent it safely ONLY if the target parent is a valid Scene object!
            if (actualSpawnPoint.gameObject.scene.IsValid())
            {
                spawnedCharacter.transform.SetParent(actualSpawnPoint);
                // Force world rotation to be standing upright after parenting!
                spawnedCharacter.transform.rotation = uprightRotation;
                Debug.Log("[GameManager] Character successfully parented to: " + actualSpawnPoint.name + " and forced world rotation upright.");
            }
            else
            {
                Debug.LogWarning("[GameManager] Spawn point is not in a valid scene! Character spawned without parent at: " + spawnedCharacter.transform.position);
            }
            
            // Force activate character gameobject in case it was disabled in prefab
            spawnedCharacter.SetActive(true);

            // Auto-attach floating name tag above player head
            if (spawnedCharacter.GetComponent<FloatingNameTag>() == null)
            {
                FloatingNameTag tag = spawnedCharacter.AddComponent<FloatingNameTag>();
                tag.displayName = playerName ?? "Player";
                tag.nameColor = Color.white; // Màu trắng giống Minecraft
                tag.heightOffset = 2.2f;
                tag.tmpFontSize = 2.5f; // Thu nhỏ lại theo yêu cầu
            }
            
            // Initially disable player controls and physics during cutscene
            PlayerController controller = spawnedCharacter.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.enabled = false;
                Debug.Log("[GameManager] PlayerController script disabled on spawned character for cutscene.");
            }
            else
            {
                Debug.LogWarning("[GameManager] PlayerController component is MISSING on " + spawnedCharacter.name + "! Make sure it is attached to the prefab.");
            }

            CharacterController cc = spawnedCharacter.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                Debug.Log("[GameManager] CharacterController component disabled on spawned character for cutscene.");
            }
        }
        else
        {
            Debug.LogError("[GameManager] CRITICAL: Spawn failed! prefabToSpawn is " + (prefabToSpawn != null ? "OK" : "NULL") + ", actualSpawnPoint is " + (actualSpawnPoint != null ? "OK" : "NULL"));
        }
    }

    // Helper method to recursively search for a child by name keyword
    private Transform FindChildRecursive(Transform parent, string nameKey)
    {
        if (parent.name.ToLower().Contains(nameKey.ToLower())) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, nameKey);
            if (result != null) return result;
        }
        return null;
    }

    public void OnBoatArrived()
    {
        Debug.Log("[GameManager] OnBoatArrived called! Transitioning to gameplay state.");
        
        // When boat arrives at shore, let character detach from boat
        if (spawnedCharacter != null)
        {
            spawnedCharacter.transform.SetParent(null); // Detach from boat
            Debug.Log("[GameManager] Spawned character successfully detached from boat parent.");
        }
        else
        {
            Debug.LogWarning("[GameManager] OnBoatArrived: spawnedCharacter is NULL! Attempting to find player by 'Player' tag...");
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                spawnedCharacter = player;
                spawnedCharacter.transform.SetParent(null);
                Debug.Log("[GameManager] Found player by Tag and successfully detached from boat parent.");
            }
        }
        
        // Switch to gameplay state
        SetGameState(GameState.Gameplay);
    }

    // ──────────────────────────────────────────────
    //  RESUME — người chơi quay lại (bỏ cutscene, giữ vị trí)
    // ──────────────────────────────────────────────

    /// <summary>Vào THẲNG game cho người chơi cũ: spawn nhân vật ở vị trí đã lưu, KHÔNG chạy cutscene.</summary>
    public void ResumeGame()
    {
        selectedCharacterIndex = PlayerPrefs.GetInt(K_CharIdx, 0);
        playerName = PlayerPrefs.GetString(K_Name, "Player");

        Vector3 pos = new Vector3(
            PlayerPrefs.GetFloat(K_PosX, 0f),
            PlayerPrefs.GetFloat(K_PosY, 2f),
            PlayerPrefs.GetFloat(K_PosZ, 0f));
        float yaw = PlayerPrefs.GetFloat(K_Yaw, 0f);

        // Đăng nhập + nạp hồ sơ chạy nền (offline tự fallback) — giống StartGame.
        _ = SignInAndLoadProfileAsync();

        SpawnCharacterAt(pos, yaw);
        if (spawnedCharacter == null)
        {
            Debug.LogWarning("[GameManager] RESUME spawn thất bại -> quay về Login.");
            SetGameState(GameState.Login);
            return;
        }

        SetGameState(GameState.Gameplay);
        Debug.Log($"[GameManager] RESUME: vào thẳng game tại {pos}, char={selectedCharacterIndex}.");
    }

    // Spawn nhân vật ở 1 vị trí mặt đất (dùng cho resume) — KHÔNG gắn lên thuyền.
    private void SpawnCharacterAt(Vector3 pos, float yaw)
    {
        if (spawnedCharacter != null) Destroy(spawnedCharacter);

        GameObject prefab = (selectedCharacterIndex == 0) ? malePrefab : femalePrefab;
        if (prefab == null)
        {
            Debug.LogError("[GameManager] RESUME spawn lỗi: thiếu malePrefab/femalePrefab.");
            return;
        }

        spawnedCharacter = Instantiate(prefab, pos, Quaternion.Euler(0f, yaw, 0f));
        spawnedCharacter.tag = "Player";
        spawnedCharacter.SetActive(true);

        if (spawnedCharacter.GetComponent<FloatingNameTag>() == null)
        {
            FloatingNameTag tag = spawnedCharacter.AddComponent<FloatingNameTag>();
            tag.displayName = playerName ?? "Player";
            tag.nameColor = Color.white;
            tag.heightOffset = 2.2f;
            tag.tmpFontSize = 2.5f;
        }

        // Tắt điều khiển/CC trước; SetGameState(Gameplay) sẽ bật lại sau khi vị trí đã đặt xong.
        PlayerController controller = spawnedCharacter.GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;
        CharacterController cc = spawnedCharacter.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
    }

    // Lưu vị trí + nhân vật khi thoát/chuyển nền. Chỉ lưu TOẠ ĐỘ khi đang ở Nông trại (base scene)
    // để resume không thả nhầm vào toạ độ thành phố (city scene chưa load lúc khởi động).
    private void SavePlayerPosition()
    {
        if (currentState != GameState.Gameplay || spawnedCharacter == null) return;

        string island = IslandTravelManager.Instance != null ? IslandTravelManager.Instance.CurrentIslandId : "farm";
        if (island == "farm")
        {
            Vector3 p = spawnedCharacter.transform.position;
            PlayerPrefs.SetFloat(K_PosX, p.x);
            PlayerPrefs.SetFloat(K_PosY, p.y);
            PlayerPrefs.SetFloat(K_PosZ, p.z);
            PlayerPrefs.SetFloat(K_Yaw, spawnedCharacter.transform.eulerAngles.y);
        }
        PlayerPrefs.SetInt(K_CharIdx, selectedCharacterIndex);
        PlayerPrefs.SetString(K_Name, playerName ?? "Player");
        PlayerPrefs.SetInt(K_HasSave, 1);
        PlayerPrefs.Save();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) SavePlayerPosition();
    }

    void OnApplicationQuit()
    {
        SavePlayerPosition();
    }

    // Tiện TEST: xoá save để chơi lại từ Login + Cutscene như người chơi mới.
    // Chuột phải vào component GameManager trong Inspector -> "Clear Save...".
    [ContextMenu("Clear Save (test chơi lại từ đầu)")]
    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(K_HasSave);
        PlayerPrefs.DeleteKey(K_PosX); PlayerPrefs.DeleteKey(K_PosY); PlayerPrefs.DeleteKey(K_PosZ);
        PlayerPrefs.DeleteKey(K_Yaw); PlayerPrefs.DeleteKey(K_CharIdx); PlayerPrefs.DeleteKey(K_Name);
        PlayerPrefs.Save();
        Debug.Log("[GameManager] Đã xoá save — lần tới sẽ chạy lại Login + Cutscene.");
    }
}
