### Design System: The Playful Homestead Framework

#### 1. Overview & Creative North Star

**Creative North Star: "The Tangible Playground"**
This design system does not aim for complex sleekness, but focuses on **friendliness, clarity, and a tactile feel**. The UI should feel like plastic or wooden toy puzzle pieces placed on the green grass of the 3D environment.

The "Tangible Playground" philosophy dictates that all interactions must be decisive. The interface is completely separated from the actual 3D world below through solid color blocks, clear borders, and high contrast, ensuring players never get visually confused when interacting.

---

#### 2. Color & Surface

The color palette is playful, clearly separating the background of panels and action buttons.

* **The Tokens:**
  * **Neutral Surface:** Light Gray (`#EFEFEF`) - Used for functional menu panels like Settings.
  * **Hero Surface:** Vibrant Purple (`#5B42F3`) - Used for achievement and competitive panels like the Leaderboard.
  * **Action Colors:** Warning Red (`#FF4B4B`), Confirm Blue (`#2D7BFF`), Achievement Yellow (`#FFC107`).
* **The "Solid Block" Rule:** The use of complex color gradients as the main background for Panels is strictly prohibited. Surfaces must be solid colors to maintain a rustic and consistent feel.
* **Surface Hierarchy:**
  * **Layer 0 (Game World):** 3D environment.
  * **Layer 1 (Overlay):** Black dimming screen (40% opacity) blocking interaction with the 3D world.
  * **Layer 2 (Panels):** Main information panels (Settings, Leaderboard).

---

#### 3. Typography

Typography in a farming game must prioritize "Quick Scanning" above all else.

* **The Friendly Sans:** Use only one Sans-serif font family with clean cuts and medium to bold thickness.
* **Header & Title:** Large text size, always centered in the header bar of Panels. White or black depending on the background; absolutely do not use neutral colors for titles.
* **Data & List:** Smaller text size, used for player names, scores, or setting items. Always left-aligned to create an easy reading flow.

---

#### 4. Elevation & Depth

This is the most characteristic point of this system. Do not use Glow or Glassmorphism effects.

* **The "Retro Offset" Shadow:** All Panels and prominent Buttons must use a Solid Drop Shadow, strictly no blurring (0px Blur).
  * **Formula:** Push the shadow down (Y: +6px) and to the right (X: +6px). The shadow color must always be dark tones (like Dark Navy or Black with 60% Opacity).
* **Pressed State:** When the player taps, the button will shift along the X/Y axis to fill the drop shadow, creating a "physical" feel like pressing a mechanical button.

---

#### 5. Components

* **Panels:** Always large rounded rectangles (Minimum Border Radius of 16px). Must have a Header Bar across the top with a contrasting color (e.g., if the background is gray, the Header is dark gray; if the background is purple, the Header is light purple). The "X" close button is always located in the top right corner.
* **Action Buttons (HUD):** Control buttons on the main screen (Plant, Cancel, Jump) must be perfect circles. Use thick strokes and large, minimalist icons in the center.
* **List Items:** Use a subtle "Zebra Striping" rule for long lists like the Leaderboard. Alternate super light pastel colors (Cream, Light Blue, Pastel Pink) so the eye can easily follow each row without needing horizontal lines.
* **Toggles & Sliders:** The track is a pill-shaped light gray, and the thumb is a solid white circle with a soft border or slight shadow to stand out against the track.

---

#### 6. Do’s and Don’ts

**Do:**

* **Round everything:** From menu panels to buttons, keep the corners rounded to create a friendly, safe feel.
* **Use high contrast colors:** Decisive buttons (Delete, Exit, Buy) must use vibrant primary colors (Red, Blue).
* **Keep padding:** Leave breathing room around text and buttons in Settings or Leaderboard panels.

**Don’t:**

* **No Blur/Glassmorphism:** Do not blur the background behind the UI. It destroys the "tangible toy" style and reduces the 3D game's performance.
* **No Serif fonts:** Serif fonts feel too formal and academic, unsuitable for a relaxing farm environment.
* **No hairline borders:** The enemy of casual games is 1px faint borders. If a border is needed, use a thick one (2px-3px) or use none at all, relying purely on color blocks.

---

*Director's Note: "Don't make the player think about where the button is. Let the colors and shapes naturally invite their fingers."*

### Design System: The Playful Homestead Framework

#### 1. Overview & Creative North Star

**Creative North Star: "The Tangible Playground" (Sân chơi Hữu hình)**
Hệ thống thiết kế này không hướng tới sự bóng bẩy phức tạp, mà tập trung vào **sự thân thiện, rõ ràng và tính xúc giác (tactile)**. UI phải tạo cảm giác như những mảnh ghép đồ chơi bằng nhựa hoặc gỗ được đặt lên trên thảm cỏ xanh của môi trường 3D.

Triết lý "The Tangible Playground" quy định rằng mọi tương tác phải dứt khoát. Giao diện tách biệt hoàn toàn khỏi thế giới 3D thực tế bên dưới thông qua các khối màu đặc, viền rõ nét và sự tương phản cao, giúp người chơi không bao giờ bị rối mắt khi thao tác.

---

#### 2. Color & Surface (Màu sắc & Bề mặt)

Bảng màu mang tính vui nhộn, tách bạch rõ ràng giữa phần nền của bảng biểu và các nút thao tác.

