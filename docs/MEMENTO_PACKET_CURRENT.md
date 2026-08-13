# Memento Packet Hiện Hành — Y WONDER GREEN FARM

**Packet ID:** `ywgf-2026-08-13-memento-v3`
**Tạo lúc:** 2026-08-13 19:52 (Asia/Ho_Chi_Minh)
**Thay thế:** `ywgf-2026-07-29-memento-v2` (SUPERSEDED — v2 đứng ở `b79fb098` nhánh `feat/gameplay-followups`,
tranche "nghiệm thu punch-list T1–T7"; toàn bộ việc đó đã xong và đã gộp `main`, nên v2 mô tả sai việc đang làm).
**Loại:** Snapshot bàn giao để bắt đầu phiên AI mới; KHÔNG phải bằng chứng production thay cho kiểm tra live.
**Giao thức:** đọc `docs/THE_MEMENTO_PROTOCOL.md` trước khi đưa packet này vào hành động.

---

## 1. Identity và mục tiêu đang làm

- **Project/workspace:** `D:\LamGameUnity\BaChuKhuRung3D` — Y WONDER GREEN FARM.
- **Git baseline đã kiểm 13/08:** nhánh **`main`** ở **`9973918f`**, đã push, khớp `origin/main`.
  `feat/gameplay-followups` (`e4f78748`) **đã gộp hết vào `main`** ở merge `eeb970c0` — không còn commit lẻ.
  Phiên mới vẫn phải tự chạy `git status --short --branch` và `git log --oneline -n 8` trước khi sửa.
- **Tranche hiện tại:** sửa gameplay/client theo phản hồi khách theo từng đợt, build và giao bản mới.
  Ví Point **không phải** việc đang làm; nó đứng yên ở trạng thái dormant từ 20/07.
- **Mục tiêu kinh doanh lớn (không đổi):** Point web và Point game là một số dư chung; PostgreSQL game là
  ledger Point authoritative. Không mở tiền thật, debit, link/migrate legacy hay deploy chỉ dựa vào packet này.
- **Người quyết định:** anh Nhieenn (chủ dự án) — tự kiêm QC, tự build Unity, tự deploy VPS. AI KHÔNG build,
  KHÔNG SSH, KHÔNG deploy thay.
- **Quyền thường trực (anh cho 13/08):** anh nghiệm thu xong ("test ổn rồi") thì AI **tự commit + push**
  nhánh đang đứng, không hỏi lại. Vẫn phải: chỉ gom file thuộc đúng đợt sửa, chừa nhiễu Unity tự sinh, và
  **không tự gộp `main`** (gộp `main` là quyết định riêng, phải hỏi).

## 2. Bắt buộc đọc và cách kiểm chứng

1. `RULES.md` (có canary xưng hô: AI xưng **"bé"**, gọi user **"anh yêu"**).
2. `docs/THE_MEMENTO_PROTOCOL.md`
3. `docs/MEMENTO_PACKET_CURRENT.md` (file này)
4. `docs/CONTEXT_RECOVERY.md` (snapshot hiện hành; root `CONTEXT_RECOVERY.md` chỉ là nhật ký cũ)
5. **`docs/CHANGELOG.md`** — nguồn chi tiết nhất và **duy nhất được ghi tiếp**; đầy đủ từ `27/05/2026`.
6. `task.md` — đọc **mục "Ưu tiên hiện tại 13/08/2026" ở đầu file**, mọi khối dưới nó là lịch sử.
7. `README.md` — tổng quan song ngữ cho người mới tiếp quản.
8. Khi đụng backend/tiền: `docs/API_CONTRACTS.md`, `docs/DB_SCHEMA.md`, `docs/SECURITY.md`,
   `docs/ADR_POINT_WALLET_AUTHORITY.md`, `docs/POINT_WALLET_BUSINESS_RULES.md`.
9. Khi đụng số liệu kinh tế/cây/thú: `Assets/_Project/Docs_KichBan/` (⚠️ tên file Excel bị tráo:
   `CayTrong*` = lâu năm, `CayTrongLauNam*` = ngắn ngày). Vật nuôi: **chốt dùng `VatNuoi2` + `SuaLai4`,
   KHÔNG áp `VatNuoi3`**.

