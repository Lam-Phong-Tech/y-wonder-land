# UNIVERSAL DESIGN SYSTEM TEMPLATE & AI UI GUIDELINES
*(Tài liệu mẫu chuẩn hóa thiết kế giao diện - Dành cho các dự án Web, Mobile App và Game)*

> [!IMPORTANT]
> **HƯỚNG DẪN DÀNH CHO AI AGENT (Đọc kỹ trước khi tạo file thiết kế cụ thể):**
> 1. Tài liệu này là **Khung Template mẫu (Universal Template)**. Khi nhận được yêu cầu viết tài liệu thiết kế giao diện (`design.md` hoặc `design_system.md`) cho một dự án cụ thể, AI phải copy khung này, thay thế các phần nằm trong ngoặc vuông `[...]` bằng thông số thiết kế thực tế của dự án đó.
> 2. AI **không được xóa** Phần II (Danh mục 8 bệnh lý UI) và Phần III (Quy trình tự kiểm tra) trong tài liệu đầu ra của dự án. Đây là các điều khoản bắt buộc để đảm bảo chất lượng UI trong suốt quá trình phát triển.

---

# [Tên Hệ Thống Thiết Kế Của Dự Án - VD: The Tangible Playground]

## PHẦN I: KHUNG HỆ THỐNG THIẾT KẾ (DESIGN SYSTEM FRAMEWORK)

### 1. Tổng quan & Triết lý thiết kế (Overview & Philosophy)
*   **Loại dự án (Project Type):** `[Web / Mobile App / Game 2D-3D]`
*   **Nền tảng mục tiêu (Target Platform):** `[Desktop Web / iOS & Android App / Cross-platform Game]`
*   **Creative North Star (Định hướng cốt lõi):** `"[Mô tả ngắn gọn triết lý thiết kế chính - VD: Minimalist, Cyberpunk, Playful, Clean & Corporate]"`
*   **Nguyên lý trải nghiệm người dùng (UX Principles):**
    *   `[Nguyên lý 1 - VD: Phản hồi cơ học rõ ràng, giảm thiểu tối đa độ trễ cảm giác]`
    *   `[Nguyên lý 2 - VD: Tối giản hóa các bước thao tác, quy tắc 3-click]`
    *   `[Nguyên lý 3 - VD: Tập trung vào nội dung chính, khoảng trắng là yếu tố dẫn đường]`

---

### 2. Hệ thống màu sắc & Độ tương phản (Color System & Contrast Rules)

#### 2.1. Color Tokens (Bảng màu hệ thống)

AI phải sử dụng chính xác các mã màu được quy định dưới đây, không tự ý phát sinh màu mới:

| Token | Giá trị (HEX/HSL/RGB) | Phạm vi sử dụng |
| :--- | :--- | :--- |
| **Primary (Brand)** | `[VD: #FFD23F]` | Các nút hành động chính (CTA), tiêu điểm cần thu hút chú ý |
| **Secondary** | `[VD: #4EA8DE]` | Các nút phụ, thẻ thông tin bổ trợ, danh mục |
| **Success** | `[VD: #4CAF50]` | Trạng thái hoàn thành, thông báo thành công, nút đồng ý |
| **Warning/Danger** | `[VD: #E63946]` | Cảnh báo lỗi, trạng thái nguy hiểm, nút hủy bỏ/xóa |
| **Background (Layer 0)** | `[VD: #1E1E24]` | Lớp nền dưới cùng của toàn bộ ứng dụng |
| **Surface Deep (Layer 1)**| `[VD: #2B2D42]` | Nền của panel chính, popup, form điền thông tin |
| **Surface Mid (Layer 2)** | `[VD: #3D405B]` | Nền của card con, list item, ô nhập liệu |
| **Text Primary** | `[VD: #F7F7F9]` | Chữ tiêu đề chính, thông số quan trọng |
| **Text Secondary** | `[VD: #A8DADC]` | Chữ mô tả phụ, nhãn chỉ dẫn, thời gian |
| **Border / Outline** | `[VD: #0F1016]` | Đường viền bao quanh các cấu phần UI (nếu có) |

