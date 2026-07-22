# Danh sách công việc dự án (Task Backlog & Progress)

## Ưu tiên hiện tại 19/07/2026: kiểm thử source-lot PostgreSQL cô lập và hoàn thiện contract hoa hồng/VIP

> Phase 1 online cơ bản đã đạt. Point web/game là cùng một ví; rate hiện tại `26,5 Point/USDT`, `1,59 Point/YWH`. Hoa hồng tiêu dùng theo nguồn: nguồn USDT trả USDT, nguồn gameplay trả Point; giữ số lẻ/FIFO/rate version/transfer source và reversal. Khách chốt 6 cấp `8% + 5 x 1%`; VIP cộng dồn từ `2.650 Point` nguồn USDT. Bản trả lời mới nhất sửa ngược bản trước: Point nguồn USDT chuyển tới vẫn tính VIP khi tiêu; refund trừ tiến độ và thu hồi VIP nếu rơi dưới ngưỡng. Share của A vẫn phát sinh khi A hoặc B chưa VIP nhưng bị khóa; chỉ mở sử dụng/rút khi cả A và B đều VIP, đồng thời mở toàn bộ lịch sử tương ứng. Candidate source-lot local đã pass, source/rate lineage hiện đủ cho phân loại VIP nhưng chưa nối runtime/deploy. Nền authority v3 production vẫn dormant; debit tắt, chưa link/migrate, chưa tiền thật.
>
> Production dry-run read-only 16/07/2026 đã chạy thành công bằng commit `7dffc8b5`: snapshot kép web/game không drift, raw/temp đã xóa, PID/build/env/health giữ nguyên và không có DB mutation. Report ẩn danh có 159 account: `NO_ACTION=143`, `UNMAPPED_LEGACY_REVIEW=7`, `MANUAL_RECONCILIATION_REQUIRED=6`, `BLOCKED=3`. Ba blocker là 6 giá trị SWAP/ví legacy có phần lẻ dưới 1 micro-Point; 6 game identity đều có opening seed đúng `5000` Point cần phân loại bằng văn bản. Ba outbox canary `1 Point` đều `SENT` và khớp đúng một game credit, tổng gửi/nhận cùng `3 Point`, không cộng đôi. Internal top-up vẫn `canary` một account thật, debit wallet tắt và callback public `404`; nền v3 đã deploy dormant nhưng balance vẫn chưa migrate.
>
> Chủ dự án đã duyệt đủ 21/21 decision item và duyệt riêng hai operation remediation. Tooling commit `7aeec648` khóa approval/checksum, executor SQLite append-only và executor PostgreSQL serializable/idempotent. Hai operation đã pass local, bản sao SQLite/PostgreSQL trên VPS, apply/replay/rollback cô lập và production backup/preflight; account link và balance migration vẫn `DEFER/NOT_AUTHORIZED`.
>
> Remediation production đã đạt `[x]`: backup `/root/ywonder-point-backups/point-remediation-20260716T163736Z-7aeec648` có manifest SHA-256 `7c5dc5272cc791e1632f1a4738ed3a2b73ef89d4ba6b3544848360fff8977954`. Residual `ROUND_HALF_EVEN` đã chuẩn hóa đúng 3 account/6 giá trị, audit `NORMALIZE=6/ROLLBACK=0`, replay không ghi thêm và báo cáo nền mới đưa `BLOCKED 3 -> 0`, residual value `6 -> 0`. Ba Point synthetic trước đây là canary không tiền đã được đảo đúng một lần sau khi account đã tiêu dùng: số dư tại preflight là `4370`, không còn là ảnh lịch sử `5003`; production hiện là `4367 Point`, ledger remediation đúng `1 x -3`, rollback ledger `0`, replay idempotent. Validation summary root-only có SHA-256 `e31f737bc0177fcceb6fa3f6b1c980a6c5ad379ac4ee7f13352ef50b2e46c37d`. Nghiệm thu client sau reversal cũng đạt: relogin EXE đọc `4367`, EXE -> APK giữ `4367` và đẩy EXE, APK -> EXE vẫn `4367` và đẩy APK; cả hai client bị thay phiên đều hiện toast đúng. Không deploy/restart/link/migrate, không dùng tiền thật; PID game/web vẫn `186418/186434`, health `200/307/404`, outbox pending `0`, warning log `0`. Internal top-up vẫn khóa ở `mode=canary`, allowlist đúng 1, `allowRemote=false`; public callback vẫn `404`.
>
> Reconciliation hậu remediation đã đạt `[x]`: tooling đọc metadata ledger `point_remediation_reversal`, kiểm operation/checksum/source refs/tổng micros/rollback và fail-closed nếu thiếu, trùng hoặc lệch. Production double-snapshot read-only tạo report `/root/ywonder-point-reports/point-wallet-migration-post-remediation-20260716T172353Z-c62f14df.json`, SHA-256 `7397b05d18780cc056dea7c3727aedcc80400570d421caa58072c92d4bf6fd2a`: `BLOCKED=0`, residual `0`, ba outbox synthetic tổng `3 Point` được xác nhận đã reversal và không còn đề xuất đảo lần hai. Worksheet mới có 16 account review/17 decision còn hiệu lực: 6 opening seed, 10 legacy balance và 1 zero-balance history; synthetic/residual pending đều `0`. SQLite hash, PID `186418/186434`, health `200/307/404` giữ nguyên; source tạm/raw đã xóa, không deploy/restart/DB mutation.
>
> Successor policy hậu remediation đã đạt `[x]`: `docs/QA/POINT_WALLET_MIGRATION_POST_REMEDIATION_POLICY_2026-07-17.json`, SHA-256 `89fcdc1b22d8ac9d20d5bf4761b5696c4d0503b0962eb7f7bd9f28af6d88aacc`, chỉ duyệt ba lựa chọn còn hiệu lực: opening seed và legacy balance tiếp tục `DEFER_ACCOUNT_LINK`, zero-balance history dùng `ARCHIVE_HISTORY_WITH_NO_BALANCE_MIGRATION`. Full migration/remediation suite pass local. Production applicator checksum-pinned tạo artifact root-only `point-wallet-migration-approved-post-remediation-20260716T173254Z-84042119.{json,md}`, SHA-256 `aa371bd52ef22ef473d390b2d14cf44d53f62f0b23b216f1ef74f02503c96ca8` / `9fa41f3f5d9fa70a6a0957f8d518ad936a3e4dc51efb3e64eb26cf9dd7f00891`: 17 approved, 0 pending, source `BLOCKED=0`, nhưng migration vẫn `BLOCKED_NO_BALANCE_MIGRATION_AUTHORIZED`. SQLite SHA-256 `a4f0ffa570071c9799c3c3c915519b65bea434b0bfe886f1a59f2b572ac22467`, PID `186418/186434`, health game/web `200/200`, web root `307` và callback public `404` giữ nguyên; không deploy/restart/DB mutation/link/migrate/reversal/normalization.
>
> Deploy dormant authority v3 đã đạt `[x/~]`: game release `a22312df3aee5701a31aa502d2fea3728546b2b1`, web release `/var/www/ywonder-releases/point-v3-a22312df`, build `2rdR_xG8o4G1uonGEYEg0`. Migration PostgreSQL `006` và schema SQLite authority/debit đã áp cộng thêm; tại checkpoint deploy, toàn bộ bảng link/conversion/debit/reservation mới có `0` row. Cả hai service đặt `WEB_POINT_WALLET_DEBIT_ENABLED=false`; public credit/reserve `404`. Backup root-only `/root/ywonder-point-backups/point-wallet-dormant-20260717T050203Z-a22312df`, manifest hậu switch SHA-256 `473bffad9ff2199d27fcb50f02c88f44d8f2a4aafc6088e3481f33427b3ca88f`. Hai rollback tự động do lỗi smoke harness đã phục hồi đúng release/env/health và không đổi balance; lần switch cuối pass regression web/SSO/cron/dormant route, ổn định 45 giây restart `0`, temp `0`. Không payment/conversion/debit/link/migrate/open/YWH tại checkpoint này. Trạng thái mới hơn là canary QA không tiền hoàn tất ở đoạn ngay dưới; debit vẫn tắt và chưa mở cho account legacy.

> Canary kỹ thuật không tiền trên QA riêng đã đạt `[x]`: profile `WalletQA2026` có mapping duy nhất và baseline Point `5000`. Funding synthetic có audit cấp đúng `2 USDT` theo tỷ giá Admin active `26,5 Point/USDT`; chủ dự án thao tác đổi thật trên UI web, tạo đúng một SWAP/conversion/outbox/game ledger và đưa web + game lên `5053 Point`, remainder `0`. Replay cùng transaction không cộng đôi; HMAC sai/quá hạn, ngoài allowlist, payload conflict, sai player và zero amount đều bị từ chối không đổi DB. Ma trận EXE logout/login, EXE -> APK, APK full close/relogin, APK -> EXE đều giữ `5053`; phiên cũ bị đẩy và có toast ở cả hai chiều. Không restart service, không dùng tiền thật, real deposit/withdraw totals vẫn `0`; debit vẫn `false`, public callback `404`, rollout chỉ đúng QA. Đây là `technical no-money canary complete`, chưa phải `real-money canary complete`.
>
> Cổng Next.js đã đạt `[x]`: source commit backup `b2510d4ad38eef89881931609ede60010ac50a83` đổi đúng 7 file từ `49ee6f3...`, bundle/patch khớp active release; source Next.js `14.2.18` cũ vẫn nguyên vẹn làm rollback. Không thực hiện double-restart production chỉ để drill sau khi upgrade đã khỏe; rollback script và bản cũ đã pass loopback smoke.

### Xác nhận nghiệp vụ ví Point từ BA/khách 16/07/2026

- `[x]` Point web và Point game là cùng một loại tiền, phải cùng hiển thị một số dư như một ví; không được tạo hai balance spendable rồi cộng gộp tùy ý.
- `[x]` Tỷ giá hiện tại: `1 USDT = 26,5 Point`, `1 YWH = 1,59 Point`; vẫn lưu rate version để Admin đổi sau không sửa giao dịch cũ. Cho phép `YWH <-> Point` và `Point -> USDT`.
- `[x/~]` Hoa hồng tiêu dùng theo nguồn Point: nguồn USDT trả USDT, nguồn gameplay trả Point; 6 cấp `8% + 5 x 1%`. VIP cộng dồn toàn thời gian từ `2.650 Point` nguồn USDT; Point chuyển tới vẫn tính khi tiêu, refund trừ tiến độ và thu hồi VIP nếu rơi dưới ngưỡng. Share gắn A-B, vẫn vào bể khóa nếu một bên chưa VIP, chỉ sử dụng/rút khi cả hai VIP và mở hồi tố toàn bộ khoản tương ứng.
- `[x]` Các câu trả lời mơ hồ cũ về `YWH -> Point`, transfer và hoa hồng đã được câu trả lời trực tiếp 17-19/07 supersede; không dùng lại làm contract.
- `[x]` Chọn ledger Point authoritative duy nhất: `player_economy.pos` PostgreSQL game; web linked account chỉ đọc cùng balance và đóng băng `balanceGXL/lockedGXL`. ADR đã ghi lý do, invariant và rollback gate.
- `[x]` Candidate rate/debit nền: rate Admin append version bất biến, conversion snapshot micros/rate/remainder; game có HMAC loopback `reserve/capture/release` và migration `006_point_wallet_reservations.sql`.
- `[x/~]` Reconciliation read-only đã chốt `BLOCKED=0`; successor policy checksum-pinned đã duyệt đủ 17 decision còn hiệu lực nhưng không cấp quyền ghi/link/migrate. Web saga `Point -> USDT` đã pass full isolated validator và nền candidate đã deploy dormant với debit tắt; chưa kích hoạt luồng reserve/journal/capture/release cho người dùng. `Point -> YWH`, migration/link từng account, Point transfer và outbox hoa hồng theo nguồn USDT/Point vẫn chờ contract/phê duyệt.
- `[x]` **The Memento Protocol (20/07):** thêm `docs/THE_MEMENTO_PROTOCOL.md` và `docs/MEMENTO_PACKET_CURRENT.md` để bàn giao giữa các phiên AI bằng packet có bằng chứng, ownership worktree, production checkpoint, quyết định nghiệp vụ, next gate và prompt khởi động; `docs/CONTEXT_RECOVERY.md` là snapshot hiện hành, root `CONTEXT_RECOVERY.md` là nhật ký lịch sử cần đối chiếu.

### Candidate ví Point v3 - bằng chứng 16/07/2026

- `[x]` Audit source production xác nhận `USDT -> Point` cũ vừa tăng `Wallet.balanceGXL` vừa queue game credit; game spend chỉ trừ PostgreSQL. Màn Admin lưu rate `26.5 Point/USDT` nhưng settlement vẫn hard-code `0.06 USDT/Point`.
- `[x]` ADR chọn game PostgreSQL làm ledger Point duy nhất; unlinked web wallet giữ legacy cho tới khi reconciliation, không tự cộng gộp.
- `[x]` Backend candidate thêm signed loopback `point-reserve`, `point-capture`, `point-release`; cùng ID/cùng payload idempotent, cùng ID/khác payload `409`, identity mismatch bị chặn, terminal state không đi chéo.
- `[x]` Web authority overlay thêm `PointExchangeRateVersion`, rate micros số nguyên, snapshot `rateVersionId/rateMicros/roundingRemainder`; retry đọc journal trước rate hiện hành. UI candidate nhận rate Admin thay vì estimate `0.06` cho Point.
- `[x]` Local pass: reservation integration gồm restart/race, credit regression, security, Phase 1 isolated, animal placement và authority safety.
- `[x]` Web debit extension đã pass cô lập: `GamePointDebit` state machine, durable browser intent, exact gross/fee/net micros, explicit fee BPS, cross-direction operation lock, pending USDT không spendable trước capture và cron retry. Overlay SHA-256 `2cd12483d811b966f26c3c24db52b2254c6e8a82126ece464e5cdabe8e9e986d` pass Prisma generate/validate/migration, DB E2E, Next.js build `g078J34tOR0M2Gmd1WpIp`, credit runtime và debit fault E2E trên bản sao; log SHA-256 `cc8a2f903d4f158060d1ec517bd0ba65359c85aeeac16b0cadaac76f271f8175`. Đây là bằng chứng validator trước deploy; checkpoint dormant phía trên là trạng thái production mới hơn và vẫn chưa bật giao dịch.
- `[x]` PostgreSQL smoke pass trong schema tạm `ywtest_1784200766939_669756`; reserve/release đồng thời chỉ ghi một lần, capture sống qua pool restart.
- `[x]` VPS validator pass hai lượt trên bản sao SQLite/schema cô lập. Lượt cuối: overlay SHA-256 `99a901958f4fb65379c32cf9e128cda9244ffa162f71e85333df635672a6cb2b`, build `S7-EGQjdX4Wv8vxyDOlU8`, gồm fault Admin đổi rate giữa retry. Kết quả: live web không đổi, production DB không đổi, service không restart, không dùng tiền thật.
- `[x]` Migration dry-run đã pass local và production read-only: SQLite `mode=ro/query_only`, PostgreSQL `REPEATABLE READ READ ONLY`, capture hai lượt chống drift, HMAC ref và fail-closed duplicate/orphan/mismatch. Legacy SQLite REAL được lượng tử hóa `ROUND_HALF_EVEN` chỉ để lập report, giữ residual atto-Point và bắt buộc `BLOCKED`; outbox settlement vẫn yêu cầu micro chính xác. PostgreSQL Unix-socket peer auth chỉ hạ riêng exporter sang user service, không truyền HMAC key/`PGPASSWORD`.
- `[x]` Remediation report và hai operation đã hoàn tất có audit: 3 account/6 residual đã về exact micro, `BLOCKED=0`; synthetic canary đã đảo `3000000` micros với đúng một ledger, số dư game tại operation `4370 -> 4367`. Apply/replay/rollback đều pass trên bản sao cô lập; production apply/replay và postflight đạt, không dùng rollback. Backup, receipt, final report và validation summary đều root-only/checksum-pinned. Nền candidate v3 sau đó đã deploy dormant; account link, opening seed, balance migration, bật web debit/YWH vẫn là các change riêng chưa được phép.
- `[x]` Fresh reconciliation sau remediation nhận diện đúng ba source synthetic đã reversal, không tạo `syntheticCreditTreatment` lần hai. Report mới có `MANUAL=6`, `UNMAPPED_LEGACY=10`, `NO_ACTION=143`, `BLOCKED=0`; worksheet còn 17 decision (`6 opening + 10 legacy balance + 1 zero history`), tổng synthetic chưa xử lý `0`, remediated `3 Point`. Local/full suite và VPS candidate pass; production chỉ đọc, file mode `0600`, không restart/deploy/mutation.
- `[x]` Successor policy chỉ chứa ba key defer/archive đã được ghim checksum và áp vào đúng worksheet hậu remediation: `17/17` decision approved, pending `0`, source blocked `0`; migration gate vẫn khóa vì không có quyền balance migration. Artifact JSON/Markdown root-only mode `0600`; SQLite/PID/health/callback không đổi và không operation nào được chạy.

### Thứ tự ưu tiên ngay khi mở session kế tiếp

> **Cập nhật 20/07 - source-lot PostgreSQL `[~]`:** `server/postgresSmokeTest.js` đã được mở rộng để áp migration `007` trong schema tạm và kiểm race idempotency, conflict, FIFO, `UNATTRIBUTED`, restart persistence và không tự đổi Point authoritative. `node --check` cùng JSON source-ledger pass. Máy local không có Docker/PostgreSQL/WSL distro nên chưa thể chạy `npm.cmd run test:postgres --prefix server`; cần PostgreSQL disposable + DSN riêng, tuyệt đối không dùng DSN production hoặc deploy để thay thế gate này.

> Ưu tiên mới nhất của anh: tiếp tục ví Point trước. Các lỗi gameplay bên dưới vẫn giữ nguyên backlog nhưng tạm đứng sau các cổng ví.

0. `[~]` **Ví Point chung:** remediation, client acceptance, fresh reconciliation, successor policy, full isolated validator, deploy nền và canary kỹ thuật không tiền trên QA riêng đều đạt. Luồng `2 USDT synthetic -> UI web -> 53 Point -> game ledger -> EXE/APK` đã đối soát đúng một lần ở số dư `5053`. Còn canary nhà cung cấp thật cực nhỏ khi được duyệt, migration account legacy, khóa toàn bộ positive grant gameplay, `Point -> YWH`, transfer/rút USDT bên ngoài và hoa hồng. Giữ `WEB_POINT_WALLET_DEBIT_ENABLED=false`, `mode=canary` đúng QA, public callback `404`; không mở `open` hoặc tự coi synthetic là tiền thật.

1. `[~]` **P0 thả thú không trừ túi trên artifact thật - đã khóa nguyên nhân, chờ build/runtime:** `FarmScene` hiện chỉ map mục Chuồng sang `Fence.prefab` có `FenceAutoConnect`; ba prefab `AnimalPenSpawner` cũ không được scene/build catalog tham chiếu. Artifact gây `7 -> 7` chứa client enclosure cũ chỉ spawn + save farm, trong khi hai file client atomic mới vẫn là thay đổi chưa commit nên chưa thể coi artifact cũ đã có fix. Source hiện gọi transaction server, áp snapshot túi/farm, toast luôn số con còn lại và từ chối fail-closed mọi pen legacy. Lượt test artifact mới đầu tiên chưa tới bước thả vì mua Gà báo lỗi chung; `Player.log` xác nhận server vẫn khỏe và shop chưa gửi request, mà bị client chặn bằng `PENDING_STATE_SYNC_FAILED`: scoped canary trả `403` cho reward item dương, queue đã bỏ mutation nhưng giữ cờ cần đối soát suốt phiên. `ShopTransactionService` nay chỉ khi queue đã rỗng mới chạy bootstrap authoritative có guard rồi tiếp tục shop; queue thật còn pending vẫn fail-closed, API delta dương không được mở lại. Toast đồng bộ/đối soát cũng đã tách khỏi lỗi kết nối chung. `Assembly-CSharp`, atomic placement, Phase 1 cô lập và Point-authority guard đều pass. Còn build EXE/APK mới và nghiệm thu mua con giống -> `7 -> 6`, relogin, EXE -> APK -> EXE, retry/đóng app và ca đặt thất bại.
2. `[~]` **Retest hotfix gameplay đang chờ artifact - đã re-verify source 21/07 (CÓ ĐỦ):** hủy cho thú ăn phải dừng coroutine/animation và hoàn thức ăn; cây/thú phải chết hoặc héo ngay ở `0%`, persist qua relogin và EXE -> APK -> EXE.
   - **Re-verify 21/07:** cả hai fix đã hiện diện đầy đủ trong source. Hủy cho ăn: `FarmInteractionController.CancelTimedAction` (~dòng 588) `StopCoroutine` + `timedActionCancelRefund` hoàn thức ăn + `PlayerController.CancelAction` mở khóa; feed thật chỉ chạy trong `onComplete`. Chết ngay `0%`: `FarmTile.Update` (~181) `GetWaterFraction()<=0 -> DieFromDrought`, `FarmAnimal.Update` (~138) hết cửa sổ đói `-> DieFromHunger`, đều gọi `FarmStateSync.Save*` ngay khi chết; `IsTutorialActive()` giờ là dead code (không nơi gọi) nên đã bỏ miễn chết trong tutorial. Kết luận: nếu tester vẫn thấy bug là do **build cũ**. Chỉ còn nghiệm thu runtime trên build mới.
