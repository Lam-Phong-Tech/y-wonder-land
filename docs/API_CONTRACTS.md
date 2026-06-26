# API Contracts — Y WONDER GREEN FARM
# Cập nhật: 2026-06-16
# Backend: REST API riêng (Node/Go/Python) + DB (theo kịch bản khách)

> ⚠️ **ĐỔI HƯỚNG (16/06/2026):** Dự án dùng **REST API riêng**, KHÔNG dùng UGS.
> Phần "UGS" bên dưới giữ lại làm tham khảo cũ — KHÔNG còn áp dụng.

---

## REST API — Đợt 1 (Profile + Tutorial) — ĐÃ IMPLEMENT (server stub)

Server stub dev: `server/` (Node/Express, lưu `data.json`), mặc định `http://localhost:3000`.
Client: `Assets/_Project/Scripts/Backend/` (`ApiClient`, `AuthService`, `PlayerProfileService`). Offline-first: lỗi mạng -> fallback cache `PlayerPrefs`.

| Method | Endpoint | Body | Trả về |
|---|---|---|---|
| GET  | `/` | — | `{ ok }` (health) |
| POST | `/auth/register` | `{ username, password }` | `{ token, userId }` |
| POST | `/auth/login` | `{ username, password }` | `{ token, userId }` |
| GET  | `/player/profile` | header `Authorization: Bearer <token>` | `{ player_profile {...} }` |
| PUT  | `/player/profile` | `{ player_profile {...} }` + Bearer | `{ ok, updatedAt }` |

`player_profile`: theo `docs/DB_SCHEMA.md` + field `characterCreated` (bool, đã tạo nhân vật) và `tutorialCompleted` (bool).
**Token đợt 1:** JWT đơn giản (stub, KHÔNG production). Auth đợt 1 dùng username = tên nhân vật, mật khẩu sinh & lưu local (chưa nối UI Login — để đợt 2).

> **Lộ trình:** Đợt 2 nối UI Login/Register + Economy + Inventory; Đợt 3 Farm/Animal/Resource; Đợt 4 realtime (Photon) + Firebase push.

---

# (THAM KHẢO CŨ — UGS, KHÔNG còn áp dụng)

> **Lưu ý:** Đây là blueprint cho việc tích hợp UGS. Các service chưa tích hợp thật.
> Cập nhật file này khi implement từng service.

---

## 1. Authentication

### Initialize UGS
```csharp
// Gọi 1 lần khi app khởi động
await UnityServices.InitializeAsync();
```
- **Khi nào:** App launch, trước mọi UGS call khác
- **Error:** `ServicesInitializationException`
- **Quy tắc:** PHẢI gọi trước khi dùng bất kỳ service nào

### Sign Up (Username + Password)
```csharp
await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
```
- **Params:** username (3-20 chars), password (8+ chars, 1 uppercase, 1 number)
- **Returns:** void (player ID tự lưu trong `AuthenticationService.Instance.PlayerId`)
- **Error:** `AuthenticationException` (username taken, weak password)
- **Sau khi thành công:** Tạo `player_profile` trong Cloud Save

### Sign In (Username + Password)
```csharp
await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
```
- **Params:** username, password
- **Returns:** void
- **Error:** `AuthenticationException` (invalid credentials)
- **Sau khi thành công:** Load `player_profile` từ Cloud Save

### Sign In (Anonymous)
```csharp
await AuthenticationService.Instance.SignInAnonymouslyAsync();
```
- **Dùng khi:** Player muốn thử game trước khi tạo tài khoản
- **Lưu ý:** Data vẫn được lưu, có thể link với account sau

### Sign Out
```csharp
AuthenticationService.Instance.SignOut();
```
- **Quy tắc:** Save data trước khi sign out

### Kiểm tra trạng thái
```csharp
bool isSignedIn = AuthenticationService.Instance.IsSignedIn;
string playerId = AuthenticationService.Instance.PlayerId;
```
- **Quy tắc:** LUÔN kiểm tra `IsSignedIn` trước khi gọi service khác

---

## 2. Cloud Save

### Save Data
```csharp
var data = new Dictionary<string, object> {
    { "player_profile", profileData }
};
await CloudSaveService.Instance.Data.Player.SaveAsync(data);
```
- **Params:** Dictionary<string, object>
- **Key naming:** `snake_case` (xem DATA_SCHEMA.md)
- **Max size:** 5MB per player
- **Error:** `CloudSaveException`, `CloudSaveValidationException`

### Load Data
```csharp
var keys = new HashSet<string> { "player_profile" };
var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
```
- **Params:** HashSet<string> keys
- **Returns:** Dictionary<string, Item> (Item.Value.GetAs<T>() để deserialize)
- **Error:** `CloudSaveException` (key not found → tạo mới)

### Delete Data
```csharp
await CloudSaveService.Instance.Data.Player.DeleteAsync("key_name");
```
- **Quy tắc:** KHÔNG delete production data. Dùng `_deprecated` suffix

### Quy tắc Cloud Save
1. Auto-save mỗi 60 giây trong gameplay
2. Force save khi: thoát game, chuyển scene, mua item
3. Retry 3 lần nếu save fail, sau đó cache local
4. KHÔNG save mỗi frame — batch changes

