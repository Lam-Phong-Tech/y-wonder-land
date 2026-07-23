# CÂU HỎI KHÁCH CÒN TREO — chờ dịp hỏi tiếp

> Cập nhật: 22/07/2026. Gộp phần khách CHƯA trả lời sau vòng 2 (`CAU_HOI_KHACH_VONG_2_2026-07-21.md`)
> và các câu MỚI nảy sinh từ ảnh infographic "Cây hoa hồng" khách gửi 22/07.
> Khách đã hứa "để anh làm chi tiết ra hệ thống" cho phần lớn mục 1–2 → chờ bản chi tiết đó.

---

## 1. CÂU 21 — Màn hình theo dõi VIP (khách mới trả lời MỘT PHẦN)

**Đã rõ:** chỉ báo VIP — chưa VIP thì chữ VIP **mờ** (có nút "Nâng cấp VIP"); đã VIP thì **huy hiệu VIP rõ**.
(Khách gửi 2 ảnh dashboard minh hoạ.)

**⚠️ Khách hiểu lệch:** ảnh khách gửi là **dashboard WEB**. Bên em hỏi về **màn hình theo dõi VIP trong GAME**
(Tổng thành viên / Tổng "3F1, VIP liền kề" / Doanh số theo ngày–tuần–tháng) → **phần thống kê vẫn chưa có gì.**

**Còn phải hỏi:**
1. **"3F1"** nghĩa là gì? (có phải 3 người giới thiệu trực tiếp không?) — khách nói *"để anh ghi chi tiết ra hệ thống"*.
2. **"VIP liền kề"** là người VIP gần nhất **tuyến trên**, hay **số người VIP tuyến dưới**?
3. **"Doanh số"** tính tiền tiêu **riêng người đó**, hay **cộng cả tuyến dưới**?

---

## 2. DỮ LIỆU CẦN BÊN WEB CUNG CẤP (khách nói "để anh làm chi tiết")

1. Quy định **phí / mức tối thiểu / mức tối đa / hạn mức rút** đang áp dụng.
2. **Cách lấy dữ liệu cây giới thiệu 6 cấp** (game hiện KHÔNG có dữ liệu này — không có thì không dựng được
   màn hình VIP lẫn phần chia hoa hồng phía game).

---

## 3. CÂU MỚI — phát sinh từ infographic "Cây hoa hồng" (22/07)

Infographic đã chốt giúp: **F1 8% · F2–F6 mỗi tầng 1% · tổng 13%**, tính trên **doanh số Point tiêu dùng hợp lệ**
từng tầng, ghi nhận theo **upline–downline trực tiếp**, tối đa 6 tầng. Nhưng nảy ra 3 điểm cần làm rõ:

1. **KYC:** infographic ghi *"Thành viên phải KYC thành công và tài khoản hoạt động hợp lệ"* mới được hoa hồng.
   → Game có phải **kiểm tra trạng thái KYC** không? Lấy trạng thái đó từ **API web nào**?
   (Trước giờ chưa hề bàn tới KYC trong 21 câu.)
2. **Đơn vị trả hoa hồng:** infographic ví dụ trả bằng **Point** (tổng 28.000.000 Point), nhưng **Câu 15**
   lại nói ghi **USDT** cho hoa hồng tuyến dưới.
   → Khi nào trả **Point**, khi nào trả **USDT**? (theo NGUỒN của số Point đã tiêu?)
3. **Tự động vs thủ công:** infographic ghi *"hoa hồng được chi trả **tự động** theo chu kỳ"*, còn **Câu 15**
   chốt là **duyệt rút thủ công** (báo admin/Telegram rồi chuyển tay).
   → Hiểu là **ghi nhận/cộng tự động, còn RÚT ra ngoài thì thủ công** — đúng không ạ?

---

## 4. ĐIỂM CẦN ĐỂ Ý (không phải câu hỏi, nhưng dễ làm sai)

