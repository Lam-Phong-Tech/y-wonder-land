# Web -> Game Backend Journey
# Cập nhật gần nhất: 2026-07-17

> Mục tiêu của tài liệu này là làm rõ phần đang mơ hồ trong kịch bản: hệ thống web đã có trước, game sẽ dùng tài khoản web làm tài khoản đăng nhập game, nhưng gameplay hiện vẫn còn nhiều dữ liệu local. Tài liệu này định nghĩa hành trình cần xây, kết quả mong đợi, cách kiểm tra và các câu hỏi cần chốt.

## Cập nhật quyết định BA/khách 16/07/2026

Nguồn chuẩn: `docs/POINT_WALLET_BUSINESS_RULES.md`. Mục này thay thế giả định cũ
rằng web chỉ gửi một khoản top-up để cộng thêm vào ví Point riêng của game:

- Point web và Point game là cùng một loại tiền, phải hiển thị cùng một số dư như
  hai giao diện của một ví.
- Người dùng đổi `USDT -> Point`, `YWH <-> Point` và `Point -> USDT`; tỷ giá do
  Admin thay đổi.
- Point hiện hữu trên web không được copy/cộng thêm vào game. Cần chọn một ledger
  authoritative và migrate/link sao cho chỉ còn một balance spendable.
- Mọi tiêu dùng game phải phát sinh hoa hồng YWH cho người giới thiệu tương tự HUB;
  tối thiểu gồm vật nuôi, cây dài/ngắn ngày, mồi câu, lượt vòng quay, lượt đào khoáng
  và mọi giao dịch được phân loại là tiêu dùng game.
- Debit Point và payout YWH phải liên kết bằng source transaction ID/transactional
  outbox để retry, refund hoặc timeout không trừ/trả hai lần.

ADR kỹ thuật `docs/ADR_POINT_WALLET_AUTHORITY.md` đã chốt cho candidate:

1. PostgreSQL game `player_economy.pos` là ledger Point spendable duy nhất của
   account đã link; web đóng băng balance Point legacy ở `0` và đọc balance ký HMAC.
2. Account legacy chưa link không tự cộng vào game. Mỗi account phải có báo cáo
   reconciliation và phê duyệt migration riêng.
3. Tỷ giá Admin tạo version bất biến; settlement dùng integer micros và lưu rate
   snapshot/rounding remainder. Retry không đọc lại tỷ giá mới.
4. Point dùng cho luồng web-side phải đi qua `reserve -> capture|release` idempotent
   trong PostgreSQL, không trừ một balance web thứ hai.

Nền authority v3 đã chứng minh identity pinning, single-balance projection, HMAC
credit/balance, Admin rate version và state machine reservation bằng test cô lập.
Ngày 17/07, extension web saga `Point -> USDT` hoàn tất durable request ID, pending
settlement journal, capture/release retry, exact fee/rate snapshot và cross-direction
lock; USDT pending không spendable trước capture. Overlay 17 file đã pass full Prisma
migration/DB E2E, Next.js build và credit/debit fault E2E trên source + SQLite
production bản sao.

Nền này sau đó đã deploy production ở trạng thái dormant: game release
`a22312df3aee5701a31aa502d2fea3728546b2b1`, web release
`/var/www/ywonder-releases/point-v3-a22312df`, build `2rdR_xG8o4G1uonGEYEg0`.
Migration PostgreSQL `006` và schema authority/debit SQLite đã áp cộng thêm nhưng
các bảng link/conversion/debit/reservation vẫn rỗng; cả hai service giữ
`WEB_POINT_WALLET_DEBIT_ENABLED=false`, public credit/reserve `404`. Không có
payment, conversion, debit, link hoặc migration balance. `Point -> YWH`, transfer,
rút ngoài và payout hoa hồng vẫn còn; cổng tiếp theo là identity QA riêng cùng
canary không tiền được duyệt riêng, không chuyển `WEB_TOPUP_MODE=open`.

---

## Cập nhật quyết định 15/07/2026

Phần roadmap ngày 06/07 bên dưới được giữ làm lịch sử hình thành MVP. Các quyết định
mới sau đây thay thế những câu hỏi cũ về tiền tệ và phạm vi nạp:

- Game chỉ còn một tiền tệ là `Point`; `UPoint` nghỉ khỏi runtime, HUD, API và
  schema active. Dữ liệu UPoint cũ chỉ được archive để đối soát, không tự quy đổi.
- Web là nguồn xác nhận giao dịch nạp; PostgreSQL game giữ số dư Point dùng trong
  gameplay. Website gửi callback server-to-server tới game-server sau khi giao dịch
  đạt trạng thái thành công cuối cùng.
- Callback dùng transaction ID bất biến, HMAC, timestamp và idempotency. Unity
  không biết secret và không gửi số Point cần cộng.
- Hạ tầng nhận top-up phía game-server đã được triển khai local và kiểm thử; mặc
  định vẫn tắt. Chưa bật tiền thật cho tới khi audit đúng điểm duyệt giao dịch web,
  triển khai retry/outbox và khóa các endpoint client còn có thể tự cộng Point/item.