3. `[~]` **Sửa hành vi trồng lên cụm chanh dây - code/compile đạt, chờ runtime:** ô slave hiện bị chặn trước khi mở túi/trừ hạt/chạy animation và hiển thị `Ô thuộc giàn`; cây nhiều ô kiểm đủ cụm trước khi giữ hạt, chỉ flood-fill ô đã cuốc nối liền 4 hướng theo lưới `BuildSurfaceCell`, không còn lấy ô xa toàn scene. State đổi/thiếu ô/action bận đều có toast và hoàn hạt. Windows/Android compile sạch; còn test cụm đủ/thiếu 20 ô, ô slave, relogin và EXE -> APK -> EXE.
4. `[~]` **Audit QARich và chi phí xây dựng - source đã đạt, vá thêm 1 lỗ hổng 21/07, chờ runtime/DB:** không có bypass theo account (account `DemoRich01..05` chỉ được `GiveTestLoadout` nạp sẵn vật liệu, xây vẫn bị trừ - `GameManager.cs:646`); cost set ở `BuildModeOverlayController` (`penWoodCost=4`, `pathStoneCost=4`, ruộng cost 0). Đặt công trình bắt buộc `RemoveItem` thành công, dùng reason `build_place` và log `before/after` (`GhostPlacementController.cs:574-593`); Windows/Android compile sạch.
   - **Re-verify 21/07 lòi 1 bug + đã vá (bé):** phá công trình chỉ hoàn vật liệu ở nhánh phá CHUỒNG/hàng rào (`FarmInteractionController.cs:2974`), còn nút **Delete trong Build Mode KHÔNG hoàn** vật liệu (`BuildModeOverlayController.DeleteBuildingAt`) → lát đường đá tốn 4 đá, xóa đi mất trắng. Không phải exploit (chỉ thiệt người chơi) nhưng bất nhất. **Fix:** thêm `BuildSurfaceCell.SumRefund(occupant, out wood, out stone)` (đọc mọi ô occupant chiếm, không đổi state) và cho `DeleteBuildingAt` cộng dồn rồi `inv.AddItem(..., "build_refund")` trước khi xóa; ruộng miễn phí hoàn 0. Còn test QARich trước-sau-relogin, audit transaction và **nghiệm thu runtime nút Delete hoàn đúng vật liệu**. Đặt/trừ vật liệu vẫn chưa phải transaction server nguyên tử nên chưa `[x]`.
5. `[~]` **P0 - một cây có thể cộng gỗ hai lần: prefab + Farm đã sửa (21/07), chờ runtime:** nguyên nhân gốc là `Assets/_Project/Prefabs/Tree.prefab` chứa hai `HarvestableResource` cho cùng một cây (một ở root cấu hình đúng `axe_01`/`wood_01`, một ở child `Log` để rỗng); `RealtimeClient.RegisterSharedResource` sinh networkId theo hierarchy path khi `resourceId` rỗng nên hai component thành hai networkId, backend coi là hai tài nguyên và thưởng một lần cho mỗi ID.
   - **Re-audit 21/07 (số liệu mới, thay số cũ):** `Tree.prefab` guid `93ebe91eead762d47894d764142c2600`. `FarmScene` có **4** instance cây, `CityScene` có **14** instance cây. Cả 14 cây City đã được chủ dự án `m_RemovedComponents` xóa đúng component **root** (fileID `4668956307432685771`) từ hôm 20/07 → mỗi cây City chỉ còn một component (`Log`); **lỗi nhân đôi ở City đã hết**. 4 cây Farm không override nên vẫn inherit cả hai component từ prefab → còn dính.
   - **Fix 21/07 (bé, chỉ sửa `Tree.prefab`, không đụng scene):** xóa component `HarvestableResource` root (fileID `4668956307432685771`) khỏi cả `m_Component` lẫn định nghĩa; điền cấu hình đúng vào component `Log` (nơi có sẵn BoxCollider + mesh): `requiredTool: axe_01`, `yieldItemId: wood_01`. Verify: prefab còn đúng **1** `HarvestableResource`, `0` tham chiếu tới root cũ, YAML nguyên vẹn 13 object. Chọn giữ `Log` bỏ root để KHỚP với các override City sẵn có (đều giữ `Log`); nhờ đó cả game thống nhất một kiểu và 4 cây Farm tự động còn một component.
   - **Còn lại / lưu ý:** (a) chưa nghiệm thu runtime — collider nằm trên `Log` (component giữ lại) nên chặt cây về lý thuyết vẫn chạy, cần build EXE/APK chặt thử để xác nhận một cây chỉ cộng gỗ một lần, prompt tắt khi depleted, retry trước respawn không cộng lại. (b) 14 cây City giờ có stale `m_RemovedComponents` trỏ tới root đã xóa — Unity bỏ qua an toàn, tự dọn khi save scene. (c) child `Log` còn gắn một `FarmInteractionController` (singleton) nghi lạc chỗ — CHƯA đụng, ngoài phạm vi bug nhân đôi, chờ chủ dự án xem lại. (d) phần client cũ ghi trong audit (prompt direct-tap không kiểm `isHarvestable`, `OnSharedResourceUnavailable` chưa xóa `currentHoverObject/currentActions`, model client bỏ cờ `duplicate=true` vẫn hiện toast/EXP) là các điểm gia cố phòng thủ, chưa sửa; backend PostgreSQL vẫn idempotent theo `resourceId + cycle + playerId`.
6. `[~]` **VÍ CHUNG - authority v3 + QA canary không tiền đã đạt:** Point web/game là một ví; game PostgreSQL là ledger. `USDT -> Point` dùng rate version Admin; canary QA đã chạy đúng UI web, journal/outbox/HMAC/game ledger và client EXE/APK với `5000 -> 5053`. Reserve/capture/release và web saga `Point -> USDT` đã pass cô lập nhưng production debit vẫn tắt. Còn canary tiền thật, migration số dư cũ, positive grant hardening, `Point -> YWH`, transfer/rút ngoài và contract hoa hồng theo nguồn USDT/Point; callback public tiếp tục `404`.
7. `[ ]` **Bật chính thức sau khi hợp đồng ví được chốt và canary đạt:** rà log/audit/reconciliation và các bề mặt gameplay còn có thể tự sinh Point/item; chỉ giữ callback production bật khi không còn đường client khai báo phần thưởng dương và bài nghiệm thu tiền thật đạt đầy đủ.

### Quyết định tiền tệ chính thức 15/07/2026

- `[x]` **Chốt nghiệp vụ một tiền tệ trong game:** game chỉ dùng `Point`. `UPoint` không còn là tiền tệ sản phẩm và toàn bộ chữ/số dư/UI `UPoint` sẽ được bỏ khỏi game.
- `[x]` **Chốt nguồn nạp Point:** người dùng nạp tiền qua web; sau khi giao dịch được web xác nhận/duyệt, số Point tương ứng phải được cộng trực tiếp vào đúng ví Point của người chơi trong game.
- `[x/~]` **BA/khách đã chốt ranh giới Point; ADR đã chốt authority candidate:** Point/GXL web và Point game là cùng loại/cùng một ví; USDT -> Point, YWH <-> Point, Point -> USDT và tỷ giá Admin. PostgreSQL game được chọn làm ledger Point cho account đã link; exact micros/rate version, reserve/capture/release và web orchestrator `Point -> USDT` nội bộ đã pass cô lập, còn nền schema đã deploy dormant. Còn kích hoạt có QA, migration số dư cũ, transfer/rút ngoài/reversal, YWH và phân loại đầy đủ các nguồn Point.
- `[x]` **Loại bỏ UPoint khỏi runtime và giao diện - production/code/compile/runtime đạt:** `EconomyManager`, bootstrap/mutation DTO, HUD, popup sự kiện, JSON/PostgreSQL store, schema và smoke test chỉ còn Point. Cache Point đổi sang chuỗi 64-bit; release production `6e41be4d298ac51b0246583a14759c47ab9b47b8` đã áp migration `004_single_point_currency.sql` để archive số dư/transaction UPoint legacy không quy đổi và migration `005_web_topup_point_remainder.sql` để giữ phần lẻ Point. Backend regression và biên dịch `Assembly-CSharp` độc lập bằng Unity Roslyn đã pass; EXE/APK thực tế chỉ hiển thị ví Point và cùng đọc số dư `5003`. Migration contract xóa cột legacy chỉ làm sau mốc rollback này được nghiệm thu.
- `[x/~]` **Nối web -> Point theo server-to-server:** ba lượt synthetic lịch sử trên account thật `Nhien345` đã được reversal có audit; account đó không còn dùng làm QA. Canary mới dùng profile riêng `WalletQA2026`: funding synthetic `2 USDT` loại riêng, thao tác đổi thật trên web, một SWAP/conversion/outbox/game ledger `+53`, web/game cùng `5053`, duplicate và fault matrix đều đạt. Delivery kỹ thuật hoàn tất; còn tiền thật, migration, YWH/USDT hai chiều/rút/hoa hồng và contract cuối. Public callback tiếp tục `404`.
- `[x]` **Nghiệm thu ví Point duy nhất trong canary không tiền:** Windows EXE và Android APK của QA mới nhận realtime `5000 -> 5053`, giữ `5053` qua EXE logout/login, EXE -> APK, APK full close/relogin và APK -> EXE. Phiên cũ bị thu hồi, toast phiên thay thế xuất hiện đúng cả hai chiều. PostgreSQL, web và HUD thống nhất; đây là nghiệm thu kỹ thuật không tiền, không thay thế canary nhà cung cấp thật.
- `[x/~]` **Source-lot Point local đã có, runtime/production chưa nối:** migration candidate `007`, domain module và adapter JSON/PostgreSQL lưu micro-Point, source event idempotent, origin `USDT/YWH/GAMEPLAY/ADMIN/LEGACY/UNATTRIBUTED`, source/commission rate riêng và transfer lineage. FIFO mixed-source pass và dừng ở lô chưa phân loại; standalone transfer bị chặn tới khi có transaction nguyên tử. Bản VIP mới tính theo origin nên source/rate lineage hiện có đã đủ, không cần chủ nạp USDT gốc. Chưa apply PostgreSQL, chưa backfill 5.000/số dư cũ, callback v1/v2 chưa gửi source envelope và shop/reservation/transfer chưa mutate lots. Máy local chưa có PostgreSQL DSN/runtime; phải test migration/adapter bằng schema tạm cô lập trước deploy.
- `[~]` **Hoa hồng tiêu dùng/VIP - khách chốt tiếp 19/07, chưa triển khai payout:** rate `26,5 Point/USDT`, `1,59 Point/YWH`; FIFO/rate version/transfer giữ nguồn; payout chờ tối thiểu khoảng 10 phút, lỗi không sinh hoa hồng, refund đặc biệt reversal. Cấp 1 `8%`, cấp 2-6 mỗi cấp `1%`. VIP cộng dồn toàn thời gian từ `2.650 Point` nguồn USDT; Point chuyển tới vẫn tính khi tiêu, refund trừ tiến độ và thu hồi VIP nếu rơi dưới ngưỡng. Share vẫn sinh vào bể khóa khi A hoặc B chưa VIP, chỉ mở dùng/rút khi cả hai VIP và mở toàn bộ lịch sử tương ứng. Còn chốt ảnh hưởng thu hồi VIP lên hoa hồng khác đã mở/trả, thu hồi hoa hồng đã bị tiêu, độ chính xác thập phân và phí/hạn mức/quy trình rút. Production hiện vẫn spend `pos` nguyên, chưa có fractional spend/commission outbox/VIP pool.
- `[~]` **P0 trước khi bật nạp tiền thật/open mode:** `PUT /player/inventory` đã khóa; account canary bị chặn delta Point/item dương bằng scoped identity guard. Trước `open` vẫn phải đặt `CLIENT_ASSET_GRANTS_ENABLED=false` toàn cục và chuyển tutorial/điểm danh/vòng quay/thu hoạch/câu cá/farm-thú cùng các luồng hoàn tài nguyên sang action/claim server-authoritative. Không coi canary synthetic là phê duyệt tiền thật.

### Nhiệm vụ mới từ sếp 12/07/2026

- `[x]` **Đóng gói tài liệu tester gameplay + lợi nhuận chăn nuôi:** đã tạo bộ `outputs/019f354a-e7f6-7af1-8408-d11408550cb3/YWonder_Tester_Handoff_RC1_2026-07-12.zip`, gồm workbook gameplay RC1, workbook chăn nuôi/lợi nhuận 10 loài, hướng dẫn đọc trước và hai file nguồn `VatNuoi2.xlsx` + `SuaLai4VatNuoi.xlsx`. Không chứa mật khẩu/token/secret.
- `[~]` **Chênh lệch dữ liệu chăn nuôi cần BA/test lead chốt:** tổng thức ăn Thỏ trong file là `90` nhưng tính từ `80 ngày / 1 ngày x 1` là `80`; Vịt trong file là `180` nhưng tính từ `45 ngày / 0,5 ngày x 1` là `90`; giá trứng Vịt trong `VatNuoi2` là `4,5 Point` nhưng runtime generator hiện là `5 Point`. Đã ghi thành điểm chờ xác nhận, chưa tự sửa dữ liệu/game.
- `[x]` **Phân tích cổng Đăng nhập/Đăng ký web:** yêu cầu mới đã chốt cả hai nút cùng mở `https://ywonder.net/vi/login`; người mới dùng link `Tạo trang trại mới` trên web. Audit xác nhận login có `callbackUrl` nhưng register chưa giữ callback và luôn về dashboard, nên chỉ `Application.OpenURL` chưa thể đưa phiên web về game. Thiết kế polling + PKCE/callback cùng domain đã ghi tại `docs/WEB_REGISTER_REDIRECT_PLAN_2026-07-12.md`.
- `[x]` **Nấc A web credential chạy song song, không thay luồng cũ:** release `5db92436a7974b38866fa3291f5f3e3577a2f30f` đã deploy versioned sau backup PostgreSQL/env/unit; production chạy `WEB_AUTH_MODE=http`, `AUTH_TRANSITION_MODE=parallel`, giữ đăng nhập/đăng ký local. Secret chỉ được copy nội bộ trong VPS, không in/lưu. Public acceptance bằng một account web thật và một account game local đã pass login -> bootstrap -> relogin, stable playerId và tách dữ liệu. Unity tách rõ bốn lệnh account; Nấc A vẫn là fallback khi cần rollback Nấc B.
- `[x]` **Nấc B Browser SSO - backend/web/client artifact đã nghiệm thu:** PKCE/callback/exchange và bootstrap PostgreSQL đã pass bằng tài khoản web thật. Commit callback `cac56e0f` đã deploy build `Q_dfxErFS68Q3YBChCShT`: session web nhớ sẵn không còn tự approve mà phải chọn `Tiếp tục với tài khoản này` hoặc `Đăng nhập tài khoản khác`; đổi account expire đúng session cookie, giữ callback qua login, dùng `no-store` và không còn cảnh báo khóa giả. Anh đã test đúng EXE/APK: account cũ, đổi account, đăng ký account web mới qua OTP/mã giới thiệu, tạo nhân vật đúng một lần, relogin/cross-device giữ cùng player/dữ liệu và phiên mới thay phiên cũ đều đạt. Local/web credential tiếp tục song song làm rollback, chưa cutover xóa luồng local.

#### Cổng bàn giao tiếp theo
- `[~]` **Hotfix gameplay phát hiện sau nghiệm thu Browser SSO - chờ build test:** đã khóa `FarmInteractionController` về đúng controller `FarmManager` sở hữu timed coroutine, nên nút hủy cho thú ăn phải dừng cả animation/coroutine, mở khóa nhân vật và hoàn thức ăn thay vì vẫn cho ăn. Cây/thú nay chết hoặc héo ngay khi thanh nước/đói về `0%` cả lúc tutorial đang chạy; trạng thái biến mất được lưu ngay vào snapshot farm để không sống lại khi đổi thiết bị. `Assembly-CSharp` compile sạch; cờ auto-sprint serialized cũ được chuẩn hóa về `false` để không bật lại tính năng kéo joystick chạy nhanh. Cần test Editor/EXE/APK trước khi đổi `[x]`.
- `[~]` **MVP cho sếp/khách trải nghiệm - còn 2 cổng:** (1) retest hai hotfix gameplay trên EXE/APK và xác nhận relog/cross-device không phục hồi cây/thú đã chết; (2) bàn giao artifact, tài liệu tester và kết quả test 2–5 thiết bị. Browser SSO artifact đã qua cổng riêng phía trên.
- `[ ]` **Production hoàn chỉnh - còn 2 nhóm lớn sau Nấc B:** (1) server-authoritative cho gameplay còn lại và trạng thái resource qua restart; (2) monitoring/admin/audit/cert renewal cùng nghiệm thu 5–20 thiết bị thật. Farm hai chiều xuyên thiết bị kèm revision/conflict và outbox gửi bù đã pass post-deploy trên EXE/APK. Không gộp hai nhóm còn lại vào cam kết thời gian bàn giao MVP.

### Khoảng trống trước nghiệm thu production

#### P0 - bắt buộc trước khi tuyên bố đồng bộ tài khoản hoàn chỉnh
- `[~]` **P0 mới 15/07 - thả vật nuôi trừ con giống nguyên tử, server production + client source đã đạt, chờ artifact:** thêm transaction `POST /player/farm/animals/place` cho JSON/PostgreSQL; server khóa inventory + farm revision, kiểm item/đủ số ô chuồng/ô chưa có thú, trừ đúng `1` con giống và thêm đúng một animal có `instanceId` vào snapshot trong cùng transaction. Retry dùng idempotency key nên không trừ/sinh đôi; conflict trả inventory + farm authoritative. Audit 16/07 xác nhận `FarmScene` dùng `Fence.prefab`/`BuildSurfaceCell`, còn ba prefab `AnimalPenSpawner` cũ không có tham chiếu scene; lỗi `7 -> 7` khớp chính xác client enclosure cũ chỉ spawn + save farm và artifact đã test chưa có hai file client atomic đang nằm trong worktree. Client nay áp snapshot inventory/farm, toast số con còn lại ngay sau server accept và fail-closed nhánh legacy để không còn mutation local rời rạc. `test:farm-animal-placement`, `test:security`, `test:phase1:isolated` cùng Roslyn Windows/Android đều đạt. Release production `9c5bd9d1066637b6f176fcd09781358d22a73de4` vẫn khỏe; top-up public tiếp tục tắt. **Còn trước `[x]`:** build lại cả EXE/APK từ source hiện tại rồi nghiệm thu số lượng giảm ngay, logout/relogin, EXE -> APK -> EXE, đóng app/retry không trừ hoặc sinh đôi và ca đặt thất bại không làm mất thú. Phá chuồng hoàn con đúng một lần vẫn là transaction server-authoritative kế tiếp cần chốt/test riêng.
- `[x]` **Đồng bộ farm xuyên thiết bị hai chiều - production accepted:** nguyên nhân lỗi account `Thu2026` là Unity từng có bốn snapshot PlayerPrefs độc lập (`YW_BuildState`, `YW_PlacedTiles`, `YW_FarmState`, `YW_AnimalState`) nhưng bootstrap chưa áp `farm_state` PostgreSQL vào runtime. `FarmStateSync` nay gộp bốn nguồn thành một snapshot server, tải lại ô đất/cây/timer/công trình/chuồng/thú và đẩy thay đổi sau cuốc, xây, gieo, tưới, thu hoạch, chăm thú hoặc logout. Post-deploy EXE/APK đã pass A -> B -> A cho bố cục farm, cây/nước, xây dựng, chuồng/thú, túi đồ và thời gian bù.
- `[x]` **Chống ghi đè farm giữa client/server - production accepted:** backend dùng compare-and-set nguyên tử theo `expected_version`, tự tăng revision và trả `409 FARM_STATE_CONFLICT` kèm snapshot authoritative khi client cũ ghi đè. Unity giữ revision, phục hồi snapshot server khi gặp `409`, và lưu outbox farm bền vững theo `playerId` để retry sau mất mạng/đóng app mà không lưu token. Migration PostgreSQL `003`, PostgreSQL store smoke và public Phase 1 stale-write smoke đã pass trên release production `21cc20d2a827e5327429cf5f0ecf67a6b67fdf79`.
- `[x]` **Nghiệm thu đổi thiết bị và đóng app đột ngột:** EXE/APK chứa checkpoint `21cc20d2` đã pass đăng nhập tuần tự A -> B -> A, tiền/túi/farm/cây/nước/xây dựng/chuồng/thú đồng nhất, thời gian bù đúng và outbox gửi lại chính xác sau khi app bị đóng trước khi upload hoàn tất. Một account chỉ online một phiên; máy B thay máy A ngay theo rule `4008`.

