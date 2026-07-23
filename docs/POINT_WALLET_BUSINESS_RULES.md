# Quy Tắc Nghiệp Vụ Ví Point Web - Game

Ngày ghi nhận: 16/07/2026; cập nhật trực tiếp từ khách: 17-19/07/2026.
Nguồn: nội dung anh chuyển lại từ trao đổi với `@Đặng Trung Hiếu` và các câu trả lời trực tiếp tiếp theo của khách.
Trạng thái: **đã ghi nhận quyết định nghiệp vụ; chưa phải hợp đồng API, chưa triển khai production**.

## 1. Các quyết định đã xác nhận

| Chủ đề | Xác nhận nghiệp vụ |
|---|---|
| Point trên web và Point trong game | Là cùng một loại tiền. |
| Số dư hiển thị | Người dùng phải nhìn thấy cùng một số dư ở web và game, giống hai giao diện của cùng một ví. |
| Số dư Point đang có trên web | Không tạo bản sao hoặc tách ví khi kết nối game; web và game cùng hiển thị số dư của một ví. |
| Nguồn tạo Point | Người dùng nạp USDT và đổi sang Point. |
| Chuyển đổi YWH | Cho phép đổi `YWH -> Point` và `Point -> YWH`. |
| Tỷ giá hiện tại | `1 USDT = 26,5 Point`; `1 YWH = 1,59 Point`. Giữ cấu hình có version để Admin có thể thay đổi về sau mà không sửa giao dịch cũ. |
| Đổi Point ra USDT/rút | Có cho phép. Quy trình, phí, hạn mức và phê duyệt chưa được cung cấp. |

## 2. Cách hiểu mới thay thế phần trả lời còn mơ hồ trước đây

Lượt hỏi đầu từng nhận câu trả lời `không` cho nhóm sau:

- Hoa hồng và thưởng giới thiệu.
- Thưởng nhiệm vụ.
- Đầu tư, hoàn vốn và nhận lãi trên web.
- Đổi quà hoặc staking.
- Đổi YWH sang Point.
- Chuyển Point cho người khác.

Các câu trả lời trực tiếp sau đó đã thay thế cách hiểu trên đối với hai nghiệp vụ:

- `YWH -> Point` thành công phải tăng ví Point chung; web và game cùng hiển thị số dư mới.
- Chuyển Point phải giảm người gửi, tăng người nhận và cùng phản ánh trên web/game.

Vì vậy không được triển khai danh sách cũ theo nghĩa "web đổi Point nhưng game không đổi". Thưởng nhiệm vụ, đầu tư, staking và các thưởng web khác vẫn cần phân loại rõ tài sản trả thưởng và nguồn Point trước khi nối vào ví chung.

## 3. Hoa hồng tiêu dùng trong game

- Web hiện tại giữ nguyên cơ chế tài sản của HUB.
- Khi người chơi tuyến dưới tiêu dùng trong game, người giới thiệu phải nhận hoa hồng tương tự hệ thống HUB.
- Hoa hồng có 6 cấp: cấp trực tiếp nhận `8%`; 5 cấp tiếp theo mỗi cấp nhận `1%`; tổng tối đa `13%` trên cùng giá trị tiêu dùng đủ điều kiện.
- VIP được tính cộng dồn toàn thời gian khi tài khoản đã tiêu đủ `2.650 Point` có nguồn USDT; không yêu cầu tiêu trong một giao dịch.
- Point nguồn USDT nhận từ người khác qua chuyển khoản vẫn được cộng vào tiến độ VIP của người nhận khi người đó tiêu dùng.
- Khi một giao dịch đã cộng tiến độ VIP bị refund, hệ thống phải trừ lại tiến độ và thu hồi VIP nếu tổng còn lại không đủ điều kiện.
- Khi B tiêu dùng, hoa hồng của A vẫn được tính và ghi vào bể riêng kể cả A hoặc B chưa VIP; khoản này chưa được cộng vào số dư có thể sử dụng hoặc rút.
- Từng khoản hoa hồng của A chỉ được mở khi cả A (người nhận/tuyến trên) và B (người tiêu dùng/tuyến dưới tạo ra khoản đó) đều đã VIP.
- Khi đủ điều kiện, toàn bộ hoa hồng lịch sử đang khóa tương ứng được mở, không chỉ các khoản phát sinh sau thời điểm đạt VIP.
- Loại tài sản trả hoa hồng phụ thuộc nguồn Point đã được tiêu:
  - Point được đổi từ USDT rồi tiêu trong game: trả hoa hồng bằng `USDT`.
  - Point do nuôi trồng, sản phẩm hoặc phần thưởng trong game tạo ra rồi tái tiêu dùng: trả hoa hồng bằng `Point`.