- Browser SSO, PostgreSQL, HTTPS/WSS, phiên đơn và farm xuyên thiết bị đã qua các
  cổng MVP được ghi trong `task.md` và `docs/CONTEXT_RECOVERY.md`; các đoạn
  "chưa có" phía dưới phải được hiểu theo mốc lịch sử 06/07.

---

## 0. Yêu cầu trước mắt từ sếp (chốt ngày 06/07/2026)

Yêu cầu thô cần chuyển thành roadmap kỹ thuật:

- Khách hàng phải đăng nhập game bằng tài khoản web hiện có hoặc tài khoản đã được cấp sẵn.
- Khách phải có tài khoản trước khi chơi; MVP không dùng guest account.
- Một tài khoản web tương ứng đúng một nhân vật game.
- Nếu tài khoản web bị khóa hoặc xóa mềm, game cũng phải chặn đăng nhập/không cho dùng dữ liệu đó.
- Người chơi vào game online realtime để chat và thấy/tương tác cơ bản với nhau trên các đảo công cộng.
- Đảo farm không thuộc realtime công cộng trong yêu cầu trước mắt; farm là không gian riêng theo tài khoản và sẽ sync bằng game-state riêng. Chat thế giới vẫn là kênh global cho người đang online, kể cả khi người chơi đang ở farm.
- MVP sắp tới chưa cần làm hệ thống nạp/rút. Ưu tiên số 1 là chứng minh yếu tố online + realtime cho khách hàng.
- Sếp cần xem được dashboard backend online sau này, nhưng bản dashboard online phải có đăng nhập admin, phân quyền và audit log.

Roadmap dưới đây tách yêu cầu này thành từng loop có thể code, demo và kiểm tra được.

---

## 1. Kết luận ngắn

Game cần 3 lớp rõ ràng:

| Lớp | Vai trò | Chủ sở hữu dữ liệu |
|---|---|---|
| Web hiện có | Tài khoản người dùng, đăng nhập, thông tin định danh, có thể có Point web | Web/VPS |
| Game backend | Cầu nối giữa Unity và web, lưu trạng thái game, kiểm tra giao dịch game | Game server |
| Unity client | Hiển thị, điều khiển, gửi yêu cầu hành động | Không phải nguồn dữ liệu quyền lực |

Quy ước cần chốt để hết mơ hồ:

- Tài khoản web là danh tính gốc. Người chơi đăng nhập game bằng tài khoản web hoặc tài khoản được cấp sẵn nhưng vẫn phải map vào cùng mô hình `web_user_id -> playerId`.
- Game backend map `web_user_id -> playerId` và lưu trạng thái game riêng theo `playerId`.
- 1 tài khoản = 1 nhân vật game trong MVP.
- MVP online/realtime ưu tiên account bridge, token, realtime public islands và trạng thái online tối thiểu. Tiền game, inventory, shop, daily limit, farm/animal state chuyển dần sang server-authoritative sau lát realtime.
- Unity không giữ `GAME_API_SECRET`, không gọi thẳng API web nội bộ.
- Theo mốc 06/07, nạp/rút chưa nằm trong MVP đầu tiên. Quyết định 15/07 đã mở luồng nạp web -> Point; Unity vẫn tuyệt đối không được tự cộng ví.
- Realtime trước mắt chỉ áp dụng cho đảo công cộng như `city`/`mine`; farm không share realtime công cộng. Client rời room public khi vào farm nhưng vẫn giữ WebSocket để nhận/gửi chat global.

Quyết định 06/07/2026 từ trao đổi với anh: `Point` là tiền trong game và cũng là tiền có thể nạp từ web để dùng trong game, nhưng MVP sắp tới chưa làm nạp/rút. Trong MVP online/realtime, nếu cần hiển thị hoặc test shop thì `Point` tạm là game-server currency có transaction/idempotency; phần web wallet/top-up/spend chuyển sang phase sau. Unity vẫn chỉ gọi game-server.

Các điểm trên đã được chốt ngày 15/07: không còn UPoint; web xác nhận giao dịch
nạp, còn PostgreSQL game giữ số dư Point gameplay. Điểm chưa chốt là nơi/trạng thái
duyệt giao dịch thật trên web và cơ chế outbox/retry để chuyển giao dịch sang game.

---

## 2. Hiện trạng thật hôm nay

Đã có:

- `server/` Node/Express backend MVP.
- `POST /auth/web-login` làm adapter để game-server gọi web auth hoặc mock auth.
- `/player/bootstrap` trả `player_profile`, `economy`, `inventory`, `farm_state`, `daily_limits`.
- `/player/economy/apply`, `/player/inventory/adjust`, `/player/daily-limits/consume` có `idempotency_key`.
- Dashboard local `/admin` để xem/sửa dữ liệu demo trong JSON store.
- PostgreSQL target schema đã có trong `server/schema.sql`.

Chưa xong:

- Unity chưa dùng `/player/bootstrap` làm nguồn chính cho `EconomyManager` và `InventoryManager`.
- Shop mua/bán trong Unity vẫn đang đổi `PlayerPrefs` local, chưa gọi backend transaction.
- `STORE_MODE=postgres` mới là scaffold, chưa có query thật.
- Web account là nguồn đăng nhập nhưng hành trình product chưa được mô tả rõ trong kịch bản cũ.
- Đã chốt hướng `Point` vừa là tiền game vừa là tiền nạp từ web, nhưng MVP sắp tới chưa làm nạp/rút; web wallet/spend/reserve chuyển sang phase sau.
- Dashboard `/admin` hiện là dev/local dashboard, chưa phải dashboard online an toàn cho sếp xem.

