# Quy Tắc Nghiệp Vụ Ví Point Web - Game

Ngày ghi nhận: 16/07/2026
Nguồn: nội dung anh chuyển lại từ trao đổi với `@Đặng Trung Hiếu`.
Trạng thái: **đã ghi nhận quyết định nghiệp vụ; chưa phải hợp đồng API, chưa triển khai production**.

## 1. Các quyết định đã xác nhận

| Chủ đề | Xác nhận nghiệp vụ |
|---|---|
| Point trên web và Point trong game | Là cùng một loại tiền. |
| Số dư hiển thị | Người dùng phải nhìn thấy cùng một số dư ở web và game, giống hai giao diện của cùng một ví. |
| Số dư Point đang có trên web | Không tạo bản sao hoặc tách ví khi kết nối game; web và game cùng hiển thị số dư của một ví. |
| Nguồn tạo Point | Người dùng nạp USDT và đổi sang Point. |
| Chuyển đổi YWH | Cho phép đổi `YWH -> Point` và `Point -> YWH`. |
| Tỷ giá | Do Admin thay đổi; không chốt cố định `1 Point = 0,06 USDT` hoặc `1 USDT = 25 Point`. |
| Đổi Point ra USDT/rút | Có cho phép. Quy trình, phí, hạn mức và phê duyệt chưa được cung cấp. |

## 2. Các nghiệp vụ web được trả lời là không trực tiếp làm đổi Point trong game

BA/khách trả lời `không` cho nhóm sau:

- Hoa hồng và thưởng giới thiệu.
- Thưởng nhiệm vụ.
- Đầu tư, hoàn vốn và nhận lãi trên web.
- Đổi quà hoặc staking.
- Đổi YWH sang Point.
- Chuyển Point cho người khác.

Danh sách trên được ghi đúng theo câu trả lời chuyển tiếp, nhưng chưa đủ nhất quán để triển khai. Nếu web và game thật sự dùng **một ví Point**, mọi giao dịch đã commit vào ví Point, dù phát sinh ở web hay game, bắt buộc phải làm thay đổi cùng một số dư mà cả hai bên hiển thị. Vì vậy câu `không làm đổi Point trong game` chỉ có thể hiểu an toàn là nghiệp vụ đó không ghi vào ledger Point; nếu nghiệp vụ có ghi Point thì game phải nhìn thấy số dư mới.

## 3. Hoa hồng tiêu dùng trong game

- Web hiện tại giữ nguyên cơ chế tài sản của HUB.
- Khi người chơi tuyến dưới tiêu dùng trong game, người giới thiệu phải nhận hoa hồng tương tự hệ thống HUB.
- Tài sản trả hoa hồng được nêu là `YWH`, không phải Point.
- Các ví dụ bắt buộc gồm:
  - Mua vật nuôi.
  - Mua cây trồng dài ngày và ngắn ngày.
  - Mua mồi câu.
  - Mua lượt vòng quay.
  - Mua lượt đào khoáng.
- Phạm vi được mô tả chung là mọi giao dịch `tiêu dùng trong game`.

Chưa được cung cấp: tỷ lệ hoa hồng, số tầng giới thiệu, điều kiện đủ chuẩn, giá trị làm căn cứ, thời điểm ghi nhận, nguồn YWH chi trả, quy tắc làm tròn, hoàn/hủy giao dịch và xử lý khi payout lỗi.

## 4. Các điểm bắt buộc phải chốt trước khi hoàn thiện hoặc bật tiền thật

1. **Mâu thuẫn YWH -> Point:** một câu trả lời nói nghiệp vụ này không đổi Point trong game, câu khác xác nhận cho phép đổi YWH sang Point và ngược lại. Cần chốt rằng conversion đã thành công có thay đổi ví Point chung hay không; theo nguyên tắc một ví, câu trả lời hợp lý về kỹ thuật phải là có.
2. **Chuyển Point:** nếu chuyển Point cho người khác là giao dịch trên ví Point chung thì số dư người gửi/người nhận phải thay đổi ở cả web và game. Cần chốt câu `không` đang nói không cho game nhận sự kiện hay nói giao dịch không dùng Point.
3. **Ledger authoritative:** ADR candidate đã chọn PostgreSQL game `player_economy.pos` cho account đã link. Quyết định này đủ để code/test cô lập; vẫn cần phê duyệt rollout và reconciliation từng account trước migration production.
4. **Tỷ giá động:** candidate đã có version bất biến cho `USDT -> Point`, integer micros, rounding remainder và audit Admin. Vẫn cần chốt cặp `YWH <-> Point`, phí và quy tắc làm tròn nghiệp vụ.
5. **Rút Point -> USDT:** game candidate đã có reserve/capture/release idempotent; vẫn cần web orchestrator, phí, min/max, phê duyệt, reversal và đối soát giao dịch bên ngoài.
6. **Số dư Point web hiện hữu:** cần kiểm kê và kế hoạch chuyển sang ledger duy nhất mà không nhân đôi hoặc làm mất số dư.
7. **Hoa hồng YWH:** cần công thức và contract payout idempotent theo cùng transaction tiêu dùng game; refund/hủy phải có reversal tương ứng.

## 5. Hệ quả kỹ thuật đã chốt từ yêu cầu một ví

- Web và game là hai bề mặt hiển thị của cùng một số dư Point authoritative.
- Mọi credit, debit, conversion, transfer, reserve, withdrawal và reversal phải có transaction ID bất biến, idempotency và audit.
- Một giao dịch tiêu dùng game phải trừ Point đúng một lần; sự kiện hoa hồng YWH phải dùng cùng source transaction hoặc transactional outbox để không trả thiếu/trùng.
- Unity chỉ gửi ý định hành động. Unity không gửi số Point/YWH muốn cộng và không giữ secret ví.
- Candidate v3 hiện bao phủ `USDT -> Point`, rate Admin bất biến, balance projection và phía game của `reserve/capture/release`; chưa bao phủ migration legacy, web orchestrator Point -> USDT/YWH, transfer hoặc hoa hồng YWH.
- Giữ callback public `404`, không chuyển `WEB_TOPUP_MODE=open` và không dùng tiền thật trong khi hợp đồng kỹ thuật chưa hoàn chỉnh.

## 6. Ghi chú canary hiện tại

Identity canary trước đây bị ghi nhầm là tài khoản QA. Chủ tài khoản đã xác nhận `Nhien345` là tài khoản thật. Log runtime chứng minh scoped grant block làm các reward dương của account này bị `403`, kéo theo shop bị client chặn; tài khoản đối chứng `senh2026` nhận nước và mua hàng bình thường. Không tiếp tục dùng `Nhien345` làm QA cho vòng ví tiếp theo; cần đưa canary về dormant hoặc chuyển sang một tài khoản QA riêng bằng thay đổi production được duyệt độc lập.
