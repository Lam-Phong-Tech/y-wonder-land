using UnityEngine;
using YWonderLand.Backend;

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
    private bool logoutInProgress;
    private static readonly string[] DemoRichAccounts = { "DemoRich01", "DemoRich02", "DemoRich03", "DemoRich04", "DemoRich05" };
    private const string DemoFreshAccount = "DemoNew01";

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
        if (!alwaysStartFresh && PlayerScopedPrefs.GetInt(K_HasSave, 0) == 1)
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
                SetSpawnedCharacterGameplayEnabled(false);
                break;

            case GameState.Menu:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (boatCutscene != null) boatCutscene.enabled = false;
                SetSpawnedCharacterGameplayEnabled(false);
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

    public void LogoutToLogin()
    {
        if (logoutInProgress) return;
        _ = LogoutToLoginAsync(true);
    }

    public void LogoutToLoginWithoutSaving()
    {
        if (logoutInProgress) return;
        _ = LogoutToLoginAsync(false);
    }

    private async Awaitable LogoutToLoginAsync(bool persistGameplayState)
    {
        logoutInProgress = true;
        try
        {
            Debug.Log("[GameManager] LogoutToLogin: cleaning gameplay session before showing Login.");

            SetSpawnedCharacterGameplayEnabled(false);
            if (persistGameplayState)
            {
                // The ordinary logout path persists once before auth is revoked.
                SavePlayerPosition();
                FarmStateSync.SaveRuntimeState();
                bool farmFlushed = await FarmStateSync.FlushAsync();
                if (!farmFlushed)
                    Debug.LogWarning($"[GameManager] Logout continued with durable pending farm sync: {FarmStateSync.LastError}");
                bool mutationsFlushed = await GameplayMutationSync.FlushAsync();
                if (!mutationsFlushed)
                    Debug.LogWarning($"[GameManager] Logout continued with pending state sync: {GameplayMutationSync.LastError}");
            }

            if (YWonderLand.Realtime.RealtimeClient.Instance != null)
                await YWonderLand.Realtime.RealtimeClient.Instance.DisconnectForAuthenticationChangeAsync();

            HideGameplayUiForLogin();
            ClearGameplayCameraTarget();
            DestroyRemotePlayers();

            if (boatCutscene != null)
                boatCutscene.enabled = false;

            if (spawnedCharacter == null)
            {
                var taggedPlayer = GameObject.FindGameObjectWithTag("Player");
                if (taggedPlayer != null)
                    spawnedCharacter = taggedPlayer;
            }

            SetSpawnedCharacterGameplayEnabled(false);

            if (spawnedCharacter != null)
            {
                Destroy(spawnedCharacter);
                spawnedCharacter = null;
            }

            ClearResumeFlag();
            var auth = YWonderLand.Backend.AuthService.Instance;
            if (auth != null)
            {
                if (persistGameplayState)
                    await auth.SignOutAfterGameplaySavedAsync();
                else
                    auth.SignOutWithoutSavingGameplay();
            }
            SetGameState(GameState.Login);
        }
        finally
        {
            logoutInProgress = false;
        }
    }

    private void SetSpawnedCharacterGameplayEnabled(bool enabled)
    {
        if (spawnedCharacter == null) return;

        var player = spawnedCharacter.GetComponent<PlayerController>();
        if (player != null)
        {
            if (!enabled)
            {
                player.SetMoveInput(Vector2.zero);
                if (player.IsBusy)
                    player.CancelAction();
            }

            player.enabled = enabled;
        }

        var playerInput = spawnedCharacter.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
            playerInput.enabled = enabled;

        var characterController = spawnedCharacter.GetComponent<CharacterController>();
        if (characterController != null)
            characterController.enabled = enabled;
    }

    private void HideGameplayUiForLogin()
    {
        BuildModeOverlayController.Instance?.Hide();
        FishingOverlayController.Instance?.Hide();
        AnimalInteractionPopupController.Instance?.Hide();
        YWonderLand.UI.ResourceInteractionUIController.Instance?.Hide();

        HidePopupIfPresent<InventoryPopupController>();
        HidePopupIfPresent<ShopPopupController>();
        HidePopupIfPresent<WorkshopPopupController>();
        HideEventPopupsForLogin();
        HidePopupIfPresent<MapPopupController>();
        HidePopupIfPresent<QuestPopupController>();
        HidePopupIfPresent<MailboxPopupController>();
        HidePopupIfPresent<ProfilePopupController>();
        HidePopupIfPresent<FriendsPopupController>();
        HidePopupIfPresent<LeaderboardPopupController>();
        HidePopupIfPresent<PiggyBankPopupController>();
        HidePopupIfPresent<RewardPopupController>();
        HidePopupIfPresent<ConfirmDialogController>();
        HidePopupIfPresent<LevelUpOverlayController>();

        GameHUDController.Instance?.HideInteractionPrompt();
        GameHUDController.Instance?.HideFishingCancelProgress();
        GameHUDController.Instance?.SetHUDVisible(false);
        ChatPanelController.Instance?.SetChatVisible(false);
        UIPopupTracker.ClearAll();
    }

    private void HideEventPopupsForLogin()
    {
        foreach (var popup in FindObjectsByType<EventPopupController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (popup == null) continue;
            popup.Hide();
            popup.HideLuckyWheel();
        }
    }

    private void HidePopupIfPresent<T>() where T : MonoBehaviour
    {
        foreach (var popup in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (popup == null) continue;
            var hideMethod = typeof(T).GetMethod("Hide", System.Type.EmptyTypes);
            hideMethod?.Invoke(popup, null);
        }
    }

    private void ClearGameplayCameraTarget()
    {
        foreach (var thirdPersonCam in FindObjectsByType<ThirdPersonCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (thirdPersonCam != null)
                thirdPersonCam.SetTarget(null);
        }
    }

    private void DestroyRemotePlayers()
    {
        foreach (var remote in FindObjectsByType<YWonderLand.Realtime.RemotePlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (remote != null)
                Destroy(remote.gameObject);
        }
    }

    private void ClearResumeFlag()
    {
        PlayerScopedPrefs.DeleteKey(K_HasSave);
        PlayerScopedPrefs.Save();
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

    public void StartGameFromProfile()
    {
        var auth = YWonderLand.Backend.AuthService.Instance;
        var profileService = YWonderLand.Backend.PlayerProfileService.Instance;
        var profile = profileService != null ? profileService.Profile : null;

        playerName = !string.IsNullOrEmpty(profile?.name)
            ? profile.name
            : (!string.IsNullOrEmpty(auth?.Username) ? auth.Username : "Player");

        selectedCharacterIndex = string.Equals(profile?.gender, "female", System.StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        if (auth != null && profileService != null)
            ApplyDemoAccountOverrides(auth.Username, profileService);

        Debug.Log($"[GameManager] Starting from existing profile without intro cutscene: {playerName}, gender={selectedCharacterIndex}.");
        StartExistingCharacterDirectly();
    }

    private async Awaitable SignInAndLoadProfileAsync()
    {
        var auth = YWonderLand.Backend.AuthService.Instance;
        var profile = YWonderLand.Backend.PlayerProfileService.Instance;
        if (auth == null || profile == null) return;

        string backendUsername = auth.IsSignedIn && !string.IsNullOrEmpty(auth.Username)
            ? auth.Username
            : null;

        if (string.IsNullOrEmpty(backendUsername))
        {
            // Fallback cho flow cũ/offline: chưa qua Login backend thì dùng tên nhân vật làm account local.
            backendUsername = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
            string password = GetOrCreateLocalPassword(backendUsername);
            await auth.EnsureSignedInAsync(backendUsername, password);
            backendUsername = !string.IsNullOrEmpty(auth.Username) ? auth.Username : backendUsername;
        }
        else
        {
            Debug.Log($"[GameManager] Using signed-in backend account: {backendUsername}");
        }

        bool bootstrapLoaded = false;
        var bootstrap = YWonderLand.Backend.PlayerBootstrapService.Instance;
        if (bootstrap != null)
            bootstrapLoaded = await bootstrap.LoadBootstrapAsync();

        if (!bootstrapLoaded)
            await profile.LoadProfileAsync();

        profile.ApplyCharacterInfo(playerName, selectedCharacterIndex == 0 ? "male" : "female");
        ApplyDemoAccountOverrides(backendUsername, profile);
    }

    private string GetOrCreateLocalPassword(string username)
    {
        if (IsFixedDemoAccount(username))
        {
            string demoKey = "YW_LocalPwd_" + username;
            PlayerPrefs.SetString(demoKey, username);
            PlayerPrefs.Save();
            return username;
        }

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

    private static bool IsFixedDemoAccount(string username)
    {
        return IsRichDemoAccount(username)
            || string.Equals(username, DemoFreshAccount, System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsRichDemoAccountName(string username)
    {
        return IsRichDemoAccount(username);
    }

    private static bool IsRichDemoAccount(string username)
    {
        if (string.IsNullOrEmpty(username)) return false;
        foreach (string account in DemoRichAccounts)
        {
            if (string.Equals(username, account, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private void ApplyDemoAccountOverrides(string username, YWonderLand.Backend.PlayerProfileService profile)
    {
        if (!IsRichDemoAccount(username))
            return;

        if (profile != null && profile.Profile != null)
        {
            if (!profile.Profile.characterCreated)
                profile.ApplyCharacterInfo(username, profile.Profile.gender);

            if (!profile.Profile.tutorialCompleted)
                profile.SetTutorialCompleted(true);
        }

        string seedKey = "YW_DemoLoadoutSeeded_" + username;
        if (PlayerPrefs.GetInt(seedKey, 0) == 1) return;

        if (YWonderLand.Backend.PlayerBootstrapService.Instance != null
            && YWonderLand.Backend.PlayerBootstrapService.Instance.HasServerBootstrap)
        {
            Debug.Log($"[GameManager] {username}: server bootstrap loaded, skip local rich demo loadout.");
            return;
        }

        YWonderLand.Managers.InventoryManager.Instance?.GiveTestLoadout();
        PlayerPrefs.SetInt(seedKey, 1);
        PlayerPrefs.Save();
        Debug.Log($"[GameManager] {username}: seeded rich demo loadout and skipped tutorial.");
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
        var config = YWonderLand.Backend.BackendConfig.Active;
        if (config != null && !config.useOfflineFallback)
        {
            SetGameState(GameState.Login);
            YWonderLand.Environment.ScreenToast.ShowInfo("Đang xác nhận phiên với máy chủ...", 3f);
            _ = ResumeGameAfterServerValidationAsync();
            return;
        }

        ResumeGameFromLocalSave(false);
    }

    private async Awaitable ResumeGameAfterServerValidationAsync()
    {
        var auth = YWonderLand.Backend.AuthService.Instance;
        var bootstrap = YWonderLand.Backend.PlayerBootstrapService.Instance;
        if (auth == null || bootstrap == null || !auth.IsSignedIn)
        {
            SetGameState(GameState.Login);
            return;
        }

        string tokenBeingValidated = auth.Token;
        bool loaded = await bootstrap.LoadBootstrapAsync();

        // A manual login may have replaced the cached token while validation was in flight.
        if (auth.Token != tokenBeingValidated)
            return;

        if (!loaded)
        {
            long failedStatus = bootstrap.LastStatus;
            if (failedStatus == 401)
                auth.SignOutWithoutSavingGameplay();

            SetGameState(GameState.Login);
            string message = failedStatus == 401
                ? "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
                : "Không thể kết nối máy chủ. Game chưa vào chế độ online.";
            YWonderLand.Environment.ScreenToast.Show(message, 6f);
            return;
        }

        ResumeGameFromLocalSave(true);
    }

    private void ResumeGameFromLocalSave(bool bootstrapAlreadyLoaded)
    {
        selectedCharacterIndex = PlayerScopedPrefs.GetInt(K_CharIdx, 0);
        playerName = PlayerScopedPrefs.GetString(K_Name, "Player");

        Vector3 pos = new Vector3(
            PlayerScopedPrefs.GetFloat(K_PosX, 0f),
            PlayerScopedPrefs.GetFloat(K_PosY, 2f),
            PlayerScopedPrefs.GetFloat(K_PosZ, 0f));
        float yaw = PlayerScopedPrefs.GetFloat(K_Yaw, 0f);

        if (!bootstrapAlreadyLoaded)
            _ = SignInAndLoadProfileAsync();

        SpawnCharacterAt(pos, yaw);
        if (spawnedCharacter == null)
        {
            Debug.LogWarning("[GameManager] RESUME spawn thất bại -> quay về Login.");
            SetGameState(GameState.Login);
            return;
        }

        // Thuyền: resume bỏ cutscene -> tự đặt thuyền tại bến (kẻo nó nằm lại điểm xuất phát).
        var boat = FindFirstObjectByType<BoatCutscene>();
        if (boat != null) boat.SnapToDock();

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

    private void StartExistingCharacterDirectly()
    {
        FloatingNameTag.GloballyHidden = false;

        Vector3 spawnPos;
        float spawnYaw;
        if (!TryGetSavedFarmPose(out spawnPos, out spawnYaw)
            && !TryGetDockSpawnPose(out spawnPos, out spawnYaw))
        {
            spawnPos = new Vector3(0f, 2f, 0f);
            spawnYaw = 0f;
            Debug.LogWarning($"[GameManager] Existing profile direct start could not resolve saved or dock spawn. Falling back to {spawnPos}.");
        }

        SpawnCharacterAt(spawnPos, spawnYaw);
        if (spawnedCharacter == null)
        {
            Debug.LogWarning("[GameManager] Existing profile direct start failed to spawn player -> returning to Login.");
            SetGameState(GameState.Login);
            return;
        }

        SetGameState(GameState.Gameplay);
        Debug.Log($"[GameManager] Existing profile direct start: entered gameplay at {spawnPos}.");
    }

    private bool TryGetSavedFarmPose(out Vector3 position, out float yaw)
    {
        position = Vector3.zero;
        yaw = 0f;
        if (!PlayerScopedPrefs.HasKey(K_PosX)
            || !PlayerScopedPrefs.HasKey(K_PosY)
            || !PlayerScopedPrefs.HasKey(K_PosZ))
            return false;

        position = new Vector3(
            PlayerScopedPrefs.GetFloat(K_PosX, 0f),
            PlayerScopedPrefs.GetFloat(K_PosY, 2f),
            PlayerScopedPrefs.GetFloat(K_PosZ, 0f));
        yaw = PlayerScopedPrefs.GetFloat(K_Yaw, 0f);
        return float.IsFinite(position.x)
               && float.IsFinite(position.y)
               && float.IsFinite(position.z)
               && float.IsFinite(yaw);
    }

    private bool TryGetDockSpawnPose(out Vector3 position, out float yaw)
    {
        position = Vector3.zero;
        yaw = 0f;

        var boat = FindFirstObjectByType<BoatCutscene>();
        if (boat == null)
            return false;

        boatCutscene = boat;
        boatObject = boat.gameObject;
        boat.SnapToDock();

        Transform spawnPoint = FindChildRecursive(boat.transform, "spawn");
        if (spawnPoint == null)
            spawnPoint = boat.transform;

        position = spawnPoint.position;
        yaw = spawnPoint.eulerAngles.y;
        return true;
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
            PlayerScopedPrefs.SetFloat(K_PosX, p.x);
            PlayerScopedPrefs.SetFloat(K_PosY, p.y);
            PlayerScopedPrefs.SetFloat(K_PosZ, p.z);
            PlayerScopedPrefs.SetFloat(K_Yaw, spawnedCharacter.transform.eulerAngles.y);
        }
        PlayerScopedPrefs.SetInt(K_CharIdx, selectedCharacterIndex);
        PlayerScopedPrefs.SetString(K_Name, playerName ?? "Player");
        PlayerScopedPrefs.SetInt(K_HasSave, 1);
        PlayerScopedPrefs.Save();
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
        PlayerScopedPrefs.DeleteKey(K_HasSave);
        PlayerScopedPrefs.DeleteKey(K_PosX); PlayerScopedPrefs.DeleteKey(K_PosY); PlayerScopedPrefs.DeleteKey(K_PosZ);
        PlayerScopedPrefs.DeleteKey(K_Yaw); PlayerScopedPrefs.DeleteKey(K_CharIdx); PlayerScopedPrefs.DeleteKey(K_Name);
        PlayerScopedPrefs.Save();
        Debug.Log("[GameManager] Đã xoá save — lần tới sẽ chạy lại Login + Cutscene.");
    }
}
