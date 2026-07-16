# SECURITY & ANTI-CHEAT - Y WONDER GREEN FARM

> Tài liệu kỹ thuật nội bộ. Cập nhật: 16/07/2026.
> Nguồn sự thật production hiện là game-server Node.js + PostgreSQL sau Nginx HTTPS/WSS.

## 1. Quyết định tiền tệ

- Game chỉ có một ví hiển thị và sử dụng: `Point`.
- BA/khách xác nhận Point web và Point game là cùng một ví/số dư, không phải hai
  balance độc lập để đồng bộ cộng dồn. Nguồn chuẩn: `docs/POINT_WALLET_BUSINESS_RULES.md`.
- `UPoint` đã nghỉ khỏi runtime, HUD, API và fresh schema. Migration mở rộng `004_single_point_currency.sql` lưu số dư UPoint cũ vào bảng audit `legacy_upoint_balances`, không tự quy đổi và tạm giữ cột cũ để rollback release trước an toàn. Migration contract xóa cột chỉ chạy sau khi release Point-only đã deploy/verify.
- Cho phép `USDT -> Point`, `YWH <-> Point` và `Point -> USDT`; tỷ giá do Admin thay
  đổi. Mọi conversion/rút phải ghi vào ledger Point chung, có rate version và audit.
- Giao dịch tiêu dùng game phải phát sinh hoa hồng YWH idempotent cho referrer theo
  contract HUB sau khi BA chốt công thức; không được tin số YWH do Unity gửi.
- Unity không được giữ secret, không gọi callback nạp và không được gửi số Point cần cộng.

## 2. Mô hình đe dọa

| Tài sản | Đe dọa | Hậu quả |
|---|---|---|
| Point | Client sửa cache hoặc gửi delta dương | Lạm phát, mất khả năng đối soát tiền nạp |
| Inventory | Client tự thêm item rồi bán | Biến item giả thành Point |
| Giao dịch web | Callback giả, replay hoặc đổi số tiền | Cộng Point trái phép/cộng trùng |
| Hai ledger Point | Web và game cùng giữ balance spendable | Double-spend, số dư hiển thị khác nhau |
| Tỷ giá Admin | Sửa rate không audit hoặc dùng rate cũ | Chênh lệch tài sản, khiếu nại/rút sai |
| Rút/chuyển đổi | Timeout sau debit, retry hoặc reversal sai | Mất tiền hoặc chi trả hai lần |
| Hoa hồng YWH | Gửi payout lặp hoặc payout cho sai referrer | Lạm phát YWH, sai tuyến giới thiệu |
| Shop/gameplay | Giá do client khai, double-spend | Sai số dư và item |
| Token | Lộ token hoặc nhiều phiên cùng account | Chiếm tài khoản/ghi đè state |
| Thời gian | Chỉnh giờ máy | Sai cây, thú, daily limit và phần thưởng |

## 3. Nguyên tắc bắt buộc

**Client chỉ gửi ý định; server quyết định kết quả.**

| Không được tin từ client | Server phải làm |
|---|---|
| Số Point muốn cộng | Xác định từ giao dịch web hoặc luật gameplay server |
| Giá mua/bán | Tra catalog server và kiểm inventory/số dư |
| Item thưởng | Xác minh hành động, loot table và cooldown |
| Thời điểm hoàn thành | Dùng server time/timestamp đã lưu |
| Kết quả giao dịch trước đó | Tra `idempotency_key` trong PostgreSQL |

## 4. Web top-up -> Point

Game-server có endpoint nội bộ:

```text
POST http://127.0.0.1:3000/internal/web/point-credit
X-YWonder-Timestamp: <unix-seconds>
X-YWonder-Signature: <HMAC-SHA256>
```

Quy tắc an toàn:

- Route mặc định tắt bằng `WEB_TOPUP_ENABLED=false` và chỉ nhận loopback khi `WEB_TOPUP_ALLOW_REMOTE=false`.
- Dùng secret riêng `WEB_TOPUP_SECRET` tối thiểu 32 ký tự; không dùng lại secret đăng nhập và không đưa vào repo/Unity/browser.
- Producer mới dùng domain `ywonder-point-credit-v2`; chữ ký bao phủ transaction ID, web user ID, `expected_player_id`, số Point, thời điểm giao dịch, nguồn và thông tin định danh tùy chọn. Game tra mapping authoritative và trả `409 GAME_POINT_IDENTITY_MISMATCH` trước khi cộng nếu player không khớp. V1 chỉ giữ tạm cho thứ tự deploy tương thích.
- Request mặc định chỉ hợp lệ trong cửa sổ 300 giây.
- `source + transaction_id` là idempotency key bất biến. Retry cùng payload trả `duplicate=true`; cùng key nhưng payload khác trả `409 IDEMPOTENCY_CONFLICT`.
- Cộng Point và ghi `game_transactions.type=web_topup_credit` trong cùng transaction PostgreSQL.
- Người chơi online nhận số dư absolute qua `economy_updated`; bootstrap/relogin vẫn là nguồn khôi phục cuối cùng.
- Nginx không được public `/internal/*`; port Node/PostgreSQL vẫn đóng ngoài Internet.
- Web account đã link lưu duy nhất `gamePlayerId`, đọc balance game bằng request HMAC riêng và fail closed nếu response trả player khác. Một game player không được link cho hai web account.
- Web-side debit/rút/đổi ngược dùng ba route loopback `point-reserve`, `point-capture`, `point-release`, canonical domain `ywonder-point-reservation-v1` và cùng identity pinning. Các route này có kill switch riêng `WEB_POINT_WALLET_DEBIT_ENABLED=false`.
- `reserve` trừ Point và tạo reservation trong cùng PostgreSQL transaction; `capture` không trừ lần hai; `release` hoàn đúng một lần. `CAPTURED/RELEASED` là terminal, còn cùng ID khác payload hoặc đi chéo terminal state phải trả conflict.
- Admin đổi tỷ giá bằng append version mới. Conversion đã commit phải lưu `rateVersionId`, rate micros, source/destination micros và rounding remainder; retry không được tính lại theo rate đang active.

## 5. Cổng P0 trước khi bật tiền thật

Endpoint top-up riêng đã an toàn và có test, nhưng toàn bộ ví chưa đạt mức tiền thật nếu các endpoint gameplay chung vẫn chấp nhận client tự gửi delta.

Trước khi chuyển sang `WEB_TOPUP_MODE=open` hoặc dùng tiền thật, cần khóa/đổi toàn bộ các đường sau:

- `/player/economy/apply` không được nhận delta Point dương do Unity tự khai.
- `/player/inventory/adjust` không được nhận item dương tùy ý vì item giả có thể bán thành Point.
- `PUT /player/inventory` không được cho client thay toàn bộ snapshot túi đồ production.
- Tutorial, điểm danh, vòng quay, thu hoạch, câu cá, farm/thú và các reward khác phải chuyển sang endpoint hành động riêng hoặc claim server-side.
- Mỗi endpoint phải tự kiểm điều kiện, catalog/reward, cooldown, phiên active và idempotency.

Trạng thái production 16/07/2026: canary chỉ bật cho đúng một identity, nhưng chủ tài khoản đã xác nhận `Nhien345` là account thật chứ không phải QA cô lập. Web/game allowlist và `CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS` khớp identity này; `CLIENT_ASSET_GRANTS_ENABLED=true` giữ reward legacy cho account ngoài canary. `Player.log` chứng minh reward dương của `Nhien345` bị `403` rồi artifact cũ mắc `PENDING_STATE_SYNC_FAILED`; account đối chứng `senh2026` nhận nước và mua thành công, không có `403/409/503`. Remote ingress vẫn tắt và callback public `404`. Các drill EXE/APK/restart/retry trước đó giữ đúng Point `5003`, ba ledger và outbox `3 SENT / 7 attempts / 0 pending`; ví web vẫn toàn `0`. Đây chưa phải giao dịch tiền thật hoặc quyền bật `open`, và không tiếp tục dùng `Nhien345` làm QA.

Candidate authority v3 mới hơn đã pass cô lập nhưng chưa deploy: unique `User.id -> gamePlayerId`, linked-wallet Point freeze, signed balance identity check, callback v2, immutable Admin rate version và Point reservation `reserve/capture/release` trên JSON/PostgreSQL. Fault E2E còn đổi rate sau khi conversion commit để chứng minh outbox retry vẫn gửi quote cũ. Validator pass migration, Next.js `15.5.20` build và runtime E2E với production không đổi. Playbook synthetic fixed-rate `0,06 USDT -> 1 Point` tại `docs/QA/WEB_POINT_NO_MONEY_CANARY.md` vẫn bị giữ lại; test mới phải dùng version tỷ giá Admin đang active.

