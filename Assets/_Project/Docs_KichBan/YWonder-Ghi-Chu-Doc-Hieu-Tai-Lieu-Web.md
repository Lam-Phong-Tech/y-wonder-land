# Ghi chú đọc hiểu bộ tài liệu YWonderHub

> Ngày đọc và tổng hợp: 14/07/2026
>
> Phạm vi: 5 tài liệu Word về người dùng, đại lý, quản trị, cơ chế kỹ thuật và danh mục chức năng web.
>
> Nguyên tắc: các file `.docx` gốc vẫn là nguồn đối chiếu chính thức. File này là bản đọc hiểu phục vụ phát triển game/backend, không tự thay thế quyết định mới của BA, khách hàng hoặc hiện trạng production.
>
> Kiểm tra chuyển đổi: đủ 32 bảng và 1.296 đơn vị văn bản (đoạn/ô bảng); không phát hiện nội dung nguồn bị thiếu trong 5 bản Markdown.

## 0. Quyết định nghiệp vụ mới nhất ngày 15/07/2026

Quyết định này mới hơn và được ưu tiên hơn các diễn giải chưa chốt trong năm tài liệu nguồn:

1. Unity game chỉ có **một loại tiền tệ là Point**.
2. `UPoint` không còn là tiền tệ của game; số dư, nhãn và UI `UPoint` phải được loại bỏ.
3. Web là kênh đăng ký/đăng nhập và nạp tiền. Khi giao dịch nạp được web xác nhận/duyệt, hệ thống phải cộng trực tiếp số Point tương ứng vào đúng hồ sơ game đã liên kết.
4. Việc cộng Point phải diễn ra giữa web server và game server bằng giao dịch có định danh bất biến, chữ ký/xác thực và idempotency; Unity client không được tự quyết định số tiền cộng.
5. Game backend/PostgreSQL là nguồn số dư Point dùng trong gameplay. Web sở hữu yêu cầu thanh toán, trạng thái duyệt và lịch sử nạp; hai hệ thống không được duy trì hai số dư Point có thể chỉnh độc lập.

## 1. Tài liệu nguồn

| Tài liệu | Mốc trong tài liệu | Nội dung chính |
| --- | --- | --- |
| [YWonder-HDSD-Nguoi-Dung.md](YWonder-HDSD-Nguoi-Dung.md) | 18/06/2026 | Đăng ký, đăng nhập, ví, đầu tư, Partnerhub, nông trại web, KYC và khu vui chơi của MEMBER. |
| [YWonder-HDSD-Dai-Ly.md](YWonder-HDSD-Dai-Ly.md) | 18/06/2026 | Luồng AGENT, duyệt đại lý, hoa hồng F1-F6 và các chức năng dùng chung với MEMBER. |
| [YWonder-HDSD-Admin.md](YWonder-HDSD-Admin.md) | 18/06/2026 | Quyền ADMIN/SUPER_ADMIN, duyệt nạp-rút, người dùng, đại lý, KYC, cấu hình và audit. |
| [YWonder-Co-Che-Ky-Thuat.md](YWonder-Co-Che-Ky-Thuat.md) | 13/06/2026 | Cron tự duyệt KYC, đổi tên dự án và cơ chế trả hoa hồng đại lý. |
| [YWonder-Tong-Hop-Chuc-Nang.md](YWonder-Tong-Hop-Chuc-Nang.md) | 11/06/2026 | 91 chức năng web: 74 Live, 8 vừa cập nhật, 17 thuộc Phase 2 tại thời điểm lập tài liệu. |

## 2. Kết luận ngắn

1. **Web phải là nguồn danh tính người dùng lâu dài.** Người chơi đăng ký/đăng nhập trên `ywonder.net`; game nhận một định danh web ổn định rồi ánh xạ sang đúng một hồ sơ game.
2. **Web và game vẫn là hai miền trách nhiệm khác nhau.** Web sở hữu tài khoản, vai trò, KYC, thanh toán/nạp-rút, đầu tư và hoa hồng. Game sở hữu nhân vật, số dư Point gameplay, tài nguyên, túi đồ, farm và realtime.
3. **Game chỉ dùng Point.** Giao dịch nạp được web duyệt sẽ cộng vào chính số dư Point gameplay; `UPoint` và UI liên quan bị loại bỏ. Hợp đồng API và sổ giao dịch chống cộng trùng vẫn là điều kiện bắt buộc trước khi vận hành tiền thật.
4. **MEMBER và AGENT có hành trình kích hoạt khác nhau.** Thành viên thường đăng ký bằng mã giới thiệu hợp lệ; tài khoản đăng ký làm đại lý phải chờ Admin duyệt.
5. **Quyền root VPS không đồng nghĩa với tài khoản SUPER_ADMIN trên web.** Root quản trị máy chủ; SUPER_ADMIN là vai trò ứng dụng được lưu và kiểm tra trong hệ thống web.
6. **“Nông trại” trên web hiện được mô tả như một sản phẩm tài chính dùng USDT.** Nó không tự động đồng nghĩa với vòng lặp trồng trọt/chăn nuôi tương tác trong Unity.

