# API Contracts — Y WONDER GREEN FARM
# Cập nhật: 2026-07-10
# Backend: REST API riêng (Node/Go/Python) + DB (theo kịch bản khách)

> ⚠️ **ĐỔI HƯỚNG (16/06/2026):** Dự án dùng **REST API riêng**, KHÔNG dùng UGS.
> Phần "UGS" bên dưới giữ lại làm tham khảo cũ — KHÔNG còn áp dụng.

---

## REST API — Đợt 1 (Profile + Tutorial) — ĐÃ IMPLEMENT (server stub)

Server stub dev: `server/` (Node/Express, lưu `data.json`), mặc định `http://localhost:3000`.
Client: `Assets/_Project/Scripts/Backend/` (`ApiClient`, `AuthService`, `PlayerProfileService`). Offline-first: lỗi mạng -> fallback cache `PlayerPrefs`.

Public demo target cho Unity: `https://api.ywonder.net/game-api`. Namespace này đi qua Nginx tới game backend PostgreSQL; `/api/game/*` vẫn thuộc web API cũ. Trong lúc test local/LAN, `BackendConfig.asset` có thể tạm trỏ `http://127.0.0.1:3000` hoặc IP LAN của máy chạy backend.
Server stub vẫn hỗ trợ cả endpoint local (`/auth/login`) và endpoint legacy có prefix (`/game-api/auth/login`) để tránh vỡ nếu sau này cần proxy qua `ywonder.net/game-api`.

| Method | Endpoint | Body | Trả về |
|---|---|---|---|
| GET  | `/` | — | `{ ok }` (health) |
| GET  | `/health` | — | `{ ok, checkedAt }` |
| POST | `/auth/register` | `{ username, password, email?, phone? }` | `{ token, userId, playerId, username, email }` |
| POST | `/auth/login` | `{ username, password }` | `{ token, userId }` |
| GET  | `/player/profile` | header `Authorization: Bearer <token>` | `{ player_profile {...} }` |
| PUT  | `/player/profile` | `{ player_profile {...} }` + Bearer | `{ ok, updatedAt }` |

`player_profile`: theo `docs/DB_SCHEMA.md` + field `characterCreated` (bool, đã tạo nhân vật) và `tutorialCompleted` (bool).
**Token MVP:** game-server phát JWT HS256 bằng secret chỉ nằm trong env VPS; production
startup gate từ chối secret ngắn/fallback. Unity lưu game token, không giữ DB/web secret.

Quy tắc account local hiện tại:
- `username`: 9-20 ký tự, chỉ chữ Latin, số và `_`, khớp validation của Unity.
- `password`: 9-20 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt.
- `email`: UI đăng ký yêu cầu và gửi lên; API vẫn để optional cho legacy/offline bridge,
  nhưng nếu có thì phải đúng định dạng và không trùng account khác.
- Auth endpoint có rate limit theo IP/tài khoản. Khi vượt giới hạn trả HTTP `429`
  với `{ error:"RATE_LIMITED", retryAfterSec }` và header `Retry-After`.

> **Lộ trình:** Đợt 2 nối UI Login/Register + Economy + Inventory; Đợt 3 Farm/Animal/Resource; Đợt 4 realtime (Photon) + Firebase push.

---

## REST API - Game backend MVP bridge (lam truoc trong luc cho Web API)

Muc tieu: game backend co contract on dinh truoc, web auth that se cam vao adapter sau. Hien `WEB_AUTH_MODE=mock` chi nen dung cho test account cap san; khi cho khach dang ky tai khoan game local trong Phase 1, dung `/auth/register` + `/auth/login` va co the set `WEB_AUTH_MODE=disabled` de khong cho mock bypass password. Khi ben web cung cap endpoint on dinh, doi sang `WEB_AUTH_MODE=http` va set `WEB_AUTH_LOGIN_URL`.

Unity login Phase 1 thu `/auth/login` truoc. Neu backend tra `404 USER_NOT_FOUND`, client moi fallback `/auth/web-login`; neu sai password cua local account thi `/auth/login` tra `401` va client khong fallback mock/web.

