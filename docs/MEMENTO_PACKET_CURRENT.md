# Memento Packet Hiện Hành — Y WONDER GREEN FARM

**Packet ID:** `ywgf-2026-07-29-memento-v2`
**Tạo lúc:** 2026-07-29 (Asia/Ho_Chi_Minh)
**Thay thế:** `ywgf-2026-07-20-memento-v1` (SUPERSEDED — packet v1 đứng ở HEAD `e9282903`, tranche "source-lot
PostgreSQL + hoa hồng/VIP"; từ 22/07 dự án đã chuyển hẳn sang gameplay/client nên v1 mô tả sai việc đang làm).
**Loại:** Snapshot bàn giao để bắt đầu phiên AI mới; KHÔNG phải bằng chứng production thay cho kiểm tra live.
**Giao thức:** đọc `docs/THE_MEMENTO_PROTOCOL.md` trước khi đưa packet này vào hành động.

---

## 1. Identity và mục tiêu đang làm

- **Project/workspace:** `D:\LamGameUnity\BaChuKhuRung3D` — Y WONDER GREEN FARM.
- **Git baseline đã kiểm 29/07:** `codex/backend-mvp` ở `b79fb098`, **đã push, khớp `origin`**.
  Nhánh làm việc MỚI: **`feat/gameplay-followups`** (tách từ `b79fb098`, đã push, có upstream).
  Phiên mới vẫn phải tự chạy `git status --short --branch` và `git log --oneline -n 8` trước khi sửa.
- **Tranche hiện tại:** hoàn thiện GAMEPLAY/CLIENT theo phản hồi khách + đưa các endpoint server-authoritative
  mới (câu cá / vòng quay / vé đào) lên production. Ví Point **không phải** việc đang làm; nó đứng yên ở
  trạng thái dormant từ 20/07.
- **Mục tiêu kinh doanh lớn (không đổi):** Point web và Point game là một số dư chung; PostgreSQL game là
  ledger Point authoritative. Không mở tiền thật, debit, link/migrate legacy hay deploy chỉ dựa vào packet này.
- **Người quyết định:** anh Nhieenn (chủ dự án) — tự kiêm QC, tự build Unity, tự deploy VPS. AI KHÔNG build,
  KHÔNG SSH, KHÔNG deploy thay.

## 2. Bắt buộc đọc và cách kiểm chứng

1. `RULES.md` (có canary xưng hô: AI xưng **"bé"**, gọi user **"anh yêu"**).
2. `docs/THE_MEMENTO_PROTOCOL.md`
3. `docs/MEMENTO_PACKET_CURRENT.md` (file này)
4. `docs/CONTEXT_RECOVERY.md` (snapshot hiện hành; root `CONTEXT_RECOVERY.md` chỉ là nhật ký cũ)
5. `CHANGELOG.md` — **hiện là nguồn chi tiết NHẤT cho 22–29/07**, mới hơn `task.md`.
6. `task.md` (backlog; phần ưu tiên đã được cập nhật 29/07)
7. Khi đụng backend/tiền: `docs/API_CONTRACTS.md`, `docs/DB_SCHEMA.md`, `docs/SECURITY.md`,
   `docs/ADR_POINT_WALLET_AUTHORITY.md`, `docs/POINT_WALLET_BUSINESS_RULES.md`.
8. Khi đụng số liệu kinh tế/cây/thú: `Assets/_Project/Docs_KichBan/` (⚠️ tên file Excel bị tráo:
   `CayTrong*` = lâu năm, `CayTrongLauNam*` = ngắn ngày).

**Nguồn sự thật:** bằng chứng production mới nhất > code/schema/test đang chạy > quyết định khách mới nhất đã
ghi tài liệu > recovery/task/changelog > chat cũ. Chưa đủ bằng chứng thì gắn `UNKNOWN`, không suy đoán.

## 3. Ràng buộc an toàn và ownership worktree

### Tuyệt đối không làm

- Không `git reset --hard`, `git checkout --`, `git clean`, `git stash -u`, revert hàng loạt, xoá asset hàng loạt
  (đã mất asset thật 2 lần vì stash).
- Không sửa/xoá `.meta` của Unity. Được phép COMMIT `.meta` mới do Unity sinh cho file AI vừa tạo.
- Không đưa password, SSH key, token, HMAC, email cá nhân, ID người chơi vào repo/log/packet/commit.
- Không SSH/restart/deploy/đổi env VPS; không bật top-up/debit; không link/migrate balance; không dùng tiền thật.
- ⚠️ Mật khẩu root VPS từng lộ cleartext trong log Codex, **chưa xác nhận đã rotate**.

### Worktree bẩn — của chủ dự án, không gom vào commit (trạng thái 29/07)

- `Assets/_Project/_Scenes/CityScene.unity`, `FarmScene.unity`
- `Assets/_Project/Settings/Mobile_RPAsset.asset`, `Assets/_Project/Editor/TextureSizeReducer.cs`
- `.meta` của texture (`Assets/Building/**`), `Assets/ThirdParty/TextMesh Pro/**`
- Chưa track: `Assets/Building/NEWMap/SuaNenThanhPho/`, `Assets/_Recovery/0 (8).unity`, `0 (9).unity`,
  `server/RUNBOOK_deploy_fishing.md`

## 4. Kiến trúc / authority — điều dễ hiểu sai

| Trạng thái | Ai là authority | Ghi chú |
|---|---|---|
| Ví Point | PostgreSQL game `player_economy.pos` | Web linked account chỉ đọc; debit vẫn TẮT |
| Túi đồ / kinh tế | Server (có hàng đợi delta lưu đĩa, idempotent) | Delta dương bị siết, không cho client tự khai thưởng |
| Câu cá / vòng quay / vé đào | **Server** (endpoint mới, 24/07) | Client offline giữ local |
| Thả thú vào chuồng | **Server** `/player/farm/animals/place` (atomic) | Trừ túi + append animal trong 1 transaction |
| Nông trại (ô đất, cây, công trình, **chuồng**) | **Client khai — server chỉ chép** | `PUT /player/farm-state` lưu nguyên `build_state_json`, chỉ chống ghi đè bằng `expected_version` (409). KHÔNG validate nội dung |
| Realtime | Chỉ tài nguyên dùng chung (cây/đá) + quản phiên | `realtimeServer.js` KHÔNG biết chuồng/công trình |

## 5. Trạng thái theo nhãn

### VERIFIED (có bằng chứng, ngày kèm theo)

- **Punch-list khách 29/07 (T1–T7) đã code xong** — commit `3db7ac8a` + `b79fb098`. Biên dịch
  `Assembly-CSharp` bằng Roslyn của Unity 6000.3.15f1 exit `0`. Chi tiết từng task ở `CHANGELOG.md`.
  **Chưa nghiệm thu runtime.**
- **Lỗi mất gỗ đã tìm ra gốc:** `BuildModeOverlayController.PickUpBuilding` gọi `BuildSurfaceCell.ClearOccupant`
  (hàm này xoá luôn `BuildMaterialId/BuildCost` của ô) rồi đặt lại với vật liệu rỗng → công trình đã DỜI một
  lần thì hủy hoàn 0. Đã vá bằng `GhostPlacementController.ActivateCarried`. Ba đường hủy còn lại hoàn đúng
  từ trước.
- **22–29/07 đã làm (xem `CHANGELOG.md` + `git log`):** hệ bệnh/vắc-xin vật nuôi theo `VatNuoi2`; đổi
  `SecondsPerGameDay` 60 → 86400 (1 ngày game = 1 ngày thật); câu cá/vòng quay/vé đào server-authoritative;
  account mới = 0 Point; hàng đợi delta lưu đĩa; tab "Đồ dùng" trong túi; loạt fix mobile/HUD (joystick, cursor
  lock, `InteractionContainer` nuốt click); login chỉ qua web + account trình diễn R1–R5.

### DECIDED (khách/BA đã chốt)

- 1 ngày thật = 1 ngày game. Account mới bắt đầu 0 Point. Câu cá: 10 lượt free/ngày, hết thì tốn mồi, trừ mồi
  khi HOÀN THÀNH animation, mồi không buff cá hiếm. Giá mồi/vé đào 2, vé vòng quay 5.
- Tỷ giá `1 USDT = 26,5 Point`, `1 YWH = 1,59 Point`; hoa hồng 6 cấp `8% + 5×1%`; VIP cộng dồn từ
  `2.650 Point` nguồn USDT. (Contract chi tiết ở `docs/POINT_WALLET_BUSINESS_RULES.md`.)
- Đổi tên đảo: Hải Phú → **Nam Du**, Mộc Nhi → **Phú Quốc** (chỉ tên hiển thị, giữ `id`/`sceneName`).
- Số bệnh của dê/ngỗng trông như gõ nhầm nhưng là ý khách — **ĐỪNG tự sửa**.

### IN_PROGRESS

- **Deploy server câu cá + giá vé + economy 0 Point** — code đã push từ 24/07, runbook ở
  `server/RUNBOOK_deploy_fishing.md`. **Chưa có bằng chứng đã deploy.** Nếu ship client đăng-nhập trước khi
  deploy thì `/player/fishing/catch` trả 404. Chủ dự án tự chạy; AI không deploy.
- **Nghiệm thu runtime punch-list T1–T7** — ma trận test nằm cuối mục 29/07 trong `CHANGELOG.md`.
- **P0 thả thú `7 → 6` trên artifact thật** — code atomic đã có từ 16/07, chờ build mới nghiệm thu.
- **Retest hotfix gameplay** (hủy cho ăn hoàn thức ăn; cây/thú chết ngay ở 0%) — source đã re-verify 21/07,
  chỉ thiếu runtime trên build mới.

### UNKNOWN / BLOCKED — phải verify trước khi hành động

- **Trạng thái thật của production sau 20/07: KHÔNG BIẾT.** Mọi số liệu ví Point trong `docs/CONTEXT_RECOVERY.md`
  là ảnh chụp tới 20/07: authority v3 deploy **dormant**, `WEB_POINT_WALLET_DEBIT_ENABLED=false`, top-up
  `mode=canary` đúng 1 identity QA, public callback `404`, canary kỹ thuật không tiền đã pass (`5000 → 5053`).
  Trước bất kỳ thao tác remote nào phải chạy preflight read-only theo `docs/SECURITY.md`.
- **Gate PostgreSQL source-lot** vẫn blocked: máy local không có Docker/psql/WSL nên chưa chạy
  `npm.cmd run test:postgres --prefix server`. Migration `007` chưa apply.
- **Rủi ro mới (29/07):** dời chuồng ghi vào `build_state_json`; nếu upload snapshot fail (mất mạng hoặc `409`)
  thì client đã dời mà server giữ chuồng cũ → thả thú lần sau có thể bị từ chối, hoặc relogin chuồng nhảy về
  chỗ cũ. Chưa test online.
- Mật khẩu root VPS đã rotate chưa: **chưa xác nhận**.

## 6. Cách tự kiểm chứng (lệnh an toàn)

| Mục đích | Lệnh | Kỳ vọng |
|---|---|---|
| Git baseline | `git status --short --branch`, `git log --oneline -n 8` | branch `feat/gameplay-followups`, HEAD `b79fb098` trở đi |
| **Compile C# KHÔNG cần mở Unity** | Roslyn của Unity: `D:\Du_lieu_Unity\6000.3.15f1\Editor\Data\NetCoreRuntime\dotnet.exe` chạy `...\DotNetSdkRoslyn\csc.dll` với response file | `csc exit code: 0` |
| Test server (JSON store) | `npm.cmd run test:point-source-ledger --prefix server` | pass |
| Gate PostgreSQL | `npm.cmd run test:postgres --prefix server` | **chạy không được trên máy này** — thiếu runtime |
| Sức khoẻ prod (read-only) | `irm https://api.ywonder.net/game-api/health` | xem `server/RUNBOOK_deploy_fishing.md` |

**Công thức compile-check:** gom mọi `.cs` trong `Assets/` trừ thư mục `Editor` và thư mục có `.asmdef`
(~123 file); tham chiếu `Editor\Data\Managed\UnityEngine\*.dll` + `Library\ScriptAssemblies\*.dll` (bỏ
`Assembly-CSharp*`) + `Newtonsoft.Json.dll` trong `Library\PackageCache` + `netstandard.dll`. Đây là cách
phiên 29/07 kiểm 4 lượt sửa mà không cần chủ dự án mở Editor.

## 7. Bước tiếp theo, đúng thứ tự

1. **Deploy server câu cá/vé/economy** theo `server/RUNBOOK_deploy_fishing.md` — chủ dự án tự chạy trên VPS.
   Gate đạt: `/player/fishing/catch` không còn 404, health `200`, account demo câu được cá.
2. **Build EXE/APK mới** rồi nghiệm thu punch-list T1–T7 + P0 thả thú `7 → 6` + hotfix cho ăn / cây chết 0%.
   Gate đạt: đủ ma trận trong `CHANGELOG.md`; fail thì ghi lại log và số liệu thật, không sửa mù.
3. Xử lý rủi ro snapshot khi dời chuồng (cho `Confirm()` chờ flush xong mới báo thành công) — chỉ làm nếu
   bước 2 thấy chuồng nhảy về chỗ cũ sau relogin.
4. Ví Point (canary tiền thật, migration số dư cũ, hoa hồng/VIP): **chỉ khi có phê duyệt riêng**, kèm backup
   và rollback. Không tự mở.

## 8. Prompt khởi động cho phiên AI mới

```text
Tiếp tục Y WONDER GREEN FARM tại D:\LamGameUnity\BaChuKhuRung3D theo The Memento Protocol.
Xưng "bé", gọi tôi là "anh yêu".

Trước khi thay đổi bất kỳ thứ gì:
1. Đọc RULES.md, docs/THE_MEMENTO_PROTOCOL.md, docs/MEMENTO_PACKET_CURRENT.md,
   docs/CONTEXT_RECOVERY.md, CHANGELOG.md, task.md theo thứ tự.
2. Chạy git status --short --branch và git log --oneline -n 8; báo cáo branch, HEAD,
   worktree và ownership trước khi sửa.
3. Không reset/clean/checkout/revert hàng loạt. Không sửa/xoá .meta. Không đưa secret vào repo.
   Không đụng scene/Map2.1/HDRI/animation của tôi nếu không được giao rõ ràng.
4. Phân biệt VERIFIED / DECIDED / IN_PROGRESS / UNKNOWN. Không tự deploy, không SSH,
   không bật wallet/debit/top-up, không dùng tiền thật.

Nhánh làm việc: feat/gameplay-followups.
Mục tiêu trước mắt: [điền task].
Báo plan ngắn gọn rồi làm tới khi có bằng chứng nghiệm thu.
```

## 9. Điều kiện làm mới packet

Cập nhật ngay sau bất kỳ sự kiện nào: commit/merge/**deploy**/rollback, build EXE/APK được nghiệm thu,
migration/schema đổi, quyết định khách mới, lỗi P0 mới, đổi ownership worktree, canary/giao dịch, hoặc trước
khi kết thúc một phiên AI dài. Ghi ngày giờ + đường dẫn bằng chứng hoặc lệnh read-only lặp lại được; chưa
xác minh thì để `UNKNOWN`. Không copy secret/token/password/email/raw log nhạy cảm vào packet.
