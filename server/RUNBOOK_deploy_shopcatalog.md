# Runbook deploy — Sửa danh sách/giá cửa hàng + default 0 Point

**Release:** `77a2840689faaf92130272ab1e8d8f3dae12bd3b` (`77a28406`)
**Đang chạy trên prod:** `e495d057…` (khởi động 24/07 18:07)
**Máy:** VPS `42.96.18.14` — service `ywonder-game-server.service`, sau reverse proxy `api.ywonder.net`

> Tài liệu này để **anh tự chạy trên server**. Bé không SSH, không chạy migration prod,
> không đụng secret. Bé chỉ soạn bước và đọc output khi anh dán về.

---

## 0. Đợt này deploy CÁI GÌ

Chỉ **3 file** trong `server/` đổi kể từ release đang chạy:

| File | Nội dung | Rủi ro |
|---|---|---|
| `shopCatalog.json` | Danh sách + giá 8 cửa hàng, sinh lại từ asset Unity | Không — file dữ liệu thuần |
| `migrations/008_new_account_zero_point_default.sql` | `alter table player_economy alter column pos set default 0` | Rất thấp — xem dưới |
| `schema.sql` | Chép lại đổi trên cho schema gốc | Không — chỉ dùng khi tạo DB mới |

**`package.json` KHÔNG đổi** ⇒ dùng lại `node_modules` của release cũ, không cần `npm install`.

### Migration 008 làm gì

Một câu `ALTER TABLE ... SET DEFAULT`. Postgres chỉ sửa metadata, **không quét bảng, không khoá lâu,
không đụng một dòng dữ liệu nào**. Tài khoản cũ đã lỡ nhận 5000 Point **vẫn giữ nguyên** — muốn chỉnh
phải làm bằng thao tác riêng có duyệt.

Prod đang ở migration 007, nên 008 sẽ chạy. `migratePostgres.js` có bảng `schema_migrations`, chạy lại
lần nữa cũng bỏ qua (idempotent).

### Sau khi deploy sẽ hết lỗi gì

12 giao dịch đang bị chặn (`SHOP_ITEM_NOT_ALLOWED` 403):
- **Cửa hàng Vật phẩm** — Gỗ, Đá, Nước tưới
- **Mini Garden** — Cà rốt, Bắp cải, Dưa hấu, Bắp ngô, Bí ngô, Rau muống, Khoai lang, Cỏ voi, Phân bón

Và **phân bón hết bị trừ 50 Point** (niêm yết 1, server đang tính 50).

---

## 1. Trên máy dev (Windows) — đóng gói

```powershell
cd D:\LamGameUnity\BaChuKhuRung3D
git archive --format=tar.gz -o D:\ywonder_release_77a28406.tar.gz 77a28406 server/
```

Kiểm tra gói có đúng 3 file kia không trước khi gửi đi:

```powershell
tar -tzf D:\ywonder_release_77a28406.tar.gz | Select-String "shopCatalog|008_new_account|schema.sql"
```

Đẩy lên server:

```powershell
scp D:\ywonder_release_77a28406.tar.gz root@42.96.18.14:/tmp/
```

---

## 2. Trên VPS — xem hiện trạng TRƯỚC khi đụng gì

```bash
readlink -f /opt/ywonder-game/current
```

```bash
ls /opt/ywonder-game/current | head -20
```

> ⚠️ **Dừng lại đọc kết quả.** Bước 4 giải nén bằng `--strip-components=1`, giả định `index.js`
> nằm NGAY gốc release (`current/index.js`). Nếu lệnh `ls` trên cho thấy có thư mục `server/`
> bọc ngoài thì **bỏ `--strip-components=1`** đi. Dán kết quả cho bé nếu anh không chắc.

```bash
systemctl status ywonder-game-server --no-pager | head -12
```

```bash
curl -s https://api.ywonder.net/game-api/health
```

Phải thấy `"ok":true`. Ghi lại `storage` — prod phải là **postgres**, nếu ra JSON thì **dừng, hỏi lại**.

---

## 3. Backup DB (bắt buộc, chạy dưới `ywonder_game`)

DB dùng peer-auth qua socket nên **phải** chạy dưới đúng user, chạy root sẽ lỗi
`role "root" does not exist`.

```bash
sudo -u ywonder_game pg_dump "postgresql:///ywonder_game?host=/var/run/postgresql" -Fc -f /tmp/ywonder_game_pre77a28406.dump
```

```bash
ls -lh /tmp/ywonder_game_pre77a28406.dump
```

File phải > 0 byte. Chưa có backup thì **không đi tiếp**.

---

## 4. Dựng release mới (chưa đổi symlink — chưa ảnh hưởng gì)

```bash
mkdir -p /opt/ywonder-game/releases/77a28406
```

```bash
tar -xzf /tmp/ywonder_release_77a28406.tar.gz -C /opt/ywonder-game/releases/77a28406 --strip-components=1
```

