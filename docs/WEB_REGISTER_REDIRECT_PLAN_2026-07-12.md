# Kế hoạch: nút Đăng ký trong game mở trang đăng ký YWonder

Ngày phân tích: 12/07/2026
Yêu cầu gốc: khi vào game, nút **Đăng Ký** mở `https://ywonder.net/vi/register`.
Trạng thái: **backend hardening và test tích hợp cục bộ đã triển khai; chưa deploy
web-auth lên VPS và chưa sửa nút/form Unity**.

## 1. Kết luận ngắn

Không nên chỉ thay callback của nút bằng `Application.OpenURL()` rồi bàn giao ngay. Phần mở trình duyệt là đơn giản, nhưng tài khoản vừa tạo trên web chỉ dùng được trong game khi đồng thời hoàn thành cầu xác thực web ở game-server.

Luồng đích:

1. Người chơi bấm **Đăng Ký** trong game.
2. Game mở trình duyệt mặc định tới trang đăng ký YWonder bằng HTTPS.
3. Người chơi đăng ký trên web rồi quay lại game.
4. Người chơi nhập Email/SĐT/ID đăng nhập và mật khẩu vào game.
5. Unity gọi game-server; game-server gọi API xác thực web bằng secret chỉ lưu trên VPS.
6. Game-server map duy nhất `web_user_id -> playerId`, trả game JWT và bootstrap dữ liệu người chơi.

## 2. Hiện trạng đã có

- Trang `https://ywonder.net/vi/register` đang phản hồi HTTPS `200`.
- Unity đã có `AuthService.LoginAsync()`:
  - thử `/auth/login` cho account game local;
  - nếu trả `404 USER_NOT_FOUND` thì thử `/auth/web-login`.
- Node game-server đã có `/auth/web-login`, `webAuthProvider` và mapping web user sang một player.
- Secret web auth đã được thiết kế để chỉ nằm trong env game-server; Unity không giữ secret.
- Production RC hiện ưu tiên account đăng ký trực tiếp trong game và web auth đang được vận hành theo hướng tắt cho tới khi cutover.
- Trang đăng ký bắt nhập mã giới thiệu nhưng hiện chấp nhận chuỗi bất kỳ; đây là rủi
  ro attribution/hoa hồng phía web, không phải lỗi game-server.

## 3. Các vấn đề nếu chỉ đổi nút

### 3.1. Đăng ký được nhưng có thể không đăng nhập được

Nếu game-server vẫn để `WEB_AUTH_MODE=disabled`, tài khoản mới trên web không qua được `/auth/web-login`.

### 3.2. Ô đăng nhập hiện không phù hợp web account

- `UsernameField` đang giới hạn 20 ký tự và báo lỗi nếu dài hơn 20.
- `PasswordField` cũng đang giới hạn 20 ký tự.
- Placeholder hiện là `Tên đăng nhập`.

Email thường dài hơn 20 ký tự. Web cũng có thể cho đăng nhập bằng SĐT hoặc ID, nên phải đổi nhãn và giới hạn trước khi nghiệm thu.

### 3.3. Endpoint đăng ký local vẫn còn public

Nếu UI đã yêu cầu đăng ký trên web nhưng `POST /auth/register` vẫn mở trên production, người dùng kỹ thuật vẫn có thể tạo account local ngoài luồng web. Cần có env gate để tắt đăng ký local ở production nhưng vẫn cho phép ở local/staging khi cần.

### 3.4. Account khóa/xóa mềm

Game-server hiện chủ yếu tin kết quả API web. Khi cutover phải test rõ account `locked`, `inactive` hoặc `softDeleted` bị từ chối và không tạo/khôi phục phiên game.

### 3.5. Trùng định danh local và web

Client hiện thử account local trước. Nếu một ID web trùng username local, kết quả `401` local sẽ không fallback sang web. Cần chốt quy tắc account cũ trước khi mở rộng cho khách.

### 3.6. Mã giới thiệu bắt buộc nhưng chưa được xác thực

Không nên hướng dẫn người chơi nhập mã ngẫu nhiên. BA/web cần cấp một mã chính thức
cho nguồn người chơi từ game, hoặc hỗ trợ query parameter/prefill như `?ref=YGAME`,
hoặc đổi field thành tùy chọn. Đây là quyết định nghiệp vụ web; Unity chỉ mở URL đã
được chốt.

## 4. Thiết kế triển khai đề xuất

### 4.1. Unity UI

Phương án ít rủi ro cho bản đầu:

- Giữ hình thức tab/nút **Đăng Ký** hiện tại.
- Bấm nút sẽ mở trình duyệt ngoài bằng `Application.OpenURL()` và giữ game ở màn Login.
- Hiện thông báo: `Đã mở trang đăng ký. Sau khi hoàn tất, hãy quay lại game để đăng nhập.`
- Form đăng ký nội bộ giữ ở trạng thái ẩn trong một bản để rollback; không còn đường UI gọi `AuthService.RegisterAsync()`.
- Không cài WebView/package mới.

URL không hardcode trong controller. Thêm trường `registrationUrl` vào `BackendConfig` và đặt giá trị trong `Assets/Resources/BackendConfig.asset`.

### 4.2. Ô đăng nhập