---

## 3. Economy

### Get Player Balances
```csharp
var balances = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();
```
- **Returns:** GetBalancesResult (list of CurrencyBalance)
- **Mỗi balance có:** CurrencyId, Balance (long)

### Increment Currency
```csharp
await EconomyService.Instance.PlayerBalances.IncrementBalanceAsync("GOLD", 100);
```
- **Params:** currencyId, amount (positive = thêm, negative = trừ)
- **Quy tắc:** Validate server-side. Client KHÔNG tự thay đổi balance

### Get Virtual Items
```csharp
var items = await EconomyService.Instance.Configuration.GetCurrenciesAsync();
var purchases = await EconomyService.Instance.Configuration.GetVirtualPurchasesAsync();
```

### Make Purchase
```csharp
var result = await EconomyService.Instance.Purchases.MakeVirtualPurchaseAsync("purchase_id");
```
- **Params:** purchaseId (từ Dashboard)
- **Returns:** MakeVirtualPurchaseResult (costs + rewards)
- **Error:** `EconomyException` (insufficient funds)
- **Quy tắc:** Hiển thị confirmation dialog trước khi purchase

---

## 4. Leaderboards

### Submit Score
```csharp
var result = await LeaderboardsService.Instance.AddPlayerScoreAsync("lb_level", score);
```
- **Params:** leaderboardId, score (double)
- **Returns:** LeaderboardEntry (rank, score)

### Get Top Scores
```csharp
var scores = await LeaderboardsService.Instance.GetScoresAsync("lb_level", 
    new GetScoresOptions { Limit = 10 });
```
- **Params:** leaderboardId, options (Limit, Offset)
- **Returns:** LeaderboardScoresPage (Results list)

### Get Player Rank
```csharp
var entry = await LeaderboardsService.Instance.GetPlayerScoreAsync("lb_level");
```
- **Returns:** LeaderboardEntry (rank, score, playerId)

---

## 5. Friends

### Get Friends List
```csharp
var friends = await FriendsService.Instance.GetFriendsAsync();
```

### Add Friend
```csharp
await FriendsService.Instance.AddFriendAsync(playerId);
```
- **Lưu ý:** Gửi friend request, cần người kia accept

### Remove Friend
```csharp
await FriendsService.Instance.DeleteFriendAsync(playerId);
```

### Set Presence (Online Status)
```csharp
await FriendsService.Instance.SetPresenceAsync(Availability.Online);
```

---

## 6. Lobby

### Create Lobby
```csharp
var options = new CreateLobbyOptions {
    MaxPlayers = 4,
    IsPrivate = false,
    Data = new Dictionary<string, DataObject> {
        { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "farming") }
    }
};
var lobby = await LobbyService.Instance.CreateLobbyAsync("Room Name", 4, options);
```

### Join Lobby
```csharp
var lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
// hoặc
var lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
```

### Leave Lobby
```csharp
await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
```

### Query Lobbies
```csharp
var query = await LobbyService.Instance.QueryLobbiesAsync();
```

### Heartbeat (giữ lobby alive)
```csharp
// Host phải gọi mỗi 15 giây, nếu không lobby bị xóa
await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
```
- **Quy tắc:** Dùng coroutine/InvokeRepeating cho heartbeat

---

## 7. Analytics

### Send Custom Event
```csharp
AnalyticsService.Instance.CustomData("quest_completed", new Dictionary<string, object> {
    { "questId", "quest_001" },
    { "timeSpent", 120.5f },
    { "playerLevel", 5 }
});
AnalyticsService.Instance.Flush(); // Gửi ngay
```

### Event Naming Convention
| Event Name | Khi nào | Params |
|---|---|---|
| `player_login` | Đăng nhập thành công | method (password/anonymous) |
| `level_up` | Lên level | oldLevel, newLevel |
| `quest_completed` | Hoàn thành quest | questId, timeSpent |
| `item_purchased` | Mua item | itemId, currencyType, amount |
| `session_start` | Bắt đầu session | — |
| `session_end` | Kết thúc session | duration |

---

## 8. Push Notifications

### Register for Notifications
```csharp
await PushNotificationsService.Instance.RegisterForPushNotificationsAsync();
```

### Handle Notification
```csharp
PushNotificationsService.Instance.OnNotificationReceived += notification => {
    Debug.Log($"Notification: {notification.Title} - {notification.Body}");
};
```

- **Quy tắc:** KHÔNG spam notifications. Max 1 push/ngày cho marketing

---

## Error Handling Pattern

Tất cả UGS call PHẢI tuân theo pattern này:

```csharp
try
{
    await SomeUGSService.Instance.SomeMethodAsync();
}
catch (AuthenticationException ex)
{
    Debug.LogError($"[Auth] {ex.Message}");
    // Hiển thị UI error cho player
}
catch (RequestFailedException ex)
{
    Debug.LogError($"[Network] {ex.Message} (Code: {ex.ErrorCode})");
    // Retry hoặc chuyển offline mode
}
catch (Exception ex)
{
    Debug.LogError($"[Unexpected] {ex.Message}");
}
```