#### 2.2. Quy tắc tương phản & Phân cấp bề mặt (Surface Hierarchy)
*   **Quy tắc tương phản (Contrast Rule):** `[Mô tả cách phối màu để chữ luôn đọc được rõ trên nền. VD: Chữ Text Primary chỉ được đặt trên nền Layer 1 hoặc Layer 2. Tỷ lệ tương phản tối thiểu đạt WCAG AA 4.5:1]`
*   **Phân cấp độ sâu (Surface Depth):**
    *   **Layer 0 (Background):** `[Mã màu]` - Nền tĩnh phía sau cùng.
    *   **Layer 1 (Card/Container):** `[Mã màu]` - Khung chứa lớn.
    *   **Layer 2 (Element/Interactive):** `[Mã màu]` - Các phần tử tương tác nằm trên Container.

---

### 3. Font chữ & Căn chỉnh (Typography & Alignment)

*   **Font Chữ Chính (Primary Font):** `[Tên Font - VD: Inter, Outfit, Roboto]`
*   **Font Chữ Phụ (Secondary Font - nếu có):** `[Tên Font - VD: JetBrains Mono (dùng cho code/thông số)]`
*   **Bảng tỷ lệ chữ (Type Scale):**

| Token | Kích thước (rem/px/pt) | Line Height | Weight | Sử dụng cho |
| :--- | :--- | :--- | :--- | :--- |
| **Heading 1 (H1)** | `[VD: 2.5rem / 40px]` | `[VD: 1.2]` | Bold | Tiêu đề lớn của trang/màn hình |
| **Heading 2 (H2)** | `[VD: 1.75rem / 28px]`| `[VD: 1.3]` | Bold | Tiêu đề nhóm, tiêu đề popup |
| **Body Large** | `[VD: 1.25rem / 20px]`| `[VD: 1.5]` | SemiBold | Chữ trên nút bấm chính, danh mục |
| **Body Normal** | `[VD: 1.0rem / 16px]` | `[VD: 1.5]` | Regular | Nội dung bài viết, dòng thông số |
| **Caption** | `[VD: 0.75rem / 12px]`| `[VD: 1.4]` | Regular | Chú thích nhỏ, ngày tháng, nhãn phụ |

*   **Quy tắc căn lề (Alignment Rule):**
    *   `[VD: Tiêu đề trang phải luôn căn giữa hoàn hảo. Chữ trong form nhập liệu luôn căn trái. Chữ trong nút bấm căn giữa tuyệt đối theo cả 2 chiều.]`

---

### 4. Hệ thống khoảng cách & Bo góc (Spacing & Unit System)

Dự án áp dụng hệ thống lưới `[8px hoặc 4px]` để tối ưu hóa khoảng cách, tránh tình trạng khoảng cách ngẫu nhiên.

*   **Bảng khoảng cách (Spacing Scale):**
    *   `xs` (Extra Small): `[VD: 4px]` - Khoảng cách cực nhỏ (nhãn đính kèm icon).
    *   `sm` (Small): `[VD: 8px]` - Padding trong nút bấm, khoảng cách giữa các dòng.
    *   `md` (Medium): `[VD: 16px]` - Padding tiêu chuẩn của Card, khoảng cách các khối thông tin.
    *   `lg` (Large): `[VD: 24px]` - Padding của Panel lớn, khoảng cách giữa các cột.
    *   `xl` (Extra Large): `[VD: 32px]` - Rìa ngoài của Popup, khoảng cách giữa các phần lớn của trang.
*   **Bo góc (Corner Radius Scale):**
    *   `Lớn (Popup/Modal/Panel)`: `[VD: 24px]`
    *   `Trung bình (Card/List item)`: `[VD: 12px]`
    *   `Nhỏ (Button/Input/Tab)`: `[VD: 8px]`
*   **Độ dày viền (Border Width Scale - nếu có):**
    *   `Khung viền lớn`: `[VD: 3px]`
    *   `Nút bấm/Input`: `[VD: 2px]`

---

### 5. Chiều sâu & Trạng thái tương tác (Elevation & States)