Hệ quả hiện tại:

- Nếu 5 máy cùng đăng nhập `DemoRich01`, backend có thể có cùng `playerId`, nhưng mua/bán trong game vẫn local nên các máy chưa ảnh hưởng nhau.
- Dashboard chứng minh backend có dữ liệu, nhưng chưa chứng minh gameplay đã server-authoritative.

---

## 3. Hành trình tài khoản chuẩn

### Bước 1 - Người dùng có tài khoản web

Nguồn dữ liệu:

- Web tạo và quản lý account.
- Web hiện có đăng nhập bằng email, số điện thoại và mật khẩu.
- Web trả về `web_user_id`, `username`/`refCode` hoặc thông tin đủ để game-server xác thực.
- Web cần trả trạng thái tài khoản: `active`, `locked`, `soft_deleted` hoặc tương đương.

Kết quả mong đợi:

- Một người dùng có một định danh web ổn định.
- Nếu người dùng đổi tên hiển thị, `web_user_id` vẫn không đổi.
- Nếu tài khoản bị khóa/xóa mềm trên web, game-server chặn đăng nhập hoặc khóa gameplay online.

Kiểm tra:

- Gọi API web auth với username/password.
- Response có `userId` hoặc `user_id`.
- Response có trạng thái tài khoản và payload đủ để game-server verify.
- Khóa mềm một tài khoản test trên web, đăng nhập game phải bị từ chối với lỗi rõ ràng.

### Bước 2 - Unity đăng nhập qua game backend

Unity gọi:

```text
POST /auth/web-login
```

Game backend làm:

1. Nhận email/số điện thoại/username và password từ Unity, hoặc nhận tài khoản được cấp sẵn trong môi trường demo.
2. Gọi API web bằng `GAME_API_SECRET` ở server-side.
3. Verify response của web và trạng thái tài khoản.
4. Tìm hoặc tạo `game_players` theo `web_user_id`.
5. Trả về game JWT cho Unity.

Kết quả mong đợi:

- Unity chỉ biết game token.
- Game backend có `playerId` ổn định cho `web_user_id`.
- Một account web login ở nhiều máy vẫn ra cùng `playerId`.

Kiểm tra:

```powershell
$login = irm http://127.0.0.1:3000/auth/web-login `
  -Method Post -ContentType application/json `
  -Body '{"username":"DemoRich01","password":"demo"}'
$login.playerId
$login.webUserId
```

### Bước 3 - Game load trạng thái từ backend

Unity gọi:

```text
GET /player/bootstrap
Authorization: Bearer <game-token>
```

Backend trả:

```json
{
  "player_profile": {},
  "economy": {},
  "inventory": {},
  "farm_state": {},
  "daily_limits": {}
}
```

Unity cần làm:

- Nạp `player_profile` vào profile service.
- Nạp `economy` vào `EconomyManager`.
- Nạp `inventory` vào `InventoryManager`.
- Nạp `daily_limits` vào hệ câu cá/đào đá.
- Farm/build/animal có thể làm sau theo phase riêng.

Kết quả mong đợi:

- Mở game trên máy A và máy B cùng account thì ban đầu thấy cùng tiền/túi đồ.
- Dashboard `/admin` và HUD/inventory trong Unity cùng số liệu.

Kiểm tra:

- Sửa POS của `DemoRich01` trên dashboard.
- Đăng nhập lại Unity bằng `DemoRich01`.
- HUD phải hiện POS mới từ backend, không phải giá trị PlayerPrefs cũ.

### Bước 4 - Gameplay gửi hành động, không tự sửa dữ liệu nhạy cảm

Ví dụ shop bán cá:

Unity gửi:

```text
POST /player/shop/sell
{
  "item_id": "fish_ca_hoang_de_01",
  "quantity": 2,
  "idempotency_key": "client-action-guid"
}
```

Backend kiểm tra:

1. Người chơi có đủ item không.
2. Item có được bán ở shop này không.
3. Giá bán server biết là bao nhiêu.
4. Trừ item, cộng Point, ghi transaction trong một thao tác.
5. Trả `economy`, `inventory`, `transaction`.

Kết quả mong đợi:

- Client không tự khai giá.
- Retry cùng `idempotency_key` không cộng tiền hai lần.
- Máy khác reload cùng account thấy inventory và Point đã đổi.

Kiểm tra:

- Máy A bán 2 cá, dashboard thấy Point tăng và cá giảm.
- Máy B cùng account reload/bootstrap, thấy cùng số liệu mới.
- Gửi lại cùng request/idempotency key, transaction báo duplicate và số tiền không tăng lần nữa.

---

## 4. Loop phát triển hoàn chỉnh

Mỗi vòng chỉ chọn 1 lát nhỏ để giảm rủi ro.

### Loop 0 - Ghi rõ contract và dữ liệu

Việc làm:

- Viết API contract.
- Chốt owner dữ liệu: web owner identity, game backend owner game state.
- Chốt trạng thái MVP online/realtime: chưa làm nạp/rút; account + realtime public islands là lát chứng minh đầu tiên.
- Ghi riêng backlog phase sau cho web wallet: `balance`, `credit/top-up`, `debit/spend`, `transaction/ref idempotency`.

Kết quả:

- Không còn câu "backend có rồi nhưng không biết làm gì".
- Có checklist để demo cho sếp.

Kiểm tra:

- Một người mới đọc `docs/WEB_GAME_BACKEND_JOURNEY.md` hiểu được flow.

### Loop 1 - Login web account vào game account

Việc làm:

- Unity login qua `/auth/web-login`.
- Game-server map `web_user_id -> playerId`.
- Dashboard hiện player theo `web_user_id`.

Kết quả:

- Account web là account game.

Kiểm tra:

- Login cùng username trên 2 máy ra cùng `playerId`.
- Login 2 username khác nhau ra 2 `playerId`.

### Loop 2 - Bootstrap economy + inventory

Việc làm:

- Thêm client DTO cho bootstrap.
- Thêm method nạp server economy/inventory vào managers.
- Giữ fallback local nếu mất mạng, nhưng có log rõ đang offline/local.

Kết quả:

- Tiền/túi đồ trên Unity khớp dashboard sau login.

Kiểm tra:

- Sửa `DemoRich01` trong `/admin`, login lại Unity thấy đúng.
- Clear PlayerPrefs local, login lại vẫn có dữ liệu từ backend.

### Loop 3 - Shop buy/sell server-authoritative

Việc làm:

- Backend thêm endpoint shop transaction hoặc server-side transaction wrapper.
- Unity shop gọi backend khi online.
- Backend trả state mới cho Unity cập nhật HUD/inventory.

Kết quả:

- Giao dịch mua/bán thật sự làm đổi backend.
- Nhiều máy dùng cùng account thấy cùng kết quả sau sync/reload.

Kiểm tra:

- Máy A bán cá, máy B reload thấy Point tăng và cá giảm.
- Dashboard transaction có record.
- Retry cùng key không nhân đôi.
- Không đủ tiền/item trả lỗi và Unity không đổi local state.

### Loop 4 - Daily limit server-side cho câu cá/đào đá

Việc làm:

- Unity đọc `daily_limits` từ bootstrap.
- Câu cá/đào đá consume lượt qua backend khi online.
- Offline fallback chỉ dùng cho demo hoặc có rule rõ.

Kết quả:

- 10 lượt/ngày không bị reset bằng cách đổi máy.

Kiểm tra:

- Máy A dùng hết 10 lượt đào.
- Máy B cùng account không đào tiếp được trong cùng ngày.

### Loop 5 - Farm/build/animal sync

Việc làm:

- Chuẩn hóa `farm_state` server schema.
- Gửi snapshot hoặc action log khi trồng, tưới, xây, thả thú, cho ăn.
- Dùng server time cho grow/hunger/death.

Kết quả:

- Farm của một account là một trạng thái chung.
- Đóng/mở app hoặc đổi máy vẫn thấy farm đúng.

Kiểm tra:

- Máy A trồng cây.
- Máy B login sau thấy cây đó.
- Đổi giờ máy không làm cây lớn sai nếu đang online.

---

## 5. Quy tắc offline cần chốt

Có 3 mức lựa chọn:

| Mức | Mô tả | Ưu điểm | Rủi ro |
|---|---|---|---|
| A - Online-only cho giao dịch nhạy cảm | Mua/bán, nhận thưởng, câu/đào daily limit phải có mạng | Dễ chống cheat, dễ debug | Mất mạng thì bị chặn vài tính năng |
| B - Offline queue | Cho chơi offline, ghi action queue, sync sau | Trải nghiệm tốt hơn | Cần xử lý conflict/cheat phức tạp |
| C - Local demo fallback | Nếu mất mạng thì dùng PlayerPrefs, không hứa sync production | Nhanh cho demo | Dễ bị hiểu nhầm là backend hoàn chỉnh |

Quyết định 06/07/2026 từ anh: mất mạng thì không cho mua/bán. Vì vậy MVP chọn A cho shop, ví Point, daily limit và các giao dịch nhạy cảm. Có thể giữ C cho demo offline không nhạy cảm, nhưng UI/log phải ghi rõ đang dùng local fallback và không hứa sync production. Chưa nên chọn B trong giai đoạn crunch vì dễ mở rộng quá sâu.

---

## 6. Kết quả phỏng vấn backend ngày 06/07/2026

### 6.1 Đã chốt với anh

