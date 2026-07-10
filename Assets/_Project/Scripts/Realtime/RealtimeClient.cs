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
        [Serializable]
        public sealed class ResourceReward
        {
            public string itemId;
            public string displayName;
            public string kind;
            public int quantity;
        }

        public sealed class ResourceHarvestResult
        {
            public bool accepted;
            public string code;
            public string resourceId;
            public string resourceType;
            public int miningTurnsRemaining = -1;
            public readonly List<ResourceReward> rewards = new List<ResourceReward>();
        }

        private sealed class PendingResourceHarvest
        {
            public string resourceId;
            public float expiresAt;
            public Action<ResourceHarvestResult> callback;
        }

        private sealed class SharedResourceState
        {
            public string resourceId;
            public string resourceType;
            public Vector3 position;
            public bool available;
            public float respawnInSec;
            public int cycle;
            public string updatedAt;
        }

        public static RealtimeClient Instance { get; private set; }

        private static readonly HashSet<string> SharedRooms = new HashSet<string> { "city", "mine" };

        [Header("Connection")]
        [SerializeField] private float reconnectDelaySec = 5f;
        [SerializeField] private float stateSendIntervalSec = 0.15f;
        [SerializeField] private float resourceManifestIntervalSec = 1.5f;
        [SerializeField] private float resourceManifestRadius = 250f;
        [SerializeField] private float resourceRequestTimeoutSec = 8f;

        private ClientWebSocket socket;
        private CancellationTokenSource socketCts;
        private bool connectInProgress;
        private bool sessionReplacementHandled;
        private string currentRoom = "";
        private string serverSelfId = "";
        private float nextConnectionCheck;
        private float nextReconnectAt;
        private float nextStateSendAt;
        private float nextResourceManifestAt;
        private Vector3 lastSamplePosition;
        private float lastSampleTime;
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

        private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
        private readonly Dictionary<string, RemotePlayerController> remotePlayers = new Dictionary<string, RemotePlayerController>();
        private readonly Dictionary<string, YWonderLand.Environment.HarvestableResource> sharedResources =
            new Dictionary<string, YWonderLand.Environment.HarvestableResource>();
        private readonly Dictionary<int, string> resourceIdsByInstance = new Dictionary<int, string>();
        private readonly Dictionary<string, SharedResourceState> sharedResourceStates =
            new Dictionary<string, SharedResourceState>();
        private readonly Dictionary<int, string> appliedResourceVersions = new Dictionary<int, string>();
        private readonly Dictionary<string, PendingResourceHarvest> pendingResourceHarvests =
            new Dictionary<string, PendingResourceHarvest>();
        private readonly Dictionary<string, string> pendingRequestByResource = new Dictionary<string, string>();
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
        public bool RequiresServerResourceSync
        {
            get
            {
                string desiredRoom = GetDesiredRoom();
                return !string.IsNullOrEmpty(desiredRoom)
                    && AuthService.Instance != null
                    && AuthService.Instance.IsSignedIn;
            }
        }
        public bool CanUseServerResourceSync => RequiresServerResourceSync && IsConnected && !string.IsNullOrEmpty(currentRoom);

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

            if (CanUseServerResourceSync && Time.unscaledTime >= nextResourceManifestAt)
            {
                nextResourceManifestAt = Time.unscaledTime + Mathf.Max(0.5f, resourceManifestIntervalSec);
                SendResourceManifest();
            }

            TickPendingResourceHarvests();
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

        public bool TryRequestResourceHarvest(
            YWonderLand.Environment.HarvestableResource resource,
            Action<ResourceHarvestResult> callback)
        {
            if (resource == null || !CanUseServerResourceSync)
            {
                callback?.Invoke(new ResourceHarvestResult
                {
                    accepted = false,
                    code = "REALTIME_DISCONNECTED",
                });
                return false;
            }

            string resourceId = RegisterSharedResource(resource);
            if (string.IsNullOrEmpty(resourceId))
            {
                callback?.Invoke(new ResourceHarvestResult
                {
                    accepted = false,
                    code = "RESOURCE_ID_MISSING",
                });
                return false;
            }

            if (pendingRequestByResource.ContainsKey(resourceId)) return true;

            string requestId = Guid.NewGuid().ToString("N");
            pendingResourceHarvests[requestId] = new PendingResourceHarvest
            {
                resourceId = resourceId,
                expiresAt = Time.unscaledTime + Mathf.Max(2f, resourceRequestTimeoutSec),
                callback = callback,
            };
            pendingRequestByResource[resourceId] = requestId;
            _ = SendResourceHarvestAsync(resource, resourceId, requestId);
            return true;
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

                sessionReplacementHandled = false;
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
            ClearSharedResourceSession();

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
            ClearSharedResourceSession();
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
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            int closeCode = result.CloseStatus.HasValue ? (int)result.CloseStatus.Value : 0;
                            string closeReason = result.CloseStatusDescription ?? "";
                            if (closeCode == 4008 ||
                                closeReason.Equals("SESSION_REPLACED", StringComparison.OrdinalIgnoreCase))
                            {
                                mainThreadQueue.Enqueue(HandleSessionReplaced);
                            }
                            return;
                        }
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

            string animationName = GetLocalAnimation(player, speed);
            _ = SendJsonAsync(new
            {
                type = "player_state",
                position = new { x = pos.x, y = pos.y, z = pos.z },
                yaw = player.transform.eulerAngles.y,
                animation = animationName,
                animationSpeed = player.CurrentAnimationSpeed,
                tool = player.CurrentActionTool.ToString(),
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
                    SyncResourceSnapshot(msg["resources"] as JArray);
                    nextResourceManifestAt = 0f;
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
                case "resource_snapshot":
                    SyncResourceSnapshot(msg["resources"] as JArray);
                    break;
                case "resource_state":
                    ReceiveResourceState(msg["resource"] as JObject);
                    break;
                case "resource_harvest_result":
                    ReceiveResourceHarvestResult(msg);
                    break;
                case "error":
                    string errorCode = msg.Value<string>("code") ?? "";
                    Debug.LogWarning($"[Realtime] Server error: {errorCode} {msg.Value<string>("message")}");
                    if (errorCode.Equals("SESSION_REPLACED", StringComparison.OrdinalIgnoreCase))
                        HandleSessionReplaced();
                    break;
            }
        }

        private void SendResourceManifest()
        {
            if (!CanUseServerResourceSync) return;

            PlayerController player = ResolveLocalPlayerForState();
            if (player == null) return;

            float maxDistance = Mathf.Max(25f, resourceManifestRadius);
            float maxDistanceSqr = maxDistance * maxDistance;
            var manifest = new List<object>();
            foreach (var resource in FindObjectsByType<YWonderLand.Environment.HarvestableResource>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (resource == null || !resource.gameObject.scene.IsValid()) continue;
                if ((resource.transform.position - player.transform.position).sqrMagnitude > maxDistanceSqr) continue;

                string resourceId = RegisterSharedResource(resource);
                if (string.IsNullOrEmpty(resourceId)) continue;
                Vector3 pos = resource.transform.position;
                manifest.Add(new
                {
                    resourceId,
                    resourceType = resource.type == YWonderLand.Environment.HarvestableResource.ResourceType.Tree ? "tree" : "rock",
                    position = new { x = pos.x, y = pos.y, z = pos.z },
                });
            }

            if (manifest.Count > 0)
                _ = SendJsonAsync(new { type = "resource_manifest", resources = manifest });
        }

        private async Task SendResourceHarvestAsync(
            YWonderLand.Environment.HarvestableResource resource,
            string resourceId,
            string requestId)
        {
            Vector3 pos = resource != null ? resource.transform.position : Vector3.zero;
            string resourceType = resource != null && resource.type == YWonderLand.Environment.HarvestableResource.ResourceType.Tree
                ? "tree"
                : "rock";

            await SendJsonAsync(new
            {
                type = "resource_manifest",
                resources = new[]
                {
                    new
                    {
                        resourceId,
                        resourceType,
                        position = new { x = pos.x, y = pos.y, z = pos.z },
                    }
                },
            });
            await SendJsonAsync(new { type = "resource_harvest", requestId, resourceId });
        }

        private string RegisterSharedResource(YWonderLand.Environment.HarvestableResource resource)
        {
            if (resource == null) return "";

            int instanceId = resource.GetInstanceID();
            if (resourceIdsByInstance.TryGetValue(instanceId, out string cachedId))
            {
                sharedResources[cachedId] = resource;
                if (sharedResourceStates.TryGetValue(cachedId, out var cachedState))
                    ApplyResourceState(resource, cachedState);
                return cachedId;
            }

            var spawner = resource.GetComponentInParent<YWonderLand.Environment.ResourceSpawner>();
            string owner = spawner != null && !string.IsNullOrWhiteSpace(spawner.spawnerID)
                ? spawner.spawnerID.Trim()
                : resource.gameObject.scene.name;
            string localId = !string.IsNullOrWhiteSpace(resource.resourceId)
                ? resource.resourceId.Trim()
                : BuildHierarchyPath(resource.transform);
            string type = resource.type == YWonderLand.Environment.HarvestableResource.ResourceType.Tree ? "tree" : "rock";
            string networkId = $"{owner}:{type}:{localId}";

            if (sharedResources.TryGetValue(networkId, out var existing) && existing != null && existing != resource)
                networkId = $"{networkId}:{BuildHierarchyPath(resource.transform)}";

            resourceIdsByInstance[instanceId] = networkId;
            sharedResources[networkId] = resource;
            if (sharedResourceStates.TryGetValue(networkId, out var state))
                ApplyResourceState(resource, state);
            return networkId;
        }

        private static string BuildHierarchyPath(Transform target)
        {
            if (target == null) return "resource";

            var parts = new List<string>();
            Transform current = target;
            while (current != null)
            {
                parts.Add($"{current.name}[{current.GetSiblingIndex()}]");
                current = current.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private void SyncResourceSnapshot(JArray resources)
        {
            if (resources == null) return;
            foreach (JToken token in resources)
            {
                ReceiveResourceState(token as JObject);
            }
        }

        private void ReceiveResourceState(JObject data)
        {
            if (data == null) return;

            string resourceId = data.Value<string>("resourceId") ?? "";
            if (string.IsNullOrEmpty(resourceId)) return;

            var state = new SharedResourceState
            {
                resourceId = resourceId,
                resourceType = data.Value<string>("resourceType") ?? "rock",
                position = ParsePosition(data["position"] as JObject),
                available = data.Value<bool?>("available") ?? true,
                respawnInSec = data.Value<float?>("respawnInSec") ?? 0f,
                cycle = data.Value<int?>("cycle") ?? 0,
                updatedAt = data.Value<string>("updatedAt") ?? "",
            };
            sharedResourceStates[resourceId] = state;

            if (sharedResources.TryGetValue(resourceId, out var resource) && resource != null)
                ApplyResourceState(resource, state);
        }

        private void ApplyResourceState(
            YWonderLand.Environment.HarvestableResource resource,
            SharedResourceState state)
        {
            if (resource == null || state == null) return;

            int instanceId = resource.GetInstanceID();
            string version = $"{currentRoom}|{state.cycle}|{state.available}|{state.updatedAt}";
            if (appliedResourceVersions.TryGetValue(instanceId, out string applied) && applied == version) return;
            appliedResourceVersions[instanceId] = version;

            resource.transform.position = state.position;
            if (state.available)
            {
                resource.RestoreState(0f, 0.0);
                return;
            }

            resource.CancelHarvest();
            resource.RestoreState(Mathf.Max(0.1f, state.respawnInSec), 0.0);
            resource.respawnTimer = 0f;
            YWonderLand.Environment.FarmInteractionController.Instance?.OnSharedResourceUnavailable(resource);
        }

        private void ReceiveResourceHarvestResult(JObject data)
        {
            if (data == null) return;

            ApplyServerInventory(data["inventory"] as JObject);
            if (data["resource"] is JObject resourceState)
                ReceiveResourceState(resourceState);

            string requestId = data.Value<string>("requestId") ?? "";
            if (string.IsNullOrEmpty(requestId) || !pendingResourceHarvests.TryGetValue(requestId, out var pending))
                return;

            var result = new ResourceHarvestResult
            {
                accepted = data.Value<bool?>("accepted") ?? false,
                code = data.Value<string>("code") ?? "RESOURCE_REWARD_FAILED",
                resourceId = data.Value<string>("resourceId") ?? pending.resourceId,
                resourceType = data.Value<string>("resourceType") ?? "",
                miningTurnsRemaining = data["limit"]?.Value<int?>("remaining") ?? -1,
            };
            if (data["rewards"] is JArray rewards)
            {
                foreach (JObject reward in rewards)
                {
                    result.rewards.Add(new ResourceReward
                    {
                        itemId = reward.Value<string>("itemId") ?? "",
                        displayName = reward.Value<string>("displayName") ?? "",
                        kind = reward.Value<string>("kind") ?? "resource",
                        quantity = reward.Value<int?>("quantity") ?? 0,
                    });
                }
            }

            CompletePendingResourceHarvest(requestId, pending, result);
        }

        private static void ApplyServerInventory(JObject inventory)
        {
            if (inventory == null || YWonderLand.Managers.InventoryManager.Instance == null) return;

            int maxSlots = inventory.Value<int?>("maxSlots") ?? 50;
            var slots = new List<YWonderLand.Managers.InventorySlot>();
            if (inventory["slots"] is JArray incomingSlots)
            {
                foreach (JObject slot in incomingSlots)
                {
                    string itemId = slot.Value<string>("itemId") ?? "";
                    int quantity = slot.Value<int?>("quantity") ?? 0;
                    if (!string.IsNullOrWhiteSpace(itemId) && quantity > 0)
                        slots.Add(new YWonderLand.Managers.InventorySlot(itemId, quantity));
                }
            }
            YWonderLand.Managers.InventoryManager.Instance.ApplyServerState(maxSlots, slots);
        }

        private void TickPendingResourceHarvests()
        {
            if (pendingResourceHarvests.Count == 0) return;

            var expired = new List<string>();
            foreach (var pair in pendingResourceHarvests)
            {
                if (Time.unscaledTime >= pair.Value.expiresAt) expired.Add(pair.Key);
            }

            foreach (string requestId in expired)
            {
                if (!pendingResourceHarvests.TryGetValue(requestId, out var pending)) continue;
                CompletePendingResourceHarvest(requestId, pending, new ResourceHarvestResult
                {
                    accepted = false,
                    code = "RESOURCE_REQUEST_TIMEOUT",
                    resourceId = pending.resourceId,
                });
            }
        }

        private void CompletePendingResourceHarvest(
            string requestId,
            PendingResourceHarvest pending,
            ResourceHarvestResult result)
        {
            pendingResourceHarvests.Remove(requestId);
            if (pending != null && !string.IsNullOrEmpty(pending.resourceId))
                pendingRequestByResource.Remove(pending.resourceId);

            try { pending?.callback?.Invoke(result); }
            catch (Exception e) { Debug.LogWarning($"[Realtime] Resource result callback failed: {e.Message}"); }
        }

        private void ClearSharedResourceSession()
        {
            foreach (var pair in new List<KeyValuePair<string, PendingResourceHarvest>>(pendingResourceHarvests))
            {
                CompletePendingResourceHarvest(pair.Key, pair.Value, new ResourceHarvestResult
                {
                    accepted = false,
                    code = "REALTIME_DISCONNECTED",
                    resourceId = pair.Value.resourceId,
                });
            }
            sharedResources.Clear();
            resourceIdsByInstance.Clear();
            sharedResourceStates.Clear();
            appliedResourceVersions.Clear();
            pendingRequestByResource.Clear();
        }

        private void HandleSessionReplaced()
        {
            if (sessionReplacementHandled) return;
            sessionReplacementHandled = true;
            nextReconnectAt = float.PositiveInfinity;

            Debug.LogWarning("[Realtime] Session replaced by a newer login. Signing out this client.");
            _ = DisconnectAsync();

            if (GameManager.Instance != null)
                GameManager.Instance.LogoutToLogin();
            else
                AuthService.Instance?.SignOut();

            YWonderLand.Environment.ScreenToast.Show(
                "Tài khoản đã đăng nhập ở thiết bị khác. Phiên này đã được đăng xuất.",
                6f);
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
            float animationSpeed = data.Value<float?>("animationSpeed") ?? 1f;
            string tool = data.Value<string>("tool") ?? "None";

            if (!remotePlayers.TryGetValue(playerId, out var remote) || remote == null)
            {
                GameObject go = CreateRemotePlayerObject(playerId, name, gender, position, yaw);
                if (go == null) return;
                remote = go.GetComponent<RemotePlayerController>();
                remotePlayers[playerId] = remote;
            }

            remote.ApplySnapshot(position, yaw, animation, animationSpeed, tool);
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

        private static string GetLocalAnimation(PlayerController player, float speed)
        {
            if (player == null) return "Idle";

            string currentState = player.CurrentAnimationState;
            if (!string.IsNullOrWhiteSpace(currentState)) return currentState;

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