##### Ma trận đồng bộ và ca test tuần tự A -> B -> A (13/07/2026)
- `[x]` **Đã đồng bộ theo cùng tài khoản:** định danh/profile nhân vật, Point và túi đồ đều dùng dữ liệu server; bài đổi hai tài khoản A -> B -> A trước đó đã xác nhận dữ liệu riêng không bị lẫn. UPoint đã nghỉ khỏi runtime/schema active ngày 15/07; số cũ chỉ còn archive audit sau migration `004`.
- `[x]` **Farm cùng tài khoản xuyên thiết bị:** hotfix bao phủ bố cục ruộng/đường/chuồng, đất đã cuốc, cây và timer gieo/tưới/lớn/chín/héo, cây lâu năm/cụm nhiều ô, công trình, chuồng/thú, trạng thái cho ăn, sản phẩm và số lần thu hoạch. Outbox + revision/`409` đã pass local, production smoke và bài test tuần tự post-deploy trên đúng EXE/APK.
- `[~]` **Vị trí đứng cuối:** vị trí Farm đã lưu theo account và từng được xác nhận khôi phục; vẫn cần kiểm tra xuyên thiết bị. City/Mine chưa khôi phục trực tiếp từ server nên chưa được coi là đồng bộ hoàn chỉnh.
- `[~]` **Giới hạn gameplay:** câu cá/đào mỏ và `daily_limits` mới đồng bộ một phần, chưa server-authoritative toàn bộ.
- `[x]` **Giữa các tài khoản khác nhau:** tiền, túi đồ, farm và nhân vật phải tách riêng theo `playerId`; phần dùng chung chỉ gồm presence/di chuyển/hoạt ảnh/chat và cây/đá công cộng trong City/Mine. Các luồng realtime này đã pass các lần test trước.
- `[ ]` **Khoảng trống còn lại:** trạng thái cây/đá công cộng còn nằm trong RAM và có thể hồi lại sau backend restart; điểm danh/vòng quay và một số tiến trình phụ cần regression test xuyên thiết bị; vị trí City/Mine và daily limit/câu cá vẫn chưa server-authoritative toàn bộ.
- `[x]` **Ca test A -> B -> A post-deploy:** máy A tạo/thay đổi ruộng, cây, nước, công trình, chuồng/thú và túi đồ; máy B nhận đúng snapshot và thời gian đã trôi; quay lại máy A không phục hồi cache cũ. Ca phiên trùng xác nhận phiên mới thay phiên cũ bằng mã `4008`; ca đóng app đột ngột xác nhận outbox gửi bù đúng khi đăng nhập lại.
- `[x]` **Kết quả runtime retest 14/07 - đạt trên production:** anh đã xác nhận phiên đăng nhập mới thay phiên cũ ngay mà không cần đóng hẳn app; A -> B -> A không còn quay về snapshot cũ; ruộng/cây/tưới, xây dựng, túi đồ, chuồng/thú, bù thời gian và gửi bù sau đóng app đều đồng bộ chính xác. Checkpoint code và production release: `21cc20d2`.
- `[x]` **Nguyên nhân phiên đơn đã sửa trong code local:** mọi lần register/login/web login/browser exchange nay rotate `sessionId`, lưu phiên active theo player và đưa `sid` vào JWT; REST từ token cũ trả `401 SESSION_REPLACED`, WebSocket cũ bị đóng `4008` ngay khi phiên mới được cấp. Unity theo dõi token/scope đã mở socket, đóng/reconnect khi auth đổi và có `/auth/logout` để thu hồi phiên chủ động.
- `[x]` **Nguyên nhân farm bị ghi cũ/không lên server đã sửa trong code local:** `PUT /player/farm-state` dùng revision compare-and-set; client bị `4008` đi qua logout không-save nên không thể upload snapshot cũ. Pending farm được ghi vào outbox PlayerPrefs theo player và chỉ xóa khi đúng payload được server nhận; lỗi mạng/`401` giữ lại để lần đăng nhập sau retry trước bootstrap; `409` phục hồi snapshot server.
- `[x]` **Regression local P0 14/07:** `node --check`, Phase 1 JSON smoke, realtime smoke, `test:security`, `test:browser-auth` và `test:web-auth` đều pass. Test bao gồm token cũ bị `401`, socket cũ bị `4008`, logout thu hồi token, farm stale write nhận `409` và không ghi đè. Unity Editor không phát sinh `error CS` mới sau lần compile cuối. PostgreSQL smoke thật sau đó đã pass trong schema tạm trên VPS production.
- `[x]` **Cổng triển khai backend P0:** đã tạo backup PostgreSQL + env + systemd unit, apply `003_active_player_sessions.sql`, deploy versioned release `21cc20d2a827e5327429cf5f0ecf67a6b67fdf79` và chạy PostgreSQL/public Phase 1 smoke. Session replacement, logout revoke, stale farm `409`, HTTPS/WSS và cleanup account smoke đều pass. Kiểm tra độc lập từ máy Windows xác nhận health PostgreSQL; chỉ `80/443` mở, `3000/5432/8080` vẫn đóng.
- `[x]` **Cổng nghiệm thu client sau deploy P0:** EXE/APK chứa checkpoint `21cc20d2` đã pass B thay A ngay khi A còn mở, A -> B -> A giữ đúng farm/cây/nước/túi/xây dựng/chuồng/thú, không phục hồi snapshot cũ và retry outbox thành công sau đóng app đột ngột.
- `[ ]` **Tách cấu hình thời gian Demo/Production:** RC hiện dùng `GameTimeConfig.SecondsPerGameDay = 60f` để khách xem nhanh vòng chơi. Trước bản vận hành phải có cấu hình Production `1 ngày game = 24 giờ thật`, áp đúng `CayTrong2`, `CayTrongLauNam2`, `VatNuoi2`, các cập nhật BA mới hơn và test bằng đồng hồ mô phỏng/server time; không ngồi chờ thực tế hàng tháng.
- `[~]` **Chốt số liệu thời gian còn mơ hồ:** cây ngắn ngày mới được BA xác nhận miệng 24 giờ; cây lâu năm có chu kỳ tưới/thu hoạch trong file nhưng phải đối chiếu các cập nhật sau file (đặc biệt chanh dây 5.300 Point/cụm 20 cây và rule gieo/thu cụm) trước khi khóa Production Time.

#### P1 - bắt buộc cho sign-off vận hành ổn định
- `[~]` **Nghiệm thu đúng bản RC trên 4–5 thiết bị thật khác mạng:** hiện mới pass 1 EXE mạng A + 1 APK mạng B; bài 20 client còn lại là tự động, chưa phải 20 người cầm máy chơi. Chạy P0 trong `YWonder_Phase1_TestCases_2026-07-11.xlsx` trên chính EXE/APK bàn giao.
- `[~]` **Xác minh gia hạn HTTPS tự động:** certificate hiện hợp lệ nhưng `certbot.timer` từng ở trạng thái `enabled/inactive`; phải chạy renewal dry-run và xác nhận timer trước sign-off dài hạn.
- `[~]` **Theo dõi reconnect chậm:** người chơi có thể phải chờ lâu khi nối lại realtime. Đo thời gian reconnect trên Wi-Fi/4G, chặn sinh player trùng và thêm thông báo rõ nếu vượt ngưỡng; hiện chưa chặn bản trải nghiệm.
- `[ ]` **Lưu trạng thái cây/đá realtime qua restart:** claim/depleted/respawn của tài nguyên công cộng hiện còn nằm trong RAM Node; backend restart sẽ làm mới trạng thái khai thác. Chuyển snapshot tài nguyên và `respawnAt` vào PostgreSQL hoặc khôi phục theo timestamp server để người chơi không thấy cây/đá hồi sai sau deploy/restart.
- `[ ]` **Hoàn thiện server-authoritative cho gameplay còn lại:** rà câu cá, đào mỏ/daily limits, phần thưởng/chi phí ngoài shop, cây trồng, thú nuôi và xây dựng; server phải kiểm tra điều kiện, idempotency và là nguồn dữ liệu cuối cùng thay vì chỉ nhận delta từ client.
- `[ ]` **Đồng bộ vị trí ngoài farm:** vị trí farm đã lưu theo account; City/Mine hiện chưa khôi phục trực tiếp từ server vì scene cần load trước. Chốt quy tắc spawn an toàn theo đảo và lưu island/pose hợp lệ.
- `[ ]` **Bổ sung vận hành production:** dashboard/admin có xác thực, phân quyền và audit log để khóa account, xem/chỉnh dữ liệu có kiểm soát; monitoring/alert khi Node/PostgreSQL/Nginx/WebSocket chết, backup lỗi hoặc ổ đĩa gần đầy. Dashboard dev `/admin` vẫn phải đóng public.
- `[ ]` **Dọn account P1 cũ:** PostgreSQL còn `P1A_h09433`, `P1B_h09433`, `P1Race_h09433` dùng cho restart/reboot acceptance. Sau khi chốt không cần regression nữa, backup rồi xóa/vô hiệu hóa; không gửi các account này cho tester thay cho `QARich`.
- `[ ]` **Ngừng hạ tầng demo cũ sau nghiệm thu VPS:** xác nhận không build nào còn trỏ Cloudflare Quick Tunnel hoặc backend Windows/local, sau đó tắt tiến trình/service cũ và lưu tài liệu rollback cần thiết; chỉ giữ `https://api.ywonder.net/game-api` làm endpoint RC/production.
- `[ ]` **Vòng đời account QA:** sau đợt test phải đổi mật khẩu hoặc vô hiệu hóa `QARich0001..QARich0005`; không bật `DEMO_ACCOUNTS_ENABLED=true` trên production vì demo seeding có thể ghi lại economy/inventory lúc restart.

#### Ngoài phạm vi MVP Online RC hiện tại
- `[~]` **Nấc B browser SSO và nghiệm thu trước cutover:** callback/exchange một lần + PKCE, giữ callback qua đăng ký và bộ chọn/đổi tài khoản web đã pass production; account web thật map ổn định về cùng player. Còn test EXE/APK cross-device, restart/rollback Browser SSO, account web khóa/xóa và phiên trùng `4008` trên luồng web. Production tiếp tục giữ account game local/QARich; chưa tắt local và chưa dùng nạp/rút hoặc tiền thật trong RC.
- `[~]` **Hệ web cũ chung domain cần bên web xử lý:** các vấn đề CORS/header/stack trace của route web nằm ngoài namespace `/game-api`; game backend không được sửa chồng lên route web cũ. Ghi nhận bằng chứng, giao owner web khắc phục và regression test để bảo đảm Nginx vẫn tách biệt hai hệ thống.
- `[~]` **Mở rộng tải sau 20 người:** bài tự động 20 client đã pass; chỉ nâng room/server, load test dài và scale ngang khi khách chốt số CCU mục tiêu lớn hơn.

### Phase 1 - tài khoản game tự đăng ký + lưu tiến trình MVP
- `[x]` Tạo 5 tài khoản QA production `QARich0001..QARich0005` qua API đăng ký bình thường, không bật lại demo seeding. Mỗi account đã fresh-login và `/player/bootstrap` xác nhận `500.000 Point / 2.500 UPoint`, profile đã tạo nhân vật + hoàn tất tutorial, 80 ô kho và 31 loại item phục vụ test shop/farm/xây dựng/chăn nuôi. Mật khẩu ngẫu nhiên lưu ngoài repo; phải đổi mật khẩu hoặc vô hiệu hóa account sau đợt test.
- `[x]` Commit checkpoint trước khi quay lại backend: `8054205 feat: sync backend bootstrap and farm save polish`.
- `[x]` Unity register gửi `email` lên backend; server `/auth/register` lưu `username/email/phone/password_hash` vào JSON store, chặn trùng username và email.
- `[x]` Unity login nay thử `/auth/login` local trước; nếu server trả `USER_NOT_FOUND` mới fallback sang `/auth/web-login`. Nhờ vậy tài khoản tự đăng ký có password thật, còn web bridge vẫn dùng được khi có web account thật.
- `[x]` Server `/auth/login` trả `404 USER_NOT_FOUND` khi chưa có tài khoản local và `401` khi sai mật khẩu, để không vô tình fallback web/mock khi nhập sai password của tài khoản đã đăng ký.
- `[x]` Thêm `server/phase1SmokeTest.js` và npm script `test:phase1` để chứng minh register -> login -> `/player/bootstrap` -> lưu Point/inventory/farm_state -> idempotency -> realtime chat.
- `[x]` Đã chạy test Phase 1 trên server tạm port `3101`, data file riêng trong temp: `npm.cmd run test:phase1 --prefix server` pass.
- `[x]` Hotfix backend demo account: seed sẵn `DemoRealtime01..05` và `DemoRich01..05`, cho phép password `demo` hoặc trùng tên account; token demo cũ được map về player local chuẩn để tránh máy cache cũ/máy fresh login nhìn sai tiền hoặc sai nhân vật.
- `[x]` Hotfix realtime duplicate session: server chỉ giữ 1 phiên/account, gửi `SESSION_REPLACED` và đóng phiên cũ mã `4008`; Unity phiên cũ dừng reconnect, đăng xuất và quay về Login. Public smoke test và EXE runtime với `Nhien0001` đã được anh xác nhận: phiên mới thay phiên cũ đúng yêu cầu.
- `[x]` Hotfix online-only: build public không còn tự resume gameplay bằng cache khi backend/tunnel chết; login/register phân biệt rõ lỗi mất kết nối, sai mật khẩu và account/email trùng. Bổ sung 10/07: `ApiClient` giữ mã lỗi JSON, tài khoản không tồn tại hoặc sai mật khẩu đều hiện đúng "Sai tên tài khoản hoặc mật khẩu" thay vì báo nhầm máy chủ tạm ngừng. Anh đã test lại bản EXE/APK và xác nhận luồng đăng nhập hoạt động đúng.
- `[x]` Đồng bộ animation realtime ngoài walk/run: gửi đúng state hiện tại, tốc độ và dụng cụ cho `Jump`, `Swimming`, `Hoeing`, `Mining`, `TreeCuttingV4`, `Watering`, `Fishing`, `Feed`, `Planting`; remote dùng Animator nam/nữ tương ứng. Smoke test server đã pass `Jump` và `Mining + Pickaxe`, Unity compile không lỗi; anh đã build hai client và xác nhận các hoạt ảnh hoạt động tốt.
- `[x]` Đồng bộ tài nguyên cây/đá ở room công cộng `city/mine`: server giữ trạng thái theo `resourceId`, chỉ người claim đầu tiên được cộng thưởng, ghi inventory + lượt đào nguyên tử/idempotent, broadcast biến mất, snapshot cho người vào sau và hồi sinh sau 20 giây. Unity chỉ cộng túi đồ sau khi server xác nhận; mất kết nối không tự cộng local. Smoke test backend temp/public và Unity compile đều pass; ngày 10/07 anh đã test bản build nhiều client và xác nhận vận hành rất ổn.
- `[x]` Đã audit tài nguyên/tiền/túi đồ/shop/farm-state tại `docs/PHASE1_STATE_SYNC_AUDIT.md`: profile có đọc/ghi server; shop và khai thác cây/đá public đã server-authoritative; farm/crop/animal, câu cá và nhiều reward/chi phí khác vẫn local. PostgreSQL adapter đã hoàn thiện ngày 11/07 nhưng các khoảng trống gameplay ownership này vẫn còn, nên Phase 1 chưa nghiệm thu toàn bộ.
- `[x]` P1 shop buy/sell server-authoritative: catalog server sinh từ 109 `ItemDefinition` + 8 `ShopDefinition`, API nguyên tử `POST /player/shop/transaction` kiểm shop/item/giá/số lượng/idempotency và trả cùng lúc economy + inventory. Unity khóa nút khi request đang chạy, retry bằng cùng key khi mất phản hồi, chỉ áp tiền/túi từ server và không fallback giao dịch local khi mất mạng. Smoke temp/public, C# compile và runtime EXE/APK/relogin đã pass; ngày 10/07 anh xác nhận mọi thứ hoạt động khá tốt. Reconnect đôi lúc hơi lâu nhưng không chặn demo, theo dõi riêng nếu tăng tần suất.
- `[x]` Tách cache gameplay theo `playerId`: đã thêm `PlayerScopedPrefs`, migration legacy chỉ cho một account nhận, event lưu scope cũ/nạp scope mới và chuyển Point/inventory/tool/EXP/vị trí, farm/cây, ô lát, công trình, thú, lượt câu/đào, điểm danh/vòng quay sang key riêng. Online-only không đọc/ghi save gameplay chung khi chưa đăng nhập; setting thiết bị và auth vẫn dùng chung đúng chủ đích. C# compile pass; ngày 10/07 anh đã test A -> B -> A và đóng hẳn/mở lại EXE, xác nhận hai tài khoản không lẫn dữ liệu và khôi phục đúng.
- `[x]` Đồng bộ mọi biến động gameplay của inventory/economy: `InventoryManager.AddItem/RemoveItem` và `EconomyManager.Add/Spend` nay xếp hàng gọi `inventory/adjust` hoặc `economy/apply`, mỗi delta có idempotency key và retry cùng key. Bootstrap, shop và logout chờ hàng đợi để không nạp đè snapshot cũ. Nhờ đó cùng một luồng bao phủ hạt, nước, nông sản, gỗ/đá/cá, thức ăn, thú, phân bón, quà, vật liệu xây và Point/UPoint ngoài shop. Unity compile, Phase 1 smoke, API relogin riêng (`+20 nước`, `+2/-1 hạt sầu riêng`, `+50/-20 Point`) và bản Unity runtime đều pass; ngày 10/07 anh xác nhận vận hành ổn.
- `[x]` Khôi phục vị trí farm theo account: logout lưu pose trước khi hủy nhân vật/clear auth; lần đăng nhập hồ sơ cũ ưu tiên tọa độ farm đã lưu rồi mới fallback về bến. Ngày 10/07 anh test bản mới và xác nhận ổn. Hiện chưa khôi phục trực tiếp vị trí ở City/Mine vì các scene đảo đó cần được load trước.
- `[~]` Nối `farm_state` hai chiều đã có hotfix Unity + API round-trip, chờ EXE/APK runtime; `daily_limits` cho câu cá/đào mỏ vẫn chưa nối hoàn chỉnh.
- `[x]` Backend Phase 1 đã public thử qua Cloudflare Quick Tunnel với JWT secret ngẫu nhiên, `WEB_AUTH_MODE=disabled`, dashboard admin tắt và max 20 người/room. Public REST/WebSocket smoke test pass; EXE runtime xác nhận hai account khác nhau gặp/chat được trong City và account trùng bị thay phiên mã `4008`. URL Quick Tunnel là runtime tạm, không commit làm URL production.
- `[x]` Audit read-only VPS game `42.96.18.14`: Ubuntu 22.04.5 LTS trên KVM, 2 vCPU, RAM 3.8 GiB + swap 3.8 GiB, disk 50 GB còn khoảng 37 GB, timezone Asia/Ho_Chi_Minh/NTP đúng; UFW deny inbound và mới chỉ mở SSH 22. Chưa có Node, PostgreSQL, Caddy/Nginx/Docker, không có service lỗi hay ứng dụng cũ cần giữ. Cấu hình đủ demo khoảng 20 người; báo cáo ở `docs/VPS_GAME_AUDIT_2026-07-10.md`. Mật khẩu giữ ngoài repo.
- `[x]` Nền PostgreSQL production trên VPS: tạo OS service account `ywonder_game` không có interactive shell, role PostgreSQL `ywonder_game` không có quyền superuser/createdb/createrole và database cùng tên dùng peer authentication qua Unix socket. Migration `001_initial` đã tạo 10 bảng public; env production nằm ngoài repo dưới `/etc/ywonder-game`; backup timer hằng ngày đã `enabled/active`, lượt backup đầu tiên thành công và restore drill vào database tạm đã pass rồi dọn sạch. PostgreSQL vẫn chỉ listen `127.0.0.1:5432`, UFW không public `5432`; chưa import `data.json`.
- `[x]` Private staging VPS: Node `24.18.0` LTS và Caddy `2.11.4` đã cài; backend commit `ebc9982` chạy bằng `ywonder-game-server.service`, Caddy proxy nội bộ và cả hai service đều `enabled/active`. Migration skip đúng `001_initial`; health xác nhận store `postgres`; full Phase 1 REST/WebSocket smoke qua Caddy pass. Node chỉ listen `127.0.0.1:3000`, Caddy chỉ listen `127.0.0.1:8080`; test từ máy anh xác nhận chỉ `22` mở, còn `80/443/3000/5432/8080` đều đóng. Không import JSON cũ, DNS và Unity URL chưa đổi.
- `[x]` Gia cố backend trước public ở commit `09433bff`: production startup gate chặn secret yếu/JSON/mock/dashboard/demo/public bind; bcrypt async cost 12; rate limit theo IP + account + đăng ký; body/CORS/security header/request ID/log không ghi body-token; HTTP timeout; WebSocket giới hạn connection/payload/message; shutdown sạch và systemd sandbox bổ sung. `test:security`, full Phase 1 local và `npm audit --omit=dev` đều pass.
- `[x]` Private redeploy hardening: release `09433bff1e739bd2573c8068ffa58f445cd01bb6` chạy trên PostgreSQL thật; full Phase 1 qua SSH tunnel -> Caddy pass, sai password trả `401` kèm rate-limit `15`, `/admin` trả `404`, backup timer active/enabled. Lần chạy root đầu dừng an toàn trước switch vì migration giữ `USER=root`; script đã ép `PGUSER/USER/LOGNAME=ywonder_game` và lần hai pass. Chỉ `22` public; `80/443/3000/5432/8080` vẫn đóng, DNS `api.ywonder.net` vẫn ở `45.119.83.233`.
- `[x]` Controlled restart acceptance trên VPS production: trước restart đã chụp fingerprint profile/economy/inventory/farm/daily-limit/transaction của `P1A_h09433`, `P1B_h09433`, `P1Race_h09433`; restart PostgreSQL rồi `ywonder-game-server` lúc `11:35:58 +07` và fingerprint sau restart khớp hoàn toàn. Ba account login/bootstrap lại qua SSH tunnel + Caddy đều giữ Point, inventory và farm state; Node/Caddy health trả `storage.mode=postgres`, PostgreSQL/Node/Caddy/backup timer đều `active/enabled`.
- `[x]` Full VPS reboot acceptance: VPS boot lại lúc `13:13:19 +07`, `boot_id` đổi sang `ee8dfd96-8d69-43c0-a0ab-5ddcccd109f9`; PostgreSQL, `ywonder-game-server`, Caddy và backup timer đều tự lên ở trạng thái `active/enabled`. Health qua private Caddy trả `storage.mode=postgres`; ba account P1 login/bootstrap lại được và canonical fingerprint trước/sau reboot khớp `a003b888ed68b5ee95e43efae2ee0873fafd291dac66aac0ffceeaf7c649bf6e`.
- `[x]` Private automated 20-client acceptance: thêm `server/phase1LoadTest.js` + `npm run test:load`, tạo/reuse 20 account theo prefix riêng, bootstrap đủ profile/economy/inventory/farm/daily limits, mở 20 WebSocket cùng room `city`, nhận đủ roster 19 peer, relay state/chat/ping và giữ kết nối. Lượt chạy thật qua Caddy private + PostgreSQL pass; p95 auth `1532.4 ms`, bootstrap `31.9 ms`, WebSocket connect `36.6 ms`. Backup pre-cutover `/var/backups/ywonder-game/ywonder_game_20260711T072715Z.dump` có SHA-256 `04dda7ac1048d0de493a25f91ab98116f784494460c1cbfa390479d646679a7e`; hậu kiểm xác nhận account tải còn `0`, không OOM và PostgreSQL/Node/Caddy/backup timer vẫn active.
- `[x]` Public Nginx audit read-only: DNS đã trỏ `api.ywonder.net -> 42.96.18.14`; Nginx giữ `80/443`, HTTP -> HTTPS và certificate hợp lệ; `3000/5432/8080` vẫn đóng public. Nginx hiện giữ `/api/game/* -> 3033` cho web API cũ và mọi path còn lại `-> 3036`, nên `/player/bootstrap` và `/realtime` đang `404`; backend PostgreSQL của ta vẫn khỏe tại loopback `3000/8080`. Báo cáo: `docs/NGINX_PUBLIC_AUDIT_2026-07-11.md`.
- `[x]` Public Nginx cutover: đã backup `/etc/nginx/sites-available/ywonder.net.conf`, thêm đúng `/game-api/*` và WebSocket `/game-api/realtime -> 127.0.0.1:3000`, giữ nguyên `/api/game/* -> 3033` và root `-> 3036`. `nginx -t`/reload pass; Nginx, game-server, PostgreSQL và Caddy đều active/enabled. Backup SHA-256 `87c987eb81767be2e121a4a3fc035600329cc95b73595ca4ddb192650c55a878`, config mới `b7b6cc5b28d89b37e35b94ac26099e992efef25d199cd6489e97d8248b5185d8`.
- `[x]` External REST/WSS acceptance: từ máy Windows, `https://api.ywonder.net/game-api/health` trả PostgreSQL; automated 20-client pass với p95 auth/bootstrap/WSS `1666.4/64.9/173.7 ms`; full Phase 1 pass register/login/shop/idempotency/farm-state/relogin/chat/session replacement. Account test đã dọn về `0`, ba account P1 gốc còn đủ; `80/443` mở và `3000/5432/8080` vẫn đóng public.
- `[x]` Unity `Assets/Resources/BackendConfig.asset` đã chuyển `baseUrl = https://api.ywonder.net/game-api`, giữ `useOfflineFallback = 0`; chờ build EXE/APK và test thiết bị thật.
- `[~]` `certbot.timer` hiện `enabled` nhưng `inactive`; certificate hiện vẫn hợp lệ nhưng cần xác minh/bật lịch renewal trước nghiệm thu production dài hạn.
- `[~]` Test thiết bị thật ngoài mạng: do giới hạn thiết bị, anh đã test 1 EXE qua mạng A + 1 APK qua mạng B; chat realtime và đào khoáng đồng bộ đều hoạt động rất tốt. Vẫn cần test lại bản hotfix mới và mở rộng lên 4–5 máy khi có điều kiện; chưa đủ để tick nghiệm thu 5–20 thiết bị.
- `[x]` Hotfix sau test 2 thiết bị: tutorial NPC đã bỏ hoàn toàn node đào khoáng và kết thúc sau đúng ba nhóm `chặt cây -> xây ruộng/trồng trọt -> xây chuồng` (11 bước, không bắt thả thú/cho ăn); `StartTutorial()` khóa chạy lặp theo phiên, reset khi đổi/logout account và dấu `!` được dedupe/dọn khi hoàn tất. Form đăng ký mobile không còn hiện cảnh báo độ dài liên tục khi đang gõ; nội dung chuẩn hóa thành username/password mới cần ít nhất 9 ký tự theo đúng backend. C# compile pass; ngày 11/07 anh đã build/test lại và xác nhận ba lỗi tutorial, dấu `!` và cảnh báo đăng ký mobile đều đã xử lý đúng.