| Nhóm | Quyết định | Ý nghĩa kỹ thuật |
|---|---|---|
| Tài khoản | Web hiện có đăng nhập bằng email/số điện thoại/password, chưa dùng token từ Unity | Unity gửi credential tới game-server; game-server gọi web auth server-side |
| Tài khoản | Một tài khoản có đúng một nhân vật | DB game dùng unique mapping `web_user_id -> playerId`; không làm nhiều slot nhân vật trong MVP |
| Tài khoản | Một account có thể đăng nhập nhiều máy | Tiền/túi đồ/shop/daily/farm phải lấy từ server, không được phụ thuộc PlayerPrefs từng máy |
| Tài khoản | Account bị khóa/xóa mềm thì game cũng mất quyền dùng | Game-server phải hỏi/nhận trạng thái account từ web và chặn login/gameplay online |
| Tài khoản | Khách phải tạo tài khoản trước khi chơi | Không làm guest/anonymous trong MVP backend thật |
| Tiền | `Point` vừa là tiền trong game vừa là tiền nạp | `Point` phải có ledger giao dịch rõ ràng; mọi cộng/trừ nhạy cảm phải qua server |
| Phạm vi MVP | MVP sắp tới chưa cần nạp/rút | Không làm web wallet/top-up/spend trong lát MVP; tập trung online + realtime cho khách hàng |
| Gameplay online | Ưu tiên online tiền, túi đồ, shop, farm/thú trước | Loop gameplay-state đi theo thứ tự dễ kiểm chứng: economy/inventory/shop trước, farm/animal sau |
| Offline | Mất mạng thì không cho mua/bán | Shop và ví Point là online-only |
| Daily/server time | Daily limit và timer nhạy cảm tính theo giờ server | Không tin giờ điện thoại; cần chốt timezone server, khuyến nghị giờ Việt Nam cho khách VN |
| Admin | Sếp/admin có thể chỉnh dữ liệu | Dashboard online cần login, role super admin và audit log |
| Demo data | Dữ liệu demo có thể reset | Cần endpoint/tool reset riêng cho staging/demo, không dùng trên production khách thật |

### 6.2 Còn cần hỏi sếp/web team

| Câu hỏi | Vì sao cần chốt | Khuyến nghị của bé |
|---|---|---|
| `UPoint` còn vai trò gì không? | Đã chốt ngày 15/07 | Không; archive legacy để audit, không dùng trong sản phẩm |
| Web đã có API trừ tiền/spend/reserve chưa? | Mua vật phẩm bằng tiền nạp cần trừ ví an toàn | Chưa cần cho MVP sắp tới; hỏi để chuẩn bị phase nạp/rút sau |
| Web hay game-server là ledger cuối cùng của `Point`? | Quyết định nơi tính số dư chính thức | MVP online/realtime chưa cần chốt đến mức block; nếu test shop thì game-server làm ledger demo trước |
| Tên miền API public là gì? | Unity cần base URL ổn định | Đã chốt `https://api.ywonder.net/game-api` cho REST và `wss://api.ywonder.net/game-api/realtime` cho realtime; `/api/game/*` vẫn dành cho web API cũ |
| Server đặt máy case có chạy production thật không? | Máy case có rủi ro điện/mạng/IP/backup | Có thể dùng cho staging/MVP; production lâu dài nên cân nhắc VPS/cloud hoặc tối thiểu backup + UPS + domain + monitoring |
| Backup DB như nào? | Mất DB là mất tiền/túi đồ/farm | PostgreSQL dump hằng ngày, giữ nhiều bản, có test restore |
| Dev/staging/prod phân thế nào? | Tránh demo reset nhầm dữ liệu thật | Tối thiểu có local/dev và staging; production chỉ mở khi domain/backup/admin/audit sẵn sàng |
| Dashboard admin cho sếp xem online có phạm vi nào? | Admin chỉnh tiền/item rất nhạy cảm | Làm super admin trước, nhưng mọi chỉnh sửa phải ghi actor/time/reason |

### 6.3 Tư vấn kiến trúc backend cho MVP

- DB production/staging nên dùng PostgreSQL. JSON store chỉ dùng dev/local test; SQLite cũng chưa đủ tốt cho giao dịch tiền và lịch sử audit nếu game online mở rộng.
- Nếu sếp yêu cầu server đặt ở máy case, có thể chạy Node server và PostgreSQL trên cùng máy trong MVP/staging. Điều kiện tối thiểu: tự khởi động lại khi mất điện, HTTPS, firewall/router đúng, backup DB, log lỗi và test từ điện thoại ngoài mạng LAN.
- Game-server là cổng duy nhất cho Unity. Unity không giữ secret web. MVP sắp tới không gọi API ví web vì chưa làm nạp/rút.
- Với `Point`, nếu MVP cần test shop hoặc số dư chung nhiều máy thì mọi thay đổi vẫn nên tạo transaction có `idempotency_key`. Nếu request bị retry, server trả kết quả cũ và không cộng/trừ lần hai.
- Farm không nên đưa vào realtime công cộng. Farm là state riêng theo account; sync bằng `/player/farm-state` hoặc action log, dùng server time cho cây/thú.
- Realtime trước mắt chỉ gồm presence, chat, vị trí/yaw/animation, emote và tương tác nhẹ trong room công cộng. Không để client tự quyết định tiền/item qua realtime.

---

## 7. Demo script cho sếp

### Demo hiện tại

Nói rõ:

- Backend MVP đã có API, dashboard, data player, storage facade, daily limit và idempotency.
- Đây là nền để nối game online, nhưng gameplay Unity chưa hoàn toàn server-authoritative.
- Khi web thật đang sập hoặc chưa có tài khoản web, dùng `WEB_AUTH_MODE=mock` để test bằng tài khoản cấp sẵn.

Chứng minh:

1. Mở `/admin`.
2. Chọn `DemoRich01`.
3. Xem profile/economy/inventory/dailyLimits/farmState.
4. Gọi `/auth/web-login` và `/player/bootstrap` để chứng minh game account load được data server.