### ⚠️ Tài liệu LỖI THỜI — không dùng để định hướng

| File | Vì sao |
|---|---|
| `CHANGELOG.md` (gốc repo) | Bản lưu trữ, dừng ở 29/07. Đã dán băng rôn. Dùng `docs/CHANGELOG.md`. |
| `Assets/_Project/Docs_KichBan/LoTrinh_Demo_Thu2.md` | Lộ trình demo tháng 6, xong từ lâu. Đã dán băng rôn. |
| Mọi doc ghi `SecondsPerGameDay = 60` | SAI — thực tế `86400` (1 ngày thật = 1 ngày game) từ 22/07. |

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

### Worktree bẩn — nhiễu Unity tự sinh, KHÔNG gom vào commit (trạng thái 13/08)

- `Assets/AddressableAssetsData/link.xml` + `.meta`
- `Assets/_Project/Settings/UniversalRenderPipelineGlobalSettings.asset`
- `Assets/ThirdParty/TextMesh Pro/**` (font fallback tự thêm glyph khi Play)
- `Assets/Resources/PerformanceTestRun*.json` (+ `.meta`) — Test Framework sinh khi Play

## 4. Kiến trúc / authority — điều dễ hiểu sai

| Trạng thái | Ai là authority | Ghi chú |
|---|---|---|
| Ví Point | PostgreSQL game `player_economy.pos` | Web linked account chỉ đọc; debit vẫn TẮT |
| Túi đồ / kinh tế | Server (có hàng đợi delta lưu đĩa, idempotent) | Delta dương bị siết, không cho client tự khai thưởng |
| Câu cá / vòng quay / vé đào | **Server** (endpoint từ 24/07) | Client offline giữ local |
| Điểm danh | **Server** (migration `009`, deploy 31/07) | `max_rewarded_day` chặn vòng lặp nhận tiền |
| Thả thú vào chuồng | **Server** `/player/farm/animals/place` (atomic) | Trừ túi + append animal trong 1 transaction |
| Nông trại (ô đất, cây, công trình, **chuồng**) | **Client khai — server chỉ chép** | `PUT /player/farm-state` lưu nguyên `build_state_json`, chỉ chống ghi đè bằng `expected_version` (409). KHÔNG validate nội dung |
| Realtime | Chỉ tài nguyên dùng chung (cây/đá) + quản phiên | `realtimeServer.js` KHÔNG biết chuồng/công trình |

### Bẫy kiến trúc hay vấp

- **Ô trồng KHÔNG phải `TilePlacementSystem`/`FarmManager`.** Ruộng là Build Mode đặt prefab Dirt lên
  `BuildSurfaceCell`; persistence bám `BuildSurfaceCell` + wall-clock `RealNow()`.
- **Bảng nút tương tác có 2 nguồn:** *foot-probe* (ô trước mặt, tự đổi theo bước chân) và *chạm thẳng*
  (dính tới khi đi xa `directTapMaxRange` = 3.5m). Ô ruộng rộng 1m nên dính = kẹt cả vạt ~7 ô — đã vá 13/08.
- **Màn múa mở TỪ POPUP** (cho ăn, tưới từ popup "Xem ruộng") thì `currentHoverObject` thường đã bị
  foot-probe xoá → phải gán `promptRestoreHint` trước khi gọi `BeginTimedAction`, không thì xong việc là
  mất bảng nút. Mới vá cho luồng cho ăn; tưới/chữa bệnh/vắc-xin **chưa vá** (chưa ai báo lỗi).
- **`OnResourceHarvested` là event dùng chung với `TutorialManager`** — một handler throw là chặn cả chuỗi.
  Toast/VFX chặt-đào phải gọi TRỰC TIẾP.
- **Singleton phải `Destroy(this)` không phải `Destroy(gameObject)`** — `SystemsBootstrapper` gắn nhiều
  manager lên một object, huỷ nhầm cả `GameManager` (đã xảy ra 23/06).

## 5. Trạng thái theo nhãn

### VERIFIED (có bằng chứng, ngày kèm theo)

