# Kế hoạch: đăng nhập và đăng ký game qua cổng web YWonder

Ngày cập nhật: 12/07/2026

Yêu cầu đã chốt:

- Giữ nguyên luồng **Đăng nhập local** và **Đăng ký local** đang hoạt động để người
  chơi vẫn có thể tạo tài khoản và vào game trong thời gian tích hợp.
- Bổ sung **Đăng nhập bằng web** và **Đăng ký trên web** như hai lựa chọn chạy
  song song; cả hai lựa chọn web đều mở `https://ywonder.net/vi/login`.
- Người chưa có tài khoản bấm **Tạo trang trại mới** ngay trên trang login.
- Sau khi xác thực trên web, game phải nhận đúng tài khoản web, tạo/nạp đúng một
  player và bootstrap dữ liệu chơi.
- Chỉ chuyển hoàn toàn sang web sau khi Web SSO, lưu dữ liệu, cross-device,
  restart và rollback đều đã nghiệm thu.

Trang đăng ký hiện ghi **Mã giới thiệu (tùy chọn)** nhưng source runtime vẫn đặt
thuộc tính HTML `required` cho ô này. Vì vậy hành vi thực tế hiện tại là bắt buộc;
không tự sửa cho tới khi BA/web chốt lại quy tắc.

## 1. Kết luận

Chỉ gọi `Application.OpenURL("https://ywonder.net/vi/login")` là chưa đủ. Cách đó
chỉ tạo phiên đăng nhập trong trình duyệt; Unity không đọc được cookie HttpOnly và
không biết tài khoản web nào vừa đăng nhập.

Vì web và game-server đang ở cùng VPS, có thể làm luồng browser login an toàn mà
không đưa mật khẩu web hoặc secret vào Unity. Phương án phù hợp cho cả EXE và APK
là **callback cùng domain + yêu cầu đăng nhập một lần + polling/PKCE**. Polling
là đường nhận kết quả chính cho cả EXE/APK. Custom URI chỉ đánh thức APK sau khi
web hoàn tất; URI này không mang token/request ID nên không làm lộ quyền đăng
nhập. EXE portable không đăng ký protocol vào Windows và vẫn hoàn tất khi người
dùng quay lại cửa sổ game.

Không thay callback của hai nút local hiện tại. Web SSO được thêm bằng control và
feature flag riêng, mặc định tắt cho bản khách cho đến khi callback và exchange
token hoạt động end-to-end. Nếu đổi luồng mặc định trước, người chơi có thể đăng
nhập web thành công nhưng bị kẹt ở ngoài game.

## 2. Kết quả audit trang web công khai

Đã kiểm tra trực tiếp bản đang chạy ngày 12/07/2026:

- `https://ywonder.net/vi/login` trả HTTPS `200`.
- Form login nhận **Email hoặc ID đăng nhập** và mật khẩu.
- Trang login có tham số `callbackUrl`; đăng nhập thành công sẽ điều hướng tới
  giá trị này, mặc định là `/vi/dashboard`.
- Liên kết **Tạo trang trại mới** hiện trỏ tới `/register` nhưng không mang theo
  `callbackUrl`.
- Trang register không đọc `callbackUrl`; đăng ký và đăng nhập thành công sẽ đưa
  thẳng tới `/vi/dashboard`.
- `/api/auth/session`, `/api/auth/providers` và `/api/auth/csrf` đang hoạt động
  theo Auth.js/NextAuth. Cookie phiên là HttpOnly + Secure và không được đưa vào
  Unity.
- Chưa thấy one-time code, game callback hoặc deep link sẵn có.

Do đó luồng **tài khoản đã có** có thể tận dụng `callbackUrl`, nhưng luồng **tạo
tài khoản mới** sẽ mất callback nếu web không sửa phần register.

### 2.1. Audit source/VPS ngày 12/07/2026

- Web Next.js chạy bằng `greenxland.service`, cổng nội bộ `3033`, source tại
  `/var/www/ywonder`.
- Thư mục deploy web không có Git metadata khả dụng. Trước mọi thay đổi phải tạo
  backup timestamp cho source/build, service và Nginx; rollback không thể dựa vào
  `git checkout` ngay trên VPS.
- `GAME_API_SECRET` đã có trong tiến trình web, dài 64 ký tự. Không in/copy secret
  qua chat hoặc đưa vào Unity; game-server có thể nhận nội bộ qua env trên VPS.
- `POST /api/game/auth` đã hoàn chỉnh: nhận username/email/refCode + password,
  bcrypt, rate limit 10 lần/phút/định danh, chỉ nhận account `ACTIVE`, trả stable
  web user ID và game token 12 giờ.
- NextAuth session gắn stable `session.user.id`; đủ làm khóa canonical
  `web_user_id -> playerId` cho browser SSO.
- Login đã đọc `callbackUrl`; register chưa giữ callback và luôn chuyển về
  `/{locale}/dashboard` sau đăng ký/OTP.
