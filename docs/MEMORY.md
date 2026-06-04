# MEMORY.md — Đúc kết kinh nghiệm
> Cập nhật: 2026-06-03
> Mục đích: Ghi lại sai lầm + cách khắc phục để AI không lặp lại.

---

## 🔴 SAI LẦM NGHIÊM TRỌNG

### 1. AI vô tình xóa element khi sửa UI
**Tình huống:** Khi sửa nút trên GameHUD, AI đã xóa mất nút Cài đặt, thanh tiền tệ và nút Balo.
**Nguyên nhân:** Dùng replace quá rộng, viết lại cả block code thay vì sửa đúng chỗ.
**Quy tắc:**
- PHẢI kiểm tra lại TOÀN BỘ elements sau khi sửa UI
- Dùng multi_replace chỉ sửa đúng dòng cần sửa, KHÔNG viết lại cả file
- Luôn chụp màn hình TRƯỚC và SAU khi sửa UI để so sánh

### 2. Không bao giờ sửa file .meta
**Tình huống:** Unity dùng file .meta để track assets, references, GUIDs.
**Quy tắc:** Nếu sửa/xóa file .meta → Unity mất track → reference bị null → lỗi cả project.

### 3. Unity version mismatch — Awaitable API
**Tình huống:** Toolkit unity-ai-workflow dùng `Awaitable` (Unity 6.2+) nhưng dự án chạy Unity 2022 LTS.
**Hậu quả:** Compile error, code không chạy được.
**Quy tắc:**
- Dự án này dùng **Unity 2022 LTS** — KHÔNG có `Awaitable`, `Application.exitCancellationToken`
- Async pattern: dùng **UniTask** hoặc **async Task**
- Luôn kiểm tra Unity version trước khi copy code từ nguồn ngoài

---

## 🟡 LỖI UI TOOLKIT THƯỜNG GẶP

### 4. Element bị chìm sau element khác (Z-Order)
**Tình huống:** Nút X đặt TRƯỚC panel trong UXML → bị panel đè lên, chỉ thấy nửa nút.
**Nguyên nhân:** UI Toolkit render theo thứ tự UXML — element sau đè lên element trước.
**Quy tắc:**
- Element cần nổi lên trên → khai báo **SAU** trong UXML
- Ví dụ: Nút X overlay → đặt SAU panel, KHÔNG phải trước

### 5. display: none → không thấy gì trong UI Builder
**Tình huống:** Settings popup có `display: none` trong USS → UI Builder hiện trống.
**Giải thích:** Đây là hành vi ĐÚNG — popup chỉ hiện khi controller gọi `Show()` lúc runtime.
**Mẹo:** Tạm bỏ `display: none` để preview trong UI Builder → thêm lại khi xong.

### 6. Toggle checkmark thành ô vuông xanh
**Tình huống:** Style `background-color` cho `.unity-toggle__checkmark` → hiện ô vuông tím thay vì dấu tick.
**Quy tắc:**
- Muốn toggle kiểu pill/switch → style `.unity-toggle__checkmark` thành hình dài bo tròn (border-radius lớn)
- Không chỉ đổi background-color, phải đổi cả kích thước và border-radius

### 7. Text bị cắt trong Dropdown
**Tình huống:** "Tiếng Việt" hiện thành "Tiếng" — dropdown quá hẹp.
**Nguyên nhân:** Label bên trái chiếm quá nhiều width → dropdown không đủ chỗ.
**Quy tắc:**
- Label width nên ≤ 80px cho layout 2 cột
- Dropdown cần `min-width` để đảm bảo text không bị cắt
- Luôn test với text dài nhất (ví dụ: "Tiếng Việt" thay vì "EN")

### 8. Title text bị lệch khi có nút X absolute
**Tình huống:** Header có title + nút X (position absolute) → title bị đẩy lệch.
**Quy tắc:**
- Title dùng `position: absolute; left: 0; right: 0;` → căn giữa tuyệt đối
- Hoặc tách nút X ra ngoài header hoàn toàn

---

