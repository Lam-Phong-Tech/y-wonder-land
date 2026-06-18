# 🎯 KẾ HOẠCH BÀN GIAO — YWONDERLAND (2 TUẦN CUỐI)

> Lập ngày 16/06/2026. **Deadline ≈ 01/07/2026** (khách: 5 tuần kể từ 27/05). Còn **~15 ngày**.

## 1. Bối cảnh & ràng buộc
- **Ê-kíp:** anh Lâm (code/Unity) + bé (AI hỗ trợ code) + 2 artist 3D. **Không có backend dev riêng.**
- **Khách chốt (bắt buộc có khi bàn giao):**
  1. **Game đẩy lên CH Play (Android).**
  2. **Kết nối với web của game** — web đã có, **chưa có API cho game** → mình phải làm API + nối account.
- **Thực tế:** 2 tuần KHÔNG đủ làm 16 module. **Phải cắt scope mạnh**, tập trung 1 vòng lặp chơi ổn định + nối web + lên store.

## 2. Mục tiêu bàn giao (Definition of Done)
Một bản **MVP Android** cài được từ CH Play (ít nhất Internal/Closed testing), trong đó:
- Đăng nhập **bằng tài khoản web của khách** (qua API), dữ liệu lưu **thật trên server**.
- Chơi trọn vòng lặp: Onboarding → Tutorial → Nông trại (trồng/tưới/thu hoạch, chặt/đào) → Câu cá → Shop mua/bán → lưu tiến trình.
- Chạy ổn định trên điện thoại Android thật (không crash, không kẹt).

## 3. Phạm vi: GIỮ vs CẮT (đề xuất — cần anh chốt với khách)
| GIỮ (MVP) | CẮT / hoãn sau bàn giao |
|---|---|
| Onboarding + Tutorial | 3 map: Mỏ, Hải Phú, Mộc Nhi → khóa "Sắp ra mắt" |
| Nông trại: trồng/tưới/thu hoạch, chặt/đào | Cosmetic (thay đồ nhân vật) |
| Câu cá | Workshop nâng cấp sâu, Pet, Bạn bè online |
| Shop mua/bán cơ bản (1-2 shop) | Chat AI, Leaderboard online, Event phức tạp |
| Túi đồ, Tiền (POS) | Heo đất lãi suất (để mock/hoãn) |
| **Lưu thật qua API + nối web khách** | iOS (chỉ làm Android) |
| Đổi đảo Farm ↔ City | ~46 model 3D → chỉ làm bộ tối thiểu cho MVP |
| Build Android + publish CH Play | Level/EXP đầy đủ (tối giản: chỉ cộng điểm) |

## 4. Ba luồng việc SONG SONG
- **A. Gameplay MVP ổn định** (anh + bé): vá bug, khóa các tính năng cắt cho gọn, tối ưu mobile (touch input, joystick, FPS).
- **B. Backend API thật + nối web** (anh + bé): nâng `server/` stub → API thật, **deploy** (Render/Railway/VPS), phối hợp bên web khách để xác thực account chung, lưu profile/inventory/tiền.
- **C. Build & Publish Android** (anh): Player Settings, icon, package name, ký số (keystore), build AAB, tạo app trên Google Play Console, nộp **Internal Testing** SỚM.
- **Art (2 artist):** hoàn thiện model ưu tiên MVP (nhân vật, vài cây/vật nuôi, vài building cho City). Theo `RaSoat_KichBan_vs_Code.md`.

## 5. Lộ trình theo tuần
### TUẦN 1 (16–23/06): Khóa scope + dựng API thật + build thử
- [ ] **Chốt scope MVP với khách** (gửi bảng mục 3) — *làm NGAY ngày đầu.*
- [ ] **Phối hợp web khách:** lấy tài liệu/đầu mối để game đăng nhập bằng account web (cơ chế token chung). *Đây là việc gating — đẩy sớm nhất.*
- [ ] Nâng `server/` stub → API thật (profile + inventory + tiền), **deploy lên host** (có URL public).
- [ ] Client: trỏ `BackendConfig.baseUrl` sang URL thật; nối đăng nhập web.
- [ ] **Build APK thử & chạy trên điện thoại Android thật** (càng sớm càng tốt — lộ lỗi mobile sớm).
- [ ] Tối ưu điều khiển cảm ứng (joystick, tap tương tác) cho mobile.
- [ ] Artist: giao đợt model MVP đầu tiên.

### TUẦN 2 (24/06–01/07): Hoàn thiện + QA + phát hành
- [ ] Nối hết dữ liệu thật (inventory, tiền, farm state) qua API.
- [ ] QA toàn vòng lặp trên 2-3 máy Android khác nhau; sửa bug chặn.
- [ ] Polish nhẹ (âm thanh, hiệu ứng, loading).
- [ ] **Tạo keystore + build AAB release**, tạo app Google Play Console.
- [ ] **Nộp Internal Testing (CH Play) — chậm nhất ~27/06** (chừa thời gian Google duyệt + sửa nếu bị từ chối).
- [ ] Buffer 1-2 ngày cuối cho sự cố.

