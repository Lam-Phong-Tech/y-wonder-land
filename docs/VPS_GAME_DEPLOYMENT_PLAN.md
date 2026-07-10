# Ke hoach trien khai VPS game

Ngay lap: 10/07/2026

## 1. Thong tin da duoc xac nhan

- VPS game rieng: `42.96.18.14`.
- Tai khoan ban giao ban dau: `root`; mat khau phai giu ngoai Git, tai lieu va source code.
- Anh da yeu cau Ubuntu Server 24.04 LTS. He dieu hanh thuc te chi duoc chot sau khi SSH va doc `/etc/os-release`.
- Theo xac nhan cua anh, VPS chua co dich vu can giu lai va duoc phep cai game-server cung PostgreSQL.
- Sau khi nghiem thu, `api.ywonder.net` se tro ve `42.96.18.14`.
- Website `ywonder.net` va Web Account API van la he thong rieng; game-server se goi Web API theo ket noi server-to-server khi bat lai web auth.
- May phat trien cua anh dung Windows. Backend demo hien tai van chay bang Node + JSON + Cloudflare Tunnel cho den khi VPS moi dat du tieu chi cutover.

Trang thai truy cap hien tai:

- Lan kiem tra dau, cong SSH `22` tung phan hoi.
- Lan kiem tra gan nhat tu may lam viec khong toi duoc `22/80/443/3000`.
- `api.ywonder.net` hien van phan giai ve `45.119.83.233`, chua tro sang VPS game moi.

Day khong phai loi code game. Owner/infra can dam bao VPS dang bat va cho phep SSH truoc khi trien khai.

## 2. So do dich vu muc tieu

```text
Unity EXE/APK
    |
    | HTTPS + WebSocket
    v
api.ywonder.net (42.96.18.14)
    |
    v
Caddy :443
    |
    v
Node game-server 127.0.0.1:3000
    |
    v
PostgreSQL 127.0.0.1:5432

Node game-server
    |
    | server-to-server, khi bat web auth
    v
https://ywonder.net/api/game/auth
```

Khong mo truc tiep cong `3000` hoac `5432` ra Internet. Unity chi ket noi qua HTTPS/WSS cong `443`.

## 3. Nguyen tac trien khai

1. Khong tat Cloudflare Tunnel/backend demo dang hoat dong truoc khi VPS moi pass test.
2. Khong doi `BackendConfig.asset` sang IP tho. Chi doi sang `https://api.ywonder.net` sau khi health, HTTPS va WebSocket deu pass tu ngoai mang.
3. Khong dung `root` de chay game lau dai. Tao user deploy rieng va test SSH key thanh cong truoc khi han che root/password.
4. Khong luu JWT secret, DB password, root password hoac `GAME_API_SECRET` vao Git.
5. Moi moc phai co backup va cach quay lui truoc khi sang moc tiep theo.

## 4. Lo trinh trien khai

### Moc A - Khoi phuc truy cap va audit chi doc

Can lam:

- Owner xac nhan VPS dang bat va mo SSH `22` cho may lam viec, hoac cung cap co che whitelist/VPN neu co.
- SSH vao bang `root` mot lan de kiem tra, chua cai dat hay xoa gi.
- Ghi lai: OS/version, CPU, RAM, disk, timezone, public IP, hostname, cac cong dang nghe, firewall va cac service dang chay.
- Xac nhan dung Ubuntu 24.04 LTS; neu khac, bao lai anh truoc khi quyet dinh cai tiep hay rebuild VPS.

Ket qua dat:

- SSH on dinh.
- Co ban audit tai nguyen va khong co dich vu/dulieu can bao ton ngoai du kien.
- Anh duoc bao cao ro VPS du hay thieu tai nguyen cho demo 20 nguoi.

### Moc B - Hoan tat phan game con thieu cua Giai doan 1

Lam song song trong repo, khong phu thuoc VPS:

- Tach PlayerPrefs/cache theo `playerId` de tai khoan A khong doc du lieu cua tai khoan B tren cung may.
- Noi `farm_state` hai chieu cho build/tile/crop/animal.
- Doc va ghi `daily_limits` theo gio server, uu tien cau ca va dao mo.
- Chay test tu dong 20 ket noi va test that 5-20 EXE/APK.

Ket qua dat:

- Doi tai khoan tren cung may khong ro ri tien, tui do hoac farm.
- Restart/relogin van khoi phuc dung du lieu theo account.
- 20 ket noi realtime khong pha vo room, chat, presence va session replacement.

### Moc C - Hoan thien PostgreSQL trong source code

Khoang trong hien tai:

- `server/postgresStore.js` moi la scaffold va moi method deu `notImplemented()`.
- `server/schema.sql` chua co bang account local de luu `username/email/phone/password_hash` cua `/auth/register`.

Can lam:

- Them PostgreSQL driver va migration co version.
- Bo sung bang account local, lien ket account -> `game_players`.
- Implement day du store cho profile, economy, inventory, farm state, daily limits va transactions.
- Dung DB transaction that cho shop va resource claim; giu idempotency key/unique constraint.
- Viet script import `data.json` sang PostgreSQL va bao cao record loi/trung.
- Chay cung mot bo smoke test voi `STORE_MODE=json` va `STORE_MODE=postgres`.

Ket qua dat:

- Register/login/bootstrap/shop/resource/farm/daily-limit pass tren PostgreSQL.
- Restart Node va PostgreSQL khong lam mat account, tien, item hoac farm.
- Retry request khong cong/tru hai lan.

### Moc D - Chuan bi Ubuntu va cai dich vu

Chi lam sau khi Moc A pass:

- Cap nhat goi he thong va bat dong bo thoi gian.
- Tao user deploy rieng, cai SSH key, kiem tra dang nhap lai thanh cong.
- Cau hinh firewall: cho phep SSH co kiem soat, HTTP `80`, HTTPS `443`; chan public `3000` va `5432`.
- Cai Node.js LTS phu hop, PostgreSQL, Git va Caddy.
- Tao database/user rieng voi quyen toi thieu.
- Tao file env chi doc boi service account.
- Tao `systemd` service cho Node va lich backup PostgreSQL hang ngay.

Ket qua dat:

- Node/PostgreSQL/Caddy tu khoi dong lai sau reboot.
- Database chi nghe local.
- Backend chi nghe `127.0.0.1:3000`.
- Backup tao duoc va thu restore thanh cong tren database test.

### Moc E - Deploy staging tren VPS

Can lam:

- Deploy dung commit da nghiem thu, chay migration va import data demo neu can giu tai khoan cu.
- Cau hinh toi thieu:
  - `STORE_MODE=postgres`
  - `DATABASE_URL=<secret tren VPS>`
  - `JWT_SECRET=<secret dai tren VPS>`
  - `WEB_AUTH_MODE=disabled` cho tai khoan game local trong dot demo dau
  - `WEB_AUTH_LOGIN_URL=https://ywonder.net/api/game/auth` khi bat web auth
  - `REALTIME_MAX_ROOM_PLAYERS=20`
  - `ADMIN_DASHBOARD_ENABLED=false`
  - `PORT=3000`
- Test `/health`, register, login, bootstrap, shop va WebSocket ngay tren VPS truoc khi doi DNS.

Luu y domain:

- Khi `api.ywonder.net` tro sang game VPS, game-server khong duoc goi nguoc `api.ywonder.net/api/game/auth` neu endpoint do van thuoc web cu. Dung `https://ywonder.net/api/game/auth` de tranh goi nham chinh no.
- Can xac nhan voi ben web xem co he thong nao khac dang phu thuoc `api.ywonder.net/api/game/*`; neu co, Caddy tren game VPS phai proxy rieng path do ve web host cu.

### Moc F - DNS, HTTPS va nghiem thu ngoai mang

Can lam:

- Ha DNS TTL truoc cutover neu owner cho phep.
- Doi duy nhat ban ghi `api.ywonder.net` sang `42.96.18.14`; khong doi `ywonder.net`.
- Caddy cap TLS va proxy ca REST lan WebSocket ve Node.
- Test tu mang ngoai:
  - `https://api.ywonder.net/health`
  - dang ky/dang nhap
  - relogin giu du lieu
  - shop mua/ban
  - 20 WebSocket
  - chat/presence/action/resource
  - duplicate session ma `4008`
- Reboot VPS va lap lai test toi thieu.

Ket qua dat:

- HTTPS hop le, WSS ket noi on dinh.
- Node/PostgreSQL/Caddy tu khoi dong sau reboot.
- Khong public dashboard admin, DB hay port Node.

### Moc G - Chuyen build Unity va rollback

- Chi sau Moc F moi dat Unity `baseUrl = https://api.ywonder.net` va build EXE/APK moi.
- Giu backend Windows + Cloudflare Tunnel cu trong thoi gian nghiem thu de co duong quay lai.
- Neu VPS loi: dung phat build moi, khoi phuc DNS/base URL cu va khong ghi tiep vao hai database cung luc.
- Sau khi khach nghiem thu, dong tunnel tam va luu snapshot JSON cu lam archive read-only.

## 5. Thu tu cong viec ngay tiep theo

1. Xin owner mo SSH/kiem tra whitelist cho `42.96.18.14`.
2. Audit chi doc VPS; khong can cung cap password cho den khi bat dau buoc nay.
3. Trong luc cho SSH, bat dau tach cache theo `playerId` trong Unity.
4. Sau audit, hoan thien PostgreSQL adapter va bang account local tren may phat trien truoc.
5. Chi deploy len VPS sau khi bo test PostgreSQL local pass.

## 6. Thong tin tuyet doi khong ghi vao repo

- Root password / deploy-user password.
- SSH private key.
- `JWT_SECRET`.
- `DATABASE_URL` that.
- `GAME_API_SECRET` / `WEB_AUTH_SECRET`.
- File `.env` production.