Tài liệu hành trình product/backend đầy đủ nằm ở `docs/WEB_GAME_BACKEND_JOURNEY.md`. Quy ước chính:
- Web là nguồn tài khoản; game backend chỉ nhận/verify account web qua adapter server-side.
- Game backend map `web_user_id -> playerId` và là nguồn dữ liệu game state.
- MVP chốt 1 account web = 1 game player/nhân vật. Khách phải có tài khoản trước khi chơi; không làm guest account trong backend thật.
- Account web `locked` hoặc `soft_deleted` phải bị game-server chặn login/gameplay online.
- Unity không gọi trực tiếp API web nội bộ và không giữ `GAME_API_SECRET`.
- Economy/inventory/daily limit/farm-state phải chuyển dần sang server-authoritative. Shop và khai thác cây/đá public đã nối lát đầu tiên; farm/crop/animal/câu cá và các reward/chi phí khác vẫn chưa hoàn tất.
- MVP sắp tới chưa làm nạp/rút. `Point` có thể là game-server currency cho demo/state sync; web wallet/top-up/spend chuyển sang phase sau. Khi sang phase tiền thật, game-server phải ghi ledger/transaction rõ ràng và gọi web wallet API server-side; Unity không được tự cộng/trừ ví nạp.
- Realtime trước mắt chỉ dành cho đảo công cộng như `city`/`mine`; farm không join room realtime công cộng. Chat là kênh toàn server cho client còn online, không phụ thuộc đang đứng cùng room.

| Method | Endpoint | Body | Tra ve |
|---|---|---|---|
| POST | `/auth/web-login` | `{ username/email/refCode, password }` hoac `{ token }` | `{ token, playerId, webUserId, player_profile }` |
| POST | `/auth/browser/start` | `{ code_challenge, intent:"login"|"register" }` | `{ requestId, authUrl, expiresInSec, pollIntervalMs }` |
| POST | `/auth/browser/approve` | `{ requestId, webUser }` + server Bearer secret | `{ ok, duplicate }`; chỉ callback web gọi qua loopback |
| POST | `/auth/browser/exchange` | `{ requestId, code_verifier }` | `{ token, playerId, webUserId, player_profile, status:"complete" }` hoặc `202 pending` |
| GET | `/player/bootstrap` | Bearer token | `{ player_profile, economy, inventory, farm_state, daily_limits }` |
| GET | `/player/economy` | Bearer token | `{ economy }` |
| PUT | `/player/economy` | `{ economy }` + Bearer | `{ ok, economy }` |
| POST | `/player/economy/apply` | `{ delta_pos, delta_upos, type, ref, idempotency_key }` + Bearer | `{ ok, economy, transaction }` |
| GET | `/player/inventory` | Bearer token | `{ inventory }` |
| PUT | `/player/inventory` | `{ inventory }` + Bearer | `{ ok, inventory }` |
| POST | `/player/inventory/adjust` | `{ item_id, quantity_delta, type, ref, idempotency_key }` + Bearer | `{ ok, inventory, transaction }` |
| POST | `/player/shop/transaction` | `{ shop_id, mode:"buy"|"sell", item_id, quantity, idempotency_key }` + Bearer | `{ ok, economy, inventory, transaction, duplicate }` |
| GET | `/player/daily-limits` | Bearer token | `{ daily_limits }` |
| POST | `/player/daily-limits/consume` | `{ limit_key, amount, max_count, period_key, type, ref, idempotency_key }` + Bearer | `{ ok, daily_limits, limit, transaction }` |
| GET | `/player/farm-state` | Bearer token | `{ farm_state }` |
| PUT | `/player/farm-state` | `{ farm_state }` + Bearer | `{ ok, farm_state }` |

Quy uoc mapping:
- `web_user_id` la ID on dinh do web tra ve.
- `playerId` la ID noi bo game backend.
- DB game rieng luu `web_user_id -> playerId`, khong ghi truc tiep vao DB web.
- `web_user_id` phai unique trong `game_players` de dam bao 1 web account = 1 game player.
- Game-server phai kiem tra account status tu web: `active`, `locked`, `soft_deleted` hoac field tuong duong.
- MVP online/realtime chua lam nap/rut va chua can web wallet. Phase sau: tien nap tu web phai di qua web wallet API; `Point` la tien trong game va tien nap, con can chot `UPoint` dung lam gi va web hay game-server la ledger cuoi cung cua Point.
- `STORE_MODE=json` là mặc định dev/local. Từ 11/07/2026, `STORE_MODE=postgres` đã có driver `pg`, migration versioned và query thật cho account/profile/economy/inventory/farm/daily-limit/transactions; cùng bộ Phase 1 smoke đã pass trên PostgreSQL test thật. Production DB/backup/deploy vẫn chưa hoàn tất; xem `docs/POSTGRESQL_PHASE2_RUNBOOK.md`.
- Production startup gate bắt buộc loopback bind, PostgreSQL, secret JWT dài, tắt
  dashboard/demo seed và cấm `WEB_AUTH_MODE=mock`. HTTP có request ID, body limit,
  security headers và structured access log không chứa body/token. Realtime giới hạn
  tổng connection, payload và message rate; các ngưỡng được cấu hình bằng env.