## 6. Rủi ro lớn & cách giảm
| Rủi ro | Cách giảm |
|---|---|
| **Duyệt CH Play mất thời gian** | Dùng **Internal Testing track** (duyệt nhanh hơn production), nộp SỚM tuần 2 |
| **API nối web khách bị kẹt** (chưa có API) | Đẩy việc phối hợp web lên ĐẦU tuần 1; nếu web chưa sẵn → tạm dùng server của mình + thống nhất chuẩn token để ghép sau |
| **Lỗi chỉ xuất hiện trên mobile** | Build APK thật ngay tuần 1, không đợi cuối |
| **Ký số / keystore lần đầu** | Làm thử sớm; GIỮ keystore cẩn thận (mất là không update được app) |
| **Ôm đồm quá nhiều tính năng** | Bám đúng bảng GIỮ/CẮT mục 3, từ chối thêm việc ngoài MVP |

## 7. Việc CẦN CHỐT NGAY (anh xử lý trong 1-2 ngày tới)
1. **Gửi khách bảng GIỮ/CẮT (mục 3)** để thống nhất phạm vi 2 tuần.
2. **Hỏi khách về web/API:** ai làm API phía web? cơ chế đăng nhập chung (token/OAuth)? base URL?
3. **Tài khoản Google Play Console** đã có chưa? (cần để publish — đăng ký mất phí + có thể cần xác minh).
4. Xác nhận **mức Android tối thiểu** + máy test.

## 8. Yêu cầu BỔ SUNG sau demo (14–15/06) + đánh giá khả thi

> Khách nêu thêm sau khi demo. Cột **Ước lượng**: 🟢 nhỏ (≤0.5 ngày) · 🟡 vừa (1–2 ngày) · 🔴 lớn (3+ ngày / tính năng mới). Cột **Đề xuất**: MVP (làm trong 2 tuần) · HOÃN (bản update sau bàn giao).

### 3D / Đồ họa
| Yêu cầu | Trạng thái | Ước lượng | Đề xuất |
|---|---|---|---|
| Trang phục đổi kiểu dáng/màu sắc (Cosmetic) | ❌ chưa có | 🔴 tính năng lớn (UI + model + lưu) | **HOÃN** |
| Chỉnh lại biển | 🟡 có shader nước | 🟡 | MVP (nếu kịp) |
| Làm lại Splash | ✅ đã dọn 16/06 | 🟢 | MVP (chỉ tinh chỉnh thêm) |
| Nhân vật cao hơn | 🟡 chỉnh scale | 🟢 (lưu ý bài học scale nhân chồng) | MVP |

### Gameplay
| Yêu cầu | Trạng thái | Ước lượng | Đề xuất |
|---|---|---|---|
| Thêm cá (loại cá câu) | 🟡 có hệ thống câu | 🟡 thêm data + model | MVP |
| Tính năng tương tác **xã hội** | ❌ (cần online) | 🔴 lớn | **HOÃN** |
| Thêm item + hoạt ảnh thú ăn (Feed) | 🟡 đã tư vấn | 🟡 (VFX rắc thức ăn) | MVP |
| Bỏ đá rung / cây rung khi chặt | ✅ **ĐÃ XONG 16/06** | — | ✅ Done |
| Xóa lá khi chặt xong cây | ✅ **ĐÃ XONG 16/06** | — | ✅ Done |

### Camera / Hiệu năng
| Yêu cầu | Trạng thái | Ước lượng | Đề xuất |
|---|---|---|---|
| Chỉnh tầm nhìn camera (far clip/culling tránh load thừa) | ❌ | 🟡 | MVP (quan trọng cho FPS mobile) |
| Sửa camera đỡ chóng mặt | ❌ | 🟡 tuning | MVP |
| Làm Minimap | ❌ | 🔴 tính năng mới | HOÃN (hoặc bản đơn giản nếu kịp) |

### UX / Tương tác
| Yêu cầu | Trạng thái | Ước lượng | Đề xuất |
|---|---|---|---|
| Ô vuông đánh dấu nơi gieo hạt | 🟡 có highlight tutorial | 🟢 | MVP (dễ + tăng UX rõ rệt) |
| Tương tác NPC bằng **bước vào vùng** (bỏ bấm) | 🟡 có trigger sẵn (MapPortal) | 🟡 | MVP |

### Map / Thiết kế
| Yêu cầu | Trạng thái | Ước lượng | Đề xuất |
|---|---|---|---|
| Đảo 1 (Farm): mua/bán sản phẩm | 🟡 có khung Shop | 🟡 | MVP |
| Độ rộng đảo = **900 ô** | spec thiết kế | 🟢 ghi nhận | MVP (thông số) |
| Khi cuốc → ô **4x4**, 1 ô trống chứa đúng 1 con vật | ❌ (hệ đặt chuồng/animal grid) | 🔴 logic mới | HOÃN / bản tối giản |

