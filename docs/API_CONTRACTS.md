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
- Game chỉ còn một tiền tệ `Point`; `UPoint` đã nghỉ hưu khỏi runtime/API/HUD và fresh schema. Dữ liệu PostgreSQL cũ được archive bằng migration mở rộng `004`, không tự quy đổi; cột legacy chỉ bị xóa ở migration contract sau khi release Point-only đã deploy/verify làm mốc rollback.
- Giao dịch nạp đã được web xác nhận phải gọi game-server qua kênh server-to-server có chữ ký và transaction ID bất biến. Unity không được gửi số Point cần cộng. Shop và khai thác cây/đá public đã server-authoritative; farm/crop/animal/câu cá cùng các reward/chi phí khác vẫn cần tiếp tục siết quyền server.
- Realtime trước mắt chỉ dành cho đảo công cộng như `city`/`mine`; farm không join room realtime công cộng. Chat là kênh toàn server cho client còn online, không phụ thuộc đang đứng cùng room.

| Method | Endpoint | Body | Tra ve |
|---|---|---|---|
| POST | `/auth/web-login` | `{ username/email/refCode, password }` hoac `{ token }` | `{ token, playerId, webUserId, player_profile }` |
| POST | `/auth/browser/start` | `{ code_challenge, intent:"login"|"register" }` | `{ requestId, authUrl, expiresInSec, pollIntervalMs }` |
| POST | `/auth/browser/approve` | `{ requestId, webUser }` + server Bearer secret | `{ ok, duplicate }`; chỉ callback web gọi qua loopback |
| POST | `/auth/browser/exchange` | `{ requestId, code_verifier }` | `{ token, playerId, webUserId, player_profile, status:"complete" }` hoặc `202 pending` |
| GET | `/player/bootstrap` | Bearer token | `{ player_profile, economy, inventory, farm_state, daily_limits }` |
| GET | `/player/economy` | Bearer token | `{ economy }` |
| PUT | `/player/economy` | Bất kỳ body + Bearer | `405 ECONOMY_SERVER_AUTHORITATIVE`; client không được ghi đè ví |
| POST | `/player/economy/apply` | `{ delta_pos, type, ref, idempotency_key }` + Bearer | `{ ok, economy, transaction }`; UPoint khác 0 trả `400`; delta Point dương trả `403 CLIENT_POSITIVE_ECONOMY_DELTA_FORBIDDEN` khi strict toàn cục hoặc web user nằm trong scoped block list |
| GET | `/player/inventory` | Bearer token | `{ inventory }` |
| PUT | `/player/inventory` | Bất kỳ body + Bearer | `405 INVENTORY_SERVER_AUTHORITATIVE`; client không được thay nguyên túi |
| POST | `/player/inventory/adjust` | `{ item_id, quantity_delta, type, ref, idempotency_key }` + Bearer | `{ ok, inventory, transaction }`; item dương trả `403 CLIENT_POSITIVE_INVENTORY_DELTA_FORBIDDEN` khi strict toàn cục hoặc web user nằm trong scoped block list |
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
- `Point` vừa là tiền gameplay vừa là tiền nạp. Web là nguồn giao dịch nạp; PostgreSQL game là nguồn số dư dùng trong game. UPoint không còn vai trò sản phẩm.
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

`/api/game/credit` là contract cũ để cộng ledger phía web, không được coi là callback nạp tiền vào game. Luồng top-up mới chỉ chạy sau khi web xác nhận giao dịch thành công và gọi endpoint loopback bên dưới.

### Hợp đồng nghiệp vụ ví Point được xác nhận 16/07/2026

Nguồn chuẩn: `docs/POINT_WALLET_BUSINESS_RULES.md`.

