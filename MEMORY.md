# MEMORY - Những Bài Học Rút Ra

## Về Hệ Thống Sự Kiện (Events) & Vòng Đời Unity
- **Vấn đề:** Không thể nghe ngóng sự kiện `OnAnimalBought` từ `AnimalManager` lúc game vừa chạy (hàm `Start()` của `TutorialManager`).
- **Nguyên nhân:** Có thể do `AnimalManager` chưa được khởi tạo, hoặc GameObject chứa nó đang bị vô hiệu hóa lúc tải Scene, khiến `Instance` trả về `null`.
- **Bài học:** Khi sử dụng mô hình Singleton (`Instance`), đặc biệt với các Manager không nằm trong Bootstrapper (tạo động lúc runtime), tuyệt đối không nên đăng ký Event (`+=`) ngay trong hàm `Awake` hay `Start` nếu Manager đó có thể chưa sẵn sàng. Việc đăng ký (subscribe) nên được dời đến lúc **hành động cụ thể thực sự cần diễn ra** (Late Binding) - ví dụ như lúc người chơi đi vào phạm vi của Chuồng Thú. Điều này đảm bảo an toàn 100%.

## Về Thiết Kế Lời Thoại NPC
- **Vấn đề:** NPC hay có cảm giác bị "vô hồn" hoặc lặp đi lặp lại cùng một câu nói làm phiền người chơi.
- **Bài học:** Cần chia nhỏ các Pool lời thoại theo **State** (Trạng thái) thay vì dồn chung 1 mảng.
  - `walkDialogues`: Lời thoại mồi khi đang đi đường.
  - `actionDialogues`: Lời thoại giao việc chính (nói 1 lần đầu).
  - `idleWarningDialogues` / `waitPlayerDialogues`: Lời thoại châm biếm, thúc giục nếu người chơi đứng yên quá lâu không chịu làm.
  - Sử dụng Timer đếm ngược (AFK Timer) để kích hoạt thoại thúc giục thay vì spam mỗi frame.

## Về Trải Nghiệm Tutorial (Game Feel)
- **Vấn đề:** Bắt người chơi chờ 60 giây ngoài đời thực chỉ để xem 1 củ cà rốt lớn lên trong lúc đang học chơi là một cực hình, dễ gây hiểu lầm là game bị lỗi.
- **Bài học:** Mọi thông số thời gian (Growth Time, Crafting Time...) trong Tutorial **BẮT BUỘC** phải được Override thành siêu ngắn (ví dụ 5 giây). Người chơi cần có *Phản hồi tức thì* (Instant Feedback) trong giai đoạn học luật game.

## Về UI Feedback
- **Vấn đề:** Người dùng click chuột liên tục 100 lần vào ô đất vì tưởng game không nhận lệnh.
- **Bài học:** Bất cứ khi nào có yếu tố chờ đợi (Timer), **phải có Progress Bar hoặc Countdown Number** hiện lên rõ ràng để chứng minh cho người chơi thấy "hệ thống đang hoạt động và đang xử lý". Đừng chỉ dựa vào Console log.

## Về Mô Hình Chuồng Từ Hàng Rào (20/06)
- **Vấn đề:** Mất CẢ CHỤC vòng debug vì hiểu sai bản chất hàng rào: cứ tưởng "chuồng = vòng nhiều ô rào quây 1 vùng đất trống ở giữa" → thử flood-fill ô-trống, raycast collider tường... đều sai.
- **Sự thật:** Hàng rào của dự án này (FenceAutoConnect) là **1 hộp vuông NẰM TRÊN đúng 1 ô** → **Ô CÓ RÀO chính là ô chuồng** để thả thú. Nhiều ô-rào kề nhau = chuồng to.
- **Bài học:** Trước khi code hệ phụ thuộc hình học prefab (rào/tường/ô), **hỏi/nhìn kỹ prefab THẬT chiếm bao nhiêu ô + collider ở đâu** rồi mới chọn thuật toán. Đừng giả định mô hình "ring + interior" mặc định. Dùng **gizmo `OnDrawGizmos`** để nhìn trạng thái ô (tagged/occupied/animal) là cách debug nhanh nhất.

## Về Lookup Dữ Liệu Không Phụ Thuộc Singleton Trong Scene
- **Vấn đề:** UI túi/shop không hiện thông tin con vật vì `AnimalManager.Instance` null (quên gắn vào scene).
- **Bài học:** Hàm tra cứu data (def/asset) nên có **fallback load thẳng từ `Resources`** khi Manager-singleton chưa có trong scene (vd `AnimalManager.LookupDefinition` → `Resources.Load`). Như vậy tính năng không chết chỉ vì thiếu 1 GameObject manager, đỡ phụ thuộc setup Editor.