- **Tỷ giá:** khách dùng nhất quán **1 USDT = 26,5 Point** (100 USDT = 2.650; 10 USDT = 265),
  KHÁC con số 26 ghi trong biên bản 22/06. Khách nói tỷ giá **do Admin đặt** → **KHÔNG hardcode**,
  phải để cấu hình được, mặc định 26,5.
- **Tái đầu tư mở lại quyền rút (Câu 14):** khách ghi *"tối thiểu 100 USDT **và** tiêu dùng 2.650 point"*
  — chưa rõ là phải làm **cả hai**, hay nạp 100 USDT rồi tiêu chính 2.650 point đó. Nên xác nhận lại 1 câu.

---

## 5. 🚨 ĐỐI CHIẾU TÀI LIỆU — luật nào CÓ trong tài liệu, luật nào khách TỰ THÊM (rà 22/07)

> **Lý do rà:** một bên khác hỏi "các tính năng VIP / tiền tệ / nạp rút này là khách tự thêm hay tài liệu có sẵn?".
> Bé đã grep toàn bộ `Assets/_Project/Docs_KichBan/`. Kết quả dưới đây dùng làm **bằng chứng** khi hỏi lại khách.

**Bộ tài liệu chia 2 nhóm:**

| Nhóm | Ngày | File |
|---|---|---|
| Kịch bản game gốc | 24/06 | `YWONDERLAND_KichBan3D_ChiTiet.md`, `CayTrong2`, `VatNuoi2`, `CachTinh`… |
| **Tài liệu nền tảng WEB** | **16/07** | `YWonder-HDSD-Nguoi-Dung / Dai-Ly / Admin`, `YWonder-Co-Che-Ky-Thuat`, `YWonder-Tong-Hop-Chuc-Nang` |

### 5.1 ✅ CÓ SẴN trong tài liệu (KHÔNG phải khách tự nghĩ ra)

- **VIP** — có ở **cả hai nhóm**. Kịch bản game gốc nhắc VIP **32 lần** (mở đảo Hải Phú/Mộc Nhi, chuồng >10,
  thăm nông trại bạn, pet/maid VIP, thẻ KNX; tiền tệ POS + UPOS). Tài liệu web gắn VIP với ví USDT.
- **Hoa hồng 6 tầng F1–F6** — `YWonder-HDSD-Dai-Ly.md:167-184`. Tỷ lệ **F1 8% · F2–F6 mỗi tầng 1%**
  (khớp infographic khách gửi 22/07). **Tài liệu còn CHI TIẾT HƠN infographic:**
  - Cột *"số thành viên active cần đạt"*: F1 **5** · F2 **25** · F3 **125** · F4 **625** · F5 **3.125** · F6 **5.625**
    → **mỗi tầng chỉ trả khi đủ tuyến dưới active** (infographic KHÔNG có điều kiện này).
  - **Chỉ tính cho upline là Đại lý** (bỏ qua mắt xích là thành viên thường).
  - **F1 (8%) trả NGAY vào ví, rút liền; F2–F6 gom trả ngày 10/20/30** hằng tháng (cron).
  - Nhóm hàng đặc thù (vd "Vật nuôi") có thể bị admin **loại trừ hoa hồng**.
- **KYC** — có trong cả 6 file `YWonder-*`.
- **Nạp / rút USDT** — có, rất nhiều.

### 5.2 🚨 CÓ trong tài liệu nhưng **NGƯỢC HẲN** lời khách — PHẢI CHỐT TRƯỚC KHI CODE RÚT TIỀN

`YWonder-HDSD-Nguoi-Dung.md:102`:
> *"Khi đang hồi vốn: mỗi tháng rút tối đa 20% của tổng vốn đã nạp; **tổng rút không vượt quá vốn gốc**."*

`YWonder-HDSD-Admin.md:124`:
> *"đang hồi vốn rút ≤ 20%/tháng vốn nạp (không quá vốn gốc); **đã hồi đủ vốn → chỉ tiêu trong app 15%/tháng,
> không rút tiền mặt**."*

