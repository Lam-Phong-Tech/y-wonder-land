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
    /// Farm remains private; the client connects only while the player is in city or mine.
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
        private float nextConnectionCheck;
        private float nextReconnectAt;
        private float nextStateSendAt;
        private Vector3 lastSamplePosition;
        private float lastSampleTime;
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

        private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
        private readonly Dictionary<string, RemotePlayerController> remotePlayers = new Dictionary<string, RemotePlayerController>();

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
            if (!IsConnected || string.IsNullOrEmpty(currentRoom) || string.IsNullOrWhiteSpace(message)) return false;
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
            bool shouldConnect = !string.IsNullOrEmpty(desiredRoom)
                                 && AuthService.Instance != null
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

            if (IsConnected && currentRoom != desiredRoom)
            {
                JoinRoom(desiredRoom);
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
                JoinRoom(room);
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
            var player = PlayerController.Instance;
            if (player == null) return;

            Vector3 pos = player.transform.position;
            float now = Time.unscaledTime;
            float dt = Mathf.Max(0.001f, now - lastSampleTime);
            float speed = (pos - lastSamplePosition).magnitude / dt;
            lastSamplePosition = pos;
            lastSampleTime = now;

            string animationName = speed > 0.15f ? (player.IsSprintActive() ? "Run" : "Walk") : "Idle";
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

            GameObject go = prefab != null
                ? Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f))
                : GameObject.CreatePrimitive(PrimitiveType.Capsule);

            go.name = $"RemotePlayer_{name}_{playerId}";
            go.tag = "Untagged";

            foreach (var input in go.GetComponentsInChildren<PlayerInput>(true)) input.enabled = false;
            foreach (var controller in go.GetComponentsInChildren<PlayerController>(true)) controller.enabled = false;
            foreach (var characterController in go.GetComponentsInChildren<CharacterController>(true)) characterController.enabled = false;
            foreach (var collider in go.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            var remote = go.GetComponent<RemotePlayerController>();
            if (remote == null) remote = go.AddComponent<RemotePlayerController>();
            remote.Initialize(playerId, name);
            return go;
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