Dùng lại `node_modules` của release đang chạy (`package.json` không đổi):

```bash
cp -a /opt/ywonder-game/current/node_modules /opt/ywonder-game/releases/77a28406/node_modules
```

```bash
chown -R ywonder_game:ywonder_game /opt/ywonder-game/releases/77a28406
```

Xác nhận file giá mới đã nằm đúng chỗ — phải in ra **1** và **0**:

```bash
grep -c '"buyPrice": 1' /opt/ywonder-game/releases/77a28406/shopCatalog.json && node -e "const c=require('/opt/ywonder-game/releases/77a28406/shopCatalog.json');console.log('phan bon:',JSON.stringify(c.items.fertilizer_01));console.log('MiniGarden mua:',c.shops.Shop_MiniGarden.buyItemIds.length,'mon');"
```

Mong đợi: `phan bon: {"category":"items","buyPrice":1,"sellPrice":0,"canSell":false}` và
`MiniGarden mua: 9 mon`.

---

## 5. Chạy migration 008

```bash
sudo -u ywonder_game env DATABASE_URL="postgresql:///ywonder_game?host=/var/run/postgresql" node /opt/ywonder-game/releases/77a28406/scripts/migratePostgres.js
```

Kết quả mong đợi: bỏ qua 001–007, chạy 008. Xác nhận:

```bash
sudo -u ywonder_game psql "postgresql:///ywonder_game?host=/var/run/postgresql" -c "select column_default from information_schema.columns where table_name='player_economy' and column_name='pos';"
```

Phải ra `0`. Nếu vẫn là `5000` thì migration chưa chạy — **dừng, đừng đổi symlink**.

---

## 6. Đổi symlink + khởi động lại

Ghi lại release cũ để còn đường lùi:

```bash
readlink -f /opt/ywonder-game/current
```

> 📌 **Chép cái đường dẫn vừa in ra để dành.** Đó là nút quay lui của anh.

```bash
ln -sfn /opt/ywonder-game/releases/77a28406 /opt/ywonder-game/current
```

```bash
systemctl restart ywonder-game-server
```

```bash
systemctl status ywonder-game-server --no-pager | head -12
```

Phải thấy `active (running)`, thời gian khởi động là **vừa nãy**.

---

## 7. Nghiệm thu

```bash
curl -s https://api.ywonder.net/game-api/health
```

```bash
journalctl -u ywonder-game-server -n 40 --no-pager
```

Không được có dòng lỗi nào lúc khởi động.

**Test trong game (anh làm):**
- [ ] Mini Garden → tab Mua → mua **Cà rốt** → vào kho, trừ đúng **4 Point/cái**
- [ ] Mini Garden → mua **Phân bón** → trừ đúng **1 Point** (trước là 50)
- [ ] Cửa hàng Vật phẩm → mua **Gỗ / Đá / Nước tưới** → không còn báo lỗi
- [ ] Siêu thị Verdant → chỉ còn tab Mua (Bánh mì, Táo), không còn tab Bán rỗng
- [ ] Tạo tài khoản MỚI → vào game phải là **0 Point**

> Bốn mục đầu cần **APK mới** mới thấy đủ (nút Verdant và câu báo lỗi nằm ở client).
> Nhưng giá tiền thì bản APK cũ cũng ăn ngay sau deploy, vì giá là server quyết.

---

## 8. Quay lui nếu hỏng

```bash
ln -sfn <đường-dẫn-release-cũ-đã-chép-ở-bước-6> /opt/ywonder-game/current
```

```bash
systemctl restart ywonder-game-server
```

Migration 008 **để nguyên, không cần lùi** — nó chỉ đổi default cho dòng tạo về sau, code cũ không
phụ thuộc vào default 5000 (commit `4c0dd8eb` đã ghi thẳng `0` trong câu insert từ 24/07).

Hỏng nặng tới mức phải phục hồi DB:

```bash
sudo -u ywonder_game pg_restore -d "postgresql:///ywonder_game?host=/var/run/postgresql" --clean --if-exists /tmp/ywonder_game_pre77a28406.dump
```

---

## 9. Dọn dẹp (sau khi chạy ổn vài ngày)

```bash
rm /tmp/ywonder_release_77a28406.tar.gz
```

Giữ lại release cũ và file dump ít nhất một tuần.

---

## Ghi chú

- ⚠️ Trên VPS có 3 service tên na ná. Đợt này **chỉ đụng `ywonder-game-server.service`**.
  `greenxland.service` là web Next.js, `thodung-api.service` là dự án khác hoàn toàn — đừng restart nhầm.
- ⚠️ Mật khẩu root của VPS này **đã lộ dạng chữ thường trong log Codex** và tới giờ vẫn chưa xác nhận
  đã đổi. Nhân lúc SSH vào, anh cân nhắc đổi mật khẩu + chuyển sang khoá SSH.