| | Tài liệu (16/07) | Khách nói miệng (Câu 14, 22/07) |
|---|---|---|
| Trần rút | **100%** (không vượt vốn gốc) | **300%** |
| Hết hạn mức | **Ngừng rút tiền mặt**, chỉ tiêu trong app 15%/tháng | **Tái đầu tư → tính tiếp 300% nữa** |

→ **Hai luật chọi nhau hoàn toàn.** Liên quan TIỀN THẬT → không được tự đoán.

### 5.3 ❌ KHÔNG có trong BẤT KỲ tài liệu nào (grep ra **0 kết quả**)

- **"300%"** — trần rút.
- **"2.650" / "2650"** — mốc VIP.
- **"26,5"** — tỷ giá USDT↔Point.

→ Ba con số này **chỉ xuất hiện trong lời khách trả lời**, không có văn bản nào chống lưng.

### 5.4 Câu cần hỏi khách

1. **Trần rút rốt cuộc là 100% (theo tài liệu) hay 300% (theo lời anh)?** Nếu 300% thì tài liệu web
   phải sửa lại, vì hiện đang ghi *"tổng rút không vượt quá vốn gốc"*.
2. Sau khi hồi đủ vốn: **ngừng rút tiền mặt + chỉ tiêu 15%/tháng** (tài liệu) hay **tái đầu tư để rút tiếp**
   (lời anh)?
3. Hoa hồng F2–F6 có giữ **điều kiện số thành viên active** (5/25/125/625/3.125/5.625) và **chỉ trả cho Đại lý**
   như tài liệu không? Infographic anh gửi không nhắc 2 điều kiện này.
4. **F1 trả ngay / F2–F6 trả ngày 10-20-30** — game có phải theo đúng lịch này không?
5. Ba con số **300% · 2.650 Point · tỷ giá 26,5** xin anh cho **văn bản chính thức** để bên em ghi vào hợp đồng
   dữ liệu (hiện chưa có trong tài liệu nào).

---

## 6. HỆ BỆNH THÚ — ĐÃ CHỐT: GIỮ NGUYÊN SỐ KHÁCH (23/07)

> ✅ **Sếp trả lời 23/07: "số liệu vắc-xin và thuốc là do khách họ muốn, mình cứ làm theo."**
> Cả 4 câu hỏi bên dưới **đóng lại, không đổi số**. Giữ lại phần phân tích để sau này ai đọc
> code khỏi tưởng là bug rồi "sửa" nhầm.

Đã áp **nguyên số** 4 cột bệnh của `VatNuoi2.md` vào game (thời điểm phát bệnh · tỉ lệ phát bệnh ·
số mũi vắc-xin · số liều thuốc).

Hai chỗ bảng tính không ghi rõ, bé chọn cách hiểu **khớp công thức chi phí của `CachTinh.md`**.
Khách không phản hồi khác nên **chốt theo cách hiểu này**:

1. **"Thời điểm phát bệnh" = 0.3** → hiểu là **× Số ngày nuôi**; bò 0.3 × 270 = đổ bệnh ở **ngày thứ 81**.
2. **"Số lượng thuốc trị bệnh cần" = 10 (bò)** → hiểu là *cả vòng nuôi chỉ bệnh 1 lần, lần đó tốn 10 liều*,
   vì công thức `CachTinh` chỉ cộng tiền thuốc **đúng một lần** rồi nhân với tỉ lệ phát bệnh.

⚠️ Đây là **suy luận của bên mình**, khách chưa xác nhận bằng văn bản. Nếu sau này khách nói khác
thì chỉ phải sửa 1 dòng trong `ItemDataGenerator.SetAnimalDisease(...)`, không đụng logic.

### 6.b Ý đồ thiết kế: thuốc đắt là để ÉP người chơi tiêm phòng

Dựng lại được đúng công thức của khách: `Tổng CP = Giá giống + Thức ăn + Vắc-xin + TỉLệBệnh × Thuốc`
(bò: 7800 + 5940 + 120 + 0.4×700 = 14140 ✓ khớp ô tổng chi phí trong `VatNuoi2`).

So "tiền vắc-xin cả vòng" với "kỳ vọng tiền thuốc" (= tỉ lệ bệnh × giá thuốc) thì thấy rõ chủ ý:

