# Design System Template & AI UI Guidelines

> **Note:** Tài liệu này là template chuẩn thiết kế giao diện cho các dự án game, được tối ưu hóa đặc biệt dành cho AI Agent thiết kế. Nó tích hợp các bài học xương máu từ báo cáo *"Các bệnh lý giao diện của AI Agent"* và trải nghiệm thực tế từ dự án **Bà Chúa Khu Rừng 3D (Y WONDER LAND)**.

---

## 1. Overview & Creative North Star

**Creative North Star: "[Tên Triết Lý Thiết Kế Của Dự Án - VD: The Tangible Playground]"**
*   **Mục tiêu:** Mang lại cảm giác giao diện có thể "chạm", "cầm nắm" và tương tác vật lý trực quan như đồ chơi trẻ em, kích thích sự tò mò.
*   **Triết lý cốt lõi:**
    *   **Thân thiện & Vui nhộn:** Sử dụng các đường bo góc lớn, nét viền dày tối màu (Comic-like) và màu sắc tương phản rõ ràng.
    *   **Trực quan & Tối giản:** Cắt giảm tối đa các trang trí thừa, không lạm dụng hiệu ứng lung linh giả tạo hay các biểu tượng (icon) không cần thiết.
    *   **Rõ ràng về Trạng thái (Feedback):** Mọi tương tác (Hover, Pressed, Disabled) đều phải có sự thay đổi rõ rệt bằng hoạt ảnh hoặc dịch chuyển hình học (ví dụ: nút lún xuống khi bấm).

---

## 2. Color & Surface

### 2.1. The Color Tokens (Bảng màu hệ thống)

AI tuyệt đối không được tự ý sinh mã màu ngẫu nhiên ngoài danh sách này:

| Token | Giá trị HEX | Sử dụng cho |
| :--- | :--- | :--- |
| **Primary (Brand)** | `#FFD23F` | Các nút hành động chính (CTA), tiêu điểm quan trọng |
| **Secondary** | `#4EA8DE` | Các nút phụ, thông tin bổ trợ |
| **Background (Layer 0)** | `#1E1E24` | Nền tối phía sau cùng của toàn bộ màn hình |
| **Surface Deep (Layer 1)** | `#2B2D42` | Nền của các Panel chính, khung chứa popup |
| **Surface Mid (Layer 2)** | `#3D405B` | Nền của Card con, các item trong danh sách |
| **Text Primary** | `#F7F7F9` | Chữ chính, tiêu đề |
| **Text Secondary** | `#A8DADC` | Chữ phụ, chú thích, thông số |
| **Border Dark** | `#0F1016` | Nét viền bao quanh toàn bộ các element UI (Độ dày 3px-4px) |
| **Success** | `#4CAF50` | Trạng thái hoàn thành, đồng ý |
| **Warning/Danger** | `#E63946` | Trạng thái cảnh báo, hủy bỏ, lỗi |

### 2.2. Phối màu & Phân cấp bề mặt (Surface Hierarchy)

*   **Quy tắc phối màu (The Contrast Rule):** Tất cả các thành phần tương tác (nút, input) bắt buộc phải có nét viền `Border Dark` để tách biệt rõ ràng khỏi lớp nền, tránh bị chìm màu.
*   **Phân cấp độ sâu lớp nền:**
    *   **Layer 0 (Screen BG):** `#1E1E24` (Độ tương phản thấp nhất)
    *   **Layer 1 (Panel Popup):** `#2B2D42` với viền `Border Dark` 4px và góc bo `32px`.
    *   **Layer 2 (Card/List Item):** `#3D405B` với viền `Border Dark` 3px và góc bo `16px`.
    *   *Quy tắc:* Lớp càng nổi lên trên thì màu càng sáng dần lên để hướng mắt người dùng tốt hơn.

---

## 3. Typography

*   **Font Family:** Chỉ sử dụng duy nhất font chính của game (ví dụ: `font-definition: resource("Fonts/Inter-Bold")` hoặc font tương đương có thuộc tính Bold rõ ràng). Không trộn lẫn nhiều font.
*   **Bảng tỷ lệ chữ (Type Scale):**