- Nginx hiện proxy `/game-api/*` sang game-server trên host `api.ywonder.net`.
  Host `ywonder.net` chưa có exact callback route sang game-server.

## 3. Luồng đích

1. Người chơi bấm Đăng nhập hoặc Đăng ký trong game.
2. Unity tạo `code_verifier`, tính PKCE challenge và gọi
   `POST /auth/browser/start` tới game-server.
3. Game-server tạo `requestId` ngẫu nhiên, hạn sử dụng ngắn và trả về URL đăng
   nhập dạng:
   `https://ywonder.net/vi/login?callbackUrl=<callback-cùng-domain>`.
4. Unity mở URL đó bằng trình duyệt và giữ màn hình chờ đăng nhập.
5. Người đã có tài khoản đăng nhập. Người mới bấm Tạo trang trại mới và đăng ký.
6. Sau khi web xác thực, trình duyệt vào callback cùng host
   `ywonder.net/api/game/browser/callback`; Next.js tự đọc cookie NextAuth HttpOnly.
7. Callback web lấy `session.user.id`, kiểm tra account `ACTIVE`, rồi gọi nội bộ
   `127.0.0.1:3000/auth/browser/approve` bằng `GAME_API_SECRET`. Cookie/secret
   không đi qua Unity hoặc Internet.
8. Unity gọi `POST /auth/browser/exchange` với `requestId + code_verifier`.
   Server kiểm tra PKCE, chỉ cho dùng một lần, map `web_user_id -> playerId`, rồi
   trả game JWT và bootstrap.
9. Unity vào game; callback thử mở `ywondergreenfarm://auth/complete` để đánh
   thức APK, còn mật khẩu web, cookie web và secret server không đi qua client.

Nếu Android tạm dừng app khi mở browser, exchange tiếp tục khi app được focus lại.
EXE có thể polling trong lúc cửa sổ browser đang mở.

## 4. Phần cần sửa ở web

Ba thay đổi nhỏ nhưng bắt buộc:

1. Link `Tạo trang trại mới` trên login phải giữ tham số `callbackUrl` khi sang
   register.
2. Register phải đọc và validate `callbackUrl`; sau khi đăng ký/xác thực thành
   công, điều hướng về callback thay vì luôn vào `/vi/dashboard`.
3. Thêm route Next.js `/api/game/browser/callback`: đọc session web hiện tại,
   kiểm tra account rồi approve request qua loopback game-server. Route trả trang
   thành công, thử mở APK và hướng dẫn người dùng EXE quay lại cửa sổ game.

Chỉ chấp nhận callback tương đối an toàn hoặc HTTPS thuộc allowlist
`ywonder.net`, `www`, `agent`, `admin`; từ chối protocol/host/port lạ để tránh
open redirect.

Nếu không có source web để sửa ngay, phương án tạm là: đăng ký xong quay lại game,
bấm Đăng nhập lần nữa và đăng nhập web. Phương án này dùng được để test, nhưng
không phải UX bàn giao mong muốn.

## 5. Phần cần sửa ở game-server

- Thêm bảng/record `browser_auth_requests` trong PostgreSQL:
  `request_id_hash`, `pkce_challenge`, `status`, `web_user_id`, `expires_at`,
  `consumed_at`.
- Thêm `POST /auth/browser/start` có rate limit.
- Thêm `POST /auth/browser/approve` chỉ nhận secret server-to-server và identity
  web đã được route Next.js xác thực; không log secret/request thô/token.
- Thêm `POST /auth/browser/exchange`; request một lần, hết hạn 2-5 phút, PKCE bắt
  buộc.
- Dùng mapping duy nhất `web_user_id -> playerId`, sau đó dùng lại game JWT,
  `/player/bootstrap` và realtime rule `4008` hiện tại.
- Thêm cleanup request hết hạn và test replay/PKCE/rate limit/account guard.
- Không cần đổi Nginx: callback thuộc namespace Next.js `/api/game/*`; game-server
  vẫn chỉ public dưới `api.ywonder.net/game-api` và port 3000/5432 tiếp tục đóng.

## 6. Phần cần sửa ở Unity

- Đổi URL cổng web trong `BackendConfig` thành `https://ywonder.net/vi/login`.
- Giữ nguyên tab/nút Đăng nhập local và Đăng ký local.
- Thêm hai nút phụ `Đăng nhập bằng web` và `Đăng ký trên web`; cả hai gọi cùng
  một `StartWebLoginAsync()`.
- Thêm feature flag cấu hình; khi flag tắt, UI và hành vi local không thay đổi.
- Không gửi username/password web từ Unity trong luồng mới.
- Lưu `requestId` và `code_verifier` chỉ trong bộ nhớ trong phiên đăng nhập.
- Poll/exchange khi app active lại; cho phép hủy và thử lại.
- Android manifest nhận `ywondergreenfarm://auth/complete` vào
  `UnityPlayerGameActivity` ở chế độ `singleTask`; URI chỉ wake app, polling mới
  thực hiện exchange.