## 3. Vai trò và địa chỉ truy cập

| Vai trò | Khu vực web | Cách có tài khoản/quyền |
| --- | --- | --- |
| MEMBER | `https://ywonder.net/vi/login` | Đăng ký bằng mã giới thiệu hợp lệ, xác thực theo cấu hình email của web. |
| AGENT | `https://agent.ywonder.net` hoặc đăng nhập chung rồi được điều hướng | Có thể được Admin tạo/kích hoạt ngay hoặc tự đăng ký rồi chờ Admin duyệt. |
| ADMIN | `https://admin.ywonder.net`, đăng nhập qua `/vi/backend/login` | Do SUPER_ADMIN tạo hoặc đổi vai trò; nên bật 2FA. |
| SUPER_ADMIN | Khu quản trị | Vai trò ứng dụng cao nhất, không được suy ra từ quyền SSH/root. |

Hệ thống web phân quyền theo `MEMBER`, `AGENT`, `ADMIN` và dùng subdomain để điều hướng đúng khu vực. Trang hồ sơ/thông báo là các trang dùng chung giữa vai trò.

## 4. Hành trình tài khoản

### 4.1 Thành viên thường

1. Nhận mã/link giới thiệu hợp lệ.
2. Nhập họ tên, ID đăng nhập, email, số điện thoại và mật khẩu.
3. ID đăng nhập đồng thời là mã giới thiệu của tài khoản.
4. Hoàn tất bước xác thực email theo cấu hình production.
5. Đăng nhập bằng email hoặc ID đăng nhập.
6. Khi vào game lần đầu qua Browser SSO, game tạo đúng một `playerId` và một nhân vật cho định danh web đó.

### 4.2 Đại lý

- Đăng ký MEMBER thông thường vẫn cần mã giới thiệu.
- Luồng **tự đăng ký làm Đại lý** được mô tả là không cần mã mời, nhưng sau OTP tài khoản chuyển sang trạng thái chờ Admin duyệt.
- Chỉ sau khi Admin duyệt, AGENT mới vào được khu đại lý.
- Vì vậy lỗi “tài khoản chờ duyệt/đã khóa” phải được phân biệt với sai mật khẩu hoặc lỗi máy chủ; game không nên gộp các lỗi này thành một thông báo chung.

### 4.3 Quản trị

- Admin/Super Admin duyệt đại lý, nạp/rút, KYC, đề xuất doanh nghiệp và quản lý vai trò/khóa tài khoản.
- Mọi thao tác quản trị cần audit log; không dùng API quản trị trực tiếp từ client game.

## 5. Các lớp tiền và tài sản trên web

| Loại | Ý nghĩa theo tài liệu |
| --- | --- |
| USDT | Tiền nạp/rút chính; có số dư riêng, dùng cho nuôi trồng web và nhận lãi. |
| Point | Điểm tiện ích web, dùng đầu tư, hoa hồng, đổi quà và nhiệm vụ. |
| YWH | Token được mua bằng USDT, có lịch khóa/nhả rồi có thể bán lại thành USDT. |
| Royalty Point | Điểm cổ tức từ staking; các chức năng staking/cổ tức phần lớn được ghi là Phase 2. |

Tài liệu ngày 18/06 ghi tỷ giá tham chiếu `1 Point = 1 YWH = 0,06 USDT`, đồng thời ghi tỷ giá Point có thể được Admin cấu hình. Không được hard-code tỷ giá này vào game nếu chưa có quyết định hiện hành từ BA và API trả tỷ giá từ server.

### Nạp/rút hiện cần Admin duyệt thủ công

- Người dùng chuyển USDT đúng mạng/địa chỉ và báo đã chuyển.
- Admin đối chiếu on-chain rồi duyệt để cộng số dư.
- Mỗi mã giao dịch chỉ được cộng một lần.
- Rút tiền dùng mật khẩu rút riêng và chờ Admin duyệt.
- Game client tuyệt đối không được tự gửi “số tiền cần cộng” làm dữ liệu đáng tin cậy.

## 6. Hoa hồng

