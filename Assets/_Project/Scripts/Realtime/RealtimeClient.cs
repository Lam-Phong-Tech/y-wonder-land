using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using YWonderLand.Backend;

namespace YWonderLand.Realtime
{
    /// <summary>
    /// WebSocket client for MVP realtime: global chat plus remote players in shared islands.
    /// Farm remains private: the client keeps chat online but only joins public rooms in city or mine.
    /// </summary>
    public class RealtimeClient : MonoBehaviour
    {
        public static RealtimeClient Instance { get; private set; }

        private static readonly HashSet<string> SharedRooms = new HashSet<string> { "city", "mine" };

        [Header("Connection")]
        [SerializeField] private float reconnectDelaySec = 5f;
        [SerializeField] private float stateSendIntervalSec = 0.15f;

        private ClientWebSocket socket;
        private CancellationTokenSource socketCts;
        private bool connectInProgress;
        private string currentRoom = "";
        private string serverSelfId = "";
        private float nextConnectionCheck;
        private float nextReconnectAt;
        private float nextStateSendAt;
        private Vector3 lastSamplePosition;
        private float lastSampleTime;
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

        private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
        private readonly Dictionary<string, RemotePlayerController> remotePlayers = new Dictionary<string, RemotePlayerController>();
        private static readonly HashSet<string> RemoteToolObjectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Axe",
            "Pickaxe",
            "BasicPickaxe",
            "MetalHoe",
            "Plant",
            "WaterBucket",
            "binhnuoc",
            "fishingrod",
            "hammer",
            "oat",
            "FishingLine",
            "Bobber",
        };

        public bool IsConnected => socket != null && socket.State == WebSocketState.Open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("RealtimeClient");
            DontDestroyOnLoad(go);
            go.AddComponent<RealtimeClient>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            while (mainThreadQueue.TryDequeue(out var action))
            {
                try { action?.Invoke(); }
                catch (Exception e) { Debug.LogWarning($"[Realtime] Main-thread handler error: {e.Message}"); }
            }

            if (Time.unscaledTime >= nextConnectionCheck)
            {
                nextConnectionCheck = Time.unscaledTime + 1f;
                TickConnection();
            }

            if (IsConnected && !string.IsNullOrEmpty(currentRoom) && Time.unscaledTime >= nextStateSendAt)
            {
                nextStateSendAt = Time.unscaledTime + stateSendIntervalSec;
                SendLocalState();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            _ = DisconnectAsync();
        }

        public bool SendChat(string message)
        {
            if (!IsConnected || string.IsNullOrWhiteSpace(message)) return false;
            _ = SendJsonAsync(new { type = "chat", message = message.Trim() });
            return true;
        }

        public void SendEmote(string emote, float duration)
        {
            if (!IsConnected || string.IsNullOrEmpty(currentRoom)) return;
            _ = SendJsonAsync(new { type = "emote", emote, duration });
        }

        private void TickConnection()
        {
            string desiredRoom = GetDesiredRoom();
            bool shouldConnect = AuthService.Instance != null
                                 && AuthService.Instance.IsSignedIn
                                 && PlayerController.Instance != null
                                 && IsGameplay();

            if (!shouldConnect)
            {
                if (IsConnected || connectInProgress) _ = DisconnectAsync();
                return;
            }

            if (!IsConnected && !connectInProgress && Time.unscaledTime >= nextReconnectAt)
            {
                _ = ConnectAsync(desiredRoom);
                return;
            }

            if (!IsConnected) return;

            if (!string.IsNullOrEmpty(desiredRoom) && currentRoom != desiredRoom)
            {
                JoinRoom(desiredRoom);
            }
            else if (string.IsNullOrEmpty(desiredRoom) && !string.IsNullOrEmpty(currentRoom))
            {
                LeaveRoom();
            }
        }

        private bool IsGameplay()
        {
            return GameManager.Instance == null || GameManager.Instance.currentState == GameManager.GameState.Gameplay;
        }