- Hoa hồng bằng Point giữ phần thập phân. Ví dụ `156 Point x 3% = 4,68 Point`; không làm tròn thành `4` hoặc `5 Point`.
- Nếu giao dịch đã trả hoa hồng rồi sau đó được hoàn/hủy, hệ thống phải thu hồi hoa hồng tương ứng.
- Khách hỏi có thể chỉ trả hoa hồng sau khi giao dịch thành công hay không. Câu trả lời kỹ thuật là có và đây là hướng bắt buộc: giao dịch lỗi hoặc hủy trước commit không được tạo payout; payout chỉ được phát từ giao dịch mua đã commit thành công.
- Payout phải chờ ít nhất khoảng `10 phút` hoặc lâu hơn để hệ thống xác nhận trạng thái giao dịch trước khi trả. Khoảng chờ nên là cấu hình có version; candidate mặc định `600 giây`, chưa bật production.
- Các ví dụ bắt buộc gồm:
  - Mua vật nuôi.
  - Mua cây trồng dài ngày và ngắn ngày.
  - Mua mồi câu.
  - Mua lượt vòng quay.
  - Mua lượt đào khoáng.
- Phạm vi được mô tả chung là mọi giao dịch `tiêu dùng trong game`.

Quy tắc tỷ giá hiện tại, giữ số lẻ, hoàn/hủy, FIFO, transfer giữ nguyên nguồn, 6 cấp hoa hồng và ngưỡng VIP đã được chốt. Điều kiện mở khóa là cả A và B đều VIP; tiến độ `2.650 Point` cộng dồn từ mọi Point còn giữ nguồn USDT, kể cả nhận qua transfer. Refund phải trừ lại tiến độ và thu hồi VIP nếu rơi dưới ngưỡng. Câu trả lời này là bản mới nhất và thay thế hai câu trả lời ngược lại trước đó.

## 4. Các quyết định khách chốt thêm ngày 18-19/07

1. **Thứ tự dùng nguồn Point:** dùng FIFO theo lô, Point vào ví trước được tiêu trước. Một giao dịch có thể dùng nhiều lô và payout phải tách theo phần debit của từng nguồn.
2. **Hoa hồng USDT:** tỷ giá hiện tại là `1 USDT = 26,5 Point`; mỗi source lot ghim rate version tại lúc tạo để retry không đọc rate mới.
3. **Point chuyển khoản:** giữ nguyên nguồn gốc và rate snapshot của source lot từ người gửi. Point kiếm trong game chuyển đi vẫn là Point kiếm trong game; không tự biến thành nguồn USDT-like.
4. **Point đổi từ YWH:** tỷ giá hiện tại `1 YWH = 1,59 Point`; khi tiêu trong game, quy đổi căn cứ hoa hồng USDT theo `1 USDT = 26,5 Point`.
5. **Admin/legacy Point:** quy đổi căn cứ hoa hồng USDT theo `1 USDT = 26,5 Point`; mọi grant/migration phải lưu rate version và Admin actor để audit.
6. **Chậm trả hoa hồng:** giữ payout ở trạng thái chờ ít nhất khoảng 10 phút hoặc lâu hơn. Chỉ giao dịch đã thành công cuối cùng mới mở payout; giao dịch lỗi được coi như không tồn tại đối với hoa hồng.
7. **Bảng hoa hồng:** cấp 1 trực tiếp `8%`; cấp 2 đến cấp 6 mỗi cấp `1%`.
8. **Điều kiện VIP:** tiến độ cộng dồn toàn thời gian từ tiêu dùng `2.650 Point` có nguồn USDT. Point nguồn USDT được người khác chuyển tới vẫn tính cho người tiêu dùng. Refund trừ lại tiến độ và thu hồi VIP nếu tổng còn lại dưới ngưỡng. Hoa hồng vẫn được ghi vào bể khóa khi A hoặc B chưa VIP; chỉ được sử dụng/rút khi cả A và B đều VIP. Khi đủ điều kiện, toàn bộ khoản lịch sử đang khóa tương ứng được mở.

## 5. Các gate còn phải làm rõ trước khi code payout hoặc bật tiền thật

