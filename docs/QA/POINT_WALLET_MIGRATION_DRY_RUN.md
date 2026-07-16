# Point Wallet Migration Dry-Run

Trạng thái: candidate và lượt production read-only ngày 16/07/2026 đã pass;
**report còn blocker nên chưa được migrate**.

## Mục tiêu

Đối chiếu Point web SQLite và Point game PostgreSQL theo từng identity trước khi
link một ví chung. Công cụ này chỉ tạo báo cáo; không sinh SQL migration, không
đổi mapping và không thay đổi số dư.

## Bảo đảm an toàn

- Web exporter mở SQLite bằng `mode=ro`, bật `query_only` và kiểm tra foreign key.
- Game exporter dùng `REPEATABLE READ READ ONLY`; lỗi giữa chừng luôn `ROLLBACK`.
- Runner chụp cả hai ledger hai lượt. Chỉ khi từng snapshot web/game giống hệt
  giữa hai lượt mới tạo báo cáo; có giao dịch chen giữa thì dừng.
- Snapshot thô chứa `User.id`, `playerId` và transaction ID chỉ nằm trong thư mục
  tạm quyền `0700`, `umask 077`, rồi bị xóa khi thành công, lỗi hoặc interrupt.
- Báo cáo thay identity bằng HMAC ref. Khóa HMAC tối thiểu 32 ký tự, không lưu
  trong repo, log hoặc báo cáo.
- Báo cáo luôn có `automaticMigrationAllowed=false`,
  `migrationStatementsGenerated=0` và `databaseMutationsPerformed=false`.
- `Wallet.balanceGXL`, `lockedGXL` và lịch sử `Transaction.amount` là SQLite REAL
  legacy. Giá trị dưới 1 micro-Point được lượng tử hóa `ROUND_HALF_EVEN` chỉ trong
  snapshot tạm; report giữ residual có dấu bằng atto-Point và gắn
  `LEGACY_SUB_MICRO_VALUE_PRESENT`. Account đó luôn `BLOCKED`. Outbox settlement
  vẫn bắt buộc chính xác tối đa 6 chữ số thập phân và fail ngay nếu lệch.

## Kiểm tra candidate

Linux/VPS có `python3` trong `PATH`:

```bash
cd /path/to/server
npm run test:web-point-migration-dry-run
bash -n deploy/run-point-wallet-migration-dry-run.sh
```

Windows không có Python trong `PATH` phải trỏ `PYTHON_BIN` tới Python 3 trước khi
chạy npm test. Không được bỏ qua test SQLite exporter.

## Chuẩn bị lượt production

1. Xin duyệt một lượt **read-only**; không gộp với deploy, restart hoặc migration.
2. Xác nhận đúng file SQLite active và đúng `DATABASE_URL` PostgreSQL game. Không
   in URL, password hoặc env ra terminal/log.
3. Nạp `DATABASE_URL` bằng cơ chế root-only đã duyệt của VPS.
4. Tạo hoặc nạp một `POINT_MIGRATION_REPORT_KEY` root-only ổn định để các report
   có thể đối chiếu cùng account ref. Không nhập khóa trực tiếp vào shell history.
5. Chọn đường dẫn report mới, chưa tồn tại, trong thư mục chỉ operator đọc được.
6. Nếu `DATABASE_URL` dùng Unix socket peer auth và runner chạy bằng root, đặt
   `POINT_MIGRATION_GAME_EXPORT_USER` bằng đúng user systemd của game. Runner chỉ
   hạ hai lệnh PostgreSQL exporter sang user này, xóa HMAC key và `PGPASSWORD`
   khỏi child; snapshot/output vẫn nằm trong thư mục root `0700`.

Ví dụ khung chạy, không chứa credential:

```bash
read -r -s -p "Point migration report key: " POINT_MIGRATION_REPORT_KEY
export POINT_MIGRATION_REPORT_KEY
export POINT_MIGRATION_GAME_EXPORT_USER=ywonder_game
./deploy/run-point-wallet-migration-dry-run.sh \
  /approved/path/to/web.sqlite \
  /approved/private/path/point-migration-report.json
unset POINT_MIGRATION_REPORT_KEY POINT_MIGRATION_GAME_EXPORT_USER DATABASE_URL
```

Kết quả thành công phải in đúng các cờ:

```text
POINT_WALLET_MIGRATION_DRY_RUN=success
DATABASE_MUTATIONS_PERFORMED=no
RAW_SNAPSHOTS_RETAINED=no
```

## Bằng chứng production 16/07/2026

- Candidate commit: `7dffc8b5`; report lúc `2026-07-16T13:48:26Z`.
- Report root-only:
  `/root/ywonder-point-reports/point-wallet-migration-dry-run-20260716T134826Z-7dffc8b5.json`.
- SHA-256:
  `3ef6343bbef65bcfd35bce78aab10408df24c71c3c5fa90acd800788f3f14f16`.
- 159 account: `NO_ACTION=143`, `UNMAPPED_LEGACY_REVIEW=7`,
  `MANUAL_RECONCILIATION_REQUIRED=6`, `BLOCKED=3`.
- Tổng web Point sau lượng tử hóa là `3422666667` micros, locked Point bằng `0`;
  tổng Point của 6 game identity đã map là `25511000000` micros.
- Ba outbox canary đều `SENT`, mỗi row `1000000` micros và khớp đúng một game
  credit; tổng gửi = tổng nhận = `3000000` micros, không duplicate. Các source
  synthetic không có web `Transaction` thật nên vẫn cần review, không phải bằng
  chứng tiền thật.
- Ba account `BLOCKED` chứa tổng 6 giá trị sub-micro từ SWAP/ví legacy. Sáu game
  identity đều có opening balance `5000000000` micros; source backend xác nhận
  `5000 Point` là default tạo economy, nhưng vẫn cần quyết định phân loại bằng văn
  bản trước migration.
- Hậu kiểm: PID web/game `186434/186418`, health `200/200`, build/env không đổi,
  public Point credit `404`, wallet debit tắt, upload/raw temp bằng `0`. Không có
  restart, deploy, DB/mapping/balance mutation hoặc thanh toán thật.

## Đọc trạng thái

| Trạng thái | Ý nghĩa | Hành động |
|---|---|---|
| `BLOCKED` | Dữ liệu/mapping/ledger vi phạm invariant | Không link; sửa nguyên nhân và chạy lại cùng quy trình |
| `MANUAL_RECONCILIATION_REQUIRED` | Account đã map nhưng có seed, legacy hoặc lịch sử chưa phân loại | Đối chiếu bằng chứng và xin duyệt từng khoản |
| `UNMAPPED_LEGACY_REVIEW` | Web có Point cũ nhưng chưa có game mapping | Giữ nguyên web; chưa migrate |
| `READY_TO_LINK` | Hai phía không có chênh lệch chưa giải thích | Vẫn cần duyệt identity và kế hoạch rollback |
| `ALREADY_LINKED` | Link đã tồn tại và không có vấn đề trong snapshot | Kiểm tra lại balance UI/client trước rollout |
| `NO_ACTION` | Không có mapping hoặc giá trị cần xử lý | Không thay đổi |

## Gate hoàn thành

Dry-run production chỉ được coi là đạt khi:

- `BLOCKED=0`;
- mọi reason ở nhóm manual/legacy có quyết định bằng văn bản;
- tổng Point web/game và từng outbox/ledger được đối chiếu;
- report không chứa raw web user ID, player ID hoặc transaction ID;
- hậu kiểm xác nhận DB, service PID/build/env và số dư không đổi.

Ngay cả khi gate đạt, report không phải lệnh migrate. Migration ghi dữ liệu phải
là task/release riêng, có backup, transaction ID bất biến, rollback và phê duyệt.
