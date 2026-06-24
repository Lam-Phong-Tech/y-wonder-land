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

## API
| Method | Endpoint | Body | Trả về |
|---|---|---|---|
| GET  | `/` | — | `{ ok: true }` (health check) |
| POST | `/auth/register` | `{ "username", "password" }` | `{ token, userId }` |
| POST | `/auth/login` | `{ "username", "password" }` | `{ token, userId }` |
| GET  | `/player/profile` | — (header `Authorization: Bearer <token>`) | `{ player_profile { ... } }` |
| PUT  | `/player/profile` | `{ "player_profile": { ... } }` (Bearer token) | `{ ok: true, updatedAt }` |

`player_profile` theo `docs/DATA_SCHEMA.md` + field `tutorialCompleted` (bool).

## Smoke test nhanh (PowerShell)
```powershell
# Đăng ký
$r = irm http://localhost:3000/auth/register -Method Post -ContentType application/json -Body '{"username":"test","password":"12345678"}'
$tok = $r.token
# Lấy profile
irm http://localhost:3000/player/profile -Headers @{ Authorization = "Bearer $tok" }
# Cập nhật cờ tutorial
irm http://localhost:3000/player/profile -Method Put -ContentType application/json -Headers @{ Authorization = "Bearer $tok" } -Body '{"player_profile":{"tutorialCompleted":true}}'
```