1. **Hoàn đặc biệt sau final:** giao dịch lỗi trước final không sinh payout. Refund phải đảo tiến độ và có thể thu hồi VIP. Nếu người nhận đã tiêu hoa hồng hoặc VIP bị thu hồi sau khi những hoa hồng khác đã mở, cần chốt thu hồi từ số dư hiện có, khóa phần còn lại hay tạo khoản nợ trừ vào hoa hồng tương lai.
2. **Độ chính xác Point/USDT:** khách đã chốt giữ `4,68 Point`; đề xuất dùng fixed-point `1 Point = 1.000.000 micros`, không dùng Float. Cần chốt số chữ số hiển thị, nhập liệu và thanh toán cho cả Point lẫn USDT.
3. **Ledger authoritative:** ADR candidate đã chọn PostgreSQL game cho account đã link; vẫn cần phê duyệt rollout và reconciliation từng account trước migration production.
4. **Tỷ giá/rút:** vẫn cần chốt phí, min/max, quy trình rút USDT bên ngoài và đối soát nhà cung cấp.
5. **Số dư Point web hiện hữu:** cần kiểm kê và migrate một lần mà không nhân đôi hoặc làm mất số dư.

## 6. Hệ quả kỹ thuật đã chốt từ yêu cầu một ví

- Web và game là hai bề mặt hiển thị của cùng một số dư Point authoritative.
- Mọi credit, debit, conversion, transfer, reserve, withdrawal và reversal phải có transaction ID bất biến, idempotency và audit.
- Một giao dịch tiêu dùng game phải trừ Point đúng một lần và ghi rõ phần Point đã dùng theo từng nguồn nội bộ, dù người dùng chỉ nhìn thấy một tổng số dư.
- Mỗi credit tạo một source lot fixed-point có `origin`, số lượng còn lại, thời điểm nhận và rate snapshot nếu có. Shop khóa các lot theo FIFO và ghi allocation cùng transaction mua.
- Point transfer di chuyển/split chính source lot và giữ nguyên `origin/rate`; không mint một lot USDT-like mới ở người nhận.
- Chỉ giao dịch mua đã commit mới tạo sự kiện hoa hồng. Payout dùng cùng source transaction hoặc transactional outbox; replay không trả hai lần và refund dùng reversal tham chiếu đúng payout gốc.
- Commission theo state machine `PENDING -> PAID|CANCELED`; `PAID -> REVERSED` chỉ bằng bút toán đối ứng. `eligible_at` được ghim từ policy delay có version, retry không tính lại theo config mới.
- Sau `PENDING`, mỗi share phải lưu cả `recipient_player_id` (A) và `consumer_player_id` (B). Nếu một trong hai chưa VIP, share đi vào `LOCKED_VIP`; chỉ khi cả A và B đều VIP mới chuyển toàn bộ share lịch sử tương ứng sang `PAYABLE/PAID`.
- Tiến độ VIP phải được cộng idempotent từ phần Point nguồn USDT trong allocation của giao dịch đã commit; transfer giữ nguyên nguồn nên người nhận vẫn được cộng khi tiêu. Refund phải tạo reversal tiến độ idempotent, tính lại trạng thái VIP và chạy song song với hoàn Point/reversal hoa hồng.
- Point lẻ phải trở thành số dư spendable thực sự. `pos BIGINT` cộng `web_point_micros_remainder` hiện mới mang phần lẻ conversion và shop vẫn chỉ tiêu Point nguyên, nên chưa đáp ứng quy tắc `4,68 Point`.
- Unity chỉ gửi ý định hành động. Unity không gửi số Point/YWH muốn cộng và không giữ secret ví.
- Candidate v3 hiện bao phủ `USDT -> Point`, rate Admin bất biến, balance projection, phía game của `reserve/capture/release` và web saga `Point -> USDT` nội bộ. Candidate local 19/07 đã thêm source-lot schema/domain/store và FIFO planner ở trạng thái dormant; chưa apply migration, backfill hoặc nối mutation runtime. Fractional spending, allocation nguyên tử, commission payout USDT/Point và reversal hoa hồng vẫn chưa có.
- Giữ callback public `404`, không chuyển `WEB_TOPUP_MODE=open` và không dùng tiền thật trong khi hợp đồng kỹ thuật chưa hoàn chỉnh.

## 7. Ghi chú canary hiện tại

Canary không tiền hiện đã dùng identity QA riêng `WalletQA2026` và đạt chuỗi web/game/client `5000 -> 5053`, retry/idempotency và ma trận EXE/APK. Kết quả này chỉ chứng minh đồng bộ một số dư Point nguyên và đường chuyển đổi kỹ thuật; chưa chứng minh source attribution, Point thập phân spendable, hoa hồng USDT/Point, payout nhiều tầng, hoàn/hủy hoặc tiền thật.