| Token | Cỡ chữ (PX) | Độ dày (Weight) | Sử dụng cho |
| :--- | :--- | :--- | :--- |
| **H1 (Headline)** | `40px` | Bold | Tiêu đề lớn (Leaderboard, Friends) |
| **H2 (Sub-headline)**| `28px` | Bold | Tiêu đề popup nhỏ, danh mục |
| **Body Large** | `20px` | Medium/Bold | Nội dung nút bấm chính, tên người dùng |
| **Body Normal** | `16px` | Regular/Medium | Chi tiết dòng, mô tả chỉ số |
| **Caption** | `12px` | Regular | Thời gian, trạng thái offline, ghi chú nhỏ |

*   **Quy tắc căn lề chữ:**
    *   Tiêu đề chính phải được căn giữa hoàn hảo. Nếu có icon trang trí bên cạnh, phần padding của hộp tiêu đề phải được bù trừ hợp lý để chữ không bị lệch sang bên (VD: không được để padding-left cố định khi ẩn icon).
    *   Nút bấm phải có chữ căn giữa theo cả 2 chiều dọc và ngang.

---

## 4. Spacing & Units System (Hệ thống khoảng cách bội số của 8)

Để tránh **Bệnh loạn đơn vị (Unit Chaos)**, toàn bộ kích thước, khoảng cách và viền phải tuân theo hệ số nhân của 8px (hoặc tối thiểu là 4px trong các chi tiết cực nhỏ):

*   **Padding / Margin Scale:**
    *   `xs`: `4px` (Khoảng cách giữa icon và text rất gần)
    *   `sm`: `8px` (Padding trong card nhỏ, khoảng cách dòng)
    *   `md`: `16px` (Padding tiêu chuẩn của Card, khoảng cách giữa các phần tử)
    *   `lg`: `24px` (Padding của Panel lớn, khoảng cách giữa các cụm nút)
    *   `xl`: `32px` (Khoảng cách rìa ngoài của Popup lớn)
*   **Border Radius Scale (Bo góc vật lý):**
    *   `Panel/Popup`: `32px`
    *   `Card/Item/Tabs`: `16px`
    *   `Button/Input`: `12px`
    *   `Avatar/Icon`: `8px`
*   **Border Width Scale (Độ dày nét vẽ):**
    *   `Panel lớn`: `4px`
    *   `Card/Nút/Input`: `3px`
    *   `Chi tiết nhỏ`: `2px`

---

## 5. Elevation & Depth (Mô phỏng 3D Vật lý)

Không dùng đổ bóng nhòe (soft shadows) kiểu Web hiện đại vốn bị chìm trong nền tối. Chỉ sử dụng **Retro Comic Shadow (Bóng cứng)**:

*   **Bóng mặc định (Default state):** Đổ bóng lệch xuống dưới và sang phải một khoảng cố định (`offset-x: 4px`, `offset-y: 4px`), tô màu bóng trùng với màu `Border Dark` (`#0F1016`).
*   **Bóng khi ấn (Pressed state):** Nút dịch chuyển dịch xuống dưới-phải `4px` và khoảng cách bóng giảm về `0px` để tạo cảm giác cơ học thật sự.

---

## 6. Component Specs & State Machine (Quy chuẩn cấu phần UI)

Mọi thành phần UI khi được dựng lên bởi AI đều bắt buộc phải có đầy đủ các trạng thái tương tác sau:

### 6.1. Buttons (Nút bấm)
*   **Cấu trúc:** Text ở trung tâm (Hạn chế tối đa icon, chỉ dùng icon khi thực sự cần thiết như nút "X" đóng popup).
*   **Các trạng thái tương tác:**
    1.  **Normal:** Màu nền Primary/Secondary + Viền viền Dark 3px + Bóng cứng 4px.
    2.  **Hover:** Màu nền sáng lên 10% (VD: dùng `unity-background-image-tint-color` hoặc đổi màu nhẹ).
    3.  **Active/Pressed:** Dịch chuyển phần tử xuống dưới-phải 4px (`translate: 4px 4px`), bóng cứng giảm về 0.
    4.  **Disabled:** Màu xám nhạt (`#6c757d`), mất bóng cứng, không nhận sự kiện click.