## Ưu tiên hiện tại 06/07/2026: tạm gác backend, quay lại chỉnh sửa game

> Quyết định mới từ anh: tạm dừng chuỗi backend sau khi đã có nền mock/API/dashboard/realtime; giữ toàn bộ task backend bên dưới để quay lại sau khi xong nhóm chỉnh sửa game trước mắt.

### Yêu cầu khách 06/07/2026 - backlog chỉnh sửa game trước mắt

> Nguyên tắc: đây là yêu cầu khách, chưa triển khai. Trước khi sửa từng mục phải đọc script/prefab/scene liên quan và ưu tiên những lỗi ảnh hưởng demo/build/mobile trước. Backend vẫn tạm gác cho tới khi nhóm này ổn.

#### A. Nền game, ánh sáng, nước
- `[ ]` Giữ hướng ánh sáng/màu nền hiện tại vì anh xác nhận đang đúng mẫu khách từng gửi; khi chỉnh scene phải tránh làm game tối lại.
- `[x]` Màu nước biển Farm/City đã chỉnh và anh đã test lại trên điện thoại: màu xanh đã cải thiện, đủ chốt cho demo hiện tại. Nếu phát sinh lỗi màu mới trên máy thật thì mở task riêng theo ảnh/video triệu chứng.

#### B. Thành phố / City scene
- `[ ]` Dời nhà MiniGarden bán sản phẩm sang bên trái nhà nâng cấp dụng cụ, giữ cửa quay ra đường chính.
- `[ ]` Trang trí quanh nhà MiniGarden bằng hoa/cây cảnh thực tế hơn, tránh cảm giác khối đá/hoạt hình quá thô so với game 3D.
- `[ ]` Làm bãi biển kéo từ khu nhà MiniGarden xuống phía biển theo yêu cầu khách; cần bố trí lối đi/khu bãi hợp lý để nối với phần bãi biển có thuyền, bờ kè và điểm câu cá.
- `[ ]` Làm bãi biển thành phố có thuyền và bờ kè như bên farm; đặt vùng câu cá đúng khu bãi biển/gần bờ kè để người chơi tập trung đông vui.
- `[ ]` Tất cả cửa hàng trong game cần bảng hiệu chuyên nghiệp dạng model 3D gắn trước nhà; dùng tên bảng hiệu hiện tại làm nội dung hiển thị.
- `[ ]` Nơi dịch chuyển ở thành phố đổi thành căn nhà giống bên farm nhưng cao hơn, trang trí thêm để có phong cách riêng.
- `[x]` MiniGarden/Sa Chi/Sầu Riêng/Chanh dây: repo đã có seed/product, iconTexture và shop whitelist cho `sacha_01`, `durian_01`, `passion_fruit_01`; anh đã check phần shop/data tổng thể và chốt các sản phẩm này đủ đi tiếp. Icon sản phẩm chanh dây mới có thể đổi mapping sau nếu anh bổ sung asset mới.
- `[~]` Chanh dây rule mới từ BA/khách: `200 USDT / 20 cây` là giá cả cụm 20 cây; tỉ giá 26.500 được quy về `5.300 Point` trong game. Đã cập nhật `passion_fruit_seed_01.buyPrice = 5300` và `ItemDataGenerator` để không bị generator trả về 1560. Code đã tách `seedItemCost` khỏi `plotSlots`: `Giống chanh leo` trong shop được hiểu là 1 gói/cụm 20 cây giá 5.300 Point, khi gieo trừ 1 item giống nhưng vẫn chiếm 20 ô đất bằng `plotSlots = 20`. Đã vá persistence cây nhiều ô bằng `slaveTileKeys` để save/load lại cụm chanh dây không mất ô phụ. Vẫn cần test runtime riêng và chốt/sửa sản lượng thu hoạch cho đúng ý "cụm 20 cây -> sản phẩm tương ứng" nên chưa tick x.

#### C. Nông trại / Farm scene và cửa hàng
- `[x]` Các cửa hàng trong game phải hiển thị vật phẩm và câu chữ to hơn khoảng 3 lần so với hiện tại, đặc biệt trên điện thoại: đã thêm layout mobile riêng cho ShopPopup theo hướng giữ khung popup gần kích thước cũ, chỉ làm card/icon/chữ/giá/nút thao tác lớn hơn để mỗi màn hình hiện ít sản phẩm hơn và scroll xem tiếp. Cập nhật 08/07: anh đã chốt layout shop lớn hơn/filter shop sau khi test.
- `[x]` Sửa trigger cửa hàng ở farm và city: anh đã thu/đặt lại trigger vừa vùng mặt trước cửa hàng để chỉ đứng đúng khu vực cửa mới hiện vật phẩm/vật nuôi/shop; khi đã vào sâu hơn trong vùng cửa thì UI vẫn phải giữ, không bị mất. Cần test lại nhanh trên APK/EXE sau build.
- `[x]` Chặn lỗi mất tiến trình khi alt-tab/mất focus trong lúc load: các hệ save farm/build/cây nhiều ô/ô tự đặt/thú/tài nguyên nay chỉ được ghi PlayerPrefs sau khi load/restore xong, tránh ghi save rỗng hoặc crop rỗng đè lên dữ liệu cũ. Editor/EXE cũng bật chạy nền để loading/async không bị treo khi anh qua cửa sổ khác.
- `[ ]` Thêm 5 cây xanh trong trang trại để người chơi chặt cây và nhận gỗ.
- `[ ]` Nhân vật phải có bóng người rõ ràng khi di chuyển trong scene.
- `[ ]` Cối xoay gió phải quay và nhìn chuyên nghiệp hơn; hiện tại khách chưa thấy đạt.

#### D. Tutorial, điểm danh, red-dot chỉ dẫn
- `[x]` Khi đăng nhập, người chơi phải làm hết hướng dẫn NPC tân thủ thì mới nhận được quà điểm danh ngày đầu trong popup: đã code khóa điểm danh ngày 1 nếu `tutorialCompleted` chưa xong; anh đã check xong nhóm tutorial/điểm danh hiện tại.
- `[x]` Thêm dấu chấm đỏ ở các nút cần bấm để ngầm chỉ dẫn người chơi nhận quà/khám phá/chức năng mới: đã thêm red-dot runtime cho nhiệm vụ/tutorial, lịch điểm danh, nút búa khi tutorial cần xây, và túi đồ khi tutorial cần dùng vật phẩm/thú/thức ăn; anh đã check xong UX hiện tại.

#### E. Tâm tương tác, build/farm thao tác ô đất
- `[x]` Đổi tâm tương tác trước mũi chân nhân vật thành ô vuông kích thước 1 ô đất: đã thêm `FrontBuildCellSelector` tự chọn `BuildSurfaceCell` ngay phía trước theo hướng mặt nhân vật và vẽ viền trắng runtime. Anh đã test và xác nhận viền sáng tốt.
- `[x]` Workflow build mới bước 2: đã chuyển nút búa/build xuống cụm tay phải phía trên nút Jump, và mở build list cũ ở góc nhìn nhân vật hiện tại, không tự bật camera top-down/ẩn GameHUD. Anh đã chốt nhóm build flow hiện tại.
- `[x]` Workflow build mới bước 3: khi chọn Ruộng/Đường đá/Chuồng, ghost tự snap và ghim vào đúng ô đang viền trắng trước mặt; người chơi chỉ cần bấm OK để xác nhận hoặc X để hủy. Anh đã chốt nhóm build flow hiện tại.
- `[x]` Workflow build compact popup: đã thay full HUD build bằng popup ngang gọn bên phải gần nút búa; góc popup hiện vật liệu đúng cho 3 công trình (Gỗ/Đá), bên dưới có 3 thẻ Ruộng/Đường đá/Chuồng. Chọn thẻ sẽ pin ghost vào ô trắng; nút tích xanh và X nằm ngay dưới thẻ để xác nhận/hủy. Anh đã chốt nhóm build flow hiện tại.
- `[x]` Bỏ nút X hủy hoạt ảnh HUD khi đang mở build popup/xây Ruộng/Đường đá/Chuồng; build flow chỉ dùng nút tích xanh/X dưới thẻ và nút đóng popup, tránh nút X đỏ nổi đè lên khu vực Jump trên mobile.
- `[x]` Yêu cầu mới của khách: bỏ tâm/crosshair cho tương tác chính. Cây/tảng đá dùng tap/click trực tiếp lên vật thể trong tầm khoảng 3.5m, có spherecast assist để đỡ miss collider/góc bấm và UI tự ẩn khi nhân vật đi xa. Ruộng/nước/chuồng dùng prompt theo ô/điểm trước chân để thao tác ổn hơn trên điện thoại.
- `[x]` Fix click nền Thành phố xuyên xuống biển: direct tap chỉ được xuyên sai số bề mặt `0.05m` sau collider đặc, thay vì khoảng hỗ trợ gần `1.7m`; nền đất chặn WaterSource/FishingSpot bên dưới nhưng click trực tiếp biển/điểm câu vẫn hoạt động. Anh đã test runtime ngày 10/07 và xác nhận ổn.
- `[ ]` Khi đặt khung vào ô đất thì hiện biểu tượng làm nông; bấm biểu tượng mở ngay 3 lựa chọn: cuốc đất, lát đá, xây chuồng.
- `[ ]` Menu 3 lựa chọn này không được chuyển sang trang khác như hiện tại; phải là thao tác tại chỗ, icon/chữ to hơn khoảng 3 lần.

#### F. UI/HUD, zoom, mobile controls
- `[x]` Giao diện farm/HUD mobile lớn hơn: đã thêm class `hud-mobile` cho mobile build, tăng kích thước cụm thông tin nhân vật/quest/tiền/sidebar/joystick/sprint/build/jump/nút tương tác, thêm safe inset/popup close guard để giảm lỗi nút `X` bị lẹm; anh đã test/chốt nhóm HUD mobile hiện tại.
- `[ ]` Thêm/kiểm tra chức năng thu phóng cả map/camera và UI theo nhu cầu khách.
- `[x]` Bỏ chức năng kéo joystick mạnh/giữ lâu thì tự chạy nhanh; giữ lại nút chạy tự động để người chơi chủ động bật/tắt auto-run. Anh đã test APK và xác nhận tốt.
- `[x]` Auto-run cancel mới: khi auto-run đang bật mà người chơi bắt đầu điều khiển joystick, `GameHUDController` sẽ tắt auto-run ngay và cập nhật lại trạng thái nút Sprint. Anh đã chốt nhóm joystick/auto-run.
- `[x]` Joystick mobile: đã bỏ các mũi tên text trang trí trong joystick để tránh lỗi mũi tên trái render thành ô vuông/missing glyph trên thiết bị. Anh đã chốt nhóm joystick/mobile.
- `[x]` Nhân vật đứng yên: đã bỏ nhánh tự xoay về yaw camera khi thả joystick; nhân vật sẽ giữ hướng di chuyển cuối cùng. Anh đã chốt nhóm joystick/mobile.
- `[x]` Emote vẫy tay/chỉ tay: không hiện nút X hủy hoạt ảnh; rê joystick sẽ tự hủy `Waving`/`Pointing` và cho nhân vật di chuyển.
- `[x]` Sửa bug joystick mobile: đã tách touch khỏi action `Look`, chặn pointer joystick không đi vào vùng xoay camera, và đảo lại trục dọc touch-look để vuốt lên = camera ngẩng lên, vuốt xuống = camera cúi xuống. Anh đã test APK: joystick/camera đỡ hơn, trục dọc đúng cảm giác hơn, logic di chuyển giữ đồng bộ giữa các map.

#### G. Tiết kiệm / Version 2
- `[~]` Gói tiết kiệm để version 2: không sửa tên, chỉ sửa phần tiền và lãi suất; bỏ heo trong logic/visual liên quan nếu đang dùng.
- `[~]` Lãi suất yêu cầu: 30 ngày = 2%, 90 ngày = 7%, 270 ngày = 22%. Cần kiểm tra thêm cách tính lãi/cuối kỳ khi bắt đầu làm version 2.

#### H. Múc nước, tưới cây
- `[x]` Tưới cây phải trừ 1 nước mỗi lần.
- `[x]` Nhân vật muốn tưới cây phải đi múc nước; mỗi thao tác múc nước được 10 thùng/nước đưa về kho.
- `[x]` Khi mũi chân/điểm trước chân nhân vật tới gần hồ nước (`WaterSource`) thì tự hiện gợi ý `Múc nước`; không cần hiện viền trắng như ô đất canh tác.

## Tạm gác 06/07/2026: backend làm tại nhà, chưa cần chung mạng công ty

> Bối cảnh: anh làm online ở nhà khoảng 3 ngày, không phụ thuộc máy case/mạng công ty. Tập trung các phần có thể code/test local: DB-ready, API contract, server storage, Unity sync từng phần. Các việc public domain/máy vật lý/proxy production tạm gác đến khi có lại máy case.

