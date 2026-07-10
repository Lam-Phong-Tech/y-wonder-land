# Phase 1 State Sync Audit

Ngày rà soát: 10/07/2026

## Kết luận

Dự án đang ở **cuối Giai đoạn 1 - Demo online nhanh**, chưa sang Giai đoạn 2.

Phần tài khoản, đăng nhập, bootstrap và realtime đảo công cộng đã hoạt động qua Node + JSON + Cloudflare Tunnel. Tuy nhiên, chỉ hồ sơ người chơi đã có vòng đọc/ghi server tương đối đầy đủ. Point, túi đồ, shop, farm và giới hạn ngày vẫn chưa hoàn thành vòng đồng bộ hai chiều từ gameplay lên backend.

Vì vậy hiện có thể chứng minh khách đăng ký, đăng nhập, gặp nhau, chat và thấy hoạt ảnh của nhau. Chưa thể cam kết mọi thay đổi tiền/túi/farm trên một thiết bị sẽ xuất hiện đúng trên thiết bị khác hoặc còn nguyên sau lần bootstrap tiếp theo.

## Ma trận hiện trạng

| Nhóm dữ liệu | Backend hiện có | Unity đọc từ server | Unity ghi về server | Kết luận |
|---|---|---|---|---|
| Tài khoản local demo | `/auth/register`, `/auth/login`, bcrypt hash, chặn trùng username/email | Có | Có khi đăng ký | Đạt Phase 1 |
| Hồ sơ/nhân vật/tutorial | `GET/PUT /player/profile`, có trong bootstrap | Có | Có qua `SaveProfileAsync` | Đạt MVP |
| Point/UPoint | bootstrap, `economy/apply`, idempotency | Có, rồi cache vào PlayerPrefs | Chưa; `AddPOS/SpendPOS` chỉ sửa PlayerPrefs | Chưa đồng bộ hai chiều |
| Túi đồ | bootstrap, `inventory/adjust`, idempotency | Có, rồi cache vào PlayerPrefs | Chưa; `AddItem/RemoveItem` chỉ sửa PlayerPrefs | Chưa đồng bộ hai chiều |
| Shop mua/bán | Chưa có giao dịch shop nguyên tử | Không dùng backend khi giao dịch | Chưa; gọi trực tiếp EconomyManager + InventoryManager | Chưa server-authoritative |
| Farm/build/crop/animal/resource | `GET/PUT /player/farm-state` và JSON mẫu | Payload được tải nhưng Unity không áp dụng | Chưa; gameplay dùng nhiều key PlayerPrefs | Chưa đồng bộ |
| Giới hạn câu cá/đào mỏ | `daily-limits/consume`, idempotency, giờ server | Payload được tải nhưng DTO Unity chưa đọc `limits` | Chưa; gameplay vẫn dùng PlayerPrefs | Chưa server-authoritative |
| Realtime city/mine | WebSocket presence, transform, chat, emote, action animation/tool | Có | Có, dữ liệu tạm thời trong RAM server | Đạt mục tiêu realtime MVP |
| PostgreSQL | Có `schema.sql` và interface store | Không | Không; `postgresStore.js` mới là scaffold | Chưa bắt đầu Giai đoạn 2 |

## Bằng chứng trong code

- `PlayerBootstrapService.ApplyBootstrap` hiện chỉ áp dụng profile, economy và inventory. `farm_state` và `daily_limits` không được đưa vào gameplay.
- `EconomyManager` và `InventoryManager` tải cache, sau đó mọi hàm cộng/trừ đều gọi `PlayerPrefs.Save()`; không gọi API mutation.
- `ShopPopupController.OnActionClicked` trừ/cộng tiền và item trực tiếp ở client. Hai thay đổi này không phải một transaction server duy nhất.
- `BuildPersistence`, `TilePlacementSystem`, `AnimalManager`, `ResourceSpawner`, câu cá và giới hạn đào mỏ đều còn lưu bằng các key PlayerPrefs.
- Tìm toàn bộ Unity C# không thấy lời gọi tới `/player/economy`, `/player/inventory`, `/player/farm-state` hoặc `/player/daily-limits` ngoài bootstrap/profile.
- Backend JSON đã có endpoint và idempotency cho economy/inventory/daily limits, nhưng chưa có client gameplay sử dụng chúng.

## Rủi ro cần xử lý trước khi gọi Phase 1 hoàn tất

1. Các key Point, inventory, farm/build/animal và daily limit hiện không gắn playerId. Đổi tài khoản trên cùng máy có thể dùng chung cache local, đặc biệt với farm vì bootstrap chưa ghi đè farm.
2. Mua/bán trên client có thể nhìn đúng ngay lúc chơi nhưng server không đổi. Lần đăng nhập sau, bootstrap có thể trả lại snapshot cũ.
3. Nếu chỉ nối riêng `economy/apply` rồi `inventory/adjust`, một request thành công và request còn lại thất bại sẽ làm lệch tiền với item. Shop cần một API transaction nguyên tử.
4. JSON store phù hợp demo ngắn hạn nhưng chưa có backup, migration, khóa dữ liệu và độ bền vận hành như PostgreSQL.

## Thứ tự triển khai tiếp theo

1. **Shop transaction server-authoritative:** thêm API mua/bán nguyên tử, server kiểm item/giá/số lượng, dùng idempotency key; Unity chỉ cập nhật HUD/túi từ response server và không cho giao dịch khi mất mạng.
2. **Tách cache theo playerId:** đổi hoặc bọc các key local để tài khoản A không đọc farm/cache của tài khoản B.
3. **Farm state hai chiều:** mở rộng DTO Unity cho buildings/tiles/crops/animals/resources, load trước khi khôi phục world và save về server theo checkpoint.
4. **Daily limits:** đọc `limits` từ bootstrap và dùng `/daily-limits/consume` cho câu cá/đào mỏ theo giờ server.
5. **Kiểm thử Phase 1:** test tự động 20 kết nối, sau đó test thật 5-20 EXE/APK ngoài mạng; kiểm đăng ký, relogin, shop, inventory, farm, chat, presence và action animation.
6. Khi các tiêu chí trên đạt mới chuyển **Giai đoạn 2**: cài PostgreSQL, triển khai query thật trong `postgresStore`, bổ sung nơi lưu account local nếu còn giữ `/auth/register`, migration và backup.

## Tiêu chí nghiệm thu ngắn

- Mua một item, đăng xuất, đăng nhập ở máy khác: Point giảm và item vẫn còn đúng.
- Bán một item, retry request: chỉ bán/cộng tiền đúng một lần.
- Xây/cuốc/trồng, restart backend và đăng nhập lại: farm được khôi phục đúng theo account.
- Hết lượt câu cá/đào ở máy A: máy B đăng nhập cùng account vẫn thấy hết lượt theo ngày server.
- 20 client cùng room không vượt giới hạn, thấy nhau, chat và nhận state hoạt ảnh; account trùng thay phiên cũ bằng mã 4008.
