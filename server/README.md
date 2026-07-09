# YWONDERLAND — Server stub (dev/test)

> ⚠️ **CHỈ dùng để phát triển & test ở local.** Đây KHÔNG phải server production:
> - JWT secret để cứng trong code, không có rate-limit, không HTTPS.
> - Lưu dữ liệu bằng 1 file `data.json` (không phải DB thật).
>
> Server production (Node/Go/Python + PostgreSQL/MongoDB theo kịch bản) sẽ thay thế sau,
> nhưng **giữ nguyên API contract** bên dưới để client Unity không phải sửa.

## Mục đích (Đợt 1)
Chứng minh luồng **lưu thật** end-to-end cho `player_profile` + cờ `tutorialCompleted`:
client Unity ↔ REST ↔ lưu trữ.

## Cài & chạy
Cần **Node.js LTS** (https://nodejs.org).

```bash
cd server
npm install
npm start
```
Mặc định chạy tại `http://localhost:3000` (đổi bằng biến môi trường `PORT`).

Storage mặc định là JSON file để test local nhanh:

```powershell
$env:STORE_MODE="json"
$env:YW_DATA_PATH="D:\LamGameUnity\BaChuKhuRung3D\server\data.json"
```

`STORE_MODE=postgres` đã có adapter scaffold (`server/postgresStore.js`) và schema target
(`server/schema.sql`), nhưng chưa có query implementation/driver DB. Khi cắm DB thật,
giữ nguyên interface trong `server/store.js` để route API không phải đổi.

## Giao diện xem backend local

Khi server đang chạy, mở trình duyệt:

```text
http://127.0.0.1:3000/admin
```

Dashboard này dùng để demo/dev:

- Xem tổng số players, profiles, inventory slots, transactions.
- Tạo user demo.
- Chọn player và sửa JSON của profile, economy, inventory, daily limits, farm state.
- Xóa player khỏi JSON store local.

Đây **không phải admin production** và mặc định chỉ nên dùng trong môi trường dev/local.
Theo roadmap 06/07/2026, dashboard cho sếp xem online phải là phase riêng: có login admin,
role `super_admin`, HTTPS, audit log cho mọi chỉnh sửa tiền/item/farm/daily limit, và
chỉ cho reset dữ liệu demo/staging chứ không reset nhầm production.
Nếu muốn tắt dashboard:

```powershell
$env:ADMIN_DASHBOARD_ENABLED="false"
```

Hành trình đầy đủ từ tài khoản web -> tài khoản game -> gameplay server-authoritative nằm ở
`../docs/WEB_GAME_BACKEND_JOURNEY.md`. Dashboard chỉ chứng minh dữ liệu backend MVP; Unity shop/economy/inventory
chỉ đổi dashboard sau khi loop server-authoritative được nối vào gameplay.

## URL public target

Khi public xong, client Unity sẽ trỏ `Assets/Resources/BackendConfig.asset` tới:

```text
https://api.ywonder.net
```

Trong lúc test nội bộ/local, asset này có thể tạm trỏ `http://127.0.0.1:3000` hoặc IP LAN của máy chạy backend. Không commit secret hay URL tạm nếu không cần.

Server stub hỗ trợ cả 2 dạng route:

- Local/dev: `http://localhost:3000/auth/login`
- Public qua reverse proxy: `https://api.ywonder.net/auth/login`
- Legacy nếu cần đi qua web domain: `https://ywonder.net/game-api/auth/login`

Khi dùng Caddy trên Windows, tham khảo `server/Caddyfile.example`. Nếu website chính cũng chạy trên cùng máy, giữ cấu hình website hiện có và chỉ proxy path `/game-api*` vào Node server port `3000`.

## API
| Method | Endpoint | Body | Trả về |
|---|---|---|---|
| GET  | `/` | — | `{ ok: true }` (health check) |
| GET  | `/health` | — | `{ ok: true, checkedAt }` |
| POST | `/auth/register` | `{ "username", "password" }` | `{ token, userId }` |
| POST | `/auth/login` | `{ "username", "password" }` | `{ token, userId }` |
| GET  | `/player/profile` | — (header `Authorization: Bearer <token>`) | `{ player_profile { ... } }` |
| PUT  | `/player/profile` | `{ "player_profile": { ... } }` (Bearer token) | `{ ok: true, updatedAt }` |

`player_profile` theo `docs/DB_SCHEMA.md` + field `characterCreated` (bool, đã tạo nhân vật) và `tutorialCompleted` (bool).

## Game API MVP trong luc cho Web API

`WEB_AUTH_MODE=mock` la mac dinh hien tai de game backend chay duoc truoc khi ben web giao endpoint login/verify. Khi co API web that:

```powershell
$env:WEB_AUTH_MODE="http"
$env:WEB_AUTH_LOGIN_URL="https://api.ywonder.net/api/game/auth"
$env:WEB_AUTH_SECRET="<GAME_API_SECRET from VPS .env>"
```

Unity KHONG goi truc tiep endpoint web nay va KHONG duoc giu `GAME_API_SECRET`.
Unity goi `/auth/web-login` cua game-server; game-server moi goi `api.ywonder.net/api/game/auth`.
Neu SSL cua `api.ywonder.net` dang ket ha tang, co the override tam `WEB_AUTH_LOGIN_URL=https://ywonder.net/api/game/auth`.

## Test realtime khi web dang sap / chua co tai khoan web

Dung `WEB_AUTH_MODE=mock` de gia lap tai khoan duoc cap san. Trong mode nay, game-server map username thanh
`web_user_id = mock:<username>` va tra game JWT nhu flow web-login that, nen Unity/Realtime van test duoc ma khong can web.

Chay server local:

```powershell
cd server
$env:WEB_AUTH_MODE="mock"
$env:PORT="3000"
npm.cmd start
```

Account test de nhap trong Unity:

```text
DemoRealtime01 / demo
DemoRealtime02 / demo
DemoRealtime03 / demo
```

Neu test trong Unity Editor cung may chay server, dat `BackendConfig.baseUrl = http://127.0.0.1:3000`.
Neu test dien thoai/2 may trong LAN, dat `BackendConfig.baseUrl = http://<IP-may-chay-server>:3000`.

Smoke test tu dong REST + WebSocket:

```powershell
cd server
$env:REALTIME_TEST_BASE_URL="http://127.0.0.1:3000"
npm.cmd run test:realtime
```

Test nay login `DemoRealtime01`, `DemoRealtime02`, `DemoRealtime03`; 2 client join `city`,
client thu ba khong join room van nhan/gui duoc chat global, kiem tra `player_state`,
va thu join `farm` de dam bao farm khong phai realtime public room.

Endpoint moi:

| Method | Endpoint | Ghi chu |
|---|---|---|
| POST | `/auth/web-login` | Mock/adapter login web -> tra game token + playerId |
| GET | `/player/bootstrap` | Lay profile + economy + inventory + farm_state |
| GET/PUT | `/player/economy` | Vi game rieng trong MVP |
| POST | `/player/economy/apply` | Apply delta Point/UPoint game, co idempotency key |
| GET/PUT | `/player/inventory` | Doc/ghi inventory MVP |
| POST | `/player/inventory/adjust` | Cong/tru item, co idempotency key |
| GET | `/player/daily-limits` | Doc gioi han ngay cho fishing/mining |
| POST | `/player/daily-limits/consume` | Tru luot server-side, mac dinh 10 luot/ngay |
| GET/PUT | `/player/farm-state` | Doc/ghi farm-state JSON MVP |

`/player/bootstrap` nay tra:

```json
{
  "player_profile": {},
  "economy": {},
  "inventory": {},
  "farm_state": {},
  "daily_limits": {}
}
```

Daily limit payload de tru luot:

```json
{
  "limit_key": "mining",
  "amount": 1,
  "max_count": 10,
  "idempotency_key": "uuid-or-client-action-id"
}
```

## Realtime MVP

Game-server also exposes WebSocket realtime:

| Endpoint | Purpose |
|---|---|
| `ws://localhost:3000/realtime?token=<game-server-jwt>` | Local dev realtime |
| `wss://api.ywonder.net/realtime?token=<game-server-jwt>` | Public realtime when SSL/proxy is ready |
| `wss://ywonder.net/game-api/realtime?token=<game-server-jwt>` | Legacy path if proxying under `/game-api` |

MVP scope:

- shared rooms: `city`, `mine`
- farm không join realtime room công cộng; farm là private state theo account và sync bằng REST/action log sau
- global chat across server, including connected clients that are not currently in a shared room
- max players per room: `REALTIME_MAX_ROOM_PLAYERS` (default `20`)
- remote player position/yaw/`Idle`/`Walk`/`Run`
- emotes: `Waving`, `Pointing`

Nginx/Caddy must allow WebSocket Upgrade for `/realtime`, not only normal HTTP.

PostgreSQL target schema nam o `server/schema.sql`; stub hien van luu JSON de dev/test nhanh.

## Smoke test nhanh (PowerShell)
```powershell
# Đăng ký
$r = irm http://localhost:3000/auth/register -Method Post -ContentType application/json -Body '{"username":"test","password":"12345678"}'
$tok = $r.token
# Lấy profile
irm http://localhost:3000/player/profile -Headers @{ Authorization = "Bearer $tok" }
# Cập nhật cờ tutorial
irm http://localhost:3000/player/profile -Method Put -ContentType application/json -Headers @{ Authorization = "Bearer $tok" } -Body '{"player_profile":{"characterCreated":true,"tutorialCompleted":true}}'
```