*   **Bóng đổ (Elevation/Shadows):**
    *   `Trạng thái tĩnh (Default)`: `[VD: Đổ bóng mờ nhẹ soft shadow hoặc bóng cứng retro offset-x: 4px, offset-y: 4px màu #000]`
    *   `Trạng thái nổi (Hovered/Focused)`: `[VD: Bóng đổ sâu hơn để tạo cảm giác nổi lên gần mắt hơn]`
*   **Quy chuẩn Trạng thái phần tử (Interactive States):**
    *   **Normal:** Trạng thái mặc định của phần tử.
    *   **Hover (Chỉ áp dụng Web/Desktop):** `[VD: Đổi màu nền sáng/tối đi 10% hoặc tăng bóng đổ]`
    *   **Active / Pressed (Click/Chạm):** `[VD: Giảm bóng đổ về 0, dịch chuyển phần tử xuống dưới-phải 2px để giả lập lực nhấn vật lý]`
    *   **Focus (Khi chọn vào input/nút):** `[VD: Xuất hiện outline sáng màu Primary xung quanh phần tử]`
    *   **Disabled (Vô hiệu hóa):** `[VD: Opacity giảm còn 50%, màu xám, không nhận sự kiện click]`

---

### 6. Thông số chi tiết các cấu phần UI (Component Specifications)

#### 6.1. Buttons (Nút bấm)
*   **Primary Button (Nút chính):** Nền màu `[Mã màu]`, chữ màu `[Mã màu]`, viền `[Độ dày]`.
*   **Secondary Button (Nút phụ):** Nền màu `[Mã màu]`, chữ màu `[Mã màu]`, viền `[Độ dày]`.
*   **Quy tắc Icon trong nút:** `[VD: Hạn chế dùng icon trong nút chính trừ khi đó là nút hành động đặc thù (VD: Lưu có icon Disk). Nếu dùng icon, khoảng cách giữa icon và text là 8px].`

#### 6.2. Text Inputs (Ô nhập liệu)
*   **Cấu trúc:** Nhãn trên đầu (Label) + Ô nhập liệu (Input Box) chứa Placeholder mờ + Nhãn lỗi bên dưới (Error Label).
*   **Kích thước chiều cao:** `[VD: Cố định 44px trên mobile, 40px trên web]`.

#### 6.3. Modals & Popups
*   **Overlay:** Lớp che phủ nền màu `[Mã màu - VD: Đen opacity 40%]` chặn tương tác phía sau.
*   **Cơ chế đóng:** Bắt buộc có nút đóng (X) góc trên bên phải và hỗ trợ đóng khi click ra vùng Overlay.

---

## PHẦN II: DANH MỤC 8 "BỆNH LÝ UI" CỦA AI VÀ PHƯƠNG PHÁP PHÒNG TRÁNH

> [!WARNING]
> Đây là các lỗi thiết kế giao diện kinh điển mà AI Agent thường mắc phải do thói quen sao chép thiết kế hào nhoáng nhưng phi thực tế. AI khi thực hiện code UI bắt buộc phải đọc và né tránh các lỗi này đối với từng nền tảng tương ứng.

### 1. Hội chứng "Nghiện" Glassmorphism & Icon Overuse (Lạm dụng mờ kính & Biểu tượng)
*   **Biểu hiện:** AI rất thích dùng hiệu ứng kính mờ (`backdrop-filter: blur`) và nhét icon vào mọi vị trí có thể (trước tiêu đề, trên tất cả các nút bấm, cạnh mọi dòng chữ) để tạo cảm giác "hiện đại".
*   **Tác hại:** Làm UI bị rối mắt, mất phân cấp thông tin, gây nặng hiệu năng render phần cứng (đặc biệt trên các thiết bị di động tầm trung).
*   **Quy tắc phòng tránh theo nền tảng:**
    *   **Web:** Chỉ dùng hiệu ứng blur cho thanh định hướng (sticky Navbar) hoặc Overlay của Modal lớn. Nút bấm phải ưu tiên hiển thị bằng Text rõ ràng, chỉ dùng icon khi icon đó mang tính toàn cầu (như bánh răng cho Cài đặt, thùng rác cho Xóa).
    *   **Mobile App:** Hạn chế tối đa backdrop-blur vì hiệu năng di động có hạn. Nút bấm trên thiết bị di động cần diện tích chạm lớn, chữ rõ ràng quan trọng hơn icon nhỏ khó nhấn.
    *   **Game UI:** Tránh lạm dụng kính mờ trừ khi game theo phong cách Sci-Fi. Các game casual, hoạt hình hoặc retro phải dùng các mảng màu đặc (Solid Surface) sắc nét.

