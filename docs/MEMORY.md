# MEMORY.md — Đúc kết kinh nghiệm
> Cập nhật: 2026-06-01
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
