# Point Wallet Migration Dry-Run

Trạng thái: remediation và lượt production read-only hậu kiểm ngày 17/07/2026 đã
pass; report mới có `BLOCKED=0`, nhưng **account link/balance migration vẫn chưa
được phép**. Worksheet còn 17 quyết định defer/archive cần policy successor được
ghim checksum trước khi đi tiếp.

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
- Stable report key production nằm ngoài service env trong file root-only mode
  `0600`; chỉ lưu fingerprint trong tài liệu. Key phải được dùng lại để đối chiếu
  source refs remediation, không được thay bằng key ngẫu nhiên giữa các lượt.
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

## Worksheet quyết định production 16/07/2026

Generator `server/deploy/pointWalletMigrationDecisionWorksheet.js` chỉ nhận report dry-run
đúng schema/checksum, từ chối field identity thô, không sinh migration SQL và ghi output mới
bằng chế độ exclusive `0600`. Unit test được ghim trong suite dry-run.

- Generator SHA-256:
  `923017f8f6574b8d036ca7045a70f2982897aa22d8e8e1d1d00df0d2a812906e`.
- JSON root-only:
  `/root/ywonder-point-reports/point-wallet-migration-decision-worksheet-20260716T143041Z-923017f8.json`,
  SHA-256 `d3cf5dbb9d4eabcae3e4c9e9f97e7c0146440d3eb13f9495626bbefb115cf455`.
- Markdown root-only:
  `/root/ywonder-point-reports/point-wallet-migration-decision-worksheet-20260716T143041Z-923017f8.md`,
  SHA-256 `8a3f52381c3b4f2008f2e3aceeeead10178ed009c5d82e083738e990928c8cd9`.
- Worksheet có 16 account ẩn danh và 21 mục quyết định `PENDING`: 6 opening balance
  tổng `30000 Point`, 1 nhóm synthetic canary tổng `3 Point`, 9 account có balance web
  legacy khác 0 (gồm các account residual) và 1 account balance 0 nhưng còn lịch sử legacy.
  Ba account residual dưới micro vẫn `BLOCKED`; các nhóm có thể chồng nhau.
- Tổng balance web nằm trong review là `3422.666667 Point`. Không được hiểu đây là số
  cần cộng vào game. Một account đã map đồng thời có `12 Point` commission web và opening
  seed game, nên mâu thuẫn nghiệp vụ một ví phải được giải quyết trước migration.
- Lượt sinh worksheet không gọi database, không restart/deploy service và đã xóa script
  tạm. Tại thời điểm sinh ban đầu, worksheet chỉ là phiếu xin quyết định và 21 lựa chọn
  đều `PENDING`; policy ở mục kế tiếp là bằng chứng phê duyệt mới hơn.

## Policy phê duyệt production 16/07/2026

Chủ dự án đã duyệt policy ở
`docs/QA/POINT_WALLET_MIGRATION_APPROVED_POLICY_2026-07-16.json`:

- opening seed `5000 Point`: `DEFER_ACCOUNT_LINK`, chưa preserve hoặc đảo;
- balance web legacy: `DEFER_ACCOUNT_LINK`, giữ nguyên và chưa migrate;
- synthetic canary tổng `3 Point`: `AUDITED_REVERSAL_BEFORE_OPEN`;
- lịch sử legacy có balance 0: `ARCHIVE_HISTORY_WITH_NO_BALANCE_MIGRATION`;
- residual dưới micro: `APPROVE_ROUND_HALF_EVEN_WITH_RESIDUAL_AUDIT`.

`server/deploy/pointWalletMigrationDecisionApproval.js` chỉ áp policy lên đúng worksheet
và checksum đã duyệt. Tool từ chối field identity thô, decision thiếu/thừa, giá trị ngoài
`allowedValues`, cờ cấp quyền ghi, input/output trùng và output đã tồn tại. File mới luôn
dùng `wx`/`0600`; tool không có kết nối database hoặc HTTP và không sinh SQL.

- Applicator SHA-256:
  `84042119984beda631d4c3f9305ce5756998e60ac127778dbafac3d98aa00107`.
- Policy SHA-256:
  `ab262f7f532df2bc008ce9e0ca0769a7a8f78cb0b1ad747309b4e09d0af19e63`.
- Approved JSON root-only:
  `/root/ywonder-point-reports/point-wallet-migration-approved-decisions-20260716T152324Z-84042119.json`,
  SHA-256 `76c3368cc5937db5c93d8adbebb23d5abe2f2a8c279dd9f8d1daada5ddc7ca37`.
- Approved Markdown root-only:
  `/root/ywonder-point-reports/point-wallet-migration-approved-decisions-20260716T152324Z-84042119.md`,
  SHA-256 `dd7a6190cdf7e5d8fe4c55a3b59275adb08921173e70eb1330ced3c863355282`.