### 2. Hội chứng "Loạn Đơn Vị" (Unit Chaos)
*   **Biểu hiện:** AI trộn lẫn lộn các đơn vị đo lường trong cùng một file CSS/Style (ví dụ: dùng cả `px`, `rem`, `em`, `%`, `vh`, `vw`, `pt` vô nguyên tắc cho padding, margin, font-size).
*   **Tác hại:** UI bị vỡ layout khi thay đổi kích thước màn hình hoặc zoom trình duyệt.
*   **Quy tắc phòng tránh theo nền tảng:**
    *   **Web:** 
        *   Font-size: Bắt buộc dùng `rem` (hoặc `em` cho các thành phần con phụ thuộc trực tiếp vào cha) để hỗ trợ tính năng zoom của người khiếm thị.
        *   Layout & Spacing: Dùng `rem` hoặc `px` cố định tuân thủ Spacing Scale (bội số của 8).
        *   Chiều rộng/cao khối lớn: Dùng `%`, `vw`, `vh` hoặc Flexbox/Grid.
    *   **Mobile App:** Bắt buộc dùng đơn vị đo chuẩn của framework (như `dp` trong Android, `points` trong iOS, hoặc các đơn vị không đơn vị trong React Native/Flutter). Tuyệt đối không hardcode đơn vị điểm ảnh vật lý (pixel).
    *   **Game UI (Unity UI Toolkit):** Dùng `px` cho các khoảng cách cố định nhỏ (padding, border), dùng `%` hoặc Flexbox cho việc co giãn tỷ lệ màn hình (Responsive).

### 3. Hội chứng "Màu Sắc Vô Hồn" (Color Chaos)
*   **Biểu hiện:** AI dùng các bảng màu gradient chói mắt từ Dribbble nhưng quên định nghĩa các màu chức năng cơ bản, dẫn đến việc dùng màu sắc không có ý nghĩa nhất quán.
*   **Tác hại:** Người dùng không nhận biết được đâu là hành động nguy hiểm (Xóa), đâu là hành động an toàn (Lưu), hoặc đâu là trạng thái thành công.
*   **Quy tắc phòng tránh theo nền tảng:**
    *   **Mọi nền tảng (Web, App, Game):** Phải định nghĩa rõ và tuân thủ bảng màu chức năng (Success = Xanh lá, Danger = Đỏ, Warning = Vàng/Cam, Info = Xanh dương). Màu sắc của nút tương tác chính (Primary) phải đồng nhất trên toàn bộ hệ thống, không đổi màu nút chính theo từng màn hình một cách ngẫu hứng.

### 4. Bệnh "Chột" Trạng Thái (Missing States Failure)
*   **Biểu hiện:** AI thiết kế các nút bấm và ô nhập liệu vô cùng đẹp mắt ở trạng thái tĩnh (Normal) nhưng bỏ qua việc lập trình các trạng thái Hover, Active, Focus, Disabled.
*   **Tác hại:** Người dùng click vào nút mà không có phản hồi thị giác, tạo cảm giác ứng dụng bị đơ, giật lag hoặc chất lượng kém.
*   **Quy tắc phòng tránh theo nền tảng:**
    *   **Web:** Nút bấm bắt buộc có đủ style cho `:hover` (chuột rà qua), `:active` (chuột nhấn xuống), `:focus` (phím tab di chuyển tới), và `:disabled`.
    *   **Mobile App:** Thiết lập trạng thái `pressed` (khi chạm tay vào nút nền tối đi hoặc lún xuống) và trạng thái `loading` (hiển thị spinner xoay tròn khi đang xử lý tác vụ gửi biểu mẫu).
    *   **Game UI:** Các nút tương tác bắt buộc phải có hiệu ứng cơ học rõ rệt (ví dụ: nút dịch chuyển xuống dưới-phải khi Pressed và bóng đổ co lại).