### Chốt lại phần mơ hồ: Web account -> Game account -> Backend gameplay
- `[x]` Viết tài liệu hành trình rõ ràng tại `docs/WEB_GAME_BACKEND_JOURNEY.md`: web là nguồn tài khoản, game backend map `web_user_id -> playerId`, Unity chỉ gọi game-server, gameplay nhạy cảm chuyển dần sang server-authoritative.
- `[x]` Ghi rõ hiện trạng ban đầu: backend MVP đã có API/dashboard/data mẫu còn Unity shop/economy/inventory local. Cập nhật 10/07: shop đã chuyển sang transaction server-authoritative và relogin giữ đúng Point/inventory; các gameplay khác vẫn chuyển dần.
- `[x]` Ghi nhận yêu cầu trước mắt từ sếp: khách đăng nhập bằng tài khoản web hoặc tài khoản được cấp sẵn, vào game online realtime để chat/tương tác ở các đảo công cộng; đảo farm không thuộc realtime công cộng trong phase này.
- `[x]` Chốt thêm phạm vi MVP sắp tới: chưa cần làm hệ thống nạp/rút; ưu tiên đảm bảo yếu tố online + realtime cho khách hàng.
- `[x]` Chốt account MVP: web hiện đăng nhập bằng email/số điện thoại/password; khách phải tạo tài khoản trước khi chơi; 1 account = 1 nhân vật; account bị khóa/xóa mềm thì game cũng bị chặn; nhiều máy cùng account phải dùng chung state server.
- `[x]` Chốt gameplay online sau lát realtime: tiền, túi đồ, shop, farm/thú sẽ chuyển dần server-side; mất mạng thì không cho mua/bán; daily limit/timer nhạy cảm tính theo giờ server; sếp/admin có thể chỉnh dữ liệu qua dashboard nhưng phải có audit.
- `[x]` Chốt ví ngày 15/07/2026: game chỉ có một tiền tệ `Point`; bỏ `UPoint` và UI liên quan. Tiền nạp qua web được cộng trực tiếp vào ví Point của đúng người chơi trong game.
- `[~]` API game-server nhận Point từ giao dịch nạp web đã code/test local bằng server-to-server + HMAC + `transactionId/idempotency_key`; còn audit/đấu callback-outbox phía web, rehearsal migration và deploy an toàn. Phần rút tiền khỏi game chưa nằm trong quyết định này.
- `[~]` Hỏi/chốt timezone server cho daily reset: khuyến nghị `Asia/Saigon` nếu khách chủ yếu ở Việt Nam; nếu dùng UTC phải ghi rõ trên UI/tài liệu. Tạm gác đến khi quay lại backend.
- `[~]` Chốt DB/hạ tầng: PostgreSQL cho staging/production; JSON chỉ dev/local; server case có backup, auto-start, HTTPS, WebSocket Upgrade, firewall/router và test ngoài LAN. Tạm gác đến khi quay lại backend.
- `[~]` Loop A kế tiếp: chuẩn hóa login web/cấp sẵn -> game JWT, map `web_user_id -> playerId`, xử lý `active/locked/soft_deleted`. Tạm gác đến khi xong nhóm chỉnh sửa game.
- `[x]` Loop B kế tiếp: kiểm lại realtime public islands (`city`/`mine`) và bảo đảm farm không join room realtime công cộng; chat vẫn là kênh global khi client online nhưng không join room.
- `[x]` Loop C kế tiếp: dựng/test lát online + realtime end-to-end đã pass trên build EXE + APK theo test của anh: `DemoRealtime01` và `DemoRealtime02` đăng nhập được, thấy nhau và chat được.
- `[x]` Loop D sau realtime: `PlayerBootstrapService` đã gọi `/player/bootstrap` sau login/resume và hydrate `PlayerProfileService`, `EconomyManager`, `InventoryManager`; anh đã test `DemoRich01` đọc đúng `500.000 Point / 2.500 UPoint` từ backend demo.
- `[x]` Loop E sau realtime/state: shop buy/sell server-authoritative + catalog giá đã pass smoke và runtime; relogin giữ đúng Point/inventory. Reconnect đôi lúc chậm nhưng hiện không ảnh hưởng nghiệm thu loop.
- `[~]` Loop F sau đó: nâng `/admin` thành dashboard online cho sếp: login admin, role `super_admin`, audit log, reset demo/staging an toàn. Tạm gác đến khi xong nhóm chỉnh sửa game.

### Đã có nền tảng
- `[x]` Web auth contract đã có: game-server gọi web auth qua `WEB_AUTH_MODE=http`, Unity không giữ `GAME_API_SECRET`.
- `[x]` Backend Node/Express stub đã có `auth`, `player/profile`, `player/bootstrap`, `economy`, `inventory`, `farm-state` MVP.
- `[x]` Realtime MVP đã có WebSocket `/realtime` và `/game-api/realtime`, room chung `city`/`mine`, chat global, remote player state; Unity giữ WebSocket cho chat khi rời shared room nhưng không hiện remote player ở farm.
- `[x]` Thêm smoke test tự động `server/realtimeSmokeTest.js` và npm script `test:realtime` để test realtime bằng account cấp sẵn khi web đang sập (`WEB_AUTH_MODE=mock`).
- `[x]` Smoke test local bằng data tạm đã pass: `DemoRealtime01` + `DemoRealtime02` + `DemoRealtime03` login qua `/auth/web-login`; 2 client join `city`, client thứ ba không join room vẫn nhận/gửi chat global; `player_state` hoạt động và join `farm` bị chặn `ROOM_NOT_SHARED`.
- `[x]` Test LAN mức cơ bản đã đạt: 2 Editor trong cùng mạng công ty có thể gặp nhau/chat ở city. Bug visual remote còn kiểm lại sau khi máy case online.
- `[x]` `server/schema.sql` + `server/migrations/001_initial.sql` đã thành schema PostgreSQL thật, có `game_accounts`, inventory meta và transaction snapshot/idempotency; migration `001_initial` đã pass trên PostgreSQL test VPS. Chưa phải DB production.

### Đã làm / chờ quay lại sau task game
- `[x]` Rà và chốt schema PostgreSQL tối thiểu cho MVP: `game_players`, `player_profiles`, `player_economy`, `player_inventory`, `player_farm_state`, `player_daily_limits`, `game_transactions`.
- `[x]` Tách `server/store.js` thành lớp storage rõ ràng để sau này đổi `data.json` sang PostgreSQL không phải sửa API route nhiều.
- `[x]` Giữ `jsonStore` cho dev/local test, thêm khung `postgresStore` hoặc adapter DB-ready theo env `STORE_MODE=json|postgres`.
- `[x]` Hoàn thiện `/player/bootstrap` làm nguồn load đầu game: trả profile + economy + inventory + farm_state + daily_limits theo đúng user.
- `[x]` Chuẩn hóa transaction/idempotency cho `economy/apply` và `inventory/adjust` để shop/mua/bán/đào/câu không bị nhân đôi khi retry.
- `[x]` Thiết kế API daily limit server-side cho câu cá và đào đá 10 lượt/ngày; Unity vẫn fallback PlayerPrefs khi offline.
- `[~]` Lập danh sách Unity managers còn đang lưu PlayerPrefs cần chuyển dần: Economy, Inventory, Farm/Build, Animal, Fishing, Mining, PiggyBank, Event. Tạm gác đến khi quay lại backend/state sync.
- `[~]` Chọn 1 luồng nhỏ để nối thử trước theo scope mới: account + realtime `city/mine` vì đây là yêu cầu MVP gần nhất. `inventory + economy` qua shop bán/mua chuyển sang sau khi realtime pass. Tạm gác.
- `[~]` Viết tài liệu handoff local test: chạy server, env mẫu, endpoint smoke test, cách đổi `BackendConfig` sang localhost/LAN. Tạm gác.

### Cập nhật 06/07/2026 - backend storage adapter + daily limits
- `[x]` `server/store.js` đã thành storage facade có `JsonStore` class cho dev/local, chọn mode bằng `STORE_MODE`.
- `[x]` Hoàn thiện `server/postgresStore.js` bằng driver `pg`: local account/web player, profile, economy, inventory, farm state, daily limits, transaction ledger; shop/resource/delta/limit dùng DB transaction và advisory idempotency lock.
- `[x]` Thêm dashboard backend local tại `http://127.0.0.1:3000/admin` để xem/tạo/sửa/xóa dữ liệu demo trong JSON store.
- `[x]` `server/schema.sql` thêm bảng `player_daily_limits`, đủ nhóm tối thiểu `game_players`, `player_profiles`, `player_economy`, `player_inventory`, `player_farm_state`, `player_daily_limits`, `game_transactions`.
- `[x]` `/player/bootstrap` trả thêm `daily_limits`; thêm `GET /player/daily-limits` và `POST /player/daily-limits/consume`.
- `[x]` `economy/apply`, `inventory/adjust`, `daily-limits/consume` đều nhận `idempotency_key`; retry cùng key không cộng đôi Point/item/lượt.
- `[x]` Smoke test Node với data file tạm: đào mỏ 10 lượt còn 0, lần 11 bị `DAILY_LIMIT_EXCEEDED`, economy/inventory retry không cộng đôi.
- `[x]` Query thật cho `STORE_MODE=postgres`, async REST/admin/realtime, migration/import/verify scripts và `test:postgres` đã hoàn tất. JSON regression, PostgreSQL direct smoke, Phase 1 REST/WebSocket, Node restart, dashboard read đều pass; import schema tạm xác minh `36 accounts / 51 players / 82 transactions`. Runbook: `docs/POSTGRESQL_PHASE2_RUNBOOK.md`.
- `[~]` Nối Unity client đọc `daily_limits` từ `/player/bootstrap` và chuyển câu cá/đào đá sang server-authoritative khi online. Tạm gác.

### Tạm gác đến khi có máy case/mạng công ty
- `[~]` Public `api.ywonder.net`, Caddy/Nginx thật, SSL, firewall/router, service auto-start trên Windows server.
- `[~]` Test điện thoại ngoài mạng công ty, test WebSocket public `wss://api.ywonder.net/game-api/realtime`.
- `[~]` Backup DB thật trên máy case và giám sát uptime.
- `[~]` Sửa/tái test các bug realtime chỉ xuất hiện khi 2 máy LAN cùng vào city nếu máy case đang mất mạng.

### Cập nhật 09/07/2026 - thông tin hạ tầng/web auth đã nhận từ chat 01/07
- `[x]` Đã có thông tin domain public dự kiến: `ywonder.net` và `api.ywonder.net`; DNS/web hiện liên quan IP `45.119.83.233`. IP máy vật lý/game API được trao đổi là `113.171.82.46`.
- `[x]` Đã được báo port public cần dùng là `80` và `443`; tuy nhiên cần kiểm tra lại từ ngoài mạng vì trước đó `api.ywonder.net` bị kẹt SSL/WAF/default-server, không phải lỗi code game.
- `[x]` Web auth endpoint dùng được ngay cho game-server: `POST https://ywonder.net/api/game/auth`, gọi server-side với `Authorization: Bearer <GAME_API_SECRET>`. Secret nằm/lưu riêng, không đưa vào Unity và không ghi vào repo.
- `[x]` Web auth response đã được bàn giao dạng camelCase + snake_case: `userId/user_id`, `username`, `refCode/ref_code`, `fullName/full_name`, `gameToken/game_token`, `tokenType`, `expiresIn/expires_in`. `gameToken` là JWT HS256, verify bằng `GAME_API_SECRET`.
- `[x]` Web có endpoint cũ đọc/cộng ledger web: `GET https://ywonder.net/api/game/balance?uid=<username>` và `POST https://ywonder.net/api/game/credit`. Hai route này không tự động là callback nạp vào game; callback mới phải gắn đúng lúc giao dịch web thành công và gọi route nội bộ có chữ ký của game-server.
- `[x]` Đã có tài khoản test web `gametest`; mật khẩu test lưu riêng, không ghi vào repo.
- `[~]` `api.ywonder.net` chỉ là URL đẹp hơn cho game API; đang cần owner/infra xử lý SSL/WAF hoặc DNS-01. Trong khi chờ, game-server có thể gọi web auth qua `https://ywonder.net/api/game/auth`.
- `[~]` Cần chốt nơi chạy game-server public: theo chat, web VPS riêng, game API dự kiến chạy ở máy vật lý/game-server port nội bộ; cần xác nhận quyền truy cập, service auto-start, domain/proxy tới game-server, DB thật và backup trước khi báo deadline production.

### Gác phase sau, chưa nên làm trong 3 ngày này
- `[~]` Firebase push notification cho cây/vật nuôi.
- `[x]` Bỏ hướng IAP/UPOS: quyết định 15/07 chỉ dùng một ví Point và nguồn nạp là website. Nếu sau này phát hành nạp trực tiếp qua App Store/Google Play thì mở task receipt validation mới, không phục hồi UPoint mặc định.
- `[~]` Bạn bè/thăm farm, leaderboard production, report/profanity moderation.
- `[~]` Photon/Mirror: chưa cần đổi vì WebSocket MVP hiện đủ cho mục tiêu gần là chat + thấy người chơi ở city/mine.

## Cập nhật 01/07/2026: web auth bridge + realtime multiplayer MVP

- `[x]` Ghi nhận contract web thật: `POST https://api.ywonder.net/api/game/auth` dùng `Authorization: Bearer <GAME_API_SECRET>`, trả cả camelCase/snake_case: `userId/user_id`, `refCode/ref_code`, `fullName/full_name`, `gameToken/game_token`, `expiresIn/expires_in`.
- `[x]` Cập nhật `webAuthProvider` để game-server gọi web auth qua env `WEB_AUTH_MODE=http`, `WEB_AUTH_LOGIN_URL`, `WEB_AUTH_SECRET` hoặc `GAME_API_SECRET`; Unity không giữ secret.
- `[x]` Verify `gameToken` JWT HS256 phía game-server theo spec `{ sub, uid, username, iat, exp }`, `sub/uid = web userId`.
- `[x]` Unity login Phase 1 thử `/auth/login` local trước; nếu server trả `USER_NOT_FOUND` mới fallback `/auth/web-login` để vẫn giữ đường nối web thật sau này.
- `[x]` Backend thêm WebSocket realtime `/realtime` và `/game-api/realtime` cho room chung `city` và `mine`, chat toàn server, presence, state remote player, emote `Waving`/`Pointing`.
- `[x]` Unity thêm `RealtimeClient` tự tạo runtime, giữ kết nối WebSocket khi đang gameplay để nhận/gửi chat global; chỉ join room `city`/`mine` để gửi local state và nhận remote player.
- `[x]` Remote player dùng prefab nhân vật hiện tại nhưng disable `PlayerInput`, `PlayerController`, `CharacterController`, collider/rigidbody để không chặn input/local gameplay.
- `[~]` Infra tạm gác khi làm ở nhà: Nginx/Caddy proxy WebSocket Upgrade cho `/realtime`; REST chạy không đảm bảo realtime chạy nếu thiếu Upgrade headers.
- `[x]` Test Unity 2 client mức LAN cơ bản: đăng nhập 2 tài khoản, cùng vào city thấy nhau/chat được theo test của anh; bug visual remote bám theo còn để kiểm lại khi máy case online.
- `[~]` Production tạm gác đến khi có máy case/public endpoint: set env `WEB_AUTH_MODE=http`, `WEB_AUTH_LOGIN_URL=https://api.ywonder.net/api/game/auth`, `WEB_AUTH_SECRET=<GAME_API_SECRET>`, `JWT_SECRET=<secret dài>`, `REALTIME_MAX_ROOM_PLAYERS=20`. Nếu `api.ywonder.net` còn kẹt SSL thì override tạm `WEB_AUTH_LOGIN_URL=https://ywonder.net/api/game/auth`.

## Cập nhật 01/07/2026: vòng quay may mắn dạng 12 múi

- `[x]` Giữ 12 phần thưởng hiện tại, gồm cả ô `Chúc may mắn lần sau`.
- `[x]` Giữ nguyên tỉ lệ/weight quay thưởng hiện tại trong `EventPopupController`.
- `[x]` Đổi visual vòng quay sang nền 12 múi màu runtime, không cần asset nền mới.
- `[x]` Mỗi múi chỉ hiển thị icon item đang có trong `ItemDatabase`; bỏ tên item và số lượng trong múi cho gọn.
- `[x]` Ô `Chúc may mắn lần sau` vẫn tồn tại trong logic thưởng nhưng để trống, không hiện icon vòng quay.
- `[x]` Đưa nút quay vào tâm vòng quay bằng icon mới `arrowforspin.png`; bỏ icon vòng quay ở giữa và mũi tên vẽ bằng USS, footer chỉ còn số lượt còn lại.
- `[x]` Khóa kích thước vòng quay bằng min/max width/height để giảm nguy cơ flex làm méo vòng khi xoay.
- `[ ]` Test Unity: mở Sự kiện -> Vòng quay, xác nhận đủ 12 múi, chỉ icon item, ô may mắn trống, tâm có icon `Spin` mới và vẫn bấm quay được.
- `[ ]` Test Unity: quay thử và nhìn khi đang xoay, vòng không bị méo rõ ở desktop và mobile landscape.
- `[ ]` Test Unity: quay thử, vòng dừng đúng phần thưởng, trừ lượt ngày, trao item/toast như trước.

## Cập nhật 01/07/2026: tối ưu đặt Build Mode trên mobile

- `[x]` Giữ nguyên luồng đặt bằng chuột/PC: raycast phải trúng `BuildSurfaceCell` như trước, không dùng assist.
- `[x]` Thêm touch aim offset trong `GhostPlacementController` để điểm ngắm mobile nằm cao hơn ngón tay, đỡ bị tay che ô nhỏ.
- `[x]` Thêm touch assist chọn `BuildSurfaceCell` gần nhất trên màn hình khi tap/kéo lệch khỏi collider ô nhỏ nhưng vẫn nằm trong bán kính hỗ trợ.
- `[x]` `BuildModeOverlayController` truyền đúng nguồn input touch/mouse khi pin vị trí, để mobile và PC dùng hai mức hỗ trợ khác nhau.
- `[ ]` Test trên điện thoại thật: Build Mode -> chọn Ruộng/Đường đá/Chuồng -> tap/kéo quanh ô nhỏ, đặc biệt hơi lệch khỏi ô, xác nhận ghost vẫn snap đúng ô mong muốn và nút OK/X hiện đúng.
- `[ ]` Test lại Editor/PC: click chuột đặt công trình không bị thay đổi cảm giác cũ.

## Cập nhật 01/07/2026: Farm tile dùng model đất thật

- `[x]` Tắt `FarmTileMarker` tự vẽ viền ô đất màu trắng/vàng/xanh/cam khi trồng trọt.
- `[x]` Tắt fallback primitive cube/sphere/cylinder trong `FarmTile` mặc định để không còn mảng màu prototype khi gieo/tưới/chín.
- `[x]` Giữ `plowedVisual` dưới cây ở trạng thái Planted/Watered/Ripe và ưu tiên `CropDefinition.cropPrefab` khi có.
- `[x]` Hỗ trợ `Soil Visual`/`Plowed Visual` gán trực tiếp prefab asset trong Inspector; `FarmTile` tự instantiate visual con và không tắt cả GameObject khi `Soil Visual` là chính prefab `DatThuong`.
- `[x]` Thêm hủy ô trồng đặt bằng Build Mode: prompt ngoài gameplay có `G - Hủy ô trồng` xác nhận 2 lần như hủy chuồng, menu xóa trong Build Mode cũng bắt được mesh con và clear `BuildSurfaceCell`.
- `[ ]` Editor: gán model đất mới xây vào `Soil Visual`, model đất đã cuốc vào `Plowed Visual`; kiểm tra crop nào thiếu `cropPrefab`.

## Cập nhật 01/07/2026: tránh bàn phím mềm che input mobile

- `[x]` Thêm helper `MobileKeyboardAvoidance` dùng chung cho UI Toolkit để đo/ước lượng chiều cao bàn phím mềm iOS/Android.
- `[x]` Login/Register tự dịch panel lên khi focus username/password/email để input không bị bàn phím che.
- `[x]` Chat dùng chung helper keyboard avoidance, vẫn giữ offset riêng khi Build Mode đang mở.
- `[ ]` Test trên điện thoại thật: login username/password, register đủ 4 field, và chat input đều còn nhìn thấy khi bàn phím bật.

## Cập nhật 01/07/2026: shop thu mua đá quý + filter chợ cá

- `[x]` Tạo nhánh `codex/gem-shop-fish-market-icons` từ `dev` cho task shop mới.
- `[x]` Thêm `Shop_GemShop` dạng SellOnly, whitelist 6 đá quý: Kyanite, Orange Calcite, Green Calcite, Fire Quartz, Amethyst, Ruby.
- `[x]` Cập nhật `ShopDataGenerator` để khi chạy lại `YWonderLand > Generate Shop Data` không mất shop thu mua đá quý.
- `[x]` Bổ sung filter `Cá` (`food`) và `Đá quý` (`materials`) cho Shop Popup; icon sản phẩm vẫn lấy từ `ItemDefinition.iconTexture`.
- `[x]` Gắn icon `Da`/`Go` từ `Assets/Sprites/icon/BoSungIcon/` cho đá/gỗ ở túi đồ qua `ItemDefinition`, và hiển thị icon vật liệu trong Build Mode.
- `[x]` Thêm helper toast item-icon dùng chung trong `ScreenToast`; đã áp dụng cho câu cá, đào đá, chặt cây, múc nước, thu hoạch cây/thú, shop mua/bán, điểm danh và vòng quay.
- `[x]` Editor: anh đã gắn `Shop_GemShop` vào quầy/NPC thu mua đá quý trong scene.
- `[ ]` Test trong Unity: đào được đá quý -> mở shop đá quý -> tab bán hiện đúng đá trong túi, icon đúng, bán cộng Point đúng theo `sellPrice`.
- `[ ]` Test trong Unity: vào Build Mode kiểm tra pill vật liệu và chi phí ô xây hiện icon `Go`/`Da`; chặt cây/đào đá/múc nước/thu hoạch/mua bán shop đều có toast icon đúng.