### 📊 ĐÁNH GIÁ TỔNG (thành thật)
- **Tổng yêu cầu mới:** ~17 mục. Trong đó **2 đã xong**, ~9 nhỏ/vừa (làm được trong MVP nếu ưu tiên), **~5 là tính năng MỚI lớn** (Cosmetic, Xã hội, Minimap, Animal grid 4x4).
- **Hai việc CỐT LÕI khách chốt vẫn là:** (1) lên CH Play, (2) nối web. Hai việc này **một mình đã ăn gần hết 2 tuần** của ê-kíp không có backend dev.
- **Kết luận:** Làm **HẾT** danh sách + lên store + nối web trong 2 tuần với 1 dev + 2 artist là **KHÔNG khả thi**. Đây không phải lỗi năng lực — mà là **kỳ vọng vượt khung thời gian/nguồn lực** (scope creep sau demo).
- **Cách xử lý đề xuất:** Tách 2 bản giao:
  - **Bản 1 (đúng hạn ~1/7):** MVP lên CH Play + nối web + các fix/polish NHỎ (cột "MVP").
  - **Bản 2 (update sau, +2–4 tuần):** các tính năng LỚN (Cosmetic, Xã hội, Minimap, Animal grid...).
  - → Khách vẫn có sản phẩm trên chợ đúng hạn; phần chỉn chu đẩy vào update. Đây là cách làm chuẩn của game mobile.

## 9. Chiến lược khi khách KHÔNG lùi deadline (dev làm việc qua BA)

> Bối cảnh: dev (anh) KHÔNG làm việc trực tiếp với khách — phòng Marketing/BA làm. Khách lớn tuổi, lowtech, **không thỏa hiệp deadline**, chỉ quan tâm *"đăng game lên chợ và thu tiền về"*.

### ⭐ Kim chỉ nam (bất biến #1)
**GAME LÊN ĐƯỢC CH PLAY + CHẠY ỔN + NỐI WEB.** Khi phải chọn giữa 2 việc → luôn ưu tiên việc nào **đưa game lên chợ nhanh hơn**. Tính năng đẹp mà cản lên chợ → hoãn.

### Quy tắc ra quyết định khi bị giằng
- Việc **cản phát hành** (build lỗi, crash, không nối được web) → **ưu tiên số 1**.
- Việc **không ảnh hưởng phát hành** (cosmetic, minimap, social) → **tối giản hoặc hoãn**, không để nó kéo lùi deadline.

### Descope NGẦM (silent descoping) — vũ khí chính
Tính năng lớn không kịp → KHÔNG làm đầy đủ, cũng KHÔNG để trống/lỗi. Thay bằng:
- **Bản tối giản** trông "có vẻ xong" (vd Minimap = ảnh tĩnh + chấm vị trí; Cosmetic = đổi màu vài món).
- **Khóa "Sắp ra mắt"** (3 đảo, tính năng xã hội) — nhìn chuyên nghiệp, không phải lỗi.
> Khách lowtech đánh giá bằng **"nhìn thấy chạy được"**, không phân biệt được "đầy đủ" vs "tối giản". Tận dụng điều này một cách trung thực: ưu tiên thứ **NHÌN THẤY ĐƯỢC** (vào game, mua bán, mượt) hơn thứ ngầm.

### Làm việc qua BA (không cãi khách trực tiếp)
- **Báo cáo tiến độ phi kỹ thuật cho BA mỗi 2–3 ngày**: dạng %, **video/ảnh chạy thật**, không thuật ngữ. BA cần "đạn" để giữ khách.
- Đề xuất BA "bán" cho khách thông điệp dễ nuốt: *"Bản lên chợ đúng hạn, các cải tiến cập nhật liên tục sau"* — đúng cách game mobile vận hành, khách dễ gật.
- **CYA (tự bảo vệ):** mọi cảnh báo rủi ro/trễ → ghi bằng **văn bản** (email/chat nội bộ) gửi BA/PM, có mốc thời gian. Nếu sau này trễ, anh có bằng chứng đã cảnh báo sớm — không thành người chịu trận.

### Điều anh (dev) NÊN và KHÔNG NÊN
- ✅ NÊN: dồn sức vào "lên chợ + nối web + ổn định"; làm polish dễ-ăn-điểm để có cái demo cho BA.
- ❌ KHÔNG NÊN: hứa "làm hết" để yên chuyện; ôm hết yêu cầu rồi vỡ trận cuối kỳ; im lặng tới deadline mới báo trễ.

---
> 📌 Mỗi sáng anh chỉ cần mở file này, nhìn mục **5 (tuần hiện tại)** và làm theo checklist. Không nhìn cả 16 module nữa cho đỡ ngợp. Bé sẽ cập nhật tiến độ [x] cùng anh mỗi khi xong một mục.