- Khi `WEB_AUTH_MODE=http`, startup gate còn bắt buộc `WEB_AUTH_LOGIN_URL` dùng HTTPS
  và có `WEB_AUTH_SECRET`/`GAME_API_SECRET` tối thiểu 16 ký tự. Giai đoạn
  `AUTH_TRANSITION_MODE=parallel` bắt buộc `LOCAL_REGISTRATION_ENABLED=true`; chỉ
  khi cutover `web-primary` mới bắt buộc false và `/auth/register` trả `403
  LOCAL_REGISTRATION_DISABLED`. Account web khóa/xóa mềm/inactive bị từ chối trước
  khi mapping player; lỗi upstream được chuẩn hóa, không chuyển message nội bộ của web
  về Unity.
- Khi bật `BROWSER_AUTH_ENABLED`, login/callback phải là HTTPS cùng origin,
  callback cố định `/api/game/browser/callback`; request ID lưu hash SHA-256, hạn 10
  phút, exchange bắt buộc PKCE và chỉ dùng một lần. Callback Next.js đọc session web
  nhưng không được auto-approve session đã ghi nhớ. Người dùng phải xác nhận tài
  khoản hiện tại hoặc chọn đăng nhập account khác; lựa chọn đổi account chỉ expire
  cookie session-token Auth.js/NextAuth và giữ callback qua `/vi/login`. Chỉ thao
  tác xác nhận mới approve qua `127.0.0.1`; callback/redirect phải `no-store` và
  Unity không nhận cookie/password/secret web.
- `daily_limits` mac dinh gom `fishing` va `mining`, reset theo `period_key` ngay server dang `YYYY-MM-DD`. Can chot timezone server; khuyen nghi `Asia/Saigon` cho khach VN. Hien stub cu co the dang dung UTC nen can doi/ghi ro truoc production.
- `idempotency_key` phai duy nhat cho moi action co retry; server tra `duplicate=true` khi nhan lai cung key va khong apply them tien/item/luot.
- Shop transaction khong nhan/gia tin `unit_price` tu Unity. Server tra `shopCatalog.json` sinh tu `ItemDefinition` + `ShopDefinition`, kiem access mode/whitelist/canSell va doi Point + inventory trong mot lan ghi. Cung key nhung body khac tra `IDEMPOTENCY_CONFLICT`.
- Moi khi sua gia hoac danh sach shop trong Unity, chay `npm.cmd run catalog:generate --prefix server` va commit `server/shopCatalog.json` cung thay doi data.

### Web auth contract do web team bàn giao

Game-server gọi web, Unity KHÔNG gọi trực tiếp và KHÔNG giữ `GAME_API_SECRET`.

| Method | Endpoint | Header | Body | Trả về |
|---|---|---|---|---|
| POST | `https://ywonder.net/api/game/auth` | `Authorization: Bearer <GAME_API_SECRET>` | `{ "username": "<username|email|phone|refCode>", "password": "..." }` | `{ ok, userId, user_id, username, refCode, ref_code, fullName, full_name, status, locked, softDeleted, gameToken, game_token, tokenType, expiresIn, expires_in }` |
| GET | `https://ywonder.net/api/game/balance?uid=<username>` | `Authorization: Bearer <GAME_API_SECRET>` | — | Web Point balance |
| POST | `https://ywonder.net/api/game/credit` | `Authorization: Bearer <GAME_API_SECRET>` | `{ "uid":"<username>", "amount": number, "ref":"<event id>", "reason":"..." }` | Web credit result |

Cập nhật bàn giao 09/07/2026 từ chat 01/07:
- Endpoint web auth đang dùng được ngay qua SSL hợp lệ là `POST https://ywonder.net/api/game/auth`.
- `api.ywonder.net` đã được cấu hình nhưng SSL/public routing đang kẹt ở lớp hạ tầng/WAF/default-server; không chặn việc nối game nếu game-server tạm gọi `ywonder.net/api/game/auth`.
- `GAME_API_SECRET` nằm ở server/.env hoặc do owner cấp riêng; tuyệt đối không đưa vào Unity, không commit vào repo.
- Đã có test account web `gametest`; mật khẩu lưu riêng ngoài repo.
- `gameToken` là JWT HS256 chuẩn, payload có `{ sub, uid, username, iat, exp }`, trong đó `sub/uid = web userId`.

