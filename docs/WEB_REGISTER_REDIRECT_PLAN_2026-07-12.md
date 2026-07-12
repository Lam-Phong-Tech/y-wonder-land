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
được ưu tiên hơn custom deep link vì EXE portable không cần cài protocol vào
Windows.

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
   `ywonder.net/game-api/auth/browser/callback` nên gửi kèm cookie web an toàn.
7. Game-server xác minh web session qua dịch vụ web nội bộ, lấy `web_user_id`
   ổn định và đánh dấu `requestId` đã được chấp nhận.
8. Unity gọi `POST /auth/browser/exchange` với `requestId + code_verifier`.
   Server kiểm tra PKCE, chỉ cho dùng một lần, map `web_user_id -> playerId`, rồi
   trả game JWT và bootstrap.
9. Unity vào game; mật khẩu web, cookie web và secret server không đi qua client.

Nếu Android tạm dừng app khi mở browser, exchange tiếp tục khi app được focus lại.
EXE có thể polling trong lúc cửa sổ browser đang mở.

## 4. Phần cần sửa ở web

Hai thay đổi nhỏ nhưng bắt buộc cho tài khoản mới:

1. Link `Tạo trang trại mới` trên login phải giữ tham số `callbackUrl` khi sang
   register.
2. Register phải đọc và validate `callbackUrl`; sau khi đăng ký/xác thực thành
   công, điều hướng về callback thay vì luôn vào `/vi/dashboard`.

Chỉ chấp nhận callback nội bộ đã whitelist, ví dụ bắt đầu bằng
`/game-api/auth/browser/callback`. Không cho callback tùy ý để tránh open redirect.

Nếu không có source web để sửa ngay, phương án tạm là: đăng ký xong quay lại game,
bấm Đăng nhập lần nữa và đăng nhập web. Phương án này dùng được để test, nhưng
không phải UX bàn giao mong muốn.

## 5. Phần cần sửa ở game-server

- Thêm bảng/record `browser_auth_requests` trong PostgreSQL:
  `request_id_hash`, `pkce_challenge`, `status`, `web_user_id`, `expires_at`,
  `consumed_at`.
- Thêm `POST /auth/browser/start` có rate limit.
- Thêm `GET /auth/browser/callback` chỉ nhận request hợp lệ, forward cookie tới
  web session API và không log cookie/token.
- Thêm `POST /auth/browser/exchange`; request một lần, hết hạn 2-5 phút, PKCE bắt
  buộc.
- Dùng mapping duy nhất `web_user_id -> playerId`, sau đó dùng lại game JWT,
  `/player/bootstrap` và realtime rule `4008` hiện tại.
- Thêm cleanup request hết hạn và test replay/CSRF/open redirect/rate limit.
- Nginx trên host `ywonder.net` proxy chính xác callback `/game-api/auth/browser/*`
  vào `127.0.0.1:3000`; vẫn không public port 3000/5432.

## 6. Phần cần sửa ở Unity

- Đổi URL cổng web trong `BackendConfig` thành `https://ywonder.net/vi/login`.
- Giữ nguyên tab/nút Đăng nhập local và Đăng ký local.
- Thêm hai nút phụ `Đăng nhập bằng web` và `Đăng ký trên web`; cả hai gọi cùng
  một `StartWebLoginAsync()`.
- Thêm feature flag cấu hình; khi flag tắt, UI và hành vi local không thay đổi.
- Không gửi username/password web từ Unity trong luồng mới.
- Lưu `requestId` và `code_verifier` chỉ trong bộ nhớ trong phiên đăng nhập.
- Poll/exchange khi app active lại; cho phép hủy và thử lại.
- Hiện trạng thái rõ: đang mở trình duyệt, đang chờ xác thực, hết hạn, bị khóa,
  web tạm lỗi.
- Giữ account local/QARich và API local trong toàn bộ giai đoạn chuyển tiếp.
  Chỉ tắt sau khi có quyết định cutover riêng và backup/rollback đã kiểm tra.

## 7. Thứ tự triển khai

1. `[x]` Audit source/service web, stable session user ID, secret presence và
   Nginx route; không đọc giá trị secret và không thay đổi VPS.
2. `[~]` **Nấc A - cầu web credential song song:** source local đã thêm transition
   flag cho phép `WEB_AUTH_MODE=http` cùng `LOCAL_REGISTRATION_ENABLED=true`, giữ
   `/auth/login` và `/auth/register` local; security/web-auth/full Phase 1 đều
   pass. Còn commit/deploy versioned và copy secret nội bộ giữa env trên cùng VPS,
   không qua client/chat.
3. Deploy versioned game-server rồi test: account local/QARich vẫn vào được,
   account web thật đăng nhập qua `/auth/web-login`, bootstrap đúng và cùng một
   web account luôn map về cùng player. Chưa sửa/restart web service.
4. **Nấc B - browser SSO:** backup toàn bộ web source/build/service/Nginx trước;
   thêm callback preservation vào login/register web và test nội bộ.
5. Implement migration + browser auth start/callback/exchange trên game-server,
   kèm PKCE, request một lần, expiry và integration test giả lập web session.
6. Thêm exact Nginx callback route trên host `ywonder.net`, kiểm tra cấu hình rồi
   reload; không mở port 3000/5432.
7. Test browser flow bằng account web thật trên EXE và APK, chưa đổi luồng local.
8. Bật hai nút web song song bằng feature flag, compile và build; local vẫn là
   đường dự phòng.
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

## 9. Điều kiện còn thiếu trước khi code web

- Đã có quyền root và xác nhận source/artifact deploy của web trên VPS.
- Đã xác nhận NextAuth session trả stable `session.user.id`.
- Cần tạo và kiểm tra gói backup/rollback web vì deploy hiện không có Git metadata.
- Cần thêm exact Nginx callback route trên host `ywonder.net` ở Nấc B.
- Chốt cách giữ QARich/local account cho tester trong bản chuyển tiếp.
