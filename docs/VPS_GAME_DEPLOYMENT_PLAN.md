# Ke hoach trien khai VPS game

Ngay lap: 10/07/2026

## 1. Thong tin da duoc xac nhan

- VPS game rieng: `42.96.18.14`.
- Tai khoan ban giao ban dau: `root`; mat khau phai giu ngoai Git, tai lieu va source code.
- Anh da yeu cau Ubuntu Server 24.04 LTS; audit thuc te xac nhan may dang chay Ubuntu 22.04.5 LTS (Jammy). Khuyen nghi giu 22.04 cho deadline demo, chi rebuild neu cong ty bat buoc 24.04.
- Theo xac nhan cua anh, VPS chua co dich vu can giu lai va duoc phep cai game-server cung PostgreSQL.
- Sau khi nghiem thu, `api.ywonder.net` se tro ve `42.96.18.14`.
- Website `ywonder.net` va Web Account API van la he thong rieng; game-server se goi Web API theo ket noi server-to-server khi bat lai web auth.
- May phat trien cua anh dung Windows. Backend demo hien tai van chay bang Node + JSON + Cloudflare Tunnel cho den khi VPS moi dat du tieu chi cutover.

Trang thai truy cap hien tai:

- TCP `22` da toi duoc tu may lam viec; SSH handshake thanh cong.
- Banner xac nhan `OpenSSH_8.9p1 Ubuntu-3ubuntu0.15`, fingerprint ED25519 `SHA256:dpAuiUAA7K0h3iDDGxys6XOpHv1uVRGUSW4y23qYgZk`, ho tro `publickey,password`.
- Audit read-only da dat: KVM, 2 vCPU, RAM 3.8 GiB + swap 3.8 GiB, disk 50 GB con khoang 37 GB, timezone Asia/Ho_Chi_Minh va NTP active.
- UFW active, deny incoming mac dinh; chi `22/tcp` dang listen/duoc allow. `80/443/3000/5432` chua public.
- Chua co Node/npm, PostgreSQL, Caddy/Nginx hoac Docker; khong co failed service hay ung dung cu can bao ton.
- `api.ywonder.net` hien van phan giai ve `45.119.83.233`, chua tro sang VPS game moi.

Moc A audit read-only da dat. Bao cao: `docs/VPS_GAME_AUDIT_2026-07-10.md`.

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

Trang thai: **DAT ngay 10/07/2026**.

Can lam:

- Owner xac nhan VPS dang bat va mo SSH `22` cho may lam viec, hoac cung cap co che whitelist/VPN neu co.
- SSH vao bang `root` mot lan de kiem tra, chua cai dat hay xoa gi.
- Ghi lai: OS/version, CPU, RAM, disk, timezone, public IP, hostname, cac cong dang nghe, firewall va cac service dang chay.
- OS thuc te la Ubuntu 22.04.5 LTS; da bao cao va khuyen nghi giu de phuc vu deadline demo.

Ket qua dat:

- SSH on dinh.
- Co ban audit tai nguyen va khong co dich vu/dulieu can bao ton ngoai du kien.
- Anh duoc bao cao ro VPS du hay thieu tai nguyen cho demo 20 nguoi.

### Moc B - Hoan tat phan game con thieu cua Giai doan 1

Lam song song trong repo, khong phu thuoc VPS:

- `[x]` Tach PlayerPrefs/cache theo `playerId`; A -> B -> A va full EXE restart da pass.
- Noi `farm_state` hai chieu cho build/tile/crop/animal.
- Doc va ghi `daily_limits` theo gio server, uu tien cau ca va dao mo.
- `[x]` Chay test tu dong 20 ket noi qua private Caddy/PostgreSQL; roster/state/chat/ping, health, cleanup va OOM gate deu pass.
- `[ ]` Test that 5-20 EXE/APK ngoai mang sau khi HTTPS/WSS public.

Ket qua dat:

- Doi tai khoan tren cung may khong ro ri tien, tui do hoac farm.
- Restart/relogin van khoi phuc dung du lieu theo account.
- 20 ket noi realtime khong pha vo room, chat, presence va session replacement.

### Moc C - Hoan thien PostgreSQL trong source code

Trang thai source: **DAT ngay 11/07/2026**.