- **Production sống, `2026-08-13T12:52Z`:** `GET https://api.ywonder.net/game-api/health` → `ok:true`,
  `service: ywonderland-stub`, `storage.mode = postgres`. (VPS từng DOWN 06/08, nay đã trở lại.)
- **Toàn bộ tranche tháng 8 đã gộp `main` và anh đã nghiệm thu runtime:**
  - PHA 1 cộng dồn sản phẩm vật nuôi · PHA 2 đảo luật chết (thú để XÁC, cây HÉO, giữ ô) · PHA 3 công cụ cứu
    60% giá mua + nút Cứu/Dọn trong 2 popup (khách chốt 04/08).
  - Đợt khách báo **13/08** (4 việc): ruộng hết "dính" cả vạt; mốc chết vịt `12h/24h`, hươu `48h/96h`,
    rùa `4 ngày/8 ngày`; cho ăn xong không mất bảng nút chuồng; ẩn công tắc "Chữ nổi trên cây/thú".
    Chi tiết: `docs/CHANGELOG.md` mục `[2026-08-13a]`, `[2026-08-13b]`.
  - README bàn giao song ngữ Anh-Việt ở gốc repo (khảo sát từ code thật).
- **Bản export iOS trong repo đã đủ điều kiện nộp store:** 19 icon, có `Icon-Store-1024.png`
  1024×1024 PNG colortype `2` (RGB, **không alpha**). Commit `2f3fdd79`.
- **Biên dịch không cần mở Unity:** `Assembly-CSharp` (128 file) và `Assembly-CSharp-Editor` (13 file) đều
  `csc exit code: 0` với Roslyn của Unity `6000.3.15f1` — xem công thức ở mục 6.

### DECIDED (khách/BA đã chốt)

- 1 ngày thật = 1 ngày game. Account mới bắt đầu 0 Point.
- **Mốc chết 2 tầng** (chưa cho ăn → đã cho ăn), **ĐỪNG rút thành một mốc**: thú `24h → 48h`, cây `8h → 20h`.
  Ngoại lệ khách chốt 13/08: **vịt `12h/24h`, hươu `48h/96h`, rùa `4 ngày/8 ngày`**. Số nằm ở **CẢ HAI** nơi —
  `ItemDataGenerator.SetAnimalGameplay` **và** file `.asset` trong `Assets/Resources/Items/`.
- Chữ nổi trên cây/thú: **BỎ HẲN**, công tắc trong Cài đặt cũng đã **ẩn** (13/08). Khoá 2 chỗ:
  `SettingsPopup.uxml` (`display:none`) + `FarmLabelVisibility.ForceHidden`. Nhãn "ĐÃ CHẾT" trên xác vẫn hiện.
- Câu cá: 10 lượt free/ngày, hết thì tốn mồi, trừ mồi khi HOÀN THÀNH animation, mồi không buff cá hiếm.
  Giá mồi/vé đào 2, vé vòng quay 5. Câu cá + đào đá **chỉ ở đảo `city`**.
- Tỷ giá `1 USDT = 26,5 Point`, `1 YWH = 1,59 Point`; hoa hồng 6 cấp `8% + 5×1%`; VIP cộng dồn từ
  `2.650 Point` nguồn USDT. (Contract chi tiết ở `docs/POINT_WALLET_BUSINESS_RULES.md`.)
- Chặn thao tác thì **phải báo bằng toast** (`NotifyBlocked`), không được chặn im lặng.
- Số bệnh của dê/ngỗng trông như gõ nhầm nhưng là ý khách — **ĐỪNG tự sửa**.
- Phân bón: món hàng vô dụng (không code nào áp lên cây), đã **ẩn 4 chỗ** phía client, giữ asset để không
  hỏng kho người chơi cũ. `shopCatalog` phía server còn sót.

### IN_PROGRESS / việc tiếp theo

- `[ ]` **Nút admin "tạm đóng hỗ trợ 40%"** — mới có cờ client `RescueConfig.CompanySupport40On`
  (`Assets/_Project/Scripts/Environment/RescueConfig.cs:16`), chưa có UI lẫn endpoint. VPS đã sống lại
  nên hết bị chặn hạ tầng.