- Đổi placeholder thành `Email / SĐT / ID đăng nhập`.
- Tăng giới hạn định danh lên tối thiểu 128 ký tự, hoặc chốt theo contract web.
- Tăng giới hạn mật khẩu lên tối thiểu 128 ký tự, không áp rule đăng ký local lên form login.
- Không log mật khẩu; log định danh phải được cân nhắc giảm/che khi production.

### 4.3. Game-server

- Set trên VPS:
  - `WEB_AUTH_MODE=http`
  - `WEB_AUTH_LOGIN_URL=https://ywonder.net/api/game/auth`
  - `WEB_AUTH_SECRET=<secret lưu ngoài repo>`
- Thêm `LOCAL_REGISTRATION_ENABLED=false` cho production; `/auth/register` trả lỗi rõ khi bị tắt.
- Giữ đăng nhập account local cũ/QARich trong giai đoạn chuyển tiếp, nhưng không cho tạo local account mới từ public API.
- Từ chối account web khóa/xóa mềm/inactive bằng error code ổn định.
- Giữ mapping duy nhất `web_user_id -> playerId` và rule một account chỉ có một phiên realtime (`4008`).

## 5. Thứ tự làm an toàn

1. **Xác nhận contract web:** định danh đăng nhập chính, giới hạn mật khẩu, response account khóa/xóa mềm và secret production.
2. **Test cầu web-auth trên VPS trước:** dùng một account web mới gọi `/auth/web-login`, rồi `/player/bootstrap`; chưa đổi Unity.
3. **Gia cố backend:** tắt đăng ký local bằng env, xử lý trạng thái account web và collision/migration.
4. **Sửa Unity:** thêm URL vào config, đổi callback Đăng Ký, nới ô login và đổi placeholder.
5. **Compile + smoke Editor:** nút mở đúng URL, quay lại game không mất màn Login, account local cũ vẫn đăng nhập.
6. **Build EXE/APK:** test mở browser trên Windows và Android, quay lại app, đăng nhập account web vừa tạo.
7. **Nghiệm thu dữ liệu:** cùng web account đăng nhập lại sau restart vẫn đúng player, Point, túi và profile; phiên trùng bị thay bằng `4008`.
8. **Rollout:** deploy backend trước, sau đó phát build Unity; giữ cách rollback về build cũ và `WEB_AUTH_MODE=disabled` nếu web auth lỗi.

## 6. Test case bắt buộc

| ID | Nội dung | Kết quả mong đợi |
|---|---|---|
| WEBREG-001 | Bấm Đăng Ký trên EXE | Mở đúng `https://ywonder.net/vi/register`; game không thoát |
| WEBREG-002 | Bấm Đăng Ký trên APK | Mở trình duyệt; quay lại app vẫn ở Login |
| WEBREG-003 | Đăng ký web rồi login bằng định danh được cấp | Vào đúng một player và tạo bootstrap mặc định |
| WEBREG-004 | Email dài hơn 20 ký tự | Nhập và đăng nhập được |
| WEBREG-005 | Password dài hơn 20 ký tự | Nhập và đăng nhập được nếu web cho phép |
| WEBREG-006 | Sai mật khẩu web | Báo sai thông tin, không báo server ngừng |
| WEBREG-007 | Account web bị khóa/xóa mềm | Game từ chối đăng nhập, không tạo player mới |
| WEBREG-008 | Đăng nhập lại cùng account | Giữ đúng profile, Point, túi và playerId |
| WEBREG-009 | Hai thiết bị cùng account | Phiên mới thay phiên cũ bằng mã `4008` |
| WEBREG-010 | Account QARich/local cũ | Vẫn login trong giai đoạn chuyển tiếp |
| WEBREG-011 | Gọi `/auth/register` production trực tiếp | Bị chặn khi `LOCAL_REGISTRATION_ENABLED=false` |
| WEBREG-012 | Web auth tạm lỗi/timeout | Báo lỗi rõ, không tạo account local thay thế |

## 7. Các câu cần sếp/bên web chốt trước khi code

1. Sau đăng ký, người dùng sẽ đăng nhập game bằng Email, SĐT, refCode hay một ID riêng?
2. Giới hạn mật khẩu web là bao nhiêu ký tự và có hỗ trợ ký tự Unicode không?
3. `GAME_API_SECRET` hiện tại còn đúng cho endpoint production không?
4. Có tắt hoàn toàn việc tự đăng ký account local ở production không? Khuyến nghị: **có**.
5. Account game local cũ sẽ giữ riêng, link sang web account hay chỉ dùng làm QA?
6. Mã giới thiệu chính thức dành cho người chơi đến từ game là gì, và web có hỗ trợ
   điền sẵn bằng query parameter hay không?

## 8. File dự kiến sửa sau khi kế hoạch được duyệt

- `Assets/_Project/Scripts/Backend/BackendConfig.cs`
- `Assets/Resources/BackendConfig.asset`
- `Assets/_Project/UI/LoginScreenController.cs`
- `Assets/_Project/UI/LoginScreen.uxml`
- `server/index.js`
- `server/security.js` hoặc module config tương đương
- `server/.env.example`
- Test backend/Unity và tài liệu QA tương ứng

`LoginScreenController.cs` và `LoginScreen.uxml` thuộc module QC cần sửa có chủ đích, compile/test đầy đủ và không refactor ngoài phạm vi.