### Demo Phase 1 bằng tài khoản khách tự đăng ký

Mục tiêu của Phase 1 là có bản online/realtime để khách vào chơi, chưa làm nạp/rút.
Nếu web account thật chưa ổn định, chạy game-server bằng tài khoản game local:

```powershell
cd server
$env:WEB_AUTH_MODE="disabled"
$env:STORE_MODE="json"
$env:PORT="3000"
npm.cmd start
```

Unity register gọi `/auth/register` với `username/password/email`.
Unity login gọi `/auth/login` trước; chỉ khi server báo `USER_NOT_FOUND` mới fallback sang web bridge.
Không public cho khách bằng `WEB_AUTH_MODE=mock`, vì mock là chế độ giả lập account cấp sẵn, không kiểm tra password thật.

Smoke test tự động:

```powershell
cd server
$env:PHASE1_TEST_BASE_URL="http://127.0.0.1:3000"
npm.cmd run test:phase1
```

Kết quả pass cần có:

- Tạo được 2 tài khoản mới, lưu username/email/password_hash trong JSON store.
- Sai mật khẩu bị chặn.
- Login lại vẫn load được `/player/bootstrap`.
- Point, inventory và farm-state đổi qua API rồi login lại vẫn còn.
- Retry cùng `idempotency_key` không cộng/trừ đôi.
- 2 account join `city` và chat realtime được.

### Demo realtime bằng tài khoản cấp sẵn khi web sập

Account test:

```text
DemoRealtime01 / demo
DemoRealtime02 / demo
```

Chạy server:

```powershell
cd server
$env:WEB_AUTH_MODE="mock"
npm.cmd start
```

Smoke test tự động:

```powershell
cd server
$env:REALTIME_TEST_BASE_URL="http://127.0.0.1:3000"
npm.cmd run test:realtime
```

Kết quả pass cần có:

- 2 account login qua `/auth/web-login` và có `playerId` riêng.
- Cả hai join `city`, nhận `welcome` và `player_joined`.
- Account A gửi chat, account B nhận được.
- Account B gửi `player_state`, account A nhận được remote state.
- Join `farm` bị chặn bằng `ROOM_NOT_SHARED`, đúng vì farm không phải realtime public room. Client không join room vẫn phải nhận/gửi được chat global.

### Demo sau Loop 3

Nói rõ:

- Account web là account game.
- Shop mua/bán đã đi qua backend.
- Nhiều máy cùng account nhìn cùng số dư sau sync.

Chứng minh:

1. Máy A login `DemoRich01`, thấy 500000 Point.
2. Máy B login `DemoRich01`, thấy 500000 Point.
3. Máy A bán cá hoặc mua seed.
4. Dashboard transaction xuất hiện, economy/inventory đổi.
5. Máy B reload/bootstrap, thấy số mới.
6. Retry cùng action không cộng/trừ đôi.

---

## 8. Định nghĩa hoàn thành cho backend MVP thật

Backend MVP được coi là đủ rõ khi đạt các điều kiện này:

- Web account hoặc tài khoản được cấp sẵn login vào game qua game-server, Unity không giữ secret.
- Game backend map ổn định `web_user_id -> playerId`.
- 1 account = 1 nhân vật, đăng nhập nhiều máy ra cùng `playerId`.
- Account web bị khóa/xóa mềm thì game-server chặn login/gameplay online.
- `/player/bootstrap` là nguồn load đầu game tối thiểu cho profile và dữ liệu cần thiết để vào online.
- Realtime đảo công cộng (`city`/`mine`/đảo non-farm) có presence và remote player state; chat là kênh global cho client online; farm không share realtime công cộng.
- 2 thiết bị/tài khoản khác nhau test được từ cùng môi trường demo: cùng vào `city` hoặc `mine`, thấy nhau và chat được.
- Mất mạng hoặc token sai thì realtime/login lỗi rõ ràng, không treo game.
- Dashboard online cho sếp có login admin, role super admin tối thiểu và audit log nếu cho chỉnh dữ liệu.
- Tài liệu ghi rõ phần nào còn local/offline fallback.

Chưa cần trong MVP này:

- Nạp/rút, IAP App Store/Google Play và validate receipt thật. Ví nạp từ web là luồng riêng cho phase sau.
- Shop buy/sell server-authoritative hoàn chỉnh nếu mục tiêu demo chỉ là online + realtime. Có thể làm sau khi lát realtime ổn.
- Web wallet API `debit/spend/reserve`.
- Bạn bè/thăm farm.
- Offline queue phức tạp.
- Farm/build/animal sync hoàn chỉnh realtime. Farm/animal chỉ cần sync state theo tài khoản ở phase sau economy/shop nếu không kịp.
- Admin dashboard production có phân quyền nhiều cấp. MVP chỉ cần super admin an toàn, có login và audit.

---

## 9. Roadmap rõ ràng theo yêu cầu trước mắt của sếp

Thứ tự bắt đầu sau khi chốt "MVP chưa cần nạp/rút":