        private string GetDesiredRoom()
        {
            string island = IslandTravelManager.Instance != null ? IslandTravelManager.Instance.CurrentIslandId : "";
            return SharedRooms.Contains(island) ? island : "";
        }

        private async Task ConnectAsync(string room)
        {
            connectInProgress = true;
            try
            {
                await DisconnectAsync();

                string token = AuthService.Instance != null ? AuthService.Instance.Token : "";
                if (string.IsNullOrEmpty(token)) return;

                socketCts = new CancellationTokenSource();
                socket = new ClientWebSocket();
                Uri uri = BuildRealtimeUri(token);
                await socket.ConnectAsync(uri, socketCts.Token);

                Debug.Log($"[Realtime] Connected: {uri.GetLeftPart(UriPartial.Path)}");
                if (!string.IsNullOrEmpty(room))
                {
                    JoinRoom(room);
                }
                _ = ReceiveLoopAsync(socket, socketCts.Token);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Realtime] Connect failed: {e.Message}");
                nextReconnectAt = Time.unscaledTime + reconnectDelaySec;
                await DisconnectAsync();
            }
            finally
            {
                connectInProgress = false;
            }
        }

        private async Task DisconnectAsync()
        {
            currentRoom = "";
            serverSelfId = "";
            ClearRemotePlayers();

            try
            {
                socketCts?.Cancel();
                if (socket != null && socket.State == WebSocketState.Open)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "client disconnect", CancellationToken.None);
                }
            }
            catch
            {
                // Best-effort cleanup; network teardown can fail during scene/app shutdown.
            }
            finally
            {
                socket?.Dispose();
                socket = null;
                socketCts?.Dispose();
                socketCts = null;
            }
        }

        private Uri BuildRealtimeUri(string token)
        {
            string baseUrl = BackendConfig.Active != null ? BackendConfig.Active.baseUrl.TrimEnd('/') : "http://localhost:3000";
            if (baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = "wss://" + baseUrl.Substring("https://".Length);
            }
            else if (baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = "ws://" + baseUrl.Substring("http://".Length);
            }

            return new Uri($"{baseUrl}/realtime?token={Uri.EscapeDataString(token)}");
        }

        private void JoinRoom(string room)
        {
            if (!IsConnected || string.IsNullOrEmpty(room)) return;
            currentRoom = room;
            _ = SendJsonAsync(new
            {
                type = "join",
                room,
                name = GetLocalName(),
                gender = GetLocalGender(),
            });
        }

        private void LeaveRoom()
        {
            if (!IsConnected || string.IsNullOrEmpty(currentRoom)) return;
            _ = SendJsonAsync(new { type = "leave" });
            currentRoom = "";
            ClearRemotePlayers();
        }

        private async Task SendJsonAsync(object payload)
        {
            ClientWebSocket activeSocket = socket;
            CancellationTokenSource activeCts = socketCts;
            if (activeSocket == null || activeCts == null || activeSocket.State != WebSocketState.Open) return;

            try
            {
                string json = JsonConvert.SerializeObject(payload);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await sendLock.WaitAsync(activeCts.Token);
                try
                {
                    if (activeSocket.State == WebSocketState.Open && !activeCts.IsCancellationRequested)
                    {
                        await activeSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, activeCts.Token);
                    }
                }
                finally
                {
                    sendLock.Release();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Realtime] Send failed: {e.Message}");
                nextReconnectAt = Time.unscaledTime + reconnectDelaySec;
                await DisconnectAsync();
            }
        }

        private async Task ReceiveLoopAsync(ClientWebSocket activeSocket, CancellationToken token)
        {
            byte[] buffer = new byte[8192];

            try
            {
                while (activeSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var builder = new StringBuilder();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await activeSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                        if (result.MessageType == WebSocketMessageType.Close) return;
                        builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    while (!result.EndOfMessage);

                    string json = builder.ToString();
                    mainThreadQueue.Enqueue(() => HandleIncoming(json));
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on disconnect.
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Realtime] Receive failed: {e.Message}");
            }
            finally
            {
                mainThreadQueue.Enqueue(() =>
                {
                    if (socket == activeSocket)
                    {
                        _ = DisconnectAsync();
                        nextReconnectAt = Time.unscaledTime + reconnectDelaySec;
                    }
                });
            }
        }

        private void SendLocalState()
        {
            var player = ResolveLocalPlayerForState();
            if (player == null) return;

            Vector3 pos = player.transform.position;
            float now = Time.unscaledTime;
            float dt = Mathf.Max(0.001f, now - lastSampleTime);
            float speed = (pos - lastSamplePosition).magnitude / dt;
            lastSamplePosition = pos;
            lastSampleTime = now;

            string animationName = GetLocalLocomotionAnimation(player, speed);
            _ = SendJsonAsync(new
            {
                type = "player_state",
                position = new { x = pos.x, y = pos.y, z = pos.z },
                yaw = player.transform.eulerAngles.y,
                animation = animationName,
            });
        }

        private void HandleIncoming(string json)
        {
            JObject msg;
            try
            {
                msg = JObject.Parse(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Realtime] Bad JSON from server: {e.Message}");
                return;
            }

            string type = msg.Value<string>("type") ?? "";
            switch (type)
            {
                case "connected":
                    break;
                case "welcome":
                    currentRoom = msg.Value<string>("room") ?? currentRoom;
                    serverSelfId = msg.Value<string>("selfId") ?? serverSelfId;
                    Debug.Log($"[Realtime] Joined {currentRoom}: selfId={serverSelfId}, authUserId={AuthService.Instance?.UserId}");
                    SyncInitialPlayers(msg["players"] as JArray);
                    break;
                case "player_joined":
                    SpawnOrUpdateRemote(msg["player"] as JObject);
                    break;
                case "player_left":
                    RemoveRemote(msg.Value<string>("playerId"));
                    break;
                case "player_state":
                    SpawnOrUpdateRemote(msg);
                    break;
                case "chat":
                    ShowChat(msg);
                    break;
                case "emote":
                    PlayRemoteEmote(msg);
                    break;
                case "error":
                    Debug.LogWarning($"[Realtime] Server error: {msg.Value<string>("code")} {msg.Value<string>("message")}");
                    break;
            }
        }

        private void SyncInitialPlayers(JArray players)
        {
            if (players == null) return;
            foreach (JObject player in players)
            {
                SpawnOrUpdateRemote(player);
            }
        }

        private void SpawnOrUpdateRemote(JObject data)
        {
            if (data == null) return;

            string playerId = data.Value<string>("playerId") ?? "";
            if (string.IsNullOrEmpty(playerId) || IsSelf(playerId)) return;

            string name = data.Value<string>("name") ?? "Player";
            string gender = data.Value<string>("gender") ?? "male";
            Vector3 position = ParsePosition(data["position"] as JObject);
            float yaw = data.Value<float?>("yaw") ?? 0f;
            string animation = data.Value<string>("animation") ?? "Idle";

            if (!remotePlayers.TryGetValue(playerId, out var remote) || remote == null)
            {
                GameObject go = CreateRemotePlayerObject(playerId, name, gender, position, yaw);
                if (go == null) return;
                remote = go.GetComponent<RemotePlayerController>();
                remotePlayers[playerId] = remote;
            }

            remote.ApplySnapshot(position, yaw, animation);
        }

        private GameObject CreateRemotePlayerObject(string playerId, string name, string gender, Vector3 position, float yaw)
        {
            GameObject prefab = null;
            if (GameManager.Instance != null)
            {
                prefab = gender == "female" ? GameManager.Instance.femalePrefab : GameManager.Instance.malePrefab;
            }

            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            PlayerController localPlayerBeforeClone = ResolveLocalPlayerForState();
            GameObject stagingRoot = null;
            GameObject go;
            if (prefab != null)
            {
                stagingRoot = new GameObject("RemotePlayer_Staging");
                stagingRoot.SetActive(false);
                go = Instantiate(prefab, position, rotation, stagingRoot.transform);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.SetPositionAndRotation(position, rotation);
            }

            go.name = $"RemotePlayer_{name}_{playerId}";
            go.tag = "Untagged";

            RestoreLocalPlayerInstance(localPlayerBeforeClone, go);
            HideRemoteEquipmentVisuals(go);
            DisableRemoteGameplayComponents(go);

            foreach (var characterController in go.GetComponentsInChildren<CharacterController>(true)) characterController.enabled = false;
            foreach (var collider in go.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            foreach (var animator in go.GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
            foreach (var camera in go.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            foreach (var listener in go.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;

            if (stagingRoot != null)
            {
                go.transform.SetParent(null, true);
                go.transform.SetPositionAndRotation(position, rotation);
                Destroy(stagingRoot);
            }

            var remote = go.GetComponent<RemotePlayerController>();
            if (remote == null) remote = go.AddComponent<RemotePlayerController>();
            remote.Initialize(playerId, name);
            return go;
        }

        private PlayerController ResolveLocalPlayerForState()
        {
            var instance = PlayerController.Instance;
            if (IsValidLocalStateSource(instance)) return instance;

            var taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null &&
                taggedPlayer.TryGetComponent(out PlayerController taggedController) &&
                IsValidLocalStateSource(taggedController))
            {
                if (instance != null && instance != taggedController)
                    Debug.LogWarning($"[Realtime] Ignoring invalid PlayerController.Instance '{instance.name}', using tagged local player '{taggedController.name}'.");
                return taggedController;
            }

            foreach (var candidate in FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!IsValidLocalStateSource(candidate)) continue;
                if (instance != null && instance != candidate)
                    Debug.LogWarning($"[Realtime] Ignoring invalid PlayerController.Instance '{instance.name}', using local player '{candidate.name}'.");
                return candidate;
            }

            return null;
        }

        private static bool IsValidLocalStateSource(PlayerController player)
        {
            if (player == null || !player.isActiveAndEnabled) return false;
            if (player.GetComponent<RemotePlayerController>() != null ||
                player.GetComponentInParent<RemotePlayerController>() != null)
                return false;
            if (player.name.StartsWith("RemotePlayer_", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!player.gameObject.scene.IsValid()) return false;

            return player.CompareTag("Player") || player.GetComponent<PlayerInput>() != null;
        }

        private static string GetLocalLocomotionAnimation(PlayerController player, float speed)
        {
            if (player == null) return "Idle";
            if (speed <= 0.15f) return string.IsNullOrWhiteSpace(player.animIdle) ? "Idle" : player.animIdle;
            if (player.IsSprintActive()) return string.IsNullOrWhiteSpace(player.animRun) ? "Run" : player.animRun;
            return string.IsNullOrWhiteSpace(player.animWalk) ? "Walk" : player.animWalk;
        }

        private static void HideRemoteEquipmentVisuals(GameObject remoteRoot)
        {
            foreach (var equipment in remoteRoot.GetComponentsInChildren<YWonderLand.Player.EquipmentManager>(true))
            {
                SetInactive(equipment.axeModel);
                SetInactive(equipment.wateringCanModel);
                SetInactive(equipment.fishingRodModel);
                SetInactive(equipment.hoeModel);
                SetInactive(equipment.seedBagModel);
                SetInactive(equipment.pickaxeModel);
                SetInactive(equipment.feedModel);
                SetInactive(equipment.hammerModel);
            }

            foreach (var fishingLine in remoteRoot.GetComponentsInChildren<YWonderLand.Environment.FishingLineController>(true))
            {
                if (fishingLine.line != null) SetInactive(fishingLine.line.gameObject);
                if (fishingLine.bobber != null) SetInactive(fishingLine.bobber.gameObject);
            }

            foreach (var child in remoteRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child != remoteRoot.transform && RemoteToolObjectNames.Contains(child.name))
                    SetInactive(child.gameObject);
            }
        }

        private static void RestoreLocalPlayerInstance(PlayerController localPlayer, GameObject remoteRoot)
        {
            foreach (var remotePlayer in remoteRoot.GetComponentsInChildren<PlayerController>(true))
            {
                remotePlayer.enabled = false;
            }

            if (localPlayer == null || PlayerController.Instance == localPlayer) return;

            var property = typeof(PlayerController).GetProperty(
                "Instance",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var setter = property?.GetSetMethod(true);
            if (setter != null)
            {
                setter.Invoke(null, new object[] { localPlayer });
            }
            else
            {
                var backingField = typeof(PlayerController).GetField(
                    "<Instance>k__BackingField",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                backingField?.SetValue(null, localPlayer);
            }

            Debug.LogWarning("[Realtime] Remote clone attempted to replace PlayerController.Instance; restored local player instance.");
        }

        private static void SetInactive(GameObject go)
        {
            if (go != null) go.SetActive(false);
        }

        private static void DisableRemoteGameplayComponents(GameObject remoteRoot)
        {
            foreach (var behaviour in remoteRoot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                if (behaviour is RemotePlayerController) continue;
                if (behaviour is FloatingNameTag) continue;
                behaviour.enabled = false;
            }
        }

        private void PlayRemoteEmote(JObject data)
        {
            string playerId = data.Value<string>("playerId") ?? "";
            if (string.IsNullOrEmpty(playerId) || IsSelf(playerId)) return;

            if (remotePlayers.TryGetValue(playerId, out var remote) && remote != null)
            {
                remote.PlayEmote(data.Value<string>("emote") ?? "", data.Value<float?>("duration") ?? 2f);
            }
        }

        private void ShowChat(JObject data)
        {
            if (ChatPanelController.Instance == null) return;

            string playerId = data.Value<string>("playerId") ?? "";
            string name = data.Value<string>("name") ?? "Player";
            string message = data.Value<string>("message") ?? "";
            bool self = IsSelf(playerId);
            if (self) return;

            Debug.Log($"[Realtime] Chat from {name}: {message}");
            ChatPanelController.Instance.ReceiveMessage(self ? "Bạn" : name, message, self ? "#E5A93C" : "#74B9FF");
        }

        private void RemoveRemote(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            if (!remotePlayers.TryGetValue(playerId, out var remote)) return;

            if (remote != null) Destroy(remote.gameObject);
            remotePlayers.Remove(playerId);
        }

        private void ClearRemotePlayers()
        {
            foreach (var pair in remotePlayers)
            {
                if (pair.Value != null) Destroy(pair.Value.gameObject);
            }
            remotePlayers.Clear();
        }

        private Vector3 ParsePosition(JObject pos)
        {
            if (pos == null) return Vector3.zero;
            return new Vector3(
                pos.Value<float?>("x") ?? 0f,
                pos.Value<float?>("y") ?? 0f,
                pos.Value<float?>("z") ?? 0f);
        }

        private bool IsSelf(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return false;
            if (!string.IsNullOrEmpty(serverSelfId) && playerId == serverSelfId) return true;
            return AuthService.Instance != null && playerId == AuthService.Instance.UserId;
        }

        private string GetLocalName()
        {
            if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(GameManager.Instance.playerName))
                return GameManager.Instance.playerName;
            if (AuthService.Instance != null && !string.IsNullOrWhiteSpace(AuthService.Instance.Username))
                return AuthService.Instance.Username;
            return "Player";
        }

        private string GetLocalGender()
        {
            return GameManager.Instance != null && GameManager.Instance.selectedCharacterIndex == 1 ? "female" : "male";
        }
    }
}