- Kết quả: 16 account, 21 decision `APPROVED`, 0 `PENDING`; decision gate
  `APPROVED`, migration gate `BLOCKED_NO_BALANCE_MIGRATION_AUTHORIZED`.
- Hậu kiểm: PID game/web `186418/186434`, health `200/200`, web root `307 -> 200`,
  callback public `404`, temp upload `0`.
- Artifact trung gian `point-wallet-migration-approved-decisions-20260716T151542Z-dabbee60.*`
  được giữ root-only để audit nhưng đã bị artifact `84042119` phía trên supersede; không dùng
  artifact trung gian làm nguồn release.

Phê duyệt policy không phải phê duyệt vận hành. Lượt này không gọi DB, không deploy hoặc
restart, không link/migrate account, không đảo synthetic `3 Point` và không normalize
residual. Hai việc sau phải là change riêng có audit, backup/rollback và duyệt vận hành;
sau đó chạy lại dry-run để giải quyết `BLOCKED=3` trước mọi migration.

## Remediation dry-run production 16/07/2026

`server/deploy/pointWalletMigrationRemediationPlan.js` chỉ nhận approved artifact đúng
checksum cùng snapshot web/game mới. Planner dựng lại report bằng cùng HMAC key rồi
fail-closed nếu account, status, mapping, balance, outbox, ledger hoặc residual khác bản đã
duyệt. Output không có identity thô, SQL hoặc lệnh thực thi; mọi operation đều
`NOT_AUTHORIZED` và cần phê duyệt vận hành riêng.

- Planner SHA-256:
  `4677eae4abf327480610e34fe09a0f6f8d2e138190f87075f75cec31eee98fb4`.
- JSON root-only:
  `/root/ywonder-point-reports/point-wallet-remediation-dry-run-20260716T155640Z-4677eae4.json`,
  SHA-256 `fdd118a89f0ac70a60a96eb587d5f7a89b8dd731f02a9922d96410fda2b65357`.
- Markdown root-only:
  `/root/ywonder-point-reports/point-wallet-remediation-dry-run-20260716T155640Z-4677eae4.md`,
  SHA-256 `975342dc9bec62f6166e7ef62d9fb657ff36d78a0f3028f902470320dd279416`.
- Synthetic plan: 1 account, 3 source, tổng reversal đề xuất `3000000` micros; từng
  source khớp đúng một game credit và operation ID đề xuất chưa tồn tại.
- Residual plan: 3 account, 6 giá trị; giữ source/normalized/residual signed atto-Point và
  đề xuất append-only audit. Projection sau khi operation được duyệt là `BLOCKED 3 -> 0`.
- Authorization: reversal `NOT_AUTHORIZED`, residual normalization `NOT_AUTHORIZED`,
  account link `DEFERRED`, balance migration/deployment `NOT_AUTHORIZED`; execution
  statements `0`, database mutations `false`.
- Candidate tạm đầu tiên dừng trước khi tạo plan vì `ywonder_game` không traverse được
  thư mục upload source `0700`. Runner đã xóa raw temp, orchestrator xóa upload, output
  count bằng 0 và PID/health không đổi. Lượt thành công chỉ mở read-only cho source không
  nhạy cảm; snapshot raw vẫn nằm trong temp root-only `0700` và bị xóa sau run.
- Hậu kiểm cuối: PID game/web `186418/186434`, health `200/200`, web root `307 -> 200`,
  callback public `404`, critical log match `0`, remediation/upload temp `0`; không restart,
  deploy, DB write, reversal, residual normalization, account link hoặc migration.

Manifest này chỉ đủ để review và xin duyệt change. Trước khi thực thi vẫn phải có backup
checksum, operation ID bất biến, transaction nguyên tử balance + ledger/audit, rollback và
fresh double-snapshot ngay trước write. Sau write phải chạy lại dry-run; chỉ report mới có
`BLOCKED=0` mới được đi tiếp tới account link/migration.

## Reconciliation sau remediation 17/07/2026

Exporter/report/worksheet được harden để đọc ledger `point_remediation_reversal` và
`point_remediation_reversal_rollback`. Evidence hợp lệ phải khớp operation ID,
idempotency, request/plan/approval checksum, source refs HMAC, toàn bộ outbox/game
credit, tổng Point micros và phép tính remainder. Thiếu, trùng, dùng lại source, sai
amount hoặc rollback lệch đều fail-closed; chỉ apply hợp lệ chưa rollback mới được
ghi nhận `REVERSED` và không sinh quyết định đảo lần hai.

- Report root-only:
  `/root/ywonder-point-reports/point-wallet-migration-post-remediation-20260716T172353Z-c62f14df.json`.
