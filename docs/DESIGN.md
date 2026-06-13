# DESIGN SYSTEM: COZY DARK PALIA FRAMEWORK

*(Hệ thống thiết kế giao diện chính thức cho dự án game 3D - Y WONDER GREEN FARM)*

---

# [ENGLISH VERSION]

> [!IMPORTANT]
> **AI AGENT INSTRUCTIONS (Must read before writing any UI layout or C# controllers):**
>
> 1. All UI screens in this project (UXML, USS, C# Controllers) must comply strictly with the design system specifications outlined in Part I.
> 2. You must study and strictly follow the 8 AI UI diseases detailed in Part II.
> 3. You must execute the 15-question Self-Check Protocol in Part III before submitting any UI work.

---

## COZY DARK PALIA - DESIGN SPECIFICATIONS

### PART I: DESIGN SYSTEM FRAMEWORK

#### 1. Overview & Creative North Star

* **Project Type:** 3D Farming & Life Simulation Game
* **Target Platform:** PC (Steam/Web) & Mobile (Landscape layout)
* **Creative North Star:** **"Cozy. Simple. Friendly." (The Palia Philosophy)**
  * *Concept:* The interface serves as a "supportive frame" for the game content. It must never overpower the 3D graphics. It relies on pure flat graphics and soft gradients rather than heavy textures or realistic materials.
  * *Interactions:* Interactions should feel bouncy and soft via scale changes. Buttons are pure flat pill-shaped (no 3D lips or drop shadows).
  * *Integration:* We use a **Dark Theme** to emphasize social features (Chat, Friends) and ensure texts have perfect readability and contrast over long play sessions.

#### 2. Color System & Contrast Rules

##### 2.1. Color Tokens (Exact Palia Hex Codes)

All components must use these exact color values to maintain visual unity:

| Token                     | Value                     | Usage                                                                   |
| :------------------------ | :------------------------ | :---------------------------------------------------------------------- |
| **Primary Navy**    | `#3A4766`               | Base dark background for main panels                                    |
| **Deep Blue**       | `#305080`               | Inner cards, secondary sections                                         |
| **Mystic Black**    | `#151A27`               | Deepest backgrounds, overlays, input fields, dark text on light buttons |
| **Accent Yellow**   | `#FDEF70`               | Primary action buttons, active tabs, glowing highlights                 |
| **Yellow Lip (3D)** | `#D0C350`               | (DEPRECATED) Active states color (No longer used as 3D border)          |
| **Accent Blue**     | `#4EB0E6`               | Sliders, secondary highlights, links                                    |
| **Palia White**     | `#FFFCF7`               | Main text titles, headings, and labels                                  |
| **Bright Grey**     | `#DFDCE4`               | Explanatory subtexts                                                    |
| **Overcast Grey**   | `#A6A4A9`               | Disabled states, placeholders, subtle borders                           |
| **Tan / Cream**     | `#D5B892` / `#FEF4E6` | Light paper/wood backgrounds for item cards                             |

##### 2.2. Contrast Rules & Surface Hierarchy

* **Contrast Rule:** Texts must always contrast heavily with the background. Palia White on Mystic Black or Primary Navy. Mystic Black text on Accent Yellow buttons.
* **Surface Depth Hierarchy:**
  * **Layer 0 (Game World):** 3D gameplay scene.
  * **Layer 1 (Overlay):** Mystic Black dimming screen (`rgba(21, 26, 39, 0.7)`).
  * **Layer 2 (Panels):** Main panels (Primary Navy `#3A4766` with large 24px rounded corners).
  * **Layer 3 (Cards/Elements):** Inner elements (Deep Blue `#305080` or Tan `#D5B892`).

#### 3. Typography & Alignment

* **Primary Fonts:** We use a dual-font system.
  * **Header Font:** **Merriweather** (Serif font for a classic, storybook feel).
  * **Body Font:** **Quicksand** (Rounded Sans-serif for cozy, friendly reading).
* **Type Scale:**
  * **H1 (Titles):** `44px` (Merriweather Regular, used for main panel headers).
  * **H2 (Sub-headers):** `34px` (Merriweather Bold).
  * **H3 (Section Headers):** `24px` (Merriweather Bold).
  * **Body Large:** `24px` (Quicksand Bold, button labels).
  * **Body Normal:** `20px` (Quicksand Bold, item names).
  * **Caption:** `18px` (Quicksand Bold, secondary parameters).

#### 4. Spacing, Borders & Unit System

* **Spacing Scale:**
  * `xs`: `4px`, `sm`: `8px`, `md`: `16px`, `lg`: `24px`, `xl`: `32px`.
* **Corner Radius Scale (SOFT & COZY):**
  * `Panels / Modals`: `24px` to `32px` (Very soft and round).
  * `Cards / Items`: `12px` to `16px`.
  * `Buttons`: `24px` (Pill-shape, fully rounded ends).
* **Border Thickness:**
  * Avoid thick harsh borders. Use soft translucent borders `rgba(255,255,255,0.1)`.

#### 5. Component Specifications

##### 5.1. Buttons

* **Primary:** Pure Flat Pill-shaped (fully rounded). Accent Yellow `#FDEF70` background, Mystic Black `#151A27` text. On active/press, scale down to 0.98 or change color (no 3D borders or drop shadows allowed).
* **Secondary:** Pill-shaped outline buttons. Transparent or dark blue fill with light border.

##### 5.2. Sliders & Checkboxes

* **Sliders:** Track is Mystic Black `#151A27`, Fill and Thumb are Accent Blue `#4EB0E6`.
* **Checkboxes:** Soft rounded squares, yellow border when checked.

##### 5.3. Sprites & Icons (2D vs 2.5D Rule)

* **HUD Icons:** MUST use **Flat 2D Sprites**. Simple, minimalist, high contrast, clean shapes (e.g., solid white icons). Reason: High readability, avoids cluttering the 3D world view.
* **Popup Icons (Inventory, Shop, Items):** MUST use **2.5D / Stylized 3D Render Sprites**. Isometric or top-down angles, soft shading, chunky clay-like feel, vibrant colors. Reason: Gives tangibility, weight, and visual reward to collected items.

---

### PART II: 8 "AI UI DISEASES" & PREVENTION IN Y WONDER GREEN FARM

#### 1. The "Glassmorphism & Sharp Edges" Addiction

* **In this project:** **Glassmorphism IS ALLOWED exclusively for HUD components** (e.g., dark navy translucent background `rgba(58, 71, 102, 0.5)` with thin soft white borders) to avoid blocking the 3D camera. However, main Popups must remain solid flat graphics. Do NOT use sharp 4px corners; everything must be cozy and heavily rounded (12px to 32px).

#### 2. Unit Chaos (Mixed Metric Units)

* **In this project:** Unity UI Toolkit has strict layouts. You must use `px` for borders, corner-radius, and font-sizes. Use `%` or flex properties for responsive container dimensions. Never mix unrelated styling units.

#### 3. Color Chaos (Functional Color Neglect)

* **In this project:** Stick EXACTLY to the Palia hex codes. Primary action buttons are Accent Yellow (`#FDEF70`). Do not introduce random rainbow gradients or neon colors.

#### 4. Missing States Failure (Static UI Syndrome)

* **In this project:** All buttons must implement `:hover`, `:active`, and `:disabled` styles. Writing static buttons is considered a layout bug.

#### 5. Typography Failure (Default Font Syndrome)

* **In this project:** Do not use Arial/Roboto. You must use Merriweather for headers and Quicksand for body text. Failing to separate Serif and Sans-serif ruins the Palia aesthetic.

#### 6. Text Input Neglect

* **In this project:** Search fields must have a clean placeholder, static labels, and clear yellow highlighted borders on focus.

#### 7. Layout Cutoff (Text Overrun)

* **In this project:** Always design buttons with a safety margin (padding-left/right at least 24px for pill buttons). Ensure text does not get truncated.

#### 8. Visual Hierarchy Mess

* **In this project:** Follow the Type Scale. Headers must be prominent (`44px` Merriweather).

---

### PART III: AI AGENT SELF-CHECK PROTOCOL

*Before submitting any UI changes, you must satisfy this 15-question checklist:*

1. [ ] Are all colors exactly mapped to the Palia hex codes (Primary Navy, Accent Yellow, Mystic Black)?
2. [ ] Are primary buttons pure Flat Pill-shaped (no 3D drop shadows or bottom borders)?
3. [ ] Are fonts properly assigned (Merriweather for Headers, Quicksand for Body)?
4. [ ] Are panel corners extremely soft and cozy (24px to 32px)?
5. [ ] Did you keep glassmorphism limited to HUD only (not abused on popups) and avoid sharp minimalist edges?
6. [ ] Have you written full `:hover`, `:active`, and `:disabled` styles?
7. [ ] Does the UI feel like a "supportive frame" rather than an overpowering sci-fi dashboard?
8. [ ] Are sliders styled with Accent Blue thumbs and Mystic Black tracks?
9. [ ] Have you avoided neon colors and thick black borders?
10. [ ] Does the UI scale correctly under different screen ratios?
11. [ ] Is there an overlay dismiss script and a clear close button?
12. [ ] Are inputs equipped with static labels and proper focus highlights?
13. [ ] Did you avoid hardcoding fixed widths for text elements?
14. [ ] Is text contrast extremely high (Palia White on Navy, Mystic Black on Yellow)?
15. [ ] Did you unregister all UI toolkit event callbacks in C# to prevent memory leaks?

---

---

# [BẢN TIẾNG VIỆT]

> [!IMPORTANT]
> **HƯỚNG DẪN DÀNH CHO AI AGENT (Đọc kỹ trước khi tạo file thiết kế cụ thể):**
>
> 1. Tất cả các màn hình giao diện (UXML, USS, C# Controllers) phải tuân thủ nghiêm ngặt hệ thống thiết kế Palia tại Phần I.
> 2. AI phải nghiên cứu và tuân thủ tuyệt đối 8 bệnh lý UI được nêu chi tiết ở Phần II.
> 3. AI phải thực hiện bộ 15 câu hỏi tự kiểm tra tại Phần III trước khi bàn giao công việc UI.

---

## COZY DARK PALIA - THÔNG SỐ THIẾT KẾ CHI TIẾT

### PHẦN I: KHUNG HỆ THỐNG THIẾT KẾ

#### 1. Tổng quan & Triết lý thiết kế (Châm ngôn)

* **Loại dự án:** Game 3D mô phỏng Nông trại, Sinh tồn & Xã hội
* **Nền tảng mục tiêu:** PC & Mobile Landscape
* **Creative North Star:** **"Cozy. Simple. Friendly." (Ấm cúng, Đơn giản, Thân thiện).**
  * *Triết lý:* Giao diện chỉ là một "khung viền hỗ trợ" (supportive frame) để tôn lên nội dung game, tuyệt đối không được áp đảo đồ họa 3D. Sử dụng đồ họa phẳng thuần túy (pure flat graphics) và dải màu chuyển êm ái (soft gradients).
  * *Tính tương tác:* Các nút bấm mềm mại, hình viên thuốc (pill-shape) thiết kế phẳng hoàn toàn (không viền lún 3D, không đổ bóng). Cảm giác bấm tạo ra nhờ hiệu ứng thu nhỏ (Scale) hoặc đổi màu nền.
  * *Chủ đề:* Sử dụng **Dark Theme** (Nền tối) vì game chú trọng vào tính năng Xã hội. Nền tối giúp chữ nổi bật, tăng độ tương phản, giúp người chơi đọc Chat và danh sách bạn bè thời gian dài không bị mỏi mắt.

#### 2. Hệ thống màu sắc & Độ tương phản

##### 2.1. Color Tokens (Mã Hex chuẩn Palia)

Bắt buộc dùng chính xác mã màu sau:

| Token                            | Giá trị                 | Phạm vi sử dụng                                                           |
| :------------------------------- | :------------------------ | :--------------------------------------------------------------------------- |
| **Primary Navy**           | `#3A4766`               | Nền xanh tối cho các Panel chính                                         |
| **Deep Blue**              | `#305080`               | Nền xanh sâu hơn cho các thẻ (Cards) bên trong                         |
| **Mystic Black**           | `#151A27`               | Đen huyền bí cho màn chắn, ô nhập liệu, và chữ trên nút vàng    |
| **Accent Yellow**          | `#FDEF70`               | Vàng rực rỡ cho Nút bấm chính, tab đang chọn, hiệu ứng glow        |
| **Yellow Lip (3D)**        | `#D0C350`               | Vàng sậm dùng làm viền đáy (bottom-border) tạo độ lún 3D cho nút |
| **Accent Blue**            | `#4EB0E6`               | Xanh dương sáng cho Thanh trượt (Slider), link                          |
| **Palia White**            | `#FFFCF7`               | Trắng sữa cho tiêu đề và chữ chính                                   |
| **Bright / Overcast Grey** | `#DFDCE4` / `#A6A4A9` | Xám cho chữ phụ, trạng thái khóa, placeholder                          |
| **Tan / Cream**            | `#D5B892` / `#FEF4E6` | Be/Kem cho các thẻ item giấy/gỗ                                          |

#### 3. Font chữ & Typography

* **Hệ hai Font (Dual-font system):**
  * **Tiêu đề (Header):** Dùng **Merriweather** (Font có chân - Serif) tạo nét cổ điển, phiêu lưu.
  * **Nội dung (Body):** Dùng **Quicksand** (Font không chân bo tròn - Rounded Sans-serif) tạo sự thân thiện.
* **Type Scale:** Cỡ chữ lớn, rõ ràng (Ví dụ: Tiêu đề H1 lên tới 44px, nút bấm 24px).

#### 4. Khối hình & Bo góc (Shapes & Radius)

* Từ bỏ các góc bo 4px-8px sắc cạnh của game Sci-fi.
* `Panels`: Bo góc cực lớn, mềm mại từ `24px` đến `32px`.
* `Nút bấm (Buttons)`: Bắt buộc là hình con nhộng (**Pill-shape**, góc bo bằng một nửa chiều cao nút, ví dụ 24px).

#### 5. Bệnh lý UI & Tự kiểm tra

* **Không LẠM DỤNG Kính mờ (Glassmorphism):** Glass CHỈ dùng cho HUD (nền navy bán trong suốt để không che camera 3D). Popup chính vẫn dùng Flat color + gradient mềm, hạn chế blur hại máy.
* **Tuyệt đối không dùng viền Neon, viền đen đặc, không đổ bóng (Drop Shadows):** Thiết kế phẳng 100%. Dùng viền mềm bán trong suốt hoặc không viền.
* **Tương tác phẳng (Flat Interaction):** Nút bấm tuyệt đối KHÔNG có `border-bottom-width`. Khi nhấn chỉ dùng `scale: 0.98` hoặc đổi màu nền.