- `[ ]` **`PUT /player/farm-state` thiếu `idempotency_key`** — chi tiết chẩn đoán ở `task.md` khối 29/07
  mục 3 (vẫn nguyên giá trị). Theo khuôn mẫu `lockIdempotency` + `findStoredTransaction` trong
  `server/postgresStore.js`. Sửa cả `server/index.js` lẫn client `FarmStateSync`, **cần deploy**.
- `[ ]` **Tinh chỉnh `deadTiltDegrees` / `deadSink`** cho đà điểu và dê (thỏ đã ổn) — việc trong Editor.
- `[~]` **Ví Point** (canary tiền thật, migration số dư cũ, hoa hồng/VIP, source-lot PostgreSQL):
  **chỉ khi có phê duyệt riêng**, kèm backup và rollback.

### UNKNOWN / BLOCKED — phải verify trước khi hành động

- **Chi tiết trạng thái ví Point trên production sau 20/07: KHÔNG BIẾT.** Số liệu trong
  `docs/CONTEXT_RECOVERY.md` là ảnh chụp tới 20/07: authority v3 deploy **dormant**,
  `WEB_POINT_WALLET_DEBIT_ENABLED=false`, top-up `mode=canary` đúng 1 identity QA, public callback `404`.
  Health `200` ngày 13/08 chỉ chứng minh **service sống**, không nói gì về mấy cờ này.
- **Chưa có bằng chứng trực tiếp** cho "account mới = 0 Point" và "giá `bait_01` = 2" trên production
  (`/health` không trả version, không có route public đọc shop catalog). Muốn chắc phải đăng nhập account mới.
- **Gate PostgreSQL source-lot** vẫn blocked: máy local không có Docker/psql/WSL nên chưa chạy
  `npm.cmd run test:postgres --prefix server`. Migration `007` chưa apply.
- **Rủi ro dời chuồng chưa test online:** dời chuồng ghi vào `build_state_json`; upload fail hoặc `409` thì
  server giữ chuồng cũ → relogin chuồng có thể nhảy về chỗ cũ. Có `idempotency_key` là hết phần lớn ca này.
- Mật khẩu root VPS đã rotate chưa: **chưa xác nhận**.

## 6. Cách tự kiểm chứng (lệnh an toàn)

| Mục đích | Lệnh | Kỳ vọng |
|---|---|---|
| Git baseline | `git status --short --branch`, `git log --oneline -n 8` | branch `main`, HEAD `9973918f` trở đi |
| **Compile C# KHÔNG cần mở Unity** | Roslyn của Unity: `D:\Du_lieu_Unity\6000.3.15f1\Editor\Data\NetCoreRuntime\dotnet.exe` chạy `...\DotNetSdkRoslyn\csc.dll` với response file | `csc exit code: 0` |
| Test server (JSON store) | `npm.cmd run test:point-source-ledger --prefix server` | pass |
| Gate PostgreSQL | `npm.cmd run test:postgres --prefix server` | **chạy không được trên máy này** — thiếu runtime |
| Sức khoẻ prod (read-only) | `irm https://api.ywonder.net/game-api/health` | `ok:true`, `storage.mode = postgres` |
| **Route đã lên prod chưa** (read-only, không token) | `Invoke-WebRequest -Method POST <url> -UseBasicParsing` rồi bắt `catch { [int]$_.Exception.Response.StatusCode }` | `401` = route ĐÃ có; `404` = chưa deploy. ⚠️ `curl` trong PowerShell là alias của `Invoke-WebRequest` nên cú pháp `curl -s -o /dev/null -w` sẽ lỗi |
| File nặng có qua LFS không | `git check-attr filter -- <file>` | `filter: lfs` → **không** vướng giới hạn 100 MiB của GitHub |

**Công thức compile-check:** gom mọi `.cs` trong `Assets/` trừ thư mục `Editor` và thư mục có `.asmdef`
(~128 file); tham chiếu `Editor\Data\Managed\UnityEngine\*.dll` + `Library\ScriptAssemblies\*.dll` (bỏ
`Assembly-CSharp*`) + `Newtonsoft.Json.dll` trong `Library\PackageCache` + `netstandard.dll`; thêm cờ
`-define:UNITY_EDITOR`. Muốn kiểm assembly Editor thì làm ngược lại (CHỈ lấy thư mục `Editor`) và tham chiếu
thêm `UnityEditor.dll` + DLL runtime vừa build.
⚠️ **Thiếu `-define:UNITY_EDITOR` sẽ báo lỗi GIẢ** kiểu `CropDatabase does not contain ClearCrops` — đó là
lỗi của bộ kiểm, không phải của code.

