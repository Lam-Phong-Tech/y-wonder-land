# Phase 1 State Sync Audit

Ngày rà soát: 10/07/2026

## Kết luận

Dự án đang ở **cuối Giai đoạn 1 - Demo online nhanh**, chưa sang Giai đoạn 2.

Phần tài khoản, đăng nhập, bootstrap và realtime đảo công cộng đã hoạt động qua Node + JSON + Cloudflare Tunnel. Lát khai thác cây/đá ở `city/mine` nay cũng do server quyết định: chỉ một người nhận thưởng, inventory/lượt đào được ghi trước khi broadcast tài nguyên biến mất và hồi lại sau 20 giây. Tuy nhiên, các thay đổi Point/túi đồ còn lại, shop, farm/crop/animal và giới hạn câu cá vẫn chưa hoàn thành vòng đồng bộ hai chiều.

Vì vậy hiện có thể chứng minh khách đăng ký, đăng nhập, gặp nhau, chat và thấy hoạt ảnh của nhau. Chưa thể cam kết mọi thay đổi tiền/túi/farm trên một thiết bị sẽ xuất hiện đúng trên thiết bị khác hoặc còn nguyên sau lần bootstrap tiếp theo.

Xác nhận runtime 10/07: anh đã test bản build nhiều client và chốt lát khai thác cây/đá public vận hành ổn định; tiêu chí một người nhận thưởng, mọi máy cùng thấy depletion/respawn đã đạt. Điều này chưa đồng nghĩa toàn bộ Point/inventory/shop/farm đã server-authoritative.

## Ma trận hiện trạng

| Nhóm dữ liệu | Backend hiện có | Unity đọc từ server | Unity ghi về server | Kết luận |
|---|---|---|---|---|
| Tài khoản local demo | `/auth/register`, `/auth/login`, bcrypt hash, chặn trùng username/email | Có | Có khi đăng ký | Đạt Phase 1 |
| Hồ sơ/nhân vật/tutorial | `GET/PUT /player/profile`, có trong bootstrap | Có | Có qua `SaveProfileAsync` | Đạt MVP |
| Point/UPoint | bootstrap, `economy/apply`, idempotency | Có, rồi cache vào PlayerPrefs | Chưa; `AddPOS/SpendPOS` chỉ sửa PlayerPrefs | Chưa đồng bộ hai chiều |
| Túi đồ | bootstrap, `inventory/adjust`, `applyResourceHarvest`, idempotency | Có, rồi cache vào PlayerPrefs | Có cho claim cây/đá public; các gameplay khác vẫn `AddItem/RemoveItem` local | Đồng bộ một phần |
| Shop mua/bán | Chưa có giao dịch shop nguyên tử | Không dùng backend khi giao dịch | Chưa; gọi trực tiếp EconomyManager + InventoryManager | Chưa server-authoritative |
| Farm/build/crop/animal | `GET/PUT /player/farm-state` và JSON mẫu | Payload được tải nhưng Unity không áp dụng | Chưa; gameplay dùng nhiều key PlayerPrefs | Chưa đồng bộ |
| Cây/đá public `city/mine` | WebSocket manifest/snapshot/claim/respawn + transaction inventory | Có, gồm người vào room sau | Có; server cấp thưởng cho claim đầu tiên | Đạt lát server-authoritative đầu tiên, state world vẫn ở RAM |
| Giới hạn câu cá/đào mỏ | `daily-limits/consume`, idempotency, giờ server | Claim đá nhận `remaining` từ server; DTO bootstrap chưa đọc toàn bộ `limits` | Đào đá public consume nguyên tử; câu cá và luồng local vẫn PlayerPrefs | Đồng bộ một phần |
| Realtime city/mine | WebSocket presence, transform, chat, emote, action animation/tool/resource state | Có | Có, dữ liệu world tạm thời trong RAM server | Đạt mục tiêu realtime MVP |
| PostgreSQL | Có `schema.sql` và interface store | Không | Không; `postgresStore.js` mới là scaffold | Chưa bắt đầu Giai đoạn 2 |

## Bằng chứng trong code

