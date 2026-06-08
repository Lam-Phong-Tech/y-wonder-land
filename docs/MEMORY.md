# MEMORY.md — Đúc kết kinh nghiệm
> Cập nhật: 2026-06-05
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

### 3. Sức mạnh của Unity 6.3 LTS — Awaitable API
**Tình huống:** Ban đầu dự án ghi nhầm là dùng Unity 2022 LTS, khiến AI không dám dùng các API async hiện đại.
**Đính chính:** Dự án đang chạy trên **Unity 6.3 LTS**!
**Quy tắc:**
- CÓ THỂ sử dụng thoải mái `Awaitable`, `Awaitable.WaitForSecondsAsync()`, `Application.exitCancellationToken`.
- Đây là một lợi thế cực lớn giúp code async/await trong Unity sạch đẹp hơn rất nhiều so với UniTask hay Coroutine truyền thống.

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

---

## 🟡 BÀI HỌC VỀ HEADER LAYOUT & THÔNG TIN THỪA (Cập nhật 04/06/2026)

### 30. Element trong header dùng flex sẽ đè lên title absolute — phải dùng position: absolute cho cả hai
- **Tình huống**: Thêm pill số dư (`🪙 5,000 POS`) vào header Shop. Header dùng `flex-direction: row; justify-content: center;`, title dùng `position: absolute; left: 0; right: 0;` để căn giữa. Pill nằm trong flex flow → bị đặt ở giữa → đè lên chữ title.
- **Hậu quả**: Pill và title chồng lên nhau, không đọc được cả hai.
- **Giải pháp**: Pill cũng phải dùng `position: absolute; left: 12px;` để ghim vào góc trái, tránh ảnh hưởng flex flow. Title giữ nguyên `position: absolute; left: 0; right: 0;` + thêm `padding: 0 120px` để tạo khoảng trống cho pill và close button hai bên.
- **Quy tắc**: Khi header có title căn giữa tuyệt đối, MỌI element phụ (badge, pill, icon) cũng phải dùng `position: absolute` để ghim vào góc. KHÔNG để chúng trong flex flow.

### 31. KHÔNG hiển thị thông tin trùng lặp — nếu tab/sidebar đã thể hiện rõ thì bỏ label phụ
- **Tình huống**: Shop popup có tab Mua/Bán ở sidebar trái (nổi bật, có màu active). Đồng thời ở bottom bar lại hiển thị thêm "Chế độ: Mua" — thông tin hoàn toàn trùng lặp.
- **Hậu quả**: Giao diện rối, người dùng thắc mắc "Chế độ" là gì. Bottom bar chiếm thêm không gian vô ích.
- **Giải pháp**: Xóa hoàn toàn bottom info bar. Tab sidebar đã đủ rõ ràng thể hiện chế độ hiện tại.
- **Quy tắc**: Trước khi thêm label hiển thị trạng thái, tự hỏi: "Thông tin này đã được thể hiện ở đâu trên màn hình chưa?". Nếu đã có → KHÔNG thêm label trùng.

### 32. Tiêu đề header dài phải có biện pháp chống tràn
- **Tình huống**: Tiêu đề shop "HAI LÚA — VẬT TƯ NÔNG TRẠI" quá dài so với chiều rộng header, nhìn như sắp chìa ra ngoài viền.
- **Giải pháp**: 
  - Giảm `font-size: 20px → 18px` khi tiêu đề có thể dài.
  - Thêm `text-overflow: ellipsis; overflow: hidden; white-space: nowrap;` để cắt an toàn nếu tràn.
  - Thêm `padding: 0 120px` cho title absolute để chừa khoảng trống cho các element ở hai góc (pill trái, close button phải).

---

## 🟢 BÀI HỌC VỀ CHẾ ĐỘ XÂY DỰNG & TRẢI NGHIỆM NGƯỜI DÙNG (Cập nhật 06/06/2026)

### 33. KHÔNG bắt sự kiện Left Click đặt công trình khi chưa Pin (Ghim)
- **Tình huống**: Trong chế độ Xây dựng, khi Ghost preview đi theo chuột, nếu user bấm Left Click thì công trình tự động xây luôn.
- **Hậu quả**: Khi user click vào UI (chọn nhà khác, chuyển tab) thì vô tình đặt luôn nhà xuống map vì tia raycast vẫn xuyên qua UI.
- **Giải pháp (Chuẩn UX Mobile/PC)**: Click trái lần 1 để "Ghim" vị trí ngôi nhà (pin). Khi ghim xong, hiện 3 nút nổi cạnh nhà: [Dấu tích/Xây], [Xoay], [Hủy]. Click trái vào nút Tích mới thực sự trừ tiền và đặt công trình.