- Hiện trạng thái rõ: đang mở trình duyệt, đang chờ xác thực, hết hạn, bị khóa,
  web tạm lỗi.
- Giữ account local/QARich và API local trong toàn bộ giai đoạn chuyển tiếp.
  Chỉ tắt sau khi có quyết định cutover riêng và backup/rollback đã kiểm tra.

## 7. Thứ tự triển khai

1. `[x]` Audit source/service web, stable session user ID, secret presence và
   Nginx route; không đọc giá trị secret và không thay đổi VPS.
2. `[x]` **Nấc A - cầu web credential song song:** release
   `5db92436a7974b38866fa3291f5f3e3577a2f30f` đã deploy versioned với
   `WEB_AUTH_MODE=http`, `AUTH_TRANSITION_MODE=parallel` và đăng ký local bật.
   PostgreSQL/env/unit đã backup, previous release còn để rollback; secret chỉ
   được copy nội bộ trong VPS, không qua client/chat.
3. `[x]` Nghiệm thu public: account game local và account web thật đều login,
   bootstrap, relogin thành công; cùng web account giữ playerId, còn web/local
   có dữ liệu tách biệt. Không sửa/restart web service hoặc Nginx. Unity source
   giữ form local và thêm nút web; Editor compile sạch, runtime EXE/APK còn chờ.
4. `[x]` **Nấc B - browser SSO backend:** release
   `67ff2565517875fcc48ea515f1fedbbf98f24b8a` đã apply migration,
   start/approve/exchange, PKCE, expiry/replay guard và bật production mà vẫn giữ
   mode `parallel`.
5. `[x]` Web build `hp9br9UtY4p9PcC214CmF` đã preserve `callbackUrl` xuyên
   login/register/OTP và thêm callback Next.js. Backup source + `.next` ở
   `/var/backups/ywonder-web/browser-sso-20260712T185239Z`; health pass.
6. `[x]` Audit chốt không cần sửa Nginx vì callback nằm trong Next.js
   `/api/game/browser/callback`; port 3000/5432 vẫn không public.
7. `[~]` Public runner bằng web session thật đã pass callback -> exchange ->
   bootstrap PostgreSQL; còn test trực tiếp trên Editor, EXE và APK.
8. `[~]` Feature flag Unity đã bật; chờ compile/import Android manifest và build.
9. Test cross-device, restart backend/PostgreSQL, account khóa/xóa, replay code và
   hai phiên trùng account.
10. Sau khi chạy song song ổn định mới lập kế hoạch cutover riêng; không tự động
    tắt đăng ký/login local trong đợt tích hợp này.

## 8. Test case bắt buộc

| ID | Nội dung | Kết quả mong đợi |
|---|---|---|
| WEBSSO-001 | Bấm Đăng nhập trên EXE | Mở đúng `/vi/login`, game chờ xác thực |
| WEBSSO-002 | Bấm Đăng ký trên APK | Cũng mở `/vi/login`, link Tạo trang trại mới dùng được |
| WEBSSO-003 | Login web account có sẵn | Game nhận đúng player và bootstrap |
| WEBSSO-004 | Tạo account mới từ link trong login | Sau đăng ký quay về callback, game vào đúng player |
| WEBSSO-005 | Hủy browser/không đăng nhập | Game không tạo player, có thể thử lại |
| WEBSSO-006 | Request hết hạn | Exchange bị từ chối, không tạo session |
| WEBSSO-007 | Dùng lại request/code | Bị từ chối vì đã consumed |
| WEBSSO-008 | Giả mạo verifier/state | Bị từ chối |
| WEBSSO-009 | Account web khóa/xóa/inactive | Game từ chối, không tạo player mới |
| WEBSSO-010 | Đăng nhập lại/cross-device | Giữ playerId, Point, túi và farm |
| WEBSSO-011 | Hai thiết bị cùng account | Phiên mới thay phiên cũ bằng `4008` |
| WEBSSO-012 | Restart backend/PostgreSQL | Dữ liệu player còn nguyên; auth request cũ hết hạn an toàn |

## 9. Điều kiện còn thiếu trước khi bật cho bản khách

- Build/deploy web callback phải pass và ghi lại đường dẫn backup rollback.
- Migration/browser API phải deploy versioned lên game-server rồi restart
  PostgreSQL/backend để chứng minh request còn hoạt động đúng.
- Test account web thật trên EXE và APK: login, register + OTP, callback, bootstrap,
  relogin, cùng playerId và dữ liệu tách biệt account local.
- Sau khi pass mới bật `browserAuthEnabled` trong build; QARich/local account vẫn
  giữ nguyên cho tester trong toàn bộ giai đoạn song song.