- Point web và Point game là cùng một loại tiền và phải hiển thị cùng số dư như một ví.
- Người dùng đổi `USDT -> Point`, `YWH <-> Point` và có thể đổi `Point -> USDT`; tỷ giá do Admin thay đổi, không cố định `0,06 USDT/Point` hoặc `25 Point/USDT`.
- Tiêu dùng game phải phát sinh payout hoa hồng bằng YWH cho người giới thiệu tương tự HUB. Phạm vi ít nhất gồm vật nuôi, cây dài/ngắn ngày, mồi câu, lượt vòng quay, lượt đào khoáng và mọi tiêu dùng game.
- Hai bề mặt không được giữ hai balance Point spendable độc lập. Mọi mutation Point phải đi vào một ledger authoritative và trả số dư absolute cho cả web lẫn game.
- Giao dịch tiêu dùng Point và sự kiện/payout YWH phải chia sẻ source transaction ID hoặc transactional outbox để retry không trừ/cộng/trả hoa hồng hai lần.

Quyết định kỹ thuật candidate đã chốt trong `docs/ADR_POINT_WALLET_AUTHORITY.md`:

- PostgreSQL game `player_economy.pos` là ledger Point spendable duy nhất cho account đã link.
- Web account đã link đóng băng `balanceGXL/lockedGXL` ở `0` và đọc balance bằng request HMAC; account legacy chưa link không tự migrate hoặc cộng dồn.
- Settlement mới dùng integer micros, rate version bất biến và lưu rate snapshot/rounding remainder trong conversion journal.
- Point dùng cho thao tác web-side đi qua state machine `reserve -> capture|release`; không ghi một delta âm rời rạc vào bản sao balance web.

Còn chặn **production/money thật**, nhưng không chặn code và test candidate cô lập:

- Báo cáo reconciliation và phê duyệt migration riêng cho từng balance web legacy.
- Mâu thuẫn nghiệp vụ `YWH -> Point`, hành vi chuyển Point giữa người dùng và các action web legacy đang cộng `balanceGXL`.
- Phí/hạn mức nghiệp vụ, phê duyệt rút và đối soát bên thanh toán cho `Point -> USDT`. Candidate local bắt buộc cấu hình fee BPS rõ ràng nhưng không tự coi đó là quyết định BA.
- Công thức, số tầng, điều kiện, thời điểm và reversal hoa hồng YWH.

Các endpoint dưới đây là contract authority v3 đã có test cô lập và đã deploy production ở trạng thái dormant. Schema/handler tồn tại nhưng debit flag vẫn `false`, route public vẫn `404` và chưa có account link; đây không phải quyền bật giao dịch thật hoặc chuyển `WEB_TOPUP_MODE=open`.

### Internal web top-up -> game Point

Endpoint này không được Nginx public dưới `/game-api`:

```text
POST http://127.0.0.1:3000/internal/web/point-credit
X-YWonder-Timestamp: <unix-seconds>
X-YWonder-Signature: <HMAC-SHA256 canonical payload>
```

```json
{
  "transaction_id": "web-transaction-id-on-dinh",
  "web_user_id": "web-user-id-on-dinh",
  "expected_player_id": "game-player-id-da-ghim",
  "point_amount": "1000.000000",
  "occurred_at": "2026-07-15T00:00:00.000Z",
  "source": "ywonder-web",
  "username": "optional",
  "display_name": "optional"
}
```

- Payload có `expected_player_id` dùng canonical domain `ywonder-point-credit-v2`; chữ ký bao phủ field này ngay sau `web_user_id`. Game chỉ nhận khi `game_players.web_user_id` tồn tại và map đúng player đã ghim, nếu không trả `409 GAME_POINT_IDENTITY_MISMATCH` trước khi đổi Point/ledger. V1 không có field này chỉ được giữ tạm để nâng game trước web; producer mới bắt buộc dùng v2.
- `WEB_TOPUP_SECRET` là secret riêng chỉ nằm ở web server và game-server; không
  dùng lại secret login/browser auth và không nằm trong Unity/browser bundle.
