# CayTrong2.xlsx — Sheet đầu: Cây trồng  (các sheet khác: Thuyết minh công thức tính)

> **📌 GHI CHÚ BỔ SUNG (team — 23/06/2026): THỜI GIAN SINH TRƯỞNG CÂY NGẮN NGÀY**
>
> Sheet gốc của khách **KHÔNG có** cột thời gian sinh trưởng / chu kỳ tưới cho cây ngắn ngày.
> **BA xác nhận (miệng): cây NGẮN NGÀY chín (thu hoạch) trong 24h = 1 ngày game** — áp cho cả 8 cây ở bảng dưới.
> - **Thời gian lớn = 24h.** Demo quy đổi qua `GameTimeConfig.SecondsPerGameDay` (60s/ngày → ~60s thật); trong code `CropDataGenerator` để `Days(1)`.
> - **Đồng hồ sống/chết khi thiếu nước (khách chốt miệng):** gieo mà CHƯA tưới sống **8h** rồi chết; mỗi lần TƯỚI đổ đầy lại sống **20h**, cạn là chết. (Áp ở `CropDefinition.noWaterDeathSec`/`wateredLifeSec` + `FarmTile`. Vì 20h < 24h nên buộc tưới ≥2 lần mới kịp thu.)
> - **Cây DÀI NGÀY (lâu năm): BA CHƯA chốt thời gian chín.** Sheet lâu năm (`CayTrongLauNam2.md`) có "Chu kỳ tưới cây = 14 ngày" + chu kỳ thu 28/28/90 ngày → tạm dùng làm mốc, chờ BA xác nhận.

| TỶ SUẤT LỢI NHUẬN CỦA CÂY TRỒNG |  |  |  |  |  |  |  |  |  |  |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| STT | Cây Trồng | Giá cây giống | Tỷ giá USDT | Slot trồng | Giá phân bón | Số lượng phân bón | Sản Phẩm | Số lượng thành phẩm | Giá vốn nông sản | EXP thu hoạch |
| 1 | Giống cỏ voi | 12 | 0.43 | 1 | 10 | 1 | Cỏ voi | 2 | 11 | 175 |
| 2 | Hạt giống bắp ngô | 8 | 0.3 | 1 | 10 | 1 | Bắp ngô | 3 | 6 | 100 |
| 3 | Hạt giống bắp cải | 3 | 0.11 | 1 | 10 | 1 | Bắp cải | 1 | 13 | 35 |
| 4 | Hạt giống bí ngô | 7 | 0.25 | 1 | 10 | 1 | Bí ngô | 11 | 1.5454545454545454 | 125 |
| 5 | Hạt giống cà rốt | 3 | 0.08 | 1 | 10 | 1 | Cà rốt | 1 | 13 | 25 |
| 6 | Hạt giống dưa hấu | 3 | 0.11 | 1 | 10 | 1 | Dưa hấu | 1 | 13 | 50 |
| 7 | Dây khoai lang giống | 7 | 0.25 | 1 | 10 | 1 | Khoai lang | 1 | 17 | 100 |
| 8 | Hạt giống rau muống | 4 | 0.13 | 1 | 10 | 1 | Rau muống | 1 | 14 | 60 |
|  |  |  | 26 |  |  |  |  |  |  |  |