### 5. Bệnh "Mù" Tỷ Lệ Tương Phản (Contrast Failure)
*   **Biểu hiện:** AI thiết kế chữ màu xám nhạt trên nền trắng, chữ trắng trên nền vàng, hoặc chữ xám đậm trên nền đen chỉ vì nhìn nó "nghệ thuật".
*   **Tác hại:** Gây mỏi mắt cực độ cho người dùng thường và khiến người dùng bị suy giảm thị lực (hoặc khi dùng thiết bị ngoài trời nắng) không thể đọc được nội dung.
*   **Quy tắc phòng tránh theo nền tảng:**
    *   **Web & Mobile App:** Bắt buộc tuân thủ tiêu chuẩn tương phản WCAG 2.1 AA. Chữ thường phải đạt tỷ lệ tương phản tối thiểu `4.5:1` so với nền. Chữ lớn (>18pt Bold hoặc >24pt Regular) phải đạt tối thiểu `3:1`. AI phải tự tính toán độ tương phản của mã màu trước khi áp dụng.
    *   **Game UI:** Nếu dùng bảng màu rực rỡ khó đạt WCAG, bắt buộc phải bao quanh chữ/khối bằng nét viền tối màu (`Border Dark`) dày tối thiểu 2px để tăng độ tương phản hình học.

### 6. Hội chứng "Lười" Thiết Kế Ô Nhập Liệu (Input Neglect)
*   **Biểu hiện:** AI vẽ ô nhập liệu chỉ là một đường kẻ ngang hoặc một hộp trống thô kệch, không có nhãn chỉ dẫn (Label), placeholder biến mất không dấu vết khi gõ chữ, hoặc không có hiển thị báo lỗi cụ thể.
*   **Tác hại:** Người dùng không biết mình đang nhập trường thông tin gì khi gõ dở dang, hoặc hoang mang khi bấm gửi mà không biết ô nào bị lỗi.
*   **Quy tắc phòng tránh theo nền tảng:**
    *   **Mọi nền tảng (Web, App, Game):** Ô nhập liệu chuẩn phải có cấu trúc 3 phần: Label tĩnh phía trên (không dùng Placeholder thay thế Label hoàn toàn), Input Box có viền phân biệt trạng thái Focus, và Helper/Error Text nằm cố định bên dưới để hiển thị cảnh báo lỗi (ví dụ: "Email không hợp lệ").

### 7. Bệnh "Tràn Viền & Cụt Chữ" (Layout Cutoff)
*   **Biểu hiện:** AI thiết kế nút bấm vừa khít với chữ ở ngôn ngữ mặc định (ví dụ: tiếng Anh "Reset"), nhưng khi chuyển sang ngôn ngữ dài hơn (tiếng Việt "Làm mới") thì chữ bị tràn khỏi viền hoặc bị cắt cụt thành "Làm...".
*   **Tác hại:** Phá vỡ tính thẩm mỹ và làm mất chức năng của nút bấm.
*   **Quy tắc phòng tránh theo nền tảng:**
    *   **Mọi nền tảng (Web, App, Game):** Luôn áp dụng quy tắc khoảng trống an toàn (Safety Margin). Chiều rộng của nút bấm/input phải có khoảng đệm tối thiểu bằng 20% chiều rộng của chữ hiển thị bên trong. Không bao giờ hardcode chiều rộng cố định (`width`) cho các phần tử chứa chữ biến đổi mà nên sử dụng `min-width` kết hợp với `padding-left/right` hợp lý.