- `source + transaction_id` là idempotency key. Retry cùng payload không cộng lần hai; cùng key nhưng payload khác trả `409 IDEMPOTENCY_CONFLICT`.
- Timestamp mặc định chỉ lệch tối đa 300 giây; route mặc định chỉ nhận loopback.
- Mỗi lần cộng ghi `game_transactions.type = web_topup_credit` trong cùng transaction với cập nhật `player_economy`.
- Player online nhận `economy_updated` với số dư absolute; bootstrap/relogin vẫn đọc lại PostgreSQL.
- Production `open` chỉ được bật khi `CLIENT_ASSET_GRANTS_ENABLED=false`. Với canary, có thể tạm giữ reward legacy cho người chơi ngoài thử nghiệm nếu `CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS` khớp tuyệt đối `WEB_TOPUP_ALLOWED_WEB_USER_IDS`; game kiểm cả JWT và mapping player authoritative trước khi nhận delta dương.
- Rollout mặc định là `WEB_TOPUP_MODE=canary`; `WEB_TOPUP_ALLOWED_WEB_USER_IDS` giới hạn đúng account thử. Account ngoài allowlist nhận `425 WEB_TOPUP_CANARY_USER_NOT_ALLOWED`, nên web outbox giữ retry thay vì mất giao dịch. Chỉ chuyển sang `open` bằng quyết định riêng sau canary; lúc đó scoped block list phải rỗng.
- Trạng thái triển khai web hook phải được nghiệm thu riêng tại đúng điểm giao dịch chuyển sang thành công; không gọi từ browser hoặc chỉ dựa vào trang báo thành công.

Đọc balance authoritative để link/hiển thị account web đã ghim dùng cùng secret và loopback:

```text
POST http://127.0.0.1:3000/internal/web/point-balance
```

Body gồm `{ request_id, web_user_id }`, canonical domain `ywonder-point-balance-v1`. Response thành công gồm `{ ok, request_id, web_user_id, player_id, point, economy }`. Web phải so `player_id` với `GamePointLinkedAccount.gamePlayerId` và fail closed bằng `GAME_POINT_IDENTITY_MISMATCH` nếu lệch.

### Internal web Point reserve/capture/release (authority v3, deployed dormant)

Ba endpoint loopback dùng cùng secret/header với Point credit và mặc định không tồn tại
cho tới khi bật riêng `WEB_POINT_WALLET_DEBIT_ENABLED=true`:

```text
POST http://127.0.0.1:3000/internal/web/point-reserve
POST http://127.0.0.1:3000/internal/web/point-capture
POST http://127.0.0.1:3000/internal/web/point-release
```

```json
{
  "reservation_id": "withdraw-or-conversion-id-on-dinh",
  "web_user_id": "web-user-id-on-dinh",
  "expected_player_id": "game-player-id-da-ghim",
  "point_amount": "1000.000000",
  "purpose": "point_to_usdt",
  "source": "ywonder-web",
  "occurred_at": "2026-07-16T00:00:00.000Z"
}
```