| Con | Vắc cả vòng | TỉLệ × Thuốc | Rẻ hơn |
|---|---|---|---|
| Hươu | 120 | 0.6 × 630 = **378** | vắc |
| Rùa | 120 | 0.6 × 700 = **420** | vắc |
| Bò | 120 | 0.4 × 700 = **280** | vắc |
| Đà điểu | 120 | 0.4 × 700 = **280** | vắc |
| Dê | 120 | 0.3 × 700 = **210** | vắc |
| Heo | 120 | 0.3 × 630 = **189** | vắc |
| Gà | 90 | 0.6 × 210 = **126** | vắc |
| Vịt | 60 | 0.6 × 140 = **84** | vắc |
| Thỏ | 60 | 0.5 × 140 = **70** | vắc (sát) |
| **Ngỗng** | **120** | 0.4 × 140 = **56** | **thuốc** |

→ 9/10 con, tiêm phòng rẻ hơn chịu rủi ro bệnh. Đây gần như chắc chắn là **cố ý**: vắc-xin là khoản
chi bắt buộc về mặt kinh tế, thuốc chỉ là hình phạt cho ai lười tiêm. **Không cần hỏi lại chỗ này.**

Hai dòng nhìn như gõ nhầm — **đã hỏi, sếp bảo giữ nguyên theo ý khách**:

1. **Ngỗng** — con DUY NHẤT ngược quy luật (tiêm 120, thuốc kỳ vọng chỉ 56 → tiêm phòng là lỗ).
   Cột "SL vắc-xin = 4" cao bất thường so với gà/vịt/thỏ (2–3 mũi). ➜ **GIỮ NGUYÊN.**
2. **Dê tốn 10 liều thuốc y hệt bò**, trong khi giá giống dê chỉ bằng ~1/6 bò và tiền thức ăn cả vòng
   chỉ 540 (thuốc chiếm 9.7% tổng CP, các con khác 1.6–2.1%). ➜ **GIỮ NGUYÊN.**

🔒 **Đừng "sửa" 2 dòng này.** Trông như lỗi copy nhưng là số khách chốt. Ai thấy lạ thì đọc mục này
trước khi đụng vào `ItemDataGenerator`.

> 📝 Bé từng ghi ở đây là "gà chữa 210 > mua mới 156 nên không ai chữa" — **sai, đã bỏ**. Tính thiếu
> thức ăn đã đổ vào: gà bệnh ở ngày 13.5/90, thức ăn đã tiêu ~162 → thay mới thật ra tốn 156+162 =
> **318**, vẫn đắt hơn chữa 210. Thỏ cũng vậy (thay ~598 vs chữa 140). Chữa luôn có lợi.

⚠️ **Hệ quả đã chấp nhận:** với thời gian thực (1 ngày game = 1 ngày thật), theo đúng bảng thì
**vịt bệnh sớm nhất ở ngày thứ ~7, gà ~13, hươu ~54, bò ~81** kể từ lúc thả. Nghĩa là người chơi mới
gần như **không thấy hệ bệnh trong tuần đầu** — đây là **hệ quả của số khách**, KHÔNG phải lỗi code.
Muốn demo/QC thấy bệnh ngay thì đặt `Sickness Onset Sec = 30` trên prefab con vật, xong trả về 0.

---

## ✅ ĐÃ CHỐT (khỏi hỏi lại) — 22/07

- Vốn khởi đầu tài khoản mới = **0 Point**. (Số 5.000 cũ là số tạm bên mình, không phải khoản tặng.)
- Khuyến mãi thật = **10 USDT/tháng = 265 Point** qua **nhiệm vụ + điểm danh**; tiêu bình thường
  nhưng **KHÔNG trả hoa hồng**.
- Tỷ lệ hoa hồng 6 tầng: **F1 8% · F2–F6 mỗi tầng 1% · tổng 13%**.
- Câu 14–20 đã chốt (xem `CAU_HOI_KHACH_VONG_2_2026-07-21.md` + CHANGELOG 22/07).