Phương án cô lập canary dùng `CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS`. Khi `CLIENT_ASSET_GRANTS_ENABLED=true`, startup chỉ cho phép canary nếu danh sách khóa grant khớp tuyệt đối `WEB_TOPUP_ALLOWED_WEB_USER_IDS`; hai API delta kiểm cả `webUserId` trong JWT đã ký và mapping `game_players.web_user_id` authoritative. Mode `open` vẫn bắt buộc global flag là `false` và danh sách scoped phải rỗng. Local và Linux artifact integration đã pass canary bị chặn, control không bị chặn, debit hợp lệ và trường hợp claim JWT khác mapping store.

Cho tới khi migration và web debit orchestrator hoàn tất, không chuyển `open` và không chạy thêm giao dịch ví trên identity thật hiện tại. Production hiện chưa bật `WEB_POINT_WALLET_DEBIT_ENABLED`; bước production tiếp theo phải được duyệt riêng: đưa canary về dormant hoặc chuyển sang tài khoản QA chuyên dụng. Việc route delivery chạy đúng không đồng nghĩa rút/YWH/hoa hồng đã hoàn chỉnh.

## 6. Auth, transport và lưu trữ

- JWT secret chỉ qua environment, đủ dài; production bind Node vào loopback.
- HTTPS/WSS bắt buộc; Nginx truyền đúng client IP, còn Node dùng `trust proxy=loopback`.
- Mật khẩu chỉ lưu bcrypt hash; không log password, token, secret hoặc request body nhạy cảm.
- Auth có rate limit theo IP và identity; body/payload có giới hạn kích thước.
- Một account chỉ có một active session; phiên mới thay phiên cũ, REST cũ nhận `401 SESSION_REPLACED`, WebSocket cũ đóng mã `4008`.
- PostgreSQL có backup/versioned migration; transaction nhạy cảm phải nguyên tử.
- Cache PlayerPrefs chỉ phục vụ hiển thị/khôi phục tạm, không phải nguồn quyết định số dư online.

### E2E không dùng tiền trên VPS

- Candidate game và web bắt buộc nằm trong thư mục riêng dưới `/tmp`; runner từ chối production path hoặc các root lồng nhau.
- Web chỉ ghi vào bản sao SQLite. Game dùng database PostgreSQL tạm có prefix `yw_point_e2e_*`, không dùng `search_path` fallback sang schema `public` của `ywonder_game`.
- Trước install/build phải đạt ngưỡng RAM, disk và load; tác vụ chạy qua `nice`/`ionice`, timeout và lock một lượt. Timeout/interrupt có PID marker để dừng đúng process group, rồi fallback drop đúng database có prefix.
- Không có lệnh start/stop/restart systemd. Runner lấy đúng `WorkingDirectory` active từ systemd, chỉ chấp nhận root web chuẩn hoặc release versioned an toàn, và bắt buộc `.env` active trỏ về env production chuẩn. Trước/sau phải giữ nguyên service PID/active timestamp, active release root, health `200`, hash env/source/build production; callback phải đúng trạng thái được bảo vệ (`404` khi dormant hoặc `401` khi exact-one canary).
- `PRODUCTION_PLAYER_DATA_MUTATED=no` chỉ hợp lệ sau khi database tạm đã bị xóa và test user/transaction không tồn tại trong PostgreSQL game hay SQLite web thật.
- Trạng thái hiện tại: harness safety test, Bash syntax và backend regression pass local. Runner đã pass lại trên VPS ngày 16/07/2026 bằng đúng release active Next.js `15.5.20`: canary rejection, first dispatch, duplicate retry, web restart cô lập, post-commit response loss và idempotent recovery đều đạt. Database tạm/bản sao SQLite/process/upload đã dọn; production PID/root/build/env/health không đổi và không dùng giao dịch tiền thật.

### Migration dry-run hai ledger

- Chỉ được chạy bằng exporter cố định: SQLite `mode=ro/query_only` và PostgreSQL
  `REPEATABLE READ READ ONLY`; không dùng câu SQL ad-hoc để copy/cộng balance.
- Vì SQLite và PostgreSQL không có transaction chung, runner phải capture cả hai
  hai lượt và dừng nếu một snapshot thay đổi giữa cửa sổ đọc.
- Raw `User.id`, `playerId` và transaction ID là dữ liệu nhạy cảm: chỉ nằm trong
  temp `0700`, bị xóa bằng trap, không gửi qua chat/log và không commit.
- Report giữ balance/evidence nhưng thay identity bằng HMAC. Khóa report tối thiểu
  32 ký tự, root-only, ổn định giữa các lượt cần so sánh và không lưu trong repo.