- Canonical domain là `ywonder-point-reservation-v1`; chữ ký bao phủ cả operation, reservation ID, hai identity, amount, purpose, source và occurred time.
- Candidate hiện chỉ nhận số Point nguyên vì `player_economy.pos` là `BIGINT`; amount có phần lẻ trả `400 POINT_RESERVATION_REQUIRES_WHOLE_POINT`.
- `reserve` khóa row player, kiểm mapping/số dư, trừ Point đúng một lần và tạo trạng thái `RESERVED` trong cùng PostgreSQL transaction.
- `capture` chỉ chuyển `RESERVED -> CAPTURED`; không trừ lần hai. `release` chuyển `RESERVED -> RELEASED` và hoàn đúng một lần.
- `CAPTURED` và `RELEASED` là terminal. Retry cùng ID/cùng payload/cùng operation trả `duplicate=true`; đổi payload hoặc đi chéo terminal state trả `409`.
- Response thành công gồm `{ ok, duplicate, operation, player_id, economy, reservation, transaction }` và gửi realtime balance absolute cho player đang online.
- Web orchestrator phải giữ nguyên `reservation_id` qua timeout/retry và chỉ capture sau khi journal settlement web `PENDING` đã commit. USDT chưa được cộng vào số dư spendable ở trạng thái này. Sau khi game xác nhận `CAPTURED`, một SQLite transaction mới cộng `balanceUsdt`, chuyển transaction `SUCCESS` và debit `CAPTURED` đúng một lần.
- Nếu không tạo được journal settlement web, orchestrator release cùng ID; không tạo một debit/credit bù bằng ID khác. Nếu journal đã tồn tại thì không được release, kể cả sau timeout; phải retry capture cùng reservation hoặc chuyển `MANUAL_REVIEW`.
- Candidate local nhận intent `convertPointToUsdtAction(pointAmount, requestId)`. `requestId` là UUID v4 được browser giữ bền; journal sinh deterministic `reservationId/sourceTransactionId`, ghim player, rate version, gross/fee/net micros và rounding. Cùng ID khác amount trả `IDEMPOTENCY_CONFLICT`.
- Mỗi account chỉ có một operation ví chưa terminal trên cả hai chiều. Action precheck và trigger SQLite chéo chặn race `USDT -> Point` với `Point -> USDT` từ nhiều tab.
- Debit USDT chỉ hiện cho identity thỏa đồng thời `WEB_TOPUP_ENABLED`, mode/allowlist, `WEB_POINT_WALLET_DEBIT_ENABLED=true`, fee `WEB_POINT_DEBIT_FEE_BPS` hợp lệ và max `WEB_POINT_DEBIT_MAX_POINTS`. Không có fee mặc định ngầm.
- Adapter `Point -> YWH`, payout/rút USDT bên ngoài và tiền thật chưa được nối. Web saga mới đã pass full Prisma migration/DB E2E, Next build và debit fault E2E trên source + SQLite production bản sao; flag production vẫn phải giữ `false` cho tới khi deploy candidate và migration/link account được duyệt riêng.

Cập nhật bàn giao 09/07/2026 từ chat 01/07:
- Endpoint web auth đang dùng được ngay qua SSL hợp lệ là `POST https://ywonder.net/api/game/auth`.
- `api.ywonder.net` đã được cấu hình nhưng SSL/public routing đang kẹt ở lớp hạ tầng/WAF/default-server; không chặn việc nối game nếu game-server tạm gọi `ywonder.net/api/game/auth`.
- `GAME_API_SECRET` nằm ở server/.env hoặc do owner cấp riêng; tuyệt đối không đưa vào Unity, không commit vào repo.
- Đã có test account web `gametest`; mật khẩu lưu riêng ngoài repo.
- `gameToken` là JWT HS256 chuẩn, payload có `{ sub, uid, username, iat, exp }`, trong đó `sub/uid = web userId`.

Điểm còn phải triển khai/chốt với web team:
- Dùng PostgreSQL game làm ledger Point authoritative theo ADR; xác định đúng
  transition web đã commit cho conversion/transfer/withdrawal và source transaction
  ID bất biến dùng để đối soát.
- Hoàn thiện outbox/orchestrator gọi `credit` hoặc `reserve/capture/release` tại đúng
  transition; retry cùng ID khi game-server lỗi, tuyệt đối không ghi balance Point web.
- Lập reconciliation/migration report từng account legacy trước khi link và đóng băng
  `balanceGXL/lockedGXL`; không bulk-add số dư web vào game.
- Chặn các action quest/commission/investment/gift/staking/transfer legacy ghi Point
  đối với account đã link; action chưa có contract phải fail closed.
- Mọi cộng/trừ ví web phải có `ref` hoặc `idempotency_key` để retry không nhân đôi.
- Mỗi giao dịch tiêu dùng game phải tạo sự kiện hoa hồng YWH idempotent; cần endpoint
  hoặc outbox cho payout/reversal sau khi BA chốt công thức.
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
| `economy_updated` | `{ type, reason:"web_topup", economy:{ version, pos, updatedAt }, duplicate, sentAt }` |

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
