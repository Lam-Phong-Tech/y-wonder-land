# Point Wallet Migration Dry-Run

Trạng thái: candidate đã pass local ngày 16/07/2026; **chưa chạy trên production**.

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

Ví dụ khung chạy, không chứa credential:

```bash
read -r -s -p "Point migration report key: " POINT_MIGRATION_REPORT_KEY
export POINT_MIGRATION_REPORT_KEY
./deploy/run-point-wallet-migration-dry-run.sh \
  /approved/path/to/web.sqlite \
  /approved/private/path/point-migration-report.json
unset POINT_MIGRATION_REPORT_KEY DATABASE_URL
```

Kết quả thành công phải in đúng các cờ:

```text
POINT_WALLET_MIGRATION_DRY_RUN=success
DATABASE_MUTATIONS_PERFORMED=no
RAW_SNAPSHOTS_RETAINED=no
```

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
