# YWONDERLAND — Game Backend

Backend hỗ trợ JSON cho dev/local và PostgreSQL cho staging. Source PostgreSQL đã
pass integration test, nhưng deployment hiện **chưa phải production** vì chưa có
rate-limit, HTTPS/WSS proxy, production secrets, backup/restore drill và monitoring.

Production bắt buộc đặt `JWT_SECRET`, DB credential và web-auth secret trong env trên
VPS; không dùng fallback dev, `data.json`, role `deploy` hoặc database `ywonder_test`.

## Mục đích
Giữ một API contract ổn định cho Unity trong khi storage chuyển từ JSON sang
PostgreSQL và từng gameplay slice chuyển sang server-authoritative.

## Cài & chạy
Cần **Node.js LTS** (https://nodejs.org).

```bash
cd server
npm install
npm start
```
Mặc định chạy tại `http://localhost:3000`. Có thể đổi bằng biến môi trường `HOST` và `PORT`; production đặt `HOST=127.0.0.1` để chỉ reverse proxy nội bộ truy cập trực tiếp Node.

Storage mặc định là JSON file để test local nhanh:

```powershell
$env:STORE_MODE="json"
$env:YW_DATA_PATH="D:\LamGameUnity\BaChuKhuRung3D\server\data.json"
```

`STORE_MODE=postgres` dùng adapter query thật trong `server/postgresStore.js`, driver `pg`
và migration versioned trong `server/migrations/`. Interface trong `server/store.js` giữ
nguyên để Unity/API contract không phải đổi khi chuyển từ JSON sang PostgreSQL.

Catalog giá và whitelist shop phía server được sinh từ đúng asset Unity hiện tại:

```powershell
npm.cmd run catalog:generate
```

Lệnh này đọc `Assets/Resources/Items/*.asset` và `Assets/_Project/Data/Shops/*.asset`,
kiểm mọi ID rồi ghi `server/shopCatalog.json`. Chạy lại và commit catalog mỗi khi sửa
giá hoặc danh sách hàng shop; server không tin giá do Unity/client gửi lên.

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
`../docs/WEB_GAME_BACKEND_JOURNEY.md`. Shop và khai thác cây/đá public nay đã ghi vào backend;
các gameplay farm/crop/animal/câu cá khác vẫn còn local cho đến khi nối từng lát tiếp theo.

## PostgreSQL Phase 2 (updated 11/07/2026)

`STORE_MODE=postgres` uses the implemented `pg` adapter, versioned migrations,
atomic transactions and JSON import tooling. See
`../docs/POSTGRESQL_PHASE2_RUNBOOK.md` for exact commands, test evidence and the
remaining production gates.

Quick command list:

```powershell
$env:DATABASE_URL="postgresql://<user>:<password>@127.0.0.1:5432/<database>"
npm.cmd run db:migrate
npm.cmd run test:postgres
npm.cmd run db:import-json
npm.cmd run db:verify
```

Do not publicize PostgreSQL port `5432`, commit a database URL, or use the VPS
test role/database as production storage.

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
| POST | `/auth/register` | `{ "username", "password", "email"?, "phone"? }` | `{ token, userId, playerId, username, email }` |
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

## Phase 1: tai khoan game local + realtime demo

Khi chua can nap/rut va web account that chua on dinh, co the cho khach dang ky tai khoan game local ngay tren game.
Khuyen nghi chay game-server voi web bridge tat de tai khoan local bat buoc dung password da dang ky:

```powershell
cd server
$env:WEB_AUTH_MODE="disabled"
$env:STORE_MODE="json"
$env:PORT="3000"
npm.cmd start
```

Unity register goi `/auth/register` va gui `username/password/email`; server luu user vao JSON store.
Unity login se thu `/auth/login` truoc. Chi khi server tra `404 USER_NOT_FOUND` thi Unity moi fallback sang `/auth/web-login`.
Khong nen public demo cho khach voi `WEB_AUTH_MODE=mock`, vi mock cho phep gia lap account cap san va khong phai he dang ky/password that.

Smoke test Phase 1:

```powershell
cd server
$env:PHASE1_TEST_BASE_URL="http://127.0.0.1:3000"
npm.cmd run test:phase1
```

Test nay tu tao 2 account moi, dang nhap lai, goi `/player/bootstrap`, sua economy/inventory/farm-state,
kiem tra idempotency, roi mo 2 WebSocket de join `city` va chat realtime.

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

Server cung seed san cac account demo local khi khoi dong:

```text
DemoRealtime01..DemoRealtime05 / demo
DemoRich01..DemoRich05 / demo
```

Voi cac account nay, password co the la `demo` hoac trung ten account, vi du
`DemoRich01 / DemoRich01`. `DemoRich` duoc seed tien/tai nguyen de test shop;
`DemoRealtime` dung de test nhieu nguoi online. Moi account chi duoc co 1 phien
realtime dang online; neu mo cung account o cua so/may khac, phien moi se thay
phien cu.

Neu test trong Unity Editor cung may chay server, dat `BackendConfig.baseUrl = http://127.0.0.1:3000`.
Neu test dien thoai/2 may trong LAN, dat `BackendConfig.baseUrl = http://<IP-may-chay-server>:3000`.

Smoke test tu dong REST + WebSocket:

```powershell
cd server
$env:REALTIME_TEST_BASE_URL="http://127.0.0.1:3000"
$env:REALTIME_TEST_AUTH_PATH="/auth/login"
npm.cmd run test:realtime
```

`REALTIME_TEST_AUTH_PATH` mac dinh la `/auth/web-login`; khi Phase 1 chay
`WEB_AUTH_MODE=disabled`, doi sang `/auth/login` nhu tren. Test nay login
`DemoRealtime01`, `DemoRealtime02`, `DemoRealtime03`; 2 client join `city`,
client thu ba khong join room van nhan/gui duoc chat global, kiem tra `player_state`,
thu join `farm` de dam bao farm khong phai realtime public room, va kiem tra
phien cu cung account bi dong ma `4008` khi phien moi ket noi.

Endpoint moi:

| Method | Endpoint | Ghi chu |
|---|---|---|
| POST | `/auth/web-login` | Mock/adapter login web -> tra game token + playerId |
| GET | `/player/bootstrap` | Lay profile + economy + inventory + farm_state |
| GET/PUT | `/player/economy` | Vi game rieng trong MVP |
| POST | `/player/economy/apply` | Apply delta Point/UPoint game, co idempotency key |
| GET/PUT | `/player/inventory` | Doc/ghi inventory MVP |
| POST | `/player/inventory/adjust` | Cong/tru item, co idempotency key |
| POST | `/player/shop/transaction` | Mua/ban nguyen tu; server tu tra catalog gia + whitelist shop |
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

Shop transaction payload:

```json
{
  "shop_id": "Shop_ItemShop",
  "mode": "buy",
  "item_id": "fertilizer_01",
  "quantity": 2,
  "idempotency_key": "uuid-or-client-action-id"
}
```

Response thành công trả cùng lúc `{ economy, inventory, transaction, duplicate }`.
Point và item được đổi trong một lần ghi store. Retry cùng key trả `duplicate=true`
và không áp lại; cùng key nhưng body khác bị từ chối `IDEMPOTENCY_CONFLICT`.

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
- remote player position/yaw, active animation, animation speed and held tool
- emotes: `Waving`, `Pointing`
- shared tree/rock manifest and snapshot per room
- server-authoritative resource claim: first claimant wins; reward inventory and the mining daily limit are written atomically
- resource depletion is broadcast to all clients, included for late joiners, and respawns after `REALTIME_RESOURCE_RESPAWN_SEC` (default `20`)

Shared resource state is in Node memory for Phase 1. Restarting the backend resets the room resource registry; PostgreSQL persistence is deferred to Phase 2.

Nginx/Caddy must allow WebSocket Upgrade for `/realtime`, not only normal HTTP.

PostgreSQL schema snapshot nam o `server/schema.sql`; migration versioned nam o
`server/migrations/`. JSON van la mode mac dinh de dev/test nhanh, con
`STORE_MODE=postgres` dung query PostgreSQL that.

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
