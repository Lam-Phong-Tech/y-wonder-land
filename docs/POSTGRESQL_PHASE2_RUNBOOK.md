# PostgreSQL Phase 2 Runbook

Ngày cập nhật: 11/07/2026

## Trạng thái hiện tại

- `server/postgresStore.js` đã có query thật cho local account, web player mapping, profile, economy, inventory, farm state, daily limits và transaction ledger.
- Shop, economy/inventory adjustment, resource harvest và daily-limit consume dùng PostgreSQL transaction thật cùng idempotency lock.
- Migration có version nằm tại `server/migrations/`; `server/schema.sql` là schema snapshot để đọc nhanh.
- Có script import `data.json`, kiểm trùng username/email trước khi ghi và rollback toàn bộ nếu một bản ghi lỗi.
- Route REST, dashboard dev và realtime resource đã chuyển sang async nhưng giữ nguyên payload API cho Unity.
- JSON store vẫn là mặc định cho local/dev và đã regression pass sau thay đổi.

## PostgreSQL test trên VPS

- VPS game đang chạy PostgreSQL `14.23` từ Ubuntu 22.04 repository.
- Service `postgresql` đang `active` và `enabled`.
- PostgreSQL chỉ listen `127.0.0.1:5432`; UFW không public cổng `5432`.
- Database `ywonder_test` và role `deploy` chỉ dùng integration test qua local socket/SSH tunnel, không phải production credential.
- Không có password/private key nào nằm trong repo.

## Lệnh migration

```powershell
cd server
$env:DATABASE_URL="postgresql://<db_user>:<db_password>@127.0.0.1:5432/<db_name>"
npm.cmd run db:migrate
```

Migration runner tạo bảng `schema_migrations`, áp file `.sql` theo thứ tự và chạy từng migration trong transaction.

## Import JSON

Kiểm tra nguồn mà chưa ghi DB:

```powershell
$env:IMPORT_DRY_RUN="true"
$env:YW_IMPORT_JSON_PATH="D:\path\to\data.json"
npm.cmd run db:import-json
```

Import thật sau khi migration và backup:

```powershell
$env:IMPORT_DRY_RUN="false"
npm.cmd run db:import-json
npm.cmd run db:verify
```

Lượt test ngày 11/07 import vào schema tạm đã xác minh đúng `36 accounts`, `51 players`, `82 transactions`, sau đó schema tạm được xóa.

## Test bắt buộc

```powershell
$env:POSTGRES_TEST_DATABASE_URL="postgresql://<test_user>:<test_password>@127.0.0.1:5432/<test_db>"
npm.cmd run test:postgres
```

`test:postgres` tự tạo schema ngẫu nhiên, kiểm concurrent idempotency, shop, resource + mining limit, fishing limit, profile/inventory/farm persistence và pool restart, rồi xóa schema test.

Sau đó chạy Node với `STORE_MODE=postgres` và dùng lại:

```powershell
$env:PHASE1_TEST_BASE_URL="http://127.0.0.1:3000"
npm.cmd run test:phase1
```

Ngày 11/07 các vòng sau đã pass trên PostgreSQL thật qua SSH tunnel:

- Migration `001_initial`.
- Direct PostgreSQL transaction smoke.
- Register/login/bootstrap/shop/economy/inventory/farm-state/realtime/session replacement.
- `/health` xác minh được kết nối store; hai đăng ký trùng chạy đồng thời trả đúng một `200` và một `409 USERNAME_EXISTS`.
- Dừng và mở lại Node vẫn giữ đúng Point và farm marker.
- Dashboard dev đọc được player/transaction từ PostgreSQL.
- JSON store Phase 1 regression vẫn pass.
- `npm audit --omit=dev`: `0 vulnerabilities`.

## Chưa phải production

- Chưa tạo database/role production riêng cho game-server.
- Chưa thử restart PostgreSQL service hoặc reboot VPS rồi restore dữ liệu.
- Chưa có backup hằng ngày và restore drill.
- Chưa cài Node.js/Caddy trên VPS, chưa có `systemd` game service.
- Chưa deploy backend, chưa đổi DNS `api.ywonder.net`, chưa bật HTTPS/WSS.
- Chưa khóa root/password SSH; vẫn giữ làm rollback cho tới khi provisioning hoàn tất.

Production phải dùng role riêng quyền tối thiểu và secret trong file env trên VPS. Không dùng role `deploy` hoặc database `ywonder_test` làm dữ liệu thật.