## Ưu tiên tiếp theo - 29/06/2026: thêm cá mới + dữ liệu đào đá mới

> Khách đã gửi số liệu mới. Phần cá/đá/daily limit/Gem Shop đã implement phần lớn; còn chờ chốt UI nâng cuốc lv2/lv3 và test Unity.

### 0. Polish đã xong ngày 29/06 trước khi sang data cá/đá
- `[x]` Đổi text hiển thị `POS` -> `Point`, `UPOS` -> `UPoint` ở UI/toast/log demo liên quan; giữ tên biến/API nội bộ.
- `[x]` Câu cá thành công có icon cá nổi/fade kèm toast, dùng icon trong `ItemDatabase` nếu có.
- `[x]` Nước biển Farm/City sáng hơn, xanh hơn; không đổi shader/sóng.
- `[x]` Khách đổi lại chăn nuôi: gia cầm gà/đà điểu/ngỗng/vịt có thịt ở vụ cuối theo Product 2 trong `VatNuoi2.md`; thịt gia cầm bán được ở Mini Garden.
- `[x]` Gắn icon mới cho thịt gà/vịt/ngỗng/đà điểu trong item assets, generator, toast vụ cuối, túi đồ và shop.
- `[x]` Cutscene thuyền không còn bị failsafe 35 giây cắt sớm; timeout nay tự tính theo quãng đường waypoint, tốc độ thuyền và buffer để thuyền kịp cập bờ.
- `[x]` Popup biểu cảm chỉ còn 2 động tác được duyệt (`Waving`, `Pointing`); bỏ `Laughing`/`Dancing` và đổi icon nút sang `BoSungIcon/VayTay.png`, `BoSungIcon/ChiTay.png`.

### A. Câu cá - thêm giống cá và tỉ lệ
- `[x]` Tìm lại hệ câu cá hiện tại (`FarmInteractionController`, item definitions/generator, shop/inventory liên quan).
- `[x]` Thêm item/product cho các loài cá mới, chỉ gắn icon nếu anh đã cung cấp đúng asset; thiếu icon thì hỏi, không đoán bừa.
- `[x]` Ghi dữ liệu cá mới vào `Assets/_Project/Docs_KichBan/CacLoaiCa.md`, kèm giá Point, tỉ lệ tier và đường dẫn icon mới.
- `[x]` Áp giá point:
  - 2 point: Cá cơm, Cá nục, Cá hồng.
  - 4 point: Cá sư tử, Cá naso, Cá nhồng.
  - 6 point: Cá sọc dưa, Cá khế, Cá mú.
  - 10 point: Cá mặt quỷ, Cá heo biển.
  - 15 point: Cá hoàng đế, Cá ngừ hoàng kim.
  - 25 point: Cá rồng đỏ.
- `[x]` Áp tỉ lệ câu từ cá giá trị cao xuống thấp: 2%, 4%, 7%, 17%, 25%, 45%.
- `[x]` Rà shop bán cá/thành phố/kho đồ/toast thu hoạch để đảm bảo cá câu được hiện đúng tên, icon, số lượng và giá trị point.

### B. Đào đá - thêm bảng đá/gem và lượt đào
- `[x]` Ghi dữ liệu đá quý mới vào `Assets/_Project/Docs_KichBan/CacLoaiDaQuy.md`, kèm giá Point, tỉ lệ, số viên, icon và ghi chú rock thường vẫn 100% với 10 rock/lượt.
- `[x]` Tìm lại hệ đào đá hiện tại trước khi sửa; xác định đang là tài nguyên thường trong `FarmInteractionController`/`HarvestableResource`.
- `[x]` Thêm item/icon đá quý cho túi đồ và toast khi đào trúng; shop thu mua đá quý đã bổ sung ở mục 01/07.
- `[x]` Hồi sinh gỗ/đá bù thời gian thật khi thoát app: lưu `respawnEndUnix` cho tài nguyên do `ResourceSpawner` quản lý, vẫn tương thích save cũ `respawnTimer`.
- `[x]` Thêm dữ liệu reward:
  - Ảnh 1: 2 point/viên, 4 viên, 50% đào trúng.
  - Ảnh 2: 3 point/viên, 4 viên, 30% đào trúng.
  - Ảnh 3: 6 point/viên, 3 viên, 12% đào trúng.
  - Ảnh 4: 12 point/viên, 2 viên, 5% đào trúng.
  - Ảnh 5: 500 point/viên, 1 viên, 2% đào trúng; nâng cấp cuốc lv2 tốn 250 point/lượt.
  - Ảnh 6 ruby quý hiếm: 3000 point/viên, 1% đào trúng; nâng cấp cuốc lv3 tốn 1500 point.
- `[x]` Áp giới hạn mỗi ngày 10 lượt đào theo ngày thật; hết lượt thì chặn đào, đào thành công thì trừ 1 lượt và toast hiện số lượt còn lại.
- `[ ]` Xác định với anh cách hiển thị/nâng cấp cuốc lv2/lv3 nếu UI hiện tại chưa có màn nâng cấp phù hợp.

### C. Đảo mỏ / map đào khoáng MVP - 30/06/2026
- `[x]` Mở khóa điểm `mine` trên bản đồ thế giới để demo có thể chọn đảo đào khoáng ngay.
- `[x]` `IslandTravelManager` cho phép travel tới `mine` và load `MineScene`; có fallback runtime `MineMap -> MineScene` cho dữ liệu Inspector cũ.
- `[x]` `FarmInteractionController` giữ câu cá chỉ ở `city`, nhưng đào đá được phép ở `city` hoặc `mine`.
- `[x]` `ResourceSpawner` hỗ trợ gắn prefab cây/đá, snap spawn xuống nền nếu cần, và random lại vị trí khi tài nguyên hồi sinh để đá mỏ không respawn cố định một chỗ.
- `[x]` `ResourceSpawner` hỗ trợ nhiều vùng spawn bằng `Collider` để anh kiểm soát khu rải đá trên map méo/rộng; nếu không gán vùng thì vẫn fallback về `spawnRadius` cũ.
- `[x]` Editor: anh đã setup `MineScene`/travel/spawn khu đào mỏ xong ở mức dùng được.
- `[x]` Editor: trong Build Settings/IslandTravelManager, mine island đã dùng `MineScene` và spawn đúng mặt đảo theo xác nhận của anh.
- `[x]` Editor: trong `MineScene`, đã đặt `ResourceSpawner`, vùng spawn và rock prefab để rải đá trên đảo mỏ.
- `[x]` Editor: đã tạo vùng spawn bằng collider/area cho map mỏ không đều.
- `[x]` Editor: đã xử lý layer/mask/snap đủ để đá spawn đúng vùng mỏ trong test hiện tại.
- `[ ]` Test: Map -> chọn Khai thác mỏ -> load `MineScene` -> đào đá -> nhận 10 rock + roll đá quý/toast icon -> chờ respawn và xác nhận đá xuất hiện ở vị trí ngẫu nhiên mới.
- `[ ]` Sau MVP: nâng cuốc lv2/lv3 và hoàn thiện shop/NPC thu mua đá quý theo UI chốt cuối.

### D. iOS/App Store Connect follow-up nếu bị hỏi lại
- `[x]` CodeMagic đã build/upload IPA lên App Store Connect được bằng exported-Xcode workflow.
- `[x]` Theo góp ý bên build, đã bỏ `submit_to_testflight: true`; build chỉ upload lên App Store Connect, add vào Internal Testing làm thủ công.
- `[x]` Tăng CodeMagic `BUILD_NUMBER` lên `2` cho bản upload lại `0.1.1 (2)`.
- `[x]` Bake `0.1.1 (2)` vào exported iOS project và thêm bước verify IPA version trước publish để tránh upload nhầm `CFBundleVersion = 1`.
- `[x]` Tăng tiếp iOS build lên `0.1.1 (4)` và thêm `ITSAppUsesNonExemptEncryption=false` vào `ios/Info.plist`; CodeMagic ép lại version/build/export-compliance key sau Unity export.
- `[ ]` Nếu tester báo không cài được, xác nhận họ đang dùng bản `0.1.1 (4)`, không phải `0.1.0 (0)`, `0.1.1 (1)`, `0.1.1 (2)` hoặc `0.1.1 (3)`.
- `[ ]` Tối ưu dung lượng iOS sau, hiện TestFlight khoảng 309 MB.

## Tiến độ đã hoàn thành (Completed)

### 1. Nâng cấp Giao diện HUD (UI/UX)
- `[x]` Thiết kế lại `GameHUD` theo phong cách **Glassmorphism** (Nền Dark Navy trong suốt `rgba(58, 71, 102, 0.5)` kết hợp viền trắng mỏng).
- `[x]` Chạy script Python tự động xóa phông nền xám, cắt grid 200x200, cạo viền 8px để bỏ line AI, và bóc tách thành công bộ icon phẳng 2D từ `FarmingIconsCollection.png`.
- `[x]` Thay thế toàn bộ text/emoji trên HUD bằng **Sprite 2D phẳng** (Flat 2D Icons).
- `[x]` Ẩn tạm thời các nút Hành động (Interact, Cancel) trên HUD (`display: none;`).
- `[x]` Thống nhất Style Guide: **HUD** dùng Sprite 2D phẳng; **Popups (Túi đồ, Shop)** dùng Sprite 2.5D/3D Render (isometric, đổ bóng).
- `[x]` Đã xây dựng các **AI Prompts** chuẩn để tạo đồ họa 2.5D isometric cho game nông trại.

### 2. Tư vấn & Gỡ lỗi 3D Pipeline (Unity & Blender)
- `[x]` Phân tích và gỡ lỗi CharacterController bị lệch tâm so với nhân vật (xử lý Pivot khác biệt giữa Model và Root Empty Object).
- `[x]` Giải thích lỗi nhảy giật trục Y do tùy chọn "Bake Into Pose" trong thẻ Animation.
- `[x]` Tư vấn sự khác biệt, ưu nhược điểm giữa Animation `Generic` và `Humanoid`.
- `[x]` Cung cấp **Quy trình chuẩn (Pipeline) xuất FBX từ Blender sang Unity Humanoid** (Bone Naming theo chuẩn Mixamo/Unity, ép T-Pose, tắt Add Leaf Bones, Apply Transforms `1, 1, 1`, trục Y-Up/-Z-Forward).

### 3. Gameplay & Hệ thống (15–19/06/2026)
- `[x]` **Backend REST đợt 1**: server stub Node/Express + client (Auth/Profile), offline-first, lưu profile + cờ tutorialCompleted thật.
- `[x]` **Tài liệu kỹ thuật**: TDD, DB_SCHEMA (ERD), SECURITY, BUILD_RELEASE; rà soát điểm mù xin khách (DiemMu_CanXinKhach, TongKet_TaiLieu_CanCo); dọn mâu thuẫn UGS.
- `[x]` **Tưới cây cầm xô**, tự gom lá vào cây, tắt rung chặt/đập, dọn Splash.
- `[x]` **Camera PUBG/Free Fire** (nhân vật quay theo yaw, hết chóng mặt); fix cuốc lệch (xoay về ô đất).
- `[x]` **Build Mode sinh prefab THẬT** (`BuildPrefabLibrary`): xây ô đất (Dirt+FarmTile) & chuồng (Nhỏ 1x1/Vừa 2x2/Lớn 3x2) + animation Hammering.
- `[x]` **Ghost preview = bản mờ prefab** (xanh/đỏ kiểu ROK), WYSIWYG; bỏ lưới hiển thị; **tự bù pivot model lệch** (MakeCenteredClone).
- `[x]` **Hàng rào tự nối liền** (`FenceAutoConnect`) — tắt cạnh giáp kiểu Minecraft.
- `[x]` **Hệ chăn nuôi cơ bản**: click chuồng → mở túi (tab Thú nuôi) chọn con vật → thả (giới hạn loài theo cỡ chuồng); **cho ăn** qua túi (Bắp ngô) + animation Feed.
- `[x]` **Tutorial viết lại** (NPC ông lão khó tính): chặt cây → đào khoáng → xây ruộng → canh tác → xây chuồng → thả thú → cho ăn. Công tắc `ForceRunTutorialForTesting`.
- `[x]` **Vật phẩm con vật**: Gà, Đà điểu, Dê, Hươu, Thỏ, Bò; fix thuyền cutscene lật.

---

## 📌 QUYẾT ĐỊNH KHÁCH (20/06) — CÂU CÁ & ĐÀO ĐÁ CHỈ Ở ĐẢO THÀNH PHỐ
> Khách chốt: **câu cá** và **đào đá** CHỈ diễn ra ở **đảo Thành phố (CityScene)**, KHÔNG có ở **đảo khởi đầu (Nông trại)**. Đảo khởi đầu tập trung trồng trọt + chăn nuôi.
- `[x]` **Gate CÂU CÁ + ĐÀO ĐÁ theo đảo (code, chắc ăn)**: `FarmInteractionController.IsOnCityIsland()` (dựa `IslandTravelManager.CurrentIslandId == "city"`). Câu cá: ẩn nút + chặn `StartFishing` nếu không ở city. Đào đá (`HarvestableResource` type Stone): ẩn nút "Đào khoáng" + chặn HandleHold/ClickHarvestResource ở đảo khác (chặt cây vẫn được mọi đảo). → KHÔNG cần đụng vị trí biển/FishingSpot/đá.
  - **CẦN Editor**: đảm bảo có FishingSpot BẬT ở khu nước thành phố (nếu nằm trong `farmOnlyObjects` thì gỡ ra/đặt riêng trong CityScene).
- `[x]` **Sửa Tutorial bỏ bước đào đá** *(module QC, đã báo)*: sau Chặt cây → sang thẳng bãi ruộng (bỏ FollowToRock + MineRock). Đánh số lại các bước thành /13. Handler đào đá cũ thành dead-code (vô hại).

---

## ✅ PHIÊN 21/06 — ĐÃ LÀM (chuẩn bị demo thứ 2)
> Lộ trình demo: `Docs_KichBan/LoTrinh_Demo_Thu2.md`. Mục tiêu: APK chơi được vòng lặp nông trại + thành phố, offline, model tạm.
- `[x]` **Toast thông báo khi nhận vật phẩm / giao dịch** (người chơi dễ nắm bắt): dùng `ScreenToast.ShowInfo` (xanh) cho thành công, `Show` (đỏ) cho thất bại.
  - **Thu hoạch cây** (`HandleHarvest`): "Thu hoạch: +N {tên}"; thiếu nước → toast đỏ kèm % giảm (gộp 1 toast, khỏi đè).
  - **Thu sản phẩm thú** (`HarvestAnimal`): "Thu hoạch: +N {tên}"; vụ cuối đã có toast "Làm thịt..." riêng → chỉ toast khi con CÒN SỐNG (tránh đè).
  - **Chặt cây / đào đá**: ~~đăng ký event `OnResourceHarvested`~~ → **ĐỔI sang gọi TRỰC TIẾP** (`HarvestResourceTick` tại 3 call-site click/hold/world-hold) vì event tĩnh dùng CHUNG với `TutorialManager`; nếu handler Tutorial ném exception thì handler toast sau bị skip → mất toast. Helper đo chênh lệch túi để ra số lượng. Vẫn KHÔNG đụng file QC `HarvestableResource.cs`. → "Chặt cây: +N Gỗ" / "Đào khoáng: +N Đá".
  - **Mua/bán shop** (`ShopPopupController.OnActionClicked`): "Đã mua/bán: Qx {tên} (∓ POS)" + toast đỏ khi thiếu POS / chuồng đầy / hết hàng.
  - **Câu cá**: thêm toast "Câu được: +1 {tên cá}" trong `FishingOverlayController.HandleQTESuccess` (module QC — sửa 1 dòng, anh đã duyệt). Panel kết quả vẫn giữ.
  - `[x]` **Bug POS câu cá — XỬ THEO HƯỚNG A** (anh chốt): câu cá CHỈ cho cá (đem shop bán mới ra tiền). Xoá chữ "Nhận +X POS!" trong mô tả 5 con cá → thay bằng "Bán được giá...". Giữ nguyên field `rewardCoins` (lỡ Phase 2 cần) + Bao Lì Xì (hứa vật phẩm sự kiện, không phải POS). ⚠️ Còn dòng `Debug.Log` "Reward coin added +X POS" thừa trong code (vô hại, chỉ dev thấy console).
- `[x]` **Sản lượng tài nguyên (khách chốt): chặt 1 cây = 10 GỖ · đào 1 đá = 10 ĐÁ**: `FarmInteractionController.HarvestResourceTick` ép `resource.minYield=maxYield=treeYield/rockYield` trước khi harvest (SerializeField `treeYield`/`rockYield`=10, chỉnh Inspector). Toast tự hiện số thật (đo chênh lệch túi). KHÔNG đụng file QC.
  - `[x]` **Fix toast chỉ báo "xong" không có số**: nhiều cây/đá trong scene để TRỐNG `yieldItemId` → đồ không vào túi + đếm = 0. Helper bù id mặc định (cây→`wood_01`, đá→`stone_01`) nếu trống → đồ vào túi đúng + toast ra "+10 Gỗ/Đá".
- `[x]` **Redesign HUD câu cá GỌN (khách 21/06)** — viết lại `FishingOverlayController.cs` + `FishingOverlay.uxml` + `Styles/FishingOverlay.uss`:
  - **BỎ:** khối chọn mồi + 3 nút, test cheat, chỉ số mồi thường/xịn, nút Thoát top-bar, **panel kết quả** (modal). Báo kết quả bằng `ScreenToast`.
  - **GIỮ + mở rộng:** 1 popup "căn thời gian" ở **góc PHẢI** (panel navy 440px, thanh căn to 34px, vùng xanh + kim, thanh giờ xanh). Tích hợp **số lượt câu/ngày** + **nút X**. Theo Cozy Dark Palia, không màu mè.
  - **Luồng + timing:** bấm F → `Show()` tự bắt đầu căn cá **8.7s** (`castDuration`, khớp animation Fishing đã chỉnh 8.5→8.7s) → giật (nút / F / Space) trúng vùng xanh → **+1 cá vào túi + toast**. Trượt/hết giờ → toast đỏ. State còn Idle↔Timing. Lượt 10/ngày (`dailyTurns`) reset theo ngày thật.
  - Số chỉnh được: SerializeField `castDuration`/`dailyTurns`/`safeZoneWidthPercent`/`pointerSpeed`. **CẦN Editor**: prefab FishingOverlay tự lấy UXML mới (cùng path) — chỉ cần Play test; nếu Inspector còn ref `confirmDialog` cũ thì kệ (đã bỏ field, vô hại).
  - ⚠️ Nợ nhỏ: re-cast bằng nút KHÔNG gọi lại `FishingLineController.PrepareCast` (dây câu cosmetic, lần đầu vẫn đúng).