* **The Tokens:**
  * **Neutral Surface:** Xám nhạt (#EFEFEF) - Dùng cho các bảng menu chức năng như Setting.
  * **Hero Surface:** Tím rực rỡ (#5B42F3) - Dùng cho các bảng mang tính thành tích, ganh đua như Leaderboard.
  * **Action Colors:** Đỏ cảnh báo (#FF4B4B), Xanh xác nhận (#2D7BFF), Vàng thành tựu (#FFC107).
* **The "Solid Block" Rule (Quy tắc Khối đặc):** Nghiêm cấm sử dụng dải màu chuyển (Gradient) phức tạp làm nền chính cho các Panel. Bề mặt phải là những dải màu phẳng (Solid Color) để duy trì sự mộc mạc và nhất quán.
* **Surface Hierarchy:**
  * **Layer 0 (Game World):** Môi trường 3D.
  * **Layer 1 (Overlay):** Màn mờ đen (opacity 40%) chặn tương tác với thế giới 3D.
  * **Layer 2 (Panels):** Các bảng thông báo chính (Setting, Leaderboard).

---

#### 3. Typography (Nghệ thuật chữ)

Chữ trong game nông trại cần đặt tiêu chí "Đọc lướt nhanh" lên hàng đầu.

* **The Friendly Sans (Phông chữ thân thiện):** Sử dụng duy nhất một họ phông chữ không chân (Sans-serif) với các nét cắt gọn gàng, độ dày vừa phải (Medium đến Bold).
* **Header & Title:** Cỡ chữ lớn, luôn được căn giữa trong thanh tiêu đề của các Panel. Màu trắng hoặc đen tùy thuộc vào nền, tuyệt đối không dùng màu trung tính cho tiêu đề.
* **Data & List:** Cỡ chữ nhỏ hơn, dùng cho tên người chơi, số điểm, hoặc các mục cài đặt. Luôn căn lề trái để tạo luồng đọc dễ dàng.

---

#### 4. Elevation & Depth (Độ cao & Chiều sâu)

Đây là điểm đặc trưng nhất của hệ thống này. Không sử dụng hiệu ứng tỏa sáng (Glow) hay mờ kính (Glassmorphism).

* **The "Retro Offset" Shadow (Đổ bóng khối lệch):** Tất cả các Panel và Nút bấm nổi bật phải dùng bóng đổ dạng khối đặc (Solid Drop Shadow), không được làm nhòe (0px Blur).
  * **Công thức:** Đẩy bóng lệch xuống dưới (Y: +6px) và sang phải (X: +6px). Màu bóng luôn là các tone màu tối đậm (như Xanh đen đậm hoặc Đen với 60% Opacity).
* **Nút bấm chìm (Pressed State):** Khi người chơi chạm vào, nút bấm sẽ dịch chuyển theo trục X/Y để lấp đầy phần bóng đổ, tạo cảm giác "vật lý" như đang bấm một nút cơ học.

---

#### 5. Components (Thành phần UI)

* **Panels (Bảng Menu):** Luôn là hình chữ nhật bo góc lớn (Border Radius tối thiểu 16px). Phải có một dải thanh tiêu đề (Header Bar) cắt ngang phía trên cùng với màu sắc tương phản (ví dụ: Nền xám thì Header xám đậm; Nền tím thì Header Tím nhạt). Dấu "X" đóng bảng luôn nằm góc trên cùng bên phải.
* **Action Buttons (Nút tương tác HUD):** Các nút điều khiển trên màn hình chính (Trồng trọt, Hủy, Nhảy) bắt buộc phải là hình tròn hoàn hảo. Sử dụng viền ngoài đậm (Stroke) và các icon lớn, tối giản ở chính giữa.
* **List Items (Danh sách):** Sử dụng quy tắc "Zebra Striping" (Sọc vằn) nhạt cho các danh sách dài như Bảng Xếp Hạng. Xen kẽ các màu pastel siêu nhạt (Kem, Xanh lơ, Hồng phấn) để mắt dễ dàng theo dõi từng hàng mà không cần vẽ đường kẻ ngang.
* **Toggles & Sliders (Thanh gạt & Kéo):** Rãnh kéo có hình viên thuốc (Pill-shaped) màu xám nhạt, cục kéo (Thumb) là hình tròn màu trắng trơn có viền mờ hoặc bóng nhẹ để nổi bật lên trên rãnh.

---

#### 6. Do’s and Don’ts

**Do (Bắt buộc):**

* **Bo góc mọi thứ:** Từ bảng menu đến nút bấm, hãy giữ các góc tròn để tạo sự thân thiện, an toàn.
* **Dùng màu tương phản cao:** Các nút mang tính quyết định (Xóa, Thoát, Mua) phải dùng các màu nguyên bản (Đỏ, Xanh) rực rỡ.
* **Giữ khoảng trống (Padding):** Để không gian nghỉ xung quanh các đoạn text và nút bấm trong bảng Setting hoặc Leaderboard.

**Don’t (Nghiêm cấm):**

* **Không dùng Blur/Glassmorphism:** Không làm mờ phông nền phía sau UI. Nó làm mất đi phong cách "đồ chơi hữu hình" và làm giảm hiệu năng của game 3D.
* **Không dùng phông chữ có chân (Serif):** Chữ có chân tạo cảm giác quá trang trọng, học thuật, không phù hợp với không gian nông trại thư giãn.
* **Không dùng viền mỏng (Hairline borders):** Kẻ thù của game casual là những đường viền 1px mờ nhạt. Nếu cần viền, hãy dùng viền dày (2px-3px) hoặc không dùng viền, chỉ dùng mảng màu.

---

*Director's Note: "Đừng bắt người chơi phải suy nghĩ xem nút bấm nằm ở đâu. Hãy để màu sắc và hình khối tự gọi mời ngón tay của họ."*