### 34. KHÔNG vẽ GL.Lines trong URP
- **Tình huống**: Dùng hàm `OnRenderObject()` và `GL.Lines` để vẽ lưới (Grid) 3D cho chế độ xây dựng.
- **Hậu quả**: Lưới hoàn toàn tàng hình trong Universal Render Pipeline (URP).
- **Giải pháp**: Trong URP, phải dùng `RenderPipelineManager.endCameraRendering`. Sử dụng `MeshTopology.Lines` để tạo Mesh, và vẽ nó bằng `Graphics.DrawMeshNow` với một Unlit Shader màu cơ bản.

### 35. KHÔNG gán trùng phím tắt cho 2 tính năng lớn
- **Tình huống**: Tính năng Câu cá gán phím `E`. Tính năng Popup Sự kiện cũng gán phím `E` ở ngoài HUD.
- **Hậu quả**: Khi user đứng gần hồ, bấm `E` thì bật cả popup câu cá lẫn popup sự kiện lên cùng lúc.
- **Giải pháp**: Map phím khác (`F` cho câu cá) hoặc dùng hệ thống Contextual Input chặn luồng sự kiện lẫn nhau.

### 36. Xóa bớt UI rối mắt — Thay bằng Contextual Menu
- **Tình huống**: Header của chế độ Xây dựng chứa 6-7 nút (Lưu, Hủy, Xoay, Nhấc, Xóa, Cất kho).
- **Hậu quả**: Chiếm diện tích, rối mắt, trải nghiệm kém so với các game Township, Hay Day.
- **Giải pháp**: Xóa hết các nút trên Header (chỉ chừa lại Tiền, Tiêu đề, Thoát). Các thao tác Xoay, Xóa, Nhấc được đặt vào Contextual Menu (menu ngữ cảnh) sẽ nổi lên NGAY CẠNH công trình khi user click vào một công trình ĐÃ XÂY.
- **Quy tắc**: Mọi title trong header PHẢI có `text-overflow: ellipsis` + `overflow: hidden`. Luôn test với tiêu đề dài nhất có thể xuất hiện (ví dụ: "KỶ NGUYÊN XANH — THẺ THÀNH VIÊN VIP").

---

## 🔴 BÀI HỌC VỀ CLOSE BUTTON & OVERFLOW (Cập nhật 04/06/2026)

### 33. Close button KHÔNG BAO GIỜ được nằm bên trong element có overflow: hidden
- **Tình huống**: Map popup có `map-container` với `overflow: hidden` (cần thiết để clip nội dung bản đồ). Close button đặt bên trong container với `position: absolute; right: -8px; top: -8px;` để nhô ra ngoài viền.
- **Hậu quả**: Nút X bị cắt góc (lẹm), trông xấu và khó nhận ra.
- **Giải pháp**: Thêm `map-wrapper` bọc bên ngoài container. Wrapper dùng `position: relative` và KHÔNG có `overflow: hidden`. Close button là con của wrapper, không phải container.
- **Cấu trúc chuẩn (bắt buộc)**:
  ```
  wrapper (position: relative, KHÔNG overflow)
    ├── container/panel (overflow: hidden — clip nội dung)
    └── close-shadow (position: absolute, right: -8px, top: -8px)
         └── close-btn
  ```
- **Quy tắc**: Trước khi đặt close button, kiểm tra TOÀN BỘ element cha xem có `overflow: hidden` không. Nếu có → phải tạo wrapper bên ngoài. Pattern chuẩn: `panel-shadow/wrapper → panel + close`. Đây chính xác là cách Settings/Shop popup hoạt động.