- `[ ]` ⏸️ **CHỜ SẾP CHỐT — thang giá MUA con giống/cây giống** *(KHÔNG chặn build demo — demo dùng giá hiện tại được)*: VatNuoi/CayTrong cho mỗi con 3 thang số: **Định giá** (bò 44.997 — công thức lợi nhuận sếp dùng số này) / **USDT** (300, AnimalDefinition đang dùng) / **demo** (1.500, ItemDefinition = shop đang tính tiền). Trộn USDT-mua + giá-bán-game → lời ~300 lần (kinh tế thủng). Báo cáo đầy đủ: `Docs_KichBan/RaSoat_SoLieu_MauThuan.xlsx` (4 sheet). **ĐÃ XÁC NHẬN ĐÚNG y nguyên:** chu kỳ/sản lượng/thức ăn/thịt/số ô cả 10 con + giá bán SP (subagent báo lệch là đọc nhầm cột). Khi sếp chốt thang giá → bé áp + thêm Chanh dây + 3 con thiếu ItemDef (Rùa/Ngỗng/Vịt) trong 1 lần. Asset còn số demo cũ → chạy lại generator.
- `[x]` **Câu cá BẢN TẠM (khách 21/06): ẨN popup, hết giờ tự +1 cá** — anh thấy minigame chưa cần, để "sửa sau": `FishingOverlayController.Show()` đổi sang KHÔNG hiện popup → state `AutoFishing` → đợi `castDuration` (8.7s, animation câu khoá người chơi) → `HandleCatch` cộng 1 cá ngẫu nhiên + toast + thu dây. Code minigame căn-giờ (Show cũ/StartCast/AttemptPull/UXML/USS) GIỮ NGUYÊN để bật lại sau. Trừ 1 lượt/lần, hết lượt → toast.
- `[x]` **Hệ NPC Shop data-driven** (chạm nhà → popup): ShopDefinition + ShopZoneTrigger + Show(ShopDefinition) + ShopDataGenerator (7 asset) + MerchantNPC.shopData. Tên shop nổi trên đầu NPC. **CẦN Editor**: chạy `Generate Shop Data`, gắn ShopZoneTrigger + collider trigger vào nhà NPC, kéo asset + kéo NPC vào `Name Tag Target`.
- `[x]` **Hủy chuồng → hoàn 50% + trả con giống** (phím G / tap).
- `[x]` **Economy số THẬT của khách**: giá hạt + nông sản + sản lượng + EXP 8 cây (ItemDataGenerator + CropDataGenerator theo `CayTrongLauNam.md`); 10 vật nuôi khớp `VatNuoi.md`. **CẦN Editor**: chạy `Generate Mock Items` + `Generate Crop Data` + `Generate Animal Data`.
- `[x]` **Cây lớn theo MỐC THỜI GIAN** (đi đảo về vẫn lớn). *(Còn: offline thật cần lưu `growStartTime` ra đĩa/server — Bước 3 persistence.)*
- `[x]` **Cây hết bóp dẹp** (bù scale ô đất) — **CẦN Editor**: chỉnh `Model Ground Offset` từng cây nếu lún.
- `[x]` **Mobile #3 chạm tương tác** (Pointer) + **chặt cây GIỮ-ĐỂ-CHẶT** + thêm rìu/cúp/cần câu vào túi.
- `[x]` **Chặt cây hết để lại lá** (so tên lá không phân biệt hoa/thường).
- `[x]` **Bơi: nhảy leo lên bờ** + ghép nhiều Box Collider tag Water cho hồ hình dạng lạ.
- `[x]` **Đổi đảo không ngập + nhẹ máy** (`farmOnlyObjects`) — **CẦN Editor**: kéo Water (+ cảnh nông trại) vào list.
- `[x]` **Build Mode**: bỏ hẳn xoay; fix nút Tích/X "đứng lì".
- `[x]` **Ẩn name tag trong cutscene** (hiện lại khi cập bến/skip). **Cap FPS 60** (cheap mobile win).
- `[ ]` **CHỜ KHÁCH**: xác nhận "giá vốn nông sản" có phải giá BÁN không; thời gian lớn cây ngắn ngày (hiện demo 20-60s); giá con giống (đã theo VatNuoi).
- `[ ]` **CẦN model 3D** (3D gửi sáng 22/06): 4 cây ngắn ngày còn lại (cabbage/sweet_potato/morning_glory/grass) → kéo vào `Crop Prefab`; 10 thú đã đủ model → gắn `AnimalPrefabLibrary`.

---

## ✅ CẬP NHẬT 23/06 (Antigravity đã làm)
- `[x]` **Persistence real-time (wall-clock) cho cây + thú**: đóng/mở app vẫn lớn-bù/đói-bù/chết-bù đúng mốc.
- `[x]` **Vòng đời chết thật**:
  - Cây: thiếu nước quá ngưỡng sẽ chết theo luật mới.
  - Thú: đói quá ngưỡng sẽ chết, biến mất và trả ô chuồng.
- `[x]` **Build persistence**: lưu/khôi phục công trình build mode (Ruộng/Chuồng/Đường) + cây + thú theo `BuildSurfaceCell`.
- `[x]` **Áp giá Point ×26** + cập nhật economy theo bộ dữ liệu khách mới.
- `[x]` **EXP/Level bản mới** + vòng quay + điểm danh 15 ngày.
- `[x]` **Generator dữ liệu đã chạy lại** (crop/animal) cho mốc thời gian và thông số mới.
- `[x]` **Tắt `ForceRunTutorialForTesting`** (không ép tua tutorial ở bản demo chính).

---

## 🐄 NHÁNH GAMEPLAY: Chăn nuôi trong lồng (animal husbandry) — tạm xếp sau cổng ví
> Sửa & bổ sung chức năng nuôi/trồng động vật trong chuồng.
> Nhánh này từng là ưu tiên số 1. Từ quyết định ngày 15/07/2026, phải hoàn tất và kích hoạt an toàn ví web -> Point game trước, sau đó mới tiếp tục các việc chăn nuôi còn lại.

### Build theo Ô ĐẤT (surface-cell snapping) — 19/06
- `[x]` **Sửa lệch grid**: bỏ snap theo lưới ảo (`cellSize=1` lệch khối cube `0.8` + origin nhảy theo player). Ghost giờ snap vào **TÂM MẶT TRÊN** của khối cube đất (`BuildSurfaceCell`).
  - File mới: `BuildSurfaceCell.cs` (component đánh dấu ô: SurfaceCenter/FootprintSize/IsOccupied + registry), `Editor/BuildSurfaceCellSetup.cs` (menu gắn hàng loạt).
  - Sửa `GhostPlacementController.cs`: raycast → `GetComponentInParent<BuildSurfaceCell>` → snap tâm ô; validate theo `IsOccupied`; stretch theo 0.8.
  - **CẦN làm trong Editor**: map = 4000 khối "cube" (cỏ) + 400 "stone", KHÔNG nhóm, đảo méo mó, chỉ nửa phải buildable → kiểu **"sơn vùng"**: đặt nhiều BoxCollider ướm vùng buildable (lấn ra biển vô hại vì chỗ đó không có cube) → chọn hết → menu `Gắn BuildSurfaceCell theo VÙNG`; lỡ lấn khối không muốn → `Gỡ theo VÙNG`. Tag khối tên "cube*", tự thêm collider. Còn menu đệ quy + gỡ tất cả. Nếu khối gộp chung 1 mesh → đổi sang snap lưới 0.8 + 1 collider gộp.
  - TODO sau: highlight các ô buildable khi mở Build Mode (đã có `BuildSurfaceCell.All`); nối hủy công trình → `SetOccupied(false)`.

### Nhiệm vụ 19/06
- `[ ]` **Hiệu ứng thu thập**: khi thu thập (chặt/đào/thu hoạch...) làm vật phẩm **bay vào túi đồ** (animation item bay về icon túi).
- `[x]` **Hủy chuồng → thu lại tài nguyên**: ngắm ô rào ngoài gameplay → nút **"Hủy chuồng"** (phím G / tap) → `DemolishEnclosure`: trả con giống về túi (`AddItem`) → phá CẢ cụm rào (flood-fill `PenEnclosure.FindPen`) → hoàn **50% giá build** vào POS (`demolishRefundRate` chỉnh được). Ô tự `Clear()` + `ClearAnimal()` nên thả lại được ngay.
  - Sửa `BuildSurfaceCell` (lưu `BuildCost` + `AnimalObject`/`AnimalItemId` ô neo), `GhostPlacementController` (ghi `SetBuildCost` lúc đặt), `FarmInteractionController` (action + `DemolishEnclosure` + lưu con vật vào ô neo lúc thả).
  - TODO(khách chốt số): khi build cost nối vật liệu/`EconomyManager` thật thì hoàn ĐÚNG loại đã tốn (hiện build cost đang là mockup overlay, refund vào ví POS). Demolish trong Build Mode (menu ngữ cảnh `DeleteBuildingAt`) vẫn chỉ free lưới ảo cũ — chưa nối `BuildSurfaceCell` (việc riêng nếu cần).
- `[x]` **Bỏ tính năng Vuốt ve** (Pet) khỏi tương tác con vật. *(Gỡ nút E + hàm PetAnimal ở FarmInteractionController; vô hiệu hóa PetInteraction.cs. Còn: gỡ component PetInteraction khỏi prefab thú trong Editor.)*
- `[x]` **Thông tin con vật**: popup hiện giá mua / số ô chuồng / thức ăn chính / thức ăn phụ / sản phẩm — restyle Cozy Dark Palia. Thêm trường vào `AnimalDefinition` + điền data 10 con qua generator. **CẦN Editor**: chạy menu `YWonderLand ▸ Generate Animal Data` để nạp dữ liệu vào các asset `Animal_*.asset` (đảm bảo con vật spawn dùng đúng asset này).
- `[ ]` **Trồng từng ô ruộng kiểu xây hàng rào**: mỗi loài thực vật tốn số ô khác nhau. *(Khách CHƯA gửi số ô/loài cây → quyết định 19/06: **tạm cho mỗi cây = 1 ô**, chỉnh lại khi có dữ liệu thật.)*
- `[x]` **Sức chứa chuồng động + validate thả thú theo số ô**: rào = hộp vuông trên 1 ô → **ô CÓ RÀO = ô chuồng**. Ngắm/click ô rào → "Thả thú" → chọn loài → validate `penSlots` vs số ô-rào liền nhau còn trống (`PenEnclosure.FindPen` BFS cụm ô-rША 4-kề; nhiều rào kề = chuồng to) → đủ thì thả (`SetAnimal`), thiếu thì `ScreenToast` báo lỗi. Click thẳng (PC) + bấm chữ (mobile) đều chạy. Gizmo hiện trạng ô.
  - File mới: `PenEnclosure.cs` (flood-fill), `AnimalPrefabLibrary.cs` (map itemId→prefab thú), `ScreenToast.cs` (toast lỗi). Sửa `BuildSurfaceCell` (Occupant/HasFence/IsFree), `GhostPlacementController` (ghi occupant), `FarmInteractionController` (luồng thả vùng quây).
  - **CẦN Editor**: thêm 1 GameObject gắn `AnimalPrefabLibrary` + điền itemId→prefab thú; hàng rào phải đặt qua Build Mode (để ghi occupant vào ô). Phụ thuộc hệ `BuildSurfaceCell` đã chạy.
  - TODO: bước "xem thông tin loài trước khi thả" (confirm dialog) — hiện đang thả ngay khi chọn; báo lỗi đang dùng OnGUI toast (nâng UI Toolkit sau).

### Nhiệm vụ 21/06 — Vật nuôi SỐNG theo thời gian + thanh HP
- `[x]` **Vật nuôi lớn/ra sản phẩm theo MỐC THỜI GIAN** (`Time.timeAsDouble`, giống cây): đói + chu kỳ sản phẩm tính từ mốc, **đi đảo thành phố về vẫn chạy bù đúng**. Viết lại `FarmAnimal.cs` (bỏ cộng dồn `deltaTime`).
- `[x]` **Gắn logic cho thú CÓ prefab**: trước đây thả thú có model chỉ ra khối trơ (không đói/không sản phẩm). Sửa `FarmInteractionController` thả thú → `AddComponent<FarmAnimal>()` + `Initialize(def, false)` (giữ model, chỉ thêm thanh HP). Nhận diện thú đổi sang `GetComponentInParent` (chắc ăn dù collider nằm ở con sâu).
- `[x]` **Thanh HP (no/đói) nổi trên đầu** — billboard tự dựng bằng code (quad Unlit 2 mặt), **tự đo chiều cao theo model**, không cần artist. No đầy = xanh, đói = đỏ, bệnh = tím; có chấm vàng "có sản phẩm". Ẩn khi chết. Field `statusBarHeight` (0 = tự đo).
- `[x]` **Popup hiện thời gian thu hoạch + tổng số lần thu** (quyết định khách: để trong popup, không nhồi lên đầu): `AnimalInteractionPopupController` thêm "No: X% · Vụ tới: 12s · Còn 37/38 lần thu" + đếm ngược SỐNG (Update 0.25s). Không sửa UXML.
- `[x]` **Fix tràn chữ popup**: tách "Độ no" + "Thu hoạch" thành 2 DÒNG riêng trong bảng (thêm `LblHunger`/`LblHarvest` vào UXML), status về ngắn gọn, cho phép xuống dòng.
- `[x]` **Cho ăn ĐÚNG tài liệu (bỏ ngô mặc định)**: `HandleFeedSelected` validate thức ăn theo `AnimalDefinition.foodMain/foodAlt` (so theo TÊN qua ItemDatabase) + trừ ĐÚNG số lượng (vd Bò sữa cần 2x Cỏ Voi hoặc 4x Khoai Lang); sai thức ăn → toast, không trừ đồ. `EnsureStarterFeed` cấp đúng thức ăn loài cho demo. **CẦN Editor**: đảm bảo ItemDatabase có item tên khớp ("Cỏ Voi", "Khoai Lang"...) — nếu thiếu sẽ có warning trong Console.
- `[x]` **Wire ĐẦY ĐỦ logic vật nuôi theo VatNuoi.md (cả 10 con)**: trước đây chỉ 3 con base (gà/bò/heo) có logic + số demo, 7 con kia không có `produceItemId`/`maxHarvests` → thu ra rỗng + "∞ lần".
  - Generator: thêm `SetAnimalGameplay` cho cả 10 con → `produceItemId`, `produceAmount` (=SL Pro1), `maxHarvests` (=Tổng lần thu VatNuoi), thịt vụ cuối. Tạo 17 item sản phẩm/thịt còn thiếu (giá bán theo cột "Giá Product 1/2" VatNuoi). Sửa giá egg/milk/pork theo VatNuoi; pig Pro1 đổi `pork_01`→`pigskin_01` (Da heo), thịt = pork_01.
  - `AnimalDefinition` thêm `meatItemId`/`meatAmount`. `FarmAnimal.HarvestProduct`: vụ CUỐI (hết số lần thu) → cộng thịt (Pro2) + **con vật biến mất** + **giải phóng ô chuồng** (`ClearAnimal`, rào vẫn còn) → thả con mới được ngay. `FarmInteractionController` gán `occupiedCells` cho con vật lúc thả.
- `[x]` **Fix tên + loại thức ăn cho khớp VatNuoi**: đổi `grass_01` "Cỏ khô"→**"Cỏ Voi"**, `cabbage_01` "Rau cải"→**"Bắp cải"**; chuyển **7 nông sản sang category "food"** để hiện trong tab cho ăn (trước đó là "items" → không chọn được). ⚠️ Nông sản giờ nằm tab "Thực phẩm" thay vì "items" — nếu Mini Garden/shop lọc theo category cần rà lại.
- `[x]` **CẦN Editor**: chạy lại `Generate Mock Items` + `Generate Animal Data` (data mới). Test thu hoạch 10 con + vụ cuối làm thịt.
- `[ ]` **CHỜ KHÁCH**: chu kỳ thu hoạch đang để giây DEMO (25s) thay vì ngày thật — chờ khách quy đổi ngày→giây.
- `[x]` **Khung CƠ BẢN cho 10 cây LÂU NĂM** (để anh gắn model): thêm 10 hạt giống + 10 sản phẩm (ItemDataGenerator) + 10 CropDefinition (CropDataGenerator, để trống `cropPrefab` cho anh kéo model). Tạm **1-lần-thu** như cây ngắn ngày. Giá Sa Chi/Sầu Riêng theo CayTrong.md, còn lại số DEMO. **CẦN Editor**: chạy `Generate Mock Items` → `Generate Crop Data`, rồi kéo model vào `Crop_<seed>.asset`.
  - TODO Phase 2: **cơ chế thu NHIỀU LẦN** cho cây lâu năm (giống vật nuôi: ra quả nhiều vụ + vụ cuối) — FarmTile hiện chỉ 1 lần thu. Số liệu thật 7/10 cây chưa có (CayTrong.md mới có Sa Chi + Sầu Riêng + chanh dây).
  - `[x]` Wire SHOP đầy đủ (ShopDataGenerator): Farm Shop bán **đủ 18 hạt** (8 ngắn + 10 lâu năm) + **đủ 10 con giống** (thêm vịt/ngỗng/rùa); Mini Garden mua **đủ nông sản + 10 SP cây lâu năm + 20 SP/thịt vật nuôi** (trước chỉ egg/milk/pork). Thêm hạt lâu năm + SP vào `GiveTestLoadout`. **CẦN Editor**: chạy lại `Generate Shop Data` + `Generate Mock Items`.
- `[x]` **Thanh "khát nước" cho CÂY (behavior B — khách chốt)**: thanh nước nổi trên cây tụt dần theo `waterIntervalSec`; cạn = khát → cây **vẫn lớn** nhưng cộng dồn thời gian khát → lúc thu **giảm sản lượng + POS** (tới tối thiểu 50%), đúng kịch bản "quên tưới → héo, mất EXP". Tưới LẠI (action "Tưới nước" khi đang lớn) đổ đầy nước. `FarmTile`: thêm `lastWaterTime`/`dryAccumSec`/`LastCareFactor` + `WaterAgain()`/`GetWaterFraction()` + thanh billboard ĐỘC LẬP (không parent ô đất để né scale lệch). `FarmInteractionController`: Watered→"Tưới nước", phạt POS + toast. **KHÔNG đụng** phần spawn/scale model cây.
  - TODO: visual "héo" trên model (đổi màu) chưa làm — hiện báo khát bằng thanh đỏ + toast khi thu (tránh tint material artist rủi ro). EXP phạt chờ hệ EXP.
- `[x]` **THỜI GIAN THỰC + TƯỚI-GATE-LỚN (khách chốt 21/06)**: 1 ngày game = 24h thực.
  - `GameTimeConfig.cs` (Core): hằng số `SecondsPerGameDay` (DEMO 60f · THẬT 86400f) + `Days()`/`Hours()` — **1 điểm chuyển demo↔thật**.
  - Generator khai thời gian theo NGÀY/GIỜ game (cây 1 ngày lớn/tưới 10h; rau muống+cỏ voi 0.5 ngày; dưa hấu+bí ngô 2 ngày; thú theo VatNuoi: gà 2/bò 7/đà điểu 6/dê+ngỗng 3/vịt 1/thỏ 40/heo+hươu 180/rùa 300 ngày).
  - **Tưới-gate-lớn**: cây CHỈ lớn khi còn nước; hết nước → NGỪNG lớn tới khi tưới lại (`FarmTile.growthAccrued`+`GetGrownSeconds`). Bỏ phạt sản lượng behavior B.
  - **CẦN Editor**: chạy lại `Generate Crop Data` + `Generate Mock Items` + `Generate Animal Data`. Test ngoài tutorial (tutorial vẫn ép 5s).
  - ⚠️ CÒN NỢ: lưu MỐC DateTime ra đĩa để offline lớn bù khi đổi sang 86400 (bản thật). `growStartTime` thành biến thừa (cảnh báo nhẹ).
- `[x]` **Fix luồng Tutorial (2 bug)** — *(module QC, sửa tối thiểu, báo rõ)*:
  - **Chặt cây/đào khoáng nhảy bước ngay**: `OnTreeArrived`/`OnRockArrived` auto-nhảy nếu túi đã có gỗ/đá — mà loadout test tặng sẵn `wood_01`/`stone_01` → bỏ đoạn auto-skip, bắt người chơi thực sự chặt 1 nhát (vẫn nghe `HarvestableResource.OnResourceHarvested`).
  - **Thả thú không cập nhật nhiệm vụ**: tutorial nghe `AnimalPenSpawner.OnAnimalPlaced` (hệ CŨ) nhưng hệ chăn nuôi đã viết lại (BuildSurfaceCell). Thêm `FarmAnimal.OnAnimalSpawned` (bắn trong `FarmInteractionController` lúc thả) → tutorial nghe sự kiện mới. `FarmAnimal`/`FarmInteractionController` cũng sửa.
- `[x]` **Chức năng MÚC NƯỚC (khách yêu cầu 21/06)** — ĐÃ LÀM:
  - `WaterSource.cs` (component đánh dấu vùng ao múc được). Item `watering_water_01` "Nước tưới". Mũi chân/điểm trước chân đến gần ao + bấm **"Múc nước"** → +10 xô/lần (`amountPerScoop`). Tưới cây **TỐN 1 xô**; hết → toast "Ra ao múc nước". KHÔNG animation (khách không cần). `FarmInteractionController` (nhận diện WaterSource + ScoopWater + HandleWater trừ nước). Loadout test có sẵn 30 xô.
  - **CẦN Editor**: gắn 1 Collider(IsTrigger) + `WaterSource` lên bề mặt ao giữa đảo. KHÔNG gắn lên nước biển.