- `READY_TO_LINK` không phải quyền tự migrate. Mọi balance seed/legacy/reward phải
  có phân loại và phê duyệt; write migration là release độc lập có backup/rollback.
- Candidate đã pass local; production dry-run chưa chạy.

## 7. Checklist nghiệm thu top-up

- [x] UPoint không còn trong runtime/HUD/API/fresh schema; cột production legacy chỉ tạm tồn tại qua nấc deploy tương thích.
- [x] Dữ liệu UPoint cũ được archive, không tự quy đổi.
- [x] Endpoint top-up riêng có HMAC, timestamp, loopback và giới hạn số Point.
- [x] Cộng Point + transaction nguyên tử và idempotent.
- [x] Test chữ ký sai, request quá hạn, retry, conflict, restart và realtime refresh.
- [~] Exporter đã audit đúng bảng Point/outbox web ở fixture và fail-closed schema lệch; còn phải chạy production read-only.
- [~] Đã chọn PostgreSQL game làm ledger authoritative và có candidate dry-run từng account; chưa phân loại/migrate số dư production.
- [~] Candidate có rate version, integer micros, rounding journal và test retry; còn chờ duyệt production và semantics YWH/rút.
- [ ] Chốt `YWH <-> Point`, chuyển Point và state machine `Point -> USDT` gồm reversal.
- [ ] Chốt công thức/số tầng/điều kiện/refund hoa hồng YWH từ tiêu dùng game.
- [~] Source web tạo outbox trong cùng transaction với bản ghi SWAP `SUCCESS`; còn chờ canary production chứng minh bằng giao dịch thật.
- [x] Có outbox/retry phía web; `sourceTransactionId` unique và retry giữ nguyên idempotency key.
- [x] Migration `004/005` đã pass schema tạm và đã deploy dormant; chưa chạy migration contract xóa cột rollback legacy.
- [~] Code khóa nguyên túi/strict mode đã deploy dormant; production chưa bật strict mode vì reward gameplay hợp lệ còn dùng generic positive delta. Cần cô lập identity canary trước lượt thử và chuyển reward sang action/claim server-authoritative trước khi mở chính thức.
- [~] Cô lập grant scope đúng một identity và account đối chứng không bị chặn, nhưng identity đó là tài khoản thật `Nhien345`, không phải QA; phải đưa về dormant hoặc chuyển sang QA riêng trước vòng ví tiếp theo.
- [x] Harness E2E không-tiền đã pass safety test local và active-production-artifact run trên VPS; runner theo đúng systemd `WorkingDirectory`, build Next.js `15.5.20`, database tạm, cleanup, canary/idempotency/phần lẻ và baseline production đều đạt.
- [x] Web restart giữa giao dịch và response loss sau server commit đã pass trong process/database cô lập: attempt đầu để lại `RETRY` sau khi game đã commit, attempt hai dùng cùng transaction ID về `SENT`, tổng ledger/Point không tăng lần hai.
- [x] Canary production không tiền đã pass restart riêng game backend và lỗi vận chuyển có kiểm soát: Point/ledger giữ nguyên, outbox `RETRY -> SENT`, web không restart, env không đổi và callback public vẫn `404`.
- [x] Client đăng nhập lại sau các outage có kiểm soát và khôi phục đúng số dư PostgreSQL authoritative `5003`.
- [x] Script bật/tắt canary có shared lock, validate-only, backup root-only và rollback tự động; VPS rollback preflight, live dormant -> canary và client relogin sau double restart đều pass. Env trở về byte-for-byte baseline, HUD/PostgreSQL cùng `5003` và ledger/outbox không đổi.
- [x] Web production đã nâng từ Next.js `14.2.18` lên release Next.js `15.5.20`, build `O-PUYMkTlVdeNCYQWp2gJ`. Build/audit `0 vulnerabilities`, route guards, production credential login/session/authenticated wallet, cleanup synthetic, DB/foreign key, rollback candidate và postflight đều pass; source cũ còn nguyên để rollback. Top-up đang ở canary đúng một identity, chưa dùng tiền thật.
- [~] Nạp synthetic có kiểm soát, retry cùng transaction, relogin, đổi EXE/APK, restart backend, web restart cô lập và post-commit timeout đã đúng một lần; giao dịch web thật cực nhỏ chưa thực hiện.
- [~] Có report đối soát web transaction/outbox source với `game_transactions` và phát hiện duplicate/mismatch; local pass, production chưa chạy.
