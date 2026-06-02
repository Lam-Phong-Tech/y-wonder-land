# DESIGN SYSTEM: THE PLAYFUL HOMESTEAD FRAMEWORK
*(Hệ thống thiết kế giao diện chính thức cho dự án Bà Chúa Khu Rừng 3D - Y WONDER LAND)*

---

# [ENGLISH VERSION]

> [!IMPORTANT]
> **AI AGENT INSTRUCTIONS (Must read before writing any UI layout or C# controllers):**
> 1. All UI screens in this project (UXML, USS, C# Controllers) must comply strictly with the design system specifications outlined in Part I.
> 2. You must study and avoid the 8 AI UI diseases detailed in Part II.
> 3. You must execute the 15-question Self-Check Protocol in Part III before submitting any UI work.

---

## THE PLAYFUL HOMESTEAD - DESIGN SPECIFICATIONS

### PART I: DESIGN SYSTEM FRAMEWORK

#### 1. Overview & Creative North Star
*   **Project Type:** 3D Farming & Life Simulation Game (Casual / Cozy)
*   **Target Platform:** PC (Steam/Web) & Mobile (Landscape layout)
*   **Creative North Star:** **"The Tangible Playground"**
    *   *Concept:* The interface should feel like physical plastic or wooden toy puzzle pieces placed on the green grass of the 3D world.
    *   *Tactile Interactions:* Every interaction must feel decisive. Buttons must visually press down mechanically, and surfaces must feel solid and graspable.
    *   *Separation:* The UI is completely separated from the 3D world below using solid color blocks, thick borders, and high contrast.

#### 2. Color System & Contrast Rules

##### 2.1. Color Tokens
All components must use these exact color values to maintain visual unity:

| Token | Value | Usage |
| :--- | :--- | :--- |
| **Primary (Yellow)** | `#FFC107` | Primary action buttons (Buy, Confirm, Plant) |
| **Secondary (Blue)** | `#2D7BFF` | Support buttons, category tabs, info cards |
| **Success (Green)** | `#4CAF50` | Accept invitations, positive achievements |
| **Danger (Red)** | `#FF4B4B` | Delete actions, Close buttons (X), Exit game |
| **Neutral Surface** | `#F5F0E8` | Base background color for generic panels (Settings, Inventory) |
| **Hero Surface** | `#5B42F3` | Competitive/Achievement panel background (Leaderboard) |
| **Card Background** | `#FFFFFF` | Inner cards, item slots, player rank rows |
| **Text Primary** | `#3D3535` | Main text titles, headings, and labels |
| **Text Secondary** | `#8A7E6E` | Explanatory subtexts, offline status, stats description |
| **Border Dark** | `#3D3535` | 2px-3px thick outlines enclosing panels, cards, and buttons |

##### 2.2. Contrast Rules & Surface Hierarchy
*   **Contrast Rule:** To ensure high readability over 3D background elements, all panels, buttons, and cards MUST have a thick `#3D3535` dark border.
*   **Surface Depth Hierarchy:**
    *   **Layer 0 (Game World):** 3D gameplay scene.
    *   **Layer 1 (Overlay):** Dimming screen (`rgba(0, 0, 0, 0.4)`) blocking 3D inputs.
    *   **Layer 2 (Panels):** Main panels (rounded corners 22px-24px, `#F5F0E8` or `#5B42F3`).
    *   **Layer 3 (Cards/Elements):** Cards inside scroll views (rounded corners 14px-16px, `#FFFFFF`).

#### 3. Typography & Alignment
*   **Primary Font:** Sans-serif (specifically Bold weight typography definition loaded from Resources). No Serif fonts allowed.
*   **Type Scale:**
    *   **H1 (Titles):** `40px` (Bold, Centered, used for panel headers like "LEADERBOARD", "INVENTORY").
    *   **H2 (Sub-headers):** `28px` (Bold, Left-aligned or Centered, used for popups/sections).
    *   **Body Large:** `20px` (SemiBold/Bold, Centered on button labels, name text).
    *   **Body Normal:** `16px` (Regular/Medium, list details, tooltips).
    *   **Caption:** `12px` (Regular, timestamps, secondary parameters).

#### 4. Spacing, Borders & Unit System
We strictly enforce a **multiples of 8px** (or 4px micro-spacing) spacing system to eliminate layout chaos:
*   **Spacing Scale:**
    *   `xs`: `4px` - Icon-to-text gap.
    *   `sm`: `8px` - Row spacing, tight padding.
    *   `md`: `16px` - Card padding, default gap between lists.
    *   `lg`: `24px` - Main panel inner padding.
    *   `xl`: `32px` - Outer margin boundaries of popup boxes.
*   **Corner Radius Scale:**
    *   `Panels / Modals`: `22px` to `24px` rounded corners.
    *   `Cards / Items`: `14px` to `16px` rounded corners.
    *   `Buttons / Input boxes`: `12px` rounded corners.
*   **Border Thickness:**
    *   `Main panels`: `3px` solid `#3D3535`.
    *   `Buttons / Cards / Tabs`: `2px` to `3px` solid `#3D3535`.

#### 5. Elevation, Depth & Interactive States
*   **The "Retro Offset" Shadow (Rigid Shadow):** Do not use soft, blurred shadows. All Panels and Buttons must use a Solid Drop Shadow (0px Blur).
    *   *Formula:* Shift shadow offset-x: `4px` (or `6px` for large panels) and offset-y: `4px` (or `6px`). Color must be `#3D3535`.
*   **Mechanical Pressed State:**
    *   *Normal state:* Elevated with shadow.
    *   *Hover state:* Slightly lighten background tint color.
    *   *Pressed state:* Button element shifts down-right by `4px` (`translate: 4px 4px`), and shadow shrinks to `0px` offset, creating a physical mechanical click feeling.
    *   *Disabled state:* Muted gray background, opacity `50%`, no click interactions.

#### 6. Component Specifications

##### 6.1. Buttons
*   Labels must be centered horizontally and vertically.
*   Minimize icon usage inside buttons; prioritize clear text buttons (e.g. "Kết bạn", "Xóa bạn") rather than raw icons.

##### 6.2. Text Inputs
*   Background `#FFFFFF` or `#E5DEC9`, border `#3D3535` 2px. Focus state shifts border color to Primary Yellow or secondary bright colors. Include a static top label.

##### 6.3. Modals & Popups
*   Must contain a clear close button (X) in the top-right corner, colored Warning Red (`#FF4B4B`). Dismissal should be triggered on clicking the dimming overlay background.

---

### PART II: 8 "AI UI DISEASES" & PREVENTION IN Y WONDER LAND

#### 1. The "Glassmorphism & Icon Overuse" Addiction
*   **In this project:** Do not use `backdrop-filter: blur` on any popup. All backgrounds must be solid (`#F5F0E8` or `#5B42F3`). Minimise icon usage; buttons must rely on Vietnamese text tags (e.g. "Đồng ý", "Xóa").

#### 2. Unit Chaos (Mixed Metric Units)
*   **In this project:** Unity UI Toolkit has strict layouts. You must use `px` for borders, corner-radius, and font-sizes. Use `%` or flex properties for responsive container dimensions. Never mix unrelated styling units.

#### 3. Color Chaos (Functional Color Neglect)
*   **In this project:** Stick to the specified Y WONDER LAND palette. Primary action buttons are Yellow (`#FFC107`). Danger buttons are Red (`#FF4B4B`). Do not introduce random gradients.

#### 4. Missing States Failure (Static UI Syndrome)
*   **In this project:** All buttons in UXML/USS must implement `:hover`, `:active` (pressed state with `translate`), and `:disabled` styles. Writing static buttons is considered a layout bug.

#### 5. Contrast Failure (Low Contrast Text)
*   **In this project:** Text must use `#3D3535` for maximum readability on white or light gray cards. Outline panels and containers with a `#3D3535` border.

#### 6. Text Input Neglect
*   **In this project:** Search fields (e.g., in Friends Popup) must have a clean placeholder, static labels, and clear focused borders.

#### 7. Layout Cutoff (Text Overrun)
*   **In this project:** Always design buttons with a safety margin (padding-left/right at least 12px-20px). Ensure Vietnamese words like "Làm mới" or "Thống kê" do not get truncated or wrapped awkwardly.

#### 8. Visual Hierarchy Mess
*   **In this project:** Follow the defined Type Scale. Headers must be prominent (`40px`), and primary cards must stand out clearly over secondary controls.

---

### PART III: AI AGENT SELF-CHECK PROTOCOL

*Before submitting any UI changes, you must satisfy this 15-question checklist:*

1.  [ ] Are all colors exactly mapped to Section 2.1 Color Tokens?
2.  [ ] Do all panels, buttons, and card elements have dark borders (`#3D3535`) to ensure contrast?
3.  [ ] Are fonts loaded from project Resources and set to Sans-serif Bold/Medium?
4.  [ ] Is every padding, margin, and gap value a multiple of 8px (or 4px)?
5.  [ ] Have you tested buttons with long Vietnamese labels to avoid truncation (e.g. "Làm mới")?
6.  [ ] Have you written full `:hover`, `:active`, and `:disabled` styles for every interactive element?
7.  [ ] Do buttons have a mechanical click feel (shifts X/Y offset, shadow goes to 0)?
8.  [ ] Are there any backdrop-blurs or glassmorphism effects? (Should be 0).
9.  [ ] Have you replaced unnecessary icons with clear text labels?
10. [ ] Does the UI scale correctly under different screen ratios (PC 16:9, Mobile Landscape)?
11. [ ] Is there an overlay dismiss script and a clear red X close button?
12. [ ] Are inputs equipped with static labels and proper focus highlights?
13. [ ] Did you avoid hardcoding fixed widths for text elements (use `min-width` and `padding`)?
14. [ ] Does the primary yellow button look visually dominant?
15. [ ] Did you unregister all UI toolkit event callbacks in C# to prevent memory leaks?

---
---

# [BẢN TIẾNG VIỆT]

> [!IMPORTANT]
> **HƯỚNG DẪN DÀNH CHO AI AGENT (Đọc kỹ trước khi tạo file thiết kế cụ thể):**
> 1. Tất cả các màn hình giao diện trong dự án này (UXML, USS, C# Controllers) phải tuân thủ nghiêm ngặt hệ thống thiết kế được quy định tại Phần I.
> 2. AI phải nghiên cứu và né tránh tuyệt đối 8 bệnh lý UI được nêu chi tiết ở Phần II.
> 3. AI phải thực hiện bộ 15 câu hỏi tự kiểm tra tại Phần III trước khi bàn giao công việc UI.

---

## THE PLAYFUL HOMESTEAD - THÔNG SỐ THIẾT KẾ CHI TIẾT

### PHẦN I: KHUNG HỆ THỐNG THIẾT KẾ

#### 1. Tổng quan & Triết lý thiết kế
*   **Loại dự án:** Game 3D mô phỏng Nông trại & Đời sống (Casual / Cozy)
*   **Nền tảng mục tiêu:** PC (Steam/Web) & Thiết bị di động (Nằm ngang - Landscape)
*   **Creative North Star:** **"The Tangible Playground" (Sân chơi Hữu hình)**
    *   *Khái niệm:* Giao diện tạo cảm giác như các mảnh ghép đồ chơi bằng nhựa hoặc gỗ được đặt lên thảm cỏ xanh của thế giới 3D.
    *   *Tính vật lý:* Mọi tương tác phải dứt khoát. Nút bấm phải lún xuống cơ học khi nhấn, các mảng khối tạo cảm giác sờ nắm được.
    *   *Sự tách biệt:* UI tách biệt hoàn toàn khỏi thế giới 3D bên dưới bằng các khối màu đặc, viền đậm và độ tương phản cao để tránh rối mắt.

#### 2. Hệ thống màu sắc & Độ tương phản

##### 2.1. Color Tokens
Tất cả các cấu phần UI phải sử dụng đúng các mã màu sau đây để đảm bảo tính nhất quán:

| Token | Giá trị | Phạm vi sử dụng |
| :--- | :--- | :--- |
| **Primary (Vàng)** | `#FFC107` | Các nút hành động chính (Mua, Xác nhận, Gieo hạt) |
| **Secondary (Xanh)** | `#2D7BFF` | Các nút phụ trợ, tab phân loại, thẻ thông tin phụ |
| **Success (Xanh lá)** | `#4CAF50` | Trạng thái đồng ý kết bạn, thành tựu tích cực |
| **Danger (Đỏ)** | `#FF4B4B` | Các hành động xóa bỏ, nút đóng (X), thoát game |
| **Neutral Surface** | `#F5F0E8` | Màu nền chính cho các panel thông thường (Cài đặt, Túi đồ) |
| **Hero Surface** | `#5B42F3` | Màu nền cho panel ganh đua/thành tích (Bảng xếp hạng) |
| **Card Background** | `#FFFFFF` | Nền các thẻ con, ô chứa vật phẩm, dòng xếp hạng |
| **Text Primary** | `#3D3535` | Chữ tiêu đề chính, nhãn chữ lớn |
| **Text Secondary** | `#8A7E6E` | Chú thích phụ, trạng thái offline, thông số nhỏ |
| **Border Dark** | `#3D3535` | Đường viền dày 2px-3px bao quanh toàn bộ panel, card, button |

##### 2.2. Quy tắc tương phản & Phân cấp bề mặt
*   **Quy tắc tương phản:** Để chữ và các khối UI không bị lẫn vào thế giới 3D bên dưới, tất cả các panel, button, card BẮT BUỘC phải có nét viền tối màu `#3D3535` bao quanh.
*   **Phân cấp độ sâu:**
    *   **Layer 0 (Game World):** Môi trường game 3D.
    *   **Layer 1 (Overlay):** Màn mờ đen (`rgba(0, 0, 0, 0.4)`) chặn click chuột xuống thế giới 3D.
    *   **Layer 2 (Panels):** Các panel chính (bo góc 22px-24px, màu `#F5F0E8` hoặc `#5B42F3`).
    *   **Layer 3 (Cards/Elements):** Các thẻ con nằm trong ScrollView (bo góc 14px-16px, màu `#FFFFFF`).

#### 3. Font chữ & Căn lề
*   **Font chữ chính:** Họ phông Sans-serif (được tải từ Resources dạng phông nét đậm). Không dùng phông có chân (Serif).
*   **Type Scale (Tỷ lệ chữ):**
    *   **H1 (Tiêu đề chính):** `40px` (Bold, Căn giữa, dùng cho Header của các Panel lớn).
    *   **H2 (Tiêu đề phụ):** `28px` (Bold, Căn lề trái/giữa, dùng cho popup con).
    *   **Body Large:** `20px` (SemiBold/Bold, chữ trên nút bấm, tên nhân vật).
    *   **Body Normal:** `16px` (Regular/Medium, nội dung chi tiết, tooltip).
    *   **Caption:** `12px` (Regular, thời gian, chú thích thông số phụ).

#### 4. Hệ thống khoảng cách & Bo góc
Dự án áp dụng hệ thống lưới **bội số của 8px** (hoặc 4px đối với chi tiết siêu nhỏ):
*   **Bảng khoảng cách (Spacing Scale):**
    *   `xs`: `4px` - Khoảng cách chữ và icon.
    *   `sm`: `8px` - Khoảng cách dòng, khoảng đệm hẹp.
    *   `md`: `16px` - Padding của Card, khoảng cách mặc định giữa các phần tử.
    *   `lg`: `24px` - Padding bên trong của Panel lớn.
    *   `xl`: `32px` - Rìa ngoài của Popup lớn.
*   **Bo góc (Corner Radius Scale):**
    *   `Panels / Modals`: Bo góc từ `22px` đến `24px`.
    *   `Cards / Items`: Bo góc từ `14px` đến `16px`.
    *   `Buttons / Ô nhập liệu`: Bo góc `12px`.
*   **Độ dày viền:**
    *   `Panel lớn`: `3px` solid `#3D3535`.
    *   `Nút / Card / Tab`: `2px` đến `3px` solid `#3D3535`.

#### 5. Chiều sâu & Trạng thái tương tác
*   **Bóng đổ khối cứng (Retro Offset Shadow):** Nghiêm cấm dùng bóng nhòe. Toàn bộ Panel và Nút phải dùng bóng đổ phẳng cứng (0px Blur).
    *   *Công thức:* Đổ bóng lệch xuống dưới `4px` (hoặc `6px` cho panel lớn) và sang phải `4px` (hoặc `6px`). Màu bóng đổ là `#3D3535`.
*   **Trạng thái cơ học khi nhấn (Pressed State):**
    *   *Normal state:* Nút nổi lên có bóng đổ rõ rệt.
    *   *Hover state:* Màu nền nút sáng lên một chút.
    *   *Pressed state:* Nút dịch chuyển xuống dưới-phải `4px` (`translate: 4px 4px`), bóng đổ co lại bằng `0px`, tạo phản hồi nhấn vật lý rõ nét.
    *   *Disabled state:* Nền xám, độ mờ 50%, không tương tác.

#### 6. Thông số chi tiết cấu phần UI

##### 6.1. Buttons (Nút bấm)
*   Chữ trên nút bắt buộc căn giữa hoàn toàn theo cả 2 chiều dọc và ngang.
*   Hạn chế tối đa dùng icon trong nút, ưu tiên dùng chữ tiếng Việt rõ nghĩa (VD: "Kết bạn", "Xóa bạn").

##### 6.2. Text Inputs (Ô nhập liệu)
*   Nền `#FFFFFF` hoặc `#E5DEC9`, viền `#3D3535` 2px. Trạng thái focus viền chuyển sang màu Vàng hoặc màu tươi sáng. Phải có nhãn tĩnh phía trên.

##### 6.3. Modals & Popups
*   Luôn chứa nút đóng (X) màu đỏ cảnh báo (`#FF4B4B`) ở góc trên bên phải. Tự động đóng popup khi bấm vào lớp overlay mờ phía sau.

---

### PHẦN II: 8 "BỆNH LÝ UI" CỦA AI & PHƯƠNG PHÁP PHÒNG TRÁNH TRONG Y WONDER LAND

#### 1. Hội chứng "Nghiện" Glassmorphism & Icon Overuse
*   *Trong dự án:* Tuyệt đối không dùng hiệu ứng mờ kính (blur) trên popup. Màu nền phải là màu phẳng (`#F5F0E8` hoặc `#5B42F3`). Hạn chế dùng icon; các nút hành động phải ghi rõ chữ tiếng Việt rõ ràng.

#### 2. Hội chứng "Loạn Đơn Vị" (Unit Chaos)
*   *Trong dự án:* Unity UI Toolkit yêu cầu thiết lập layout chặt chẽ. Phải dùng `px` cho bo góc, độ dày viền và kích thước chữ. Dùng `%` hoặc các thuộc tính flex cho chiều ngang/dọc của các container co giãn. Tuyệt đối không trộn lẫn đơn vị bừa bãi.

#### 3. Hội chứng "Màu Sắc Vô Hồn" (Color Chaos)
*   *Trong dự án:* Sử dụng đúng bảng màu đã quy định của game Y WONDER LAND. Nút chính bắt buộc màu Vàng (`#FFC107`). Nút đóng/hủy màu Đỏ (`#FF4B4B`). Không tự chế dải màu gradient ngẫu nhiên.

#### 4. Bệnh "Chột" Trạng Thái (Missing States Failure)
*   *Trong dự án:* Tất cả nút tương tác trong UXML/USS phải viết đầy đủ style cho `:hover`, `:active` (có dịch chuyển vị trí translate) và `:disabled`.

#### 5. Bệnh "Mù" Tỷ Lệ Tương Phản (Contrast Failure)
*   *Trong dự án:* Chữ hiển thị phải sử dụng màu `#3D3535` trên nền trắng hoặc nền kem nhạt để mắt dễ đọc nhất. Bao quanh các panel lớn bằng viền `#3D3535` đậm nét.

#### 6. Hội chứng "Lười" Thiết Kế Ô Nhập Liệu (Input Neglect)
*   *Trong dự án:* Ô tìm kiếm (VD: ở popup Bạn bè) phải có placeholder rõ ràng, nhãn tĩnh phía trên và viền thay đổi màu sắc nổi bật khi chọn vào (Focus).

#### 7. Bệnh "Tràn Viền & Cụt Chữ" (Layout Cutoff)
*   *Trong dự án:* Luôn chừa khoảng đệm an toàn (padding-left/right từ 12px đến 20px). Đảm bảo các từ tiếng Việt như "Làm mới", "Cài đặt" không bị rớt dòng lỗi hoặc bị che khuất mất nét chữ.

#### 8. Hội chứng "Phân Cấp Thị Giác Lộn Xộn" (Visual Hierarchy Mess)
*   *Trong dự án:* Sử dụng đúng bảng tỷ lệ chữ (H1 là `40px`, H2 là `28px`). Các thẻ danh sách quan trọng phải nằm ở vị trí trung tâm, màu sắc bắt mắt hơn các nút tùy chọn phụ.

---

### PHẦN III: QUY TRÌNH TỰ KIỂM TRA (AI AGENT SELF-CHECK PROTOCOL)

*Trước khi bàn giao bất kỳ thay đổi UI nào, AI bắt buộc phải trả lời 15 câu hỏi checklist sau:*

1.  [ ] Toàn bộ màu sắc có khớp chính xác với Color Tokens quy định tại Mục 2.1 không?
2.  [ ] Các panel, button, card đã có nét viền tối (`#3D3535`) để giữ độ tương phản tốt chưa?
3.  [ ] Font chữ có sử dụng đúng font Sans-serif nét đậm/trung bình được tải từ Resources không?
4.  [ ] Khoảng cách padding, margin, gap có tuân thủ quy tắc bội số của 8px (hoặc 4px) không?
5.  [ ] Chữ tiếng Việt dài như "Làm mới" đã được kiểm thử co giãn và không bị lỗi tràn viền hay cụt chữ chưa?
6.  [ ] Đã cấu hình đủ trạng thái `:hover`, `:active` và `:disabled` trong file USS cho các nút chưa?
7.  [ ] Nút bấm đã có hiệu ứng nhấn cơ học (dịch chuyển X/Y và bóng co về 0px) chưa?
8.  [ ] Giao diện có sử dụng hiệu ứng blur hay glassmorphism trái quy định không? (Phải là không).
9.  [ ] Các nút bấm đã được giản lược icon và thay thế bằng nhãn chữ rõ ràng chưa?
10. [ ] Giao diện có co giãn tốt khi chuyển tỷ lệ màn hình (PC 16:9, Mobile Landscape) không?
11. [ ] Đã có cơ chế click ra ngoài overlay mờ và nút đóng X màu đỏ để tắt popup an toàn chưa?
12. [ ] Ô nhập liệu đã có nhãn tĩnh phía trên và hiệu ứng đổi màu viền khi focus chưa?
13. [ ] Đã loại bỏ việc set `width` cố định cho các ô chữ (dùng `min-width` và `padding` thay thế) chưa?
14. [ ] Nút bấm màu vàng (Primary) có được thiết kế nổi bật nhất trên màn hình chưa?
15. [ ] Tất cả các callback đăng ký sự kiện trong C# đã được unregister khi tắt giao diện để tránh rò rỉ bộ nhớ chưa?