- `[x]` **#2 Tách tab túi đồ**: sản phẩm (trứng/sữa/thịt + SP cây lâu năm) tách khỏi tab "Thú nuôi" → đổi tab "Đặc biệt" thành **"Sản phẩm"** (category `products`). Live animals giữ tab "Thú nuôi". Sửa ItemDataGenerator (category) + InventoryPopup.uxml + Controller.
- `[x]` **#1 Ẩn tab filter shop không liên quan**: `ShopPopupController.UpdateFilterVisibility()` — chỉ hiện filter (Seeds/Animals/Tools/Items) có hàng trong shop đó; còn lại ẩn, giữ "Tất cả".
  - **CẦN Editor**: chạy lại `Generate Mock Items` (item nước + đổi category sản phẩm) + `Generate Shop Data`.
  - ⚠️ Lưu ý phụ: live-animal item `duck_01`/`goose_01`/`turtle_01` chưa có ItemDefinition (chỉ 7/10 con mua được ở shop) — bổ sung sau nếu cần bán 3 con này.
- `[~]` **CẦN Editor/test**: thả thú thử → chỉnh `statusBarHeight` nếu thanh lệch đầu; xác nhận prefab thú có Collider (nếu chưa, FarmAnimal tự thêm BoxCollider tạm). Đảm bảo đã chạy `Generate Animal Data` để có `produceCycleTimeSec`/`feedIntervalSec`/`maxHarvests` thật.

## 🔍 RÀ SOÁT TRƯỚC DEMO (21/06 — anh review) — đối chiếu task/lộ trình
> 13 điểm anh nêu khi chơi thử. Phần lớn TRÙNG; ➕ = GAP mới chưa có ở task/lộ trình.
- `[x]` **#11 Khoá map** (chỉ Nông trại + Thành phố) — `IslandTravelManager` gate. **(21/06 bổ sung)** Popup Map: thêm `LockMine` (icon 🔒) cho đảo **Mỏ** + `IsUnlocked("mine")=false` cứng (scene chưa có). Đổi thông báo map khóa (dialog + toast) → **"Chưa đủ điều kiện để di chuyển."** (MapPopupController + IslandTravelManager). MapPopup KHÔNG thuộc QC.
- `[ ]` ➕ **#1 Thành phố thiếu biển**: biển nằm `farmOnlyObjects` → ẩn ở city. CityScene cần **water plane RIÊNG + FishingSpot** (Editor).
- `[~]` **#2 NPC thành phố chưa đủ popup**: chỉ NPC shop mua/bán chạy. VIP/Maid/Pet/Game/Gift/Heo Đất = Phase 2 (xem mục "HỆ NPC").
- `[~]` **#3/#4 Lưu/load**: CÓ lưu local (POS/túi/ô đất/thú qua PlayerPrefs, lúc Quit/Pause). **(21/06) THÊM luồng RESUME người chơi cũ**: `GameManager` — có save → **bỏ Login+Cutscene, vào thẳng game**; lưu + thả lại **đúng vị trí** lúc thoát (chỉ lưu toạ độ khi ở Nông trại để resume an toàn). Cờ `alwaysStartFresh` (test mở đầu) + ContextMenu "Clear Save". *(GameManager là file protected — sửa theo yêu cầu anh, báo rõ.)* CÒN NỢ: (a) persistence DateTime cho offline lớn-bù (cây/thú lớn khi đóng app); (b) TẮT `giveTestLoadoutOnStart` khi build thật; (c) resume luôn về Nông trại (nếu thoát ở City thì về farm) — chấp nhận cho demo.
- `[~]` **#5/#8 Loop + công thức**: vòng lặp lõi chạy đúng; data khớp VatNuoi/CayTrong; **đã có EXP/Level**. Còn phần chốt kinh tế cuối (giá bán + anti-exploit + đồng bộ web).
- `[x]` **#6 Xây chuồng tốn GỖ + #7 Ruộng FREE + Build mode dùng VẬT LIỆU (không POS)**: `BuildModeOverlayController` đổi item sang `materialId`+amount (Ruộng=miễn phí · Đường đá=1 Đá · Chuồng=1 Gỗ/ô rào); menu hiện chi phí vật liệu + số gỗ/đá đang có. `GhostPlacementController` KIỂM + TRỪ vật liệu lúc đặt (thiếu → toast, không đặt). `BuildSurfaceCell` lưu `BuildMaterialId`+amount. Phá chuồng (`DemolishEnclosure`) HOÀN đúng vật liệu (đầy đủ) thay vì POS. Loadout test có 30 gỗ + 30 đá.
- `[x]` ➕ **#9 Tái sinh tài nguyên**: `HarvestableResource.respawnTimeSec` thành SerializeField gọn (Header "Tái sinh" + tooltip), default đổi 3600→**60s** (demo). Cây + đá dùng chung → cả hai mọc lại. **CẦN Editor**: set `respawnTimeSec` trên prefab/đối tượng cây+đá CŨ (chọn nhiều → sửa 1 lần), vì chúng đã lưu 3600.
- `[x]` ➕ **Build cost ra SerializeField**: `BuildModeOverlayController` thêm `penWoodCost`/`pathStoneCost` (Inspector) — đổi số không cần sửa code. Ruộng free.
- `[x]` ➕ **#10 Popup "Tính năng đang phát triển"** cho NPC chưa dùng được — ĐÃ CÓ (`ShopZoneTrigger.comingSoon` → `ScreenToast.ShowInfo` "🚧 ...đang phát triển").
- `[x]` ➕ **EXP/level system + HUD số (tối giản)** — `ExperienceManager` (singleton tự tạo, lưu PlayerPrefs, level ramp 100+(lv-1)*50, bắn `LevelUpOverlay`). Nối HUD (`GameHUDController` hiện Level + % EXP thật, bỏ "Level 1/0.00" cứng; sửa `SyncPlayerName` ép level 1). Cộng EXP: thu hoạch cây (`crop.expReward`) + chặt/đào (`resourceExp`=5, SerializeField). *(GameHUD module QC — sửa theo yêu cầu.)*
- `[x]` ➕ **Âm thanh/nhạc nền (khung tối giản)** — `AudioManager` (singleton tự tạo, tải clip `Resources/Audio/<tên>`, thiếu file bỏ qua êm). Nối: nhạc nền `bgm` (HUD OnEnable) · `chop` (chặt/đào) · `harvest` (thu hoạch) · `coin` (mua/bán). **CẦN: thả file audio vào `Assets/Resources/Audio/` (xem README_AUDIO.txt).** Volume có `SetMusicVolume/SetSFXVolume` — chưa nối slider Settings (2 TODO).
- `[x]` ➕ **Ẩn nút CHEAT (Tăng Level/VIP) trong Map** — cờ `showCheatButtons=false` ẩn `.map-cheat-bar`; bật lại để dev test.
- `[x]` ➕ **Tutorial CHỐNG KẸT** — `CheckFollowAutoAdvance`: bước "Đi theo NPC" quá 90s không tới → tự gọi handler tới nơi (tránh kẹt do NPC ngoài NavMesh). Chỉ áp bước đi-theo. *(TutorialManager module QC — sửa theo yêu cầu.)*
- `[ ]` **#12 Dọn data mock + tích hợp web** — Phase 2 (đã biết, nhiệm vụ lớn).

### Nhiệm vụ 20/06 (ưu tiên mới)
- `[x]` **Xây mặt đường đá (paving)**: item "Đường đá" trong menu Build → map `BuildPrefabLibrary` (nameContains "đường đá" → StoneSlab, stretch ON). Snap theo `BuildSurfaceCell`. *(Cần điền entry trong Editor.)*
- `[x]` **Dọn menu Build còn 3 mục**: 1 tab "Xây dựng" = Ruộng / Đường đá / Chuồng (rào xịn); ẩn 4 tab cũ. *(Cần BuildPrefabLibrary có 3 entry: ruộng→Dirt, đường đá→StoneSlab, chuồng→Fence (stretch Fence OFF).)*
- `[x]` **Fix ghost luôn báo đỏ**: GhostPlacementController đổi `Physics.Raycast` → `RaycastAll` + tìm `BuildSurfaceCell` gần nhất (bỏ qua collider nền/mesh đảo chắn trước).
- `[x]` **Loadout test (nhiều thức ăn + tiền)**: `InventoryManager.GiveTestLoadout()` + cờ `giveTestLoadoutOnStart` — nạp nông sản/sản phẩm/vật liệu/hạt + 100k POS để test NPC mua/bán.
- `[x]` **AnimalManager.LookupDefinition (chắc ăn)**: tra def qua Instance, fallback load thẳng Resources → info/validate chạy kể cả khi scene chưa gắn AnimalManager.
- `[x]` **Validate ô chuồng + cho thả NHIỀU con nếu đủ ô**: đã làm — `AvailableCount` (ô-rào chưa có thú) ≥ `penSlots` thì cho thả, đánh dấu `SetAnimal`; chuồng 9 ô thả được 9 gà (mỗi con 1 ô); chuồng còn 8 ô **KHÔNG** nhét được bò (9 ô) → báo lỗi. *(Còn TODO: nếu muốn giới hạn LOÀI theo cỡ chuồng thì bổ sung sau.)*
- `[x]` **Hiển thị thông tin con vật ở 3 nơi** (Tên/giá/thức ăn chính-phụ/số ô đất):
  - `[x]` Khi **xem thông tin** (popup AnimalInteractionPopup).
  - `[x]` Khi **mua** (Shop popup) — chèn "Thông tin nuôi" vào mô tả khi chọn con giống (`ShopPopupController.AnimalInfoText`).
  - `[x]` Khi **chọn trong túi đồ** (Inventory) — chèn vào mô tả khi chọn con vật (`InventoryPopupController.AnimalInfoText`).
  - *(Chèn vào mô tả nên không cần sửa UXML; nguồn data = `AnimalDefinition` — cần chạy `Generate Animal Data`.)*

---

## 👥 HỆ NPC (theo kịch bản "10+ NPC") — 20/06
> Nguồn: `Docs_KichBan/YWONDERLAND_KichBan3D_ChiTiet.md` + `DanhSachCuaHang_Game3D.md`.
> ĐÃ CÓ: **NPC Hướng dẫn** (tutorial, `GuideNPC`) + **1 Merchant NPC mẫu** (kiểu Hai Lúa). Còn lại chưa làm:

- `[~]` **Hệ Shop Keeper đa-NPC (data-driven)** — ĐÃ CODE, chờ setup Editor + test:
  - Cơ chế MỚI (sếp): **chạm NHÀ NPC → popup tự mở** (không click NPC). Tự mở · giữ mở khi đi ra · đóng bằng X. Thiết kế: `Docs_KichBan/ThietKe_NPCShop.md`.
  - File: `ShopDefinition.cs` (SO, mỗi shop 1 asset, chỉ lưu ID — giá tra ItemDatabase), `ShopZoneTrigger.cs` (gắn nhà NPC, học MapPortalTrigger), `ShopPopupController.Show(ShopDefinition)` + lọc thu mua theo whitelist, `MerchantNPC` thêm field shopData (click vẫn chạy), `Editor/ShopDataGenerator.cs` (menu `YWonderLand ▸ Generate Shop Data` sinh 6 asset).
  - **CẦN Editor**: chạy `Generate Shop Data` → 6 asset trong `Data/Shops/`; mỗi nhà NPC thêm BoxCollider(IsTrigger) + `ShopZoneTrigger` + kéo asset. Player có tag Player + CharacterController.
  - Nhóm VIP/Maid/Pet/Game/Cosmetic/Gift = feature riêng, làm sau. **CHỜ KHÁCH**: chốt giá con giống (3 nguồn lệch); duck/goose/turtle chưa có trong ItemDatabase (sẽ cảnh báo nếu thêm vào buy list).
  - Nông trại/Đảo: **Farm Shop** (hạt giống + con giống), **Item Shop** (phân/thuốc/vắc-xin), **Fish Shop** (mua cá / bán mồi).
  - Thành phố (~12 quầy): Bán cá, Workshop (nâng cấp dụng cụ), Verdant/YWonderLand, Mini Garden (mua nông sản), Hai Lúa, KNX (thẻ VIP), Maid Service, Pet Shop, Game Center, Store (thời trang), Gift Post, Heo Đất (tiết kiệm).
- `[ ]` **Maid (Hầu gái VIP)**: NPC nữ follow player ở nông trại, **tự tưới/thu hoạch** (đặc quyền VIP). Thuê tại Maid Service. Anim: Idle/Walk/Water/Harvest/Bow.
- `[ ]` **Pet (companion)**: mua ở Pet Shop → chạy theo chân (NavMesh, cách 1–2m), đứng yên→Sit, tap→Happy. Chỉ trang trí (không tham gia gameplay).
- `[ ]` **NPC khu Mỏ** (`MineScene`, mở ở Lv10): NPC **bán vé đào mỏ** + NPC **mua quặng**.
- `[ ]` **NPC Câu cá**: quầy bắt đầu câu / bán mồi (hiện chỉ có `FishingSpot` vùng nước, chưa có NPC quầy).
- `[ ]` **AI Chat NPC** (P2 — nice to have): pool câu trả lời theo từ khóa, tự nhắn vào khung chat làm sôi động khi ít người.

---

## 📱 RÀ SOÁT MOBILE (19/06) — game hướng thị trường điện thoại
> Rà soát phát hiện UI/điều khiển đang là PC-first. Sửa theo độ ưu tiên dưới.

- `[x]` **#1 Joystick ảo điều khiển di chuyển**: nối `Joystick` (GameHUD.uxml) vào `PlayerController.SetMoveInput()` qua kéo pointer (GameHUDController) + thêm style núm `.joystick-inner`. Gộp bàn phím + joystick trong PlayerController. **Cần test trên Editor/thiết bị.**
- `[x]` **#1b Nút Sprint giữ-để-chạy-nhanh (cảm ứng)**: PC bấm Shift đã chạy; sửa nút Sprint trên HUD dùng pointer capture (bỏ `PointerOutEvent` gây hủy sớm) → giữ nút là tăng tốc. Trên phone: 1 ngón giữ Sprint + 1 ngón joystick. **Cần test.**
- `[x]` **#2 Nút Jump + Hủy hoạt ảnh (X)**: Jump → `PlayerController.TriggerJump()` (1-bấm-1-nhảy). **Bỏ nút bàn tay (Interact)** — tương tác qua các **nút gợi ý nổi** quanh tâm ngắm (đã bấm được, dùng chung `ShowInteractionPrompts`). Giữ **nút X** = `PlayerController.CancelAction()` (ngắt hoạt ảnh, cất đồ, ẩn thanh tiến trình), tự hiện khi `IsBusy`. **Ngắm theo TÂM màn hình** (ổn định, không giật; gợi ý đứng yên khi rê chuột tới bấm). **Nút gợi ý "Chặt cây" bấm/tap được** (mỗi lần = 1 nhát `ClickHarvestResource`); căn giữa dưới tâm, co theo nội dung để không chặn dải ngang. Giữ chuột ở tâm vẫn chặt liên tục như cũ.
- `[x]` **#3 Tương tác đổi `Mouse.current` → `Pointer.current`**: `FarmInteractionController` giờ dùng `Pointer.current` + `pointer.press` (chung Mouse PC + Touchscreen mobile) → chạm tay chặt cây/trồng/mua chạy. PC vẫn chạy. *(Còn TODO multitouch: kéo joystick 1 ngón + tap ngón khác có thể kích hoạt hành động ở tâm — hiện chặn bằng `IsPointerOverGameObject` + `UIPopupTracker`; tinh chỉnh sau nếu lỗi.)*
- `[x]` **#4 Camera xoay bằng kéo 1 ngón** (vùng phải màn hình): thêm `LookZone` (nửa phải, con đầu của hud-root để nút đè lên) → `GameHUDController` bắt PointerDown/Move/Up (giống joystick) → `ThirdPersonCamera.AddTouchLook(delta)` cộng thẳng vào yaw/pitch (sensitivity riêng `touchHorizontalSensitivity`/`touchVerticalSensitivity`). PC khóa con trỏ ở tâm nên LookZone chỉ nhận chạm mobile (không đụng PC). Kéo-nhìn KHÔNG lỡ chặt cây (dòng 122 `IsPointerOverGameObject` chặn). `ThirdPersonCamera` thêm `Instance`.
- `[~]` **#5 Safe Area + khóa Landscape + Match**: **Match đã = 0.5** (cân ngang-dọc, ScaleMode=Scale With Screen Size) — OK sẵn. Viết `UISafeArea.cs` (đệm root UIDocument theo `Screen.safeArea`, tự cập nhật khi xoay máy; cờ `applyInEditor` để giả lập). **CẦN Editor:** (1) gắn `UISafeArea` vào GameObject có UIDocument GameHUD (và popup khác nếu muốn); (2) Player Settings ▸ Resolution ▸ khóa **Landscape**.

---

## Vấn đề còn tồn đọng (Pending Issues) — ⏳ ƯU TIÊN THẤP
> Làm SAU khi xong nhóm 19/06. Phần lớn liên quan **polish UI 2.5D + asset/artist** nên còn chờ tài nguyên.
> Đánh giá nhanh: Login & Character Select **ĐÃ XONG**; các mục còn lại chủ yếu **chờ ảnh AI / artist** hoặc là việc polish dài hơi.

- `[x]` **Login UI & Validation:** Thay thế dòng chữ Y WONDER GREEN FARM bằng logo Y Wonder Hub. Cập nhật UI và Logic để validate các trường đăng nhập, đăng ký tối đa 20 ký tự, đúng định dạng.
- `[x]` **Character Select UI & Logic:** Thay thế chữ M/F bằng Avatar ảnh (Nam/Nữ) tương ứng. Đặt ảnh vừa chọn làm avatar mặc định. Validate đặt tên nhân vật tối đa 20 ký tự.
- `[ ]` **Sửa lỗi Layout Popup cũ:** Cập nhật thêm các popup khác (Inventory, Friends, Map...) theo chuẩn Flat Graphics (Dark Theme) và sửa các lỗi chồng chéo layout nếu có.
- `[ ]` **Giao diện Popup:** Các icon 2.5D trong các Popup hiện tại (Cửa hàng, Kho đồ) chưa đúng phong cách mong muốn. Đang chờ ảnh mới từ Unity Muse/AI.
- `[ ]` **3D Model/Rigging:** Cần áp dụng quy trình Blender xuất FBX Humanoid mới cho các NPC/Nhân vật sắp tới để tránh lỗi mapping xương (vàng/đỏ) trong Unity.
- `[ ]` **Thống nhất Visual:** Cần quyết định cụ thể xem tiêu đề (Title) các Popup có nên dùng icon hay bỏ đi để không bị lạm dụng icon gây rối mắt.

---

## Bước tiếp theo (Next Steps) — ⏳ ƯU TIÊN THẤP (chờ asset/artist)
- `[ ]` **Sản xuất Asset:** Chạy AI với bộ Prompt đã tạo để sinh ra bộ vật phẩm 2.5D mới (Cà chua, Hạt giống, Rìu, Cuốc, Khoáng sản...).
- `[ ]` **Tích hợp UI Popup:** Đưa các sprite 2.5D mới vào Unity, xử lý tách nền và gắn vào các slot chứa đồ trong `InventoryPopup.uxml` và `ShopPopup.uxml`.
- `[ ]` **Kiểm thử Rigging FBX:** Import thử một file FBX nhân vật mới do bạn Artist làm theo workflow chuẩn để kiểm tra thẻ Rig Humanoid trong Unity.

## Update 2026-07-01: login profile cache isolation

- `[x]` Hotfix login nhieu tai khoan demo tren cung may: `PlayerProfileService` tach cache theo `AuthService.UserId/Username`, reset runtime profile khi doi tai khoan, va nhan `player_profile` tu `/auth/web-login` neu backend tra ve de tranh Demo05 bi dinh profile Demo02.
- `[x]` Hotfix HUD profile goc trai con giu ten cu: `GameHUDController` refresh ten tu active session, uu tien `GameManager.playerName` de khop voi name tag tren dau nhan vat.

## Cap nhat 05/07/2026: dao da 10 luot/ngay + Gem Shop catalog day du

- `[x]` Cau ca da co gioi han 10 luot/ngay tu truoc trong `FishingOverlayController` (`dailyTurns = 10`, `FishingLastDate`, `FishingFreeTurns`).
- `[x]` Dao da da co gioi han 10 luot/ngay theo ngay that. Khi het luot, nguoi choi khong bat dau dao tiep duoc; khi dao thanh cong, he thong tru 1 luot va toast hien so luot con lai.
- `[x]` Gem Shop sell mode hien du toan bo da quy trong whitelist `Shop_GemShop`, ke ca da nguoi choi dang co 0 vien.
- `[x]` Gem Shop hien so luong dang so huu tren card/detail va disable ban neu so luong bang 0.
- `[ ]` Test Unity: mo Gem Shop -> tab ban phai thay du Kyanite, Orange Calcite, Green Calcite, Fire Quartz, Amethyst, Ruby; da nao 0 vien van hien nhung khong ban duoc.
- `[ ]` Test Unity: dao da 10 lan trong ngay -> lan thu 11 bi chan bang toast het luot; doi ngay that hoac clear PlayerPrefs thi luot reset ve 10.
