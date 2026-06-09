# 🌿 Y WONDER LAND - MEMENTO PROTOCOL (BẢN GHI NHỚ TIẾN ĐỘ)
*Dự án: BaChuKhuRung3D (Game nông trại 3D)*
*Ngày cập nhật: 08/06/2026*

## 1. Tóm tắt Dự án
Đây là một tựa game mô phỏng nông trại 3D (tương tự Hay Day / Đảo Gấu). Các core mechanic chính bao gồm: 
- Trồng trọt (Cuốc đất, gieo hạt, tưới nước, thu hoạch)
- Chăn nuôi (Mua động vật từ Shop, đặt vào chuồng)
- Hệ thống UI, Inventory, Economy, Tools.

## 2. Tiến độ Hiện tại (Đã hoàn thành)
Trong phiên làm việc này, chúng ta tập trung vào việc **Làm mượt và hoàn thiện hệ thống Tutorial (Hướng Dẫn Tân Thủ) qua NPC tên Lâm**.

### Trạm 1: Hướng Dẫn Trồng Trọt (Hoàn chỉnh)
- [x] Tạo `TutorialManager` và `GuideNPC`.
- [x] NPC có AI tự động di chuyển đến mảnh đất.
- [x] Thêm Lời Thoại Động (`actionDialogues`, `waitPlayerDialogues`, `idleWarningDialogues`). Nếu người chơi AFK, anh Lâm sẽ liên tục châm chọc hối thúc.
- [x] Rút ngắn thời gian trồng trọt cho Tutorial: Chỉ mất 5 giây để cây lớn.
- [x] Cập nhật thanh Quest HUD và Banner thông báo mỗi khi hoàn thành 1 thao tác (Cuốc -> Gieo -> Tưới -> Thu hoạch).

### Trạm 2: Hướng Dẫn Chăn Nuôi (Hoàn chỉnh)
- [x] Cập nhật code để sau khi thu hoạch cà rốt, anh Lâm dẫn người chơi sang khu vực Chuồng Thú (`AnimalPen`).
- [x] Hook event `AnimalManager.OnAnimalBought` vào TutorialManager.
- [x] Khi người chơi mở Shop và mua 1 con vật nuôi bất kỳ (bò, lợn, gà), hệ thống tự động nhận diện, hoàn thành tác vụ và anh Lâm sẽ khen ngợi.

## 3. Các Vấn Đề Tồn Đọng / Lưu Ý
- **Lỗi cũ đã fix:** Lỗi `AnimalManager.Instance` bị null lúc `Start` đã được xử lý bằng cách bind event vào khoảnh khắc người chơi chạy tới chuồng thú thay vì bind từ đầu.
- **Node 3 (Trạm 3) chưa có:** Hiện tại sau khi người chơi mua thú, Tutorial bị "cắt cái rụp" và gán trạng thái `Complete` để tạm kết thúc. Về sau có thể mở rộng thêm Trạm 3 (Hướng dẫn mua bán, xây dựng...).

## 4. Bước Tiếp Theo Cần Làm (Cho phiên chat mới)
- [ ] Thiết kế và lập trình **Trạm 3** của Tutorial (nếu muốn dẫn người chơi đi xem Chợ / Xưởng chế tạo).
- [ ] Kiểm tra lại UI/UX khi chơi trên thiết bị thực tế, bổ sung hiệu ứng Particle (như lá rụng, lấp lánh khi thu hoạch).
- [ ] Mở rộng cơ chế chăm sóc động vật (cho ăn, vắt sữa bò, nhặt trứng gà).

> [!TIP]
> **Cho AI mới:** Hãy đọc kỹ phần 2 để nắm được flow của TutorialManager, script chính nằm ở `Assets\_Project\Scripts\Tutorial\TutorialManager.cs`. Game sử dụng hệ thống Event để lắng nghe các hành động của Player.
