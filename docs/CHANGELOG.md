# Changelog — Y WONDER GREEN FARM
# Format: Theo module, có ngày và danh sách files thay đổi

> **⚠️ TRẠNG THÁI QC:** Tất cả các module UI hiện tại đều **CHƯA được QC review** bởi khách hàng.
> Nếu QC/khách hàng không duyệt → sẽ sửa lại theo feedback.

---

## [2026-08-04e] — Cây chết để lại HÉO tại ô (đảo luật cũ) — PHA 2 (phần cây trồng)

### Đảo luật (khách chốt 04/08)
Trước đây `DieFromDrought` = ô về đất trống + mất giống. Giờ NGƯỢC: cây héo chết để lại tại ô,
giữ giống + giữ ô giàn, chờ người chơi cứu (60% giá mua hạt).

### Sửa (`FarmTile.cs`)
- Cờ `cropDead` (không thêm giá trị enum để khỏi đụng loạt switch trên TileState).
- `DieFromDrought`: bỏ reset về Soil + FreeSlaves + xoá model. Chỉ set `cropDead`, dừng lớn, bỏ
  thanh nước, nhuộm nâu héo (`ApplyWitheredLook`), ghi thư báo, lưu ngay.
- `Update`: `cropDead` thì đứng yên — không chết lại, không lớn.
- `InteractWater`: chặn tưới cây héo (phải CỨU mới sống). `WaterAgain` tự chặn vì hết `isGrowing`.
- Lưu/nạp `cropDead` trong `CropSave`; `RestoreSave` giữ nguyên xác héo, bỏ qua đánh giá offline.
- `ResetForPlayerState`: reset cờ khi đổi người chơi.

### ⚠️ Chưa xong — PHẢI đi kèm Pha 3
Cây héo giữ ô mà chưa cứu được → ô kẹt. Nhãn nổi "ĐÃ CHẾT" cho cây chưa làm (dựa nhuộm nâu là
tín hiệu chính). Còn: công cụ cứu 60% giá mua cho cả thú lẫn cây.

---

## [2026-08-04d] — Thú chết để lại XÁC (đảo luật cũ) — PHA 2 (phần vật nuôi)

### Đảo luật (khách chốt 04/08)
Trước đây `DieFromHunger` ghi "chết = biến mất + trả ô, không để xác". Giờ NGƯỢC: thú chết đói
để lại XÁC trong chuồng, GIỮ sản phẩm đã cộng dồn, CHIẾM ô tới khi người chơi cứu.

### Sửa (`FarmAnimal.cs`)
- `DieFromHunger`: bỏ Destroy + RemoveAnimal + ClearAnimal ô. Chỉ set Dead, giữ ô/sản phẩm, ghi
  thư báo chết, lưu ngay (reload vẫn thấy xác).
- Nghiêng model thành xác UNIVERSAL (primitive + model thật): chụp "gốc model" lúc Initialize
  (trước khi dựng thanh máu/nhãn), khi chết nghiêng 82° + lún nhẹ; chừa thanh máu/nhãn ra. Tư thế
  gốc cache lại để cứu sống trả về nguyên (Pha 3). Số nghiêng/lún ở `DeadTilt`/`DeadSink`.
- Nhãn "ĐÃ CHẾT" (đỏ) LUÔN hiện trên xác kể cả khi tắt nhãn thường; đặt world-space cho thẳng đứng.
  Dùng chữ thay 💀 vì TextMesh font mặc định không render emoji (muốn icon xương cần sprite).
- `RestoreAnimalState`: khôi phục trạng thái Dead qua reload (trước chỉ khôi phục Sick).

### ⚠️ Chưa xong — PHẢI đi kèm Pha 3 mới ship
Hiện xác CHIẾM ô mà CHƯA có công cụ cứu → ô bị kẹt. Đừng gộp main tới khi Pha 3 (cứu) xong.
Còn: cây chết để lại (FarmTile) + công cụ cứu 60% giá mua.

---

## [2026-08-04c] — Cộng dồn sản phẩm vật nuôi qua nhiều vòng (khách chốt 04/08) — PHA 1

### Yêu cầu
Trước đây thú ra 1 sản phẩm rồi ĐỒNG HỒ ĐỨNG (chưa thu thì vòng sau không chạy). Khách muốn:
mỗi vòng hoàn tất +1 sản phẩm, dồn không giới hạn, thu là gom hết một lần; số lượng/thời gian
mỗi vòng giữ nguyên. Thú chết/bệnh thì ngừng dồn; vòng đang dở lúc chết bị mất.

### Sửa
- `FarmAnimal.cs`: thêm `pendingProduct` (số vòng đã dồn chưa thu). Update tính số vòng đã trôi,
  cộng vào pending, đồng hồ chạy độc lập (thu KHÔNG reset nữa, giữ phần dư tiến tới vòng kế).
  Loài thu hữu hạn: không dồn quá số lần thu còn lại; `harvestsRemaining` trừ lúc SẢN XUẤT, làm
  thịt vòng cuối vẫn CHỜ người chơi thu mẻ cuối. `HarvestProduct` gom hết pending trong 1 lần thu.
- `BuildPersistence.cs`: lưu/nạp `pendingProduct` (mặc định -1 cho save cũ → suy từ cờ bool, không
  mất sản phẩm của người chơi cũ).
- `AnimalInteractionPopupController.cs`: hiện số đã dồn ("Có N sản phẩm!", "Sẵn sàng (N)").

### Còn lại (pha sau)
Pha 2: thú/cây chết để XÁC (giữ sản phẩm, chờ cứu). Pha 3: công cụ cứu 60% giá mua + cờ hỗ trợ 40%.

---

## [2026-08-04b] — Bỏ hậu tố "V2" ở tên vật nuôi (khách yêu cầu)

### Khách chốt 04/08
Tên vật nuôi bỏ chữ "V2": hiển thị ở thẻ chuồng, tiêu đề popup, toast đều dùng chung
trường `animalName`, sửa 1 chỗ/con là khớp hết.

### Files
- `Assets/Resources/Items/Animal_chicken_01.asset` — "Gà mái V2" → "Gà mái"
- `Assets/Resources/Items/Animal_goose_01.asset` — "Ngỗng con V2" → "Ngỗng con"
- `Assets/Resources/Items/Animal_ostrich_01.asset` — "Đà điểu V2" → "Đà điểu"
- `Assets/Resources/Items/Animal_goat_01.asset` — "Dê con V2" → "Dê con"
- `Assets/Resources/Items/Animal_rabbit_01.asset` — "Thỏ con V2" → "Thỏ con"

Chỉ sửa nội dung chuỗi, giữ nguyên escape unicode — không đụng code, không cần compile.

---

## [2026-08-04a] — Khớp đếm ngược thu hoạch với thời gian cây chín thật (hướng dẫn)

### Lỗi
Bước chờ thu hoạch trong hướng dẫn đếm ngược **5 giây** rồi NPC hô "Chín rồi kìa! Mau nhấp
vào thu hoạch." — nhưng ô đất của hướng dẫn chỉ chín ở **24 giây** (`TutorialFastGrowthSec`).
Người chơi bấm thu hoạch theo lời NPC nhưng `HandleHarvest` trả `false` vì ô chưa `Ripe`,
phải chờ thêm ~19 giây trong bối rối.

### Sửa
`TutorialManager.cs`:
- Đếm ngược khởi từ `TutorialFastGrowthSec` (24s) thay vì hằng số 5 rời rạc. Đếm ngược và ô
  đất **cùng bấm giờ từ khoảnh khắc TƯỚI**, nên hai đồng hồ chạy song song.
- Điều kiện hô "chín" giờ chốt theo **trạng thái thật của ô** (`currentState == Ripe`), không
  thuần theo giờ — tránh lệch nhịp làm tròn giữa `WaitForSeconds` (giờ game) và `growStartTime`
  (giờ tường) khiến NPC hô sớm một nhịp, người chơi bấm hụt.
- Chốt an toàn 10s phòng ô mục tiêu null (adopt hụt) — không để tutorial kẹt vĩnh viễn.

### Anh chốt 04/08
Hạ `TutorialFastGrowthSec` 24s → **8s** cho tân thủ khỏi ngồi chờ lâu. Cả ô đất lẫn đếm ngược
tự khớp theo (một nguồn sự thật duy nhất).

---

## [2026-07-31i] — Xong hướng dẫn thì bong bóng ghi "Hiện chưa có nhiệm vụ"

### Anh chốt 31/07
Đã hoàn thành hướng dẫn tân thủ thì đừng lặp mãi câu "Tìm NPC Tân Thủ để bắt đầu!".

### Hai câu bị kẹt vĩnh viễn
1. **Người chơi cũ**: `WireHud()` đặt cứng "Tìm NPC Tân Thủ để bắt đầu!", mà `TutorialManager`
   không chạy với hồ sơ `tutorialCompleted` nên không ai ghi đè → câu đó nằm mãi.
2. **Người vừa xong**: `CompleteTutorial()` ghi "Hoàn thành Hướng Dẫn Tân Thủ!" rồi cũng để đó luôn.

### Sửa
- `GameHUDController.ApplyIdleQuestText()` — chọn câu theo hồ sơ: đã xong → `"Hiện chưa có nhiệm vụ"`,
  chưa xong → nhắc tìm NPC. Tự bỏ qua nếu hướng dẫn đang chạy (lúc đó nó là chủ nhãn, các bước `[x/11]`).
- `SyncQuestText()` — hồ sơ nạp từ cache ngay ở `Awake` nhưng bản thật về sau lượt gọi mạng, nên
  chờ `IsLoaded` rồi chỉnh lại, giống cách `SyncPlayerName` chờ tên.
- `CompleteTutorial()` khoe "Hoàn thành!" 6 giây rồi trả nhãn về trạng thái rảnh.

### Lưu ý cho người sửa sau
Bong bóng **phải luôn hiện** vì nó là lối vào **duy nhất** của popup Nhiệm vụ. `SetQuest("")` sẽ
**ẩn** nó đi — đừng bao giờ truyền chuỗi rỗng cho trạng thái "không có nhiệm vụ".

### Files
- `Assets/_Project/UI/GameHUDController.cs`
- `Assets/_Project/Scripts/Tutorial/TutorialManager.cs`

`csc exit code: 0`.

---

## [2026-07-31h] — Chặn thao tác thì phải báo, đừng nuốt cú bấm

### Nguyên tắc (anh chốt 31/07)
Ngăn hành động bất thường của người chơi thì **phải có toast**. Chặn mà im lặng thì người chơi
tưởng game đơ / máy lag rồi bấm loạn — đúng kiểu bực nhất.

### Helper dùng chung
`FarmInteractionController.NotifyBlocked(message, cooldownSec = 1.5f)` — cooldown theo **từng câu**:
bấm liên tục cùng một chỗ thì không spam, nhưng đổi sang lỗi khác là báo ngay chứ không bị nuốt.

### Ba chỗ trước đây im lặng
| Chỗ | Trước | Giờ |
|---|---|---|
| Chạm ô đất ngoài tầm với | chỉ `Debug.LogWarning` | "Đứng gần ô đất hơn mới thao tác được." |
| Bấm nút **Dời ruộng** ngoài tầm | nuốt luôn cú bấm | "Đứng gần mảnh ruộng hơn mới dời được." |
| Thao tác ruộng trong lúc đang câu cá | chỉ `Debug.LogWarning` | "Đang câu cá — thu cần rồi hãy làm việc khác nhé." |

Nặng nhất là "Dời ruộng": **nút đã hiện ra** rồi bấm không ăn thua gì cả.

### CỐ Ý không thêm toast
- `BeginTimedAction` chặn vì **đang múa động tác khác** / **player đang bận**: đó là bấm sốt ruột
  chứ không phải hành động bất thường, mà lúc đó **thanh Hủy đang hiện** ngay trên màn hình rồi.
  Thêm toast chỉ tổ ồn.
- Đè chuột đào đá sai đảo (`HandleHold`) đi qua vòng lặp mỗi khung hình — luồng **bấm** đã có toast
  "Chỉ đào đá được ở Thành phố hoặc Đảo mỏ thôi!", đủ rồi.

### Files
- `Assets/_Project/Scripts/Environment/FarmInteractionController.cs`

`csc exit code: 0`.

---

## [2026-07-31g] — Bịt hẳn cheat tua nhanh: thu hẹp về đúng ô của hướng dẫn

### Vì sao khóa quầy chưa đủ
Khóa quầy (`[2026-07-31f]`) chỉ **hoãn** vòng lặp: người chơi vẫn ôm hạt sẵn, tua 24s liên tục,
tích kho rồi **xong hướng dẫn mới bán một thể**. Phải chặn ở gốc: cơ chế tua nhanh.

### Sửa
`FarmTile.GetGrowthTime()` trước đây chặn **toàn cục**:

```csharp
if (tm != null && tm.IsActive()) return 24f;   // mọi ô, mọi loại cây
```

Giờ chỉ tua nhanh **đúng một ô** mà hướng dẫn đang bám:

```csharp
if (tm != null && tm.IsFastGrowthTile(this)) return tutorialGrowthTime;
```

- `TutorialManager.IsFastGrowthTile(tile)` = còn hướng dẫn **và** `tile` đúng là `targetFarmTile`.
- `targetFarmTile` là ô người chơi **thật sự vừa thao tác** (`AdoptFarmTile`, xem `[2026-07-31d]`),
  nên nhịp hướng dẫn không đổi: cả chuỗi cuốc → gieo → tưới → thu đều trên đúng ô đó.
- Hằng số gom về `TutorialManager.TutorialFastGrowthSec = 24f` cho hai nơi dùng chung một số.
- Đổi `FindFirstObjectByType<TutorialManager>()` → `TutorialManager.Instance` (hàm này nằm trong
  đường tính tăng trưởng, quét scene ở đây là phí).

### Dọn kèm
Xoá `FarmTile.IsTutorialActive()` — **không có chỗ nào gọi**, nhưng chú thích của nó ("tutorial thì
KHÔNG cho cây chết") mô tả một luật không hề tồn tại, đọc vào là hiểu sai luật chết cây.

### Còn lại (không sửa, để anh chốt)
- `FarmAnimal.cs:240` bỏ qua **cú gieo xác suất phát bệnh** khi còn hướng dẫn. Không phải miễn
  nhiễm vĩnh viễn (`sicknessRolled` vẫn `false` nên gieo lại sau), và mốc phát bệnh cách hàng ngày
  trong khi hướng dẫn chỉ vài phút → thực tế vô hại.
- Hướng dẫn đếm ngược **5s** rồi hô "chín rồi!" trong khi ô chín ở **24s** — lệch sẵn từ trước,
  không thuộc lỗ hổng này.

### Files
- `Assets/_Project/Scripts/Environment/FarmTile.cs`
- `Assets/_Project/Scripts/Tutorial/TutorialManager.cs`

`csc exit code: 0`.

---

## [2026-07-31f] — Khóa quầy mua bán khi chưa xong hướng dẫn

### Lý do (anh chốt 31/07)
Trong lúc chưa hoàn thành hướng dẫn, cơ chế **tua nhanh thời gian vẫn luôn bật**. Người chơi
cố tình phớt lờ NPC là có nông trại tua nhanh, mở quầy được thì thành vòng lặp kiếm tiền.

### Lỗ hổng thật sự (rộng hơn tưởng)
`FarmTile.GetGrowthTime()` chặn **ngay dòng đầu**, trước cả khi đọc dữ liệu cây:

```csharp
TutorialManager tm = FindFirstObjectByType<TutorialManager>();
if (tm != null && tm.IsActive()) return 24f;   // MỌI loại cây, kể cả cây lâu năm
```

Tức là ai bỏ dở hướng dẫn thì có một nông trại **tua nhanh**, áp cho mọi loại cây, không giới hạn số ô.

> **Đính chính (31/07):** bản đầu của mục này còn ghi "cây không chết trong tutorial" dựa trên
> `FarmTile.IsTutorialActive()`. Kiểm lại thì hàm đó **không có chỗ nào gọi** — luật đó không tồn tại.
> Cây trong hướng dẫn vẫn theo mốc chết bình thường. Đã xoá hàm chết ở mục `[2026-07-31g]`.

### Sửa
Chặn ở `ShopPopupController.Show(ShopData)` — cả 4 lối vào quầy (ShopData / mock / AccessMode /
ShopDefinition) đều dồn về hàm này nên không sót đường nào: NPC, vùng chạm nhà, phím tắt cũ.

- Chặn khi `TutorialManager.Instance.IsActive()` — đúng bằng điều kiện đang bật tua nhanh.
- Người chơi cũ (`tutorialCompleted`) không bị ảnh hưởng: tutorial không chạy → `IsActive()` sai.
- Toast "Xong hướng dẫn của NPC Tân Thủ đã rồi mới mua bán được nhé!", cooldown 2s vì vùng chạm
  nhà bắn `OnTriggerEnter` mỗi lần đi qua.
- Khóa **cả tab Bán**, không chỉ tab Mua — bán mới là chỗ ra tiền.

### CÒN HỞ — cần anh chốt
Khóa quầy **không đóng hẳn** vòng lặp: người chơi vẫn có thể ôm hạt sẵn, tua 24s liên tục,
tích kho rồi **xong hướng dẫn mới bán một thể**. Muốn bịt hẳn thì phải thu hẹp cơ chế tua nhanh
về **đúng ô đất của hướng dẫn** thay vì áp toàn cục.

### Files
- `Assets/_Project/UI/ShopPopupController.cs`

`csc exit code: 0`.

---

## [2026-07-31e] — Bảng nút ruộng trễ đúng một bước

### Triệu chứng
Cuốc đất xong nhưng nút vẫn ghi **"Cuốc đất"**. Bấm lại thì nhân vật làm việc **kế tiếp** —
hiện popup chọn hạt và gieo thật. Tức là chữ trên nút trễ một nhịp so với trạng thái ô.

### Gốc rễ
`RunTimedAction` dựng lại bảng nút **TRƯỚC** khi chạy `onComplete`:

```csharp
FinishTimedAction();   // -> RestorePromptAfterTimedAction() -> đọc trạng thái ô
onComplete?.Invoke();  // <- CHỖ NÀY mới thật sự cuốc/gieo/tưới/thu
```

Mọi thao tác ruộng đều đổi trạng thái **bên trong** `onComplete` (`HandlePlow` → `InteractPlow`,
`HandleWater` → `InteractWater`, ...). Nên bảng nút luôn được dựng theo trạng thái **CŨ**:
ô còn `Soil` → in "Cuốc đất", ngay sau đó `onComplete` mới chuyển sang `Plowed`.

Bấm nút thì `PerformTileAction` đọc trạng thái **hiện tại** (`Plowed`) nên làm đúng việc kế tiếp —
chữ sai, việc đúng, đúng như anh mô tả.

Lỗi dính **cả bốn bước** ruộng (cuốc/gieo/tưới/thu) và cả các thao tác có màn múa của vật nuôi,
vì tất cả đi chung `BeginTimedAction`. Ở chế độ chạm-thẳng (mobile) vòng quét theo tầm ngắm không
chạy nên không có gì sửa lại giúp — bảng sai nằm đó tới khi chạm ô lần nữa.

### Sửa
- `RunTimedAction`: giữ lại mục tiêu, gọi `FinishTimedAction(restorePrompt: false)`, chạy
  `onComplete`, **rồi mới** `RebuildPromptFor(promptTarget)`.
- Nếu `onComplete` mở màn múa mới (chuỗi thao tác) thì bỏ qua việc dựng lại — để màn mới tự lo,
  không bày nút đè lên thanh Hủy.
- `FinishTimedAction` nhận cờ `restorePrompt` (mặc định `true`, giữ nguyên hành vi cũ cho các
  luồng khác); gộp luôn `RestorePromptAfterTimedAction` vào trong.

### Files
- `Assets/_Project/Scripts/Environment/FarmInteractionController.cs`

`csc exit code: 0`.

---

## [2026-07-31d] — Hướng dẫn tân thủ kẹt ở bước cuốc đất

### Triệu chứng
Anh cuốc đất xong, NPC **đứng im lặp thoại** như không biết người chơi đã làm.

### Gốc rễ
Cả chuỗi ruộng của hướng dẫn (cuốc → gieo → tưới → thu) treo vào **đúng MỘT ô đất** mà
`TutorialManager` **đoán trước** ngay lúc người chơi đặt ruộng xuống:

```csharp
targetFarmTile = FindNewlyBuiltTile();
targetFarmTile.OnTilePlowed += OnTilePlowed;   // chỉ nghe ô này
```

Đoán trượt là hỏng cả chuỗi. Mà `FindNewlyBuiltTile()` có sẵn một nhánh **bảo đảm** đoán trượt:

```csharp
// Fallback: nếu không thấy ô mới, lấy ô bất kỳ để không kẹt.
return all.Length > 0 ? all[0] : null;
```

Nhánh này viết ra để *chống* kẹt nhưng chính nó *gây* kẹt: nó gán hướng dẫn vào **một ô cũ ở đẩu
đâu** mà người chơi không có lý do gì đụng tới. Người chơi cuốc ô mới → sự kiện bắn trên ô mới →
hướng dẫn đang nghe ô khác → không nghe thấy gì → NPC đứng lặp thoại.

Tài khoản đã có sẵn nhiều ruộng (như máy anh, test cả ngày) thì càng dễ dính. Bước hành động
**không có** cơ chế tự nhảy — anti-kẹt chỉ phủ các bước đi-theo-NPC — nên kẹt là kẹt vĩnh viễn.

### Sửa
Bỏ hẳn kiểu đoán trước. `FarmTile` có thêm 4 sự kiện **dùng chung cho mọi ô**
(`AnyTilePlowed/Planted/Watered/Harvested`), hướng dẫn nghe những cái đó rồi **bám theo ô người
chơi thật sự vừa làm** (`AdoptFarmTile`) — cuốc ô nào thì các bước sau theo ô đó, kể cả ô cũ.

`FindNewlyBuiltTile()` giờ trả `null` khi không nhận ra ô mới thay vì lấy đại; nó chỉ còn dùng để
tua nhanh thời gian lớn, không còn quyết định tiến trình.

Sự kiện tĩnh bọc `try/catch` — một handler ném lỗi không được phép chặn luôn thao tác của người
chơi (bài học từ `OnResourceHarvested`).

---

## [2026-07-31c] — Điểm danh chuyển hẳn lên server + luật mất chuỗi

### Vì sao phải làm
Anh hỏi "điểm danh đã đồng bộ và được server quản lý chưa". Rà ra: **chưa, và đây là chỗ thủng thật**.

Toàn bộ sổ điểm danh nằm trong PlayerPrefs của máy (`YW_AttendanceClaimedDays` /
`YW_AttendanceLastDate`), grep cả `server/` không có một dòng nào về điểm danh. Nhưng **phần thưởng
thì đi thẳng lên server thật** qua `QueueEconomyDelta` / `QueueInventoryDelta`. Sổ ở máy, tiền ở
server ⇒ ba lỗ:

1. **Vặn đồng hồ / đổi múi giờ thiết bị** → qua "ngày mới" → ăn trọn 15 ngày trong vài phút.
2. **Cài lại game** → bộ đếm về 0 → quay vòng 15 ngày lại từ đầu, lặp vô hạn.
   Mỗi vòng ≈ 52 Point + 12 gỗ (bán 96 Point) + 1 thỏ + nông sản.
3. **Đổi máy** → người chơi thật mất sạch tiến độ.

Ngày lại lấy `DateTime.Now` — **tờ lịch của điện thoại**, và cắt theo nửa đêm chứ không phải đủ 24
giờ như cây/thú (`RealNow()`), nên điểm danh 23:59 rồi 00:01 là **hai ngày cách nhau 2 phút**.

### Luật mới (khách chốt 31/07)
**Nghỉ giữa chừng là mất chuỗi, quay về Ngày 1.**

Chỗ này có một cái bẫy: nếu chỉ đếm một con số thì mất chuỗi = về ngày 1 = **được trả lại quà ngày
1**, và người chơi chỉ cần điểm danh cách ngày là in tiền mãi mãi — đúng cái lỗ vừa đi bịt. Nên bảng
giữ **hai** cột:

| Cột | Nghĩa |
|---|---|
| `claimed_days` | vị trí hiện tại trong chuỗi |
| `max_rewarded_day` | ngày cao nhất **đã từng trả thưởng** |

Chuỗi tụt về 1 thật (muốn chạm quà ngày 15 phải leo lại đủ 15 ngày liên tiếp), nhưng đi qua ngày cũ
**không lĩnh lần hai**. Mốc 15 ngày là quà tân thủ — một lần cho một tài khoản.

> **Anh chốt 31/07: KHÔNG cho vòng lặp nhận tiền** — giữ `max_rewarded_day`.
> Riêng khách thì chưa được hỏi ý về điểm này; nếu sau này khách đòi cho lĩnh lại thì phải nói rõ
> hệ quả là vòng lặp kiếm tiền trước khi sửa.

### Sửa gì
**Server** — sổ + luật + trao thưởng đều về server, client chỉ vẽ lại:
- `migrations/009_player_attendance.sql`, `schema.sql` — bảng `player_attendance`
- `attendanceRules.js` (mới) — bảng thưởng 15 ngày + luật chuỗi, **dùng chung** hai kho
- `gameDay.js` (mới) — một định nghĩa "ngày game" duy nhất
- `store.js`, `postgresStore.js` — `getAttendance` / `claimAttendance`, có idempotency như vòng quay
- `index.js` — `GET /player/attendance`, `POST /player/attendance/claim`

**Tiện thể bịt một lỗi lệch giờ:** hai kho đang chấm ngày khác nhau — `store.js` cắt theo **UTC**,
`postgresStore.js` theo **Asia/Ho_Chi_Minh**. Bản chạy thật là postgres nên người chơi không thấy,
nhưng chạy kho JSON (máy dev) thì lượt câu cá / vòng quay reset **lệch 7 tiếng** so với production —
thử ở nhà đúng, lên server sai. Giờ cả hai cùng gọi `gameDay.js`.

**Client:**
- `AttendanceService.cs` (mới) — gọi 2 endpoint, gửi lại đúng key cũ khi mất mạng
- `EventPopupController.cs` — online đọc/nhận theo server; offline (demo chưa đăng nhập) giữ đường
  local **nhưng đã áp cùng luật mất chuỗi**, không còn hai hành vi khác nhau

Nốt đỏ trên HUD gọi hàm tĩnh nên có bản đệm trạng thái server + hãm 30 giây, tránh biến mỗi khung
hình thành một lượt gọi mạng.

### Đã thử
- 33 phép thử luật chuỗi trên kho JSON (bơm sẵn "hôm nay" nên không phải chờ qua ngày thật):
  nghỉ ngày → về 1 · leo lại qua ngày cũ **không** lĩnh lại · vượt mốc cũ thì có quà · hết 15 ngày
  thì khoá hẳn · gửi trùng key không cộng đôi.
- Thử thật qua HTTP với server chạy kho JSON: 200 / 400 thiếu key / 409 bấm hai lần trong ngày /
  401 không token / bản sao không cộng đôi tiền.

> **CHƯA DEPLOY.** Client gọi endpoint chưa có trên prod ⇒ **phải deploy server TRƯỚC khi build APK**,
> không thì điểm danh ăn 404. Xem `server/RUNBOOK_deploy_attendance.md`.

---

## [2026-07-31b] — Rà soát toàn bộ shop + bón phân bón thẳng

### Rà soát 8 cửa hàng
Viết script mô phỏng đúng luật `resolveShopOffer` của server rồi đối chiếu **từng món, từng shop**
giữa asset client / catalog đang chạy / catalog sinh lại. Kết quả:

**12 giao dịch đang bị máy chủ chặn** (đúng lỗi anh gặp) — tất cả do server chưa deploy:
- `Shop_ItemShop`: Gỗ, Đá, Nước tưới (thêm 30/07)
- `Shop_MiniGarden`: 8 nông sản ngắn ngày + Phân bón (khách chốt giá 30/07)

**`Shop_Verdant` có tab Bán CHẾT** — cái này deploy không chữa được, là lỗi dữ liệu client.
Nó khai thu mua 7 nông sản ngắn ngày, nhưng từ 22/06 nhóm này `canSell=false`. Client lọc bỏ món
không bán được (`ShopPopupController` dòng ~577) nên tab **luôn rỗng** — người chơi không gặp lỗi,
chỉ thấy trống. Sửa: bỏ `sellItemIds`, đổi `accessMode` **Both → BuyOnly**.
> ⚠️ Bắt buộc đổi chế độ. `sellItemIds` rỗng + `Both` thì server hiểu là **thu mua mọi thứ bán được**
> (nhánh `acceptsAllSellable`) — thành cửa mở. Khách chưa quy định Verdant thu mua gì nên để chỉ-bán.

Sau khi sửa: **không còn giao dịch nào client hiện mà server chặn**. Còn đúng 1 cảnh báo cố ý —
`watering_water_01` giá 0 (nước tưới miễn phí).

### Bón phân: bỏ hẳn bước mở túi đồ
Anh chốt 31/07. Bước chọn món trong túi là **thừa** — phân bón chỉ có ĐÚNG MỘT loại — mà lại đẻ ra
hai phiền: popup "Xem ruộng" phải đóng nên không bón liên tiếp được, và cờ `pendingFertilizeTile`
bị treo khi đóng túi giữa chừng (chính là lỗi #3 hôm nay).
- `BeginFertilize` giờ bón thẳng và **trả về bool**; xoá hẳn `pendingFertilizeTile` +
  `HandleFertilizeSelected` + nhánh trong `OnInventoryItemSelected`.
- Bấm Phân bón trong túi giờ rơi xuống `ItemUsageHint` — chỉ đường ra ruộng, đúng ý.
- Áp cho **cả** nút trong popup **và** nút ngoài ruộng (phím B). Ngoài ruộng bón xong gọi
  `RebuildPromptFor` để nút "Bón phân" biến mất khi cây đã chín.

### Tưới xong tự mở lại popup ruộng
Tưới có màn múa nên vẫn phải đóng popup, nhưng nay nhớ mảnh ruộng + cây đang chọn và **tự mở lại**
khi múa xong (`BeginWaterTile(tile, onWatered)` → `ShowPlot(tiles, selected)`). Bỏ dở thì không mở lại.

### Hai chỗ nói cho đúng bản chất
- Câu báo `SHOP_ITEM_NOT_ALLOWED` cũ ("Cửa hàng này không giao dịch vật phẩm đã chọn") **đổ oan cho
  cửa hàng**. Gặp lỗi này gần như luôn là do danh sách hàng trên máy chủ cũ hơn trong game. Đổi lại
  cho đúng để khỏi đi tìm nhầm chỗ.
- Log `FARM_STATE_CONFLICT` giờ in **cả hai số hiệu phiên bản** và nói rõ bản ghi client **bị vứt**.
  Câu cũ chỉ in phiên bản server nên không lần ra lệch bao nhiêu. Chưa sửa gì ở cơ chế — cần số liệu
  thật rồi mới quyết, xem `task.md` mục 3.

**Files:** `ShopDataGenerator.cs`, `Shop_Verdant.asset`, `server/shopCatalog.json`,
`FarmInteractionController.cs`, `AnimalInteractionPopupController.cs`, `ShopPopupController.cs`,
`FarmStateSync.cs`

---

## [2026-07-31] — 5 lỗi anh tìm ra khi chơi thử bản mới

### 1. Bảng nút biến mất sau khi tưới / cuốc
**Gốc:** `BeginTimedAction` cố ý xoá `currentActions` + ẩn prompt để nhường chỗ cho thanh Hủy trong
lúc múa động tác — nhưng `FinishTimedAction` **không ai dựng lại**. Ở chế độ chạm-thẳng (mobile),
`RefreshFrontCellInteractionPrompt` chỉ dựng theo ô TRƯỚC MẶT, nên ô vừa chạm ở bên hông thì mất hẳn.
**Sửa:** nhớ mục tiêu vào `promptTargetBeforeTimedAction`, xong việc thì `RestorePromptAfterTimedAction`
**dựng lại** bảng nút (không cất-rồi-bày, vì trạng thái đã đổi: chưa tưới → đã tưới, chín → vừa thu).
Bỏ dở giữa chừng thì KHÔNG dựng lại — người chơi chủ động thoát.

### 2. Phân bón niêm yết 1 Point nhưng trừ 50
**Gốc:** giá mua là **server quyết** (`shopCatalog.js` → `resolveShopOffer` trả `item.buyPrice`).
`server/shopCatalog.json` đã **lỗi thời cả tháng**, không chỉ mỗi phân bón:
- `fertilizer_01`: 50/25/bán-được → đúng phải 1/0/không-bán-được
- 8 nông sản ngắn ngày: `buyPrice: 0` (client là 4–11) và Mini Garden còn là `sell_only`
  ⇒ **online không mua nổi 8 món này**, dù khách đã chốt giá 30/07
- thiếu `wood_01` / `stone_01` / `watering_water_01` trong Cửa hàng Vật phẩm
- còn sót `fish_01` / `fish_02` — cá đời cũ đã bỏ khỏi game, không còn asset, không ai giữ trong túi

**Sửa:** chạy lại `server/scripts/generateShopCatalog.js` (script SẴN CÓ, đọc thẳng asset Unity).
Đã đối chiếu: `data.json` không có tài khoản nào giữ `fish_01`/`fish_02`, code client cũng không
nhắc tới, nên bỏ là sạch. Đã dò lỗ hổng mua-rẻ-bán-đắt: **không có** — mọi shop bán hạt/đá đều
`buy_only`, các shop thu mua đều liệt kê `sellItemIds` rõ ràng nên không nhận hạt/đá.
> ⚠️ **PHẢI DEPLOY SERVER** thì giá mới có tác dụng. Chưa deploy thì vẫn trừ 50.

### 3. Cuốc ruộng mà báo "hết phân bón"
**Gốc:** `BeginFertilize` đặt `pendingFertilizeTile` rồi mở túi, nhưng **đóng túi mà không chọn gì
thì cờ nằm lại mãi**. Lần sau mở túi bấm món bất kỳ, `OnInventoryItemSelected` thấy cờ còn → tưởng
đang bón phân → bắn toast về phân. Cờ này được xét TRƯỚC cả `pendingPlantTile` nên cướp luôn
luồng gieo hạt.
**Sửa:** `ClearPendingItemPickIfBagClosed()` chạy mỗi khung: túi đã đóng thì dọn
`pendingFertilizeTile` / `pendingFeedAnimal` / `pendingPlantTile`. Có chốt 1 khung hình để không dọn
nhầm ngay lúc vừa mở túi. **Cố ý không đụng** `pendingPen` / `pendingEnclosure` — hai cái đó do
luồng thả thú bất đồng bộ (chờ server) giữ.

### 4. Nút "Xem ruộng" hiện bàn tay
**Gốc:** `InteractionIconByAction` thiếu tên hành động mới → rơi về `Icon_Hand` (ảnh mặc định khi
không tra được), nên Xem ruộng / Bón phân / Dời ruộng trông giống hệt nhau.
**Sửa:** Xem ruộng = `Icon_Eye`. Bón phân mượn `Icon_FeedBowl`, ba nút Dời mượn `Icon_HandRelease`
— **chưa có ảnh riêng** cho hai nhóm này, cần artist vẽ thêm thì mới đúng nghĩa.

### 5. Dùng túi phân cuối cùng thì báo "hết phân bón"
Bón xong bắn toast thành công, rồi lần bấm sau bắn tiếp "hết phân bón" → tưởng bón hụt.
**Sửa:** gộp làm một câu — *"Đã bón phân: giảm 15% thời gian chín. Đó là túi phân cuối — mua thêm
ở Cửa hàng Vật phẩm hoặc Đại lý Hai Lúa."* Câu cảnh báo lúc mở túi cũng đổi thành "Trong túi không
còn phân bón" cho khỏi mơ hồ.

**Files:** `FarmInteractionController.cs`, `GameHUDController.cs`, `server/shopCatalog.json`

---

## [2026-07-30] — Gom việc vào popup "Xem ruộng" + Dời ruộng + Dời đường lát đá

Anh yêu cầu: *"tích hợp nhiều chức năng vào khi người chơi bấm xem ruộng như cách ta làm xem
chuồng, thêm nút dời ruộng giống với dời chuồng, tiện thì làm thêm dời đường lát đá."*

### Popup "Xem ruộng" hết chỉ-đọc — làm việc ngay trong popup
Chia hai tầng cho khỏi lẫn, dùng đúng bố cục của popup chuồng:
- **Hàng tiêu đề = việc của CẢ CỤM**: nút `BtnMoveGroup` đổi chữ theo chế độ ("Dời chuồng" /
  "Dời ruộng"). Chuồng có thêm "+ Thả thú" như cũ.
- **Hàng nút dưới = việc của THỨ ĐANG CHỌN**: `PlotActionsPanel` (Tưới nước / Thu hoạch / Bón phân)
  cho cây, song song hàng cũ (Cho ăn / Thu hoạch / Chữa bệnh / Vaccine) cho thú.
- Nút bật/tắt theo trạng thái cây; điều kiện bón hỏi thẳng `FarmInteractionController.CanFertilizeTile`
  để luật "cây ngắn ngày, đã tưới, chưa chín" chỉ nằm MỘT chỗ, popup không đoán lại.
- Mọi nút gọi lại ĐÚNG luồng cũ ngoài ruộng (`HandleWater`/`HandleHarvest`/`BeginFertilize`) —
  không có nhánh logic thứ hai để lệch số liệu hay lách kiểm tra.
- Tưới và Bón **đóng popup** (một cái có màn múa, một cái mở túi đồ); Thu hoạch **giữ popup mở**
  để thu liền tay nhiều cây trong cùng mảnh.
- Cuốc đất / gieo hạt CỐ Ý không đưa vào: danh sách chỉ liệt kê cây đang có, ô trống không có gì để chọn.

### `PenMoveController` dùng chung cho chuồng / ruộng / đường
Lớp này vốn đã thao tác trên `BuildSurfaceCell` chứ không dính gì tới rào, nên chỉ cần thêm
`SubjectLabel` là dùng lại được cả ba. **Giữ nguyên tên lớp** để khỏi lệch với tài liệu cũ.
- `Begin(cells, label)`; mọi câu gợi ý/thông báo ("Đặt … ở đây", "Hủy dời", "Đã dời …") lấy chữ từ label.
- ⚠️ **Bẫy đã bịt: khoá nhật ký của cây tính theo VỊ TRÍ** (`FarmTile.HistoryKey`). Dời ruộng mà
  không đổi khoá là lịch sử tưới/bón/thu của mọi cây thành mồ côi. `Confirm()` nay chụp khoá trước,
  đọc khoá sau rồi gọi `FarmActivityLog.RemapOwners`.
- `FarmActivityLog.RemapOwners(map)`: dọn rác ở khoá ĐÍCH **trước**, rồi mới đổi khoá tại chỗ —
  ngược thứ tự là xoá nhầm chính nhật ký vừa chuyển sang (dời ngang 1 ô thì khoá cũ của ô này
  trùng khoá mới của ô kia).

### Dời ruộng
- `BeginMovePlot(seed)`: loang ra cả mảnh bằng `FindPlotTiles`, map từng ô sang `BuildSurfaceCell`.
  Cây đi theo vì model cây là **con của ô đất**. Không tốn/hoàn vật liệu (ruộng vốn free).
- Ruộng đời cũ (`TilePlacementSystem`) không có ô nền → **từ chối thẳng**, thà không cho dời còn
  hơn dời xong mất cây.
- Vào bằng mục "Dời ruộng" (phím M) trên bảng gợi ý, hoặc nút trong popup.

### Dời đường lát đá — TỪNG VIÊN
Bản đầu bé làm nhấc cả đoạn đường liền nhau cho giống chuồng; anh bác ngay: *"dời từng viên thôi,
cả đoạn thì khó dùng"* — đúng, nhu cầu thật là nắn lại một viên đặt lệch chứ không phải bốc cả lối đi.
- `BeginMovePath` lấy **đúng bộ ô của công trình đang chỉ** qua `BuildSurfaceCell.FindAllByOccupant`
  (thêm mới; khác `FindByOccupant` chỉ trả ô đầu). Viên chiếm nhiều ô thì vẫn đi trọn bộ.
- `PenEnclosure.FindConnected(seed, predicate)` vẫn giữ (tách ra từ `FindPen`) nhưng đường **không**
  dùng tới. Vật liệu đã tốn giữ nguyên nên phá sau vẫn hoàn đúng đá.

### Chữ nghĩa phân bón nói theo PHẦN TRĂM
Anh chốt: mô tả và toast nói *"giảm 15% thời gian"* thay vì *"chín sớm hơn 3 giờ 36 phút"* — dễ hiểu
hơn với người chơi, và vẫn đúng vì cây ngắn ngày đều 24 giờ nên 3,6 giờ chính xác là 15%.
- Toast + dòng nhật ký: phần trăm **tính ra từ chính giống cây đang bón** (`bonusSec / growthTimeSec`),
  không gõ cứng → đổi `fertilizerBonusHours` trong Inspector là câu chữ tự đúng theo.
- Mô tả tĩnh (`fertilizer_01.asset` + `ItemDataGenerator` + `ItemUsageHint`) ghi thẳng 15%; đổi
  `fertilizerBonusHours` thì nhớ sửa kèm 3 chỗ này.

**Files:** `FarmActivityLog.cs`, `PenMoveController.cs`, `PenEnclosure.cs`, `BuildSurfaceCell.cs`,
`FarmInteractionController.cs`, `AnimalInteractionPopupController.cs`,
`AnimalInteractionPopup.uxml`, `Styles/AnimalInteractionPopup.uss`,
`ItemUsageHint.cs`, `ItemDataGenerator.cs`, `Resources/Items/fertilizer_01.asset`

---

## [2026-07-30] — LÀM THẬT tính năng bón phân (đảo ngược mục ẩn phía dưới)

Khách gửi thông số ngay sau khi bảo ẩn: *"kho bán sản phẩm cho vật nuôi ăn chưa có phân bón,
thêm vào luôn, giá 1 point. Mỗi lần bón phân tăng trưởng cây 15%."* → mở lại và làm thật.

- `FarmTile.ApplyFertilizer(percent)`: **dời `growStartTime` về quá khứ** đúng `percent × tổng
  thời gian lớn`. Chọn cách này vì `growStartTime` VỐN ĐÃ được lưu ra đĩa/server và VỐN ĐÃ dùng
  để tính bù offline (`RestoreSave`) → hiệu lực phân tự sống sót qua lưu/tải/đóng app mà KHÔNG
  phải thêm trường mới ở 3 chỗ. Cây lâu năm thu xong đặt lại `growStartTime` nên phân tự hết
  hiệu lực theo từng vụ. Chỉ bón được cây ĐANG LỚN (Watered); chưa tưới thì không có gì để đẩy.
- Luồng dùng: mục `Bón phân` (phím B) ở bảng gợi ý ô đất → mở túi tab Đồ dùng → chọn Phân bón.
  Theo đúng mẫu `pendingFeedAnimal`. Bón hụt thì HOÀN lại phân. Ghi vào nhật ký (`KindFertilize`,
  khôi phục lại) và hiện ở hàng "Lịch sử bón phân" trong popup Xem ruộng.
- Mở lại khỏi `HiddenItems`, trả về shop Vật phẩm + Hai Lúa, **thêm vào Mini Garden** (đúng cửa
  hàng khách mô tả). Giá mua 50 → **1**.
- ⚠️ **BẮT BUỘC phải sửa kèm: `sellPrice 25 → 0`, `canSell → false`.** Giá mua 1 mà vẫn bán lại
  được 25 là lỗ hổng mua-rẻ-bán-đắt **in tiền vô hạn** — khách chỉ nói giá mua, không nói giá bán.
- `fertilizerGrowthPercent` để ở SerializeField (0.15), đổi số khỏi sửa code.

### ✅ ĐÃ CHỐT LẠI CÙNG NGÀY — trừ THỜI GIAN CỐ ĐỊNH, chỉ cây NGẮN NGÀY
Bản đầu cộng 15% của TỔNG thời gian mỗi lần → `1/0.15 = 7` lần là cây chín ngay, **bất kể 24 giờ
hay 90 ngày**. Chanh leo thành bỏ 7 Point ăn 570 Point (gấp 81 lần), xoá sạch 180 ngày chờ của 2 vụ.
Đã báo, khách chốt: **trừ một lượng thời gian CỐ ĐỊNH, và CHỈ áp lên cây ngắn ngày.**

- `FarmTile.ApplyFertilizer(bonusSeconds, maxGrowthSec)` + `IsFertilizable(maxGrowthSec)` — rút
  thẳng số giây, không nhân phần trăm. Cửa lọc dùng `currentCrop.growthTimeSec` (số gốc của giống)
  chứ KHÔNG dùng `GetGrowthTime()`: hàm kia bị tutorial ép về 24s và đổi theo vụ tái sinh của cây
  lâu năm — dựa vào nó thì trong tutorial cây nào cũng lọt cửa "ngắn ngày".
- Hai SerializeField: `fertilizerBonusHours = 3.6` (đúng 15% của cây 24 giờ, giữ nguyên ý số khách
  đưa ban đầu) và `fertilizerMaxCropDays = 1`. Cây ngắn ngày đều đúng 86400s, nhóm kế tiếp 172800s
  nên ngưỡng 1 ngày tách sạch, không cần thêm trường vào CropDefinition.
- Mục "Bón phân" trên bảng gợi ý và nút trong túi đều ẩn/từ chối với cây không đủ điều kiện.

**Vì sao cách này đóng được lỗ hổng:** 8 cây ngắn ngày đều `canSell=false` (thức ăn chăn nuôi,
khách chốt 22/06) nên bón nhanh KHÔNG quy ra tiền trực tiếp. Cây bán được tiền (chanh leo, sầu
riêng, sa chi) nay không bón được. Bón vẫn có thể làm cây ngắn ngày chín ngay (7 lần × 3.6 giờ >
24 giờ) nhưng chỉ rút ngắn vòng làm THỨC ĂN — vòng tiền thật vẫn kẹt ở chu kỳ vật nuôi.

---

## [2026-07-30] — Ẩn hẳn phân bón (dời sang bản sau)

Khách chốt: bón phân để bản sau mới làm (khi có thông số). Ẩn hết dấu vết cho tới lúc đó.
Nhắc lại: phân bón hiện là món hàng VÔ DỤNG — không có code nào áp nó lên cây, mô tả
"Giảm 50% thời gian sinh trưởng" là lời hứa suông.

Shop và túi đồ vốn ĐÃ lọc qua `HiddenItems`, nhưng còn 3 chỗ lọt:
- **Hòm thư** — thư "Đền Bù Sự Cố" mẫu tặng "Phân bón siêu tốc" x3, và hòm thư KHÔNG lọc
  qua HiddenItems như shop/túi. Đổi thành Thuốc trị.
- **Loadout test** (`InventoryManager`) phát 300x phân — nằm chết trong kho và còn bị đẩy
  lên server qua QueueInventoryDelta. Đã bỏ.
- **Shop asset + generator** (`Shop_ItemShop`, `Shop_HaiLua`) vẫn liệt kê. Đã gỡ khỏi cả
  2 file .asset lẫn `ShopDataGenerator`, để nếu sau này ai bỏ HiddenItems thì nó cũng
  không lòi ra. Cùng lúc gỡ khỏi danh sách shop mock của `ShopPopupController`.

`HiddenItems` GIỮ nguyên entry làm lớp chặn cuối. `fertilizer_01.asset` và dòng trong
`ItemDataGenerator` GIỮ nguyên — người chơi cũ đang có phân trong kho, xoá khỏi ItemDatabase
là hỏng dữ liệu của họ; ẩn đi thì nó chỉ nằm im.

Đã bỏ `FarmActivityLog.KindFertilize` (không còn ai dùng). Muốn làm lại bản sau: thêm lại
hằng số + một dòng `LogRow` ở `AnimalInteractionPopupController.RefreshCropLog`.

⚠️ CHƯA đụng `server/shopCatalog.json` (vẫn còn phân bón) — client không chào bán nữa nên
không lộ, và sửa server thì phải deploy. Dọn cùng lần deploy tới.

---

## [2026-07-30] — Bỏ hẳn chữ nổi kiểu name-tag, công tắc chuyển vào Cài đặt

Khách chốt: BỎ hẳn kiểu hiện giờ trên đầu cây như bản cũ; cách xem chính thức là BẤM VÀO CÂY
(popup Xem ruộng). Thanh nước giữ nguyên. Công tắc bật lại thì cho vào Cài đặt, kèm cảnh báo,
để ai máy khoẻ tự cân nhắc.

- Gỡ nút `BtnLabels` khỏi sidebar HUD (uxml + controller + uss) — sidebar đỡ chật một nút.
- Thêm `ToggleCropLabels` vào Cài đặt > ĐỒ HOẠ, ngay dưới Bóng đổ (cùng nhóm hiệu năng),
  mặc định TẮT, kèm dòng cảnh báo `.settings-hint` (lớp USS mới): rối mắt + máy yếu dễ giật.
- `FarmLabelVisibility` không đổi logic, chỉ đổi nơi bật — FarmTile/FarmAnimal vẫn đọc cờ
  mỗi khung hình nên gạt công tắc là thấy ngay, không cần thoát Cài đặt.

**Nhật ký có lưu server không?** CÓ, tự động. Mọi loại mới (tưới/thu hoạch/chữa bệnh/vắc-xin)
đều nằm chung khoá `YW_FarmActivityLog` đã nối vào farm-state ở commit trước, nên không phải
nối lại gì. Ước tính ~43KB ở mức cắt hiện tại (400 mốc + 50 lần chết), trần gói là 480KB.

---

## [2026-07-30] — Nhật ký đầy đủ: tưới, thu hoạch, chữa bệnh, vắc-xin + thư báo cây chết

Mở rộng nhật ký theo yêu cầu khách. `FarmActivityLog` đổi từ "chỉ ghi cho ăn" sang kho
thao tác chung: `LogEntry{ownerId, kind, detail, unixTime}`, ownerId = animalInstanceId (thú)
hoặc `FarmTile.HistoryKey` (cây, khoá theo vị trí — cùng cách với khoá lưu trạng thái).

| Ghi cái gì | Ghi vào đâu | Hook |
|---|---|---|
| Cho ăn | từng con vật | FarmInteractionController (chỗ cho ăn) |
| Thu hoạch thú | từng con vật | `FarmAnimal.HarvestProduct` |
| Chữa bệnh | từng con vật | `FarmAnimal.Heal` |
| Tiêm vắc-xin | từng con vật | `FarmAnimal.Vaccinate` |
| Tưới nước | từng cây | `FarmTile.InteractWater` + `WaterAgain` |
| Thu hoạch cây | từng cây | `FarmTile.InteractHarvest` |
| Thú chết / **cây chết** | hòm thư | `DieFromHunger` / `DieFromDrought` |

- Dữ liệu nhật ký cũ (chỉ có cho ăn) TỰ CHUYỂN sang định dạng mới lúc nạp, không mất lịch sử.
- Gieo cây mới trên ô cũ thì XOÁ nhật ký cây cũ, kẻo mở ra thấy lịch sử của cây đã nhổ.
- Popup "Xem ruộng" nay BẤM VÀO CÂY để xem nhật ký của chính cây đó.
- Các hàng nhật ký dựng động (thú 4 hàng, cây 2 hàng); popup tự làm mới 4 lần/giây nên chỉ
  dựng lại hàng khi đổi đối tượng, còn lại chỉ thay chữ — tránh lặp lại lỗi giật vừa sửa.
- **Gộp thư báo chết**: cùng tên + cùng nguyên nhân trong 10 phút thì dồn 1 thư kèm số lượng.
  Không gộp thì cả ruộng khát nước chết cùng lúc là hòm thư nhận vài chục thư một lúc.

**CHƯA làm được: lịch sử bón phân.** Game hiện KHÔNG có thao tác bón phân nào để mà ghi —
`fertilizer_01` chỉ tồn tại như món hàng trong shop, không có code áp dụng lên cây, và đang bị
`HiddenItems` ẩn khỏi shop/túi. Đã đặt sẵn `FarmActivityLog.KindFertilize` để cắm vào khi nào
tính năng bón phân được làm.

---

## [2026-07-30] — Chống giật chữ nổi + popup "Xem ruộng"

Khách gửi ảnh farm chữ đè kín màn hình: *"nhìn như đám rừng không biết đường tưới nước luôn,
với lại những con số này nó chạy bị lag game"*. Hai vấn đề, vấn đề thứ hai (GIẬT) là mới.

**Chống giật — gốc: dựng lại chữ mỗi khung hình cho MỖI ô**
- `FarmTile.UpdateCropInfoLabel` + `LateUpdate` (thanh nước) và `FarmAnimal.UpdateInfoLabel`
  trước đây mỗi khung hình đều: `GetComponentsInChildren<Renderer>()` (cấp phát mảng mới),
  dựng chuỗi trạng thái, gán `.text` (dựng lại mesh chữ), đọc `mesh.bounds`. Vài chục ô ×
  60 khung/giây = ngập rác bộ nhớ -> khựng.
- Nay cập nhật theo NHỊP 0.25s (số chỉ đổi theo phút nên thừa mượt); chỉ gán `.text` khi chữ
  THẬT SỰ đổi; cache `Camera.main`. Vị trí/xoay vẫn mỗi khung hình, bỏ thì nhãn giật khi xoay cam.
- Mỗi ô lệch pha ngẫu nhiên lúc tạo, kẻo cả ruộng cùng tới nhịp một khung hình rồi dồn cục.
- QUAN TRỌNG: thanh nước KHÔNG dính nút ẩn chữ, nên nếu chỉ sửa phần chữ thì ẩn đi vẫn giật.

**Popup "Xem ruộng" (ý anh Nhiên: làm giống chuồng thú)**
- Thêm mục `Xem ruộng` (phím Q) vào bảng gợi ý của ô đất — song song với `Xem chuồng` của thú.
  Click vẫn LÀM VIỆC LUÔN (cuốc/gieo/tưới/thu), không bắt qua popup.
- `FindPlotTiles` — loang 4 hướng gom mọi ô đất liền nhau, viết theo `FindNearbyPlowedTiles`
  (giữ chặn lệch lưới + chặn khác tầng/đảo) nhưng lấy CẢ ô đang trồng, không giới hạn số ô.
- `AnimalInteractionPopupController.ShowPlot(List<FarmTile>)` — DÙNG CHUNG popup của chuồng thú
  (cùng là "cụm ô liền nhau, liệt kê thứ bên trong"). Khỏi dựng popup mới, KHỎI SỬA SCENE.
  Chế độ ruộng: ẩn bảng thông tin/nhật ký/hàng nút của thú, nới khung danh sách 160 -> 320px,
  tiêu đề đổi thành "Ruộng", dòng trạng thái tóm tắt "x chín · y cần tưới".
- Thẻ mỗi cây: ảnh nông sản + tên + trạng thái (dùng lại `GetStatusText`, gộp xuống dòng thành " · ").
  Ô con của giàn không tính riêng. CHỈ ĐỌC — chưa có nút tưới/thu trong popup.

---

## [2026-07-30] — Ẩn thông số trên farm + nhãn con vật + nhật ký cho ăn/chết

Khách phản ánh chữ nổi trên cây ("Lớn 50% · chín ~11 giờ 55 phút") làm mất vẻ đẹp của farm.
Yêu cầu: ẩn đi, có nút bật lại; khi bật thì hiện cả giờ cho ăn / giờ chết / giờ thu trứng-thịt
của con vật; kèm nhật ký cho ăn và thông báo con chết.

**Ẩn/hiện chữ nổi**
- `FarmLabelVisibility.cs` (MỚI): cờ toàn cục, lưu PlayerPrefs `YW_ShowFarmLabels`, mặc định TẮT.
- `FarmTile.UpdateCropInfoLabel` tôn trọng cờ. Thanh nước KHÔNG dính cờ — vẫn hiện để liếc phát
  biết cây khát (khách chốt: chỉ ẩn CHỮ, giữ thanh).
- Nút `BtnLabels` trên sidebar HUD. Dùng CHỮ thay icon ("Hiện" / "Ẩn") nên tự nói lên nó làm gì,
  khỏi đoán icon; sáng xanh khi đang bật. Bấm KHÔNG đóng popup đang mở.

**Nhãn thông tin con vật (làm mới — trước đây con vật chỉ có thanh đói, không có chữ nào)**
- `FarmAnimal`: thêm nhãn TextMesh billboard dựng y hệt nhãn cây, nằm ngay trên thanh đói.
  Nội dung: cữ cho ăn kế · còn bao lâu chết đói · vụ trứng/sữa kế · bao giờ được thịt.
- Getter mới: `GetTimeToStarveSec()`, `GetTimeToNextFeedSec()`, `GetTimeToMeatSec()`.
  (Thịt chỉ ra ở VỤ CUỐI nên = thời gian vụ kế + số vụ còn lại × chu kỳ.)
- Nhãn là CON của con vật -> con bị xoá thì nhãn đi theo, không sót rác trong scene.
- `ComputeAutoBarHeight` bỏ qua nhãn, không thì nhãn tự đo chính nó rồi trôi cao dần.

**Nhật ký (`FarmActivityLog.cs` — MỚI)**
- Lưu qua `PlayerScopedPrefs` (theo tài khoản), tự cắt bớt: 10 mốc cho ăn/con, trần 300 tổng, 50 lần chết.
- Tự nạp lại khi đổi tài khoản (nhớ scope lúc nạp) — không đưa nhầm nhật ký người này cho người kia.
- Lịch sử CHO ĂN -> popup từng con vật (khối "Lịch sử cho ăn", 5 mốc gần nhất).
- Con CHẾT -> HÒM THƯ (khách chốt: con chết rồi thì không bấm vào đâu mà xem được nữa).
  Đọc/xoá thư ghi ngược lại nhật ký nên mở lại không bị hiện như thư mới hay lòi lại thư đã xoá.
- Làm thịt vụ cuối KHÔNG báo chết (đó là kết thúc bình thường), chỉ dọn mốc cho ăn.

**Nhật ký lên server (bổ sung cùng ngày)**
Ban đầu nhật ký chỉ lưu máy -> cài lại game thì CON VẬT còn (đã sync server) nhưng nhật ký
của nó trắng trơn, nhìn như lỗi. Đã cho nhật ký đi kèm farm-state:
- `FarmStatePayload` thêm `activity_log_json`; `FarmStateSync` gói kèm lúc gửi và ghi lại lúc
  nhận snapshot (kèm `InvalidateCache()` để không xài nhật ký cũ còn trong RAM).
- KHÔNG đổi server, KHÔNG thêm API, KHÔNG migration: `compareAndSetFarmState` ở CẢ hai kiểu lưu
  (Postgres `state_json` jsonb và file JSON) đều gộp kiểu `{...current, ...incoming}`, route
  `PUT /player/farm-state` không lọc trường -> trường mới tự được lưu và trả về nguyên vẹn.
- KHÔNG thêm lượt gọi mạng: lúc cho ăn đã gọi `SaveBuildState()`, lúc chết đã gọi
  `SaveRuntimeState()` — nhật ký đi ké chuyến đó.
- `ContentScore` CỐ Ý bỏ qua nhật ký (đã ghi chú trong code): điểm đó đo "farm có nội dung thật"
  để chọn bản khi gộp dữ liệu cũ; tính nhật ký vào thì farm trống mà từng cho ăn sẽ bị chấm là
  có nội dung rồi đè mất bản thật.
- Dung lượng: mức cắt sẵn (10 mốc/con, trần 300, 50 lần chết) ≈ 30KB, xa trần 480KB của gói.

---

## [2026-07-30] — Dựng khung nhạc nền theo đảo (Nông trại / Thành phố)

Yêu cầu: nhạc nền đổi theo từng đảo (Nông trại khác, Thành phố khác), âm lượng vẫn chỉnh
được qua popup Cài đặt như cũ. Đây là DỰNG KHUNG — chưa có file nhạc thật, khách sẽ gửi sau.

- `AudioManager.cs`: thêm `PlayMusicForIsland(islandId)` — quy ước tên clip `bgm_<islandId>`
  (`bgm_farm`, `bgm_city`, `bgm_mine`), tự tải từ `Resources/Audio/`. Thêm crossfade ~1.2s khi
  đổi bài (fade cũ ra rồi fade mới vào) cho đỡ giật cụt, huỷ được nếu đổi đảo liên tục.
- `IslandTravelManager.cs`: gọi `PlayMusicForIsland` ngay lúc khởi động (đảo bắt đầu, không
  fade) và mỗi lần `TravelToAsync` xong (có fade).
- `Resources/Audio/README_AUDIO.txt`: cập nhật quy ước tên file + xoá ghi chú cũ sai
  ("slider Cài đặt còn TODO" — thực ra đã wire xong từ trước, không cần đụng gì thêm).
- CHƯA cần làm: chỉ cần thả `bgm_farm.mp3`/`bgm_city.mp3` (và `bgm_mine.mp3` nếu cần) vào
  `Assets/Resources/Audio/` là có tiếng ngay, không phải sửa code.

---

## [2026-07-30] — Thêm giá mua 10 nông sản/vật liệu + nước tưới miễn phí

Khách chốt giá mua: cỏ voi 8, bí ngô 11, bắp ngô 4, bắp cải 4, cà rốt 4, dưa hấu 4,
rau muống 5, khoai lang 9, đá 2, gỗ 8 Point; nước tưới miễn phí.

Đây là mảnh còn thiếu của bản vá hôm qua ([[2026-07-29] Chặn lỗi hạt giống & thức ăn]
bên dưới) — toast báo "hết thức ăn/hạt thì mua ở Farm Shop" nhưng trước đó
KHÔNG shop nào bán các nông sản này (chỉ bán được hạt giống, muốn có thức ăn ngay
phải tự trồng).

- 8 nông sản ngắn ngày (`carrot_01`...`grass_01`): thêm `buyPrice`, **vẫn giữ
  `canSell=false`** — mua được nhưng không bán lại, đúng luật "thức ăn chăn nuôi
  không bán" chốt 22/06.
- **Mini Garden đổi từ SellOnly sang Both**: mở thêm tab Mua cho 8 món trên. Theo
  yêu cầu, đã bỏ 8 món này khỏi danh sách BÁN của Mini Garden (trước đó có ghi tên
  trong whitelist sell nhưng vô hiệu vì `canSell=false` — dọn cho khỏi rối).
- Item Shop: thêm mua `wood_01` (8), `stone_01` (2), `watering_water_01` (0 — free).
  Giá bán đá hiện tại 12 trong ItemDatabase KHÔNG tạo lỗ hổng ăn chênh lệch: rà lại
  toàn bộ 8 shop asset + scene, không có shop nào (kể cả legacy mock) đang cho bán
  lại đá — `sellPrice` đó là số liệu chưa gắn vào đâu.
- "Đây khoai lang" trong yêu cầu khách khớp tên `sweet_potato_01` (nông sản "Khoai
  lang"), không phải hạt giống `sweet_potato_seed_01` ("Dây khoai lang") — áp giá
  cho đúng vật phẩm nông sản.

➜ `Assets/_Project/Scripts/Editor/ItemDataGenerator.cs`,
`Assets/_Project/Scripts/Editor/ShopDataGenerator.cs`,
10 file `Assets/Resources/Items/*.asset`,
`Assets/_Project/Data/Shops/Shop_ItemShop.asset`, `Shop_MiniGarden.asset`

---

## [2026-07-29] — Chặn lỗi hạt giống & thức ăn TỰ MỌC LẠI

Khách báo ở tài khoản `green farm rill1`: hạt cải / hạt cà rốt / hạt bắp dùng hết về 0 thì
**tự quay lại 3**, lặp đi lặp lại.

### Gốc lỗi
`FarmInteractionController.EnsureStarterSeeds()` — đoạn trợ giúp thời demo. Mỗi lần người chơi
bấm vào ô đất để gieo, nó rà đúng 3 hạt đó, hạt nào `<= 0` thì `AddItem(s, 3)`. Đây là **hạt
vô hạn**: trồng hết → bấm ô đất → có lại 3 hạt → trồng tiếp, không bao giờ phải mua.
Nặng hơn: `AddItem` đẩy delta lên server (`QueueInventoryDelta`) nên số hạt **bên server cũng
phồng theo**, không chỉ là lỗi hiển thị ở máy người chơi.

`EnsureStarterFeed()` y hệt, cho **thức ăn vật nuôi**: mỗi lần bấm con vật để cho ăn, hết món
chính/phụ là được cấp bù `amount × 3`. Khách chưa báo nhưng cùng một lỗi.

### Đã sửa
Bỏ cả hai đoạn phát đồ, thay bằng lời nhắc:
- Hết hạt (xét **cả nhóm `seeds`**, không riêng 3 loại) → *"Hết hạt giống rồi — ra Farm Shop mua thêm để trồng."*
- Hết thức ăn → *"{Tên con vật} cần {món chính} hoặc {món phụ} — trồng thêm hoặc mua ở Farm Shop."*

Không sợ kẹt người chơi: tài khoản mới vẫn được 5 hạt cà rốt (server `postgresStore.js`), và
hết sạch thì vẫn chặt gỗ / đập đá / câu cá bán lấy Point mua hạt.

➜ `Assets/_Project/Scripts/Environment/FarmInteractionController.cs`

---

## [2026-07-29] — Túi đồ: bỏ nút "Vứt bỏ", nút "Sử dụng" biết giải thích công dụng

Yêu cầu chủ dự án: bỏ hẳn "Vứt bỏ" ở mọi tab; "Sử dụng" mà không có việc gì để làm thì
hiện toast nói món đó dùng vào việc gì.

### Bỏ "Vứt bỏ" (mọi tab)
Nút này **chưa bao giờ hoạt động** — handler chỉ `Debug.Log`, không hề gọi `RemoveItem`, nên
người chơi bấm tưởng vứt rồi mà đồ vẫn còn. Không làm nó chạy thật vì: đây là game kinh tế
(Mai rùa 11.893 Point, Nhung hươu 12.368) nên lỡ tay là mất trắng; túi lại **tự nới slot** khi
đầy (`InventoryManager`) nên chẳng bao giờ có nhu cầu vứt; đồ thừa đã có shop thu mua.
➜ `btnDetailDiscard.style.display = None`, xoá luôn handler chết.

### "Sử dụng" — có việc thì làm việc, không thì giải thích
Nút này là **cơ chế duy nhất** để gieo hạt / cho thú ăn / thả thú / dùng vé, nên KHÔNG thay
bằng toast được. Cách làm: `InventoryPopupController.LastItemUseHandled` — handler nào thật sự
làm việc thì bật cờ; hết lượt phát mà cờ vẫn `false` thì túi bắn toast công dụng.
Cờ đặt ở phía túi (không phải `FarmInteractionController`) để **màn Thành phố** — nơi không có
controller đó — vẫn có lời giải thích.

Kèm theo: đang chờ gieo mà bấm nhầm món không phải hạt thì nay **nhắc rõ**, trước đây bỏ qua
lặng lẽ nên người chơi tưởng nút hỏng.

### `ItemUsageHint` — câu chữ SINH TỪ DỮ LIỆU, không gõ tay
Mô tả sẵn trong `ItemDefinition` là chữ tả cảnh ("Rìu gỗ đốn củi.", "Buồng chuối chín.") và
**đã hiện ngay trên nút** trong panel chi tiết → toast lấy lại chuỗi đó thì vô nghĩa. Nên sinh
câu mới từ dữ liệu, kèm số thật:

| Nhóm | Câu sinh ra | Nguồn |
|---|---|---|
| Hạt giống | "trồng ở ruộng, sau 1 ngày thu Cà rốt x1" | `CropDatabase` |
| Nông sản ngắn ngày | "thức ăn chăn nuôi — cho Heo (2), Bò (4) ăn" | tra ngược `AnimalDefinition` |
| Cá / sản phẩm / đá quý | "bán ở Siêu thị Cá được 25 Point mỗi cái" | `sellPrice` |
| Gỗ · Đá · Gạch | "vật liệu xây dựng — mở Chế độ Xây…" | tĩnh theo id |
| Dụng cụ | "dùng để chặt cây lấy gỗ. Tự động dùng khi bấm nút tương tác" | tĩnh theo id |
| Con giống | "dựng chuồng (9 ô đất), rồi bấm chuồng để thả vào nuôi. Ăn 2x Khoai lang hoặc 2x Bí ngô" | `AnimalDefinition` |
| Còn lại | dùng `description` sẵn có (mồi câu, phân bón… vốn đã rõ) | `ItemDefinition` |

Khách đổi giá/sản lượng là câu chữ **tự đổi theo**, không có bảng chữ chết phải bảo trì.

⚠️ Bẫy khi tra ngược thức ăn: `AnimalDefinition.foodMainName` là **chữ hiển thị** ("Bắp Ngô" /
"Bắp ngô", "Cỏ Voi" / "Cỏ voi"), không phải id, viết hoa không nhất quán → phải chuẩn hoá
(`Trim().ToLowerInvariant()`) trước khi so. Món ghi kèm nhưng số lượng 0 (vd "Cám") bị bỏ qua.

### Toast bị cắt mất chữ (sửa cùng đợt)
Chủ dự án test thấy toast dài lẹm mất dòng cuối. `ScreenToast.OnGUI` vẽ hộp **cứng 540×56**:
chữ tự xuống dòng nhưng hộp không cao theo, dòng thứ 3 tràn ra ngoài. Từ trước tới nay toast
chỉ là câu ngắn ("Chuồng không đủ chỗ") nên chưa lộ; câu công dụng dài 2-3 dòng thì lộ ngay.
➜ Hộp **cao theo chữ** (`style.CalcHeight`), bề ngang co lại theo màn hẹp (`Screen.width - 40`),
thêm padding 18×12, `wordWrap` khai báo rõ. Tiện thể giữ lại `GUIStyle` thay vì tạo mới mỗi lần
`OnGUI` chạy (nhiều lần / khung hình).

**Files:** `Scripts/Data/ItemUsageHint.cs` (mới), `UI/InventoryPopupController.cs`,
`Scripts/Environment/FarmInteractionController.cs`, `Scripts/Environment/ScreenToast.cs`

**Chưa nghiệm thu runtime** — cần mở túi, bấm "Sử dụng" ở cả 7 tab để đối chiếu câu chữ.

---

## [2026-07-23] — Sửa tutorial chết ở luồng RESUME · thay dữ liệu GIẢ trong UI bằng số thật

### Tutorial không chạy lại sau khi thoát giữa chừng (tài khoản mới)
Triệu chứng: người chơi mới tải game, chơi tutorial dở rồi thoát → vào lại **tutorial im lặng không chạy**.
Không phải cờ `tutorialCompleted` (cờ vẫn `false`, game **vẫn gọi** `StartTutorial()` mỗi lần vào) —
mà là luồng RESUME làm nó chết ngay, chết y hệt nhau mọi lần nên trông như "đã tắt vĩnh viễn".

- **NPC đứng chờ vĩnh viễn.** `GuideNPC.GreetingRoutine` có `while (khoảng cách > 5m) yield` không hạn giờ.
  Luồng thường thì người chơi vừa xuống thuyền là đứng cạnh NPC. RESUME lại thả người chơi về **vị trí đã lưu**,
  có thể cách NPC cả bản đồ → ông lão vẫy tay mãi, tutorial đứng ở bước 0.
  ➜ Thêm `WarpGuideNearPlayerIfFar()`: bắt đầu tutorial mà NPC xa >15m thì **dời ông lão tới cạnh người chơi**
  (bám NavMesh, quay mặt về phía người chơi). ➜ Kèm **hạn giờ 25s** ở màn chào làm hàng rào cuối.
- **NPC chưa có trạm dừng nào lúc bị gọi.** 3 node dựng trong `TutorialManager.Start()`, nhưng RESUME gọi
  `StartTutorial()` ngay trong `GameManager.Start()` — **thứ tự `Start()` giữa 2 script là không xác định**
  (project không cài Script Execution Order). GameManager chạy trước thì `tutorialNodes` rỗng → `StartNode(0)`
  rơi vào nhánh "đã hoàn thành toàn bộ tuyến" và **tự tắt component NPC**.
  ➜ Tách ra `EnsureTutorialNodesBuilt()` (idempotent), gọi từ **cả** `Start()` lẫn `StartTutorial()`.

**Files:** `Scripts/Tutorial/TutorialManager.cs`, `Scripts/Tutorial/GuideNPC.cs`

### UI: bỏ dữ liệu GIẢ, nối vào hệ thống thật
- **Thanh EXP trong Hồ sơ luôn đứng 0%.** HUD truyền `expStr` lấy từ *text của label* (`"120 / 250"`),
  popup `float.TryParse` **fail → 0**, rồi chia cho `maxExp = 100` cứng. ➜ `ProfilePopupController.Show(name)`
  đọc thẳng `ExperienceManager` (`Level`, `ExpInLevel`, `ExpForNextLevel`, `ExpPercent`, "Cấp tối đa" khi đạt 90).
- **Thống kê nông trại là số Random** (mở popup 2 lần ra 2 số khác nhau), ngày tham gia cứng `"02/06/2026"`.
  ➜ Thêm `Managers/PlayerStats.cs` đếm THẬT theo từng tài khoản (`PlayerScopedPrefs`): "cây đã trồng" cộng ở
  `FarmTile` khi gieo, "đã bán" cộng ở `ShopPopupController` khi bán, ngày tham gia ghi lần đầu rồi giữ nguyên.
  Cả hai chỗ đếm gọi **trực tiếp**, không qua event chung (một handler lỗi ở event chung chặn cả chuỗi).
  "Bạn bè" để **0** — chưa có backend thì không bịa số.
- **Heo đất không nối ví.** `playerBalance = 5000f` cứng → gửi tiết kiệm **không trừ Point thật**.
  ➜ Đọc thẳng `EconomyManager.GetPOS()`; gửi = `SpendPOS`, đáo hạn = `AddPOS` (gốc + lãi).
  Bỏ 2 dòng lịch sử bịa. Kỳ hạn đổi từ *1 ngày = 5 giây* sang **ngày thật**, thêm `Test Seconds Per Day` để test.
- **Cài đặt kéo thanh không có tác dụng** (4 chỗ còn `// TODO`). ➜ Nhạc/SFX nối `AudioManager`
  (kéo SFX phát tiếng click nghe thử), zoom camera nối `ThirdPersonCamera.SetUserZoom()` (mới),
  chất lượng hình chỉnh `renderScale` của URP + mip + LOD, bóng chỉnh `QualitySettings.shadows`.
  Tất cả lưu PlayerPrefs và **áp lại lúc vào game**, không chỉ dựng lại thanh trượt.
- Sửa 3 comment lỗi thời (`GameHUDController` "Mockup only", `SettingsPopupController` "mockup values",
  `BuildModeOverlayController` "Mock Data") — code ở đó vốn đã thật.

**Files:** `Scripts/Managers/PlayerStats.cs` (mới), `Scripts/Managers/AudioManager.cs`,
`Scripts/Camera/ThirdPersonCamera.cs`, `Scripts/Environment/FarmTile.cs`, `UI/ProfilePopupController.cs`,
`UI/GameHUDController.cs`, `UI/PiggyBankPopupController.cs`, `UI/SettingsPopupController.cs`,
`UI/ShopPopupController.cs`, `UI/BuildModeOverlayController.cs`

> ⚠️ **Còn là dữ liệu giả (chưa làm, chờ backend):** Bảng xếp hạng · Bạn bè · Hòm thư · Nhiệm vụ ·
> Sự kiện (số nguyên liệu + gói bán) · Quên mật khẩu (luôn báo thành công) · AI chat giả khi chưa nối realtime.

### Hệ bệnh: sếp chốt GIỮ NGUYÊN số khách
Sếp trả lời 23/07: *"số liệu vắc-xin và thuốc là do khách họ muốn, mình cứ làm theo"* → đóng cả 4 câu hỏi,
**không đổi số nào**. 🔒 Hai dòng trông như gõ nhầm — **dê 10 liều thuốc y hệt bò** và **ngỗng 4 mũi vắc-xin**
(con duy nhất mà tiêm phòng lỗ hơn chịu bệnh) — là số khách chốt, **đừng "sửa"**.
Chi tiết + bảng chứng minh: `docs/CAU_HOI_KHACH_CON_TREO.md` §6.

---

## [2026-07-22] — Hệ BỆNH + VẮC-XIN cho thú nuôi · chuyển sang THỜI GIAN THỰC

### Hệ bệnh / vắc-xin (trước đây là khung rỗng)
- **Phát hiện:** `AnimalState.Sick`, `canGetSick`, item `vaccine_01`/`medicine_01`, nút `BtnHeal`/`BtnVaccine` đều CÓ SẴN — nhưng **không dòng code nào cho thú bị bệnh**, nút Vắc-xin **chưa nối handler**, `Heal()` **không trừ item**. Cả vòng chăm sóc chết cứng.
- **Đã dựng:** `FarmAnimal` thêm mốc `vaccineUntilUnix` (hạn vắc-xin) + `sickRefUnix` (mốc ủ bệnh) theo **đồng hồ thật** (sống qua các phiên); `Update()` cho thú **đổ bệnh** khi loài `canGetSick`, hết hạn vắc-xin và ủ đủ lâu (miễn trong Tutorial). **Thú bệnh ngừng ra sản phẩm.**
- `Vaccinate()` = phòng bệnh (từ chối khi đang bệnh) · `Heal()` = chữa bệnh (bỏ 2 tác dụng phụ tạm bợ cũ: tự bật `isVaccinated` và tự làm no).
- **Trừ item thật:** popup nối `BtnVaccine`, bật/tắt nút theo trạng thái, tốn `medicine_01` (chữa) / `vaccine_01` (tiêm), hụt thì hoàn lại. **Bịt lỗ hổng:** `HealAnimal` trong `FarmInteractionController` trước chữa **MIỄN PHÍ** → nay tốn 1 Thuốc; thêm hành động **"Tiêm vắc-xin"**.
- **Lưu/khôi phục:** `BuildPersistence` lưu thêm hạn vắc-xin + mốc ủ bệnh + trạng thái bệnh. Save cũ (thiếu field) đếm ủ bệnh lại từ lúc load → thú đang nuôi không bị bệnh oan ngay khi mở game.
- ✅ **Đã thay số tạm bằng SỐ TÀI LIỆU** (xem mục ngay dưới) — không còn 3 ngày / 7 ngày bịa.

### Số bệnh đổ theo CÔNG THỨC tài liệu (`VatNuoi2.md` + `CachTinh.md`)
Trước đó bé dùng số tạm tự đặt (ủ bệnh 3 ngày, vắc-xin 7 ngày, **bệnh chắc chắn 100%**, thuốc luôn 1 liều).
Rà lại thì tài liệu CÓ đủ 4 cột cho từng con → nay đọc thẳng từ bảng:

| Con | Số ngày nuôi | Hệ số mốc bệnh | Tỉ lệ bệnh | Mũi vắc-xin | Liều thuốc |
|---|---|---|---|---|---|
| Gà mái | 90 | 0.15 | 0.6 | 3 | 3 |
| Bò sữa | 270 | 0.30 | 0.4 | 4 | 10 |
| Heo con | 180 | 0.50 | 0.3 | 4 | 9 |
| Đà điểu | 180 | 0.30 | 0.4 | 4 | 10 |
| Hươu | 360 | 0.15 | 0.6 | 4 | 9 |
| Dê con | 180 | 0.30 | 0.3 | 4 | 10 |
| Thỏ con | 80 | 0.40 | 0.5 | 2 | 2 |
| Ngỗng con | 90 | 0.15 | 0.4 | 4 | 2 |
| Vịt | 45 | 0.15 | 0.6 | 2 | 2 |
| Rùa con | 300 | 0.50 | 0.6 | 4 | 10 |

**Công thức áp vào code:**
- **Mốc phát bệnh = hệ số × số ngày nuôi** (bò: 0.3 × 270 = **ngày thứ 81**), thay cho "3 ngày" cứng.
- Tới mốc thì **gieo xác suất "tỉ lệ phát bệnh"** — thay cho việc bệnh 100%. Gieo **MỘT lần cho cả vòng nuôi**,
  khớp công thức chi phí của `CachTinh` (tiền thuốc chỉ nhân với tỉ lệ bệnh đúng 1 lần).
- **Một mũi vắc-xin phòng = số ngày nuôi ÷ số mũi** (bò: 270 ÷ 4 = **67,5 ngày/mũi**). Đang có vắc-xin che
  đúng lúc tới mốc = **thoát bệnh cả vòng nuôi**.
- **Chữa bệnh tốn đúng số liều theo loài** (bò 10 · heo 9 · vịt 2...), không còn cứng 1 liều. Thiếu thuốc thì
  báo rõ "Cần N Thuốc", không trừ gì.

**Đã sửa 1 lỗ hổng:** `Vaccinate()` trước reset luôn mốc ủ bệnh → tiêm 1 mũi là né bệnh vĩnh viễn. Nay mốc phát
bệnh là điểm **cố định** trong vòng nuôi, tiêm chỉ dời hạn *bảo vệ*.

**Files:** `AnimalDefinition.cs` (+5 field bệnh) · `ItemDataGenerator.cs` (`SetAnimalDisease` × 10 con) ·
`FarmAnimal.cs` · `BuildPersistence.cs` (lưu thêm `sicknessRolled`) · `FarmInteractionController.cs` ·
`AnimalInteractionPopupController.cs`.

⚠️ **2 chỗ tài liệu chưa nói rõ, bé chọn cách hiểu hợp công thức nhất — cần khách xác nhận:**
1. "Thời điểm phát bệnh 0.3" không ghi nhân với cái gì → bé hiểu là **× số ngày nuôi**.
2. "Số lượng thuốc 10" → bé hiểu là **chi phí cả vòng nuôi cho 1 lần bệnh** (vì `CachTinh` chỉ tính 1 lần).

### Thời gian thực
- `GameTimeConfig.SecondsPerGameDay` **60 → 86400** (1 ngày game = 1 ngày thật, đúng khách chốt). Cây/thú vốn đã chạy theo mốc đồng hồ thật nên **lớn-bù / đói-bù offline vẫn đúng**.
- **Hiển thị theo ngày/giờ/phút/giây:** thêm `GameTimeConfig.FormatDuration()` → "3 ngày 4 giờ" / "5 giờ 12 phút" / "2 phút 30 giây" / "45 giây" (lấy 2 mốc lớn nhất cho gọn). Gộp 2 bộ format cũ của cây và thú về dùng chung (nếu không sẽ hiện số vô nghĩa kiểu "40320m").

### ⚠️ VIỆC ANH PHẢI LÀM TRONG UNITY
Chạy lại **2 generator** để số trong `.asset` ăn theo mốc ngày mới:
1. `Y WONDER GREEN FARM/Tools/Generate Crop Data`
2. `YWonderLand/Generate Animal Data`

Generator **load asset cũ rồi chỉ ghi đè field số** → `cropPrefab` / `modelGroundOffset` / `seedlingScale` **giữ nguyên**, không mất model cây. **Không chạy thì cây/thú vẫn giữ số theo mốc 60s cũ.**

### Đã chốt
- **Cây lâu năm KHÔNG tưới thì CHẾT — giữ nguyên (chủ dự án chốt 22/07).** Cửa sổ nước 14 ngày thật, chu kỳ lớn 28 ngày → buộc phải vào tưới giữa chừng, không tưới kịp thì mất cây. Đây là hành vi MONG MUỐN, code hiện tại đã đúng, **không sửa gì**.
- Muốn test nhanh: tạm để `SecondsPerGameDay = 60f` rồi chạy lại 2 generator, xong đổi lại 86400.

---

## [2026-07-22] — Vốn khởi đầu tài khoản mới = 0 Point (khách chốt)

- **Khách chốt 22/07:** tài khoản mới bắt đầu với **0 Point**. Con số `5000` cũ trong `EconomyManager.LoadBalances` chỉ là **số tạm lúc dựng thử**, không phải khoản tặng của khách (chính nó gây hiểu nhầm ở Câu 18). Đã đổi mặc định về `0`.
- **Khuyến mãi thật (khách xác nhận):** 10 USDT/tháng = **265 Point** qua **nhiệm vụ + điểm danh**; tiêu bình thường trong game nhưng **KHÔNG trả hoa hồng**.
- **Chờ kiểm thử:** đầu game còn chơi được từ 0 Point không (tutorial cho +50 Point + hạt/dụng cụ khởi đầu).

---

## [2026-07-22] — Ẩn 8 cây lâu năm khách CHƯA chốt số liệu (khỏi shop + túi đồ)

### Tính năng theo yêu cầu
- **Ẩn tạm hạt giống + sản phẩm của 8 cây lâu năm chưa chốt số liệu** (theo `SoLieu_CanKhachChot.md` mục 2): Chuối, Dừa, Cau, Chà là, Trà, Măng tây, Hồng sâm, Sâm tiến vua. 3 cây đã chốt (Sa Chi / Sầu Riêng / Chanh dây) GIỮ nguyên.
- **Cách làm (không xoá data):** thêm `Assets/_Project/Scripts/Data/HiddenItems.cs` — 1 `HashSet` chứa 16 ID (mỗi cây 1 hạt + 1 sản phẩm) + `IsHidden(id)`. Cắm filter ở 2 điểm hiển thị: `ShopPopupController.RefreshGrid()` (1 dòng `continue` lọc cả grid MUA lẫn BÁN) và `InventoryPopupController.RefreshGrid()` (lọc mọi tab; gieo hạt dùng chung tab seeds nên cũng ẩn theo).
- **Bật lại khi khách chốt:** chỉ cần bỏ 2 ID (hạt + sản phẩm) của cây đó khỏi `HiddenItems.Ids`.

### Files
- `Assets/_Project/Scripts/Data/HiddenItems.cs` (mới) · `Assets/_Project/UI/ShopPopupController.cs` · `Assets/_Project/UI/InventoryPopupController.cs`

---

## [2026-07-22] — SỬA LỖI: không cho thú ăn được → thú chết đói

### Lỗi
- **Bấm "Cho ăn" mở túi, chọn "Sử dụng" thức ăn nhưng KHÔNG có gì xảy ra** — không cho ăn, không trừ thức ăn, con vật đói dần rồi chết. Nguyên nhân: `FarmInteractionController` chỉ đăng ký nghe `InventoryPopupController.OnItemUsed` **một lần trong `Start`**. Nếu lúc đó chưa tìm thấy popup thì đăng ký **hụt vĩnh viễn**; 3 chỗ mở túi (cho ăn / gieo hạt / thả thú) có tìm lại popup khi field null nhưng **không đăng ký lại**, nên khi bấm "Sử dụng" thì sự kiện bắn ra **không ai nghe**. 
- **Cách sửa:** thêm helper `EnsureInventoryPopupSubscribed()` — tìm popup nếu null + đăng ký **idempotent** (`-=` rồi `+=`) — gọi ở `Start` và **trước mỗi lần `ShowAtTab(...)`** (food/seeds/animals). Nhờ vậy sửa luôn cả gieo hạt và thả thú (cùng lỗi tiềm ẩn).

### Files
- `Assets/_Project/Scripts/Environment/FarmInteractionController.cs`

### Đổi kèm
- **Cho ăn rút còn ~1.5s:** trước dùng `FeedActionSpeed = 2f` (khóa 9.1s ÷ 2 = 4.55s). Nay thay bằng `FeedActionDuration = 1.5f`; clip `Feed` 9.1s phát nhanh (~6x) cho múa trọn trong ~1.5s. Cùng pattern với múc/tưới; chỉnh `FeedActionDuration` để đổi.

### Chờ build kiểm thử
- Cho ăn → chọn đúng thức ăn của loài → múa Feed (~1.5s) + trừ thức ăn + giảm đói; hủy thì hoàn; gieo hạt & thả thú qua túi vẫn chạy.

---

## [2026-07-22] — Animation múc nước + bơi-đứng-yên + nút hủy khi múc nước

### Tính năng mới theo yêu cầu khách
- **Múc nước có animation + nút hủy (X):** trước đây bấm "Múc nước" là cộng nước NGAY, không có động tác. Nay `ScoopWater` chạy qua `BeginTimedAction("ScoopWater2", ...)` — cùng luồng với chặt/đào/tưới/cho ăn — nên nhân vật múa clip `ScoopWater2`, quay mặt về ao, cầm **xô múc nước ở tay TRÁI** (`ToolType.WaterBucket` — khác bình tưới tay phải khi tưới), HUD hiện nút hủy (X) + thanh tiến trình. Nước chỉ vào túi **khi múa xong** → hủy giữa chừng không nhận gì. Clip gốc 6.57s được **phát nhanh** để múa trọn trong **~1.5s** (`speed = ScoopWaterClipDuration / ScoopWaterActionDuration`); chỉnh `ScoopWaterActionDuration = 1.5f` để đổi. Thêm `ToolType.WaterBucket` + ô `waterBucketModel` (tay trái) vào `EquipmentManager`; **cần kéo object `WaterBucket` dưới `mixamorig:LeftHand` vào ô mới trong Inspector.**
- **Bơi đứng yên (SwimIdle) riêng:** khi ở dưới nước, trước đây đứng yên hay bơi đều dùng chung clip `Swimming`. Thêm `animSwimIdle = "SwimIdle1"`; giờ ở dưới nước mà KHÔNG kéo joystick (`moveDirection.magnitude <= 0.1f`) thì phát `SwimIdle1`, có di chuyển thì `Swimming`.
- **Độ chìm khi bơi — TÁCH idle và di chuyển:** ban đầu để 1 giá trị `swimSubmergeDepth = 1.6` (nước tới vai) cho cả hai, nhưng tester phản ánh lúc **bơi di chuyển** bị chìm nghỉm nhìn nguy hiểm. Tách 2 ô: `swimSubmergeDepth` (idle, `1.6`, nước tới vai) và `swimSubmergeDepthMoving` (di chuyển). Buoyancy chọn theo `moveDirection.magnitude > 0.1f` — cùng điều kiện phân biệt `Swimming` / `SwimIdle1`. Cả hai kéo chỉnh được lúc Play. Sau đó hạ ô di chuyển xuống `0.7` (nới thang `0.2–2.5`): vì clip `Swimming` là tư thế **nằm ngang**, để `1.2` (vốn tính cho tư thế đứng "ngực ngang mặt nước") thì ngập cả thân; `0.7` cho nổi sát mặt nước.
- **Tưới nước rút còn ~3s:** trước khóa 5.6s (đúng độ dài clip `Watering`). Nay phát nhanh ~1.9x cho múa trọn trong `WateringActionDuration = 3f`; chỉnh số này để đổi thời gian tưới.
- **Trồng cây rút còn ~2s:** clip `Planting` 4.13s phát nhanh cho múa trọn trong `PlantingActionDuration = 2f` (`speed = PlantingClipDuration / PlantingActionDuration`); chỉnh `PlantingActionDuration` để đổi.
- **Thanh EXP hiện SỐ thay vì phần trăm (cho tester dễ test):** nhãn `PlayerEXP` trước ghi `exp.ToString("F2")` (vd `40.00`). Nay ghi `EXP hiện tại / EXP cần` (vd `150 / 250`, đạt cấp 90 thì `MAX`). Lộ `ExpInLevel`/`ExpForNextLevel`/`IsMaxLevel` ở `ExperienceManager`; thêm `GameHUDController.UpdateExpLabel()`; bỏ `SetPlayerEXP(float)` không còn dùng. Kèm sửa: thưởng tutorial trước vẽ giả `20.00` lên nhãn → nay cộng EXP THẬT bằng `ExperienceManager.AddEXP(20)`.

### Files
- `Assets/_Project/Scripts/Environment/FarmInteractionController.cs` — `ScoopWater` → hành động có khóa + hủy.
- `Assets/_Project/Scripts/Player/PlayerController.cs` — thêm `animSwimIdle`, tách nhánh bơi/đứng-yên.

### Chờ build kiểm thử
- Build EXE/APK mới: múc nước phát clip mới + có nút X (hủy = không nhận nước); đứng yên dưới nước ra `SwimIdle1`, bơi ra `Swimming`.

---

## [2026-07-22] — Đến gần cây/đá tự hiện nút (tia mũi chân, tàng hình như nước)

### Tính năng mới theo yêu cầu khách
- **Vấn đề:** ở chế độ direct-tap (mobile, mặc định), chặt cây/đào đá phải **tap trúng** vật thể — khác với ruộng/chuồng/nước vốn tự hiện nút khi **đến gần** nhờ tia mũi chân tàng hình.
- **Làm:** thêm `RefreshFootResourceInteractionPrompt` + `FindHarvestableNearFoot` (quét `OverlapSphere` ngay trước mũi chân, y hệt `FindWaterSourceNearFoot`) → đứng gần cây/đá là tự hiện nút **Chặt cây / Đào khoáng**, không cần ngắm. Nút dùng lại `ClickHarvestResource` nên hành vi giống hệt tap vào cây.
- **Ưu tiên:** ruộng/chuồng (front-cell) > nước > cây/đá — probe mới không giành nút của mấy cái kia.
- **Chỉnh số:** `resourceFootProbeForward` (1.1) + `resourceFootProbeRadius` (1.1) ở SerializeField (anh chỉnh tầm chiếu tùy ý). Cây/đá đã cạn tự bỏ qua (collider tắt + guard `isHarvestable`); đá chỉ hiện ở nơi cho đào.
- **File:** `Assets/_Project/Scripts/Environment/FarmInteractionController.cs`. Thêm cờ `currentPromptFromFootResource`, nối qua mọi điểm reset + `HasDirectTapPrompt`.
- **Chờ:** build EXE/APK — đến gần cây/đá thấy nút hiện; tap nút thì chặt/đào; đi xa nút tự ẩn; không tranh nút với ruộng/nước.

---

## [2026-07-22] — Harvest trùng tài nguyên chung cộng EXP ảo (đã vá)

### Cộng EXP + toast thưởng lặp khi tap trùng cây/đá
- Server `realtimeServer.js:467-473`: khi CÙNG người chơi request lại tài nguyên đã hái (retry/tap trùng), server phát lại kết quả cũ (`accepted:true`, rewards gốc) kèm cờ `duplicate:true`.
- Client cũ **không đọc cờ `duplicate`** → `HandleSharedResourceHarvestResult` coi như thành công lần đầu: túi đồ an toàn (server gửi snapshot authoritative, client set-not-add), nhưng **EXP là client-side không idempotent → mỗi lần phát lại cộng EXP + toast thưởng ẢO**.
- **Fix:** thêm `bool duplicate` vào `ResourceHarvestResult` (`RealtimeClient.cs`) + parse ở `ReceiveResourceHarvestResult`; `HandleSharedResourceHarvestResult` khi `duplicate` chỉ đồng bộ lượt đào rồi `return`, KHÔNG AddEXP/toast.
- **Files:** `Assets/_Project/Scripts/Realtime/RealtimeClient.cs`, `Assets/_Project/Scripts/Environment/FarmInteractionController.cs`.
- **Chờ:** build EXE/APK — tap trùng cây/đá không phồng EXP, không toast lặp; túi đồ vẫn đúng.

### Rà lại các exploit kinh tế cũ (QC 22/06) — đã ổn ở source hiện tại
- Phá chuồng: chống trả con giống 2 lần bằng `destroyedAnimalObjects`.
- Thú chết (`SlaughterForMeat`/`DieFromHunger`) đều `ClearAnimal()` nhả ô → không kẹt ô.
- Chuồng legacy `AnimalPenSpawner` đã fail-closed.
- 2 điểm gia cố hover (`isHarvestable`, dọn `currentHoverObject`) đã được collider-disable che → không sửa.

---

## [2026-07-21] — Sửa lỗi cây cộng gỗ hai lần (prefab) + phá Build Mode không hoàn vật liệu

### Phá công trình bằng nút Delete không hoàn vật liệu (đã vá)
- Phá chuồng/hàng rào có hoàn gỗ/đá (`FarmInteractionController.cs:2974`), nhưng nút Delete trong Build Mode (`BuildModeOverlayController.DeleteBuildingAt`) xóa công trình mà không hoàn — lát đường tốn 4 đá, xóa đi mất trắng. Không phải exploit (chỉ thiệt người chơi) nhưng bất nhất.
- Fix: thêm `BuildSurfaceCell.SumRefund(occupant, out wood, out stone)` (đọc mọi ô occupant chiếm, không đổi state); `DeleteBuildingAt` cộng dồn rồi `inv.AddItem(..., "build_refund")` trước khi xóa ô. Ruộng miễn phí (BuildMaterialId rỗng) hoàn 0. `InventoryManager.AddItem` đã đồng bộ delta lên server qua `GameplayMutationSync` nên hoàn đối soát được. Chờ nghiệm thu runtime.

### Cây cộng gỗ hai lần (prefab)

- `Assets/_Project/Prefabs/Tree.prefab` chứa hai `HarvestableResource` cho cùng một cây (root cấu hình đúng `axe_01`/`wood_01`, child `Log` để rỗng). Vì `RealtimeClient.RegisterSharedResource` sinh network resource ID theo hierarchy path khi `resourceId` rỗng, một cây vật lý tạo ra hai network ID và backend có thể thưởng gỗ một lần cho mỗi ID.
- Fix (chỉ sửa `Tree.prefab`, không đụng scene): xóa component root (fileID `4668956307432685771`) khỏi `m_Component` và định nghĩa; điền cấu hình đúng cho component `Log` (đã sẵn BoxCollider + mesh): `requiredTool: axe_01`, `yieldItemId: wood_01`. Giữ `Log` bỏ root để khớp override sẵn có trong CityScene.
- Re-audit 21/07: FarmScene có 4 cây (inherit prefab → tự động còn một component sau fix), CityScene có 14 cây đã được xóa component root từ 20/07 (không bị ảnh hưởng, stale `m_RemovedComponents` vô hại). Verify tĩnh: prefab còn đúng một `HarvestableResource`, YAML nguyên vẹn 13 object.
- Chờ nghiệm thu runtime trên build EXE/APK mới. Chưa đụng: `FarmInteractionController` lạc trên child `Log` và các điểm gia cố client (`isHarvestable`, xóa hover, cờ `duplicate=true`).

## [2026-07-20] — The Memento Protocol cho bàn giao AI

- Thêm `docs/THE_MEMENTO_PROTOCOL.md`: chuẩn bàn giao giữa hai phiên AI, gồm thứ bậc nguồn sự thật, nhãn bằng chứng, ranh giới bảo mật, checklist đóng/mở phiên, Memento Packet và prompt copy sang chat mới.
- Thêm `docs/MEMENTO_PACKET_CURRENT.md`: snapshot đã điền của Y WONDER GREEN FARM, phải được Git/live-verify lại trước khi dùng làm cơ sở thao tác.
- `RULES.md` bắt buộc đọc protocol khi tiếp tục/bàn giao; `docs/CONTEXT_RECOVERY.md` được chỉ rõ là snapshot hiện hành, còn root `CONTEXT_RECOVERY.md` là nhật ký lịch sử cần đối chiếu.
- Mở rộng `server/postgresSmokeTest.js` để schema tạm áp migration `007_point_source_ledger` và kiểm idempotency đồng thời, conflict, FIFO, chặn lô chưa phân loại, persistence qua pool restart và bất biến số dư. Cú pháp/JSON test pass; PostgreSQL runtime cô lập chưa có nên chưa đánh dấu gate PostgreSQL pass.

---

## [2026-07-19] — Chốt tỷ giá, 6 cấp hoa hồng và điều kiện VIP

### Khách đã xác nhận
- Point đổi từ USDT rồi tiêu trong game trả hoa hồng bằng USDT; Point do nuôi trồng, sản phẩm hoặc thưởng game tạo ra rồi tái tiêu dùng trả hoa hồng bằng Point. Tỷ lệ và số tầng dùng như hệ thống YWH hiện tại.
- Hoa hồng Point giữ số lẻ (`156 x 3% = 4,68 Point`), không làm tròn nguyên. Nếu giao dịch đã payout rồi được hoàn/hủy thì phải thu hồi payout gốc. Hướng kỹ thuật là chỉ phát hoa hồng sau khi giao dịch mua commit thành công.
- Nguồn Point được tiêu theo FIFO; hoa hồng USDT dùng rate gốc của từng lô. Transfer giữ nguyên source/rate. Khách sửa tỷ giá áp dụng chung thành `1 USDT = 26,5 Point`, đồng thời chốt `1 YWH = 1,59 Point`; Point do Admin cấp và Point legacy cũng dùng rate `26,5` khi tính căn cứ hoa hồng.
- Hoa hồng có 6 cấp: cấp trực tiếp `8%`, 5 cấp tiếp theo mỗi cấp `1%`, tổng tối đa `13%`. VIP tính cộng dồn toàn thời gian từ `2.650 Point` tiêu dùng có nguồn USDT nạp ngoài. Hoa hồng vẫn vào bể riêng khi A hoặc B chưa VIP, nhưng chỉ được sử dụng/rút khi cả A và B đều VIP; khi đủ điều kiện sẽ mở toàn bộ khoản lịch sử tương ứng.
- Khách sửa lại câu trả lời trước: Point nguồn USDT do người khác chuyển tới vẫn tính VIP cho người nhận khi tiêu; refund phải trừ tiến độ và thu hồi VIP nếu tổng còn lại dưới `2.650 Point`.
- Payout chờ ít nhất khoảng 10 phút hoặc lâu hơn và chỉ mở cho giao dịch thành công; giao dịch lỗi không sinh hoa hồng.

### Nền source-lot local đã thêm, chưa deploy
- Thêm migration candidate `007_point_source_ledger`, module domain và adapter JSON/PostgreSQL để lưu số micro-Point, nguồn `USDT/YWH/GAMEPLAY/ADMIN/LEGACY`, rate tạo Point, rate quy hoa hồng, giao dịch nguồn và lineage khi chuyển khoản.
- FIFO chỉ lập kế hoạch, không đổi số dư; gặp lô `UNATTRIBUTED` thì dừng để chờ phân loại. API ghi lô độc lập chặn transfer vì trừ lô người gửi và tạo lô người nhận phải nằm trong cùng transaction.
- `test:point-source-ledger`, web credit, reservation, security và Phase 1 isolated đều pass. Chưa apply migration PostgreSQL, chưa backfill 5.000 Point/số dư cũ, chưa đổi callback web, shop hay production.

### Gap còn mở
- Source/rate lineage hiện có đã đủ phân loại VIP, không cần bổ sung chủ nạp USDT gốc. Còn chốt ảnh hưởng của việc thu hồi VIP lên các hoa hồng khác đã mở/trả, cách thu hồi nếu hoa hồng đã bị tiêu, độ chính xác thập phân và phí/hạn mức/quy trình rút.
- Backend production hiện chỉ spend `pos BIGINT` nguyên; phần lẻ web mới được carry riêng, chưa spend được và chưa có source-lot active/commission outbox/reversal/VIP pool. Candidate source-lot mới chỉ local, chưa sửa DB/service/feature flag hoặc production. Máy local hiện không có PostgreSQL DSN/runtime nên migration và adapter mới vẫn phải chạy qua schema tạm cô lập trước khi deploy.

## [2026-07-17] — Canary Point không tiền trên QA riêng đã hoàn tất

### Đã thêm
- Thêm executor link QA và funding synthetic fail-closed, kèm test apply/replay/conflict/rollback. Funding dùng loại riêng `QA_SYNTHETIC_USDT_FUNDING`, ghim tỷ giá Admin active và quote integer micros; không giả làm `USDT_DEPOSIT` và không thay tổng tiền nạp/rút thật.
- Thêm script package `test:web-point-qa-link` và `test:web-point-qa-funding`. Tài liệu repo không lưu raw web/game identity.

### Bằng chứng và giới hạn
- Profile riêng `WalletQA2026` được link một-một. Baseline là web USDT `0`, Point game signed `5000`, remainder `0`; funding đúng `2 USDT` synthetic theo tỷ giá `26,5 Point/USDT`, replay cùng operation không cấp lần hai, real deposit count/totals vẫn `0`.
- Chủ dự án thao tác đổi bằng chính UI ví web. Chuỗi có đúng `1` funding journal, `1` SWAP `SUCCESS`, `1` conversion `SENT`, `1` outbox `SENT/attempts=1`, `1` game ledger `+53`; web USDT về `0`, web/game cùng `5053 Point`.
- Replay callback chính xác trả `duplicate=true`. HMAC sai/quá hạn, identity ngoài canary, cùng ID khác amount, sai expected player và amount `0` đều bị từ chối đúng mã, không đổi balance/count; callback public vẫn `404`.
- Nghiệm thu client đạt đủ EXE logout/login, EXE -> APK, APK full close/relogin và APK -> EXE. Cả bốn bước đều hiện `5053`; client cũ bị đẩy và có toast phiên thay thế ở cả hai chiều.
- Backup root-only: `/root/ywonder-point-backups/qa-funding-20260717T090332Z-141ec249`. Report chain/replay/fault: `/root/ywonder-point-reports/point-qa-conversion-chain-20260717T091536Z.txt`, `point-qa-duplicate-replay-20260717T091734Z.txt`, `point-qa-nondisruptive-faults-20260717T092438Z.txt`.
- Phân loại: `technical no-money canary complete`, chưa phải `real-money canary complete`. Không restart production, không dùng tiền thật; debit vẫn tắt, rollout chỉ đúng QA và public internal route vẫn đóng.

## [2026-07-17] — Đã deploy nền ví Point v3 ở trạng thái dormant

### Đã triển khai
- Game đang chạy release `a22312df3aee5701a31aa502d2fea3728546b2b1`; web chạy release `/var/www/ywonder-releases/point-v3-a22312df`, Next.js build `2rdR_xG8o4G1uonGEYEg0`.
- Đã áp schema cộng thêm cho PostgreSQL migration `006_point_wallet_reservations` và journal authority/debit trên SQLite web. Các bảng link/conversion/debit/reservation mới đều có `0` row; không link account và không migrate số dư legacy.
- Cả game lẫn web đều đặt tường minh `WEB_POINT_WALLET_DEBIT_ENABLED=false`. Top-up cũ vẫn chỉ ở canary một account, remote ingress tắt; route credit/reserve public trả `404`.

### Bằng chứng và giới hạn
- Backup root-only `/root/ywonder-point-backups/point-wallet-dormant-20260717T050203Z-a22312df` và rollback validate đều pass; manifest hậu switch có SHA-256 `473bffad9ff2199d27fcb50f02c88f44d8f2a4aafc6088e3481f33427b3ca88f`.
- Trước lần switch cuối, rollback tự động đã chạy thật hai lần vì lỗi riêng của smoke harness: redirect dùng sai hostname TLS và định dạng thời gian `journalctl` không được hỗ trợ. Cả hai lần đều phục hồi release/env/health cũ và giữ nguyên số dư; lần chạy sửa harness nhận ra schema cộng thêm đã tồn tại nhưng rỗng rồi hoàn tất idempotent.
- Hậu kiểm pass health, trang chủ/login/register/referral/wallet, Browser SSO redirect, cron có quyền, route dormant và ổn định tiến trình 45 giây với restart `0`; upload/stage tạm đã dọn.
- Tổng số ví/số dư web, outbox/transaction và tổng Point game không đổi tại checkpoint deploy này. Không dùng tiền thật, không conversion/debit/link/migrate/open/YWH. Canary không tiền trên QA riêng được hoàn tất sau đó và đã ghi ở mục mới hơn phía trên; deployment dormant này tự nó chưa phải bật tính năng ví chung cho người dùng.

## [2026-07-17] — Candidate web debit Point -> USDT đã pass cô lập, chưa deploy

### Đã thêm
- Thêm `GamePointDebit` saga và browser intent bền cho account đã link: ID request/reservation/transaction deterministic, snapshot rate/fee/gross/net bằng micros, state reserve/capture/release, cron retry và gate canary/fee tường minh.
- Chặn race hai chiều bằng application check và trigger SQLite chéo; một account chỉ có một operation ví chưa terminal. Transaction USDT `PENDING` được ghi trước capture nhưng chưa cộng vào số dư dùng được; chỉ settlement SQLite sau `CAPTURED` mới tăng `balanceUsdt` đúng một lần.
- Thêm DB/runtime fault test source cho replay, khác payload, mất response reserve/capture, đổi rate, settlement fail rồi release, sai player, journal bị sửa và remote conflict. Adapter YWH vẫn tắt.

### Bằng chứng và giới hạn
- Local pass: authority safety, Point reservation, Point credit, security, Phase 1 isolated, syntax TypeScript/JavaScript/Bash, migration SQL và patch trên source production partial đã ghim hash.
- Overlay 17 file SHA-256 `2cd12483d811b966f26c3c24db52b2254c6e8a82126ece464e5cdabe8e9e986d` pass full validator VPS trên source/SQLite production bản sao: Prisma generate/validate/migration, DB E2E, Next.js `15.5.20` build `g078J34tOR0M2Gmd1WpIp`, credit runtime fault E2E và debit saga fault E2E.
- Prisma che mã `RAISE(ABORT)` của SQLite trigger thành lỗi FK chung; E2E đã được gia cố bằng SQL trigger, control insert hợp lệ và kiểm row bị chặn không tồn tại. Hậu kiểm: live web/DB production không đổi, service không restart, không dùng tiền thật; production debit vẫn tắt và candidate chưa deploy/link/migrate.

## [2026-07-17] — Khôi phục asset farm/city và hoàn tất rename prefab đất

### Đã khôi phục
- Khôi phục tranche farm/scene bị thiếu từ stash đã giữ lại, không ghi đè City, Map2.1 và material mới của người dùng. Backup trước vá ở `D:\Temp\YWonder-FinalPatch-2026-07-17T02-21-02-185Z`; backup toàn đợt ở `D:\Temp\YWonder-Recovery-2026-07-17T01-51-59-432Z`.
- Hoàn tất rename `dattrong.prefab` thành `DatDaCuoc.prefab` và giữ GUID Unity `ff37f2abcf3e24b46948abf313748dae`. Mở ignore có chọn lọc cho `Assets/Building/Map1.2` để WindMill và dependency mà FarmScene dùng được theo dõi bằng Git.
- Giữ các asset WindMill, đất, prefab, scene, tài liệu và recovery scene đã khôi phục. Không phục hồi thay đổi ProjectSettings và output iOS sinh tự động chỉ có trong stash vì không thuộc nhóm file bị mất và có nguy cơ đè cấu hình mới.

### Bằng chứng
- Đối chiếu đủ 165 asset untracked phía stash: thiếu `0`. Sau khi Unity xóa metadata Addressables sinh tự động, audit còn 2.474 GUID asset: trùng `0`; 273 GUID duy nhất trên 33 file Unity YAML liên quan đều resolve đủ khi tính cả builtin và package metadata.
- Unity `6000.3.15f1` batch import/compile thoát mã `0`; log không có lỗi biên dịch C#, exception, missing reference, lỗi shader/import hoặc crash.
- Unity tự xóa lại `Assets/AddressableAssetsData/link.xml` và `.meta` trong lúc import; giữ deletion vì Addressables đã sinh bản giống hệt theo Android/iOS/Windows dưới `Library/com.unity.addressables`.
- Chủ dự án đã mở và nghiệm thu phần nội dung khôi phục. Các thay đổi CityScene/Map2.1/material phát sinh sau đó thuộc worktree người dùng và không nằm trong tranche web debit.

## [2026-07-17] — Duyệt successor policy ví Point nhưng tiếp tục khóa migration

### Đã thêm
- Thêm policy checksum-pinned `docs/QA/POINT_WALLET_MIGRATION_POST_REMEDIATION_POLICY_2026-07-17.json`, chỉ gồm ba lựa chọn còn hiệu lực sau remediation: opening seed và legacy web balance tiếp tục `DEFER_ACCOUNT_LINK`; zero-balance history dùng `ARCHIVE_HISTORY_WITH_NO_BALANCE_MIGRATION`.
- Policy không cấp quyền ghi DB, deploy, migrate balance hoặc chạy lại synthetic reversal; mọi operational change vẫn phải xin duyệt riêng.

### Bằng chứng
- Full migration/remediation execution suite pass local. Policy SHA-256 là `89fcdc1b22d8ac9d20d5bf4761b5696c4d0503b0962eb7f7bd9f28af6d88aacc`.
- Applicator production được ghim checksum tạo artifact root-only `point-wallet-migration-approved-post-remediation-20260716T173254Z-84042119.{json,md}`, SHA-256 `aa371bd52ef22ef473d390b2d14cf44d53f62f0b23b216f1ef74f02503c96ca8` / `9fa41f3f5d9fa70a6a0957f8d518ad936a3e4dc51efb3e64eb26cf9dd7f00891`: `17/17` decision approved, pending `0`, source blocked `0`, nhưng migration vẫn `BLOCKED_NO_BALANCE_MIGRATION_AUTHORIZED`.
- SQLite SHA-256 `a4f0ffa570071c9799c3c3c915519b65bea434b0bfe886f1a59f2b572ac22467`, PID game/web `186418/186434`, health `200/200`, web root `307` và callback public `404` giữ nguyên. Không deploy/restart/DB mutation/link/migrate/reversal/normalization/tiền thật; source tạm đã xóa.

## [2026-07-17] — Reconciliation ví Point sau remediation đã khóa chống đảo trùng

### Đã thêm
- Game snapshot read-only nay xuất evidence remediation có kiểm tra operation/idempotency, checksum, source ref ẩn danh và phép tính Point/remainder. Report đối chiếu đủ nguồn, tổng micros và cặp apply/rollback; evidence thiếu, trùng, dùng lại, sai account/credit hoặc sai số tiền đều fail-closed.
- Worksheet tách synthetic chưa xử lý với synthetic đã reversal. Chỉ reversal hợp lệ và chưa rollback mới loại quyết định đảo lại; artifact cũ checksum-pinned vẫn giữ tương thích.

### Bằng chứng
- Targeted test, full migration suite và hai remediation execution suite đều pass local; candidate test trên VPS pass trong thư mục tạm không chứa secret rồi được dọn.
- Production double-snapshot chỉ đọc tạo report `point-wallet-migration-post-remediation-20260716T172353Z-c62f14df.json`, SHA-256 `7397b05d18780cc056dea7c3727aedcc80400570d421caa58072c92d4bf6fd2a`: `BLOCKED=0`, residual `0`, ba outbox synthetic tổng `3000000` micros đã reversal và không còn yêu cầu đảo lần hai.
- Worksheet mới có 16 account review và 17 decision: 6 opening seed, 10 legacy balance, 1 zero-balance history; synthetic chưa xử lý và residual pending đều `0`. JSON/Markdown SHA-256 là `e223c85ddd26233f0478ae5f02a2b02b2318d5ee050f0c5343174d843a238e0f` / `5c76c718cc636514ad731afb42bc8f8de2a90723c2508d8cbb06b6224abffc73`.
- SQLite checksum, PID game/web `186418/186434`, health `200/307/404`, file mode `0600` giữ nguyên; không restart/deploy/mutation/link/migrate hoặc dùng tiền thật. Source/raw tạm đã xóa.

## [2026-07-16] — Remediation ví Point đã chạy và đối soát production

### Đã thêm
- Thêm validator approval/checksum, executor SQLite chuẩn hóa residual có audit append-only, executor PostgreSQL đảo synthetic credit bằng transaction serializable/idempotent, nhánh rollback bù và suite test chung. Tooling được khóa ở commit `7aeec648`.
- Hai executor fail-closed khi plan/source/account drift, audit dở dang, idempotency conflict, target còn phiên active, credit sai player hoặc phép tính số dư không khớp; artifact approval không chứa identity thô.

### Bằng chứng
- Local/full migration suite và execution suite pass. Bản sao SQLite/PostgreSQL trên VPS pass apply, replay, rollback bù, phục hồi logical checksum và hậu kiểm. Planner ghim approved artifact cũ dừng bằng `SOURCE_STATUS_COUNTS_DRIFT` sau khi normalization là hành vi đúng; báo cáo nền mới là cổng hậu kiểm và xác nhận `BLOCKED=0`, residual value `0`.
- Backup production `/root/ywonder-point-backups/point-remediation-20260716T163736Z-7aeec648` có manifest SHA-256 `7c5dc5272cc791e1632f1a4738ed3a2b73ef89d4ba6b3544848360fff8977954`. Fresh preflight khớp tuyệt đối 3 account/6 residual, 3 synthetic source và `3000000` micros đã duyệt.
- Production residual normalization tạo đúng `NORMALIZE=6`, `ROLLBACK=0`; replay không ghi thêm, báo cáo mới đưa `BLOCKED 3 -> 0` và residual value `6 -> 0`.
- Ba Point canary không tiền lịch sử đã được đảo đúng một lần sau các chi tiêu gameplay: balance tại thời điểm operation là `4370`, không còn là ảnh HUD cũ `5003`; số dư PostgreSQL hiện là `4367 Point`, có đúng một ledger `delta_pos=-3`, không có rollback ledger và replay idempotent.
- Nghiệm thu client thủ công sau reversal đã đạt. EXE đăng nhập mới hiển thị `4367`; đăng nhập APK khi EXE còn mở đẩy EXE ra và APK vẫn là `4367`; đăng nhập lại EXE đẩy APK ra và EXE tiếp tục là `4367`. Cả hai client bị thay phiên đều hiện toast đúng; không mua đồ hoặc nhận thưởng trong vòng kiểm tra.
- Validation summary cuối có SHA-256 `e31f737bc0177fcceb6fa3f6b1c980a6c5ad379ac4ee7f13352ef50b2e46c37d`. PID game/web vẫn `186418/186434`, health `200/307/404`, outbox pending và warning log đều `0`; không restart/deploy/link/migrate, không dùng tiền thật, không mở mode `open`. Canary vẫn đúng một account, remote ingress tắt; PostgreSQL clone và raw snapshot tạm đã xóa.

## [2026-07-16] — Remediation dry-run ví Point đã sẵn sàng xin duyệt riêng

### Đã thêm
- Planner fail-closed đọc approved artifact đã ghim checksum và snapshot kép hiện tại, đối chiếu lại toàn bộ account/fact/status trước khi lập kế hoạch. Tool xác minh từng source synthetic với đúng một game credit và tính chính xác từng residual signed atto-Point; không sinh SQL hoặc lệnh đổi số dư.
- Reversal synthetic và residual normalization chỉ có operation ID đề xuất, điều kiện vận hành và trạng thái `NOT_AUTHORIZED`. Account link, balance migration và deploy tiếp tục defer/cấm; identity thô chỉ nằm trong temp `0700` rồi bị xóa.

### Bằng chứng
- Syntax, unit/full migration suite, Point authority, credit, reservation và security smoke đều pass local; candidate pass Node `24.18.0`, Python và `bash -n` trên VPS.
- Production tạo JSON/Markdown root-only `point-wallet-remediation-dry-run-20260716T155640Z-4677eae4.*`, SHA-256 `fdd118a89f0ac70a60a96eb587d5f7a89b8dd731f02a9922d96410fda2b65357` / `975342dc9bec62f6166e7ef62d9fb657ff36d78a0f3028f902470320dd279416`: reversal plan đúng `3 Point` từ 3 source; residual có 3 account/6 giá trị; nếu operation residual được duyệt và thực hiện đúng thì projection `BLOCKED 3 -> 0`.
- Lượt candidate tạm đầu tiên dừng trước khi sinh plan vì user game không traverse được thư mục source root-only; cleanup xác nhận raw/upload/output đều 0 và PID/health không đổi. Lượt sau chỉ mở quyền đọc source không nhạy cảm cho service user, còn raw vẫn root-only, rồi pass.
- Hậu kiểm cuối giữ PID game/web `186418/186434`, health `200/200`, web root `307 -> 200`, callback public `404`, critical log `0`, temp `0`. Chưa sửa DB/Point, chưa reversal/normalize, chưa link/migrate, deploy hoặc restart.

## [2026-07-16] — Policy ví Point đã duyệt, migration vẫn khóa

### Đã thêm
- Generator fail-closed biến report HMAC production thành worksheet JSON/Markdown root-only. Tool từ chối identity thô, schema/flag/checksum sai, HMAC ref trùng và không ghi đè output; mọi decision mặc định `PENDING`, không có SQL hay số tiền migrate đề xuất.
- Phân loại riêng phần lẻ legacy dưới micro, opening balance game, synthetic credit không có nguồn web, balance web legacy đã/chưa map và lịch sử legacy balance 0.
- Thêm policy JSON đã được chủ dự án duyệt và applicator fail-closed. Tool bắt buộc checksum đúng, đủ đúng decision key, giá trị thuộc whitelist, không có field/constraint thừa và không được cấp quyền DB/deploy/migration/reversal.

### Bằng chứng
- Unit test và full suite dry-run pass. Hai worksheet mode `0600` chứa 16 account ẩn danh, 21 decision key, tổng opening seed `30000 Point`, synthetic canary `3 Point` và web legacy `3422.666667 Point` trong phạm vi review.
- Generator SHA-256 `923017f8f6574b8d036ca7045a70f2982897aa22d8e8e1d1d00df0d2a812906e`; JSON SHA-256 `d3cf5dbb9d4eabcae3e4c9e9f97e7c0146440d3eb13f9495626bbefb115cf455`; Markdown SHA-256 `8a3f52381c3b4f2008f2e3aceeeead10178ed009c5d82e083738e990928c8cd9`.
- Policy chọn defer account link cho seed/balance legacy, reversal có audit cho synthetic `3 Point`, archive không migrate lịch sử balance 0 và `ROUND_HALF_EVEN` kèm residual audit. Applicator/policy SHA-256 cuối là `84042119984beda631d4c3f9305ce5756998e60ac127778dbafac3d98aa00107` / `ab262f7f532df2bc008ce9e0ca0769a7a8f78cb0b1ad747309b4e09d0af19e63`.
- Artifact approved JSON/Markdown root-only cuối có SHA-256 `76c3368cc5937db5c93d8adbebb23d5abe2f2a8c279dd9f8d1daada5ddc7ca37` / `dd7a6190cdf7e5d8fe4c55a3b59275adb08921173e70eb1330ced3c863355282`: 21 approved, 0 pending, migration gate vẫn `BLOCKED_NO_BALANCE_MIGRATION_AUTHORIZED`. Artifact trung gian trước hardening chỉ giữ làm audit và đã bị supersede.
- Hậu kiểm giữ PID game/web `186418/186434`, health `200/200`, web root `307 -> 200`, callback public `404`, temp `0`. Không gọi DB, không deploy/restart/đổi balance, không link/migrate, chưa reversal hoặc normalize residual.

## [2026-07-16] — Production migration dry-run ví Point đã chạy, chưa được migrate

### Đã thêm
- Exporter web SQLite chỉ đọc (`mode=ro`, `query_only`, foreign-key check) và exporter game PostgreSQL `REPEATABLE READ READ ONLY`; dữ liệu tiền dùng integer micros, không dùng float để quyết định migration.
- Báo cáo ẩn danh từng account phát hiện mapping/transaction trùng, identity mồ côi, thiếu wallet, balance âm/locked, outbox chưa settle hoặc lệch game ledger. Report luôn cấm auto-migration và không sinh SQL/số tiền đề xuất.
- Runner chụp hai ledger hai lượt, dừng nếu dữ liệu đổi giữa lúc capture, chỉ giữ raw ID trong temp `0700` rồi xóa; file cuối dùng HMAC ref và quyền `0600`. Runbook nằm tại `docs/QA/POINT_WALLET_MIGRATION_DRY_RUN.md`.
- SQLite REAL legacy được làm tròn về micro gần nhất chỉ trong snapshot tạm, giữ residual chính xác bằng atto-Point và gắn blocker cho mọi account bị ảnh hưởng; outbox settlement vẫn từ chối sub-micro. Runner hỗ trợ PostgreSQL peer auth bằng cách hạ riêng game exporter sang user service và loại HMAC key/`PGPASSWORD` khỏi child.

### Bằng chứng
- Suite migration dry-run, Point authority/reservation/credit, security, Phase 1 cô lập và animal placement đều pass; Bash syntax và `git diff --check` pass.
- Lượt production read-only `2026-07-16T13:48:26Z` từ commit `7dffc8b5` đã pass snapshot kép và sinh report SHA-256 `3ef6343bbef65bcfd35bce78aab10408df24c71c3c5fa90acd800788f3f14f16`: 159 account gồm `143 NO_ACTION`, `7 UNMAPPED_LEGACY_REVIEW`, `6 MANUAL_RECONCILIATION_REQUIRED`, `3 BLOCKED`.
- Ba outbox canary, mỗi row `1 Point`, đều `SENT` và khớp đúng một game credit; tổng gửi/nhận `3 Point`, không duplicate. Ba blocker chứa 6 giá trị SWAP/ví legacy dưới 1 micro-Point; 6 mapping game đều có opening seed `5000 Point` cần phân loại.
- Hậu kiểm giữ nguyên PID game/web `186418/186434`, health `200/200`, release/build/env checksum và callback public `404`; upload/raw temp đều `0`. Không restart, không sửa DB/mapping/balance, không dùng tiền thật và chưa được phép migrate.

## [2026-07-16] — Candidate ví Point authoritative v3, chưa deploy

### Đã triển khai cô lập
- Chốt ADR dùng `player_economy.pos` trong PostgreSQL game làm số dư Point spendable duy nhất cho account đã link. Web đóng băng Point legacy của account link ở `0` và đọc balance ký HMAC; account chưa link phải đối soát từng trường hợp, tuyệt đối không cộng dồn hàng loạt.
- Thêm route nội bộ ký HMAC `point-reserve/capture/release` sau kill switch riêng mặc định tắt. Reserve trừ đúng một lần, capture không trừ lại, release hoàn đúng một lần; retry cùng payload idempotent và conflict fail closed.
- Thêm migration `006_point_wallet_reservations.sql`, state machine JSON/PostgreSQL, realtime balance absolute và test restart/race.
- Overlay web v3 dùng rate `USDT_POINT` do Admin tạo thành version bất biến, tính bằng integer micros và ghim `rateVersionId/rateMicros/roundingRemainder` vào conversion journal. Retry sau đổi rate vẫn dùng quote đã commit.

### Bằng chứng
- Các suite reservation, credit, security, authority, phase-one cô lập và animal placement pass; PostgreSQL smoke pass trong schema VPS tạm đã tự xóa.
- Overlay 13 file có SHA-256 `99a901958f4fb65379c32cf9e128cda9244ffa162f71e85333df635672a6cb2b`; validator pass migration/DB E2E, Next.js `15.5.20` build `S7-EGQjdX4Wv8vxyDOlU8` và fault E2E đổi rate sau commit.
- Chốt `LIVE_WEB_CHANGED=no`, `PRODUCTION_DATABASE_MUTATED=no`, `PRODUCTION_SERVICES_RESTARTED=no`, `REAL_PAYMENT_USED=no`. Còn thiếu web debit orchestrator, migration legacy, YWH conversion/hoa hồng và phê duyệt rollout.

## [2026-07-16] — Ghi nhận hợp đồng nghiệp vụ ví Point từ BA/khách

### Đã xác nhận
- Point web và Point game là cùng loại/cùng một số dư như một ví; USDT -> Point, YWH <-> Point, Point -> USDT và tỷ giá do Admin thay đổi.
- Tiêu dùng game phải trả hoa hồng YWH cho người giới thiệu tương tự HUB, gồm vật nuôi, cây dài/ngắn ngày, mồi câu, lượt vòng quay, lượt đào khoáng và mọi tiêu dùng game.

### Còn chặn triển khai
- Câu `YWH -> Point` và chuyển Point không làm đổi Point game mâu thuẫn với yêu cầu một ví; chưa tự suy diễn. Còn phải chốt ledger authoritative, migration Point/GXL cũ, rate version/rounding, transfer/rút/reversal và công thức hoa hồng YWH.
- Đặt playbook fixed-rate `0,06 USDT -> 1 Point` ở trạng thái HOLD; candidate authority v2 chưa deploy và không được coi là thiết kế ví cuối.
- Sửa bản ghi identity: `Nhien345` là tài khoản thật, không phải QA. Log xác nhận scoped grant block gây `403`/shop lock trên account này; đối chứng `senh2026` nhận nước và mua bình thường, không có `403/409/503`. Chưa đổi cấu hình production.

## [2026-07-16] — Vá client farm, chưa build artifact

### Fixed
- Xác nhận `FarmScene` active map Chuồng sang `Fence.prefab` có `FenceAutoConnect`; ba prefab `AnimalPenSpawner` cũ không được scene/build catalog tham chiếu. Hành vi `7 -> 7` khớp client enclosure cũ chỉ spawn + save farm mà không trừ túi; hai file client atomic vẫn đang là thay đổi chưa commit nên artifact EXE/APK đã test chưa được chứng minh có fix.
- Kết quả placement authoritative nay mang số lượng con giống còn lại từ snapshot inventory server. Unity áp snapshot đó và hiện số còn lại ngay trong toast thành công. Nhánh pen legacy chuyển sang fail-closed, không còn trừ inventory và spawn local bằng hai mutation rời nhau.
- Cây nhiều ô chặn ô slave trước khi mở túi, trừ hạt hoặc chạy animation. Việc chọn ô phụ đổi sang flood-fill 4 hướng theo lưới `BuildSurfaceCell`, nên cụm chanh dây không thể lấy ô đã cuốc rời rạc ở nơi khác trong scene. Mọi ca state sai/đổi giữa chừng/thiếu ô/action bận đều có toast và giữ hoặc hoàn hạt.
- Audit source chi phí xây không thấy bypass theo account Rich: scene active giữ ruộng miễn phí, đường `4 đá`, chuồng `4 gỗ`. Placement nay bắt buộc `RemoveItem` thành công, gắn reason `build_place` và log chính xác số lượng trước/sau để đối soát runtime/DB. Trừ vật liệu và lưu công trình vẫn chưa nằm trong cùng một transaction server.
- Bổ sung entry batch `BuildWindows` và `BuildAndroid` vào build script Unity hiện có; đường dẫn artifact nhận qua `UNITY_WINDOWS_PATH`/`UNITY_ANDROID_PATH` để build lặp lại cùng một source.
- Sửa blocker ở lượt test P0 đầu tiên trước cả bước thả thú. `Player.log` chứng minh backend vẫn khỏe và request shop chưa được gửi: scoped canary trả `403` cho reward item dương, queue đã bỏ mutation lỗi vĩnh viễn nhưng giữ `reconciliationRequired`, nên shop sau đó tự trả `PENDING_STATE_SYNC_FAILED (503)`. Shop nay chỉ khi queue đã rỗng mới dùng bootstrap authoritative có guard để đối soát rồi tiếp tục transaction; queue thật còn pending vẫn fail-closed và không mở lại API delta dương. Toast lỗi đồng bộ/đối soát cũng được tách khỏi thông báo mất kết nối chung.

### Verified
- Roslyn của Unity `6000.3.15f1` compile sạch cả runtime lẫn Editor assembly theo response file Standalone Windows và Android; `Assembly-CSharp` sau hotfix shop cũng compile pass vào output tạm riêng.
- `test:farm-animal-placement`, `test:security`, `test:phase1:isolated` và `test:web-point-authority` đều pass trong tranche này. Vẫn phải build và nghiệm thu runtime EXE/APK; không đổi hoặc deploy service production.

## [2026-07-16] — Candidate web Point authoritative, chưa deploy

### Gia cố định danh và single ledger
- Thêm khóa duy nhất `User.id -> gamePlayerId` ở web. Dispatcher ký `expected_player_id` trong contract `ywonder-point-credit-v2`; game trả `409 GAME_POINT_IDENTITY_MISMATCH` trước khi cộng Point/ghi ledger nếu mapping không tồn tại hoặc lệch. Contract v1 tạm thời vẫn tương thích để nâng release theo thứ tự an toàn.
- Web account đã link đọc Point từ endpoint balance ký HMAC và fail closed nếu `player_id` trả về khác identity đã ghim. Legacy Point web của account link bị giữ ở `0`; account cũ chưa link không bị đổi dữ liệu.
- Bổ sung test từ chối hai web account cùng ghim một game player, sai player trên JSON/PostgreSQL store, response HTTP 200 sai player, duplicate, `409`, mất response sau commit và race callback.

### Bằng chứng cô lập
- Backend integration và authority safety test pass. Overlay 12 file có SHA-256 `8f326cb79e0c8123712aec90217602f2428612cfa6e54d30c42aad3e804cf9fb`.
- Validator VPS pass migration/DB E2E, Prisma, Next.js `15.5.20` build `m31Ry3w4SeOT1N3oxcdJw` và runtime fault E2E; chốt `LIVE_WEB_CHANGED=no`, `PRODUCTION_DATABASE_MUTATED=no`, `PRODUCTION_SERVICES_RESTARTED=no`, `REAL_PAYMENT_USED=no`.
- Harness production-artifact nay chạy thêm `postgresSmokeTest` trong PostgreSQL database tạm riêng. Lượt VPS pass nhánh ghim player trên store thật, canary rejection, first dispatch, duplicate, restart web cô lập và mất response sau commit; database tạm đã xóa, service/player data production không đổi.
- Thêm `docs/QA/WEB_POINT_NO_MONEY_CANARY.md`: cổng còn lại không cần tiền thật là cấp synthetic có audit đúng `0,06 USDT`, thao tác đổi thật trên UI web, rồi đối soát cùng source transaction qua web journal/outbox, game ledger và HUD EXE/APK. Candidate chưa deploy.

## [2026-07-16] — Canary production ví web -> Point không dùng tiền

### Kích hoạt có giới hạn
- Thêm `server/deploy/activate-web-point-canary.sh` với allowlist đúng một web identity QA, backup root-only, ghi env nguyên tử, rollback tự động, health-check và cổng bắt buộc callback public vẫn `404`. Production đang chạy `WEB_TOPUP_MODE=canary`; khóa grant dương từ client chỉ áp dụng cho đúng identity canary, người chơi khác vẫn giữ luồng legacy hiện tại.
- Gia cố script bật bằng lock `flock` dùng chung và chế độ validate-only không đổi cấu hình. Thêm `server/deploy/deactivate-web-point-canary.sh` để đưa hai service về dormant nguyên tử, xóa hai allowlist/scoped block, giữ remote ingress tắt và tự phục hồi env canary nếu restart/health/guard bất kỳ không đạt.
- Browser SSO đã tạo đúng một mapping web account -> `playerId`. Baseline trước test: mọi số dư ví web bằng `0`, outbox web trống, game có `5000 Point` khởi tạo và chưa có ledger top-up.

### Nghiệm thu không tiền
- Sau SQLite online-backup, đã queue đúng một outbox synthetic `+1.000000 Point`, không tạo nạp tiền và không đổi ví web. Cron production có xác thực gửi HMAC qua loopback; outbox thành `SENT`, PostgreSQL thành `5001 Point`, có đúng một `web_topup_credit`.
- Ép retry lại cùng payload: attempts `1 -> 2`, Point vẫn `5001`, ledger vẫn một dòng. Ví web tiếp tục toàn `0`, không có outbox chưa gửi và SQLite `quick_check=ok`.
- Ba ảnh Windows EXE xác nhận HUD là `5,001` ngay khi đang online, sau logout/login và sau thoát hẳn/mở lại app. Đối soát cuối vẫn đúng một mapping, một player và một ledger.
- Android APK khôi phục `5,001`, xác nhận `5,002` sau relogin, rồi nhận realtime riêng `5,002 -> 5,003` ngay trong game. Retry cùng nguồn giữ `5,003` và một ledger; APK relogin/full process restart và lần đăng nhập Windows EXE tiếp theo đều đọc cùng `5,003`.
- Phiên đơn pass đủ hai chiều trên cùng account QA: đăng nhập EXE đẩy APK đang mở ra, đăng nhập lại APK đẩy EXE đang mở ra; cả hai client cũ đều hiện toast phiên bị thay thế. Vì vậy số dư giống nhau xuyên thiết bị không phải do hai phiên cũ cùng tiếp tục hoạt động.
- Restart có kiểm soát riêng game backend làm PID game đổi nhưng giữ nguyên PID web, hash hai env, `5,003 Point`, phần lẻ `0`, ba ledger và ba outbox `SENT`. Health game nội bộ/public cùng web đều về `200`; guard tiếp tục `401/404/401` và service game không có log mức warning.
- Fault drill dùng lại transaction synthetic realtime APK đã credit, không tạo Point mới: khi backend tắt, cron có xác thực giữ row ở `RETRY` và attempts `2 -> 3`; sau khi backend phục hồi, đúng row đó về `SENT` ở attempt `4`. Point vẫn `5,003`, ledger vẫn ba dòng và transaction nguồn vẫn đúng một ledger; tổng attempts outbox chỉ đổi `5 -> 7`.
- Rollback preflight trên VPS pass mà không đổi production: backup dormant root-only dùng default an toàn khi thiếu khóa (`enabled=false`, `mode=canary`), allowlist/block rỗng và env ngoài nhóm canary khớp live. `bash -n` Linux cùng validate-only bật/tắt đều đạt; PID game/web, hash env, số backup và public `404` không đổi. Lượt live được duyệt riêng đã chạy ở mốc bên dưới.
- Anh đã đăng nhập client sau các lần restart/fault có kiểm soát và xác nhận khôi phục đúng số dư authoritative `5,003 Point`.
- Mở rộng E2E không tiền bằng restart web process cô lập và fault proxy chỉ bind loopback. Proxy cho game tạm commit transaction thứ hai nhưng giữ response thành công tới khi web tạm timeout và ghi `RETRY`; lượt gửi lại dùng nguyên transaction ID rồi về `SENT` ở attempt hai, trong khi game tạm vẫn chỉ có đúng hai ledger, không mất hoặc cộng đôi Point.
- Gia cố runner để lấy đúng `WorkingDirectory` đang chạy của `greenxland.service`, không còn vô tình build cây rollback Next.js 14. Runner chỉ chấp nhận root web chuẩn hoặc release versioned an toàn, bắt buộc `.env` active trỏ về env production chuẩn và fingerprint source/build active, PID, service identity cùng env trước/sau.
- Lượt VPS đã sửa build đúng release active Next.js `15.5.20` và pass canary rejection, first dispatch, duplicate retry, isolated web restart, post-commit response loss cùng idempotent recovery. PostgreSQL database tạm, bản sao SQLite, process/stage/upload đều được dọn; PID production game/web giữ `181238/177260`, build `O-PUYMkTlVdeNCYQWp2gJ`, health `200/200`, exact-one canary và callback guard không đổi.
- Hậu kiểm chỉ đọc vẫn là Point QA `5,003`, phần lẻ `0`, ba ledger `web_topup_credit`, ba outbox `SENT`, tổng attempts `7`. Fault run cô lập không đổi ví web, outbox, ledger, service hoặc build production.
- Sau khi được duyệt, production đã chạy đủ exact-one canary -> dormant -> exact-one canary. Lượt tắt tạo backup `/var/backups/ywonder-point-link/deactivate-20260715T204412Z`, đưa callback loopback về `404` và restart hai service; lượt bật tạo backup `/var/backups/ywonder-point-link/canary-20260715T204455Z`, khôi phục hai env khớp byte-for-byte baseline, restart hai service và đưa loopback thiếu chữ ký về `401`; callback public luôn `404`.
- Hậu kiểm độc lập chốt PID game/web cuối `186418/186434`, đúng release/build cũ, health `200/200`, allowlist đúng một identity, scoped block khớp, remote ingress tắt và không có critical log match. Point vẫn `5,003`, phần lẻ `0`, ledger `3`; outbox vẫn `3 SENT / 7 attempts / 0 pending`; database và thư mục E2E tạm đều bằng `0`.
- Hai thư mục backup live thuộc `root:root` mode `0700`; file env bên trong giữ owner/mode production nhưng service account không thể đọc xuyên thư mục cha. Một archive source E2E cũ, không chứa `.env`/secret/dữ liệu người chơi và không process nào dùng, đã được xóa khỏi `/tmp` mà không đụng service hoặc dữ liệu production.
- Nghiệm thu client sau live double restart đã đạt: anh đăng xuất rồi đăng nhập lại và ảnh HUD hiển thị `5,003`. Hậu kiểm chỉ đọc tương ứng thấy đúng một phiên QA active, Point `5,003`, remainder `0`, ba ledger và outbox không đổi `3 SENT / 7 attempts / 0 pending`.
- Hậu kiểm đạt web/game `200`, request loopback/cron thiếu auth `401`, callback public `404`, không warning service. Chưa dùng tiền thật.

### Cổng còn lại
- Canary vẫn chỉ dành cho identity QA đã chọn, chưa bật đại trà. Live dormant -> canary và relogin client sau double restart đều đã đạt. Cổng nghiệp vụ còn lại là giao dịch ví web thật cực nhỏ có đối soát transaction ID đầu-cuối và phải được duyệt riêng; chưa bắt đầu.

## [2026-07-16] — Nâng web production lên Next.js 15 đã vá

### Deploy và rollback
- Đã deploy release versioned `/var/www/ywonder-releases/next15-49ee6f3-hardened` bằng systemd drop-in `next15-release.conf`. Build active `O-PUYMkTlVdeNCYQWp2gJ` chạy Next.js `15.5.20`, React/ReactDOM `19.2.7`; source Next.js `14.2.18` cũ tại `/var/www/ywonder` vẫn sạch và nguyên vẹn để rollback.
- Backup root-only `/var/backups/ywonder-web/next15-pre-20260716T002949+0700` đã pass checksum, gồm SQLite/env/unit/source, build/audit/smoke và rollback script. Commit nguồn backup `b2510d4ad38eef89881931609ede60010ac50a83` có parent `49ee6f3b9f9c1547bb8dbfcfc0dbec6d6502ca24`, đúng 7 file và bundle/patch khớp byte với active release.
- Rollback script, checksum drop-in và bản Next.js `14.2.18` cũ đã pass loopback smoke. Không cố ý restart production thêm hai lần chỉ để drill sau khi upgrade đã thành công; rollback target cũ vẫn sẵn sàng.

### Regression và hậu kiểm
- Build/runtime cô lập đạt home/login/session `200`, cron Point thiếu quyền `401`, Browser SSO callback thiếu request `400`, wallet chưa đăng nhập `307`; `npm audit --omit=dev` có `0 vulnerabilities`.
- Regression đăng nhập production bằng một MEMBER synthetic đạt login `302`, session `200`, wallet authenticated `200`. User cùng wallet/session/account/notification/audit/outbox liên quan đã xóa hết; SQLite integrity và foreign key đều sạch.
- Hậu kiểm cuối đạt web/login/game `200`, cron `401`, top-up public `404`, outbox web `0`, ledger PostgreSQL `web_topup_credit=0`, không còn marker synthetic, process/listener candidate hay warning/error service.
- Top-up web/game vẫn tắt, allowlist/scoped block đều rỗng và `CLIENT_ASSET_GRANTS_ENABLED` vẫn unset/default-true. Không dùng tiền thật hoặc cộng Point. Cổng tiếp theo là tạo/chọn identity QA riêng và xác minh chính xác `User.id -> playerId`.

## [2026-07-15] — Gate an toàn canary ví web -> Point

### Security
- Khóa `PUT /player/inventory` bằng `405 INVENTORY_SERVER_AUTHORITATIVE`; thêm strict mode chặn delta Point/item dương bằng `403` nhưng vẫn giữ debit hợp lệ.
- Production startup từ chối `WEB_TOPUP_ENABLED=true` nếu `CLIENT_ASSET_GRANTS_ENABLED` chưa là `false`.
- Thêm `WEB_TOPUP_MODE=canary` và allowlist web user dùng chung ở web/game. Account ngoài danh sách nhận `425` để outbox tiếp tục retry; cron không lấy các hàng ngoài canary nên không làm nghẽn account thử.
- Thêm khóa grant theo đúng web user canary bằng `CLIENT_ASSET_GRANTS_BLOCKED_WEB_USER_IDS`. Khi vẫn giữ reward legacy cho người chơi thường, danh sách khóa phải khớp tuyệt đối allowlist top-up; endpoint kiểm cả JWT đã ký và mapping player authoritative. Mode `open` vẫn bắt buộc strict toàn cục và danh sách khóa riêng phải rỗng.

### Verified local và VPS
- Pass `node --check`, `test:security`, `test:web-point-credit`, `test:phase1:isolated` và `test:farm-animal-placement`. Audit source xác nhận outbox nằm trong cùng SQLite transaction với SWAP `SUCCESS`; Browser SSO và top-up đều map cùng web `User.id` sang `game_players.web_user_id`.
- Đã gia cố E2E không dùng tiền cho lần chạy VPS kế tiếp: game/web bắt buộc chạy từ stage dưới `/tmp`; PostgreSQL dùng database tạm riêng thay vì schema trong `ywonder_game`; build có cổng RAM/disk/load, `nice`/`ionice`, timeout, lock một lượt và cleanup tiến trình có marker. Chỉ in `PRODUCTION_PLAYER_DATA_MUTATED=no` sau khi xóa database tạm, xác minh test ID không lọt vào hai DB thật, service PID/health và hash source/env production không đổi, callback thật vẫn `404`. `test:web-point-e2e-harness` và Bash syntax pass local.
- Ngày 15/07/2026 runner mới đã pass trên VPS production bằng candidate có SHA-256 cố định dưới `/tmp`, bản sao SQLite web và PostgreSQL database `yw_point_e2e_*` riêng. Canary rejection, gửi lần đầu, retry idempotent và phần lẻ Point đều pass; DB/process/stage tạm được dọn sạch, PID/active time/hash env production không đổi, health vẫn `200` và callback dormant vẫn `404`.
- Test khóa grant canary đã pass startup gate, account canary bị `403`, account đối chứng vẫn nhận reward, debit vẫn chạy và mapping store authoritative thắng claim JWT không thuộc canary. Point-credit, Phase 1, animal placement và E2E harness regression cũng pass; security suite tương tự đã pass trực tiếp từ artifact Linux live.
- Đã deploy backend hardening thành release `f573721533c0a65a3f2fc49fa6a2673b224f8bea` sau backup `/var/backups/ywonder-point-hardening/pre-f5737215-20260715T153753Z`; migration `001`-`005` đã có sẵn, PostgreSQL smoke và health đều pass.
- Follow-up khóa grant scoped đã deploy thành backend release active `32adf45fd4edb4b13b4ac3ed6c1bb69c7afbc2dc` sau backup đã xác thực `/var/backups/ywonder-point-hardening/pre-32adf45f-20260715T161547Z`; PostgreSQL smoke pass, migration `001`-`005` skip đúng.
- Đã deploy guard canary web từ commit `82e20e3ed31dc3d3aea631c7c57cbdc3e9ba8e94` sau build production cô lập. Build live `YVgCFF1Bwu_XJc4sfVpo3`, rollback tại `/var/backups/ywonder-web/point-canary-hardening-20260715T154611Z`; chỉ web restart, game PID và hai env không đổi.
- Hậu kiểm độc lập đạt game/web `200`, cron thiếu quyền `401`, callback `404`, guard canary có trong live build, không còn DB/schema/stage/process E2E, outbox Point web có `0` hàng và hai service không có warning/restart. Không dùng tiền thật.
- Production vẫn cố ý giữ `WEB_TOPUP_ENABLED=false`, allowlist web/game rỗng và `CLIENT_ASSET_GRANTS_ENABLED` chưa set nên positive grant gameplay legacy còn hoạt động. Startup interlock vẫn chặn bật top-up; chưa được chạy canary cho tới khi cô lập đường reward dương hợp lệ và chốt đúng web user/player QA.
- Hậu kiểm release `32adf45f...` đạt health local/public, web PID và env hash không đổi, callback `404`, cron `401`, guard scoped có trong live source, startup gate synthetic pass, outbox/ledger đều `0`, không còn DB/schema tạm và không có warning service. Cơ chế cô lập đã sẵn sàng nhưng scoped list vẫn rỗng cho tới khi chọn QA identity; không dùng tiền thật.
- Audit source Unity chỉ còn một HUD Point, không còn binding UPoint active; tham chiếu runtime còn lại chỉ dùng để xóa PlayerPrefs legacy. `Assembly-CSharp` compile sạch vào output tạm riêng bằng Unity `6000.3.15f1`. Chưa chạy batch build tranh project vì Editor đang mở; EXE/APK visual acceptance vẫn còn.
- Audit identity chỉ đọc thấy 5 mapping web-browser active, đều đã có dữ liệu player và không account nào được đánh dấu QA. Không ghi web ID đầy đủ/secret vào repo. Ưu tiên tạo web account QA riêng; nếu dùng account hiện hữu phải do owner chỉ định rõ trước khi đổi allowlist/env.
- Đã dựng candidate web cô lập từ clean HEAD `49ee6f3b9f9c`, nâng Next.js lên `15.5.20`, React/ReactDOM `19.2.7`, vá PostCSS/form-data và dùng alias Nodemailer `9.0.3` tương thích peer contract của Auth.js beta hiện tại. Candidate chỉ đổi đúng 7 file source/lock/config.
- Candidate đạt `npm audit --omit=dev` với `0 vulnerabilities`, production build, guard route không đăng nhập, credential login/session, trang ví có đăng nhập, referral và smoke API gửi mail trên bản sao SQLite. User/process synthetic đã được dọn; PID/active timestamp của web production không đổi.
- Artifact `/var/backups/ywonder-web/candidates/next15-hardened-49ee6f3b9f9c-20260715T165605Z.tar.gz`, SHA-256 `a31e4192463b9ef75718c851e9e31b3e6ce85543145f2072d2b65d61db122165`, sau đó đã được deploy và nghiệm thu ở release ngày 16/07 phía trên; top-up vẫn tắt.

## [2026-07-15] — Thả vật nuôi nguyên tử với túi đồ và farm

### Fixed
- Thay luồng sinh thú local bằng `POST /player/farm/animals/place`. JSON/PostgreSQL store kiểm revision farm, item trong túi, số ô theo loài, ô thuộc chuồng và chưa bị chiếm; sau đó trừ đúng `1` con giống và thêm đúng một animal do server cấp `instanceId` trong cùng transaction/idempotency.
- Unity flush chuồng mới nhất trước khi thả, áp lại inventory + farm authoritative từ response và không có fallback thả local khi mất mạng. Đã bỏ helper tự cấp thú demo mỗi lần mở túi vì helper này che lỗi số lượng.
- Collider hàng rào đặt bằng Build Mode được ưu tiên nhận diện qua `BuildSurfaceCell`/`PenEnclosure`, không đi nhầm nhánh `AnimalPenSpawner` legacy.

### Verified
- Pass `test:farm-animal-placement`, `test:security`, `test:phase1:isolated`, `test:web-point-credit` và `node --check` các file server liên quan. Unity Editor đã compile lại `Assembly-CSharp.dll` sau thay đổi, không ghi nhận lỗi C#.
- Release production versioned `9c5bd9d1066637b6f176fcd09781358d22a73de4` đã deploy sau backup PostgreSQL/env/unit và PostgreSQL smoke trên schema tạm. Local regression cùng public Phase 1 HTTP/WSS đều pass; các lần thử chưa đạt trước đó đã rollback về release khỏe.
- Double-check từ ngoài VPS: health dùng PostgreSQL, route thả thú không token trả `401`, callback nạp Point public trả `404`; `WEB_TOPUP_ENABLED=false` không đổi. Còn build EXE/APK và test tuần tự số lượng thú, relogin/cross-device/đóng app đột ngột trước khi đổi task sang `[x]`.

## [2026-07-15] — Một ví Point và cổng nhận giao dịch nạp web

### Changed
- Gỡ UPoint khỏi `EconomyManager`, bootstrap/mutation DTO, HUD, popup sự kiện, JSON/PostgreSQL store, fresh schema và test. Cache Point dùng chuỗi 64-bit; migration mở rộng `004_single_point_currency.sql` archive UPoint legacy, không tự quy đổi và tạm giữ cột cũ để rollback release trước an toàn. Chỉ tạo migration contract xóa cột sau khi release Point-only đã deploy/verify.

### Added
- Thêm route nội bộ mặc định tắt `/internal/web/point-credit`: chỉ loopback, HMAC bằng secret riêng, timestamp ngắn hạn, transaction ID bất biến và idempotency. Cộng Point + ghi `web_topup_credit` nguyên tử; player online nhận số dư absolute qua `economy_updated`.
- Test riêng đã pass chữ ký thiếu/sai, request quá hạn, amount sai, cộng đúng, realtime refresh, retry không cộng đôi, conflict khi cùng mã khác payload và persistence sau restart.
- Web production đã có outbox + cron có xác thực để gửi giao dịch Point sang game; giao dịch gửi lỗi giữ trạng thái retry và luôn dùng cùng idempotency key nguồn.

### Deploy và double-check ở trạng thái dormant
- Game release `6e41be4d298ac51b0246583a14759c47ab9b47b8` đã deploy cùng migration `004/005`; PostgreSQL smoke, Point integration và security regression pass. Web outbox/cron cũng đã deploy kèm backup/rollback.
- Hai dịch vụ đã cấu hình cùng secret nội bộ mà không in secret; backup env nằm tại `/var/backups/ywonder-point-link/dormant-20260715T043458Z`. Vẫn giữ `WEB_TOPUP_ENABLED=false` và loopback-only.
- E2E cô lập dùng đúng web build production, bản sao SQLite và PostgreSQL schema tạm đã pass gửi lần đầu, retry không cộng đôi, giữ phần lẻ Point và xác nhận không sửa dữ liệu player production. Sau restart: web/game `200`, cron không quyền `401`, callback public `404`.

### Gate
- Chưa bật tiền thật. Callback/outbox/migration đã deploy và E2E cô lập đã đạt, nhưng bắt buộc khóa các endpoint client còn tự khai delta Point/item trước khi đổi `WEB_TOPUP_ENABLED=true`. Backend regression và biên dịch `Assembly-CSharp` độc lập bằng Unity Roslyn đã pass; còn xác nhận trực quan trên EXE/APK rằng HUD không còn UPoint.

## [2026-07-14] — Hotfix hủy cho thú ăn và vòng đời trực tiếp

### Fixed
- Chặn component `FarmInteractionController` legacy trên các instance `Tree.prefab` chiếm singleton của controller thật trên `FarmManager`. Nút hủy HUD nay đi đúng timed coroutine: dừng thao tác cho ăn, mở khóa nhân vật/cursor và hoàn thức ăn đã giữ, không còn hủy animation nhưng vẫn cho thú ăn.
- Cây và thú nay chết/héo ngay trong phiên đang chơi khi thanh nước/đói về `0%`, kể cả tutorial. Trước khi xóa, code giải phóng ô cây/chuồng và lưu snapshot farm ngay để relog hoặc thiết bị khác không phục hồi vật thể đã chết.

### Verified
- `Assembly-CSharp` compile pass bằng response file do Unity sinh. Cờ serialized auto-sprint cũ được giữ để tương thích prefab nhưng luôn chuẩn hóa về `false`, nên warning `enableStickAutoSprint` đã được dọn mà không bật lại chạy nhanh bằng joystick. Còn chờ Editor/EXE/APK runtime acceptance trước khi chốt `[x]`.

## [2026-07-14] — P0 phiên đơn và chống ghi đè farm đã deploy production

### Fixed
- Mỗi lần đăng ký/đăng nhập local, web credential hoặc Browser SSO exchange nay tạo một `sessionId` mới, lưu phiên active theo player và đưa `sid` vào JWT. Token cũ bị REST từ chối bằng `401 SESSION_REPLACED`; WebSocket cũ bị đóng mã `4008` ngay khi phiên mới được cấp, không cần đóng/mở lại app.
- Thêm `POST /auth/logout` để thu hồi đúng phiên hiện tại. Unity đóng/reconnect realtime khi token hoặc player scope đổi; phiên bị thay dùng logout không-save để không đẩy snapshot farm cũ lên server.
- `PUT /player/farm-state` chuyển từ last-write-wins sang compare-and-set nguyên tử theo `expected_version`. Server tự tăng revision và trả `409 FARM_STATE_CONFLICT` kèm snapshot authoritative khi client cũ cố ghi đè.
- `FarmStateSync` lưu pending snapshot thành outbox bền vững theo `playerId`, không lưu token. Mất mạng/đóng app giữ outbox cho lần login sau; upload thành công chỉ xóa đúng payload, còn `409` phục hồi snapshot server.
- Thêm migration `003_active_player_sessions.sql` và schema cột phiên active cho PostgreSQL. JSON store cũng giữ phiên active và chặn đăng ký trùng trong tình huống hai request đồng thời.

### Verified
- `node --check`, full Phase 1 JSON smoke, realtime smoke, `test:security`, `test:browser-auth` và `test:web-auth` đều pass. Regression xác nhận token cũ bị `401`, socket cũ bị `4008`, logout thu hồi token, farm stale write nhận `409` và không thay snapshot mới.
- Unity Editor không ghi nhận `error CS` mới sau lần compile cuối. Máy dev không có `POSTGRES_TEST_DATABASE_URL`; PostgreSQL smoke cô lập sau đó đã pass trong đợt deploy production có kiểm soát.
- Anh đã build/test lại EXE/APK và xác nhận hai lỗi buổi sáng hoạt động ổn: phiên mới thay phiên cũ ngay khi app cũ vẫn mở, và đổi tuần tự EXE -> APK cùng account không còn phục hồi snapshot farm/cây/tưới cũ. Checkpoint nghiệm thu artifact test: `21cc20d2`.

### Production deployment
- Đã backup PostgreSQL/env/systemd unit và deploy versioned release `21cc20d2a827e5327429cf5f0ecf67a6b67fdf79`; migration `003_active_player_sessions` áp dụng thành công, release trước vẫn được giữ làm rollback.
- PostgreSQL smoke trong schema tạm và public Phase 1 REST/WSS smoke đều pass: rotate/revoke session, token cũ `401`, socket cũ `4008`, farm stale write `409`, persistence/relogin. Account smoke đã được dọn khỏi DB.
- Kiểm tra độc lập từ Windows xác nhận health `storage.mode=postgres`; chỉ `80/443` public, còn `3000/5432/8080` đóng.

### Nghiệm thu client sau deploy
- Đã đăng nhập mới trên EXE/APK chứa checkpoint `21cc20d2` và pass thay phiên ngay khi app cũ vẫn mở.
- Ca A → B → A giữ đúng bố cục farm, đất đã cuốc, cây/nước, xây dựng, túi đồ, chuồng/thú và thời gian bù; không còn rollback về cache cũ.
- Ca đóng app đột ngột cũng pass: outbox retry đúng khi đăng nhập lại và server giữ snapshot authoritative. Cổng P0 đồng bộ tài khoản/farm trên production được chốt `[x]`.

## [2026-07-13] — Hotfix đồng bộ nông trại xuyên thiết bị

### Fixed
- Xác định lỗi `Thu2026` không phải hai tài khoản khác nhau: EXE và APK dùng cùng player server nhưng mỗi máy tự đọc bốn cache farm cục bộ, còn `farm_state` PostgreSQL trước đây bị Unity bỏ qua khi bootstrap.
- Thêm `FarmStateSync` để gộp/lưu/khôi phục ô đất, cây và mốc thời gian, công trình, chuồng và vật nuôi qua endpoint `PUT /player/farm-state`. Snapshot server được áp lại cho bốn hệ persistence và dựng lại runtime sau login.
- Chặn thiết bị chưa bootstrap ghi cache rỗng lên server; migration legacy ưu tiên nâng từ snapshot ít nội dung sang snapshot nhiều nội dung hơn. Đây chưa thay thế conditional revision/`409` ở backend.

### Verified
- `Assembly-CSharp` compile thành công; chỉ còn warning auto-sprint cũ không liên quan.
- Phase 1 smoke riêng pass snapshot đủ bốn phần qua save -> relogin -> bootstrap. Còn phải build lại cả EXE/APK và nghiệm thu hai chiều bằng cùng account trước khi chuyển task sang `[x]`.

## [2026-07-13] — Hotfix chọn tài khoản website trước khi vào game

### Fixed
- Logout trong game chỉ xóa phiên game, còn trình duyệt có thể vẫn giữ phiên NextAuth/Auth.js. Callback cũ tự approve phiên web đã nhớ nên người chơi bị đăng nhập lại tài khoản gần nhất dù muốn đổi account.
- Callback website nay luôn yêu cầu thao tác tường minh khi đã có session: `Tiếp tục với tài khoản này` hoặc `Đăng nhập tài khoản khác`. Chỉ nút tiếp tục mới approve request và cấp game session.
- Luồng đổi tài khoản chỉ hết hạn các cookie session-token của Auth.js/NextAuth rồi quay về `/vi/login` với callback cũ; đã bỏ query `locked=1` từng làm web hiện sai cảnh báo “Tài khoản đã bị khóa”. Các trang callback/redirect dùng `no-store` để tránh tái sử dụng kết quả xác thực cũ.

### Verified
- Web callback commit `cac56e0f` đã deploy có backup/rollback; build production `Q_dfxErFS68Q3YBChCShT`, backup `/var/backups/ywonder-web/browser-callback-cac56e0f1e1258e3f7a0cc269e78c7b9dc9d740e-20260713T072713Z` và service đều khỏe.
- Probe production xác nhận không còn cờ khóa giả, có 8 cookie session được expire và response `no-store`. Nghiệm thu trình duyệt thật đã pass `callback -> approve -> PKCE exchange -> bootstrap PostgreSQL`, trả `PLAYER=p_1783873094`, profile `Lam`, Point `5000`.
- Local/web credential vẫn chạy song song; không sửa DB, Nginx hay Unity trong hotfix này. Còn smoke test cùng luồng trên chính EXE/APK bàn giao trước khi đóng client artifact.

## [2026-07-13] — Hotfix đăng ký website và quay lại EXE

### Changed
- Editor Browser SSO đã tạo nhân vật và relogin đúng cùng web account; EXE đầu tiên vẫn polling đúng nhưng không tự đưa cửa sổ game lên trước.
- Commit `c002dfa0` thêm Windows foreground restore sau browser exchange; thao tác click taskbar vẫn là fallback nếu Windows chặn focus.
- Callback web đã deploy bản chỉ sửa callback, có backup/rollback. `intent=register` lần đầu luôn trả `302` sang `/vi/register`, sau đăng ký/OTP mới quay về callback có `registration_completed=1`, tránh tự duyệt session cũ.
- Public redirect probe đã pass. Còn build lại EXE, test account web mới bằng mã giới thiệu hợp lệ, APK deep link và relogin/cross-device.

## [2026-07-13] — Nấc B Browser SSO public backend/web accepted

### Added
- Game-server có request Browser SSO lưu PostgreSQL, PKCE, expiry 10 phút, approve nội bộ bằng server secret và exchange dùng một lần; không đưa password/cookie/secret web vào Unity.
- Web Next.js giữ `callbackUrl` xuyên login/register/OTP và có `/api/game/browser/callback`; APK được đánh thức bằng `ywondergreenfarm://auth/complete`, còn EXE nhận kết quả qua polling.

### Verified
- Hotfix release `fc23f1652a8e484b42e348150d3a5a038825a2e0` tách polling exchange khỏi quota thử mật khẩu. Probe public `125` lần pending đều pass và phiên Browser SSO thứ hai vẫn start `201`; Unity cũng biết chờ `Retry-After` thay vì dừng ngay khi gặp `429`.
- Game release `f75a7d6b3c5c267fbdf17f58af7d02bdecf8d5b9` mở thẳng callback, còn session chưa login tự đi qua `/vi/login` rồi quay lại callback. Hành vi approve ngay session đã ghi nhớ ở checkpoint này đã được thay bằng bộ chọn tài khoản tường minh của commit `cac56e0f` phía trên.
- Web build `hp9br9UtY4p9PcC214CmF` deploy thành công; service active, login `200`, callback chưa login `302`. Backup rollback: `/var/backups/ywonder-web/browser-sso-20260712T185239Z`.
- Game release `67ff2565517875fcc48ea515f1fedbbf98f24b8a` deploy versioned, migration `002_browser_auth_requests` và backup DB/env/unit pass; vẫn giữ `WEB_AUTH_MODE=http`, `AUTH_TRANSITION_MODE=parallel`, local registration bật.
- Public acceptance account web thật pass `start -> callback -> approve -> exchange -> bootstrap PostgreSQL`; profile `TRAN TUNG LAM`, Point `5000`. Không in/lưu password hoặc token.
- Unity đã bật feature flag Browser SSO cho bản build tiếp theo. Chờ runtime Editor, EXE/APK và flow đăng ký mới + OTP trên thiết bị thật.

---
## [2026-07-12] — Nấc A web-auth chạy song song (production accepted)

### Added
- Audit chỉ đọc cùng VPS xác nhận web Next.js chạy cổng `3033`, source `/var/www/ywonder`, API `/api/game/auth`, stable `session.user.id` và secret 64 ký tự đã có trong env nhưng không bị in/ghi vào repo.
- Thêm `AUTH_TRANSITION_MODE=parallel` cho game-server. Chỉ cấu hình tường minh này mới cho phép web auth HTTP hoạt động cùng đăng ký local; cấu hình thiếu/sai bị production gate từ chối.
- Mở rộng integration test để chứng minh account local vẫn đăng ký/đăng nhập và giữ playerId, trong khi account web nhận player riêng, bootstrap/relogin và account-status guards vẫn đúng.
- Tách rõ bốn lệnh trong Unity: `ĐĂNG NHẬP TRONG GAME`, `ĐĂNG NHẬP WEBSITE`, `ĐĂNG KÝ TRONG GAME`, `ĐĂNG KÝ TRONG WEBSITE`. Hai nút đăng nhập gọi đúng route local/web riêng; đăng ký website mở `https://ywonder.net/vi/login` trong khi form local vẫn được giữ.

### Verified
- `test:security` pass.
- `test:web-auth` pass.
- Full Phase 1 isolated pass: register/login, shop nguyên tử, bootstrap persistence, idempotency, farm-state, realtime chat và thay phiên account `4008`.
- Release `5db92436a7974b38866fa3291f5f3e3577a2f30f` đã deploy versioned lên VPS; backup PostgreSQL/env/unit và previous release đều còn để rollback. Production xác nhận `WEB_AUTH_MODE=http`, `AUTH_TRANSITION_MODE=parallel`, local registration bật và public health dùng PostgreSQL.
- Nghiệm thu tài khoản thật pass: một account web và một account game local đều login -> bootstrap -> relogin thành công, giữ playerId ổn định và không dùng chung dữ liệu. Không ghi mật khẩu/token vào file hoặc log.
- Unity Editor tự compile thay đổi UI, toàn bộ log không có `error CS` hoặc `Compilation failed`. Chưa build/runtime-test nút web trên EXE/APK.
- Browser SSO callback/exchange + PKCE vẫn là Nấc B; chưa sửa web source hoặc Nginx.

---
## [2026-07-12] — Bộ bàn giao tester + kế hoạch đăng ký web

### Added
- Đóng gói `YWonder_Tester_Handoff_RC1_2026-07-12.zip`: workbook gameplay 75 test case, workbook chăn nuôi/lợi nhuận 10 loài có công thức, hướng dẫn tester và hai file nguồn `VatNuoi2.xlsx` + `SuaLai4VatNuoi.xlsx`; không chứa mật khẩu/token/secret.
- Thêm `docs/WEB_REGISTER_REDIRECT_PLAN_2026-07-12.md` cho yêu cầu web auth song song. URL đã chốt là `https://ywonder.net/vi/login`; source Unity hiện có nút web riêng nhưng vẫn giữ form local.

### Notes
- Ghi rõ ba điểm chờ BA/test lead xác nhận thay vì tự đổi số khách: tổng thức ăn Thỏ `90` so với phép tính `80`, tổng thức ăn Vịt `180` so với phép tính `90`, và giá trứng Vịt `4,5 Point` trong file so với `5 Point` ở runtime generator.
- Cầu web credential và mapping `web_user_id -> playerId` đã nghiệm thu ở Nấc A. Chỉ mở trình duyệt vẫn chưa phải SSO; callback một lần, PKCE, account-status/cross-device và quyết định tắt local chỉ được làm ở Nấc B sau nghiệm thu riêng.

---
## [2026-07-12] — Tài khoản QA Rich production

### Added
- Tạo `QARich0001..QARich0005` bằng luồng đăng ký production bình thường, giữ demo seeding tắt. Fresh login/bootstrap xác nhận mỗi account có 500.000 Point, 2.500 UPoint, profile đã qua tạo nhân vật/tutorial, 80 ô kho và 31 loại item; mật khẩu ngẫu nhiên lưu ngoài repo và phải đổi hoặc vô hiệu hóa sau đợt test.

## [2026-07-11] — Test thật 2 thiết bị + hotfix tutorial/login mobile

### Fixed
- Đồng bộ prefab cây/đá và tài nguyên sinh runtime về thời gian hồi 20 giây cho demo; save cũ có bộ đếm dài hơn cấu hình mới được chặn về đúng giới hạn khi load.
- Bỏ hoàn toàn node đào khoáng khỏi tuyến NPC Tân Thủ; flow 11 bước kết thúc sau `chặt cây -> xây ruộng/trồng trọt -> xây chuồng`, không còn bắt thả thú/cho ăn.
- Chặn `StartTutorial()` chạy hai lần trong cùng phiên, reset GuideNPC khi đổi/logout tài khoản, gom dấu `!` về đúng một instance và dọn dấu khi tutorial hoàn tất.
- Form đăng ký mobile không hiện lỗi độ dài liên tục khi người chơi còn nhập; câu báo đổi thành username/password mới cần ít nhất 9 ký tự, đúng rule backend. Form đăng nhập tài khoản cũ không bị áp minimum mới.

### Verified
- Anh đã test bản public trước hotfix bằng 1 EXE mạng A và 1 APK mạng B: chat realtime và đào khoáng đồng bộ hoạt động rất tốt.
- Các file C# hotfix compile pass bằng response file Unity; chỉ còn warning cũ không liên quan `enableStickAutoSprint`. Ngày 11/07 anh đã build/test lại và xác nhận tutorial mới, đăng nhập lặp không nhân dấu `!`, cùng phản hồi độ dài form đăng ký mobile đều hoạt động đúng.

---
## [2026-07-11] — Public Nginx `/game-api` cutover

### Added
- Thêm `server/deploy/configure-public-nginx.sh`: resolve đúng symlink site, backup root-only, chèn namespace có marker, `nginx -t`, reload, health gate và rollback tự động. WebSocket location tắt access log để không ghi JWT query.

### Verified
- Nginx route `/game-api/*` và `/game-api/realtime -> 127.0.0.1:3000` đã active; `/api/game/* -> 3033` và root `-> 3036` giữ nguyên. Backup/config SHA lần lượt `87c987...a878` và `b7b6cc...185d8`.
- Automated 20-client từ Windows qua HTTPS/WSS public pass, p95 auth/bootstrap/WSS `1666.4/64.9/173.7 ms`; full Phase 1 public cũng pass shop, persistence, idempotency, farm-state, chat và session replacement.
- Test accounts đã dọn về `0`, ba P1 baseline còn đủ; Nginx/game-server/PostgreSQL/Caddy active. `80/443` mở, `3000/5432/8080` vẫn đóng public. Malformed JSON trả `INVALID_JSON` không lộ stack.
- Unity config chuyển sang `https://api.ywonder.net/game-api`, giữ online-only; chờ build EXE/APK và test thật 4–5 máy.

---
## [2026-07-11] — Public Nginx read-only audit

### Verified
- DNS `api.ywonder.net` đã về `42.96.18.14`; Nginx active/enabled giữ `80/443`, HTTP chuyển HTTPS, certificate hợp lệ; `3000/5432/8080` vẫn đóng public.
- Web API cũ vẫn được giữ: `/api/game/* -> 127.0.0.1:3033`; mọi path khác của subdomain hiện tới `ywonderland-main-game-api` trên `3036`.
- Backend PostgreSQL của game vẫn healthy tại `127.0.0.1:3000` và qua Caddy private `127.0.0.1:8080`, nhưng public `/player/bootstrap` và `/realtime` đang `404` vì Nginx chưa route tới backend này.
- Chốt phương án ít rủi ro: giữ nguyên route cũ, chỉ thêm `/game-api/*` và `/game-api/realtime -> 127.0.0.1:3000`; Unity sau acceptance sẽ dùng `https://api.ywonder.net/game-api`. Xem `docs/NGINX_PUBLIC_AUDIT_2026-07-11.md`.

---
## [2026-07-11] — Private VPS automated 20-client acceptance

### Added
- Thêm `server/phase1LoadTest.js`, npm script `test:load` và `server/deploy/run-private-load-test.sh`. Bài test giới hạn đúng 20 client, kiểm đăng ký/login, bootstrap đủ 5 nhóm dữ liệu, 20 WebSocket cùng room, roster, state, chat và ping; runner VPS backup trước khi chạy và dọn account đúng prefix sau test.

### Verified
- Chạy qua private Caddy `127.0.0.1:8080` + PostgreSQL production pass: 20 client vào `city`, client cuối thấy đủ 19 peer, tất cả giữ kết nối. P95 auth `1532.4 ms`, bootstrap `31.9 ms`, WebSocket connect `36.6 ms`.
- Backup pre-cutover mới: `/var/backups/ywonder-game/ywonder_game_20260711T072715Z.dump`, `25242` bytes, SHA-256 `04dda7ac1048d0de493a25f91ab98116f784494460c1cbfa390479d646679a7e`.
- Hậu kiểm xác nhận account tiền tố tải còn `0`, không có OOM; PostgreSQL, `ywonder-game-server`, Caddy và backup timer đều active, health trả `storage.mode=postgres`.
- Đây là test tự động kín, chưa thay thế test thật 5–20 EXE/APK ngoài mạng sau khi HTTPS/WSS public.

---
## [2026-07-11] — Full VPS reboot acceptance

### Verified
- VPS reboot thật lúc `13:13:19 +07`; `boot_id` đổi thành `ee8dfd96-8d69-43c0-a0ab-5ddcccd109f9`.
- PostgreSQL, `ywonder-game-server`, Caddy và `ywonder-db-backup.timer` đều tự khởi động lại ở trạng thái `active/enabled`; private health trả `storage.mode=postgres`.
- `P1A_h09433`, `P1B_h09433`, `P1Race_h09433` login/bootstrap lại được. Canonical fingerprint dữ liệu trước/sau reboot khớp `a003b888ed68b5ee95e43efae2ee0873fafd291dac66aac0ffceeaf7c649bf6e`.
- Public DNS/HTTPS/WSS và Unity URL vẫn chưa thay đổi; từ ngoài hiện chỉ SSH `22` được mở.

---
## [2026-07-11] — Controlled restart PostgreSQL/backend

### Added
- Thêm `server/deploy/restart-private-services-verify.sh`: chụp fingerprint dữ liệu P1 trước/sau, restart PostgreSQL rồi game-server, chờ health trực tiếp/Caddy và fail nếu profile/economy/inventory/farm/daily-limit/transaction thay đổi. Script không chứa credential và không ghi password/token.

### Verified
- PostgreSQL và `ywonder-game-server` restart lúc `11:35:58 +07`; Node/Caddy health trở lại với `storage.mode=postgres`, PostgreSQL/Node/Caddy/backup timer đều `active/enabled`.
- Fingerprint của `P1A_h09433`, `P1B_h09433`, `P1Race_h09433` khớp hoàn toàn trước/sau. Kiểm tra login/bootstrap độc lập qua SSH tunnel giữ đúng Point, inventory và farm state của cả ba account.
- Full VPS reboot acceptance đã hoàn tất ở checkpoint mới hơn phía trên; public DNS/HTTPS/WSS và Unity URL chưa thay đổi.

---
## [2026-07-11] — Gia cố backend và private redeploy

### Changed
- Commit `09433bff` thêm production startup gate, bcrypt bất đồng bộ, rate limit đăng nhập/đăng ký, body/CORS/security header/request ID, log không ghi body/token, HTTP timeout, WebSocket connection/payload/message guard và graceful shutdown.
- Thêm `server/security.js`, `server/securitySmokeTest.js` và script deploy immutable có checksum, migration, systemd verify, health gate và rollback.
- Private VPS đã chuyển sang release `09433bff1e739bd2573c8068ffa58f445cd01bb6`; chưa đổi DNS, Unity URL hoặc firewall public.

### Fixed
- Lượt deploy root đầu dừng trước khi switch vì migration kế thừa `USER=root` nên PostgreSQL peer auth từ chối. Script nay ép `USER/LOGNAME/PGUSER=ywonder_game`; release cũ vẫn active trong lúc lỗi và lượt chạy lại pass.

### Verified
- `test:security`, full Phase 1 local và `npm audit --omit=dev` pass với `0 vulnerabilities`.
- Full Phase 1 qua SSH tunnel -> Caddy private -> PostgreSQL production pass; sai mật khẩu trả `401` và header rate limit `15`, `/admin` trả `404`.
- Systemd chạy bằng `ywonder_game` với sandbox bổ sung; backup timer active/enabled. `3000/5432/8080` chỉ nghe loopback; từ Internet chỉ `22` mở, còn `80/443/3000/5432/8080` đóng. `api.ywonder.net` vẫn phân giải về `45.119.83.233`.

---
## [2026-07-11] — SSH deploy và PostgreSQL Phase 2

### Changed
- Đã tạo user không đặc quyền `deploy` trên VPS game và gắn ED25519 public key; không lưu password/private key trong repo.
- Hoàn thiện `postgresStore.js`, migration versioned, bảng local account, inventory meta và transaction snapshot; REST/admin/realtime dùng async nhưng giữ nguyên API Unity.
- Thêm `db:migrate`, `db:import-json`, `db:verify`, `test:postgres` và runbook `docs/POSTGRESQL_PHASE2_RUNBOOK.md`.
- Cài PostgreSQL 14.23 trên VPS để integration test; service active/enabled và chỉ listen `127.0.0.1:5432`.

### Verified
- Đăng nhập không tương tác bằng key đã pass với đúng `uid=1001(deploy)`.
- Quyền sở hữu và mode đúng: `/home/deploy/.ssh` là `700`, `authorized_keys` là `600`, đều thuộc `deploy:deploy`.
- JSON Phase 1 regression và PostgreSQL direct/REST/WebSocket smoke đều pass; dừng/mở lại Node vẫn giữ đúng Point và farm state. DB health và hai đăng ký trùng đồng thời trả đúng một `200` + một `409 USERNAME_EXISTS`.
- Import schema tạm pass đúng `36 accounts / 51 players / 82 transactions`; dashboard dev đọc PostgreSQL pass; `npm audit --omit=dev` không có vulnerability.
- Chưa đổi sshd/UFW, chưa khóa `root/password`; PostgreSQL không public 5432. Production DB/backup/Node/Caddy/DNS vẫn chờ bước sau.
- Regression `/auth/web-login` mock đã pass lại với canonical `demo_*` playerId; presence/chat/action/resource/late-join/farm rejection và thay phiên account mã `4008` đều đúng.

---
## [2026-07-10] — Đồng bộ tài nguyên gameplay ngoài shop

### Changed
- Thêm `GameplayMutationSync`: mọi `AddItem/RemoveItem` và `Add/Spend Point/UPoint` khi đã đăng nhập được gửi tuần tự lên `inventory/adjust` hoặc `economy/apply`, mỗi thay đổi có idempotency key và retry giữ nguyên key.
- Bootstrap, shop và logout chờ các thay đổi đang gửi xong trước khi áp snapshot server mới. Một cơ chế chung bao phủ hạt, nước, nông sản, gỗ/đá/cá, thức ăn, thú, phân bón, quà, vật liệu xây và tiền gameplay ngoài shop.
- Logout lưu vị trí farm trước khi hủy nhân vật; đăng nhập hồ sơ cũ ưu tiên pose farm đã lưu rồi mới dùng vị trí bến.
- VPS `42.96.18.14` đã audit read-only: Ubuntu 22.04.5 LTS, KVM, 2 vCPU, RAM 3.8 GiB + swap 3.8 GiB, disk 50 GB còn khoảng 37 GB, timezone/NTP đúng. UFW deny inbound, chỉ SSH 22 mở; chưa có Node/PostgreSQL/Caddy/Nginx/Docker và không có app cũ cần giữ. Cấu hình đủ demo khoảng 20 người. Chi tiết tại `docs/VPS_GAME_AUDIT_2026-07-10.md`; chưa deploy và không lưu credential trong repo.

### Verified
- Unity C# compile pass; chỉ còn warning auto-sprint cũ.
- `npm.cmd run test:phase1` pass. Test API riêng sau relog trả đúng 20 nước, 1 hạt sầu riêng sau khi mua 2/trồng 1, và 5030 Point sau +50/-20; retry cùng key không cộng nước hai lần.

### Nghiệm thu runtime
- Ngày 10/07 anh đã test bản Unity mới và xác nhận đồng bộ inventory/economy gameplay cùng khôi phục vị trí farm hoạt động ổn; hai task được chuyển `[x]`.
- Vẫn nên regression thêm thu hoạch cây/cá/thú, cho thú ăn, vật liệu xây, quà và nâng dụng cụ khi chạy bộ test 5-20 client. Resume trực tiếp City/Mine chưa thuộc lát này.

---
## [2026-07-10] — Tách cache gameplay theo playerId

### Changed
- Thêm `PlayerScopedPrefs`: Point, inventory, tool/EXP, vị trí, farm/cây, ô lát/công trình, thú và counter ngày/event dùng key theo `playerId`; audio/camera/auth vẫn là setting chung theo thiết bị.
- Auth phát event trước/sau khi đổi identity để save world của account cũ rồi clear/load account mới ngay trong cùng scene.
- Legacy PlayerPrefs chỉ được migrate cho một account; online-only chưa login không còn đọc/ghi state gameplay chung.

### Verified
- Unity C# compile pass; chỉ còn warning `enableStickAutoSprint` cũ không liên quan.

### Nghiệm thu runtime
- Ngày 10/07 anh đã đổi A -> B -> A, sau đó đóng hẳn và mở lại EXE; hai tài khoản giữ đúng state riêng, không nhận dữ liệu của nhau và khôi phục đúng sau restart. Task cache theo `playerId` được chuyển `[x]`.

---
## [2026-07-10] — Shop transaction nguyên tử phía server

### Changed
- Thêm catalog server sinh từ 109 `ItemDefinition` và 8 `ShopDefinition`; Node tự kiểm giá, mode và whitelist của từng quầy, không tin giá Unity gửi lên.
- Thêm `POST /player/shop/transaction`: mua/bán đổi Point và inventory trong cùng một lần ghi JSON, có idempotency và chặn cùng key nhưng body khác.
- `ShopPopupController` không còn gọi `SpendPOS/AddPOS/AddItem/RemoveItem` khi mua bán. Unity khóa nút trong lúc chờ, retry cùng key khi mất phản hồi và chỉ áp economy + inventory từ response server; mất mạng không giao dịch local.
- Xác nhận `42.96.18.14` là VPS riêng cho game, được phép cài Node + PostgreSQL, dự kiến Ubuntu Server 24.04 LTS và sau này nhận `api.ywonder.net`. Cổng `22/80/443/3000` hiện chưa tới được từ máy làm việc, nên chưa đăng nhập/deploy/đổi build. Thêm `docs/VPS_GAME_DEPLOYMENT_PLAN.md` để kiểm soát audit, hardening, PostgreSQL, DNS, nghiệm thu và rollback; không lưu credential trong repo.

### Verified
- Catalog generator kiểm đủ các ID và tạo 109 items/8 shops.
- Backend tạm port `3190` và Quick Tunnel public hiện tại đều pass mua, bán, giá client giả bị bỏ qua, retry không nhân đôi, thiếu tiền/đồ, sai whitelist, conflict key và relogin còn dữ liệu.
- Assembly C# mới compile thành công bằng Roslyn của Unity; chỉ còn warning cũ không liên quan.
- Anh đã test runtime và xác nhận mua/bán + relogin hoạt động khá tốt; task shop được chốt `[x]`. Kết nối lại đôi lúc hơi lâu nhưng hiện không chặn demo.

### Fixed sau nghiệm thu
- Direct tap tại Thành phố không còn cho phép đi tiếp gần `1.7m` sau collider đặc. Sai số bề mặt nay là `0.05m`, để nền đất chặn WaterSource/FishingSpot nằm dưới đảo nhưng spherecast assist vẫn hỗ trợ vật thể ở sát bề mặt.

### Nghiệm thu runtime
- Anh đã test và xác nhận click nền Thành phố không còn bật nhầm UI nước/câu cá bên dưới, còn click trực tiếp mặt biển/điểm câu hợp lệ vẫn hoạt động.

---
## [2026-07-10] — Realtime action sync và audit state Phase 1

### Changed
- Realtime `player_state` gửi thêm state hoạt ảnh thật, tốc độ và dụng cụ đang cầm; remote player hỗ trợ jump, swim và các action cuốc/đào/chặt/tưới/câu/cho ăn/gieo.
- `RemotePlayerController` tìm Animator cả ở child prefab và chỉ bật model dụng cụ ở remote, không kích hoạt gameplay component của người khác.
- `server/realtimeServer.js` relay `animationSpeed` và `tool`; smoke test kiểm `Jump` và `Mining + Pickaxe`.
- Thêm `docs/PHASE1_STATE_SYNC_AUDIT.md` phân loại từng nhóm dữ liệu đã đọc/ghi server hay vẫn local.
- Sửa phân loại lỗi đăng nhập: `ApiClient` giữ mã lỗi JSON, nên tài khoản không tồn tại/sai mật khẩu không còn bị fallback web-auth 503 ghi đè thành thông báo máy chủ tạm ngừng.
- Đồng bộ cây/đá public `city/mine` theo server: một người claim thắng, inventory + lượt đào ghi nguyên tử/idempotent, broadcast biến mất, snapshot cho người vào sau và hồi lại sau 20 giây. Unity chỉ cập nhật túi khi server xác nhận.
- Mở rộng realtime smoke test để kiểm người thắng nhận thưởng, người thứ hai bị từ chối, late join thấy tài nguyên đã biến mất, respawn broadcast và session replacement 4008 vẫn hoạt động.

### Verified
- Backend tạm port `3107` pass auth, join, global chat, action state/tool relay, farm rejection và duplicate session 4008.
- Unity Editor hiện tại import các script realtime không có lỗi biên dịch C#.
- Anh đã xác nhận hoạt ảnh realtime chạy tốt trên hai client.
- Backend temp port `3188` pass smoke tài nguyên nhiều client; port `3189` pass toàn bộ Phase 1 regression; cùng smoke REST/WebSocket/resource pass tiếp qua Quick Tunnel public mới. Unity build lại `Assembly-CSharp` thành công.
- Ngày 10/07 anh đã test bản build nhiều client và xác nhận đồng bộ cây/đá, quyền nhận thưởng, biến mất và hồi sinh đều vận hành rất ổn.

### Còn lại
- Shop transaction và cache theo `playerId` đã được chốt ở các mục mới phía trên; tiếp theo là farm/daily-limit sync và test 5-20 người trước khi chốt xong Giai đoạn 1.

---
## [2026-07-09] — Phase 1 backend account smoke test

### Changed
- `AuthService` thử `/auth/login` local trước, chỉ fallback `/auth/web-login` khi server báo `404 USER_NOT_FOUND`.
- `LoginScreenController` gửi email khi đăng ký tài khoản.
- `server/index.js` lưu `email/phone` cho `/auth/register`, chặn trùng email, và phân biệt `USER_NOT_FOUND` với sai mật khẩu ở `/auth/login`.
- `server/store.js` thêm `findUserByEmail`; `server/postgresStore.js` giữ cùng interface để Phase 2 đổi PostgreSQL không vỡ route.
- Thêm `server/phase1SmokeTest.js` và npm script `test:phase1`.

### Verified
- Đã chạy server tạm port `3101` với data file riêng trong temp và chạy `npm.cmd run test:phase1 --prefix server`.
- Kết quả pass: register, login, bootstrap persistence, economy/inventory idempotency, farm-state persistence và realtime chat giữa 2 account đều hoạt động.

### Còn lại
- Public backend tạm hoặc máy case thật, build EXE/APK trỏ URL public, rồi test 5-20 người ngoài mạng.

---
## [2026-07-09] — Chặn mất save khi mất focus trong lúc load

### Fixed
- `SystemsBootstrapper` bật `Application.runInBackground` cho Editor/Standalone để loading và async request không bị dừng khi người test alt-tab sang cửa sổ khác.
- `BuildPersistence`, `TilePlacementSystem`, `FarmManager`, `AnimalManager` và `ResourceSpawner` không còn ghi PlayerPrefs khi hệ tương ứng chưa load/restore xong.
- Riêng `BuildPersistence` và `TilePlacementSystem` chỉ mở khóa save sau khi crop restore qua frame kế tiếp đã hoàn tất, tránh ghi công trình/ô đất nhưng mất cây đã trồng.

### Test cần làm
- Tạo thay đổi farm: cuốc đất, trồng cây, đổi tiền/túi đồ nếu cần.
- Thoát/mở lại hoặc travel vào farm; trong lúc loading, alt-tab sang Chrome 3-5 phút rồi quay lại.
- Kết quả kỳ vọng: ô đã cuốc/cây/công trình không bị reset do save rỗng. Nếu chỉ còn tiền quay về `500.000`, cần kiểm riêng luồng `/player/bootstrap` vì server demo hiện vẫn là nguồn tiền khi bootstrap.

---
## [2026-07-09] — Ẩn nút hủy HUD khi build và rà héo cây lâu năm

### Changed
- `GameHUDController` không hiện nút X hủy hoạt ảnh HUD khi build popup đang mở, tránh nút X đỏ đè lên khu vực Jump lúc xây Ruộng/Đường đá/Chuồng.
- `FarmTile` hỗ trợ lưu/khôi phục cây nhiều ô bằng `slaveTileKeys`, để cụm chanh dây 20 ô không bị bỏ qua khi save/load.
- Rà dữ liệu cây lâu năm: Sa Chi, Sầu Riêng và Chanh dây đều có `noWaterDeathSec = 840` và `wateredLifeSec = 840`, nên nếu gieo xong không tưới lần đầu thì cây sẽ héo sau khoảng 840 giây demo, trừ khi đang trong tutorial.

### Test cần làm
- Build/test APK hoặc Editor: mở build popup, chọn Ruộng/Đường đá/Chuồng và xác nhận xây; không còn nút X đỏ nổi ở cạnh nút Jump.
- Trồng thử Sa Chi/Sầu Riêng/Chanh dây, không tưới lần đầu và chờ qua 840 giây demo để xác nhận cây héo.
- Riêng chanh dây: test thêm save/load trong lúc cây chưa héo để xác nhận cụm 20 ô vẫn được khôi phục đúng.

---
## [2026-07-09] — Chốt giá giống chanh dây

### Changed
- Cập nhật `passion_fruit_seed_01` từ `1.560 Point` lên `5.300 Point` theo phản hồi BA/khách: `200 USDT / 20 cây`, tỉ giá `26.500` quy về `5.300 Point` trong game.
- Cập nhật `ItemDataGenerator` để khi chạy lại Generate Mock Items không trả giá giống chanh dây về số cũ.
- Tách `seedItemCost` khỏi `plotSlots` trong `CropDefinition` để một item giống có thể là gói/cụm nhiều cây: chanh dây trừ 1 item `Giống chanh leo` nhưng vẫn chiếm 20 ô đất.
- `task.md` ghi rõ phần giá đã chốt, nhưng rule chanh dây vẫn để `[~]` vì còn cần test runtime cây chiếm 20 ô, sản lượng cụm 20 cây và save/load cây nhiều ô.

### Test cần làm
- Mở shop hạt giống/vật nuôi, kiểm `Giống chanh leo` hiển thị giá `5.300 Point`.
- Mua và gieo chanh dây: phải trừ đúng tiền, trừ 1 item giống/gói chanh leo khi trồng, chiếm 20 ô đất và không lỗi save/load.

---
## [2026-07-09] — Ghi nhận bàn giao hạ tầng/web auth

### Changed
- Cập nhật `task.md`, `docs/API_CONTRACTS.md`, `docs/WEB_GAME_BACKEND_JOURNEY.md` và `docs/CONTEXT_RECOVERY.md` với thông tin hạ tầng/web auth nhận từ chat 01/07.
- Ghi rõ endpoint web auth đang dùng được là `POST https://ywonder.net/api/game/auth`; `api.ywonder.net` vẫn là target public đẹp hơn nhưng còn phụ thuộc xử lý SSL/WAF/default-server hoặc DNS-01 từ phía owner/infra.
- Ghi rõ game-server gọi web auth server-side bằng `GAME_API_SECRET`; Unity không giữ secret.
- Ghi nhận domain/IP/port không nhạy cảm, endpoint đọc/cộng Point cho phase sau và danh sách thông tin còn thiếu trước khi deploy game-server thật.
- Không ghi mật khẩu VPS, mật khẩu tài khoản test, SSH key, DB password hoặc `GAME_API_SECRET` vào repo.

### Test cần làm
- Khi có quyền hạ tầng thật: test ngoài LAN bằng 4G/5G với `/health`, đăng nhập web auth qua game-server, `/player/bootstrap` và WebSocket realtime.
- Chỉ báo production-ready sau khi xác nhận game-server public, proxy HTTPS/WebSocket, database thật, service auto-start, backup và log.

---
## [2026-07-08] — Shop popup mobile dễ đọc hơn

### Changed
- `ShopPopupController` tự gắn class `shop-mobile` khi chạy trên mobile build để shop dùng mật độ riêng cho điện thoại.
- `ShopPopup.uss` thêm layout mobile giữ khung popup gần kích thước cũ, nhưng tăng card sản phẩm, icon, tên, giá/số lượng, nút +/- và nút mua/bán; mỗi màn hình hiển thị ít sản phẩm hơn và dùng scroll để xem tiếp.
- Bật preview layout shop mobile trong Editor để test ngay trong Play Mode.
- Filter shop nay đổi theo mode/shop hiện tại: Fish Shop dùng nhãn `Cá`, Mini Garden dùng `Nông sản`, tab Bán không kéo filter từ tab Mua.
- Bỏ `gift_box_01` khỏi Fish Shop và sửa generator để không sinh lại quà trong shop cá.
- Farm Item Shop được mở lại bằng cách trả `comingSoon = 0`.
- `task.md` đã ghi nhận trigger cửa hàng farm/city được chỉnh scene và ShopPopup mobile đang chờ APK/EXE test.

### Test cần làm
- Build APK/EXE: mở các shop chính ở Farm/City, kiểm card/icon/chữ/nút dễ đọc/dễ bấm hơn.
- Kiểm danh sách shop vẫn scroll được, chọn vật phẩm vẫn hiện panel chi tiết đúng, mua/bán vẫn cộng/trừ Point và inventory như trước.
- Kiểm popup shop không che vỡ nút X, sidebar filter, panel chi tiết trên các máy điện thoại mục tiêu.

---
## [2026-07-07] — HUD mobile lớn hơn, popup có vùng an toàn cho nút X

### Changed
- `GameHUDController` tự gắn class `hud-mobile` khi chạy trên mobile build, để HUD gameplay lớn hơn mà không đổi layout desktop.
- HUD mobile đã tăng kích thước cụm thông tin nhân vật, quest prompt, tiền, sidebar, joystick, sprint, build, jump và nút tương tác trực tiếp.
- `UISafeArea` có thêm minimum inset cho mobile build để UIDocument không áp sát bo góc/thanh điều hướng.
- `DesignSystem.uss` thêm padding chung cho popup overlay, max-width/max-height cho panel chính, và min-size 44px cho các nút đóng X.
- Các `close-shadow/container` của popup nay có anchor 44x44 thật để nút X lớn không bị trôi nửa ra ngoài góc phải-trên panel.
- Các popup Event, Túi đồ, Bạn bè, Leaderboard và Hồ sơ cũng đã đặt anchor X trực tiếp trong file USS riêng để tránh bị stylesheet riêng override lại.
- Bạn bè, Sự kiện, Leaderboard, Nhiệm vụ, Túi đồ và Hồ sơ nay đặt nút X vào trong cấu trúc panel/header, cùng hướng với popup Settings, thay vì để X nổi ngoài wrapper.

### Test cần làm
- Build APK: vào farm, kiểm HUD dễ đọc/dễ bấm hơn và không đè nhau.
- Mở Túi đồ, Shop, Settings, Quest, Profile, Map, Mailbox/Piggy nếu có; nút X phải không bị lẹm và bấm được trên các máy test.
- Kiểm lại compact build popup, chat bar, joystick và vùng xoay camera bên phải không bị HUD mới che sai.

---
## [2026-07-07] — Joystick không còn ô vuông, nhân vật giữ hướng khi thả tay

### Fixed
- Bỏ các ký tự mũi tên text trang trí trong joystick mobile để tránh lỗi font/device render mũi tên trái thành ô vuông.
- Khi thả joystick, nhân vật không còn tự xoay về yaw camera; hướng đứng yên sẽ giữ theo hướng di chuyển cuối cùng.

### Test cần làm
- Build APK/EXE: joystick trái không còn hiện ô vuông nhỏ ở mép trái vòng tròn.
- Kéo joystick xuống để nhân vật quay mặt về phía màn hình rồi thả tay; nhân vật phải giữ hướng đó, không tự quay lưng lại.

---
## [2026-07-07] — Auto-run tắt khi kéo joystick

### Changed
- Khi auto-run đang bật, chỉ cần người chơi kéo joystick vượt một ngưỡng nhỏ thì `GameHUDController` tắt auto-run ngay.
- Nút Sprint/auto-run cập nhật trạng thái sáng/tắt ngay khi joystick hủy auto-run, tránh cảm giác vẫn đang bật.

### Test cần làm
- Bấm Sprint để bật auto-run, sau đó kéo joystick trái; nhân vật phải dừng auto-run ngay và đi theo joystick.
- Thả joystick sau khi đã hủy auto-run; nhân vật không được tự chạy tiếp nếu chưa bấm Sprint lại.

---
## [2026-07-07] — Compact build popup

### Changed
- Build Mode nay mở bằng popup ngang gọn ở bên phải gần nút búa, không còn dùng full HUD top/bottom rườm rà cho flow 3 công trình.
- Popup chỉ hiển thị vật liệu đúng với 3 lựa chọn hiện có: Gỗ và Đá.
- Ruộng, Đường đá, Chuồng hiển thị thành 3 thẻ ngang; bấm thẻ sẽ pin ghost preview vào ô viền trắng trước mặt.
- Nút tích xanh và nút X nằm ngay dưới thẻ đang chọn để xác nhận đặt hoặc bỏ chọn, thay cho cụm OK/X nổi ngoài world.

### Test cần làm
- Build/test APK/EXE: bấm nút búa, popup phải nằm gần cụm nút phải và không che joystick/camera/jump.
- Chọn từng thẻ: ghost phải hiện trên ô viền trắng; bấm tích xanh đặt đúng ô, bấm X dưới thẻ thì hủy chọn.
- Test thiếu vật liệu hoặc ô bị chiếm: không được đặt sai, popup/ghost không được đóng nhầm.

---
## [2026-07-07] — Direct tap world interaction

### Changed
- Chuyển tương tác thế giới sang kiểu tap/click trực tiếp lên vật thể trong tầm gần thay vì quét theo tâm màn hình liên tục.
- Cây, đá, hồ nước/câu cá, shop, chuồng/ô build chỉ hiện UI action sau khi người chơi bấm vào đúng vật thể đủ gần; tap xa hoặc tap trống sẽ không mở action.
- Tầm tap trực tiếp hiện dùng `directTapMaxRange = 3.5m` để dễ chọn vật thể hơn trên mobile.
- Direct tap nay gom raycast thường với spherecast assist nhỏ trong world-space để tap hơi lệch hoặc collider nhỏ/lệch vẫn dễ bắt hơn.
- Kiểm tra tầm direct-tap nay đo theo object/collider gốc đã resolve, không chỉ đo đúng điểm `hit.point`, giảm lỗi cùng một vật thể nhưng góc này hiện/góc khác không hiện.
- UI action của flow direct-tap tự ẩn khi nhân vật đi ra khỏi tầm vật thể đang chọn.
- Ẩn chấm `Crosshair` trên HUD vì flow mới không còn dùng tâm tương tác.

### Test cần làm
- Build/test APK/EXE: đứng gần cây, đá, hồ nước, chuồng/cửa hàng rồi tap đúng vật thể; UI action phải hiện.
- Test cùng một vật thể từ nhiều góc camera/nhân vật khác nhau; prompt không nên chỉ hiện ở một góc may mắn.
- Bấm action trên UI phải thao tác như trước; nếu đi xa khỏi vật thể thì action không được thực hiện.
- Tap nền trống hoặc đứng ngoài tầm khoảng 3.5m phải không hiện prompt tương tác.
- Sau khi prompt hiện, đi xa khỏi vật thể đó thì UI action phải tự ẩn.

---
## [2026-07-07] — Front-cell build workflow step 3

### Changed
- Chọn item trong build list giờ tự đưa ghost preview lên đúng `BuildSurfaceCell` đang có viền trắng trước mặt nhân vật và ghim tại đó.
- Trong workflow mới, người chơi không cần tap màn hình để ghim vị trí nữa; chỉ cần bấm OK để xác nhận hoặc X để hủy.
- `GhostPlacementController.ConfirmPlacement()` trả về kết quả thật để UI chỉ thoát placement khi đặt thành công; thiếu vật liệu hoặc ô bị chiếm sẽ giữ ghost lại và báo lỗi.

### Test cần làm
- Build/test Editor/APK: đứng trước một ô có viền trắng, bấm búa, chọn Ruộng/Đường đá/Chuồng; ghost phải hiện ngay trên ô viền trắng.
- Bấm OK phải đặt công trình vào đúng ô đó; bấm X phải hủy ghost và quay lại build list.
- Thử ô đã bị chiếm hoặc thiếu vật liệu: ghost đỏ/báo lỗi, không được đặt sai.

---
## [2026-07-07] — Front-cell build workflow step 2

### Changed
- Chuyển nút búa/build khỏi cụm túi đồ góc phải-trên xuống cụm tay phải, nằm trên nút Jump.
- `BuildModeOverlayController` mặc định không kích hoạt camera top-down và không ẩn GameHUD khi mở build list, để người chơi giữ góc nhìn nhân vật và vẫn điều khiển được nhân vật/camera.
- Giữ lại hai cờ Inspector `useTopDownBuildCamera` và `hideGameHudWhileOpen` nếu cần bật lại flow build cũ để debug.

### Test cần làm
- Build/test Editor/APK: bấm nút búa bên phải phải mở build list cũ nhưng camera không đổi sang top-down.
- Khi build list đang mở, joystick và vùng xoay camera vẫn hoạt động; chưa test/hoàn thiện ghost cố định tại ô trước mặt.

---
## [2026-07-07] — Front-cell build workflow step 1

### Added
- Thêm `FrontBuildCellSelector` runtime: tự chọn `BuildSurfaceCell` ngay phía trước mũi chân nhân vật theo hướng mặt nhân vật và vẽ viền trắng trên mặt ô.
- Đây là bước đầu của workflow build mới: không đổi camera build, không dùng ghost di chuyển tự do; các bước nút búa/build list/ghost cố định sẽ nối sau.

### Test cần làm
- Test Editor/APK: khi nhân vật xoay/di chuyển, ô ngay trước chân đổi viền trắng đúng hướng mặt nhân vật.
- Kiểm tra viền không hiện khi không có `BuildSurfaceCell` hợp lệ trước mặt và không che UI/camera mobile.

---
## [2026-07-07] — Hotfix joystick mobile không xoay camera

### Fixed
- Đổi binding `Look` trong `Assets/InputSystem_Actions.inputactions` từ `<Pointer>/delta` sang `<Mouse>/delta` để thao tác touch/joystick trên mobile không bị camera đọc như input nhìn toàn màn hình.
- Siết pointer handling trong `GameHUDController`: joystick capture pointer riêng, chặn propagation ở down/move/up/capture-out, và `LookZone` từ chối pointer đang thuộc vùng joystick.
- Đảo lại trục dọc touch-look trong `ThirdPersonCamera`: vuốt lên thì camera ngẩng lên, vuốt xuống thì camera cúi xuống; không đổi trục chuột PC.
- Bỏ cơ chế joystick kéo mạnh/giữ lâu tự bật chạy nhanh; nút Sprint trên HUD chỉ còn dùng để bật/tắt auto-run chủ động.
- Vẫn giữ luồng mong muốn trên mobile: ngón trái kéo joystick để di chuyển, ngón phải kéo nửa phải màn hình để xoay camera.

### Test cần làm
- Build/test trên điện thoại: kéo joystick bên trái không làm camera/map xoay.
- Kéo nửa phải màn hình vẫn xoay camera bình thường, với trục dọc đúng cảm giác: vuốt lên nhìn lên, vuốt xuống nhìn xuống; PC mouse và gamepad right-stick vẫn điều khiển camera.
- Kéo joystick hết biên/giữ lâu vẫn chỉ đi bộ thường; bấm nút Sprint mới bật/tắt auto-run.

---
## [2026-07-06] — Backend storage adapter và daily limits

### Added
- Thêm `docs/WEB_GAME_BACKEND_JOURNEY.md` để làm rõ hành trình Web account -> Game account -> backend gameplay, các loop phát triển, kết quả mong đợi và cách kiểm tra.
- Thêm storage facade trong `server/store.js`: local/dev dùng `JsonStore`, có lựa chọn `STORE_MODE=json|postgres`.
- Thêm `server/postgresStore.js` làm scaffold adapter PostgreSQL để route API giữ interface ổn định trước khi viết query DB thật.
- Thêm dashboard backend local tại `/admin` để xem player, profile, economy, inventory, daily limits, farm state và transactions.
- Thêm bảng `player_daily_limits` vào `server/schema.sql`.
- `/player/bootstrap` trả thêm `daily_limits`; thêm `GET /player/daily-limits` và `POST /player/daily-limits/consume`.
- Thêm `server/realtimeSmokeTest.js` và npm script `test:realtime` để test auth + WebSocket bằng account cấp sẵn khi web auth đang sập.

### Changed
- Cập nhật `task.md`, `server/README.md`, `docs/API_CONTRACTS.md`, `docs/ARCHITECTURE.md`, `docs/TECHNICAL_DESIGN.md`, `docs/DB_SCHEMA.md`, `docs/CONTEXT_RECOVERY.md` để ghi rõ backend hiện là MVP API/dashboard/WebSocket; Unity shop/economy/inventory còn local và loop tiếp theo theo scope mới là account online + realtime.
- Ghi nhận quyết định định hướng cho phase sau: tiền nạp từ web để dùng trong game phải đi qua web wallet API do game-server gọi; còn cần chốt tiền nạp vào `Point`, `UPoint` hay ví mới và xin endpoint trừ tiền/spend/reserve nếu web chưa có.
- Ghi nhận phỏng vấn backend mới: web đăng nhập bằng email/số điện thoại/password; khách phải có tài khoản trước khi chơi; 1 account = 1 nhân vật; account khóa/xóa mềm chặn game; nhiều máy cùng account dùng chung state server.
- Chuyển yêu cầu thô từ sếp thành roadmap backend: account web/cấp sẵn, realtime đảo công cộng `city`/`mine` nhưng không gồm farm, server-authoritative Point/inventory/shop, daily/farm/animal theo server time, dashboard online có login/audit, và hạ tầng PostgreSQL/HTTPS/WebSocket/backup.
- Cập nhật hướng ví: `Point` vừa là tiền game vừa là tiền nạp web; còn cần hỏi vai trò `UPoint`, web hay game-server là ledger cuối cùng, và endpoint spend/debit/reserve.
- Bổ sung scope mới từ anh: MVP sắp tới chưa cần hệ thống nạp/rút; ưu tiên account online + realtime cho khách hàng. Web wallet/top-up/spend chuyển sang phase sau, không block lát MVP này.
- Sửa scope kết nối Unity realtime: `RealtimeClient` giữ WebSocket khi đang gameplay để chat toàn server vẫn nhận/gửi được ngoài shared room; chỉ join room `city`/`mine` để hiện remote player/state, nên farm vẫn là đảo riêng.
- Game-server JWT/WebSocket mang sẵn `username/displayName` để client chưa join room vẫn gửi chat với đúng tên account, không hiện fallback `Player`.
- `economy/apply` kiểm tra `idempotency_key` trước khi validate số dư để retry không cộng/trừ Point thêm.
- `inventory/adjust` nhận `idempotency_key` để retry không cộng/trừ item thêm.
- `daily-limits/consume` ghi transaction theo `idempotency_key`, phục vụ giới hạn câu cá/đào đá 10 lượt/ngày trên server.

### Verified
- `node --check` passed cho `server/store.js`, `server/postgresStore.js`, `server/index.js`.
- `node --check` passed cho `server/adminDashboard.js`.
- Smoke test Node bằng data file tạm xác nhận đào mỏ bị chặn sau 10 lượt/ngày và retry economy/inventory không nhân đôi.
- Smoke test Express bằng server localhost tạm xác nhận `/player/bootstrap` có `daily_limits` và lượt đào mỏ thứ 11 trả HTTP 409.
- Smoke test dashboard xác nhận `/admin` có HTML, tạo được user demo, sửa economy JSON và xóa user khỏi JSON store tạm.
- Smoke test realtime local bằng `WEB_AUTH_MODE=mock` pass: `DemoRealtime01`, `DemoRealtime02`, `DemoRealtime03` login qua `/auth/web-login`; 2 client join `city`, client thứ ba không join room vẫn nhận/gửi chat global, gửi `player_state`, và bị chặn khi join `farm` bằng `ROOM_NOT_SHARED`.

---
## [2026-07-02] — Hotfix chat realtime

### Fixed
- Sửa đường gửi chat realtime trong Unity: các gói WebSocket gửi ra được xếp hàng tuần tự, tránh tin chat bị rơi khi đang spam gói vị trí nhân vật.
- Khi bấm gửi chat online, người gửi thấy tin của mình ngay trong lịch sử chat; echo từ server của chính mình bị bỏ qua để không bị trùng bong bóng.
- Thêm log nhẹ `[Realtime] Chat from ...` khi Unity nhận được tin chat từ người chơi khác để dễ kiểm tra trên Console.

### Test cần làm
- Build lại EXE mới, mở Editor + EXE, đăng nhập 2 tài khoản khác nhau.
- Cả hai vào `city` hoặc `mine`, mở chat và gửi qua lại; bên còn lại phải thấy tin nhắn, Console nên có log `[Realtime] Chat from ...` ở máy nhận.
- Farm vẫn là đảo riêng: không thấy remote player/interaction ở farm, nhưng chat thế giới vẫn phải nhận/gửi được khi client đang online.

---
## [2026-07-01] — Vòng quay may mắn dạng 12 múi

### Changed
- Đổi giao diện vòng quay từ kiểu icon rải quanh vòng tối sang vòng quay 12 múi màu giống mẫu anh gửi.
- Mỗi múi chỉ hiển thị icon item đang có trong `ItemDatabase`; đã bỏ tên item và số lượng trong múi cho gọn.
- Ô “may mắn lần sau” vẫn giữ trong danh sách thưởng nhưng để trống, không còn dùng icon vòng quay.
- Nút quay được đưa vào tâm vòng bằng icon mới `arrowforspin.png`; icon vòng quay ở giữa và mũi tên vẽ bằng USS đã bị bỏ, footer chỉ còn hiển thị số lượt còn lại.
- Logic phần thưởng không đổi: vẫn dùng danh sách 12 phần thưởng và tỉ lệ/weight hiện tại.

### Test cần làm
- Mở Sự kiện -> Vòng quay, kiểm tra vòng tròn không bị méo khi quay, đủ 12 múi, chỉ hiện icon item.
- Kiểm tra ô “may mắn lần sau” trống, tâm vòng có icon `Spin` mới và vẫn bấm quay được.
- Bấm quay vài lần, vòng quay phải dừng đúng phần thưởng, trừ lượt ngày và trao item/toast như cũ.

## [2026-07-01] — Tối ưu đặt Build Mode trên mobile

### Added
- Thêm trợ giúp đặt công trình chỉ áp dụng cho touch/mobile trong `GhostPlacementController`: điểm ngắm được nâng lên trên ngón tay và nếu tap không trúng chính xác collider ô nhỏ thì hệ thống chọn `BuildSurfaceCell` gần nhất trên màn hình trong bán kính hỗ trợ.

### Changed
- `BuildModeOverlayController` truyền rõ tap đặt vị trí là touch hay mouse; mobile dùng đường đặt dễ bấm hơn, còn PC/mouse vẫn giữ raycast chính xác như cũ.

### Test cần làm
- Trên điện thoại, vào Build Mode -> chọn Ruộng/Đường đá/Chuồng -> kéo/tap quanh các ô nhỏ, đặc biệt hơi lệch khỏi ô, ghost vẫn nên bắt vào ô hợp lệ gần nhất.
- Trên PC/Editor, click chuột đặt công trình vẫn phải giữ cảm giác cũ, không tự nhảy sang ô khác nếu ray không trúng ô.

## [2026-07-01] — Farm tile dùng model đất thật

### Changed
- Tắt `FarmTileMarker` tự vẽ viền ô đất màu trắng/vàng/xanh/cam khi trồng trọt.
- `FarmTile` không tự tạo cube/sphere/cylinder màu prototype mặc định nữa; đất thường/đất đã cuốc lấy từ `soilVisual`/`plowedVisual` anh gán trong Inspector.
- Khi gieo/tưới/chín, ô vẫn giữ model đất đã cuốc bên dưới cây; cây ưu tiên lấy model thật từ `CropDefinition.cropPrefab`.
- `FarmTile` hỗ trợ trường hợp `Soil Visual`/`Plowed Visual` trỏ tới prefab asset: tự sinh instance con trong scene, và nếu `Soil Visual` là chính object `DatThuong` thì chỉ tắt renderer đất thường chứ không tắt cả GameObject/FarmTile.
- Ô trồng đặt bằng Build Mode có thể hủy như chuồng: prompt ngoài gameplay thêm `G - Hủy ô trồng` với xác nhận 2 lần, còn menu xóa trong Build Mode bắt được cả khi click vào mesh con của `DatThuong`.
- Khi hủy ô trồng, hệ thống clear `BuildSurfaceCell`, lưu `BuildPersistence` ngay và `FarmTile.OnDestroy` dọn thanh nước/chữ nổi độc lập để không còn sót UI trên không.

### Editor cần làm
- Gán model đất mới xây vào `Soil Visual`, model đất đã cuốc vào `Plowed Visual`.
- Đảm bảo các `CropDefinition` cần hiển thị cây đã có `cropPrefab`; nếu thiếu prefab cây thì sẽ không còn fallback màu vàng/xanh/đỏ.

## [2026-07-01] — Tránh bàn phím mềm che input mobile

### Added
- Thêm `MobileKeyboardAvoidance` dùng chung cho UI Toolkit để đo/ước lượng chiều cao bàn phím mềm iOS/Android.

### Changed
- Login/Register tự dịch panel lên khi người chơi focus ô nhập, giúp username/password/email không bị bàn phím che.
- Chat dùng lại helper chung thay cho fallback cục bộ; vẫn giữ offset riêng khi đang ở Build Mode.

## [2026-07-01] — Shop thu mua đá quý và filter chợ cá

### Added
- Thêm `Shop_GemShop` dạng SellOnly để thu mua 6 item đá quý `gem_*`.
- Thêm filter `Cá` (`food`) và `Đá quý` (`materials`) trong Shop Popup, dùng cho Fish Shop/Gem Shop.
- Thêm helper toast dùng chung trong `ScreenToast` để toast nhận/mua vật phẩm tự resolve tên và icon từ `ItemDatabase`.

### Changed
- Cập nhật `ShopDataGenerator` để chạy lại generator vẫn sinh/cập nhật đủ 8 shop, gồm shop đá quý.
- Chuyển toast câu cá, đào đá, thu hoạch cây/thú, múc nước, mua/bán shop, điểm danh và vòng quay sang helper item-icon chung.
- Build Mode nay hiển thị icon `Go`/`Da` ở pill vật liệu và chi phí từng ô xây.
- `ItemDataGenerator` map `wood_01`, `stone_01`, `watering_water_01` sang `BoSungIcon/Go.png`, `Da.png`, `NuocTuoi.png`; item nước tưới đã được gắn icon texture.

### Editor cần làm
- Gắn `Shop_GemShop` vào `ShopZoneTrigger` hoặc `MerchantNPC` ở quầy/NPC thu mua đá quý muốn dùng.
- Test bán đá quý từ túi đồ: icon hiện đúng, số lượng bị trừ, Point cộng theo `sellPrice`.
- Test thêm: chặt cây/đào đá/múc nước/thu hoạch/mua bán shop để xác nhận toast có icon đúng và không đè nhau.

## [2026-06-30] — Nền MVP đảo đào khoáng

### Added
- Mở nền code cho đảo đào khoáng: bản đồ thế giới có thể chọn `mine`, `IslandTravelManager` có thể đi tới `MineScene`, và tương tác đào đá được phép ở cả `city` lẫn `mine`.
- `ResourceSpawner` hỗ trợ gắn prefab cây/đá thật, raycast xuống nền nếu cần, và random lại vị trí khi tài nguyên hồi sinh để đá ở đảo mỏ có thể xuất hiện lại ở vị trí khác trong vùng spawn.
- `ResourceSpawner` hỗ trợ vùng spawn kiểm soát bằng các `Collider` anh đặt trong scene: random có trọng số theo diện tích vùng, giữ khoảng cách tối thiểu giữa tài nguyên, có gizmo trong Scene view, và vẫn fallback về `spawnRadius` nếu chưa gán vùng.

### Changed
- Giữ câu cá chỉ ở `city`; riêng đào đá được mở rộng từ city-only sang `city` hoặc `mine`.
- Thêm fallback runtime `MineMap -> MineScene` để dữ liệu Inspector/scene cũ chưa dọn vẫn có thể load đúng scene mới trong lúc setup Unity.

### Editor cần làm
- Trong Build Settings, thay entry cũ `Assets/_Project/_Scenes/MineMap.unity` bằng `Assets/_Project/_Scenes/MineScene.unity`.
- Trong `IslandTravelManager` ở scene chính, set island `mine` dùng `sceneName = MineScene`.
- Trong `MineScene`, đặt một `ResourceSpawner` cho đảo mỏ: `spawnerID = Mine`, `treeCount = 0`, `rockCount` theo mật độ test, bật `randomizePositionOnRespawn`, gắn `rockPrefab` nếu có.
- Với map đảo méo/rộng, tạo vài `BoxCollider` cao dạng trigger phủ các vùng mặt đất hợp lệ, kéo các collider đó vào `ResourceSpawner > Spawn Areas`; script sẽ tự rải đá bên trong các vùng này thay vì spawn hình tròn.
- Nếu bật `snapSpawnToGround`, tạo layer/mask riêng cho nền đảo mỏ để raycast không bắt nhầm collider khác.
- Nếu đã từng chạy spawner trước đó, dùng context menu `Clear Saved Resource State` trên component hoặc đổi `spawnerID` để tài nguyên được sinh lại theo vùng mới.

### Chưa làm trong MVP này
- Giới hạn 10 lượt đào/ngày.
- Nâng cấp cuốc lv2/lv3 theo chi phí Point.
- Shop/NPC thu mua đá quý.

### Changed Files
- `Assets/_Project/Scripts/Managers/IslandTravelManager.cs`
- `Assets/_Project/UI/MapPopupController.cs`
- `Assets/_Project/Scripts/Environment/FarmInteractionController.cs`
- `Assets/_Project/Scripts/Managers/ResourceSpawner.cs`
- `Assets/_Project/Scripts/Environment/HarvestableResource.cs`

---
## [2026-06-29] — Point wording, fish reward icon, water color, poultry final meat

### Added
- Câu cá thành công giờ gọi `ScreenToast.ShowInfoWithIcon`: toast vẫn hiện kết quả như cũ, đồng thời có icon cá bay nhẹ/fade phía trên toast; nếu thiếu icon item thì dùng fallback an toàn.
- Thêm `Assets/_Project/Docs_KichBan/CacLoaiCa.md` để ghi dữ liệu cá mới: giá Point, tỉ lệ câu theo tier, item ID đề xuất và đường dẫn icon.
- Thêm `Assets/_Project/Docs_KichBan/CacLoaiDaQuy.md` để ghi dữ liệu đá quý mới: giá Point, tỉ lệ đào trúng, số viên, ghi chú nâng cấp cuốc, giới hạn lượt/ngày và đường dẫn icon.
- Thêm 14 `ItemDefinition` cá mới trong `Assets/Resources/Items/`, gắn icon từ `Assets/Sprites/icon/CacLoaiCa/` để shop/túi đồ/toast dùng chung một nguồn dữ liệu.
- Thêm 6 `ItemDefinition` đá quý trong `Assets/Resources/Items/`, gắn icon từ `Assets/Sprites/icon/CacLoaiDaQuy/` để túi đồ/toast dùng chung một nguồn dữ liệu.
- Gắn 4 icon thịt gia cầm mới từ `Assets/Sprites/icon/ThitGiaCam/` cho thịt gà, thịt vịt, thịt ngỗng và thịt đà điểu.

### Changed
- Popup biểu cảm nhân vật trong chat/HUD chỉ còn 2 động tác được duyệt: bỏ `Laughing` và `Dancing`, đồng thời đổi 2 nút còn lại sang icon ảnh `Assets/Sprites/icon/BoSungIcon/VayTay.png` và `Assets/Sprites/icon/BoSungIcon/ChiTay.png`.
- Đổi text hiển thị tiền từ `POS` sang `Point` ở HUD, shop, inventory/animal info, Heo đất, Tiệm rèn, Event/Attendance, toast/log demo và test UI. Giữ nguyên tên biến/API nội bộ `POS/UPOS` để tránh rủi ro đổi logic kinh tế.
- Đổi text hiển thị tiền premium từ `UPOS` sang `UPoint`.
- Cấu hình tỉ lệ câu cá theo bảng khách chốt: 25 Point = 2%, 15 Point = 4%, 10 Point = 7%, 6 Point = 17%, 4 Point = 25%, 2 Point = 45%; sau khi trúng nhóm giá, game chọn ngẫu nhiên một loài trong nhóm đó.
- Fish Shop đã whitelist toàn bộ cá mới để cá câu được bán lại đúng tên, icon và giá Point.
- `ItemDatabase.GetItem` có fallback load `Resources/Items/{id}` để item mới vẫn resolve được trước khi Unity/generator refresh lại list trong `ItemDatabase.asset`.
- Đào đá giữ đá thường 100% với 10 rock, sau đó roll thêm 1 đá quý theo bảng khách chốt: Ruby 1%, Amethyst 2%, Fire Quartz 5%, Green Calcite 12%, Orange Calcite 30%, Kyanite 50%.
- Khi đào trúng đá quý, toast gọi `ScreenToast.ShowInfoWithIcon` để hiện icon đá quý; item đá quý hiển thị trong tab Nguyên liệu của túi đồ qua `ItemDefinition.iconTexture`.
- Hồi sinh tài nguyên gỗ/đá giờ bù thời gian khi người chơi thoát app: `HarvestableResource` lưu mốc `respawnEndUnix`, `ResourceSpawner` lưu khi đào/chặt xong, pause app và quit app; save cũ chỉ có `respawnTimer` vẫn đọc được.
- Cutscene thuyền không còn bị cắt cứng ở 35 giây: timeout failsafe tự tính theo tổng quãng đường waypoint + tốc độ thuyền + buffer, nên thuyền có thời gian cập bờ trước khi vào gameplay.
- Chỉnh màu nước biển Farm/City sáng hơn và xanh hơn trên material đang được scene dùng, không đổi shader/sóng.
- Khách đổi lại: 4 gia cầm gà/đà điểu/ngỗng/vịt vẫn lấy trứng theo chu kỳ, nhưng vụ cuối sẽ trả thịt theo Product 2 trong `VatNuoi2.md`; thịt gia cầm bán được ở Mini Garden.
- Toast vụ cuối của gia cầm chuyển sang `ScreenToast.ShowInfoWithIcon`, nên lúc nhận thịt sẽ hiện icon thịt mới; túi đồ và shop dùng cùng `ItemDefinition.iconTexture`.
- Điều chỉnh CodeMagic iOS: chỉ upload IPA lên App Store Connect, bỏ tự động `submit_to_testflight` để bên build add vào Internal Testing thủ công.
- Tăng CodeMagic iOS `BUILD_NUMBER` lên `2` để upload lại bản `0.1.1 (2)` lên App Store Connect.
- Bake trực tiếp `0.1.1 (2)` vào exported iOS Xcode project và thêm bước kiểm tra version/build của IPA trước khi publish.
- Tăng tiếp iOS build lên `0.1.1 (4)` và thêm `ITSAppUsesNonExemptEncryption=false` vào `ios/Info.plist`; CodeMagic cũng ép key này sau mỗi lần Unity export để tránh App Store Connect treo build ở trạng thái Missing Export Compliance.

### Changed Files
- `Assets/_Project/Scripts/Environment/ScreenToast.cs`
- `Assets/_Project/UI/FishingOverlayController.cs`
- `Assets/_Project/Docs_KichBan/CacLoaiCa.md`
- `Assets/_Project/Docs_KichBan/CacLoaiDaQuy.md`
- `Assets/_Project/Scripts/Data/ItemDatabase.cs`
- `Assets/Resources/Items/fish_ca_*.asset`
- `Assets/Resources/Items/gem_*.asset`
- `Assets/_Project/Data/Shops/Shop_FishShop.asset`
- `Assets/_Project/Scripts/Cutscenes/BoatCutscene.cs`
- `Assets/_Project/Scripts/Environment/HarvestableResource.cs`
- `Assets/_Project/Scripts/Environment/FarmInteractionController.cs`
- `Assets/_Project/Scripts/Managers/ResourceSpawner.cs`
- `Assets/IgniteCoders/Simple Water Shader/Resources/Water_mat_01.mat`
- `Assets/Art/Environment/Materials/water.mat`
- `Assets/_Project/Scripts/Editor/ItemDataGenerator.cs`
- `Assets/_Project/Scripts/Editor/ShopDataGenerator.cs`
- `Assets/_Project/Scripts/Managers/InventoryManager.cs`
- `Assets/_Project/Data/Shops/Shop_MiniGarden.asset`
- `Assets/Resources/Items/Animal_{chicken,ostrich,goose,duck}_01.asset`
- `Assets/Resources/Items/{chicken,ostrich,goose,duck}_meat_01.asset`
- `Assets/Sprites/icon/ThitGiaCam/{ThitGa,ThitVit,ThitNgong,ThitDaDieu}.png`
- `codemagic.yaml`
- `ios/Info.plist`
- `ios/UnityFramework/Info.plist`
- `ios/Unity-iPhone.xcodeproj/project.pbxproj`
- `Assets/_Project/UI/**`
- `Assets/_Project/Scripts/Managers/EconomyManager.cs`
- `Assets/_Project/Scripts/Environment/FarmInteractionController.cs`
- `Assets/_Project/Scripts/Data/*Definition.cs`
- `Assets/Test/UITestHelper.cs`

---
## [2026-06-27] — Fix bundle id cho CodeMagic iOS signing

### Fixed
- Thêm bước trong workflow CodeMagic Xcode-only để ép bundle id của Xcode project đã export sẵn về `com.ywonder.greenfarm` trước khi chạy `xcode-project use-profiles`.
- Mục tiêu là để CodeMagic match đúng provisioning profile App Store `ywonderland_greenfarm_appstore` mà bên build đã tạo cho bundle id `com.ywonder.greenfarm`.
- Đặt Unity iPhone Bundle ID trong `ProjectSettings` thành `com.ywonder.greenfarm` để các lần export iOS sau khớp Apple Developer và App Store Connect.

### Changed Files
- `codemagic.yaml`
- `ProjectSettings/ProjectSettings.asset`

---
## [2026-06-27] — Dọn dữ liệu thịt gia cầm trong loadout demo

### Fixed
- Bỏ các item thịt gia cầm khỏi loadout rich/demo để tester không còn được cấp sẵn "Thịt gà", "Thịt vịt", "Thịt ngỗng", "Thịt đà điểu" rồi hiểu nhầm là hàng phải bán được.
- Đánh dấu các item thịt gia cầm là không bán được (`canSell=false`, `sellPrice=0`), khớp quyết định lúc 27/06: gia cầm chỉ lấy trứng.
- Xóa dữ liệu hiển thị sản phẩm phụ dạng thịt khỏi 4 definition gia cầm, vẫn giữ sản phẩm chính là trứng.

### Superseded
- Cập nhật 29/06: quyết định này đã bị khách đổi lại; 4 gia cầm nay có thịt ở vụ cuối và thịt bán được.

### Changed Files
- `Assets/_Project/Scripts/Managers/InventoryManager.cs`
- `Assets/_Project/Scripts/Editor/ItemDataGenerator.cs`
- `Assets/Resources/Items/Animal_chicken_01.asset`
- `Assets/Resources/Items/Animal_ostrich_01.asset`
- `Assets/Resources/Items/Animal_goose_01.asset`
- `Assets/Resources/Items/Animal_duck_01.asset`
- `Assets/Resources/Items/chicken_meat_01.asset`
- `Assets/Resources/Items/ostrich_meat_01.asset`
- `Assets/Resources/Items/goose_meat_01.asset`
- `Assets/Resources/Items/duck_meat_01.asset`

---
## [2026-06-26] — iOS CI, CodeMagic và icon game

### Added
- Thêm `README.md` ở root repo để người clone `main` nắm cách mở project, build, trạng thái backend và nhánh làm việc.
- Thêm `codemagic.yaml` ở root repo để CodeMagic nhận workflow build Unity iOS.
- Thêm workflow CodeMagic Xcode-only/TestFlight: build từ Xcode project iOS đã export sẵn, không cần activate Unity trên CodeMagic.
- Thêm Xcode project iOS đã export sẵn trong `ios/` để phục vụ workflow TestFlight không chạy Unity.
- Thêm rule Git LFS cho binary lớn của iOS export (`.a`, `.resS`, `usymtool`, `usymtoolarm64`).
- Thêm `Assets/_Project/Editor/BuildScript.cs` để CI có thể export Xcode project iOS từ Unity bằng batch mode.
- Thêm asset thumbnail/icon game trong `Assets/_Project/UI/Sprites/`.
- Gắn `ThumbnailGame.jpg` vào icon Standalone và Android adaptive icon trong `ProjectSettings/ProjectSettings.asset`.

### Fixed
- Sửa lỗi compile trong build script do namespace `YWonderLand.Environment` che mất `System.Environment`.
- Đổi API set bundle id iOS sang `NamedBuildTarget.iOS`, bỏ warning obsolete của Unity 6.

### Changed Files
- `README.md`
- `.gitattributes`
- `codemagic.yaml`
- `ios/**`
- `Assets/_Project/Editor/BuildScript.cs`
- `Assets/_Project/Editor/BuildScript.cs.meta`
- `Assets/_Project/UI/Sprites/ThumbnailGame.jpg`
- `Assets/_Project/UI/Sprites/ThumbnailGame.jpg.meta`
- `Assets/_Project/UI/Sprites/Black.jpg`
- `Assets/_Project/UI/Sprites/Black.jpg.meta`
- `ProjectSettings/ProjectSettings.asset`

---
## [2026-06-26] — Interaction, chuồng, câu cá và icon build/popup

### Added
- Câu cá chuyển sang luồng hành động có thời lượng 8.7s khớp clip `Fishing`, có UI hủy/progress và nhả chuột trong lúc hành động đang chạy.
- Kết thúc câu cá mới cộng cá vào túi: cá thường hoặc cá hiếm, tỉ lệ cá hiếm 20%.
- Popup chuồng hỗ trợ xem theo nhóm: click vào vùng chuồng liền kề sẽ hiện danh sách toàn bộ thú trong chuồng dưới dạng card, chọn card nào thì hành động đúng con đó.
- Card thú trong popup chuồng dùng icon thật từ `ItemDatabase`, không còn dùng chữ cái/emoji fallback nếu item đã có ảnh.
- Build Mode dùng icon công trình từ `Assets/Sprites/icon/BoSungIcon/` cho Ruộng, Đường đá và Chuồng.

### Changed
- Tầm tương tác câu cá được chỉnh để đứng trên bờ vẫn câu được, hiện giới hạn khoảng 5m.
- Khi đang câu cá, prompt `F Câu cá` được ẩn để không đè lên nút hủy/progress.
- Các hành động thế giới đang được chuẩn hóa theo hướng có thể hủy, chạy theo độ dài clip: chặt cây, đào khoáng, cuốc/trồng/tưới, cho ăn.
- Popup chuồng gom nút hành động vào một hàng; `Chữa bệnh` và `Vaccine` vẫn hiển thị nhưng bị khóa/mờ vì dữ liệu vaccine/bệnh chưa được khách chốt.
- Thanh đói/thanh nước trên world bar dùng material URP Unlit riêng để tránh lỗi màu tím trong build.

### Fixed
- Không còn hiện UI câu cá khi nhân vật đang bơi/dưới nước.
- Chặn lỗi `Collider.ClosestPoint` với collider không hỗ trợ/non-convex khi tính khoảng cách tương tác.
- Sửa các lỗi prompt/click của ô đất, chuồng, con vật sau các lần chỉnh raycast/tầm tương tác.
- Hủy chuồng liền kề giờ dọn sạch UI tương tác còn sót, xóa object thú trong chuồng, trả con giống về túi và hoàn vật liệu theo rule hoàn tài nguyên hiện có.
- Build Mode trên Android tiếp tục dùng touch/pointer để đặt công trình; các glyph dễ thành ô vuông được thay bằng chữ/icon an toàn hơn.

### Changed Files
- `Assets/_Project/Scripts/Environment/FarmInteractionController.cs`
- `Assets/_Project/Scripts/Environment/FarmAnimal.cs`
- `Assets/_Project/Scripts/Environment/FarmTile.cs`
- `Assets/_Project/Scripts/Environment/GhostPlacementController.cs`
- `Assets/_Project/Scripts/Environment/PenEnclosure.cs`
- `Assets/_Project/UI/AnimalInteractionPopup.uxml`
- `Assets/_Project/UI/AnimalInteractionPopupController.cs`
- `Assets/_Project/UI/FishingOverlay.uxml`
- `Assets/_Project/UI/FishingOverlayController.cs`
- `Assets/_Project/UI/BuildModeOverlay.uxml`
- `Assets/_Project/UI/BuildModeOverlayController.cs`
- `Assets/_Project/UI/GameHUDController.cs`
- `Assets/_Project/UI/Styles/AnimalInteractionPopup.uss`
- `Assets/_Project/UI/Styles/BuildModeOverlay.uss`
- `Assets/Art/Environment/Material/WorldBar_Unlit.mat`
- `Assets/Resources/Materials/WorldBar_Unlit.mat`
- `Assets/Building/New/fence/Fence.prefab`
- `Assets/Sprites/icon/BoSungIcon/**`

---
## [2026-06-25] — Existing character login flow

### Changed
- Thêm `characterCreated` vào `player_profile` ở Unity client và Node server stub.
- Login giờ nạp `/player/profile` trước; nếu `characterCreated=true` thì bỏ qua màn tạo nhân vật.
- `DemoRich01` đến `DemoRich05` được coi là tài khoản đã có nhân vật, nên tester đăng nhập là vào game, không phải chọn giới tính/đặt tên.
- Màn tạo nhân vật sẽ đánh dấu profile là đã tạo nhân vật khi người chơi xác nhận tên/giới tính.

### Changed Files
- `Assets/_Project/UI/LoginScreenController.cs`
- `Assets/_Project/Scripts/Managers/GameManager.cs`
- `Assets/_Project/Scripts/Backend/PlayerProfileService.cs`
- `server/index.js`
- `server/README.md`
- `docs/API_CONTRACTS.md`
- `docs/ARCHITECTURE.md`
- `docs/DB_SCHEMA.md`
- `docs/TECHNICAL_DESIGN.md`

---
## [2026-06-25] — Shop tab icon cleanup

### Changed
- Popup shop bỏ emoji icon trong các tab chế độ/danh mục: `Mua`, `Bán`, `Hạt giống`, `Vật nuôi`, `Dụng cụ`, `Vật phẩm`.
- Icon ảnh của hàng hóa trong card item và panel chi tiết vẫn giữ theo `ItemDefinition.iconTexture/iconSprite`.
- Tên shop dài được căn giữa trong vùng header còn trống, không còn tràn xuống dưới pill POS hoặc nút đóng.
- Bỏ thuộc tính `z-index` không được UI Toolkit hỗ trợ trong `ShopPopup.uss`.

### Changed Files
- `Assets/_Project/UI/ShopPopup.uxml`
- `Assets/_Project/UI/Styles/ShopPopup.uss`

---
## [2026-06-25] — Workshop icon rendering

### Changed
- Popup Tiệm rèn hiển thị icon dụng cụ và nguyên liệu nâng cấp bằng ảnh asset thay vì emoji label.
- Gắn `iconTexture` cho nhóm dụng cụ/vật liệu: rìu, cuốc, cần câu, xô tưới, cuốc chim, gỗ, đá, sắt, quặng.
- Bỏ thuộc tính `z-index` không được UI Toolkit hỗ trợ trong `WorkshopPopup.uss`.

### Changed Files
- `Assets/_Project/UI/WorkshopPopup.uxml`
- `Assets/_Project/UI/WorkshopPopupController.cs`
- `Assets/_Project/UI/Styles/WorkshopPopup.uss`
- `Assets/Resources/Items/axe_01.asset`
- `Assets/Resources/Items/hoe_01.asset`
- `Assets/Resources/Items/fishing_rod_01.asset`
- `Assets/Resources/Items/watering_can_01.asset`
- `Assets/Resources/Items/pickaxe_01.asset`
- `Assets/Resources/Items/wood_01.asset`
- `Assets/Resources/Items/stone_01.asset`
- `Assets/Resources/Items/iron_01.asset`
- `Assets/Resources/Items/ore_01.asset`

---
## [2026-06-25] — Quest popup icon cleanup

### Changed
- Popup Nhiệm vụ bỏ emoji kiếm/quà/check trong danh sách nhiệm vụ; nhiệm vụ đang làm/đợi nhận dùng icon ảnh từ `Assets/Sprites/icon/`.
- Nhiệm vụ đã nhận thưởng hiển thị dấu tích visual trong ô vuông, đồng bộ cách nhìn với Hộp thư.
- Ô phần thưởng nhiệm vụ dùng icon từ `ItemDatabase` hoặc `Assets/Sprites/icon/BoSungIcon/`, không còn render emoji cũ.
- Bỏ các thuộc tính `z-index` không được UI Toolkit hỗ trợ trong `QuestPopup.uss` để tránh warning.

### Changed Files
- `Assets/_Project/UI/QuestPopup.uxml`
- `Assets/_Project/UI/QuestPopupController.cs`
- `Assets/_Project/UI/Styles/QuestPopup.uss`

---
## [2026-06-25] — Mailbox read/reward icons

### Changed
- Hộp thư: ô trạng thái thư đã đọc hiển thị dấu tích vẽ bằng UI thay vì emoji/glyph dễ thành ô vuông.
- Badge quà trong danh sách thư dùng icon mới `Assets/Sprites/icon/SanPham/VatPham/giftbox.png`; quà đã nhận hiển thị dấu tích.
- Phần thưởng đính kèm trong chi tiết thư dùng icon ảnh từ `ItemDatabase` hoặc `Assets/Sprites/icon/BoSungIcon/`, không còn render emoji cũ.

### Changed Files
- `Assets/_Project/UI/MailboxPopupController.cs`
- `Assets/_Project/UI/Styles/MailboxPopup.uss`

---
## [2026-06-25] — Piggy bank icon cleanup

### Changed
- Popup Heo đất bỏ emoji icon ở balance pill, tab, gói gửi, nút gửi và title thời gian còn lại.
- Icon heo ở trạng thái đang gửi/lịch sử gửi chuyển sang ảnh `Assets/Sprites/icon/BoSungIcon/Piggy.png`.
- Bỏ các thuộc tính `z-index` không được UI Toolkit hỗ trợ trong `PiggyBankPopup.uss` để tránh warning.

### Changed Files
- `Assets/_Project/UI/PiggyBankPopup.uxml`
- `Assets/_Project/UI/PiggyBankPopupController.cs`
- `Assets/_Project/UI/Styles/PiggyBankPopup.uss`

---
## [2026-06-25] — Event popup icon cleanup

### Changed
- Popup Sự kiện & Quà tặng bỏ icon trang trí ở tiêu đề.
- Pill thời gian sự kiện bỏ emoji đồng hồ, chỉ giữ chữ thời gian.
- Các tab trong popup Sự kiện giờ chỉ hiển thị chữ, không còn emoji icon.
- Các card gói ưu đãi không còn render emoji icon của gói; vẫn giữ tag, tên gói, mô tả, giá và trạng thái mua/hết hàng.
- Bảng điểm danh hiển thị icon ảnh từ `Assets/Sprites/icon` khi có, riêng quà cây trồng dùng icon đang gắn trong `ItemDatabase`.
- Vòng quay may mắn hiển thị icon ảnh cho các phần thưởng từ `Assets/Sprites/icon` và `ItemDatabase`; tiêu đề, hub giữa vòng, nút QUAY bỏ emoji text.
- Bỏ các thuộc tính `z-index` không được UI Toolkit hỗ trợ trong `EventPopup.uss` để tránh warning.

### Changed Files
- `Assets/_Project/UI/EventPopup.uxml`
- `Assets/_Project/UI/EventPopupController.cs`
- `Assets/_Project/UI/Styles/EventPopup.uss`

---
## [2026-06-25] — Leaderboard tab icons

### Changed
- Popup Leaderboard đổi 5 tab `EXP`, `Level`, `Fashion`, `Pet`, `Rich` sang icon ảnh từ `Assets/Sprites/icon/BoSungIcon/`.
- Tab `Level` dùng icon riêng `lv.png`.
- Hạng 1/2/3 trong bảng Leaderboard dùng icon huy chương vàng/bạc/đồng thật thay cho emoji.
- Cột giá trị Leaderboard: Fashion hiện số bộ trang phục, Pet hiện số lượng pet, Rich bỏ glyph kim cương và chỉ còn số Gold.

### Changed Files
- `Assets/_Project/UI/LeaderboardPopupController.cs`
- `Assets/_Project/UI/Styles/LeaderboardPopup.uss`

---
## [2026-06-25] — Inventory item icon rendering

### Fixed
- Kho đồ giờ hiển thị `ItemDefinition.iconTexture/iconSprite` cho card vật phẩm và panel chi tiết, đồng bộ với cách cửa hàng đang hiển thị icon.
- Vật phẩm chưa có ảnh được gán vẫn fallback về emoji/text như cũ.

### Changed Files
- `Assets/_Project/UI/InventoryPopupController.cs`
- `Assets/_Project/UI/Styles/InventoryPopup.uss`

---
## [2026-06-25] — HUD POS/UPOS pill

### Added
- Thêm pill `UPOS` ở HUD top-right để hiển thị premium currency song song với `POS`.
- `EconomyManager` có event `OnUPOSChanged` và helper `AddUPOS`/`SpendUPOS` để số dư premium cập nhật live.

---
## [2026-06-25] — APK build-mode touch + safe glyph hotfix

### Fixed
- `BuildModeOverlayController` không còn phụ thuộc `Mouse.current`/`Keyboard.current` để đặt công trình; Android tap giờ đi qua `Touchscreen.current`, ép ghost raycast ngay tại điểm tap trước khi pin vị trí.
- `GhostPlacementController` đọc cả touch lẫn mouse để ghost cập nhật trên APK, vẫn giữ mouse cho Editor/Windows.
- Thay glyph điều khiển dễ lỗi font Android (`✕`, `✔`, `⌂`) trong các nút close/build placement bằng ASCII an toàn (`X`, `OK`, `B`) để tránh nút hiện thành ô vuông trên điện thoại.

### Changed Files
- `Assets/_Project/Scripts/Environment/GhostPlacementController.cs`
- `Assets/_Project/UI/BuildModeOverlayController.cs`
- `Assets/_Project/UI/BuildModeOverlay.uxml`
- `Assets/_Project/UI/*.uxml` (các nút close chuyển `✕` -> `X`)

---
## [2026-06-24] — Tối ưu cảm ứng Mobile + Sprint theo hướng PUBG/FreeFire

### Changed
- **Chốt trạng thái demo build:** giữ `GameTimeConfig.SecondsPerGameDay = 60f` để cây/thú chạy nhanh cho APK/Windows test chéo; không đổi về 24h thật trước demo.
- **Ghi expected timing cho tester:** cây ngắn ngày ~60s sau tưới; tutorial 24s; Sa Chi/Sầu Riêng ~28 phút; Chanh dây ~90 phút; thú nhanh gồm vịt 60s, gà 120s, dê/ngỗng 180s, đà điểu 360s, bò 420s.
- **NPC tutorial marker:** thay dấu chấm than primitive bằng prefab `Assets/_Project/Prefabs/ExclamationMark.prefab`, vẫn giữ fallback primitive nếu scene chưa gán prefab.
- **VPS/backend:** xác nhận client có khung REST nhưng mới phủ `auth/profile/tutorialCompleted`; deploy VPS chỉ đủ cho online tối thiểu nếu chạy `server/` stub + cấu hình `BackendConfig.baseUrl`. Backend thật cho POS/inventory/farm/cây/thú/server-time/IAP là phase riêng sau demo.
- **Điều khiển mobile:** hoàn thiện luồng `Sprint` hold + tap, auto-run không bị auto-dừng khi xoay camera; đổi hướng bằng joystick mới break sprint.
- **Camera cảm ứng:** smoothing riêng cho touch, khóa góc nhìn phù hợp kiểm duyệt; giảm tối thiểu bắn ngang.
- **Đi lùi / đổi hướng:** sửa lại hành vi khi kéo joystick lùi để nhân vật quay đầu trước rồi chạy theo hướng mới.
- **Build/chuồng:** trạng thái gõ búa + preview ghost build tiếp tục cập nhật theo hướng trực quan.
- **Popup/flow học chơi:** NPC tutorial giữ nhịp chậm hơn, không spam thoại liên tiếp.

### Changed Files
- `Assets/_Project/Scripts/Player/PlayerController.cs`
- `Assets/_Project/Scripts/Camera/ThirdPersonCamera.cs`
- `Assets/_Project/UI/GameHUD.uxml`
- `Assets/_Project/UI/GameHUDController.cs`
- `Assets/_Project/UI/Styles/GameHUD.uss`
- `Assets/_Project/Scripts/Environment/GhostPlacementController.cs`
- `Assets/_Project/Scripts/Environment/FenceAutoConnect.cs`

## [2026-06-20] — Điều khiển mobile + Build snap theo ô đất thật + Hệ chuồng từ hàng rào + Thông tin con vật

### Added
- **Điều khiển cảm ứng (GameHUD):** joystick ảo điều khiển di chuyển (`PlayerController.SetMoveInput`); nút **Sprint giữ-để-chạy** (fix `Clickable` nuốt event bằng `TrickleDown`); nút **Jump** (`TriggerJump`); nút **X hủy hoạt ảnh** (`CancelAction`, tự hiện khi `IsBusy`).
- **Build snap theo Ô ĐẤT THẬT:** `BuildSurfaceCell` (gắn lên khối cube map, cube=0.8) thay lưới ảo lệch; ghost ướm vào tâm mặt trên khối. Tool Editor **"sơn vùng"** (`BuildSurfaceCellSetup`): kéo BoxCollider trùm vùng → gắn hàng loạt khối `cube*` trong vùng (+ collider). Gizmo hiện trạng ô.
- **Hệ chuồng từ hàng rào (task #6):** rào = hộp vuông trên 1 ô → **ô có rào = ô chuồng**. `PenEnclosure.FindPen` BFS cụm ô-rào liền nhau (nhiều rào kề = chuồng to). Ngắm/click ô rào → "Thả thú" → chọn loài → validate `penSlots` vs số ô còn trống → thả (`SetAnimal`) hoặc `ScreenToast` báo lỗi. `AnimalPrefabLibrary` (itemId→prefab thú + spawnHeightOffset). Click thẳng (PC) + bấm chữ (mobile) đều chạy.
- **Popup Thông tin con vật:** hiện giá mua / số ô chuồng / thức ăn chính-phụ / sản phẩm. Thêm trường vào `AnimalDefinition`; điền data 10 con qua generator (nguồn: bảng VatNuoi khách). Restyle popup theo Cozy Dark Palia.
- **Thông tin con vật ở Shop + Túi đồ:** chèn "Thông tin nuôi" (giá/ô/thức ăn) vào mô tả khi chọn con vật. `AnimalManager.LookupDefinition` (tra Instance, fallback Resources → chạy kể cả khi scene chưa gắn AnimalManager).
- **Mặt đường đá (paving):** item "Đường đá" trong Build Mode → map `BuildPrefabLibrary` (StoneSlab).
- **Loadout test:** `InventoryManager.GiveTestLoadout()` + cờ `giveTestLoadoutOnStart` — nạp nông sản/sản phẩm/vật liệu/hạt + tiền để test NPC mua/bán.

### Changed
- **Tương tác ngắm theo điểm chạm/tâm** cho ổn định; **nút gợi ý "Chặt cây"... bấm/tap được** (fix picking-mode cha Ignore); tia ngắm **xuyên qua hàng rào + ô bị chiếm** để tới ô chuồng.
- **Bỏ tính năng Vuốt ve** (Pet) khỏi tương tác con vật.
- **Dọn menu Build còn 3 mục** (Ruộng / Đường đá / Chuồng); ẩn 4 tab cũ.
- **Fix ghost Build luôn báo đỏ:** `GhostPlacementController` đổi `Physics.Raycast` → `RaycastAll` + tìm `BuildSurfaceCell` gần nhất (bỏ qua collider nền/mesh đảo chắn trước).
- **Lập task Hệ NPC** (theo kịch bản "10+ NPC"): shop keeper đa-NPC, Maid, Pet, NPC mỏ, NPC câu cá, AI chat (xem task.md).

### Changed Files
- `Scripts/Player/PlayerController.cs`, `UI/GameHUD.uxml`, `UI/GameHUDController.cs`, `UI/Styles/GameHUD.uss`
- `Scripts/Environment/{BuildSurfaceCell,PenEnclosure,AnimalPrefabLibrary,ScreenToast}.cs` [NEW], `GhostPlacementController.cs`, `FarmInteractionController.cs`, `PetInteraction.cs`
- `Scripts/Editor/BuildSurfaceCellSetup.cs` [NEW], `Scripts/Editor/ItemDataGenerator.cs`, `Scripts/Data/AnimalDefinition.cs`
- `UI/AnimalInteractionPopup.uxml`, `UI/AnimalInteractionPopupController.cs`, `UI/Styles/AnimalInteractionPopup.uss` [NEW]
- `UI/ShopPopupController.cs`, `UI/InventoryPopupController.cs`, `UI/BuildModeOverlayController.cs`, `UI/BuildModeOverlay.uxml`
- `Scripts/Managers/{AnimalManager,InventoryManager}.cs`, `task.md`

---
## [2026-06-19] — Build Mode trực quan: ghost mờ kiểu ROK + bù pivot + hàng rào tự nối

### Changed
- **Bỏ lưới hiển thị** trong Build Mode theo yêu cầu khách (giữ logic snap ô, chỉ tắt phần vẽ lưới).
- **Ghost preview = bản MỜ của chính prefab** (kiểu ROK/Hay Day): chọn item thấy luôn hình công trình mờ **xanh lá** (đặt được) / **đỏ** (không), theo chuột + snap lưới + xoay. Đặt = clone y hệt ghost (WYSIWYG). Item chưa khai báo prefab vẫn fallback khối Cube.
- Stretch prefab (đất/hàng rào) lên **đúng 1 ô (100%)** để đặt liền kề khít sát.

### Added
- **Tự bù pivot lệch** (`MakeCenteredClone`): bọc prefab vào wrapper căn tâm cụm mesh → model artist export pivot lệch (vd Fence lệch 88m) vẫn hiện/đặt đúng ngay vị trí nhắm, xoay quanh tâm. Không cần sửa prefab.
- **`FenceAutoConnect`**: hàng rào kề nhau (trực giao) tự **tắt cạnh tiếp giáp** ở cả hai → nối liền thành vùng quây (Minecraft-style), không cần nhiều prefab biến thể. Chọn cạnh tắt theo **vị trí thực** (không phụ thuộc tên), refresh sau 1 frame để vị trí ổn định.

### Fixed
- Ghost prefab "tàng hình"/đặt văng xa do pivot model lệch tâm → đã bù tự động.
- Hàng rào tắt cạnh lung tung khi đặt nhiều ô → do tính tâm khi vị trí chưa ổn định + đo bounds khi cạnh đã tắt; đã chụp tâm 1 lần lúc đủ cạnh + delay 1 frame.

### Changed Files
- `Scripts/Environment/GhostPlacementController.cs` *(refactor ghost = prefab mờ + bù pivot)*, `FenceAutoConnect.cs` [NEW]
- `UI/BuildModeOverlayController.cs` (tắt lưới)

---
## [2026-06-18] — Tutorial mới (NPC ông lão khó tính) + Cho ăn qua túi + sửa thuyền cutscene

### Added
- **Viết lại TutorialManager theo flow mới** (Giai đoạn 1 — đảo nông trại): Lên đảo (chào) → tới Cây → **chặt cây** → tới Mỏ → **đào khoáng** → tới Bãi ruộng → **xây ruộng** (Build) → cuốc → trồng → tưới → thu hoạch → tới Bãi chuồng → **xây chuồng** → **thả thú** → **cho ăn** → hoàn thành. (Bỏ bán chợ/workshop khỏi flow tân thủ; câu cá + sang đảo = Giai đoạn 2.)
- **NPC dẫn 4 trạm** (Cây/Mỏ/Bãi ruộng/Bãi chuồng) — kéo Empty waypoint vào Inspector. Tự bắt ô đất người chơi vừa xây để theo dõi cuốc/trồng/tưới/thu hoạch.
- **Giọng NPC ông lão ~70 tuổi khó tính** (xưng tôi–cậu): câu chào, giao việc, giục khi afk, càu nhàu. Thoại NPC cập nhật theo từng bước (hết lặp câu cũ).
- **2 hook mới cho tutorial**: `AnimalPenSpawner.OnAnimalPlaced` (thả thú), `FarmAnimal.OnAnimalFed` (cho ăn).
- **Công tắc `Force Run Tutorial For Testing`**: ép chạy lại tutorial dù hồ sơ đã hoàn thành (tiện dev; nhớ tắt khi release).
- **Cho ăn động vật qua túi đồ**: click thú đói → mở túi (tab Thực phẩm) → chọn **Bắp ngô** → animation Feed (cầm `oat` tượng trưng) → trừ đồ. Cả nút "Cho Ăn" trong popup Thú nuôi cũng đi cùng luồng này.
- Thêm vật phẩm **Đà điểu, Dê, Hươu, Thỏ** vào database; **Bắp ngô** chuyển category `items` → `food` (hiện ở túi để cho ăn).

### Fixed
- **Thuyền cutscene lật ngang khi cập bến**: tự suy "góc bù model" từ rotation đã căn sẵn → giữ tư thế đúng khi xoay, nhân vật không rơi nước.
- **Animation gõ búa khi xây**: gọi đúng state `Hammering2` (trước gọi sai tên "Hammering" → nhân vật cầm búa nhưng không gõ).
- **NPC lặp lại thoại cũ / câu thoại dồn dập / câu chào mất nhanh**: thoại chuyển bước hiện trễ 2.5s, câu chào kéo 9s.

### Changed Files
- `Scripts/Tutorial/TutorialManager.cs` *(viết lại)*, `Scripts/Environment/AnimalPenSpawner.cs`, `FarmAnimal.cs` [MODIFIED]
- `Scripts/Environment/FarmInteractionController.cs`, `UI/AnimalInteractionPopupController.cs`, `UI/BuildModeOverlayController.cs` [MODIFIED]
- `Scripts/Cutscenes/BoatCutscene.cs`, `Scripts/Editor/ItemDataGenerator.cs`, `Scripts/Managers/InventoryManager.cs` [MODIFIED]

---
## [2026-06-18] — Build Mode sinh prefab thật + Chuồng & thả thú từ túi đồ

### Added
- **`BuildPrefabLibrary`**: bảng ánh xạ "tên item Build → prefab THẬT" (kéo thả Inspector). Build Mode đặt ô đất/chuồng giờ sinh **prefab thật** (có FarmTile/collider…) thay khối Cube placeholder. Hỗ trợ `stretchToFootprint` (đất co vừa ô) + `yOffset` (chỉnh chìm/nổi) + khớp từ khóa **cụ thể nhất** (tránh nhầm chuồng).
- **Hệ chuồng trại**: thanh Build có **Chuồng nhỏ (1x1) / vừa (2x2) / lớn (3x2)**. Đặt chuồng chạy animation Hammering. Mỗi loại chỉ nuôi loài đúng cỡ.
- **`AnimalPenSpawner`**: click chuồng → mở **túi đồ (tab Thú nuôi)** chọn con vật → thả vào chuồng. Giới hạn loài theo `allowedAnimals` (map itemId→prefab) — không cho bò vào chuồng gà. `maxCapacity` (demo=1), `spawnHeightOffset` chỉnh độ cao con vật.
- **Tab "Thú nuôi"** trong túi đồ (UXML + controller, lọc category `animals`).
- **Vật phẩm con vật mới**: Đà điểu, Dê, Hươu, Thỏ (`ostrich_01/goat_01/deer_01/rabbit_01`) trong ItemDataGenerator; cấp sẵn để test (`EnsureStarterAnimals`).
- **Ruộng 1x1** trên thanh Build (đặt từng ô đất nhỏ).

### Changed
- **Nhân vật xoay thẳng về ô đất** khi cuốc/gieo/tưới/thu hoạch (`PlayerController.FaceTowards`) — hết lệch do camera lệch vai GTA.
- **Camera Build Mode** dốc hơn (góc ≥84°) để nhân vật ở giữa màn hình, không bị thanh chat che.

### Fixed
- **Prefab Build bị dựng đứng/lật**: giữ rotation gốc prefab (Blender xoay) + chỉ thêm yaw, không ghi đè.
- **Thuyền cutscene lật ngang khi cập bến** (`BoatCutscene`): tự suy "góc bù model" từ rotation đã căn sẵn (`autoOffsetFromInitialRotation`) → thuyền giữ tư thế đúng khi xoay, nhân vật không rơi nước.

### Changed Files
- `Scripts/Environment/BuildPrefabLibrary.cs`, `AnimalPenSpawner.cs` [NEW]
- `Scripts/Environment/GhostPlacementController.cs`, `FarmInteractionController.cs` [MODIFIED]
- `Scripts/Player/PlayerController.cs`, `Scripts/Camera/BuildCameraController.cs`, `Scripts/Cutscenes/BoatCutscene.cs` [MODIFIED]
- `UI/BuildModeOverlayController.cs`, `UI/InventoryPopup.uxml`, `UI/InventoryPopupController.cs` [MODIFIED]
- `Scripts/Editor/ItemDataGenerator.cs`, `Scripts/Managers/InventoryManager.cs` [MODIFIED]

---
## [2026-06-17] — Thiết kế lại UI Onboarding + Gameplay tile/búa/camera

> ⚠️ Phần **UI Onboarding** bên dưới có **sự hỗ trợ của Gemini Pro 3.1** (làm khi phiên Claude đạt giới hạn). Toàn bộ thay đổi phiên này **CHƯA commit** tại thời điểm ghi.

### Changed — UI Onboarding (Gemini Pro hỗ trợ)
- **Màn Login redesign**: bỏ chữ "Y WONDER GREEN FARM" + "CUỘC PHIÊU LƯU BẮT ĐẦU", thay bằng **logo Ywonder Hub** (`Y_Wonder_Hub_Logo2.png`, đã tách nền). Thêm **validate ≤ 20 ký tự** cho username/password ở cả form Đăng nhập lẫn Đăng ký (`max-length="20"` + check code).
- **Character Select redesign**: thay ký tự ♂/♀ bằng **avatar ảnh** (`Male_Avatar.jpg`, `Female_avatar2.jpg`); lưu `PlayerGender` (static) để đặt avatar mặc định; validate tên nhân vật **2–20 ký tự** (trước 2–16).
- **Đồng bộ 3 tông màu chủ đạo** (Cam `#eb6b2a` · Xanh lá `#7cb641` · Xanh biển `#2596be`) vào toàn bộ onboarding:
  - Login: nút Cam viền hover Xanh lá; ô input focus viền Xanh lá; tab active Xanh biển viền Xanh lá.
  - Character Select: nút/tiêu đề/thẻ giới tính/popup cảnh báo theo 3 tông; sửa USS (z-index, line-height).
- **`FloatingNameTag` nâng cấp**: xóa thẻ `<mark>` bị lỗi khoảng cách ký tự dài; thêm **3D Quad làm nền** căn giữa tự động; sửa công thức tính chiều rộng nền (dùng padding tĩnh thay vì ×0.8) → tên dài không tràn.
- **`GameManager`**: sửa lỗi đè màu tên nhân vật → khôi phục **màu trắng chuẩn** (thay vì vàng).
- **Splash & Loading**: gỡ trang trí dư (ngôi sao, version tag); nền Splash đổi **mystic-black**; thanh tiến trình **Xanh lá**, logo text **Cam**.
- **`LoadingScreenController`**: thêm tham số `destinationName` cho `ShowLoadingAsync` (hiện tên địa điểm đích khi chuyển scene).
- Tinh chỉnh kèm theo: `GameHUD`, `ProfilePopup`, `BuildModeOverlay`, `BuildCameraController`, `GhostPlacementController`, `MapPopupController`.

> ✅ **Chốt tên game chính thức = "Y WONDER GREEN FARM"** (18/06): tên loading mặc định Gemini đặt là đúng. Splash vẫn dùng **logo Ywonder Hub** (logo hình, không hiện chữ tên). Tài liệu kỹ thuật đã đồng bộ lại tên này (trước đó vài file ghi tạm "YWONDERLAND").

### Added — Gameplay (phiên Claude 16/06, gộp ghi ở đây)
- **`FarmTileMarker`**: ô vuông viền màu theo trạng thái đất (vàng=sẵn gieo, xanh=đang lớn, cam=chín), tự gắn vào mọi FarmTile.
- **Hammer Build (kiểu Minecraft)**: `TilePlacementSystem` + `HammerBuildController` — cầm búa gõ ô trước mặt để lát (tốn 4 đá + 4 gỗ), ô preview sáng/đỏ. *(Phím G tạm — Bước 2 sẽ thay nút HUD.)*
- **`NpcProximityInteract`**: bước vào vùng quanh NPC dịch vụ tự mở Shop/Workshop/Heo đất (không cần bấm).
- **Camera PUBG/Free Fire**: nhân vật luôn quay lưng về người chơi theo yaw camera; bỏ camera trôi; giảm độ nhạy (0.8/0.6); Free-Look giữ Alt.

### Changed Files (chính)
- UI: `LoginScreen.uxml/.uss`, `LoginScreenController.cs`, `CharacterSelect.uxml/.uss`, `CharacterSelectController.cs` [MODIFIED]
- Gameplay: `FarmTileMarker.cs`, `TilePlacementSystem.cs`, `HammerBuildController.cs`, `NpcProximityInteract.cs` [NEW]
- `ThirdPersonCamera.cs`, `PlayerController.cs`, `EquipmentManager.cs` [MODIFIED]

---
## [2026-06-16] — Rà soát điểm mù tài liệu + Bộ tài liệu kỹ thuật

### Added (tài liệu cho khách/BA)
- **`Docs_KichBan/DiemMu_CanXinKhach.md`** [NEW]: audit 3 lớp (kịch bản khách + docs nội bộ + code thật) → liệt kê toàn bộ điểm mù cần khách làm rõ, nhóm theo hệ thống, đánh dấu mức 🔴 chặn code / 🟡 chặn cân bằng / ⚪ chặn bàn giao. Kèm 4 mục GATING (backend, API web, định danh đăng nhập, publish Android) + bảng mâu thuẫn nội bộ.
- **`Docs_KichBan/TongKet_TaiLieu_CanCo.md`** [NEW]: tổng kết toàn bộ tài liệu dự án cần có, chia **Nhóm A (khách/BA cung cấp)** vs **Nhóm B (team tự viết)** vs **Nhóm C (viết lại)**; có câu nhắn mẫu gửi BA.

### Added (tài liệu kỹ thuật — team tự viết)
- **`docs/TECHNICAL_DESIGN.md`** [NEW] (TDD): kiến trúc backend REST offline-first, stack hiện tại vs cần chốt, luồng đăng nhập/tutorial đợt 1, lộ trình đợt 2–4, 8 rủi ro kỹ thuật đã biết.
- **`docs/DB_SCHEMA.md`** [NEW] (ERD): lược đồ DB thật theo REST — bảng `users`/`profiles` (đã có) + đề xuất economy/inventory/transactions/farm/animal/piggy_bank/quests + danh mục tĩnh (item/crop/animal/shop catalog).
- **`docs/SECURITY.md`** [NEW]: threat model, nguyên tắc **server-authoritative**, chống chỉnh giờ/double-spend/IAP giả, checklist anti-cheat đợt 2–3.
- **`docs/BUILD_RELEASE.md`** [NEW]: runbook build Android (keystore → AAB → Play Console), checklist phát hành, versioning.

### Changed (dọn mâu thuẫn UGS/Unity 2022 — Nhóm C)
- `docs/ARCHITECTURE.md`: **viết lại theo REST** — bỏ bảng "UGS Services" + sơ đồ "UGS Cloud", thay bằng Backend Services thật, cấu trúc thư mục `_Project/` đúng thực tế, trỏ sang TDD/DB_SCHEMA/SECURITY/BUILD.
- `docs/CONTEXT_RECOVERY.md`: prompt khởi động sửa **Unity 2022 + UGS + UniTask → Unity 6 + REST + Awaitable**; cập nhật danh sách file đọc.
- `docs/DATA_SCHEMA.md`: gắn banner **LỖI THỜI**, trỏ sang `DB_SCHEMA.md` (tránh implement nhầm theo UGS).

### Notes
- Phát hiện cần xử lý ở đợt 2–3 (ghi trong TDD/SECURITY): POS đang `int` sẽ tràn → đổi `long`; PiggyBank tách rời EconomyManager (gửi/rút chưa trừ tiền thật); lãi Heo Đất phải tính giờ server (chống chỉnh giờ máy).

### Changed Files
- `Assets/_Project/Docs_KichBan/DiemMu_CanXinKhach.md`, `TongKet_TaiLieu_CanCo.md` [NEW]
- `docs/TECHNICAL_DESIGN.md`, `docs/DB_SCHEMA.md`, `docs/SECURITY.md`, `docs/BUILD_RELEASE.md` [NEW]
- `docs/ARCHITECTURE.md`, `docs/CONTEXT_RECOVERY.md`, `docs/DATA_SCHEMA.md` [MODIFIED]

---
## [2026-06-16] — Lưu trữ THẬT (REST API) — Đợt 1: Profile + Tutorial

### Added
- **Backend REST đợt 1** (theo kịch bản khách, KHÔNG dùng UGS): chuyển từ mock/PlayerPrefs sang lưu thật cho `player_profile` + cờ `tutorialCompleted`.
- **Server stub** `server/` (Node/Express, lưu `data.json`): `/auth/register`, `/auth/login`, `GET|PUT /player/profile`. Token JWT đơn giản (chỉ dev/test, không production). Đã smoke-test end-to-end OK.
- **Client Unity** `Assets/_Project/Scripts/Backend/`: `BackendConfig` (ScriptableObject URL/timeout), `ApiClient` (UnityWebRequest + Newtonsoft, try/catch + timeout), `AuthService` (login/register, cache token), `PlayerProfileService` (load/save profile, **offline-first** fallback cache PlayerPrefs).

### Changed
- `SystemsBootstrapper`: khởi tạo thêm `AuthService` + `PlayerProfileService`.
- `GameManager.StartGame()` *(PROTECTED)*: đăng nhập + nạp profile chạy nền song song cutscene (không chặn UX, offline tự fallback).
- `TutorialManager` *(PROTECTED)*: bỏ qua tutorial nếu hồ sơ đã `tutorialCompleted`; khi hoàn thành thì ghi cờ lên hồ sơ thật.
- `docs/ARCHITECTURE.md` + `docs/API_CONTRACTS.md`: đính chính backend UGS → **REST API riêng**.

### Notes
- Auth đợt 1 dùng username = tên nhân vật + mật khẩu sinh/lưu local (CHƯA nối UI Login — để đợt 2).
- KHÔNG cài package Unity mới (dùng UnityWebRequest + Newtonsoft sẵn có).

### Changed Files
- `server/*` [NEW] · `Assets/_Project/Scripts/Backend/*.cs` [NEW]
- `Assets/_Project/Scripts/Core/SystemsBootstrapper.cs` [MODIFIED]
- `Assets/_Project/Scripts/Managers/GameManager.cs` [MODIFIED]
- `Assets/_Project/Scripts/Tutorial/TutorialManager.cs` [MODIFIED]
- `docs/ARCHITECTURE.md`, `docs/API_CONTRACTS.md` [MODIFIED]

---
## [2026-06-15] — Tưới cây (cầm xô), tự gom lá vào cây, tắt rung, dọn Splash

### Added
- **Hoạt ảnh tưới cây riêng**: động tác tưới gọi clip `Watering` riêng (tự đo độ dài clip), nhân vật cầm **bình tưới/xô** qua `EquipmentManager` (đúng pattern các nông cụ khác). Placeholder bình tưới được dựng lại có thân + quai xách + vòi + bông sen cho ra dáng.
- **Tự gắn lá rời vào cây lúc runtime** (`HarvestableResource`): cây tự tìm các object lá ở gần theo tên (`leafNameContains`) + bán kính (`leafAttachRadius`) rồi `SetParent` vào thân — khỏi phải parent tay từng cây. Lá tự ẩn + tự đổ theo cây khi bị chặt. Dùng cache tĩnh để chỉ quét scene 1 lần.

### Changed
- **Tắt hẳn rung lắc cây/đá** khi chặt/đập (trước giảm còn 10%, nay bỏ luôn) — cây/đá đứng yên, chỉ chạy thanh tiến trình.
- **Ẩn toàn bộ phần con (thân + lá)** khi cây bị chặt qua `SetVisualsActive`, thay cho việc chỉ ẩn con đầu tiên (tránh lá lơ lửng còn sót).
- **Màn Splash**: nền đổi sang **đen thuần** (hòa với nền logo JPG), **xóa tiêu đề "YWONDER GREEN FARM"** và **dòng kẻ vàng** trang trí; giữ logo YWonderHub + dòng "CUỘC PHIÊU LƯU BẮT ĐẦU" + thanh tải.

### Changed Files
- `Assets/_Project/Scripts/Environment/FarmInteractionController.cs` [MODIFIED]
- `Assets/_Project/Scripts/Environment/HarvestableResource.cs` [MODIFIED]
- `Assets/_Project/Scripts/Player/EquipmentManager.cs` [MODIFIED]
- `Assets/_Project/UI/SplashLoadingScreen.uxml` [MODIFIED]
- `Assets/_Project/UI/Styles/SplashLoadingScreen.uss` [MODIFIED]

---
## [2026-06-07] — Phase 5 & 6: Khai Thác Tài Nguyên và Câu Cá

### Added
- **Phase 5 (Khai Thác Tài Nguyên)**:
  - Hệ thống `HarvestableResource.cs` xử lý tương tác nhấn giữ (hold 3s) để chặt cây, đập đá. Rơi ra `wood_01` và `stone_01`.
  - `ResourceSpawner.cs` sinh ngẫu nhiên tài nguyên trên bản đồ, theo dõi đếm ngược thời gian hồi sinh (Respawn Timer) và lưu qua `PlayerPrefs`.
  - Giao diện thanh tiến trình lơ lửng (`ResourceInteractionUI.uxml` và Controller) theo dõi tiến độ nhấn giữ.
- **Phase 6 (Câu Cá - Đấu nối lõi)**:
  - Tích hợp `FishingOverlayController.cs` với `InventoryManager`. Đọc số lượng `bait_01` thật từ túi đồ.
  - Vượt qua QTE thành công, cá (`fish_01`, `fish_02`, `gift_box_01`) tự động thêm vào túi đồ.
  - Hệ thống 10 lượt câu miễn phí mỗi ngày, lưu và reset bằng `PlayerPrefs` theo ngày thực.
- **Item Database**: Thêm `pickaxe_01`, `fish_01`, `fish_02`, `gift_box_01` vào `ItemDataGenerator.cs`.

### Fixed
- **Obsolete API Cleanup (bởi Unity Assistant)**: Thay `enableWordWrapping` bằng `textWrappingMode = TextWrappingModes.NoWrap` trong `FloatingNameTag.cs` và `FishingSpot.cs`.
- **Code Cleanup**: Dọn dẹp biến thừa `premiumBait` trong `FishingOverlayController.cs`.

### Changed Files
- `Assets/Scripts/Environment/HarvestableResource.cs` [NEW]
- `Assets/Scripts/Managers/ResourceSpawner.cs` [NEW]
- `Assets/UI/ResourceInteractionUI.uxml` [NEW]
- `Assets/UI/ResourceInteractionUIController.cs` [NEW]
- `Assets/Scripts/Environment/FarmInteractionController.cs` [MODIFIED]
- `Assets/Scripts/Editor/ItemDataGenerator.cs` [MODIFIED]
- `Assets/UI/FishingOverlayController.cs` [MODIFIED]
- `Assets/Scripts/UI/FloatingNameTag.cs` [MODIFIED]
- `Assets/Scripts/Environment/FishingSpot.cs` [MODIFIED]

---
## [2026-06-06] — Part B (Fix Phase): Fishing 3D & Build Mode Redesign

### Added
- **Fishing Spot 3D Interaction** (`FishingSpot.cs`): Chuyển từ bấm nút UI sang trigger 3D. Lại gần hiện TextMeshPro nổi "Nhấp F để Câu", bấm F mới mở UI câu cá. Tránh đụng phím E của sự kiện.
- **Contextual Build Mode UX** (`BuildModeOverlayController.cs`):
  - Ghim (Pin) vị trí nhà trên map khi click trái thay vì xây ngay, giải phóng chuột.
  - Các nút Xây/Xoay/Hủy nổi cạnh ngôi nhà 3D.
  - Context menu Xoay/Nhấc/Xóa nổi cạnh nhà khi click vào nhà đã xây.
- **URP Grid Renderer** (`BuildGridRenderer.cs`): Dùng `RenderPipelineManager.endCameraRendering` để vẽ lưới bằng lệnh `Graphics.DrawMeshNow` thay cho `GL.Lines` (không chạy trên URP).

### Fixed
- **Build Mode Bugs**:
  - Khối Ghost không trong suốt: Sửa shader URP.
  - Lỗi click UI xuyên xuống game: Tự động ẩn `GameHUD` khi bật chế độ xây dựng.
  - Ghost bị dính chuột: Đổi logic sang Pin position.

### Changed Files
- `Assets/UI/BuildModeOverlayController.cs`
- `Assets/UI/BuildModeOverlay.uxml`
- `Assets/UI/Styles/BuildModeOverlay.uss`
- `Assets/Scripts/Environment/GhostPlacementController.cs`
- `Assets/Scripts/Environment/BuildGridRenderer.cs` [NEW]
- `Assets/Scripts/Environment/BuildGridManager.cs`
- `Assets/Scripts/Environment/FishingSpot.cs` [NEW]

---
## [2026-06-06] — Part A Hoàn thành: Onboarding Flow + Tutorial UX + Name Tags

### Added
- **Character Select UI Toolkit** (`CharacterSelect.uxml`, `CharacterSelectController.cs`):
  - Chọn giới tính (♂/♀ cards), đặt tên (2-16 ký tự, validate), popup cảnh báo xác nhận.
  - Vietnamese text set từ C# code (không gõ trực tiếp UXML).
  - GameManager tự Show/Hide theo state.
- **Tutorial UX cải thiện cho đối tượng nhỏ tuổi** (`TutorialManager.cs`):
  - Instruction Banner lớn (nền xanh) hiện mỗi bước quan trọng.
  - Countdown Timer to (48px) giữa màn hình khi chờ cây lớn, đổi màu xanh→vàng→đỏ.
  - Dấu chấm than (!) vàng nhấp nhô+xoay trên đầu NPC.
  - NPC chào ngay khi tutorial bắt đầu.
- **Floating Name Tags** (`FloatingNameTag.cs`):
  - TextMeshPro 3D + billboard rotation — chữ phẳng sắc nét kiểu Minecraft.
  - Outline đen dày, màu theo Design System (Player=Gold #FFC107, NPC=Hero #5B42F3).
  - Anti-frustum culling: overflow mode, disable occlusion, force mesh update.
  - Fade opacity khi xa, ẩn khi >30m.
  - Auto-attach cho Player (GameManager) và NPC (GuideNPC).

### Fixed
- **Legacy Input bug** (`TutorialManager.cs`): `Input.GetMouseButtonDown(0)` → `Mouse.current.leftButton.wasPressedThisFrame`.
- **URP Shader tím** (8 files): `Shader.Find("Standard")` → `Shader.Find("Universal Render Pipeline/Lit")` trong FarmTile, GhostPlacement, GuideNPC, TutorialManager.
- **CharacterSelect không ẩn sau xác nhận**: Thêm Hide() + GameManager quản lý visibility.

### Changed Files
- `Assets/UI/CharacterSelect.uxml` [NEW]
- `Assets/UI/CharacterSelectController.cs` [NEW]
- `Assets/Scripts/UI/FloatingNameTag.cs` [NEW]
- `Assets/Scripts/Tutorial/TutorialManager.cs`
- `Assets/Scripts/Tutorial/GuideNPC.cs`
- `Assets/Scripts/Managers/GameManager.cs`
- `Assets/Scripts/Environment/FarmTile.cs`
- `Assets/Scripts/Environment/GhostPlacementController.cs`

---
## [2026-06-05] — Module Build Mode / Chế độ Xây dựng (WIP)

### Added
- **Build Mode Overlay UI** — Giao diện xây dựng/trang trí nông trại:
  - **Control Bar** (cạnh trên): Pill số dư POS, tiêu đề "CHẾ ĐỘ XÂY DỰNG", 5 nút công cụ (Hoàn tác, Di chuyển, Xoay, Xóa, Lưu) + nút thoát (✕ đỏ).
  - **Category Sidebar** (cạnh trái): 5 tab dọc — Nhà cửa, Nông trại, Hàng rào, Trang trí, Đường đi. Tab active nền vàng `#FFC107`.
  - **Item Bar** (cạnh dưới): ScrollView ngang chứa card vật phẩm (80×96px) với icon, tên, giá POS. Card được chọn viền vàng 3px.
  - **Detail Tooltip** (nổi phía trên item): Panel kem `#F5F0E8` với retro shadow, hiển thị icon + tên + kích thước + giá + mô tả + nút "ĐẶT XUỐNG".
  - **Status Label**: Nhãn trung tâm mờ dần (fade-out 2s) thông báo kết quả thao tác.
  - **Màu chủ đạo**: Nâu gỗ `#8B5E3C` cho nút Xoay và sidebar button trên HUD.
  - **Mock Data**: 5 danh mục × ~5 vật phẩm = ~25 item mẫu với emoji, giá, kích thước.
- **Hệ thống 3D Placement**:
  - **BuildGridManager**: Lưới 50×50 ô (1 unit/ô), world↔grid conversion, occupancy validation (CanPlace/OccupyCells/FreeCells), Gizmos debug, **follow target** (grid bám theo nhân vật liên tục mỗi frame).
  - **BuildGridRenderer**: Vẽ lưới ô vuông runtime bằng GL.Lines trong Game View (không chỉ Scene View). Viền nâu gỗ. Bật/tắt theo Build Mode.
  - **GhostPlacementController**: Cube bán trong suốt theo chuột qua Raycast → snap grid → xanh lá (hợp lệ) / đỏ (trùng hoặc ngoài grid). Click trái đặt, click phải hủy. Hỗ trợ xoay + multi-cell (2x2, 3x3...).
  - **BuildCameraController**: Camera Top-Down 75°, smooth transition từ/về ThirdPersonCamera, WASD pan, scroll zoom. Unlock cursor cho Build Mode.
- **HUD Integration**:
  - Nút 🔨 (`BtnBuild`) trên sidebar trái, nền nâu gỗ `#8B5E3C`.
  - Phím **B** toggle Build Mode.

### ⚠️ WIP — Chưa hoàn thiện
- Ghost dùng Primitive Cube placeholder — chờ model 3D thật.
- Chưa test kỹ ghost xanh/đỏ, cho xây/hủy/di chuyển.
- Chưa có logic trừ POS qua ghost system (chỉ có mockup UI).
- Chưa có lưu/load bố cục nông trại.

### Files changed
- Assets/UI/BuildModeOverlay.uxml (NEW)
- Assets/UI/Styles/BuildModeOverlay.uss (NEW)
- Assets/UI/BuildModeOverlayController.cs (NEW)
- Assets/Editor/SetupBuildModeUI.cs (NEW)
- Assets/Scripts/Environment/BuildGridManager.cs (NEW)
- Assets/Scripts/Environment/BuildGridRenderer.cs (NEW)
- Assets/Scripts/Environment/GhostPlacementController.cs (NEW)
- Assets/Scripts/Camera/BuildCameraController.cs (NEW)
- Assets/UI/GameHUD.uxml (MODIFIED — thêm BtnBuild)
- Assets/UI/Styles/GameHUD.uss (MODIFIED — thêm .sidebar-btn-build)
- Assets/UI/GameHUDController.cs (MODIFIED — thêm reference + callback + phím B)
- docs/MEMORY.md (MODIFIED — thêm bài học #46, #47)

---
## [2026-06-05] — Module Splash/Loading Screen (Màn hình Chào/Tải game)

### Added
- **Splash Loading Screen** — Màn hình khởi động game hiển thị đầu tiên khi Play Mode:
  - **Logo thương hiệu**: Chữ "Y WONDER GREEN FARM" cỡ 48px nét đậm trắng trên nền tối `#1E1E23`, có bóng đổ retro cứng `#3D3535` lệch 4px tạo hiệu ứng khắc chữ nổi.
  - **Phụ đề**: "CUỘC PHIÊU LƯU BẮT ĐẦU" màu vàng `#FFC107`, letter-spacing 6px tạo cảm giác trang trọng.
  - **Thanh tiến trình retro**: Chiều rộng 400px, chiều cao 24px, viền dày 3px `#3D3535`, nền xám `#3A3A42`. Vệt nạp màu vàng `#FFC107` bo góc 8px, chiều rộng thay đổi mượt mà từ 0% → 100% theo eased curve (smoothstep).
  - **Nhãn trạng thái động**: Thay đổi theo 5 mốc phần trăm — "Đang tải cấu hình nông trại..." → "Đang kết nối đến máy chủ Cloud..." → "Đang đồng bộ dữ liệu thế giới 3D..." → "Đang chuẩn bị giao diện..." → "Tải hoàn tất!".
  - **Nhãn phần trăm**: Hiển thị `0%` → `100%` đồng bộ với thanh tiến trình.
  - **Trang trí**: 4 ngôi sao Unicode ✦✧ ở 4 góc màn hình tạo bầu không khí, gạch phân cách vàng mờ dưới phụ đề, nhãn phiên bản `v0.1.0-alpha` góc dưới phải.
- **Tính năng mở rộng**:
  - **Sort Order = 10**: UIDocument đặt Sort Order cao hơn Login Screen (mặc định 0) để tự động đè lên mà không cần thay đổi GameManager.
  - **Click to Skip**: Nhấp chuột vào bất kỳ đâu trên màn hình splash trong lúc tải sẽ nhảy nhanh đến 100% và chuyển cảnh.
  - **Fade-out Transition**: Khi tải hoàn tất, toàn bộ màn hình splash mờ dần (opacity 1 → 0) trong 0.5 giây, rồi GameObject tự động deactivate để lộ Login Screen bên dưới.
  - **Phím nóng P**: Bấm phím P (New Input System) bất kỳ lúc nào để bật lại Splash Screen và chạy lại mô phỏng từ 0% — tiện cho nhà phát triển kiểm thử và quay video demo.
  - **Simulated Loading**: Coroutine giả lập tiến trình từ 2 đến 3 giây ngẫu nhiên với đường cong smoothstep tạo cảm giác tải tự nhiên.

### Files changed
- Assets/UI/SplashLoadingScreen.uxml (NEW)
- Assets/UI/Styles/SplashLoadingScreen.uss (NEW)
- Assets/UI/SplashLoadingController.cs (NEW)
- Assets/Editor/SetupSplashUI.cs (NEW)

---
## [2026-06-04] — Module Fishing UI (Mini-game Câu cá)

### Added
- **Fishing Overlay** — Giao diện mini-game câu cá tương tác đầy đủ với các trạng thái:
  - **Chuẩn bị (Ready Panel)**: Chọn mồi câu (Không mồi / Mồi thường / Mồi xịn) và nút "QUĂNG CẦN" (🎣).
  - **Chờ đợi (Waiting Panel)**: Biểu tượng phao câu nhấp nhô theo nhịp sóng (sine wave animation trong Update) và nút "Thu cần" để hủy câu sớm.
  - **Giật cần (QTE Panel)**: Xuất hiện khi cá cắn câu sau 3-6 giây ngẫu nhiên.
    - Thanh đo lực chứa **Vùng Xanh (Safe Zone)** thay đổi độ rộng theo loại mồi.
    - Kim đỏ dao động liên tục qua lại bên trong thanh đo.
    - Thanh thời gian cạn dần biểu thị giới hạn **1.5 giây QTE**.
    - Nút "GIẬT CẦN!" và phím nóng `Space` để câu.
  - **Kết quả (Result Panel)**: Bảng thông báo thành công/hụt dạng modal bo góc viền đen.
    - Hiển thị tên cá, biểu tượng emoji, độ hiếm (Thường/Hiếm/Sử Thi/Sự Kiện), và mô tả phần thưởng.
    - Liên kết thưởng POS trực tiếp khi câu thành công. Có 5% cơ hội câu được Bao Lì Xì Event 🎁.
- **Tính năng mở rộng**:
  - **Bait Mechanics**: Sử dụng mồi thường tăng tỉ lệ cá hiếm, mồi xịn tăng tỉ lệ cá sử thi và mở rộng vùng xanh QTE lên 40%, giảm tốc độ kim.
  - **Bait Shop Fallback**: Tích hợp ConfirmDialog mời mua thêm 5 mồi thường giá 50 POS khi hết lượt câu miễn phí.
  - **Cheat Panel**: Thanh hỗ trợ nhà phát triển ở góc dưới bên trái gồm các nút hồi 10 lượt, mua 10 mồi và công tắc "Auto-Win QTE" giúp kiểm thử chính xác nhanh chóng.
  - **HUD Integration**: Nút 🎣 ở sidebar và phím nóng `F` trên bàn phím để mở/đóng chế độ câu cá.

### Files changed
- Assets/UI/FishingOverlay.uxml (NEW)
- Assets/UI/Styles/FishingOverlay.uss (NEW)
- Assets/UI/FishingOverlayController.cs (NEW)
- Assets/Editor/SetupFishingUI.cs (NEW)
- Assets/UI/GameHUD.uxml (MODIFIED — thêm nút 🎣)
- Assets/UI/Styles/GameHUD.uss (MODIFIED — thêm css nút 🎣)
- Assets/UI/GameHUDController.cs (MODIFIED — thêm tích hợp nút & phím F)

---
## [2026-06-04] — Module Chat UI (Khung chat thu/mở)

### Added
- **Chat Panel** — Hệ thống chat kênh thế giới đặt tại cạnh dưới giữa màn hình với 2 trạng thái:
  - **Trạng thái thu gọn (Collapsed)**: Thanh pill mờ đen đồng bộ HUD hiển thị tin nhắn mới nhất, có nút emoji nhanh và nút mở rộng (▲).
  - **Trạng thái mở rộng (Expanded)**: Khung chat 420x260px nền tối bán trong suốt (Dark Translucent — `rgba(30, 30, 35, 0.88)`) không che khuất thế giới 3D, viền tối 3px chuẩn design system.
    - **Header**: Thanh tiêu đề "KÊNH THẾ GIỚI" nền đen mờ kèm nút thu nhỏ (▼).
    - **History scroll**: Cuộn lịch sử tin nhắn nền tối mờ, chữ trắng/sáng màu dễ đọc, tự động cuộn xuống đáy khi có tin nhắn mới (On GeometryChangedEvent).
    - **Footer**: Input nhập tin nhắn màu trắng nổi bật, nút gửi ("Gửi") màu xanh blue retro, nút emoji nhanh (☺) màu kem sáng.
  - **Nút bấm Tactile đồng bộ**: Cả nút mở rộng (▲), thu nhỏ (▼) và emoji nhanh (☺) đều được thiết kế dạng phím cơ bo góc tròn, màu tím thương hiệu (`#5B42F3`), viền dày 2px `#3D3535`, có phản hồi vật lý lún 1px khi click.
- **Tính năng mở rộng**:
  - **Profanity Filter**: Tự động lọc các từ tục tĩu tiếng Việt/Anh ("ngu", "fuck", "đm", "vl"...) thành dấu `***`.
  - **Rate Limit**: Giới hạn tần suất chat (tối đa 5 tin nhắn trong 30 giây). Nếu vượt quá, hiển thị cảnh báo đỏ từ hệ thống.
  - **Mock AI Chatbot**: Tự động trả lời theo từ khóa tin nhắn ("hello", "nông trại", "shop", "bản đồ", "heo đất"...) sau 2 giây delay để mô phỏng tính năng AI NPC.
  - **Enter Hotkey**: Bấm phím `Enter` để mở rộng chat và tự động focus vào input field; bấm tiếp để gửi tin; bấm khi rỗng sẽ tắt focus/thu nhỏ.
  - **Settings Integration**: Thêm toggle "Hiện chat" vào Cài đặt (SettingsPopup) để bật/tắt hiển thị toàn bộ khung chat.

### Files changed
- Assets/UI/ChatPanel.uxml (NEW)
- Assets/UI/Styles/ChatPanel.uss (NEW)
- Assets/UI/ChatPanelController.cs (NEW)
- Assets/Editor/SetupChatUI.cs (NEW)
- Assets/UI/SettingsPopup.uxml (MODIFIED — thêm toggle Hiện chat)
- Assets/UI/SettingsPopupController.cs (MODIFIED)
- Assets/UI/GameHUD.uxml (MODIFIED — xóa MessagesBar cũ)
- Assets/UI/GameHUDController.cs (MODIFIED — xóa dọn dẹp MessagesBar)

### Fixed & Refactored
- **Xóa giao diện đè chồng trong Editor (Edit Mode)**: Loại bỏ hoàn toàn `MessagesBar` cũ trong `GameHUD.uxml` và dọn dẹp các C# bindings liên quan trong `GameHUDController.cs` để tránh đè chồng lên Chat Panel mới lúc chưa chạy game trong Unity Editor.
- **Đồng bộ hóa nút bấm**: Thay đổi style các nút tam giác, emoji phẳng không viền thành các nút đặc có khối đế màu tím viền đen dày để đúng tinh thần giao diện cơ học.
- **Lỗi biên dịch Setup script**: Sửa lỗi tham chiếu sai thuộc tính `sourceAsset` thành `visualTreeAsset` trên `UIDocument` trong C# Editor setup script.

---
## [2026-06-04] — Module Event / Exchange UI (Sự kiện mùa)

### Added
- **Event Popup** — UI sự kiện theo mùa với 2 tab:
  - **Tab Đổi quà**: Grid đổi vật phẩm event (cá, quặng, vé) lấy reward hiếm (V2 items, pet, cosmetic)
  - **Tab Gói ưu đãi**: Bundle UPOS giảm giá giới hạn thời gian, có tag "-50%"/"HOT", trạng thái "ĐÃ HẾT"
  - **Sidebar Vật phẩm**: Hiển thị số lượng 🐟 Cá event / 💎 Quặng hiếm / 🎫 Vé sự kiện
  - **Timer pill**: Đếm ngược thời gian sự kiện còn lại
  - **Header**: Festival Purple #9C27B0
  - **Close button**: Wrapper pattern chuẩn (Lessons #33 #34)
  - **Mock data**: 6 exchange items + 3 bundles

### Files changed
- Assets/UI/EventPopup.uxml (NEW)
- Assets/UI/Styles/EventPopup.uss (NEW)
- Assets/UI/EventPopupController.cs (NEW)
- Assets/UI/GameHUD.uxml (MODIFIED — thêm BtnEvent 🎁)
- Assets/UI/Styles/GameHUD.uss (MODIFIED — thêm sidebar-btn-event styles)
- Assets/UI/GameHUDController.cs (MODIFIED — thêm eventPopup reference + BtnEvent callback + E key test)

### Fixed
- **UXML comment Unicode**: Comment `<!-- ═══ ... ═══ -->` chứa ký tự Unicode `═` khiến UI Builder không mở được file. Đã đổi thành ASCII thuần.
- **Header bị co rúm khi đổi tab**: Header và tab bar thiếu `flex-shrink: 0`, bị body content ép nhỏ khi tab Đổi quà có nhiều item.
- **Bundle cards cao thấp khác nhau**: Dùng `min-height` chỉ đặt mức tối thiểu, card có description dài vẫn cao hơn. Fix: dùng `height: 280px` cố định + spacer `flex-grow: 1` đẩy nút xuống đáy.
- **Legacy Input API**: `Input.GetKeyDown` gây lỗi vì project dùng New Input System. Fix: dùng `Keyboard.current.eKey.wasPressedThisFrame`.

---
## [2026-06-04] — Module Level Up VFX/UI

### Added
- **Level Up Overlay** — Fullscreen golden VFX khi người chơi thăng cấp:
  - Background: Overlay tối + vùng glow vàng tròn ở giữa
  - **Badge** ⭐ scale animation (0.5→1)
  - **"LEVEL UP!"** text scale animation (0.6→1)
  - **Level mới** hiển thị trong pill viền vàng
  - **Mở khóa** section (xanh lá, chỉ hiện khi level có unlock): Lv.5 Câu cá, Lv.10 Mỏ đá, Lv.40 Đảo Hải Phú...
  - **Nút "TIẾP TỤC"** màu vàng gold để đóng
  - Star decorations ✦✧ trang trí xung quanh
  - Fade in/out via CSS opacity transition
- **Keyboard Test** — Bấm phím **L** trong Play Mode để test Level Up liên tục

### Files changed
- Assets/UI/LevelUpOverlay.uxml (NEW)
- Assets/UI/Styles/LevelUpOverlay.uss (NEW)
- Assets/UI/LevelUpOverlayController.cs (NEW)
- Assets/UI/GameHUDController.cs (MODIFIED — thêm levelUpOverlay reference + L key test)

---
## [2026-06-04] — Module Heo Đất UI (Piggy Bank Savings)

### Added
- **Heo Đất Popup** — Gửi tiết kiệm POS với 3 gói lãi suất:
  - **3 gói**: 12 ngày (+2%), 30 ngày (+6%), 180 ngày (+45%)
  - **Tab Gửi tiết kiệm**: Chọn gói → nhập số tiền → preview (gốc/lãi/tổng) → xác nhận
  - **Validation**: Kiểm tra số dư, chỉ cho phép 1 gói active, không rút sớm
  - **Countdown**: Đếm ngược real-time (test mode: 1 ngày = 5 giây)
  - **Đáo hạn**: Tự động cộng gốc + lãi vào balance, thêm entry lịch sử
  - **Tab Lịch sử**: Hiển thị các giao dịch đã hoàn thành + mock data
  - **Header**: Warm Gold #E8833A, balance pill góc trái (Lessons #30 #32 applied)
  - **Close button**: Wrapper pattern chuẩn (Lessons #33 #34 applied)
- **HUD Piggy Button** — Nút 🐷 trên sidebar HUD, màu #E8833A

### Files changed
- Assets/UI/PiggyBankPopup.uxml (NEW)
- Assets/UI/Styles/PiggyBankPopup.uss (NEW)
- Assets/UI/PiggyBankPopupController.cs (NEW)
- Assets/UI/GameHUD.uxml (MODIFIED — thêm BtnPiggy)
- Assets/UI/Styles/GameHUD.uss (MODIFIED — thêm sidebar-btn-piggy styles)
- Assets/UI/GameHUDController.cs (MODIFIED — thêm piggyBankPopup reference + callback)

### Fixed
- **Package card tràn nội dung**: Layout dọc (icon→tên→rate→label) xếp 4 tầng quá cao, rate bị tràn ra ngoài viền card. Sửa bằng cách chuyển sang layout **ngang** (icon ← tên → rate), ẩn label dư thừa.
- **Preview rows đè chồng**: Các dòng Gốc/Lãi/Nhận về bị overlap do thiếu `min-height`, `align-items`, `flex-shrink`. Thêm `min-height: 18px` + `flex-shrink: 0` cho label/value.

---
## [2026-06-04] — Module Map UI (Visual World Map)

### Added
- **Map Popup** — Bản đồ thế giới dạng visual map (biển + đảo), không phải danh sách:
  - Nền đại dương xanh #1A8FBF với sóng trang trí `〰〰〰` + la bàn 🧭
  - **5 đảo positioned** trên bản đồ, mỗi đảo có vùng đất riêng (hình/màu khác nhau):
    - 🏡 Nông trại (xanh lá, center-left, luôn mở)
    - 🏙️ Thành phố (xám bạc, center-right, cần tutorial)
    - ⛏️ Mỏ đá (nâu, top-center, Lv.10)
    - 🏝️ Đảo Hải Phú (vàng cát, bottom-left, Lv.40 + VIP/Vé, có 🔒 overlay)
    - 🌲 Đảo Mộc Nhi (xanh đậm, bottom-right, Lv.60 + VIP/Vé, có 🔒 overlay)
  - **Interaction**: Bấm đảo → pin viền vàng gold + floating info card hiện ở dưới → bấm "🚀 DI CHUYỂN"
  - **Info Card**: Icon, tên, status badge (ĐÃ MỞ KHÓA/ĐANG KHÓA), mô tả, yêu cầu ✅/❌, nút travel
  - **Cheat Bar**: 2 nút test — cycle level (1→5→15→45→65), cycle VIP/Vé
  - **Top bar**: Semi-transparent dark, level pill góc trái, tiêu đề "BẢN ĐỒ THẾ GIỚI"
- **HUD Map Button** — Nút tạm "🗺️ Map" trên sidebar HUD, màu #00B4D8

### Fixed
- **Close button bị cắt (clip)**: Nút X nằm bên trong `map-container` có `overflow: hidden` → bị lẹm góc. Sửa bằng cách thêm `map-wrapper` bọc ngoài (không có overflow), đặt close button ở wrapper level.
- **Close button khó thấy**: Ban đầu nút X nằm trong top bar tối màu → lẫn vào nền. Chuyển ra góc phải trên, nhô ra ngoài viền (pattern chuẩn `right: -8px; top: -8px`).

### Files changed
- Assets/UI/MapPopup.uxml (NEW — restructured: map-wrapper → map-container + close)
- Assets/UI/Styles/MapPopup.uss (NEW — ocean bg, islands, info card, wrapper)
- Assets/UI/MapPopupController.cs (NEW — visual map, dictionary data, island clicks)
- Assets/UI/GameHUD.uxml (MODIFIED — thêm BtnMap)
- Assets/UI/Styles/GameHUD.uss (MODIFIED — thêm sidebar-btn-map styles)
- Assets/UI/GameHUDController.cs (MODIFIED — thêm mapPopup reference + callback)

---
## [2026-06-04] — Shop UI Polish & Sell Mode Testing

### Fixed
- **Số dư dính header**: Pill số dư (`🪙 5,000 POS`) bị nằm giữa header đè lên tiêu đề → sửa bằng `position: absolute; left: 12px` để ghim góc trái.
- **Tiêu đề tràn viền**: Chữ "HAI LÚA — VẬT TƯ NÔNG TRẠI" quá dài, sắp chìa ra ngoài → giảm font `20px → 18px`, thêm `padding: 0 120px` + `text-overflow: ellipsis`.
- **Bottom bar thừa thông tin**: Dòng "Chế độ: Mua" và "Số dư" ở cạnh dưới bị lệch nhau giữa các tab → xóa hoàn toàn bottom info bar vì tab Mua/Bán trên sidebar đã thể hiện rõ chế độ, số dư chuyển lên header.

### Added
- **Sell Mode mock data**: Bật tab Bán (`hasSellTab = true`) với 8 item nông sản có thể bán (Cà rốt 15 POS, Rau cải 20 POS, Dưa hấu 50 POS, Trứng gà 25 POS, Sữa bò 40 POS, Gỗ 8 POS, Đá 12 POS...) để test chuyển đổi Mua/Bán.

### Files changed
- Assets/UI/ShopPopup.uxml (MODIFIED — xóa bottom bar, thêm balance pill vào header)
- Assets/UI/Styles/ShopPopup.uss (MODIFIED — thêm balance pill styles, xóa info-bar styles, sửa header title)
- Assets/UI/ShopPopupController.cs (MODIFIED — xóa lblMode, cập nhật UpdateBalance format, thêm sell mock data)

---
## [2026-06-04] — HUD Shop Test Button Integration

### Added
- **HUD Shop Button** — Tích hợp nút tạm "🛒 Shop" trên HUD để test nhanh Shop Popup:
  - Màu nền xanh lá #4CAF50 đồng bộ với header shop, viền 3px #3D3535.
  - Sử dụng bố cục ngang (flex-direction: row) gồm emoji 🛒 và nhãn chữ "Shop".
  - Hiệu ứng cơ học đầy đủ: hover phóng to/đổi màu nhẹ, active lún xuống 3px.
  - Tích hợp callback click mở ShopPopup với mock data mặc định ("Hai Lúa").
  - Cơ chế dự phòng (fallback) tự động tìm kiếm `ShopPopupController` và các popup controller khác trong `OnEnable()` nếu chưa kéo thả trong Inspector.

### Files changed
- Assets/UI/GameHUD.uxml (MODIFIED)
- Assets/UI/Styles/GameHUD.uss (MODIFIED)
- Assets/UI/GameHUDController.cs (MODIFIED)

---
## [2026-06-04] — Module Shop UI (Template chung 12 shop)

### Added
- **Shop Popup** — Template UI dùng chung cho tất cả 12 cửa hàng trong Thành phố:
  - Layout landscape 2 cột giống Inventory: Sidebar + Grid + Detail Panel
  - **Sidebar trái**: 2 tab chế độ (🛒 Mua / 💰 Bán) + 5 filter danh mục (Tất cả / Hạt giống / Vật nuôi / Dụng cụ / Vật phẩm)
  - **Grid giữa**: Item cards (icon + tên + giá POS) với hover/active/selected states
  - **Detail phải**: Icon lớn + tên + giá + mô tả + bộ chọn số lượng (−/+) + tổng tiền + nút MUA/BÁN
  - **Tab Bán**: tự ẩn nếu shop không hỗ trợ bán (cấu hình qua `ShopData.hasSellTab`)
  - Reusable API: `Show(ShopData data)` — mỗi NPC shop truyền data riêng
  - Mock data mặc định: "Hai Lúa — Vật tư nông trại" (10 items, giá theo kịch bản)
  - Header xanh lá #4CAF50, border #388E3C, style khớp popup cũ (22px radius, 3px border)
  - Nút Mua màu xanh lá, nút Bán màu cam #E8833A
  - Footer hiển thị số dư POS + chế độ hiện tại

### Files changed
- Assets/UI/ShopPopup.uxml (NEW)
- Assets/UI/Styles/ShopPopup.uss (NEW)
- Assets/UI/ShopPopupController.cs (NEW)

---

## [2026-06-04] — Forgot Password Popup + UI Consistency Fix

### Added
- **Forgot Password Popup** — Popup riêng cho luồng quên mật khẩu:
  - 1 input Email (có icon ✉, focus highlight border xanh)
  - Nút "Gửi mã xác nhận" luôn bấm được, hiện lỗi nếu email sai
  - Validate email real-time (regex), status thành công/lỗi
  - Header xanh #2D7BFF, overlay click-to-dismiss, nút X đỏ
  - Mockup flow: validate → hiện thông báo gửi mã thành công
- Tích hợp vào **LoginScreenController**: nút "Quên mật khẩu?" gọi `ForgotPasswordPopupController.Show()`

### Fixed
- **UI Consistency** — Sửa toàn bộ ConfirmDialog.uss và RewardPopup.uss cho khớp phong cách popup cũ:
  - Panel: `border-radius: 22px`, `border-width: 3px`, `border-color: #3D3535`
  - Shadow wrapper: `transparent` + `padding 6px` (không tô màu)
  - Close button: `border-radius: 10px`, `3px #3D3535`, `:active → #CC3333`
  - Nút action: `3px border`, `translate: 1px 1px`, `transition: 0.08s`
  - Bỏ shadow wrapper phía sau các nút bấm (nút phẳng)

### Files changed
- Assets/UI/ForgotPasswordPopup.uxml (NEW)
- Assets/UI/Styles/ForgotPasswordPopup.uss (NEW)
- Assets/UI/ForgotPasswordPopupController.cs (NEW)
- Assets/UI/LoginScreenController.cs (MODIFIED — thêm SerializeField + gọi Show)
- Assets/UI/Styles/ConfirmDialog.uss (MODIFIED — khớp popup cũ)
- Assets/UI/Styles/RewardPopup.uss (MODIFIED — khớp popup cũ)
- docs/MEMORY.md (MODIFIED — thêm bài học #29 UI Consistency)

---

## [2026-06-04] — Module Confirm Dialog & Reward Popup
### Added
- **Confirm Dialog** — Component reusable dạng modal nhỏ trung tâm cho toàn game:
  - 3 loại dialog: Warning (⚠ vàng #FFC107), Danger (✕ đỏ #FF4B4B), Info (i xanh #2D7BFF)
  - API: `Show(title, message, confirmText, cancelText, onConfirm, dialogType)`
  - 2 nút: Hủy bỏ (xám) + Xác nhận (màu theo type)
  - Icon Unicode trong vòng tròn màu theo type
  - Overlay click-to-dismiss, nút X đỏ góc trên phải
- **Reward Popup** — Component reusable hiển thị phần thưởng:
  - API: `Show(title, rewards, buttonText, onClaim)`
  - Lưới reward items tự động tạo từ `List<RewardItemData>`
  - Mỗi item: icon + tên + số lượng trong khung trắng bo góc 16px
  - Header vàng #FFC107, nút "Nhận thưởng" mechanical press
  - Empty state "Không có phần thưởng" khi danh sách rỗng
- Cả 2 component tuân thủ đầy đủ The Tangible Playground:
  - Retro shadow 6px offset, 0px blur
  - Spacing bội 4/8px, border 2-3px #3D3535
  - Đủ trạng thái :hover, :active, :disabled
  - Không glassmorphism, không gradient, không icon thừa
  - Callbacks đăng ký 1 lần, unregister khi disable
### Files changed
- Assets/UI/ConfirmDialog.uxml (NEW)
- Assets/UI/Styles/ConfirmDialog.uss (NEW)
- Assets/UI/ConfirmDialogController.cs (NEW)
- Assets/UI/RewardPopup.uxml (NEW)
- Assets/UI/Styles/RewardPopup.uss (NEW)
- Assets/UI/RewardPopupController.cs (NEW)

---

## [2026-06-03] — Onboarding Cinematic Skip & Tutorial Fallback
### Added
- Tích hợp Cinematic UI cho màn hình thuyền cập bến (`BoatCutscene.cs`): Bao gồm nút **Bỏ qua (Skip)** xuất hiện sau 3 giây và hội thoại dẫn truyện chạy dọc ở đáy màn hình. Bấm "Bỏ qua" sẽ dịch chuyển tức thời thuyền và camera đến vị trí kết thúc.
- Triển khai cơ chế **Tự động Tìm kiếm & Khởi tạo (Bulletproof Fallbacks)** trong `TutorialManager.cs`: Nếu mảnh đất `FarmTile` hoặc `GuideNPC` bị thiếu trong Scene, script sẽ tự động sinh các GameObject placeholder tương thích kèm BoxCollider và các thành phần logic để tutorial chạy trơn tru không lỗi.
- Triển khai **mô phỏng visual bằng hình 3D hình học (Primitives Mockup)** cho `FarmTile.cs`: Tự động vẽ Soil (Khối nâu), Plowed (Khối nâu đậm), Seed (Sprout nhỏ), Watered (Sprout vừa), Ripe (Củ cà rốt cam) khi thiếu tài nguyên 3D art từ Artist.
- Triển khai **model Capsule tạm thời** cho `GuideNPC.cs`: Vẽ Capsule màu tím cao 2m cùng mũi kim chỉ hướng màu vàng để người chơi định vị NPC. Tự động sinh 3 Waypoints dẫn đường đến mảnh đất nếu waypoints bị trống.
### Changed
- Quản lý đồng bộ hiển thị HUD (`GameManager.cs`): Tự động ẩn HUD trong các trạng thái Login, Menu, Cutscene và hiển thị lại khi vào Gameplay.
### Files changed
- Assets/Scripts/Cutscenes/BoatCutscene.cs (MODIFIED)
- Assets/Scripts/Managers/GameManager.cs (MODIFIED)
- Assets/Scripts/Tutorial/TutorialManager.cs (MODIFIED)
- Assets/Scripts/Tutorial/GuideNPC.cs (MODIFIED)
- Assets/Scripts/Environment/FarmTile.cs (MODIFIED)

## [2026-06-03] — UI/UX Layout Polish (Friends, Quest, Attendance, Settings Popups)
### Fixed
- Popup Cài đặt (Settings): Polish và hoàn thiện layout ngày 03/06.
- Popup Bạn bè (Friends): Thêm khoảng đệm an toàn `margin-right: 16px` cho cụm tìm kiếm và thu nhỏ kích thước của TextField nhập tên cùng các nút bấm để tránh đè lấn lên nút đóng X ở góc trên bên phải.
- Popup Nhiệm vụ (Quest) & Điểm danh (Attendance): Khắc phục triệt để lỗi ô vật phẩm phần thưởng bị chòi ra ngoài viền khung chứa bằng cách thiết lập `flex-shrink: 0` cho các container/grid phần thưởng và các slot con cố định, giữ nguyên layout cân đối khi kích thước màn hình thay đổi.
- Popup Nhiệm vụ (Quest) & Thông tin nhân vật (Profile): Sửa lỗi text chỉ số tiến trình (`10 / 10`) và EXP bị lệch sát đáy dưới thanh bằng cách reset `margin` và `padding` về `0` cho `.quest-progress-text` và `.profile-exp-text`.
- Popup Nhiệm vụ (Quest) & Thông tin nhân vật (Profile): Khắc phục lỗi thanh tiến trình khi đầy 100% bị khuyết vệt đen ở đầu bên phải do lỗi render bo góc bằng cách thiết lập `border-radius` cho `.quest-progress-fill` và `.profile-exp-fill` tương thích với khung track của chúng.
- Popup Điểm danh (Attendance): Reset margin và padding về 0 cho emoji và chữ số lượng phần thưởng ngày để tránh lệch tâm hiển thị.
### Files changed
- Assets/UI/Styles/FriendsPopup.uss (MODIFIED)
- Assets/UI/Styles/QuestPopup.uss (MODIFIED)
- Assets/UI/Styles/AttendancePopup.uss (MODIFIED)
- Assets/UI/Styles/ProfilePopup.uss (MODIFIED)

## [2026-06-02] — HUD Sidebar & 3 New Popups (Profile, Attendance, Quest)
### Added
- Tái cấu trúc HUD Sidebar (GameHUD.uxml) theo đúng thứ tự từ trên xuống: Leaderboard (🏆 - vàng), Điểm danh (📅 - tím, nút mới), Hòm thư (✉ - xanh dương), Bạn bè (👥 - xanh lơ). Loại bỏ nút Character cũ.
- Thiết lập Avatar (PlayerInfo) và Quest Bubble (QuestBubble) thành các phần tử tương tác bấm được (Clickable) với đầy đủ hiệu ứng phóng to/thu nhỏ (hover/active scale).
- Popup Thông tin nhân vật (Profile Popup) dạng landscape nền kem `#F5F0E8`: Hiển thị Avatar lớn, thanh tiến trình EXP lớn, và lưới chỉ số nông trại mockup (Cây đã trồng, Nông sản đã bán, Số bạn bè) tự động tải dữ liệu từ HUD.
- Popup Điểm danh 7 ngày (Attendance Popup) dạng lưới 7 ô slot quà: Hiển thị quà đính kèm và trạng thái (Đã nhận / Chưa nhận). Tích hợp nút Điểm danh nhận thưởng và cập nhật trạng thái ô lưới thời gian thực.
- Popup Nhật ký nhiệm vụ (Quest Journal Popup) dạng landscape 2 cột: Danh sách nhiệm vụ đang làm/đã xong bên trái, chi tiết yêu cầu và quà đính kèm bên phải. Cho phép nhận thưởng và đổi trạng thái khi nhiệm vụ hoàn thành.
### Changed
- Cập nhật `GameHUDController.cs` để query các phần tử mới, đăng ký callback click mở 3 popup mới (Profile, Attendance, Quest) và truyền dữ liệu động từ HUD sang Profile.
### Files changed
- Assets/UI/GameHUD.uxml (MODIFIED)
- Assets/UI/GameHUDController.cs (MODIFIED)
- Assets/UI/Styles/GameHUD.uss (MODIFIED)
- Assets/UI/ProfilePopup.uxml (NEW)
- Assets/UI/ProfilePopupController.cs (NEW)
- Assets/UI/Styles/ProfilePopup.uss (NEW)
- Assets/UI/AttendancePopup.uxml (NEW)
- Assets/UI/AttendancePopupController.cs (NEW)
- Assets/UI/Styles/AttendancePopup.uss (NEW)
- Assets/UI/QuestPopup.uxml (NEW)
- Assets/UI/QuestPopupController.cs (NEW)
- Assets/UI/Styles/QuestPopup.uss (NEW)

---

## [2026-06-02] — Module Mailbox Popup
### Added
- Giao diện Hòm thư (Mailbox Popup) dạng landscape chuẩn phong cách "The Tangible Playground" đè lên cảnh game 3D.
- Cột bên trái: Danh sách các thư cuộn mượt mà. Mỗi card thư hiển thị trạng thái động (Đã đọc/Chưa đọc bằng phong bì đóng/mở và chấm xanh dương), ngày gửi, người gửi, và huy hiệu hộp quà nếu có phần thưởng.
- Cột bên phải: Thẻ chi tiết thư nền trắng đặc bo góc, viền tối dày. Khi có thư sẽ hiện tiêu đề, nội dung, lưới ô slot quà đính kèm và nút hành động.
- Nút "Nhận tất cả" ở chân cột danh sách trái hỗ trợ claim nhanh mọi phần thưởng chưa nhận. Nút "Xóa đã đọc" hỗ trợ dọn dẹp hòm thư tự động.
- Nút "Nhận quà" và "Xóa thư" riêng lẻ cho từng thư, cập nhật trạng thái "Đã nhận" thời gian thực.
- Kết nối nút Hòm thư (phong bì) trên HUD sidebar để mở popup.
### Changed
- Cập nhật GameHUDController.cs để tích hợp liên kết gọi MailboxPopupController.Show().
### Files changed
- Assets/UI/MailboxPopup.uxml (NEW)
- Assets/UI/Styles/MailboxPopup.uss (NEW)
- Assets/UI/MailboxPopupController.cs (NEW)
- Assets/UI/GameHUDController.cs (MODIFIED)

---

## [2026-06-02] — Universal Design System & AI UI Guidelines
### Added
- Thiết lập và nâng cấp tài liệu `docs/DESIGN_SYSTEM_TEMPLATE.md` thành **Universal Design System Template** dùng chung cho cả 3 nền tảng: **Web**, **Mobile App**, và **Game**.
- Cấu trúc lại file thành **bản song ngữ (bản tiếng Anh và bản tiếng Việt)** chia làm 2 mục lớn rõ ràng, phục vụ cả các AI Agent quốc tế lẫn các nhà phát triển Việt Nam.
- Tích hợp chi tiết phân tích biểu hiện, tác hại và cách phòng tránh cụ thể cho **8 bệnh lý giao diện kinh điển của AI Agent** (lạm dụng glassmorphism/icon, loạn đơn vị, chột trạng thái, mù tương phản, cụt chữ...) trên từng nền tảng bằng cả hai ngôn ngữ.
- Cung cấp **AI Agent Self-Check Protocol** với 15 câu hỏi kiểm tra chất lượng tự động giúp AI tự kiểm duyệt UI trước khi bàn giao.
### Changed
- Cập nhật tài liệu thiết kế của dự án [DESIGN.md](file:///d:/LamGameUnity/BaChuKhuRung3D/docs/DESIGN.md) áp dụng các thông số thực tế của game **Y WONDER GREEN FARM** theo đúng khung chuẩn Universal và tích hợp các nguyên tắc ngăn ngừa bệnh UI để dự án trực tiếp áp dụng.
### Files changed
- docs/DESIGN_SYSTEM_TEMPLATE.md (NEW)
- docs/DESIGN.md (MODIFIED)

---

## [2026-06-02] — Module Friends Popup
### Added
- Popup Bạn Bè (Friends Popup) landscape theo phong cách "The Tangible Playground" và hình ảnh tham khảo.
- Cột bên trái gồm 3 tab dạng chữ gọn gàng, giảm thiểu icon: Bạn bè, Lời mời kết bạn, Tìm bạn.
- Khu vực hiển thị bên phải:
  - Thanh tìm kiếm theo tên và nút "Làm mới" danh sách gợi ý.
  - Danh sách người chơi dạng thẻ bo góc 14px, viền 2px và bóng đổ 3px, nền trắng.
  - Avatar đại diện dạng tròn hiển thị emoji, giới tính (♂/♀), cấp độ và trạng thái Online/Offline (chấm xanh/xám).
  - Các nút hành động dạng chữ rõ ràng: "Kết bạn" (xanh lá), "Xóa bạn" (đỏ), "Đồng ý" (xanh dương), "Từ chối" (xám).
- Mock Data phong phú cho cả 3 chế độ danh sách, tích hợp tìm kiếm lọc tên và chức năng thêm/xóa/phản hồi kết bạn cập nhật UI thời gian thực.
- Kết nối nút Bạn bè (👥) trên Game HUD để mở popup.
### Fixed
- Sửa lỗi chữ nút "Làm mới" bị khuyết thành "Làm" bằng cách rút gọn độ rộng của ô tìm kiếm (từ 170px xuống 120px), tránh tràn viền panel.
### Files changed
- Assets/UI/FriendsPopup.uxml (NEW)
- Assets/UI/Styles/FriendsPopup.uss (NEW)
- Assets/UI/FriendsPopupController.cs (NEW)
- Assets/UI/GameHUDController.cs (MODIFIED)

---

## [2026-06-02] — Module Leaderboard Popup
### Added
- Popup Bảng Xếp Hạng (Leaderboard) theo phong cách thiết kế "The Tangible Playground" và hình ảnh tham khảo.
- Hàng ngang chứa 5 tab phân loại: Diligence (EXP), Level (★), Fashion (★), Pet (★), Rich (Coin).
- Lưới danh sách sọc vằn Zebra (Hạng 1 nền kem nhạt, Hạng 2 nền xanh lơ nhạt, Hạng 3 nền hồng nhạt, các hạng sau trắng/xám lơ nhẹ xen kẽ).
- Huy chương Unicode (🥇, 🥈, 🥉) nổi bật cho top 3 và khung hình thoi màu vàng nhạt cho thứ hạng 4 trở đi.
- Nút đóng (X) màu đỏ cơ học có bóng đổ ở góc trên bên phải.
- Biểu tượng Cúp Vàng 3D đồ chơi mộc mạc (không chứa chữ) nhô lên đè trên thanh tiêu đề chính.
- Một thẻ nhỏ nổi lên ở góc dưới bên phải hiển thị thứ hạng của người chơi: "My Rank: 100+".
- Tích hợp dữ liệu giả lập (Mock Data) phong phú với tên nông trại thuần Việt (carot6868, Anhbaole, Haiau1982...) và chỉ số động thay đổi theo từng tab.
- Kết nối nút Cúp Vàng trên HUD để mở Bảng Xếp Hạng.
### Design
- Nền panel màu tím rực rỡ #5B42F3 (Hero Surface), bo góc 22px, viền 3px đen xám #3D3535.
- Hộp tiêu đề màu xanh lam capsule #4F59E3.
- Tab chưa chọn màu vàng cam #FDBE5B, tab đang chọn màu tím xám #8A7D9D.
### Fixed
- Căn giữa chữ "LEADERBOARD" trên thanh tiêu đề bằng cách loại bỏ phần padding bên trái dùng cho biểu tượng cúp vàng cũ (sau khi ẩn cúp vàng đi).
### Files changed
- Assets/UI/LeaderboardPopup.uxml (NEW)
- Assets/UI/Styles/LeaderboardPopup.uss (NEW)
- Assets/UI/LeaderboardPopupController.cs (NEW)
- Assets/UI/Textures/LeaderboardTrophy.png (NEW)
- Assets/UI/GameHUDController.cs (MODIFIED)

---

## [2026-06-02] — Module Inventory Popup
### Added
- Nâng cấp giao diện túi đồ (Inventory) thành bố cục 3 cột (Tabs -> Grid -> Detail Panel) landscape theo phong cách Tangible Playground
- Cột bên trái gồm 6 tab phân loại: Dụng cụ (Tool), Nguyên liệu (Material), Hạt giống (Seed), Thực phẩm (Food), Trang phục (Outfit), Đặc biệt (Special)
- Lưới 21 ô chứa vật phẩm mockup ở cột giữa
- Cột bên phải là Khung chi tiết vật phẩm (Item Detail Panel):
  - Hiển thị tên, icon lớn, số lượng và mô tả của vật phẩm được chọn.
  - Tự động thay đổi nút hành động động (Dynamic Button) theo loại (ví dụ: Trang bị, Ăn, Gieo hạt, Chế tạo...) và nút Vứt bỏ.
- Hỗ trợ tự động chọn vật phẩm đầu tiên khi mở túi đồ hoặc chuyển tab.
- Kết nối nút Túi đồ (Bag Button) trên HUD với Inventory Popup để mở/đóng popup.
- Hỗ trợ đóng popup qua nút đóng (X) hoặc bấm lại nút Túi đồ trên HUD.
### Design
- Header cam #E8833A, panel kem #F5F0E8
- Khung chi tiết bên phải màu nền trắng #FFFFFF, bo góc 16px, viền đậm 3px đồng bộ.
- Thẻ vật phẩm khi được chọn có viền cam rực rỡ #E8833A.
- Viền đậm 3px, góc bo tròn 16px-22px, retro shadow 6px offset.
- Các tab được bo tròn góc trái và có mechanical press khi chọn.
### Files changed
- Assets/UI/Styles/InventoryPopup.uss (NEW)
- Assets/UI/InventoryPopup.uxml (NEW)
- Assets/UI/InventoryPopupController.cs (NEW)
- Assets/UI/GameHUDController.cs (MODIFIED)

---

## [2026-06-01] — GitHub Repository
### Added
- Kết nối dự án với GitHub: `Lam-Phong-Tech/y-wonder-land`
- Tạo `.gitignore` cho Unity (ignore Library, Temp, Obj, Build, IDE, OS files)
- Initial commit + push thành công
### Files changed
- .gitignore (NEW)

---

## [2026-06-01] — Module Settings Popup
### Added
- Popup cài đặt landscape (nằm ngang) 2 cột theo Tangible Playground
- 4 section: Âm thanh (Music/SFX), Camera (Sensitivity/Zoom), Đồ họa (Quality/Shadow), Chung (Language)
- 5 sliders + 1 toggle + 1 dropdown
- Nút X ở góc panel (kiểu Gemini viewer)
- 2 nút dưới: Xóa tài khoản (đỏ) + Thoát game (xanh)
- Overlay đen 40% khi popup mở
- Kết nối nút ⚙ trên HUD → mở Settings popup
### Design
- Header tím #5B42F3, panel kem #F5F0E8
- Slider accent tím, toggle pill-shaped
- Retro shadow, mechanical press on buttons
### Fixed
- Nút X bị chìm sau panel → chuyển SAU panel trong UXML (z-order)
- Chữ "CÀI ĐẶT" lệch → dùng position absolute căn giữa
- "Tiếng Việt" bị cắt → giảm label width, thêm min-width dropdown
- Toggle hiện ô vuông xanh → style lại thành pill-shaped track
### Files changed
- Assets/UI/Styles/SettingsPopup.uss (NEW)
- Assets/UI/SettingsPopup.uxml (NEW)
- Assets/UI/SettingsPopupController.cs (NEW)
- Assets/UI/GameHUDController.cs (MODIFIED — kết nối nút Settings)

---

## [2026-06-01] — Fork & Customize unity-ai-workflow
### Changed
- Toàn bộ toolkit customize cho Unity 2022 LTS (từ Unity 6.2+)
- Awaitable → UniTask (async pattern)
- K&R braces → Allman braces (code examples)
- Assets/_Project/ → Assets/ (type-based folder)
- Assembly Definitions + GameDebug → optional
### Added
- UGS Dashboard section trong TOOLING.md
- 8 UGS packages trong ASSET_RESOURCES.md
- UGS integration patterns trong network-engineer agent
- Manual Q<T>() binding thay thế Runtime Data Binding
- UGS routing entries trong AGENTS.md
- Section 9: UGS Rules trong .agent/rules/RULES.md
### Files changed (15 files)
- unity-ai-workflow/README.md (MODIFIED)
- unity-ai-workflow/CLAUDE.md (MODIFIED)
- unity-ai-workflow/.agent/rules/RULES.md (MODIFIED)
- unity-ai-workflow/.agent/rules/AGENTS.md (MODIFIED)
- unity-ai-workflow/.agent/agents/network-engineer.md (MODIFIED)
- unity-ai-workflow/.agent/agents/ui-specialist.md (MODIFIED)
- unity-ai-workflow/.agent/skills/ui-toolkit-binder/SKILL.md (MODIFIED)
- unity-ai-workflow/docs/CODING_STANDARDS.md (MODIFIED)
- unity-ai-workflow/docs/NAMING_CONVENTIONS.md (MODIFIED)
- unity-ai-workflow/docs/DESIGN_PRINCIPLES.md (MODIFIED)
- unity-ai-workflow/docs/TOOLING.md (MODIFIED)
- unity-ai-workflow/docs/ASSET_RESOURCES.md (MODIFIED)
- unity-ai-workflow/docs/phases/03_ProjectSetup.md (MODIFIED)
- unity-ai-workflow/templates/ProjectConfig_Template.yaml (MODIFIED)
- RULES.md (MODIFIED — thêm AI WORKFLOW REFERENCE table)

---

## [2026-06-01] — Documentation bổ sung
### Added
- docs/CONTEXT_RECOVERY.md — Prompt khởi động khi mở chat mới
- docs/MEMORY.md — 14 bài học kinh nghiệm, sai lầm cần tránh
- Context Canary trong RULES.md (AI xưng "bé", gọi "anh yêu")
### Changed
- RULES.md — Thêm MEMORY.md vào session checklist (bước 2)
- RULES.md — Thêm AI WORKFLOW REFERENCE table
### Files changed
- docs/CONTEXT_RECOVERY.md (NEW)
- docs/MEMORY.md (NEW)
- RULES.md (MODIFIED)

---

## [2026-06-01] — Module Game HUD
### Added
- HUD layout 8 thành phần (Player Info, Currency, Quest, Sidebar, Joystick, Action Buttons, Messages, Jump)
- HUD styles theo Tangible Playground (solid colors, retro shadow, mechanical press)
- HUD controller mockup với public API (SetPlayerInfo, SetCurrency, SetQuest, SetPlayerEXP)
- Nút Settings (tròn, đen) và Bag/Inventory (xanh dương, vuông bo góc)
### Design
- Action Buttons: hình tròn hoàn hảo, viền 3px, retro shadow
- Sidebar: solid white buttons (không dùng nền trong suốt)
- Layout cột trái: Player Info → Quest Bubble → Sidebar Buttons (flow tự nhiên, không overlap)
### Files changed
- Assets/UI/GameHUD.uxml (NEW)
- Assets/UI/Styles/GameHUD.uss (NEW)
- Assets/UI/GameHUDController.cs (NEW)

---

## [2026-06-01] — Bộ Documentation
### Added
- RULES.md — Quy tắc dự án + QC Pass system + quy tắc UGS
- docs/ARCHITECTURE.md — Kiến trúc Unity + UGS (viết lại từ template web)
- docs/DATA_SCHEMA.md — Cấu trúc Cloud Save, Economy, Leaderboards
- docs/API_CONTRACTS.md — Blueprint tích hợp 8 UGS services
- docs/CHANGELOG.md (NEW — file này)
### Files changed
- RULES.md (MODIFIED)
- docs/ARCHITECTURE.md (MODIFIED — viết lại)
- docs/DATA_SCHEMA.md (NEW)
- docs/API_CONTRACTS.md (NEW)
- docs/CHANGELOG.md (NEW)

---

## [2026-05-29] — Module Character Selection
### Added
- Màn hình chọn giới tính theo Tangible Playground style
- Card nhân vật: nền trắng, viền đậm, retro shadow
- Highlight card selected: viền xanh #2D7BFF
- Nút xác nhận với mechanical press effect
### Files changed
- Assets/UI/Styles/CharacterSelect.uss (NEW)
- Assets/UI/MainMenuUI.uxml (MODIFIED — redesign)
- Assets/UI/MainMenuUIToolkit.cs (MODIFIED — class name update)

---

## [2026-05-27] — Module Login/Register UI
### Added
- Màn hình đăng nhập/đăng ký với design Tangible Playground
- Form fields: Username, Password, Remember Me checkbox
- Nút Đăng nhập/Đăng ký với retro shadow + mechanical press
- Design System tokens (DesignSystem.uss)
### Changed
- GameManager.cs — Thêm Login state vào state machine
### Files changed
- Assets/UI/Styles/DesignSystem.uss (NEW) ⚠️ PROTECTED
- Assets/UI/Styles/LoginScreen.uss (NEW)
- Assets/UI/LoginScreen.uxml (NEW)
- Assets/UI/LoginScreenController.cs (NEW)
- Assets/GameManager.cs (MODIFIED) ⚠️ PROTECTED

---

## [2026-05-27] — Khởi tạo dự án
### Added
- Unity project setup (URP, Unity 6.3 LTS)
- 3D environment cơ bản (terrain, trees, house)
- Character model + animations
- Camera controller
- docs/DESIGN.md — Hệ thống thiết kế "The Tangible Playground"
### Files changed
- docs/DESIGN.md (NEW)
- Assets/* (Initial setup)

---

<!-- Template cho module mới:

## [YYYY-MM-DD] — Module [Tên Module]
### Added
- Mô tả tính năng mới
### Changed
- Mô tả thay đổi
### Fixed
- Mô tả bug fix
### Security
- Mô tả cải thiện bảo mật
### Files changed
- path/to/file.cs (NEW/MODIFIED/DELETED)

-->