Có hai lớp riêng, dễ nhầm nếu chỉ nhìn chữ F1:

1. **Hoa hồng trực tiếp F1 cho mọi vai trò:** ví dụ trong tài liệu gồm đăng ký `10.000 Point`, nạp `5%`, đầu tư `3%`, nhận lãi `10%`; các tỷ lệ phần trăm có thể do Admin cấu hình.
2. **Hoa hồng chuỗi chỉ dành cho AGENT:** `AGENT_F1 = 8%` trả ngay; `F2-F6 = 1%/cấp` xếp hàng và trả ngày 10/20/30 nếu đạt KPI.

`AGENT_F1` được đánh dấu đã trả ngay để cron không trả trùng. Các nhóm sản phẩm có thể bị Admin loại trừ khỏi hoa hồng.

## 7. KYC và tác vụ nền

- KYC ở trạng thái `PENDING` đủ 15 phút sẽ được cron quét mỗi 5 phút và tự duyệt, nên thời gian thực tế khoảng 15-20 phút.
- Admin có thể duyệt hoặc từ chối trước thời điểm tự duyệt.
- Thiết kế cron có tính idempotent: hồ sơ đã xử lý không bị duyệt lại.
- Tài liệu kỹ thuật cảnh báo cơ chế tự duyệt bỏ qua kiểm tra danh tính thật; đây là quyết định nghiệp vụ web, không phải logic game tự quyết định.
- Các cron khác gồm trả lãi hằng ngày, hoa hồng đại lý ngày 10/20/30 và vesting/reconcile theo lịch riêng.

## 8. Phân ranh web và game

| Dữ liệu/chức năng | Nguồn sở hữu nên là |
| --- | --- |
| Email, username, số điện thoại, mật khẩu hash, vai trò, trạng thái khóa | Web |
| KYC, referral tree, hoa hồng, yêu cầu thanh toán/nạp-rút và ví web ngoài game | Web |
| `playerId`, nhân vật, profile gameplay | Game server |
| Ví Point duy nhất của game, túi đồ, farm, cây/thú, daily limits | Game server |
| Chat, vị trí/hoạt ảnh người chơi ở đảo công cộng | Game realtime server |
| Liên kết web account -> game player | Bảng ánh xạ ổn định phía game, dùng định danh web bất biến |

Không dùng email làm khóa duy nhất lâu dài vì email có thể đổi. Game cần dùng ID nội bộ do web/API xác thực trả về. Client Unity không được truy cập trực tiếp DB web hoặc giữ secret dùng giữa hai server.

## 9. Ý nghĩa đối với đấu nối hiện tại

### Đăng ký/đăng nhập

- Browser SSO là hướng phù hợp: credential chỉ nhập trên web; game nhận kết quả xác thực ngắn hạn và đổi lấy phiên game.
- Một web account chỉ tạo một hồ sơ game; đăng nhập lại hoặc đổi thiết bị phải trở về cùng `playerId` và dữ liệu.
- Tài khoản web bị khóa/xóa mềm phải bị từ chối khi tạo phiên game mới; cần cơ chế thu hồi phiên đang chạy nếu nghiệp vụ yêu cầu tức thời.
- Trong giai đoạn chuyển tiếp có thể giữ luồng local làm rollback, nhưng luồng bàn giao chính phải ghi rõ người dùng đang chọn tài khoản web hay tài khoản game cũ.

### Nạp tiền vào game

Nghiệp vụ đã chốt ngày 15/07/2026: game chỉ có ví `Point`; khi tiền nạp trên web được xác nhận/duyệt, số Point tương ứng được cộng trực tiếp vào ví Point của đúng người chơi. `UPoint` không tham gia luồng này.

Luồng kỹ thuật an toàn cần triển khai:

1. Web xác nhận/duyệt giao dịch nạp và tính số Point theo cấu hình tỷ giá authoritative phía server.
2. Web server gửi sự kiện/API server-to-server đã ký, gồm định danh web bất biến, `transactionId`, số Point và thời điểm.
3. Game server ánh xạ web account sang `playerId`, ghi `game_transactions` và cộng Point trong cùng một transaction PostgreSQL.
4. `transactionId` là idempotency key; retry cùng giao dịch chỉ trả lại kết quả cũ, tuyệt đối không cộng lần hai.
5. Unity chỉ gọi bootstrap/refresh để nhận số dư mới; client không được gửi số Point muốn cộng.
6. Giao dịch từ chối/hoàn tác, quyền nạp, tỷ giá và nơi hiển thị lịch sử vẫn phải có hợp đồng cụ thể với phía web trước khi mở production.