## 🟢 BÀI HỌC VỀ 3D / FBX

### 9. FBX từ AI 3D tools không có texture
**Tình huống:** Model từ Tripo/AI Studio import vào Unity → material xám trắng.
**Nguyên nhân:** AI tools gắn màu bằng Vertex Colors, KHÔNG phải image texture.
**Quy tắc:**
- Artist PHẢI **bake texture** trong Blender (Cycles → Bake → Diffuse Color) trước khi export
- Export FBX: Path Mode **Copy** + bật 📦 **Embed Textures**
- Hoặc gửi kèm folder texture riêng

### 10. Material hồng/xám sau import FBX
**Checklist theo thứ tự:**
1. Kiểm tra FBX có texture embed không (mở mũi tên ▶ trong Project)
2. Extract Materials: FBX → tab Materials → Extract Materials
3. Extract Textures: FBX → tab Materials → Extract Textures
4. Đổi shader sang **Universal Render Pipeline/Lit**
5. Gán texture thủ công vào Base Map nếu cần
6. Nếu vẫn hồng: Edit → Rendering → Materials → Convert All to URP

---

## 🔵 QUY TẮC LÀM VIỆC

### 11. Luôn chụp màn hình trước/sau khi sửa UI
- TRƯỚC: Screenshot để biết trạng thái hiện tại
- SAU: Screenshot để so sánh, phát hiện element bị mất/lệch

### 12. Khi USS không hiển thị đúng → test từng property
- KHÔNG sửa 5-6 property cùng lúc
- Sửa 1 property → chạy thử → xác nhận → sửa tiếp
- Đặc biệt quan trọng với: border-radius, position, overflow, margin/padding

### 13. Đọc DESIGN.md trước khi làm UI
- Mọi UI phải tuân thủ "The Tangible Playground"
- Solid colors, retro shadow (6px offset), bo góc lớn (16px+)
- KHÔNG dùng blur, gradient, glassmorphism
- Header panel luôn có màu contrasting

### 14. Context Canary — tín hiệu tràn context
- AI phải xưng **"bé"** và gọi user là **"anh yêu"**
- Nếu AI ngừng tuân thủ → context đã tràn → mở conversation mới
- Khi mở chat mới: dùng prompt trong `docs/CONTEXT_RECOVERY.md`

---

## 🔵 BÀI HỌC UI TOOLKIT BỔ SUNG

### 15. Đăng ký Sự kiện (Callback) An toàn & Tối ưu
- **Tình huống**: Thao tác khi chọn vật phẩm trong túi đồ đòi hỏi cập nhật chức năng cho các nút bấm hành động (ví dụ: Trang bị, Sử dụng).
- **Tránh sai lầm**: Đăng ký lại `.RegisterCallback<ClickEvent>` mỗi lần người chơi chọn vật phẩm khác nhau. Điều này gây tích tụ (pile-up) sự kiện dẫn đến rò rỉ bộ nhớ hoặc click một lần chạy nhiều logic cũ.
- **Giải pháp**: Chỉ đăng ký sự kiện click cho các nút hành động **MỘT LẦN DUY NHẤT** tại `RegisterCallbacks()`. Trong callback, sử dụng biến thành viên (như `selectedItem`) để lấy dữ liệu động của vật phẩm đang được chọn tại thời điểm click.

### 16. Bố cục 3 cột (Tabs -> Grid -> Details) cho Landscape UI
- **Tình huống**: Thiết kế giao diện túi đồ với màn hình ngang (16:9) để hiển thị đồng thời danh mục, danh sách vật phẩm và xem thông tin chi tiết.
- **Giải pháp**: Phân chia tỷ lệ cân đối theo cột: Cột 1 (Tabs) ~140px -> Cột 2 (Grid) linh hoạt chiếm diện tích chính -> Cột 3 (Details Panel) ~220px. Tổng chiều rộng Panel nâng từ `680px` lên `820px` tạo bố cục chắc chắn, cân đối và tận dụng tối đa chiều rộng màn hình.

---