### 34. Close button phải nằm ở vị trí chuẩn, KHÔNG được giấu trong thanh header/toolbar
- **Tình huống**: Ban đầu nút X nằm bên trong `map-top-bar` (background semi-transparent dark). Nút đỏ nhỏ trên nền tối → rất khó thấy, người dùng không biết bấm đâu để thoát.
- **Hậu quả**: Anh Phong không tìm được nút thoát map.
- **Giải pháp**: Chuyển nút X ra góc phải trên của popup, nhô ra ngoài viền (`right: -8px; top: -8px`), z-index cao nhất. Đây là vị trí chuẩn mà TẤT CẢ popup trong game đã dùng.
- **Quy tắc**: Close button luôn:
  - Nằm ở **góc phải trên** popup, nhô ra ngoài viền
  - Kích thước **36×36px**, background **#FF4B4B**, border **3px #3D3535**
  - `z-index` cao nhất trong popup
  - KHÔNG nằm bên trong header/toolbar/thanh công cụ nào
  - Tham chiếu: SettingsPopup, ShopPopup, FriendsPopup, InventoryPopup đều dùng cùng pattern này

---

## 🟡 BÀI HỌC VỀ FLEX LAYOUT & CARD OVERFLOW (Cập nhật 04/06/2026)

### 35. Card dọc xếp nhiều tầng dễ tràn — ưu tiên layout ngang khi không gian hạn chế
- **Tình huống**: Package card Heo Đất xếp dọc 4 phần tử (icon → tên → rate → label). 3 card chiếm ~300px nhưng body chỉ còn ~320px → rate (+2%, +6%, +45%) tràn ra ngoài viền card.
- **Hậu quả**: Chữ rate hiện bên dưới border card, trông rất xấu dù đã thêm `overflow: hidden`.
- **Giải pháp**: Chuyển card sang **flex-direction: row** — icon bên trái, tên giữa, rate bên phải. 1 hàng duy nhất, tiết kiệm tối đa chiều cao. Ẩn label "lãi suất" dư thừa (`display: none`).
- **Quy tắc**: Khi thiết kế card trong vùng chiều cao hạn chế (sidebar, package list):
  - **Ước tính trước**: Tính tổng chiều cao = (card_height + margin) × số_card. So với chiều cao container còn lại.
  - **Nếu không vừa** → chuyển sang layout **ngang** hoặc giảm số tầng.
  - Card ngang chỉ cần ~50px/card, card dọc cần ~100px/card.

### 36. Flex row phải có min-height và flex-shrink: 0 cho text không bị đè
- **Tình huống**: Preview section Heo Đất có 3 dòng (Gốc / Lãi / Nhận về) dùng `flex-direction: row` + `justify-content: space-between`. Nhưng thiếu `min-height` → khi container bị ép, các row co lại và đè chồng lên nhau.
- **Hậu quả**: Text "100 POS", "+2 POS", "102 POS" chồng thành 1 cục.
- **Giải pháp**: 
  - Thêm `min-height: 18px` cho mỗi row.
  - Thêm `flex-shrink: 0` cho cả label và value để không bị co.
  - Thêm `align-items: center` cho row.
  - Thêm `-unity-text-align: middle-right` cho value.
- **Quy tắc**: Mọi flex row chứa label + value PHẢI có:
  ```
  min-height: 18px;
  align-items: center;
  ```
  Và mọi text element bên trong PHẢI có `flex-shrink: 0` nếu không muốn bị co lại.

---

## 🟡 BÀI HỌC VỀ UXML, FLEX-SHRINK, CARD ĐỒNG ĐỀU & INPUT SYSTEM (Cập nhật 04/06/2026)

### 37. UXML comment KHÔNG được chứa ký tự Unicode đặc biệt
- **Tình huống**: Comment `<!-- ═══ TAB 1 ═══ -->` trong EventPopup.uxml khiến UI Builder crash với lỗi "invalid UXML syntax".
- **Nguyên nhân**: Ký tự `═` (Box Drawing Double Horizontal, U+2550) không hợp lệ trong XML parser của Unity UI Builder.
- **Giải pháp**: Chỉ dùng ký tự ASCII thuần trong comment UXML: `<!-- TAB 1: EXCHANGE -->`.
- **Quy tắc**: Mọi comment trong file `.uxml` CHỈ dùng ASCII (a-z, A-Z, 0-9, dấu câu thường). Tuyệt đối KHÔNG dùng emoji, box drawing, hoặc ký tự Unicode đặc biệt trong comment.