## 10. Các điểm chưa thống nhất trong bộ tài liệu

1. Tiêu đề “Hai loại điểm” nhưng bảng ngày 18/06 liệt kê USDT, Point, YWH và Royalty Point. Với Unity, quyết định mới ngày 15/07/2026 đã chốt chỉ dùng Point; các loại còn lại không được đưa thành tiền tệ game.
2. Tài liệu tổng hợp 11/06 dùng tên nội bộ `GXL/GXLR`; tài liệu 18/06 dùng Point/YWH/Royalty Point. Quyết định 15/07 đã loại các tên này khỏi tiền tệ Unity; phần còn phải chốt với BA/web là tỷ giá, payload API, hoàn tác và lịch sử giao dịch.
3. Tài liệu tổng hợp nói xác thực email bằng link hiệu lực 24 giờ; hướng dẫn mới hơn nói mã 6 số hiệu lực 10 phút và có thể bỏ qua nếu SMTP chưa bật. Phải kiểm tra production/API thay vì hard-code theo một bản.
4. Tỷ lệ/tỷ giá trong hướng dẫn có chỗ là ví dụ hoặc cấu hình realtime; không được coi mọi con số là hằng số kỹ thuật.
5. Mục “Nông trại” trên web là đầu tư vật nuôi sinh lãi USDT, còn Unity là gameplay tương tác. Chưa có tài liệu xác nhận hai trạng thái này là cùng một hệ thống.
6. Tài liệu tổng hợp ghi web deploy Next.js/nginx/systemd với SQLite file-based; game backend hiện dùng PostgreSQL. Hai DB có trách nhiệm khác nhau và không được gộp chỉ vì cùng VPS.
7. Một số số thứ tự tiêu đề trong hướng dẫn người dùng/đại lý bị lệch (Phần 6 nhưng mục 5.1; Phần 8 nhưng mục 7.1). Bản Markdown giữ nguyên nội dung nguồn, không tự sửa nghiệp vụ.

## 11. Các chức năng web chưa nên coi là cam kết game Phase 1

Theo tài liệu 11/06, Quest, Redeem, staking GXLR, cổ tức và presale thuộc Phase 2. Việc chúng xuất hiện trong menu hoặc hướng dẫn không có nghĩa Unity Phase 1 phải triển khai ngay. Phase 1 game hiện nên tiếp tục ưu tiên:

- web account -> game account;
- một nhân vật/tài khoản;
- lưu profile, tiền gameplay, inventory và farm;
- realtime nhiều người;
- kiểm soát phiên và đồng bộ xuyên thiết bị.

Phần nạp Point đã được chốt về nghiệp vụ ngày 15/07/2026 nhưng vẫn là cổng triển khai/nghiệm thu riêng. Không coi là đã hoàn thành chỉ vì Browser SSO đã chạy; phần rút tiền khỏi game chưa được chốt trong quyết định này.

## 12. Bài kiểm thử rút ra từ tài liệu

1. MEMBER đăng ký bằng mã giới thiệu hợp lệ, xác thực thành công, Browser SSO vào game và chỉ tạo nhân vật một lần.
2. Đăng nhập bằng email và bằng ID phải cùng ánh xạ về một `playerId`.
3. Chọn “đăng nhập tài khoản khác” không được tự dùng session gần nhất.
4. AGENT đang chờ duyệt phải nhận lỗi đúng trạng thái; sau khi Admin duyệt mới đăng nhập được.
5. Tài khoản khóa phải không tạo được phiên game mới.
6. Cùng tài khoản trên EXE/APK phải thấy cùng nhân vật, số dư gameplay, túi đồ và farm; phiên mới thay phiên cũ theo chính sách một phiên.
7. Khi có đấu nối tiền: cùng một giao dịch web retry nhiều lần chỉ được cộng game đúng một lần.
8. Không log mật khẩu, token, secret, KYC hoặc dữ liệu ví nhạy cảm trong game/server log.

## 13. Quy tắc áp dụng về sau

- Khi tài liệu, runtime production và lời chốt mới của BA khác nhau: ghi lại chênh lệch và xin quyết định, không tự chọn con số có lợi cho việc code.
- Không sửa số liệu nguồn trong năm bản Markdown chuyển đổi. Mọi diễn giải hoặc cảnh báo đặt trong file tổng hợp này.
- Khi đổi API/web schema, cập nhật tài liệu hợp đồng và bài test trước khi xóa luồng rollback.
- Không tuyên bố hệ thống ví game-web hoàn chỉnh chỉ vì đăng nhập SSO đã hoạt động; đây là hai cổng nghiệm thu độc lập.