## 🩺 BÁO CÁO Y TẾ — CÁC "BỆNH LÝ" GIAO DIỆN CỦA AI AGENT
> Nguồn: Anh yêu chia sẻ (2026-06-02). Bé cần tự kiểm tra mỗi khi làm UI.

### 17. KHÔNG lạm dụng Icon & Glassmorphism
- **Bệnh**: Nhồi nhét emoji/icon vào mọi vị trí (nút, tab, tiêu đề) mà không cần thiết, trộn lẫn style icon nét thanh và nét đậm.
- **Bé đã mắc**: ✅ Có — Anh yêu phải nhắc giảm icon ở Friends Popup.
- **Quy tắc**: Chỉ dùng icon khi nó **rõ nghĩa hơn chữ** hoặc **tiết kiệm không gian đáng kể**. Ưu tiên nút dạng TEXT rõ ràng. KHÔNG dùng blur/glassmorphism (đã có trong DESIGN.md).

### 18. KHÔNG lồng khung quá nhiều (Box-in-a-Box)
- **Bệnh**: Đóng khung mọi thứ (Card trong Card, wrapper trong wrapper) thay vì dùng khoảng trắng.
- **Quy tắc**: Mỗi element chỉ nên có tối đa **2 lớp bao bọc** (shadow wrapper + panel). Nếu thấy mình viết 3+ lớp `VisualElement` lồng nhau chỉ để chứa 1 Label → DỪNG LẠI và tối ưu.