### 38. Header và tab bar PHẢI có flex-shrink: 0
- **Tình huống**: Event popup header bị co rúm/biến mất khi tab Đổi quà hiển thị nhiều item. Tab Gói ưu đãi thì bình thường.
- **Nguyên nhân**: Header và tab bar mặc định `flex-shrink: 1`, nên khi body content có `flex-grow: 1` và vượt quá chiều cao panel → flex layout ép header co lại.
- **Giải pháp**: Thêm `flex-shrink: 0` cho cả `.event-header` và `.event-tab-bar`.
- **Quy tắc**: Mọi phần tử cố định (header, tab bar, footer) trong popup/panel PHẢI có `flex-shrink: 0`. Chỉ body/content area mới có `flex-grow: 1`.
  ```
  .popup-header { flex-shrink: 0; }
  .popup-tab-bar { flex-shrink: 0; }
  .popup-body { flex-grow: 1; overflow: hidden; }
  ```

### 39. Card đồng đều: dùng height cố định + spacer flex-grow
- **Tình huống**: 3 bundle cards trong Event popup có chiều cao khác nhau vì description dài ngắn khác nhau, và card "ĐÃ HẾT" ngắn hơn card có nút "MUA NGAY".
- **Đã thử**: `min-height: 260px` → không hiệu quả vì card có content dài vẫn vượt quá min-height.
- **Giải pháp**:
  1. Dùng `height: 280px` cố định (không phải min-height).
  2. Thêm `overflow: hidden` để không tràn.
  3. Trong Controller, thêm spacer element `flexGrow = 1` trước price section → đẩy price + button xuống đáy card.
- **Quy tắc**: Khi cần grid cards đồng đều chiều cao:
  ```
  CSS: .card { height: Xpx; overflow: hidden; }
  C#:  var spacer = new VisualElement(); spacer.style.flexGrow = 1; card.Add(spacer);
  ```

### 40. Project dùng New Input System → KHÔNG dùng UnityEngine.Input
- **Tình huống**: `Input.GetKeyDown(KeyCode.L)` gây `InvalidOperationException` vì project đã chuyển sang Input System package trong Player Settings.
- **Giải pháp**: Dùng `UnityEngine.InputSystem.Keyboard.current`:
  ```csharp
  var keyboard = UnityEngine.InputSystem.Keyboard.current;
  if (keyboard != null && keyboard.lKey.wasPressedThisFrame) { ... }
  ```
- **Quy tắc**: LUÔN kiểm tra `Keyboard.current != null` trước khi đọc phím. KHÔNG BAO GIỜ dùng `Input.GetKeyDown`, `Input.GetAxis`, hoặc bất kỳ API nào từ `UnityEngine.Input` class.

---

## 🔴 BÀI HỌC VỀ CHAT UI & SCROLLVIEW AUTO-SCROLL (Cập nhật 04/06/2026)

### 41. Tự động cuộn ScrollView xuống đáy (Autoscroll) qua GeometryChangedEvent
- **Tình huống**: Thêm tin nhắn mới vào khung chat scroll history nhưng ScrollView không tự động cuộn xuống dưới, người dùng phải kéo chuột thủ công để xem.
- **Nguyên nhân**: Khi thêm một phần tử UI mới vào ScrollView, kích thước thực tế (`layout.height`) của content container chưa kịp cập nhật ngay trong khung hình đó. Nếu gán `scrollOffset` lập tức, hệ thống sẽ cuộn theo chiều cao cũ trước khi có tin nhắn mới.
- **Giải pháp**: Đăng ký một callback tạm thời lắng nghe sự kiện thay đổi layout (`GeometryChangedEvent`) của ScrollView. Trong callback, cập nhật `scrollOffset = new Vector2(0, scrollview.contentContainer.layout.height)` và ngay lập tức hủy đăng ký callback (`UnregisterCallback`) để tránh đệ quy lặp vô hạn.
  ```csharp
  private void ScrollToBottom() {
      scrollerHistory.RegisterCallback<GeometryChangedEvent>(OnScrollHistoryGeometryChanged);
  }
  private void OnScrollHistoryGeometryChanged(GeometryChangedEvent evt) {
      scrollerHistory.UnregisterCallback<GeometryChangedEvent>(OnScrollHistoryGeometryChanged);
      scrollerHistory.scrollOffset = new Vector2(0, scrollerHistory.contentContainer.layout.height);
  }
  ```