### 8. Hội chứng "Phân Cấp Thị Giác Lộn Xộn" (Visual Hierarchy Mess)
*   **Biểu hiện:** Tiêu đề chính (H1) và tiêu đề phụ (H2) có kích thước gần bằng nhau. Nút bấm hủy bỏ (Secondary) lại được tô màu đỏ rực rỡ hơn nút xác nhận chính (Primary).
*   **Tác hại:** Người dùng bị bối rối không biết đâu là thông tin trọng tâm và dễ bấm nhầm các hành động hủy hoại dữ liệu.
*   **Quy tắc phòng tránh theo nền tảng:**
    *   **Mọi nền tảng (Web, App, Game):** Áp dụng quy tắc kích thước chữ giảm dần rõ rệt (VD: H1 gấp 1.5 lần H2, H2 gấp 1.4 lần Body). Nút bấm chính phải có trọng lượng thị giác lớn nhất (màu đậm nhất, diện tích lớn nhất), nút bấm phụ phải có màu chìm hơn hoặc dạng viền không nền (Outline Button).

---

## PHẦN III: QUY TRÌNH TỰ KIỂM TRA (AI AGENT SELF-CHECK PROTOCOL)

*AI Agent bắt buộc phải chạy checklist 15 câu hỏi này trước khi bàn giao mã nguồn UI cho người dùng:*

1.  [ ] **Màu sắc hệ thống:** Toàn bộ màu sắc sử dụng có trùng khớp với danh sách Color Tokens quy định tại Mục 2.1 không?
2.  [ ] **Độ tương phản:** Chữ trên nền đã đạt độ tương phản tối thiểu (WCAG AA 4.5:1 đối với Web/App, hoặc có viền Dark Outline đối với Game) chưa?
3.  [ ] **Tính nhất quán của Font:** Có sử dụng đúng các font chữ và tỷ lệ quy định tại hệ thống Typography không? Có bị sót font mặc định hệ điều hành không?
4.  [ ] **Quy tắc Spacing:** Toàn bộ khoảng cách margin, padding có phải là bội số của hệ số lưới (8px/4px) quy định tại Mục 4 không?
5.  [ ] **Khoảng đệm chữ (Safety Margin):** Các văn bản dài (như "Làm mới", "Xác nhận") có bị khuyết nét, xuống dòng lỗi hay tràn ra ngoài khung chứa không?
6.  [ ] **Trạng thái tương tác (Interactive States):** Tất cả các nút bấm, tab và ô nhập liệu đã có đủ style Hover, Active/Pressed và Focus chưa?
7.  [ ] **Phản hồi vật lý (Mechanical Feedback):** Các nút bấm trong Game/App có hiệu ứng lún xuống hoặc thay đổi độ sâu bóng đổ rõ rệt khi nhấn không?
8.  [ ] **Giới hạn Glassmorphism:** Đã kiểm tra và đảm bảo không lạm dụng hiệu ứng kính mờ (blur) gây giảm hiệu năng phần cứng chưa?
9.  [ ] **Tối giản hóa Icon:** Đã loại bỏ các icon thừa thãi không mang lại giá trị nhận diện nhanh chưa? Các nút chức năng chính có chữ ghi nhãn rõ ràng chưa?
10. [ ] **Co giãn Responsive:** Đã kiểm tra hiển thị giao diện trên các tỷ lệ màn hình khác nhau (Landscape, Portrait, 16:9, 4:3) chưa? Có bị vỡ layout không?
11. [ ] **Đóng mở Popup:** Tất cả các bảng thông báo, popup đã có nút đóng (X) rõ ràng và cơ chế click ra ngoài overlay để đóng chưa?
12. [ ] **Cấu trúc Input:** Ô nhập liệu đã có đủ nhãn (Label) cố định phía trên và khu vực báo lỗi (Error Text) phía dưới chưa?
13. [ ] **Đơn vị đo lường:** Đã loại bỏ hoàn toàn việc trộn lẫn đơn vị đo lường bừa bãi chưa (ví dụ: chỉ dùng rem/px nhất quán cho Web)?
14. [ ] **Phân cấp thị giác:** Nút hành động chính (Primary) có nổi bật hơn nút hành động phụ (Secondary) không?
15. [ ] **Dọn dẹp tài nguyên:** Trong mã nguồn điều khiển UI, toàn bộ sự kiện click/chạm đã được hủy đăng ký (unregister) khi đóng UI để tránh rò rỉ bộ nhớ chưa?