### 19. Phân cấp thị giác rõ ràng (Visual Hierarchy)
- **Bệnh**: Dùng màu Primary (#5B42F3) cho quá nhiều thành phần, khiến người dùng không biết đâu là hành động chính.
- **Quy tắc**: Mỗi màn hình chỉ có **MỘT hành động chính** được highlight bằng màu nổi bật. Các hành động phụ dùng màu trung tính hoặc nhạt hơn.

### 20. Giữ hệ thống đơn vị nhất quán (Unit Consistency)
- **Bệnh**: Dùng lẫn lộn px với các giá trị lẻ tẻ (13.5px, 21px, 1.5px) không theo quy tắc.
- **Bé đã mắc**: ✅ Có — border-radius: 21px, border-width: 1.5px trong FriendsPopup.uss.
- **Quy tắc**: Tuân thủ hệ thống **bội 4** hoặc **bội 8** cho tất cả kích thước: `4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48...`. Không được sinh ra số lẻ tẻ ngoài hệ thống.

### 21. Luôn thiết kế đủ trạng thái (State Coverage)
- **Bệnh**: Chỉ thiết kế trạng thái tĩnh, quên hover/active/focus/loading/empty/error.
- **Quy tắc**: Mỗi element tương tác **BẮT BUỘC** phải có ít nhất: Normal, Hover, Active (Pressed). Danh sách phải có trạng thái Empty ("Chưa có bạn bè nào").

### 22. KHÔNG rập khuôn — Giữ bản sắc riêng
- **Bệnh**: Mọi trang đều giống SaaS template: Navbar → Hero → 3 cột Features → Pricing → Footer.
- **Quy tắc**: Luôn đọc DESIGN.md trước khi làm UI. Tuân thủ phong cách "The Tangible Playground" với solid colors, retro shadow, bo góc lớn. KHÔNG copy cấu trúc web phổ thông.

### 23. Tối ưu mã nguồn — Tránh phình code (Code Bloat)
- **Bệnh**: Sinh hàng chục `div` wrapper chỉ để căn giữa, lặp lại CSS thay vì dùng class chung.
- **Quy tắc**: Trước khi tạo wrapper mới, tự hỏi: "Có thể đạt được layout này bằng 1-2 thuộc tính CSS trên element cha không?". Nếu có → KHÔNG tạo wrapper.

### 24. Checklist tự kiểm tra UI trước khi giao
- [ ] Icon có thực sự cần thiết không? Nếu bỏ icon mà vẫn hiểu → bỏ icon.
- [ ] Có wrapper/khung nào thừa không? Tối đa 2 lớp bao bọc.
- [ ] Tất cả giá trị px có tuân thủ bội 4 không?
- [ ] Mọi nút bấm đều có :hover và :active chưa?
- [ ] Danh sách rỗng có hiển thị thông báo thay thế không?
- [ ] Màu Primary chỉ dùng cho 1 CTA chính trên màn hình?

---

## 🔵 BÀI HỌC VỀ FLEX LAYOUT & SAFE ZONE (Cập nhật 03/06/2026)

### 25. Tránh móp méo/tràn phần tử bằng `flex-shrink: 0`
- **Tình huống**: Trong các popup như Nhiệm vụ (`QuestPopup`) hoặc Điểm danh (`AttendancePopup`), các ô phần thưởng nhỏ (`.reward-slot`, `.day-slot`) bị chòi ra ngoài viền bo góc dưới của khung chứa lớn khi kích thước màn hình thay đổi.
- **Nguyên nhân**: Theo cơ chế Flexbox mặc định, các container đều có `flex-shrink: 1`. Khi không gian dọc bị bóp hẹp, các khung chứa phần thưởng cũng bị co dẹt lại, trong khi các ô vật phẩm con bên trong có kích thước cố định nên bị tràn ra ngoài.
- **Giải pháp**: Thiết lập `flex-shrink: 0;` cho tất cả các container phần thưởng (`.quest-reward-section`, `.reward-grid`, `.attendance-grid`) và các slot con cố định (`.reward-slot`, `.day-slot`). Để phần cuộn mô tả (`.detail-body-scroll`) có `flex-grow: 1; flex-shrink: 1;` tự động co giãn giải phóng không gian.

### 26. Thiết lập khoảng trống an toàn (Safe Zone) tránh nút Absolute
- **Tình huống**: Cụm nút Tìm kiếm và Làm mới ở hàng trên cùng của bạn bè bị xô đẩy và đè sát vào nút Close (X) màu đỏ.
- **Nguyên nhân**: Nút Close sử dụng `position: absolute; top: -8px; right: -8px;` lấn sâu vào bên trong panel khoảng 28px. Khi cụm tìm kiếm kéo dài hết chiều rộng khả dụng của panel, mép phải của nó sẽ va chạm trực tiếp với nút Close.
- **Giải pháp**: Luôn thiết lập `margin-right: 16px;` (hoặc một khoảng đệm an toàn theo bội số của 4) cho các container chứa cụm nút/điều hướng đặt ở góc phải để đẩy chúng sang trái một khoảng vừa đủ, tránh hoàn toàn vùng chiếm dụng của nút đóng absolute.

---

## 🔵 BÀI HỌC VỀ 3D FALLBACKS & BULLETPROOF PROGRAMMING (Cập nhật 03/06/2026)

### 27. Thiết lập Fallback tự động khi thiếu Assets 3D / Waypoints
- **Tình huống**: Cần phát triển và kiểm thử các tính năng gameplay (như Hướng dẫn đi theo NPC, Trồng trọt thu hoạch) khi chưa có 3D model chính thức từ Artist hoặc waypoints chưa được thiết lập trong scene.
- **Hậu quả**: Thử nghiệm bị gián đoạn, phát sinh lỗi `NullReferenceException` hoặc kẹt luồng state machine của hướng dẫn.
- **Giải pháp**: 
  - Áp dụng triết lý *lập trình chống đạn (bulletproof programming)*: Trong các hàm khởi tạo (`Awake`/`Start`), tự động kiểm tra tham chiếu. Nếu thiếu, tự sinh (`Instantiate` / `new GameObject`) các vật thể mockup cần thiết (như ô đất `FarmTile` hay `GuideNPC`) ở tọa độ an toàn.
  - Sử dụng mô hình hình học cơ bản (Capsule cho NPC, Cylinder/Cube cho cây trồng) cùng với các màu sắc vật liệu tiêu chuẩn đặc trưng (Nâu cho đất, Xanh cho thân cây, Cam cho cà rốt chín) để người chơi kiểm thử rõ ràng trạng thái.
  - Hủy bỏ (`Destroy`) collider của các mô hình hình học con để tia Raycast click chuột trúng trực tiếp vào collider chính của đối tượng cha.

---

## 🔴 BÀI HỌC VỀ INPUT SYSTEM (Cập nhật 04/06/2026)

### 28. KHÔNG dùng UnityEngine.Input (legacy) — dự án dùng New Input System
- **Tình huống**: Viết script test dùng `Input.GetKeyDown(KeyCode.F1)` để test UI popup.
- **Hậu quả**: `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class, but you have switched active Input handling to Input System package in Player Settings.`
- **Nguyên nhân**: Dự án đã chuyển hoàn toàn sang **New Input System** (`com.unity.inputsystem`). Player Settings → Active Input Handling = "Input System Package (New)". Khi đó toàn bộ API `UnityEngine.Input` cũ bị vô hiệu hóa.
- **Quy tắc**:
  - **TUYỆT ĐỐI KHÔNG** dùng `Input.GetKeyDown`, `Input.GetAxis`, `Input.GetMouseButton` hay bất kỳ API nào từ `UnityEngine.Input`.
  - Thay bằng `UnityEngine.InputSystem`:
    - `Keyboard.current.f1Key.wasPressedThisFrame` thay cho `Input.GetKeyDown(KeyCode.F1)`
    - `Mouse.current.leftButton.wasPressedThisFrame` thay cho `Input.GetMouseButtonDown(0)`
    - `Gamepad.current.buttonSouth.wasPressedThisFrame` thay cho `Input.GetButtonDown("Submit")`
  - Luôn null-check `Keyboard.current` / `Mouse.current` trước khi truy cập (có thể null nếu không có thiết bị kết nối).

---

## 🔴 BÀI HỌC VỀ UI CONSISTENCY (Cập nhật 04/06/2026)

### 29. PHẢI đọc USS popup cũ trước khi viết popup mới — đảm bảo phong cách đồng nhất
- **Tình huống**: Tạo ConfirmDialog.uss và RewardPopup.uss mới, nhưng viết theo trí nhớ/tự sáng tạo mà không đọc lại USS của Settings/Friends/Inventory đã có sẵn.
- **Hậu quả**: Popup mới bị lệch phong cách so với popup cũ — border-width, border-radius, shadow wrapper, :active translate, close button đều khác nhau. Mất thời gian sửa đi sửa lại nhiều lần.
- **Bảng giá trị chuẩn từ popup cũ** (Settings/Friends/Inventory):
  - Panel: `border-radius: 22px`, `border-width: 3px`, `border-color: #3D3535`
  - Shadow wrapper: `background-color: transparent`, `padding-right: 6px`, `padding-bottom: 6px` (giữ cấu trúc, không tô màu)
  - Close button: `border-radius: 10px`, `border-width: 3px`, `border-color: #3D3535`, `background-color: #FF4B4B`
  - Close shadow: `background-color: transparent`, `padding-right: 2px`, `padding-bottom: 2px`
  - Nút action: `border-width: 2-3px`, `border-color: #3D3535`, KHÔNG có shadow wrapper phía sau
  - `:active` effect: `translate: 1px 1px` (nhỏ) hoặc `translate: 2px 2px` (close button). KHÔNG dùng `4px 4px`.
  - `:active` close button: `background-color: #CC3333`
  - Transition: `transition-duration: 0.08s` (không phải 0.1s)
- **Quy tắc**:
  - **TRƯỚC KHI** viết bất kỳ USS mới nào, PHẢI đọc ít nhất 2-3 file USS popup cũ (SettingsPopup.uss, FriendsPopup.uss, InventoryPopup.uss) để lấy giá trị chuẩn.
  - Copy-paste các giá trị chung (overlay, shadow wrapper, close button, panel border) từ popup cũ, chỉ thay đổi phần nội dung riêng.
  - Sau khi viết xong, tự so sánh bảng giá trị với popup cũ trước khi giao cho anh test.