- **Quy tắc**: Mọi thao tác cuộn tự động sau khi chèn thêm phần tử động vào ScrollView đều PHẢI sử dụng cơ chế lắng nghe `GeometryChangedEvent` và hủy đăng ký ngay sau đó.

### 42. Kiểm soát Focus Input TextField kết hợp Phím Enter
- **Tình huống**: Bấm `Enter` để mở rộng chat và focus viết chữ, nhưng khi viết xong bấm `Enter` tiếp lại bị double submit hoặc mất focus không mong muốn.
- **Giải pháp**: Sử dụng `FocusController` của UI Toolkit thông qua `root.focusController.focusedElement` để xác định trạng thái focus hiện tại của input field. Nếu input chưa được focus → gọi `.Focus()`. Nếu đang được focus và có chữ → gửi tin nhắn. Nếu rỗng → gọi `.Blur()` giải phóng focus và thu nhỏ chat.
- **Quy tắc**: Khi kết hợp phím cứng toàn cục với một TextField, phải kiểm tra tường minh phần tử đang được focus hệ thống trước khi quyết định hành vi.

### 43. Xóa triệt để các placeholder UI lỗi thời thay vì chỉ ẩn bằng Code runtime
- **Tình huống**: Thay thế thanh chat cũ (`MessagesBar`) bằng hệ thống Chat Panel mới linh hoạt hơn. Ban đầu chỉ dùng C# ẩn nó ở runtime trong `OnEnable()`.
- **Hậu quả**: Khi mở Unity Editor (Edit Mode) lúc chưa chạy game, code C# không thực thi khiến cả hai thanh chat đè chồng lên nhau, giao diện bị lộn xộn rất khó làm việc trong Editor.
- **Giải pháp**: Xóa bỏ hoàn toàn UI cũ lỗi thời khỏi template UXML chính (`GameHUD.uxml`), sau đó dọn dẹp sạch sẽ các biến tham chiếu, hàm query và callback đăng ký trong controller tương ứng (`GameHUDController.cs`).
- **Quy tắc**: Khi nâng cấp hoặc thay thế một cụm UI, phải thực hiện dọn dẹp tận gốc ở cả file UXML mẫu và file logic điều khiển, không lạm dụng việc ẩn giấu ở runtime để giữ không gian Editor luôn sạch sẽ.

### 44. Kim dao động QTE mượt mà bằng C# PingPong thay vì CSS Keyframes
- **Tình huống**: Cần thiết kế kim đo lực QTE dao động liên tục qua lại rất mượt mà trong UI Toolkit và cần đồng bộ tọa độ chính xác để tính toán trúng/hụt.
- **Vấn đề**: CSS Keyframes khó đồng bộ dữ liệu vị trí kim chính xác sang C# để tính toán trúng/hụt tại thời điểm click, đồng thời hỗ trợ keyframes của UI Toolkit ở các phiên bản Unity cũ thường kém ổn định.
- **Giải pháp**: Thực hiện dao động vị trí bằng C# trong `Update()` sử dụng phép toán `Mathf.PingPong(Time.time * speed, 100f)` và gán trực tiếp vào thuộc tính `style.left = Length.Percent(position)`. Vừa mượt mà, vừa tuyệt đối chính xác để check điều kiện trúng/hụt ở đầu C#.
- **Quy tắc**: Mọi thành phần UI cần tương tác dữ liệu chính xác theo tọa độ/vị trí thực tế (như kim QTE, thanh trượt game nhịp điệu) nên được điều khiển vị trí trực tiếp từ C# thay vì dùng CSS Animation tĩnh.

---

## 🟡 BÀI HỌC VỀ SPLASH SCREEN & SORT ORDER (Cập nhật 05/06/2026)

