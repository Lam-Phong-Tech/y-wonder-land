# Web Item And Notification API Handoff

Ngày cập nhật: 17/07/2026

Tài liệu này dành cho đội web cần tích hợp bán vật phẩm với game. Không chứa secret,
token thật hoặc endpoint quản trị. Trạng thái `ĐANG CÓ` và `CHƯA CÓ` phải được phân
biệt rõ; contract đề xuất bên dưới chưa được gọi trên production cho tới khi game
backend triển khai và hai đội nghiệm thu.

## 1. Trả lời ngắn

- Game hiện **chưa tích hợp Firebase Cloud Messaging (FCM)** và chưa đăng ký device
  token Android/iOS. Vì vậy hiện chưa có push notification hệ điều hành bắn về điện
  thoại khi app đã tắt.
- Web hiện có thông báo trong trang bằng `GET /api/notifications/poll` và Auth.js
  session. Đây là polling thông báo web, không phải Firebase và không phải API game.
- Game đang online có WebSocket realtime. Hiện server mới gửi
  `economy_updated` khi Point đổi; chưa có event `inventory_updated` dành cho vật
  phẩm mua từ web.
- API mua/bán đang có là `POST /player/shop/transaction`, dành cho Unity với JWT
  người chơi và catalog game. Website không được dùng endpoint này để tự giao hàng.
- Game hiện **chưa có API server-to-server an toàn để website cộng vật phẩm**.
  Cần triển khai contract `item-fulfillment` ở mục 4 trước khi website bán thật.

## 2. API game đang hoạt động

Public base URL của Unity:

```text
https://api.ywonder.net/game-api
```

### Đọc trạng thái người chơi

```text
GET /player/bootstrap
Authorization: Bearer <game-player-jwt>
```

Response chứa profile, Point, inventory, farm state và daily limits. Đây là API
Unity dùng sau đăng nhập/relogin, không phải API giao hàng của web.

### Mua hoặc bán trong shop game

```text
POST /player/shop/transaction
Authorization: Bearer <game-player-jwt>
Content-Type: application/json
```

```json
{
  "shop_id": "Shop_FarmShop",
  "mode": "buy",
  "item_id": "rabbit_01",
  "quantity": 1,
  "idempotency_key": "client-action-id"
}
```

Game-server tự đọc giá và quyền mua/bán từ `server/shopCatalog.json`, sau đó đổi
Point và inventory trong một transaction. Client không được gửi `unit_price`.
Endpoint này chỉ dành cho thao tác mua/bán bên trong game.

### Các endpoint không được dùng để giao vật phẩm web

| Endpoint | Trạng thái | Lý do |
|---|---|---|
| `POST /player/inventory/adjust` | Không bàn giao cho web | Delta item dương đang bị khóa theo security gate và sẽ bị khóa toàn cục; cho web gọi sẽ mở đường tự cộng đồ |
| `PUT /player/inventory` | Trả `405 INVENTORY_SERVER_AUTHORITATIVE` | Không cho client thay toàn bộ túi |
| `POST /player/economy/apply` | Không dùng cho thanh toán web | Delta Point dương từ client đang bị khóa; Point web đi qua API HMAC riêng |
| `POST /player/shop/transaction` | Chỉ dành cho Unity | Cần JWT của phiên người chơi và thực hiện mua/bán theo shop game, không đại diện một order web đã thanh toán |

ID vật phẩm chuẩn nằm trong khóa `items` của `server/shopCatalog.json`, ví dụ
`rabbit_01`, `bait_01`, `fertilizer_01`. Hai đội phải dùng `item_id` này, không dùng
tên hiển thị tiếng Việt làm khóa tích hợp. Khi catalog Unity thay đổi, game team sinh
lại file bằng catalog generator và phát hành version mới cho web.

### Vì sao `item_id` là `rabbit_01`, không phải UUID?

`rabbit_01` đã là ID nghiệp vụ ổn định của vật phẩm game, không phải tên người dùng
nhập trong lúc mua. Nguồn hiện tại đi theo chuỗi:

```text
Unity ItemDefinition.id
  -> Assets/Resources/Items/rabbit_01.asset
  -> server/shopCatalog.json
  -> PostgreSQL player_inventory.item_id (TEXT)
```

ID trong API không bắt buộc phải là số hoặc UUID; một stable string/slug cũng hợp
lệ nếu unique, bất biến và được catalog quản lý. Không dùng Unity `.meta` GUID làm
ID nghiệp vụ vì GUID đó nhận diện asset kỹ thuật, không phải sản phẩm bán hàng.

Website **không được tự gõ hoặc tự đặt `item_id`**. Luồng tích hợp cần một trong hai
cách sau:

1. Game phát hành catalog JSON có version/checksum để web import và chọn sản phẩm từ
   danh sách.
2. Game cung cấp API catalog read-only để web đồng bộ định kỳ.

Nếu bảng sản phẩm của web dùng UUID, giữ UUID đó làm `web_product_id` và tạo mapping
unique sang `game_item_id`, ví dụ:

```json
{
  "web_product_id": "8de4f80d-2ce2-4d4a-9db3-28643d6bc710",
  "game_item_id": "rabbit_01"
}
```

UUID sản phẩm web và ID vật phẩm game có hai vai trò khác nhau; không cần đổi toàn bộ
inventory game sang UUID chỉ để kết nối hai catalog. Phần đang thiếu hiện nay là API
catalog/mapping tự động, không phải định dạng của `rabbit_01`.

`idempotency_key` hoặc `transaction_id` lại là ID của **giao dịch**, không phải ID
vật phẩm. Giá trị production nên là UUID/order-line ID do hệ thống tạo; chuỗi
`client-action-id` trong ví dụ chỉ là placeholder minh họa.

## 3. Thông báo hiện tại

### Thông báo trong website

```text
GET https://ywonder.net/api/notifications/poll
Cookie/Auth.js session của người dùng web
```

API này trả tối đa 20 thông báo chưa đọc và một `sync` token để giao diện web poll.
Nó không đăng ký thiết bị, không dùng FCM và không gửi notification khi app đã tắt.

### Realtime trong game

Game giữ WebSocket sau đăng nhập. Event đã có cho Point:

```json
{
  "type": "economy_updated",
  "reason": "web_topup",
  "economy": { "pos": 5000 },
  "duplicate": false,
  "sentAt": "2026-07-17T00:00:00.000Z"
}
```

Chưa có event giao vật phẩm từ web. Contract mới nên gửi `inventory_updated` sau
khi PostgreSQL commit; nếu người chơi offline, lần bootstrap/relogin tiếp theo phải
đọc được inventory mới mà không phụ thuộc event realtime.

## 4. Contract cần triển khai cho website bán vật phẩm

Trạng thái: **ĐỀ XUẤT, CHƯA CÓ TRÊN PRODUCTION**.

Website chỉ gọi từ server-side/outbox. Browser và Unity không được giữ secret hoặc
gọi endpoint này trực tiếp.

```text
POST http://127.0.0.1:3000/internal/web/item-fulfillment
X-YWonder-Timestamp: <unix-seconds>
X-YWonder-Signature: <HMAC-SHA256 domain ywonder-item-fulfillment-v1>
Content-Type: application/json
```

```json
{
  "transaction_id": "web-order-or-order-line-id-bat-bien",
  "web_user_id": "web-user-id",
  "expected_player_id": "game-player-id-da-ghim",
  "items": [
    { "item_id": "rabbit_01", "quantity": 1 }
  ],
  "occurred_at": "2026-07-17T00:00:00.000Z",
  "source": "ywonder-web-shop"
}
```

Response thành công dự kiến:

```json
{
  "ok": true,
  "duplicate": false,
  "transaction_id": "web-order-or-order-line-id-bat-bien",
  "player_id": "game-player-id",
  "inventory": {
    "version": 12,
    "slots": []
  }
}
```

### Quy tắc bắt buộc

1. `transaction_id` là idempotency key bất biến. Retry cùng payload chỉ trả lại kết
   quả cũ; cùng ID nhưng khác account/item/quantity trả `409 IDEMPOTENCY_CONFLICT`.