### 6.2. Text Inputs (Ô nhập liệu)
*   **Cấu trúc:** Nhãn hướng dẫn (Placeholder) mờ + Viền nét bao quanh + Nền tối Surface Deep.
*   **Các trạng thái tương tác:**
    1.  **Normal:** Nền `#1E1E24`, Viền Dark 2px.
    2.  **Focused:** Viền đổi màu sáng hơn (Primary `#FFD23F`), xuất hiện con trỏ nhấp nháy.
    3.  **Error:** Viền đổi sang màu đỏ Danger `#E63946`.

### 6.3. Lists & Cards (Danh sách cuộn & Hộp thông tin)
*   **Cấu trúc:** ScrollView chứa danh sách các Card item xếp chồng.
*   **Quy định cuộn:** 
    *   Ẩn thanh cuộn dọc (Scrollbar) nếu không cần thiết để tối ưu diện tích hiển thị di động.
    *   Card item phải có viền bao và khoảng cách giữa các Card tối thiểu `12px` để không bị dính vào nhau.

---

## 7. Do's and Don'ts (Quy tắc hành vi của AI Agent)

### 10 Điều BẮT BUỘC Làm (DO's)
1.  **Luôn căn lề hoàn chỉnh:** Sử dụng Flexbox (`justify-content: center; align-items: center;`) để căn chỉnh chữ và nội dung nút.
2.  **Luôn có nét viền tối (`Border Dark`):** Đảm bảo mọi panel, card, button đều có viền tách biệt để giữ phong cách hoạt hình/tangible.
3.  **Kiểm tra độ dài văn bản:** Thiết kế nút bấm và input chừa khoảng trống tối thiểu 20% chiều rộng để ngăn các từ dài (VD: "Làm mới") bị cắt chữ hoặc xuống dòng đột ngột.
4.  **Sử dụng kích thước nút bấm dễ chạm:** Trên thiết bị di động, chiều cao tối thiểu của nút tương tác là `44px`.
5.  **Duy trì khoảng cách đồng đều:** Sử dụng đúng Spacing Scale bội số của 8 để thiết kế gọn gàng.
6.  **Tách biệt logic và giao diện:** Giữ mã điều khiển UI (C# Controller) độc lập với logic nghiệp vụ cốt lõi của game.
7.  **Đặt tên class USS có ngữ nghĩa:** Đặt tên rõ ràng (VD: `.friends-tab-button`, `.leaderboard-rank-card`) thay vì tên chung chung.
8.  **Đóng mở popup an toàn:** Luôn cung cấp nút đóng rõ ràng (Nút X ở góc trên bên phải) và hỗ trợ đóng popup khi người dùng bấm ra vùng ngoài panel (vùng mờ màu đen).
9.  **Tự động dọn dẹp bộ nhớ:** Đăng ký và hủy đăng ký các sự kiện Click trong C# tương ứng với chu kỳ bật/tắt UI để tránh rò rỉ bộ nhớ (Memory Leak).
10. **Tạo phân cấp hình ảnh rõ ràng:** Tiêu đề to, màu sắc tương phản cao, thông số phụ dùng màu dịu hơn.

### 10 Điều CẤM Làm (DON'Ts)
1.  **KHÔNG lạm dụng bóng mờ hoặc mờ kính (Glassmorphism):** Tránh xa hiệu ứng `backdrop-filter: blur()` trừ khi có chỉ định đặc biệt. Style của game là phẳng, sắc nét và trực quan.
2.  **KHÔNG nhét icon bừa bãi (Icon Overuse):** Chỉ dùng icon khi thực sự mang lại giá trị nhận biết nhanh. Không đặt icon trước mọi dòng chữ, tiêu đề hoặc nút bấm thông thường.
3.  **KHÔNG tự chế màu ngẫu nhiên:** Không dùng các hệ màu lạ mắt ngoài bảng màu hệ thống đã quy định.
4.  **KHÔNG hardcode tọa độ tuyệt đối:** Tránh dùng `position: absolute` bừa bãi trừ khi thiết kế popup lớp đè lên (Overlay) hoặc các góc neo đặc biệt. Luôn ưu tiên Flexbox.
5.  **KHÔNG bỏ qua trạng thái phản hồi:** Nút bấm tương tác không có trạng thái hover và pressed bị xem là lỗi thiết kế nghiêm trọng.
6.  **KHÔNG dùng font chữ mặc định của hệ điều hành:** Tránh để UI rơi vào font mặc định không có phong cách riêng.
7.  **KHÔNG cuộn chồng cuộn (Nested ScrollViews):** Không lồng ScrollView này bên trong ScrollView khác cùng hướng cuộn vì sẽ gây loạn thao tác trên di động.
8.  **KHÔNG viết CSS trực tiếp (Inline CSS) trong UXML:** Luôn tách biệt CSS ra file USS để dễ quản lý và tái sử dụng.
9.  **KHÔNG bỏ sót kiểm thử độ phân giải:** Không thiết kế UI chỉ chạy tốt trên màn hình Landscape 16:9 mà bỏ qua việc co giãn khi đổi tỷ lệ màn hình.
10. **KHÔNG tự động áp đặt hiệu ứng nhấp nháy chuyển màu liên tục:** Hạn chế các hiệu ứng màu sắc động gây mỏi mắt trừ các sự kiện đặc biệt (nhận thưởng).

---

## 8. AI Agent Self-Check Protocol (Quy trình tự kiểm soát chất lượng)

*Trước khi bàn giao bất kỳ giao diện nào cho người dùng, AI Agent phải tự trả lời 15 câu hỏi sau:*

1.  [ ] Toàn bộ màu sắc sử dụng có trùng khớp với danh sách Color Tokens ở Mục 2.1 không?
2.  [ ] Các nút tương tác có viền ngoài tối màu (`Border Dark`) để tách biệt chưa?
3.  [ ] Khoảng cách padding/margin và bo góc có tuân thủ quy tắc Spacing Scale (bội số của 8) không?
4.  [ ] Font chữ sử dụng có đồng bộ và tải từ Resource của game không?
5.  [ ] Đã thử nghiệm hiển thị với chuỗi văn bản dài nhất chưa (VD: "Làm mới" có bị khuyết nét, chữ có tràn khỏi nút)?
6.  [ ] Các nút bấm đã được cấu hình đủ 3 trạng thái USS (`:hover`, `:active/pressed`, `:disabled`) chưa?
7.  [ ] Nút bấm có hiệu ứng lún cơ học (bóng cứng giảm, dịch chuyển vị trí) khi bấm không?
8.  [ ] Có phần tử nào đang lạm dụng hiệu ứng kính mờ (glassmorphism) trái quy định không?
9.  [ ] Số lượng icon trên màn hình đã tối giản chưa? Có nút nào có thể thay bằng chữ trực quan không?
10. [ ] Có dùng `position: absolute` sai quy cách không? Giao diện có co giãn tốt khi thay đổi độ phân giải không?
11. [ ] Đã có cơ chế đóng popup rõ ràng (Nút X hoặc click ra ngoài) chưa?
12. [ ] Các phần tử tương tác đã được gán ID hoặc Class cụ thể để viết Script điều khiển chưa?
13. [ ] Các sự kiện click trong script C# đã được giải phóng (unregister) khi tắt giao diện chưa?
14. [ ] Cấu trúc cây UXML có quá sâu (nhiều lớp visual element lồng vô nghĩa) không?
15. [ ] Bản vẽ đã mang lại cảm giác "vật lý, có thể chạm được" (The Tangible Playground) chưa?

---
*Bản hướng dẫn này là tài liệu sống, cần cập nhật liên tục sau mỗi màn hình giao diện mới được triển khai thành công.*
