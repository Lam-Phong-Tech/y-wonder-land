# Web SSO VPS audit - 12/07/2026

Phạm vi: chỉ đọc tiến trình, source, tên biến môi trường, service và Nginx trên
VPS game/web `42.96.18.14`. Audit không đọc giá trị secret và không thay đổi VPS.

## Kết quả

- Web Next.js: `greenxland.service`, user `greenxland`, cổng `3033`, source
  `/var/www/ywonder`.
- Game backend: cổng `3000`, health pass, PostgreSQL pass.
- Dịch vụ phụ `thodung-api.service`: cổng `3036`, source
  `/var/www/ywonder/game-api-server`.
- Web có `GAME_API_SECRET` dài 64 ký tự và `JWT_SECRET` dài 64 ký tự. Không ghi
  giá trị vào tài liệu/repo/chat.
- Source web không có Git branch/HEAD khả dụng. Mọi thay đổi production phải dùng
  backup timestamp và rollback artifact/service/Nginx riêng.

## Contract đã xác nhận

`POST /api/game/auth`:

- Bearer `GAME_API_SECRET`, chỉ dùng server-to-server.
- Nhận username, email hoặc refCode cùng password.
- So password bằng bcrypt, giới hạn 10 lần/phút theo định danh.
- Chỉ cho account có status `ACTIVE`.
- Trả stable web user ID, username/refCode/fullName và HS256 game token 12 giờ.

NextAuth gắn stable web ID vào `session.user.id`. Đây là khóa canonical để game
map `web_user_id -> playerId`.

## Khoảng trống browser SSO

- Login đọc `callbackUrl` và chuyển tới callback sau đăng nhập thành công.
- Link từ login sang register không giữ `callbackUrl`.
- Register không đọc callback; sau đăng ký/OTP luôn chuyển tới dashboard.
- Nhãn mã giới thiệu ghi tùy chọn nhưng input runtime vẫn có `required`.
- Nginx chỉ proxy `/game-api/*` trên `api.ywonder.net`; host `ywonder.net` chưa có
  callback route cùng domain tới game-server.

## Quyết định an toàn

1. Giữ đăng nhập/đăng ký local và account QARich.
2. Bật cầu web credential ở game-server bằng transition mode rõ ràng và test trước;
   không sửa/restart web trong bước này.
3. Chỉ sau khi Nấc A pass mới backup web/Nginx và phát triển browser SSO bằng
   callback một lần + PKCE.
4. Không chuyển hoàn toàn sang web cho tới khi cross-device, restart, rollback và
   duplicate-session `4008` đều đạt.

## Kết quả chuyển tiếp Nấc A

- Release game-server `5db92436a7974b38866fa3291f5f3e3577a2f30f` đã được deploy
  versioned sau khi backup PostgreSQL, env và systemd unit; previous release vẫn
  được giữ để rollback.
- Production đang chạy `WEB_AUTH_MODE=http`, `AUTH_TRANSITION_MODE=parallel` và
  giữ đăng ký local. Secret chỉ được chuyển nội bộ giữa env trên VPS, không in ra
  terminal/chat và không lưu trong repository.
- Public acceptance bằng một account web thật và một account game local đã pass
  login, `/player/bootstrap`, relogin, stable player mapping và data isolation.
- Nấc A không sửa web source hoặc Nginx. Browser callback/exchange + PKCE vẫn là
  Nấc B và chưa được phép dùng thay hoàn toàn luồng local.