- Report SHA-256:
  `7397b05d18780cc056dea7c3727aedcc80400570d421caa58072c92d4bf6fd2a`.
- Kết quả: 159 account; `NO_ACTION=143`, `MANUAL_RECONCILIATION_REQUIRED=6`,
  `UNMAPPED_LEGACY_REVIEW=10`, `BLOCKED=0`; residual account/value đều `0`.
- Ba outbox synthetic tổng `3000000` micros được chứng minh đã reversal; unresolved
  synthetic bằng `0`, không có `OUTBOX_WITHOUT_WEB_SOURCE_TRANSACTION` còn lại.
- Worksheet JSON/Markdown root-only có 16 account và 17 decision: 6 opening seed,
  10 legacy balance và 1 zero-balance history. SHA-256 lần lượt là
  `e223c85ddd26233f0478ae5f02a2b02b2318d5ee050f0c5343174d843a238e0f` và
  `5c76c718cc636514ad731afb42bc8f8de2a90723c2508d8cbb06b6224abffc73`.
- SQLite checksum, PID game/web `186418/186434`, health `200/307/404` và file mode
  `0600` giữ nguyên; raw/source tạm đã xóa. Không restart/deploy/DB mutation,
  account link, balance migration hoặc giao dịch tiền thật.

Policy 21 quyết định cũ là bằng chứng lịch sử trước remediation, không được áp thẳng
vào worksheet mới vì còn hai key operation đã hoàn tất. Bước kế tiếp là tạo policy
successor chỉ gồm `gameOpeningBalanceTreatment=DEFER_ACCOUNT_LINK`,
`legacyWebBalanceTreatment=DEFER_ACCOUNT_LINK` và
`legacyWebHistoryTreatment=ARCHIVE_HISTORY_WITH_NO_BALANCE_MIGRATION`; policy đó
vẫn không cấp quyền ghi, deploy, link hoặc migrate.

## Successor policy hậu remediation 17/07/2026

Chủ dự án đã duyệt ba lựa chọn còn hiệu lực; policy được lưu tại
`docs/QA/POINT_WALLET_MIGRATION_POST_REMEDIATION_POLICY_2026-07-17.json`, SHA-256
`89fcdc1b22d8ac9d20d5bf4761b5696c4d0503b0962eb7f7bd9f28af6d88aacc`. Policy
không còn key synthetic/residual đã hoàn tất và đặt mọi cờ DB mutation, deploy,
balance migration, synthetic reversal về `false`; operational change vẫn phải xin
duyệt riêng.

- Full migration suite và remediation execution suite pass local.
- Applicator SHA-256
  `84042119984beda631d4c3f9305ce5756998e60ac127778dbafac3d98aa00107` đọc đúng
  worksheet SHA-256
  `e223c85ddd26233f0478ae5f02a2b02b2318d5ee050f0c5343174d843a238e0f`.
- Artifact JSON root-only:
  `/root/ywonder-point-reports/point-wallet-migration-approved-post-remediation-20260716T173254Z-84042119.json`,
  SHA-256 `aa371bd52ef22ef473d390b2d14cf44d53f62f0b23b216f1ef74f02503c96ca8`.
- Artifact Markdown root-only:
  `/root/ywonder-point-reports/point-wallet-migration-approved-post-remediation-20260716T173254Z-84042119.md`,
  SHA-256 `9fa41f3f5d9fa70a6a0957f8d518ad936a3e4dc51efb3e64eb26cf9dd7f00891`.
- Kết quả: 16 account review, `17/17` decision approved, pending `0`, source blocked
  `0`; migration gate vẫn `BLOCKED_NO_BALANCE_MIGRATION_AUTHORIZED`.
- `syntheticCreditReversal` và `legacySubMicroNormalization` đều `NOT_EXECUTED` với
  `selectedPolicy=null`; account link `DEFERRED`, balance migration `NOT_AUTHORIZED`.
- Hai artifact mode `0600`; source policy/applicator tạm trên VPS đã xóa.
- SQLite SHA-256
  `a4f0ffa570071c9799c3c3c915519b65bea434b0bfe886f1a59f2b572ac22467`, dung lượng
  `76980224` byte, PID game/web `186418/186434`, health `200/200`, web root `307` và
  callback public `404` giữ nguyên. Không deploy/restart/DB mutation/link/migrate,
  reversal, normalization hoặc tiền thật.

Gate quyết định đã đạt nhưng không phải gate vận hành. Bước kế tiếp là audit và code
web debit orchestrator `Point -> USDT/YWH` trong một tranche riêng; mọi deploy, account
link hoặc balance migration vẫn cần approval/checksum/rollback riêng.

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