2. Game-server tự map `web_user_id -> playerId` và so với `expected_player_id`.
   Sai mapping phải fail trước khi đổi inventory.
3. Chỉ nhận `item_id` có trong catalog/allowlist web-sale và quantity trong giới
   hạn. Không nhận giá, quyền lợi hoặc item metadata do browser tự khai.
4. Cộng inventory và ghi transaction ledger trong cùng PostgreSQL transaction.
5. Sau commit, game-server gửi WebSocket `inventory_updated` với snapshot absolute
   cho client online. Client offline nhận đúng dữ liệu qua bootstrap/relogin.
6. Web phải ghi outbox trước khi gọi game. Timeout hoặc backend tạm dừng phải retry
   cùng `transaction_id`, không tạo ID mới và không cộng tay bằng request khác.
7. Secret giao item nên tách khỏi login secret và Point secret, chỉ nằm trong env
   server. Public Nginx phải tiếp tục trả `404` cho `/game-api/internal/web/*`.
8. Refund/hủy order phải dùng reversal endpoint hoặc state machine có audit; không
   gọi quantity âm tùy ý từ browser.

### Mã lỗi tối thiểu

| HTTP | Code | Ý nghĩa |
|---|---|---|
| `400` | `INVALID_ITEM_FULFILLMENT` | Payload, quantity hoặc timestamp không hợp lệ |
| `401` | `INVALID_SIGNATURE` | Thiếu/sai HMAC |
| `403` | `ITEM_NOT_ALLOWED_FOR_WEB_SALE` | Item không thuộc allowlist website |
| `404` | `WEB_GAME_IDENTITY_NOT_FOUND` | Chưa có mapping web -> game |
| `409` | `GAME_ITEM_IDENTITY_MISMATCH` | Mapping không khớp player đã ghim |
| `409` | `IDEMPOTENCY_CONFLICT` | Cùng transaction ID nhưng payload khác |
| `503` | `GAME_ITEM_FULFILLMENT_UNAVAILABLE` | Lỗi tạm thời; web outbox retry cùng ID |

## 5. Nếu cần push về điện thoại

FCM là một phương án phù hợp cho Android, nhưng hiện chưa được tích hợp. Một tranche
riêng cần tối thiểu:

1. Tạo Firebase project chính thức và cấu hình Android; iOS cần thêm APNs.
2. Unity đăng ký/xoay vòng device token sau khi người dùng đồng ý nhận thông báo.
3. Game backend lưu token theo account, device, platform và trạng thái revoke.
4. Chỉ backend giữ Firebase service credential; không đưa server key vào Unity,
   website, Git hoặc tài liệu bàn giao.
5. Backend gửi push sau event nghiệp vụ đã commit, dọn token invalid và chống spam.
6. Test app foreground/background/killed, logout, đổi account và nhiều thiết bị.

Nếu mục tiêu trước mắt chỉ là cập nhật túi ngay khi người chơi đang online, dùng
WebSocket `inventory_updated` là đủ và không cần Firebase. FCM chỉ cần khi muốn báo
trên thanh notification lúc app chạy nền hoặc đã tắt.

## 6. Thông tin web team cần chốt trước khi code

- Danh sách chính xác `item_id`/bundle được bán trên web.
- Order được thanh toán bằng Point, USDT hay YWH và trạng thái nào là thành công cuối.
- Một `transaction_id` áp cho toàn order hay từng order line.
- Quy tắc hủy, hoàn tiền, thu hồi vật phẩm đã dùng và xử lý giao hàng một phần.
- Giới hạn quantity, inventory đầy, item không stack và thời hạn nhận.
- Có cần thông báo trong web, realtime trong game, FCM, hay cả ba.

Tài liệu API game đầy đủ nội bộ nằm ở `docs/API_CONTRACTS.md`. Khi bàn giao cho đội
web, ưu tiên gửi tài liệu này cùng catalog đã version hóa; không gửi env, secret,
credential VPS hoặc các endpoint admin/client mutation ngoài phạm vi tích hợp.