- `PlayerBootstrapService.ApplyBootstrap` hiện chỉ áp dụng profile, economy và inventory. `farm_state` và `daily_limits` không được đưa vào gameplay.
- `EconomyManager` và đa số luồng `InventoryManager` vẫn sửa `PlayerPrefs`; ngoại lệ hiện tại là claim cây/đá public, nơi `RealtimeClient` chỉ áp snapshot inventory sau `resource_harvest_result` từ server.
- `ShopPopupController.OnActionClicked` trừ/cộng tiền và item trực tiếp ở client. Hai thay đổi này không phải một transaction server duy nhất.
- `BuildPersistence`, `TilePlacementSystem`, `AnimalManager`, farm/crop và câu cá vẫn còn lưu bằng các key PlayerPrefs. `ResourceSpawner` còn giữ cache local, nhưng khi ở room public thì snapshot WebSocket ghi đè trạng thái nhìn thấy.
- `server/realtimeServer.js` giữ resource state theo room/ID, kiểm khoảng cách, cấp 10 gỗ hoặc 10 đá + gem, consume giới hạn `mining`, rồi broadcast depletion/respawn. `server/store.js::applyResourceHarvest` ghi reward + daily limit trong một lần `writeAll`.
- Backend JSON đã có endpoint/idempotency cho economy/inventory/daily limits, nhưng ngoài claim tài nguyên public thì các client gameplay khác chưa sử dụng chúng.

## Rủi ro cần xử lý trước khi gọi Phase 1 hoàn tất

1. Các key Point, inventory, farm/build/animal và daily limit hiện không gắn playerId. Đổi tài khoản trên cùng máy có thể dùng chung cache local, đặc biệt với farm vì bootstrap chưa ghi đè farm.
2. Mua/bán trên client có thể nhìn đúng ngay lúc chơi nhưng server không đổi. Lần đăng nhập sau, bootstrap có thể trả lại snapshot cũ.
3. Nếu chỉ nối riêng `economy/apply` rồi `inventory/adjust`, một request thành công và request còn lại thất bại sẽ làm lệch tiền với item. Shop cần một API transaction nguyên tử.
4. JSON store phù hợp demo ngắn hạn nhưng chưa có backup, migration, khóa dữ liệu và độ bền vận hành như PostgreSQL.
5. Shared resource world state hiện ở RAM Node: restart backend sẽ tạo lại registry từ manifest client và không giữ viên đá đang chờ hồi sinh. Đây là giới hạn Phase 1, chưa phải persistence world production.

## Thứ tự triển khai tiếp theo

1. **Shop transaction server-authoritative:** thêm API mua/bán nguyên tử, server kiểm item/giá/số lượng, dùng idempotency key; Unity chỉ cập nhật HUD/túi từ response server và không cho giao dịch khi mất mạng.
2. **Tách cache theo playerId:** đổi hoặc bọc các key local để tài khoản A không đọc farm/cache của tài khoản B.
3. **Farm state hai chiều:** mở rộng DTO Unity cho buildings/tiles/crops/animals/resources, load trước khi khôi phục world và save về server theo checkpoint.
4. **Daily limits:** đọc đầy đủ `limits` từ bootstrap; giữ đào đá public theo transaction hiện tại và chuyển câu cá sang server-authoritative theo giờ server.
5. **Kiểm thử Phase 1:** test tự động 20 kết nối, sau đó test thật 5-20 EXE/APK ngoài mạng; kiểm đăng ký, relogin, shop, inventory, farm, chat, presence và action animation.
6. Khi các tiêu chí trên đạt mới chuyển **Giai đoạn 2**: cài PostgreSQL, triển khai query thật trong `postgresStore`, bổ sung nơi lưu account local nếu còn giữ `/auth/register`, migration và backup.

## Tiêu chí nghiệm thu ngắn

- Mua một item, đăng xuất, đăng nhập ở máy khác: Point giảm và item vẫn còn đúng.
- Bán một item, retry request: chỉ bán/cộng tiền đúng một lần.
- Xây/cuốc/trồng, restart backend và đăng nhập lại: farm được khôi phục đúng theo account.
- Hết lượt câu cá/đào ở máy A: máy B đăng nhập cùng account vẫn thấy hết lượt theo ngày server.
- Hai client cùng đào một viên đá: chỉ một client nhận 10 đá + gem; cả hai cùng thấy đá biến mất, client vào sau cũng thấy đúng trạng thái, và cả hai cùng thấy đá hồi lại sau 20 giây.
- 20 client cùng room không vượt giới hạn, thấy nhau, chat và nhận state hoạt ảnh; account trùng thay phiên cũ bằng mã 4008.