- `[x]` Them driver `pg`, migration versioned va bang `game_accounts` lien ket account -> player.
- `[x]` Implement profile, economy, inventory, farm state, daily limits va transaction ledger.
- `[x]` Shop/resource/economy/inventory/daily limit dung DB transaction that va idempotency lock.
- `[x]` Them import `data.json`, dry-run, verify count va rollback toan bo khi loi.
- `[x]` JSON Phase 1 regression va PostgreSQL direct/REST/WebSocket smoke deu pass.
- `[x]` Node restart van giu Point, inventory va farm marker; dashboard dev doc duoc PostgreSQL.
- `[x]` Import schema tam pass `36 accounts / 51 players / 82 transactions`, sau do schema tam da xoa.

Runbook: `docs/POSTGRESQL_PHASE2_RUNBOOK.md`.

Ket qua dat:

- Register/login/bootstrap/shop/resource/farm/daily-limit pass tren PostgreSQL: **DAT**.
- Restart Node khong mat account/tien/item/farm: **DAT**. Controlled restart PostgreSQL service + backend tren production va so sanh fingerprint 3 account P1: **DAT**. Full VPS reboot + auto-start + P1 persistence: **DAT**. Backup restore drill: **DAT**.
- Retry request khong cong/tru hai lan: **DAT**, ke ca hai request economy chay dong thoi.

### Moc D - Chuan bi Ubuntu va cai dich vu

Chi lam sau khi Moc A pass:

- Cap nhat goi he thong va bat dong bo thoi gian.
- `[x]` Ngay 11/07 da tao user khong dac quyen `deploy`, cai ED25519 public key va kiem tra batch login thanh cong. `.ssh` la `700`, `authorized_keys` la `600`; root/password/sshd/UFW van giu nguyen de rollback.
- `[~]` Firewall van chi cho phep SSH `22`; PostgreSQL `5432`, Node `3000` va Caddy staging `8080` khong public. HTTP `80`/HTTPS `443` chi mo o Moc F sau hardening va xac nhan DNS.
- `[x]` PostgreSQL 14.23, Node 24.18.0 LTS va Caddy 2.11.4 da cai. PostgreSQL active/enabled va chi listen `127.0.0.1:5432`.
- `[x]` `ywonder_test` + role `deploy` van chi de integration test. Da tao OS service account, PostgreSQL role quyen toi thieu va database production rieng ten `ywonder_game`; app se ket noi bang peer auth qua Unix socket, khong luu DB password.
- `[x]` Da tao env production ngoai repo tai `/etc/ywonder-game/game-server.env`; file chi cho root va service group doc, JWT sinh truc tiep tren VPS. Admin/demo/web-auth dang tat theo default an toan.
- `[x]` Da tao `ywonder-db-backup.service/.timer`; timer enabled/active, backup dau tien va restore drill database tam deu pass, database tam da duoc xoa.
- `[x]` Da deploy commit `ebc9982` va tao `ywonder-game-server.service`; game-server va Caddy deu enabled/active.

Ket qua dat:

- `[x]` Full VPS reboot luc `13:13:19 +07` da pass; Node/PostgreSQL/Caddy/backup timer tu khoi dong `active/enabled`, private health tro lai PostgreSQL mode.
- `[x]` Database chi nghe local.
- `[x]` Backend chi nghe `127.0.0.1:3000`; Caddy staging co `bind 127.0.0.1` va chi nghe `127.0.0.1:8080`.
- `[x]` Backup tao duoc va thu restore thanh cong tren database test.
- `[x]` Controlled restart PostgreSQL + backend ngay 11/07 giu nguyen fingerprint profile/economy/inventory/farm/daily-limit/transaction cua 3 account P1; health truc tiep va qua Caddy deu tro lai `storage.mode=postgres`.
- `[x]` Sau full VPS reboot, ca ba account P1 login/bootstrap lai thanh cong; canonical fingerprint truoc/sau khop `a003b888ed68b5ee95e43efae2ee0873fafd291dac66aac0ffceeaf7c649bf6e`.

### Moc E - Deploy staging tren VPS

Trang thai:

- `[x]` Baseline `ebc9982` da duoc thay boi hardened release `09433bff1e739bd2573c8068ffa58f445cd01bb6`; migration `001_initial` skip dung vi da ap dung. Khong import JSON cu.
- Cau hinh toi thieu:
  - `STORE_MODE=postgres`
  - `DATABASE_URL=postgresql:///ywonder_game?host=/var/run/postgresql`
  - `JWT_SECRET=<secret dai tren VPS>`
  - `WEB_AUTH_MODE=disabled` cho tai khoan game local trong dot demo dau
  - `WEB_AUTH_LOGIN_URL=https://ywonder.net/api/game/auth` khi bat web auth
  - `REALTIME_MAX_ROOM_PLAYERS=20`
  - `ADMIN_DASHBOARD_ENABLED=false`
  - `HOST=127.0.0.1`
  - `PORT=3000`
- `[x]` `/health`, register, login, bootstrap, shop, idempotency va WebSocket da pass qua Caddy staging noi bo.
- `[x]` Hardening auth/rate-limit/log/HTTP-WebSocket guard, production config gate, graceful shutdown va systemd sandbox da pass local + private VPS. Full Phase 1 qua SSH tunnel/Caddy/PostgreSQL pass; `/admin=404`, rate-limit header hoat dong, backup timer active.
- `[x]` Automated 20-client private acceptance pass: 20 account bootstrap, 20 WebSocket vao `city`, day du 19 peer, state/chat/ping va hold connection. Backup pre-cutover co SHA-256 `04dda7ac1048d0de493a25f91ab98116f784494460c1cbfa390479d646679a7e`; account test da don ve 0, khong OOM, cac service van active.
- `[x]` Script `server/deploy/deploy-private-release.sh` kiem checksum, npm lockfile, config/migration, systemd, health va rollback. Luu y bat buoc ep `USER/LOGNAME/PGUSER=ywonder_game` khi migration dung peer auth; neu giu `USER=root`, deploy se dung truoc switch.

Luu y domain:

- Khi `api.ywonder.net` tro sang game VPS, game-server khong duoc goi nguoc `api.ywonder.net/api/game/auth` neu endpoint do van thuoc web cu. Dung `https://ywonder.net/api/game/auth` de tranh goi nham chinh no.
- Can xac nhan voi ben web xem co he thong nao khac dang phu thuoc `api.ywonder.net/api/game/*`; neu co, Caddy tren game VPS phai proxy rieng path do ve web host cu.

### Moc F - DNS, HTTPS va nghiem thu ngoai mang

Can lam:

- `[x]` DNS `api.ywonder.net` da ve `42.96.18.14`; khong doi `ywonder.net`.
- `[x]` Nginx da giu public `80/443`, HTTP redirect HTTPS va certificate hop le; `3000/5432/8080` van dong public.
- `[x]` Audit read-only xac nhan `/api/game/* -> 3033` va root `-> 3036`; game backend PostgreSQL van private tren `3000/8080`.
- `[ ]` Backup Nginx va them namespace rieng `/game-api/*` + WebSocket `/game-api/realtime -> 3000`; khong thay the route web cu va khong public Caddy.
- Test tu mang ngoai:
  - `https://api.ywonder.net/game-api/health`
  - dang ky/dang nhap
  - relogin giu du lieu
  - shop mua/ban
  - 20 WebSocket
  - chat/presence/action/resource
  - duplicate session ma `4008`
- Sau khi them Nginx route, chay `nginx -t`, reload va lap lai test toi thieu tu mang ngoai; full VPS auto-start baseline da pass o private checkpoint.

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

1. Xac nhan owner co quyen doi DNS `api.ywonder.net` va khong co he thong web cu dang phu thuoc cac path tren subdomain nay.
2. Backup Nginx, them `/game-api/*` va WebSocket `/game-api/realtime` toi `127.0.0.1:3000`; giu nguyen `/api/game/* -> 3033` va root `-> 3036`.
3. Test REST + WSS tu ngoai mang va lap lai health/register/login/bootstrap/shop/realtime sau khi reload Nginx; full VPS reboot baseline da pass.
4. Chi sau khi Moc F pass moi doi Unity `BackendConfig.baseUrl = https://api.ywonder.net/game-api`, build EXE/APK va test 5-20 client.

## 6. Thong tin tuyet doi khong ghi vao repo

- Root password / deploy-user password.
- SSH private key.
- `JWT_SECRET`.
- `DATABASE_URL` that.
- `GAME_API_SECRET` / `WEB_AUTH_SECRET`.
- File `.env` production.