1. **Account bridge**: đăng nhập bằng tài khoản web/cấp sẵn, trả game JWT ổn định.
2. **Realtime public islands**: city/mine có presence và remote player state; chat global không phụ thuộc cùng room; farm không join realtime công cộng.
3. **Hạ tầng demo online**: chạy server ổn định, điện thoại/PC ngoài máy dev kết nối được REST + WebSocket.
4. **State sync tối thiểu**: bootstrap profile/economy/inventory chỉ để nhiều máy cùng account không lệch dữ liệu cơ bản.
5. **Shop/economy/farm server-authoritative**: làm sau khi lát online/realtime đã pass.

### Phase A - Account bridge: web/cấp sẵn -> game player

Việc làm:

- Chuẩn hóa endpoint login game: Unity gửi email/số điện thoại/username + password tới game-server.
- Game-server gọi web auth hoặc provider tài khoản cấp sẵn, rồi map về một `web_user_id`.
- Tạo/đọc `game_players` theo `web_user_id`, bảo đảm unique 1 account = 1 player.
- Kiểm tra trạng thái account `active/locked/soft_deleted` trước khi cho vào game.
- Trả game JWT cho Unity; Unity dùng token này cho REST và WebSocket.

Kết quả:

- Khách dùng tài khoản web hoặc tài khoản được cấp sẵn để vào game.
- Nhiều máy đăng nhập cùng account vẫn là cùng một player.
- Account bị khóa/xóa mềm không vào game được.

Kiểm tra:

- Login bằng email, số điện thoại và tài khoản cấp sẵn.
- Login cùng account trên 2 thiết bị, gọi `/player/bootstrap` thấy cùng `playerId`.
- Khóa mềm account test trên web, thử login game phải bị từ chối.

### Phase B - Realtime public islands, loại trừ farm

Việc làm:

- Dùng WebSocket `wss://api.ywonder.net/game-api/realtime?token=<game-jwt>`.
- Room realtime chỉ cho các đảo công cộng: `city`, `mine`, và đảo non-farm sau này.
- Đồng bộ presence, join/leave, vị trí/yaw/animation, emote và tương tác nhẹ theo room public; chat là kênh toàn server cho client còn online.
- Farm không join room công cộng; farm là private instance theo account. Vào farm thì rời shared room, không thấy remote player, nhưng vẫn giữ chat global.

Kết quả:

- Người chơi thấy nhau và chat được ở city/mine.
- Không có chuyện người lạ vào realtime farm của người chơi trong phase này.

Kiểm tra:

- 2 điện thoại ngoài LAN đăng nhập 2 tài khoản khác nhau, cùng vào `city`, thấy nhau và chat được.
- Cùng chuyển sang `mine`, vẫn thấy nhau/chat được.
- Vào `farm`, client không join room shared/không thấy remote player; chat global vẫn nhận/gửi được nếu WebSocket còn kết nối.

### Phase C - State sync tối thiểu (mốc lịch sử trước quyết định nạp 15/07)

Việc làm:

- Unity load `Point` và inventory từ `/player/bootstrap`; UPoint đã nghỉ sau quyết định 15/07.
- Ưu tiên đọc đúng dữ liệu sau login để nhiều máy cùng account không lệch trạng thái cơ bản.
- Nếu test shop trong MVP, shop gọi game-server và dùng `Point` game-server demo, không gọi web wallet.
- Không làm nạp/rút, không gọi `debit/spend/reserve` trong MVP online/realtime.
- Mọi request ghi dữ liệu nếu có retry phải có `idempotency_key`.

Kết quả:

- Người A và B đăng nhập cùng account reload/bootstrap thấy cùng profile/số dư/túi đồ demo.
- Nếu bật shop demo, mua/bán trong game làm đổi dữ liệu backend demo.
- Retry request không nhân đôi tiền/item.

Kiểm tra:

- Dashboard đang thấy `DemoRich01` có 500000 Point.
- Máy A login thấy đúng số từ backend, không phải PlayerPrefs cũ.
- Máy B cùng `DemoRich01` reload thấy cùng số.
- Nếu bật shop demo: máy A mua seed/bán cá, dashboard thấy transaction và số dư mới.
- Tắt mạng rồi bấm mua/bán, Unity báo cần kết nối và không đổi state nhạy cảm.

### Phase D - Daily limit, farm và animal sync theo server time

Việc làm:

- Daily limit câu cá/đào đá dùng server time, không dùng giờ điện thoại.
- Chốt timezone reset ngày: khuyến nghị `Asia/Saigon` nếu khách chủ yếu ở Việt Nam; nếu giữ UTC thì UI/tài liệu phải nói rõ.
- Farm/build/animal chuyển dần sang server state hoặc action log.
- Cây/thú dùng mốc thời gian server cho grow/hunger/death.

Kết quả:

- Đổi máy/đổi giờ điện thoại không reset lượt hay làm sai timer.
- Farm của cùng account đồng bộ giữa nhiều máy sau khi reload.

Kiểm tra:

- Máy A dùng hết 10 lượt đào, máy B cùng account không đào tiếp được trong cùng ngày server.
- Máy A trồng cây/cho thú ăn, máy B login sau thấy state tương ứng.
- Đổi giờ điện thoại không làm timer nhảy sai khi online.

### Phase E - Dashboard cho sếp/admin

Việc làm:

- Chuyển `/admin` từ dev dashboard thành dashboard online có login.
- Thêm role `super_admin` trước; sau này mới tách role nhỏ hơn.
- Cho xem/tìm player, profile, Point, inventory, farm/animal, daily limits và transactions.
- Cho chỉnh dữ liệu nhạy cảm nhưng bắt buộc ghi audit: admin nào, lúc nào, chỉnh gì, lý do gì.
- Có nút reset dữ liệu demo/staging riêng, không trộn với production.

Kết quả:

- Sếp mở dashboard xem được backend game đang có dữ liệu thật.
- Sửa tiền/item/player state có dấu vết audit, không phải sửa DB tay.

Kiểm tra:

- Truy cập `/admin` khi chưa login bị chặn.
- Login super admin, sửa Point của player test, game reload thấy số mới.
- Audit log ghi đúng actor/time/before/after/reason.

### Phase F - Hạ tầng máy case/VPS/DB

Việc làm:

- Chạy Node game-server bằng service auto-start trên máy case hoặc VPS.
- Chạy PostgreSQL cho staging/production; JSON store chỉ dùng dev.
- Cấu hình HTTPS và WebSocket Upgrade qua Caddy/Nginx.
- Domain production đã nghiệm thu: `https://api.ywonder.net/game-api` cho REST, `wss://api.ywonder.net/game-api/realtime` cho realtime.
- Thiết lập firewall/router port 80/443, static IP hoặc DNS ổn định.
- Thiết lập backup DB tự động, log lỗi, giám sát uptime và kế hoạch restore.

Kết quả:

- Điện thoại ngoài mạng công ty kết nối được REST và realtime.
- Mất điện/restart máy case thì server tự chạy lại.
- Có bản backup để khôi phục khi DB lỗi.

Kiểm tra:

- Dùng 4G/5G gọi `/health` thành công.
- 2 điện thoại ngoài LAN vào `city` chat/nhìn thấy nhau.
- Tắt/mở lại máy case, service tự chạy lại.
- Restore thử một bản backup PostgreSQL vào DB test.

---

## 10. Cập nhật hạ tầng/web auth từ chat 01/07/2026

Mục này chỉ ghi thông tin vận hành không nhạy cảm. Mật khẩu VPS, mật khẩu tài khoản test, SSH key, database password và `GAME_API_SECRET` không được ghi vào repo; các giá trị đó phải lưu riêng trong kênh bảo mật hoặc `.env` trên server.

### Đã biết

- Domain liên quan: `ywonder.net`, `api.ywonder.net`.
- DNS/web hiện được trao đổi với IP `45.119.83.233`.
- IP máy vật lý/game API được trao đổi: `113.171.82.46`.
- Port public cần dùng: `80` và `443`.
- Theo trao đổi, web dùng VPS riêng; game API dự kiến chạy ở máy vật lý/game-server riêng.
- Web login trên website đã có; web auth cho game đã có endpoint server-side.
- Web auth endpoint đang dùng được ngay: `POST https://ywonder.net/api/game/auth`.
- Web auth cần header `Authorization: Bearer <GAME_API_SECRET>`. Secret lưu riêng, không đưa vào Unity.
- Response web auth trả cả key camelCase và snake_case: `userId/user_id`, `refCode/ref_code`, `fullName/full_name`, `gameToken/game_token`, `expiresIn/expires_in`.
- `gameToken` là JWT HS256 chuẩn, payload gồm `{ sub, uid, username, iat, exp }`.
- Web đã có endpoint đọc/cộng Point cho phase sau: `GET https://ywonder.net/api/game/balance?uid=<username>` và `POST https://ywonder.net/api/game/credit`.
- Đã có tài khoản test web `gametest`; mật khẩu lưu riêng, không ghi repo.

### Trạng thái `api.ywonder.net`

- `api.ywonder.net` là target đẹp cho game API public nhưng chưa nên coi là đã sẵn sàng production.
- Theo bàn giao, vhost đã được cấu hình, nhưng SSL/cert đang bị chặn ở lớp hạ tầng/WAF/default-server; có trường hợp `api.ywonder.net:443` trả cert của site khác.
- Đây là vấn đề hạ tầng, không phải lỗi code Unity hay code game-server.
- Cách gỡ cần owner/infra: dùng DNS-01 challenge với credential DNS provider, hoặc sửa WAF/default-server để `/.well-known/acme-challenge/` đi tới đúng nginx của `api.ywonder.net`.
- Trong lúc chờ, game-server vẫn có thể gọi web auth qua `https://ywonder.net/api/game/auth`.

### Còn thiếu trước khi deploy game-server thật

- Quyền truy cập máy chạy game-server thật hoặc VPS sẽ host game-server.
- Quyết định game-server chạy ở máy vật lý `113.171.82.46` hay VPS.
- Cấu hình proxy/domain public cho REST và WebSocket game-server.
- Cơ sở dữ liệu thật cho game state: khuyến nghị PostgreSQL, có `DATABASE_URL` và backup.
- Service auto-start khi máy restart.
- Log lỗi và nơi xem log.
- Dashboard admin online có đăng nhập, role và audit log nếu cho chỉnh tiền/item/farm.
- Test ngoài LAN bằng 4G/5G: `/health`, đăng nhập, `/player/bootstrap`, WebSocket realtime.
