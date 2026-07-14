# Báo cáo nội bộ Phase 1 - Y WONDER GREEN FARM

Ngày chốt: 11/07/2026  
Mục đích: báo cáo sếp về mức độ sẵn sàng của bản demo online. Không ghi mật khẩu, token hoặc SSH key trong tài liệu này.

## Kết luận ngắn

Game đã đạt mức **demo online có server thật** và có thể gửi sếp nghiệm thu nội bộ ngay. Chưa nên gọi là bản production hoàn chỉnh hoặc cam kết đã test đủ 20 thiết bị thật.

## Những phần đã hoạt động

- Game server đang chạy trên VPS riêng của công ty, dùng PostgreSQL và địa chỉ công khai `https://api.ywonder.net/game-api`.
- Người chơi có thể tự đăng ký, đăng nhập và mỗi tài khoản có dữ liệu nhân vật riêng.
- Mật khẩu được băm trên server; đăng nhập sai trả đúng thông báo và bị giới hạn số lần thử.
- Một tài khoản chỉ giữ một phiên online; đăng nhập ở thiết bị mới sẽ thay phiên cũ.
- Profile, Point, túi đồ, giao dịch shop và tiến trình tối thiểu được lưu; restart dịch vụ hoặc reboot VPS không làm mất dữ liệu kiểm thử.
- Người chơi khác mạng có thể gặp nhau, chat, nhìn thấy di chuyển/hoạt ảnh và cùng khai thác cây đá tại đảo công cộng.
- Cây/đá công cộng đồng bộ biến mất cho các máy và hồi sau 20 giây.
- Tutorial tài khoản mới đã bỏ đào khoáng, chỉ còn chặt cây, trồng trọt và xây chuồng; lỗi nhân nhiều dấu `!` đã sửa.
- Form đăng ký mobile không còn báo lỗi độ dài liên tục khi người chơi vẫn đang nhập.
- HTTPS/WSS, firewall, backup, rate limit, giới hạn dữ liệu gửi lên và log an toàn đã được kiểm tra.

## Bằng chứng kiểm thử

- Bài test tự động 20 client qua Internet đã pass: đăng ký, đăng nhập, bootstrap, realtime và giữ kết nối.
- Full Phase 1 pass: shop, idempotency, farm state API, đăng nhập lại, chat và thay phiên trùng tài khoản.
- Một EXE ở mạng A và một APK ở mạng B đã gặp nhau, chat và khai thác đồng bộ thành công.
- Unity C# compile không có lỗi; backend security smoke và Phase 1 smoke đều pass ở lần audit repo cuối.
- Checkpoint hotfix runtime `d7f75adf` đã được push đầy đủ lên nhánh GitHub `codex/backend-mvp`.

## Giới hạn cần nói rõ

- Mới test thật trên 2 thiết bị; bài 20 client còn lại là test tự động, chưa phải 20 người cầm máy chơi đồng thời.
- Một số phần farm/cây trồng/vật nuôi/câu cá vẫn dùng cache client và đang được chuyển dần sang quyền quyết định của server.
- Chưa có dashboard quản trị production cho sếp chỉnh dữ liệu; dashboard cũ chỉ dành cho local/dev và đang tắt trên production.
- Luồng tài khoản web thật và nạp/rút tiền chưa thuộc MVP này; bản hiện tại dùng tài khoản game tự đăng ký/cấp sẵn.
- Certificate hiện hợp lệ nhưng lịch tự gia hạn cần được xác minh trước khi ký nghiệm thu vận hành dài hạn.

## Nội dung có thể báo sếp

> Em đã đấu nối game với VPS và cơ sở dữ liệu thật. Bản hiện tại cho phép khách tự đăng ký, đăng nhập, lưu tài khoản và tài nguyên cơ bản, đồng thời chơi online khác mạng để gặp nhau, chat và tương tác ở đảo công cộng. Hệ thống đã vượt bài test tự động 20 kết nối và test thật EXE/APK trên hai mạng khác nhau. Em đề xuất gửi bản này để anh nghiệm thu nội bộ ngay; song song em sẽ hoàn tất kiểm tra thêm 4-5 thiết bị, lịch gia hạn bảo mật và các phần gameplay còn lưu phía máy người chơi trước khi gọi là bản production hoàn chỉnh.

## Danh sách cần xử lý tiếp

### P0 - trước khi gửi khách như bản gần-final

1. Xác minh `certbot` tự gia hạn certificate và chạy thử `renew --dry-run`.
2. Đóng gói một EXE/APK RC có số phiên bản rõ ràng, lưu checksum và đúng commit nguồn.
3. Smoke test lại chính bản RC: đăng ký, đăng nhập đúng/sai, tutorial, chat khác mạng, relogin còn Point/túi/farm.
4. Chụp backup PostgreSQL trước khi mở đợt test khách và xác nhận có thể rollback release.

### P1 - sau khi gửi sếp nghiệm thu nội bộ

1. Test thật 4-5 thiết bị khác mạng; theo dõi reconnect, ping và số phiên WebSocket.
2. Hoàn thiện `farm_state` hai chiều và đưa daily limit câu cá/đào mỏ về server-authoritative.
3. Audit vòng đời login/logout/đổi đảo để tìm callback, coroutine, singleton hoặc UI bị tạo lặp.
4. Bổ sung monitoring/alert cho game-server, PostgreSQL, HTTPS và dung lượng backup.
5. Thiết kế dashboard production có đăng nhập admin, phân quyền và audit log.

### P2 - gameplay/nội dung khách hàng còn tồn

1. Test và chốt sản lượng cụm chanh dây 20 cây; rà cây lâu năm còn lại.
2. Hoàn thiện các yêu cầu City/Farm còn mở: bố trí MiniGarden, bãi biển, bảng hiệu 3D, cây chặt, cối xay gió và bóng nhân vật.
3. Tiếp tục chuyển crop/animal/fishing/reward sang server-authoritative theo từng luồng nhỏ.
4. Tách các tính năng version 2: tiết kiệm, ví web/nạp rút, IAP, push notification và social nâng cao.

## Lưu ý repo

- Remote và local của `codex/backend-mvp` đã đồng bộ sau khi push.
- Worktree vẫn còn scene, model đất, ProjectSettings, Addressables và iOS export chưa commit; không được stage/revert hàng loạt vì đây là công việc Unity đang dở.
- `.tools`, `.claude` và `Assets/_Recovery` đang là dữ liệu local chưa track; cần quyết định ignore hoặc lưu riêng trước đợt dọn repo tiếp theo.