Điểm cần xin/chốt thêm với web team:
- MVP online/realtime chưa cần endpoint ví nạp/rút.
- Phase sau mới cần endpoint trừ tiền/spend/debit hoặc reserve/capture để game-server mua vật phẩm bằng tiền nạp.
- Phase sau mọi cộng/trừ ví web phải có `ref` hoặc `idempotency_key` để retry không nhân đôi.
- Phase sau cần xác nhận `Point` là wallet chính cho cả gameplay và tiền nạp; nếu `UPoint` còn dùng, cần định nghĩa rõ loại giao dịch nào dùng `UPoint`.
- Field trạng thái account rõ ràng để game-server chặn `locked`/`soft_deleted`.
- Trang đăng ký hiện bắt nhập mã giới thiệu nhưng chưa kiểm tra mã có tồn tại. Trước
  khi public cần BA/web chốt một mã chính thức cho người chơi đến từ game hoặc sửa web
  thành mã tùy chọn; không hướng dẫn khách điền ngẫu nhiên vì có thể sai attribution.
- Endpoint lịch sử giao dịch ví để admin/sếp đối soát.

Production env for `server/webAuthProvider.js`:

```powershell
$env:WEB_AUTH_MODE="http"
$env:WEB_AUTH_LOGIN_URL="https://ywonder.net/api/game/auth"
$env:WEB_AUTH_SECRET="<GAME_API_SECRET>"
```

`gameToken` format mới: JWT chuẩn HS256, payload includes `{ sub, uid, username, iat, exp }` (`sub`/`uid` = web `userId`). Game-server verifies with `jwt.verify(token, GAME_API_SECRET, { algorithms: ["HS256"] })`.
Sau khi hạ tầng `api.ywonder.net` có SSL/proxy đúng, có thể đổi `WEB_AUTH_LOGIN_URL` sang `https://api.ywonder.net/api/game/auth` mà không đổi Unity.

## Realtime WebSocket MVP

Unity connects to game-server after login:

```text
wss://api.ywonder.net/game-api/realtime?token=<game-server-jwt>
```

Legacy path if needed:

```text
wss://ywonder.net/game-api/realtime?token=<game-server-jwt>
```

Client -> server:

| Type | Payload |
|---|---|
| `join` | `{ type, room: "city"|"mine", name, gender }` |
| `player_state` | `{ type, position:{x,y,z}, yaw, animation, animationSpeed, tool }` |
| `chat` | `{ type, message }` |
| `emote` | `{ type, emote:"Waving"|"Pointing", duration }` |
| `resource_manifest` | `{ type, resources:[{ resourceId, resourceType:"tree"|"rock", position }] }` |
| `resource_harvest` | `{ type, requestId, resourceId }` |
| `leave` | `{ type }` |

Server -> client:

| Type | Payload |
|---|---|
| `connected` | `{ type, connectionId, sharedRooms, maxPlayers }` |
| `welcome` | `{ type, selfId, room, players, resources }` |
| `player_joined` | `{ type, player }` |
| `player_left` | `{ type, playerId, room }` |
| `player_state` | `{ type, playerId, name, room, gender, position, yaw, animation, animationSpeed, tool }` |
| `chat` | `{ type, playerId, name, room, message }` |
| `emote` | `{ type, playerId, name, room, emote, duration }` |
| `resource_snapshot` | `{ type, room, resources:[{ resourceId, resourceType, position, available, respawnInSec, cycle }] }` |
| `resource_state` | `{ type, resource:{ resourceId, resourceType, position, available, respawnInSec, cycle }, harvestedBy? }` |
| `resource_harvest_result` | `{ type, requestId, resourceId, accepted, code, rewards, inventory, daily_limits, limit }` |

Scope hiện tại: chat toàn server cho client còn kết nối WebSocket, remote player visual trong `city`/`mine`, tối đa 20 người/room. Cây/đá public là lát gameplay server-authoritative đầu tiên: server chỉ cho một claim thắng, ghi inventory + lượt đào nguyên tử/idempotent, broadcast depletion và hồi sinh sau 20 giây. Registry tài nguyên hiện nằm trong RAM Node nên reset khi backend restart.

Quy ước theo yêu cầu trước mắt của sếp:
- Realtime public island chỉ áp dụng cho `city`, `mine`, và các đảo non-farm sau này.
- `farm` không phải public realtime room. Farm là private state theo account, sync qua REST/action log ở phase farm-state; client rời room public nhưng vẫn giữ WebSocket cho chat global.
- Shop/Point vẫn phải dùng transaction server-authoritative có `idempotency_key`; không cộng/trừ trực tiếp ở Unity. Riêng claim cây/đá đi qua WebSocket nhưng server vẫn ghi transaction inventory/daily-limit trước khi trả kết quả và broadcast world state.

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
