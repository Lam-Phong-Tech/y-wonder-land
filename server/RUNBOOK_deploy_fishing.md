# Runbook deploy — Câu cá server-authoritative + giá vé/mồi + economy mặc định 0

Áp cho đợt release trên nhánh `codex/backend-mvp` (3 commit câu cá + các thay đổi giá đã push
trước đó). Chạy TRÊN máy server game vật lý (`api.ywonder.net`, Node sau reverse proxy →
`127.0.0.1:3000`). Đây là tài liệu để **anh tự chạy trên server** — bé KHÔNG tự deploy prod,
KHÔNG sửa DB thật, KHÔNG đụng secret (theo ràng buộc an toàn).

---

## 0. Thay đổi trong đợt này (để biết đang deploy cái gì)

**Câu cá server-authoritative (chống cheat):**
- `server/fishingTable.js` (MỚI) — bảng cá + luật là NGUỒN SỰ THẬT: tiers 2/4/6/10/15/25 Point
  @ 45/25/17/7/4/2%, 10 lượt free/ngày, hết free trừ 1 `bait_01`.
- `server/store.js` + `server/postgresStore.js` — hàm `resolveFishingCatch()`: server tự bốc cá,
  quản lượt/mồi, idempotent (replay cùng key trả đúng con cá cũ).
- `server/index.js` — endpoint `POST /player/fishing/catch`.
- **Không cần migration mới** — dùng bảng sẵn có (`player_daily_limits` key `"fishing"`,
  `player_inventory`, transaction để idempotency).

**Economy mặc định:**
- Account mới giờ mặc định **0 Point** (trước là 5000). Sửa ở `store.js` + `postgresStore.js`.
- ⚠️ Account CŨ đã lỡ nhận 5000 KHÔNG tự về 0 — cần anh reset thủ công riêng (xem mục 7).

**Giá (đã sửa asset Unity + `shopCatalog.json`):**
- `bait_01` mua 2 / bán 1, `mine_ticket_01` mua 2, `spin_ticket_01` (MỚI) mua 5.

---

## 1. Tiền đề

- [ ] Backup DB gần nhất OK (mục 2).
- [ ] Biết prod đang chạy `STORE_MODE` gì (gần như chắc `postgres` — README ghi prod cấm JSON store).
- [ ] Biết cách restart tiến trình Node của anh (pm2 / nssm / scheduled task / cửa sổ `npm start`).
- [ ] Có tài khoản demo để test câu cá sau khi deploy.

Kiểm tra mode + sức khoẻ hiện tại trước khi đụng gì:

```powershell
irm https://api.ywonder.net/game-api/health
```

Trả về `ok = true` và `storage` cho biết mode. Nếu **không** phải postgres ở prod → dừng lại, hỏi lại.

---

## 2. Backup trước (bắt buộc)

**Postgres** — chỉ dump, KHÔNG sửa row nào:

```powershell
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
pg_dump "$env:DATABASE_URL" -Fc -f "D:\ywonder_backups\ywonder_$stamp.dump"
```

**Nếu lỡ đang chạy JSON mode** — copy file data:

```powershell
Copy-Item "D:\LamGameUnity\BaChuKhuRung3D\server\data.json" "D:\ywonder_backups\data_$stamp.json"
```

---

## 3. Lấy code mới

```powershell
cd D:\LamGameUnity\BaChuKhuRung3D\server
git fetch origin
git checkout codex/backend-mvp
git pull --ff-only origin codex/backend-mvp
git log --oneline -5   # phải thấy 3 commit fishing trên cùng
```

3 commit cần thấy:
```
2791449f fix(fishing): bỏ discard `_ =` cho HandleCatchServerAsync — sửa CS8209
1972635d feat(fishing): client gọi server câu cá khi online, giữ local khi offline
064d8910 feat(fishing): server-authoritative catch — server tự bốc cá + quản lượt/mồi
```

---

## 4. Cài deps + đồng bộ catalog giá

Đợt này KHÔNG thêm dependency mới, nhưng chạy cho chắc:

```powershell
npm install
```

Đồng bộ lại catalog giá từ asset Unity (vì đã đổi giá mồi/vé + thêm `spin_ticket_01`):

```powershell
npm run catalog:generate
git diff --stat shopCatalog.json
```

- Nếu `shopCatalog.json` KHÔNG đổi → tốt, catalog đã khớp.
- Nếu CÓ đổi → nghĩa là bản commit lệch asset; xem diff, commit lại `shopCatalog.json` rồi mới deploy.

---

## 5. Test TRƯỚC khi restart

