# Y WONDER GREEN FARM - Hướng dẫn bàn giao kiểm thử Phase 1

Ngày chuẩn bị: 11/07/2026
Phạm vi: Windows EXE, Android APK, tài khoản game, lưu dữ liệu cơ bản và realtime ngoài mạng.

## 1. Gói gửi cho tester

Gửi đủ các mục sau trong cùng một lần bàn giao:

1. File ZIP bản Windows, gồm file EXE và toàn bộ thư mục dữ liệu đi kèm.
2. File APK Android cùng phiên bản với bản Windows.
3. File `YWonder_Phase1_TestCases_2026-07-11.xlsx`.
4. Tên phiên bản/build, ngày build và người phụ trách nhận lỗi.
5. Link nhóm chat hoặc hệ thống dùng để gửi ảnh, video và theo dõi lỗi.

Không gửi riêng file EXE vì game sẽ thiếu thư mục dữ liệu và không chạy được.

## 2. Tài khoản kiểm thử

- Mỗi tester tự tạo một tài khoản riêng để tránh ghi đè dữ liệu của nhau.
- Tên đăng nhập dài từ 9 đến 20 ký tự.
- Mật khẩu dài từ 9 đến 20 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt.
- Không dùng chung tài khoản, trừ test `AUTH-018` về thay thế phiên đăng nhập.
- Tài khoản có sẵn và mật khẩu, nếu cần, phải gửi qua kênh riêng; không ghi vào repo hoặc workbook.

## 3. Thứ tự kiểm thử

1. Mở sheet `Hướng dẫn` và điền thông tin bản build.
2. Điền thiết bị và loại mạng vào sheet `Thiết bị & mạng`.
3. Chạy toàn bộ test `P0` trước, sau đó tới `P1`; `P2` chạy khi còn thời gian hoặc có máy phù hợp.
4. Với test realtime, tối thiểu dùng một máy tính qua mạng A và một điện thoại qua mạng B.
5. Cập nhật cột `Trạng thái`, `Kết quả thực tế`, `Bằng chứng`, `Mã lỗi`, `Tester` và `Ngày test`.
6. Nếu test không đạt, tạo một dòng tương ứng trong sheet `Defect Log`.
7. Test lead xem sheet `Tổng hợp` và ký xác nhận sau khi kiểm tra bằng chứng.

## 4. Quy tắc báo lỗi

Một báo cáo lỗi hợp lệ cần có:

- Mã test case và tên bản build.
- Thiết bị, hệ điều hành và loại mạng.
- Các bước tái hiện theo đúng thứ tự.
- Kết quả mong đợi và kết quả thực tế.
- Tần suất: luôn luôn, thường xuyên, thỉnh thoảng hoặc chỉ một lần.
- Ảnh hoặc video thấy rõ toàn màn hình và thời điểm xảy ra lỗi.

Tên bằng chứng nên theo mẫu:

```text
TCID_ThietBi_Ngay_GhiChu
RT-002_SamsungA54_20260711_chat.mp4
```

Không xóa lỗi cũ sau khi dev sửa. Chuyển trạng thái sang `Chờ retest`, giữ bằng chứng cũ và bổ sung kết quả kiểm tra lại.

## 5. Tiêu chí đề nghị để chấp nhận bản build

- 100% test `P0` đã chạy và đạt.
- Không còn lỗi `Nghiêm trọng` hoặc `Cao` đang mở.
- Tỷ lệ đạt trên số test đã có kết quả đạt tối thiểu 95%.
- Bắt buộc đạt các luồng: đăng ký, đăng nhập, đăng xuất, lưu tiền/túi/farm, khóa phiên trùng, EXE/APK khác mạng, chat, nhìn thấy nhau và đồng bộ cây/đá.

## 6. Giới hạn Phase 1

- Chưa kiểm thử đủ 20 thiết bị vật lý; đã có bài test tự động 20 client và test thật với số lượng thiết bị nhỏ.
- Nạp/rút tiền, thanh toán và liên kết tài khoản web chính thức chưa thuộc phạm vi MVP này.
- Dashboard admin production chưa bàn giao cho tester.
- Farm của từng tài khoản là vùng riêng; hai người không nhìn thấy nhau trong farm không mặc định là lỗi.
- Những thay đổi phạm vi phải được test lead xác nhận trước khi đánh dấu `Bỏ qua`.

## 7. Bảo mật

Tuyệt đối không đưa vào tài liệu hoặc tin nhắn tester:

- Mật khẩu VPS hoặc tài khoản `root`.
- SSH key.
- JWT/API secret.
- Chuỗi kết nối PostgreSQL hoặc `DATABASE_URL`.
- Tài khoản thật của khách hàng.

Tester chỉ cần bản game, tài khoản test và địa chỉ backend đã được tích hợp sẵn trong build.