## 7. Build và giao bản

- **Cờ phải kiểm trước mỗi lần build:** `AnimalPrefabLibrary.testTimeScale` = `1` (khác 1 là thú/cây chết
  sai giờ; Console in cảnh báo vàng), `BackendConfig.useOfflineFallback` = `0`.
- **Bản export iOS nằm NGAY TRONG repo ở `/ios`**, ghi đè mỗi lần build rồi commit (~650 file thay đổi).
  File nặng đi qua **Git LFS** theo `.gitattributes` (`/ios/**/*.a`, `/ios/**/*.resS`) nên **không** vướng
  giới hạn 100 MiB. Repo pack đã ~1,87 GiB.
- **Bẫy icon iOS (đã dính 13/08):** Player Settings > iOS > Icon để trống thì export chỉ ra 5 icon tạm và
  **không có tấm 1024×1024** → App Store Connect chặn ở bước validate, mà chạy thử trên máy không thấy gì
  bất thường. Icon lưu trong `ProjectSettings/ProjectSettings.asset` (file được bảo vệ nhưng **phải commit**),
  và Unity chỉ ghi xuống đĩa khi **File → Save Project**.
- **APK từng 1,2 GB** vì texture (96%) — xem `Assets/_Project/Editor/TextureSizeReducer.cs`. Instancing/Static
  batching KHÔNG giảm dung lượng file.

## 8. Prompt khởi động cho phiên AI mới

```text
Tiếp tục Y WONDER GREEN FARM tại D:\LamGameUnity\BaChuKhuRung3D theo The Memento Protocol.
Xưng "bé", gọi tôi là "anh yêu".

Trước khi thay đổi bất kỳ thứ gì:
1. Đọc RULES.md, docs/THE_MEMENTO_PROTOCOL.md, docs/MEMENTO_PACKET_CURRENT.md,
   docs/CONTEXT_RECOVERY.md, docs/CHANGELOG.md, task.md (mục đầu file) theo thứ tự.
   KHÔNG dùng CHANGELOG.md ở gốc repo và LoTrinh_Demo_Thu2.md — cả hai đã lỗi thời.
2. Chạy git status --short --branch và git log --oneline -n 8; báo cáo branch, HEAD,
   worktree và ownership trước khi sửa.
3. Không reset/clean/checkout/revert hàng loạt. Không sửa/xoá .meta. Không đưa secret vào repo.
   Không đụng scene/Map2.1/HDRI/animation của tôi nếu không được giao rõ ràng.
4. Phân biệt VERIFIED / DECIDED / IN_PROGRESS / UNKNOWN. Không tự deploy, không SSH,
   không bật wallet/debit/top-up, không dùng tiền thật.
5. Sửa xong phải tự biên dịch bằng Roslyn (mục 6) trước khi báo "hoàn thành".
   Tôi nghiệm thu xong thì cứ commit + push nhánh đang đứng, khỏi hỏi lại; nhưng đừng tự gộp main.

Nhánh làm việc: main (feat/gameplay-followups đã gộp hết).
Mục tiêu trước mắt: [điền task].
Báo plan ngắn gọn rồi làm tới khi có bằng chứng nghiệm thu.
```

## 9. Điều kiện làm mới packet

Cập nhật ngay sau bất kỳ sự kiện nào: commit/merge/**deploy**/rollback, build EXE/APK/iOS được nghiệm thu,
migration/schema đổi, quyết định khách mới, lỗi P0 mới, đổi ownership worktree, canary/giao dịch, hoặc trước
khi kết thúc một phiên AI dài. Ghi ngày giờ + đường dẫn bằng chứng hoặc lệnh read-only lặp lại được; chưa
xác minh thì để `UNKNOWN`. Không copy secret/token/password/email/raw log nhạy cảm vào packet.