Syntax check nhanh 4 file server đụng tới:

```powershell
node --check fishingTable.js; node --check store.js; node --check postgresStore.js; node --check index.js
```

Bộ test (chạy trên **DB staging/test**, KHÔNG phải prod):

```powershell
npm run test:postgres   # nếu prod dùng postgres — cần DATABASE_URL trỏ DB TEST
npm run test:security
```

> Câu cá đã có smoke test memory-store 38/38 pass ở phiên làm; postgres path mới được viết theo
> mẫu `applyResourceHarvest`/shop nhưng **chưa test DB thật** — mục 6 verify bù phần này trên staging.

Nếu có môi trường staging riêng: deploy staging trước, chạy hết mục 6 ở staging rồi mới lên prod.

---

## 6. Restart server

Theo cách anh đang chạy Node (chọn 1):

```powershell
# pm2:
pm2 restart ywonder-api ; pm2 logs ywonder-api --lines 30

# nssm (Windows service):
nssm restart YWonderApi

# hoặc dừng cửa sổ npm start cũ (Ctrl-C) rồi chạy lại với đúng env production:
$env:NODE_ENV="production"; $env:HOST="127.0.0.1"; $env:PORT="3000"
$env:STORE_MODE="postgres"   # + DATABASE_URL, JWT_SECRET, WEB_AUTH_* như .env.example
npm start
```

> Prod bắt buộc `NODE_ENV=production`, `STORE_MODE=postgres`, secret mạnh, `WEB_AUTH_MODE=http`,
> `HOST=127.0.0.1`. Server sẽ TỪ CHỐI khởi động nếu secret yếu / JSON store / mock auth (theo README).

---

## 7. Verify sau deploy

**7.1 Health:**
```powershell
irm https://api.ywonder.net/game-api/health   # ok=true, storage=postgres
```

**7.2 Câu cá (cần token 1 account demo — anh lấy token qua luồng đăng nhập bình thường; bé KHÔNG xử lý token/secret):**

```powershell
$TOKEN = "<bearer token của account demo>"
$H = @{ Authorization = "Bearer $TOKEN" }
$key = [guid]::NewGuid().ToString("N")

# Lần 1: phải trả về cá + limit.remaining giảm 1
irm -Method Post "https://api.ywonder.net/game-api/player/fishing/catch" -Headers $H -ContentType "application/json" -Body (@{ idempotency_key = $key } | ConvertTo-Json)

# Replay CÙNG key: phải trả ĐÚNG con cá cũ, KHÔNG câu thêm lần nữa (idempotent)
irm -Method Post "https://api.ywonder.net/game-api/player/fishing/catch" -Headers $H -ContentType "application/json" -Body (@{ idempotency_key = $key } | ConvertTo-Json)
```

Checklist kết quả:
- [ ] Lần 1 trả `fish.itemId` + `fish.pointValue` hợp lệ, `limit.remaining` giảm.
- [ ] Replay cùng key: cùng con cá, limit KHÔNG giảm thêm.
- [ ] Câu hết 10 lượt free + hết `bait_01` → trả HTTP **409** `NO_FISHING_TURN`.
- [ ] Có `bait_01` trong kho: sau khi hết free, câu tiếp trừ đúng 1 mồi (`usedBait=true`, `baitRemaining` giảm).

**7.3 Test thật trong client** (bản đã đăng nhập): câu vài con, xác nhận cá vào kho đúng, không câu được khi hết lượt+mồi.

---

## 8. Rollback nếu hỏng

```powershell
git checkout 064d8910~1   # commit ngay trước đợt fishing (detached) — hoặc ghi sẵn SHA cũ trước khi deploy
# rồi restart lại theo mục 6
```

DB không cần rollback (không có migration/schema change đợt này). Nếu account bị sai điểm do thao tác
tay ở mục 7.4 → restore từ dump ở mục 2.

---

## 7.4 (Tuỳ chọn, RIÊNG) Reset 5000 Point account cũ

Việc này ĐỘNG VÀO SỐ DƯ tài khoản thật → **anh tự quyết + tự chạy**, bé không làm. Chỉ reset đúng
các account test lỡ nhận 5000, KHÔNG đụng account đã nạp tiền thật. Xác định danh sách trước, backup
trước, chạy trong transaction, kiểm lại rồi mới commit.

---

## Bé KHÔNG làm (ranh giới an toàn)

- Không tự deploy/restart prod, không đổi env/secret trên server.
- Không đọc/sửa row user trong DB thật; không reset số dư.
- Không cầm token/secret. Các bước trên là để anh tự chạy trên server.