### 45. UXML comment ASCII-only KHÔNG áp dụng cho text content — chữ hiển thị PHẢI có dấu tiếng Việt
- **Tình huống**: Khi tạo SplashLoadingScreen.uxml, bé viết tất cả nội dung text hiển thị (thuộc tính `text=`) bằng tiếng Việt không dấu ("CUOC PHIEU LUU BAT DAU", "Click de bo qua") vì nhầm lẫn với quy tắc #37 yêu cầu comment UXML phải ASCII-only.
- **Hậu quả**: Người dùng Việt Nam nhìn thấy chữ không dấu rất khó đọc và thiếu chuyên nghiệp, mặc dù bản thân Unity hỗ trợ Unicode trong thuộc tính `text=` hoàn toàn bình thường.
- **Giải pháp**: Phân biệt rõ ràng:
  - **Comment** `<!-- ... -->` trong UXML: Chỉ dùng ASCII (quy tắc #37, vì XML parser Unity UI Builder có thể crash với ký tự đặc biệt trong comment).
  - **Thuộc tính text=** và chuỗi C# hiển thị: LUÔN LUÔN dùng tiếng Việt đầy đủ dấu ("Đang tải tài nguyên...", "Click để bỏ qua").
- **Quy tắc**: Trước khi bàn giao file UXML hoặc C# có chữ tiếng Việt, kiểm tra nhanh toàn bộ thuộc tính `text="..."` và chuỗi `string` hiển thị xem đã có đầy đủ dấu chưa. Không bao giờ giao chữ không dấu cho người dùng cuối.

---

## 🟡 BÀI HỌC VỀ BUILD MODE & UXML ENTITY (Cập nhật 05/06/2026)

### 46. Chức năng Build Mode chưa hoàn thiện — chờ model 3D
- **Trạng thái hiện tại**: Hệ thống Build Mode đã có đầy đủ khung UI (overlay, category sidebar, item bar, tooltip, control bar), hệ thống 3D (BuildGridManager, GhostPlacementController, BuildCameraController, BuildGridRenderer), và tích hợp vào GameHUD (nút 🔨, phím B).
- **Chưa hoàn thiện**:
  - Ghost placeholder đang dùng **Primitive Cube** thay vì model 3D thật.
  - Chưa test kỹ: ghost xanh/đỏ khi đặt đúng/sai vị trí, cho xây, cho hủy, cho thay đổi vị trí.
  - Chưa có logic trừ POS khi đặt qua ghost (chỉ có mockup UI).
  - Chưa có hệ thống lưu/load bố cục nông trại.
- **Kế hoạch test sắp tới**:
  - [ ] Ghost đổi màu xanh (hợp lệ) / đỏ (trùng ô hoặc ngoài grid) khi di chuyển chuột.
  - [ ] Click trái xác nhận đặt công trình, Cube placeholder xuất hiện tại ô grid.
  - [ ] Click phải hủy ghost.
  - [ ] Phím R xoay ghost 90°.
  - [ ] Đặt trùng ô đã có công trình → ghost đỏ, không cho đặt.
  - [ ] Di chuyển nhân vật → grid bám theo liên tục.
- **Quy tắc**: Khi swap model 3D thật vào, chỉ cần thay Cube trong `GhostPlacementController.CreateGhostObject()` và `SpawnPlacedBuilding()` bằng prefab tương ứng.

### 47. UXML KHÔNG được dùng XML entity reference cho emoji ngoài BMP (U+FFFF)
- **Tình huống**: Viết `&#x1F504;` (🔄), `&#x1F5D1;` (🗑️), `&#x1F4BE;` (💾) trong file BuildModeOverlay.uxml.
- **Hậu quả**: Unity UI Builder crash: "Failed to open asset. This may be due to invalid UXML syntax."
- **Nguyên nhân**: Unity XML parser không hỗ trợ numeric character references cho code point trên U+FFFF (Supplementary Multilingual Plane). Các ký tự như emoji 🔄🗑️💾 đều nằm ở U+1Fxxx.
- **Giải pháp**: Dùng ký tự BMP-safe (U+0000–U+FFFF) trực tiếp trong thuộc tính `text=`:
  - `⌂` (U+2302) thay cho 🏠
  - `✿` (U+273F) thay cho 🎀
  - `⟳` (U+27F3) thay cho 🔄
  - `↩` (U+21A9) thay cho ↩️
  - `✕` (U+2715) thay cho ✕
- **Quy tắc**: Trong file `.uxml`, KHÔNG BAO GIỜ dùng `&#xNNNNN;` với N > 4 chữ số hex. Nếu cần icon, dùng ký tự BMP-safe hoặc chữ text thường. Trong file `.cs` thì dùng `\U0001Fxxx` bình thường vì C# string hỗ trợ đầy đủ UTF-32.

### 48. Project dùng URP — KHÔNG BAO GIỜ dùng Shader.Find("Standard")
- **Tình huống**: Tạo material bằng `new Material(Shader.Find("Standard"))` cho NPC fallback, FarmTile, GhostPlacement.
- **Hậu quả**: Mọi object hiện màu TÍM/HỒNG (shader lỗi) vì URP không hỗ trợ Built-in Standard shader.
- **Giải pháp**: Luôn dùng `Shader.Find("Universal Render Pipeline/Lit")` cho opaque objects, hoặc `Shader.Find("Universal Render Pipeline/Unlit")` cho UI/overlay objects.
- **Quy tắc**: Grep toàn bộ project tìm `Shader.Find("Standard")` → đổi hết sang URP trước khi commit.

### 49. Floating Name Tag — Dùng TextMeshPro 3D, KHÔNG dùng TextMesh hay UI Toolkit WorldToScreenPoint
- **Tình huống**: Cần hiển thị tên nhân vật nổi trên đầu trong 3D.
- **Cách SAI 1**: UI Toolkit + WorldToScreenPoint mỗi frame → text biến mất khi di chuyển.
- **Cách SAI 2**: TextMesh cơ bản → chữ 3D thô, không sắc nét, khó đọc.
- **Cách ĐÚNG**: TextMeshPro 3D (TMPro.TextMeshPro, KHÔNG phải TextMeshProUGUI) + billboard rotation.
- **Chống frustum culling**: `enableWordWrapping=false`, `overflowMode=Overflow`, `allowOcclusionWhenDynamic=false`, `sizeDelta=(20,5)`, `ForceMeshUpdate()`.
- **Camera**: Luôn refresh `Camera.main` mỗi frame (GameManager swap cameras giữa states).
- **Scale**: KHÔNG scale theo khoảng cách (để nhỏ tự nhiên khi xa). Thêm fade opacity khi xa.

### 50. Tiếng Việt có dấu trong UXML — Set từ C# code, KHÔNG gõ trực tiếp trong UXML
- **Tình huống**: Gõ text tiếng Việt có dấu trực tiếp trong file `.uxml`.
- **Hậu quả**: Chữ hiển thị không dấu hoặc ký tự lạ do encoding.
- **Giải pháp**: Trong UXML dùng placeholder (text="."), rồi set text tiếng Việt từ C# controller bằng Unicode escapes (`\u1EA0`, `\u00c2`...) hoặc string literals.
- **Lưu ý**: Rule #45 (UXML comment ASCII-only) áp dụng cho COMMENTS, không áp dụng cho text content. Nhưng vì UXML encoding không đáng tin cậy, set từ code cho an toàn.

---

## 🟢 BÀI HỌC VỀ BẢO TRÌ CODE & ĐẤU NỐI (Cập nhật 07/06/2026)

### 51. TextMeshPro Obsolete API: `enableWordWrapping`
- **Tình huống**: Sử dụng `enableWordWrapping = false` cho `TextMeshPro` dẫn đến cảnh báo Obsolete Warning trong console của các bản Unity/TMP mới.
- **Giải pháp**: Thay thế bằng API mới `textWrappingMode = TMPro.TextWrappingModes.NoWrap;` (hoặc `Normal` nếu muốn bật). Điều này giúp code tương thích tốt với tương lai và giữ console sạch sẽ.

### 52. Tận dụng Mockup UI cũ khi đấu nối hệ thống (Integration)
- **Tình huống**: Yêu cầu triển khai hệ thống Câu Cá (Phase 6). AI định lên plan code từ đầu.
- **Phát hiện**: Trước đó, file `FishingOverlay.uxml` và `FishingOverlayController.cs` đã được xây dựng sẵn dưới dạng Mockup (dùng biến ảo `premiumBait = 2`).
- **Bài học**: TRƯỚC KHI tạo UI mới hoặc viết controller mới cho một tính năng, luôn dùng `grep_search` quét thư mục `Assets/UI/` để xem tính năng đó đã từng được làm mockup hay chưa. Việc tận dụng mockup cũ và chỉ việc thay đổi logic đọc data thực tế (như gọi `InventoryManager.Instance.GetItemQuantity`) tiết kiệm đến 80% thời lượng. Khai báo biến thừa trong Mockup (như `premiumBait`) cần được xóa sạch để tránh rác.
