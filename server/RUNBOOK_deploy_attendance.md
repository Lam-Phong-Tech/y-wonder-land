# Runbook deploy — Điểm danh lên server (migration 009)

**Release:** `3f457ce5` — commit `feat(attendance): dua diem danh len server + luat mat chuoi`
**Đang chạy trên prod:** `77a28406` (deploy 31/07, 10:18)
**Máy:** VPS `42.96.18.14` — service `ywonder-game-server.service`, sau reverse proxy `api.ywonder.net`

> Tài liệu này để **anh tự chạy trên server**. Bé không SSH, không chạy migration prod,
> không đụng secret. Bé chỉ soạn bước và đọc output khi anh dán về.

> ⚠️ **Deploy cái này TRƯỚC KHI BUILD APK.** APK mới sẽ gọi `/player/attendance`; endpoint chưa
> có trên prod thì điểm danh ăn 404. Ngược lại thì an toàn: deploy server trước, APK cũ không
> biết endpoint mới nên không đụng tới, vẫn chạy như cũ.

---

## 0. Đợt này deploy CÁI GÌ

| File | Nội dung | Rủi ro |
|---|---|---|
| `migrations/009_player_attendance.sql` | Tạo **bảng mới** `player_attendance` | Rất thấp — xem dưới |
| `schema.sql` | Chép lại bảng trên cho schema gốc | Không — chỉ dùng khi tạo DB mới |
| `gameDay.js` *(mới)* | Một định nghĩa "ngày game" duy nhất | Thấp — xem cảnh báo dưới |
| `attendanceRules.js` *(mới)* | Bảng thưởng 15 ngày + luật chuỗi | Không — logic thuần |
| `store.js` | Dùng `gameDay.js`; thêm `getAttendance`/`claimAttendance` | Thấp |
| `postgresStore.js` | Như trên, cho kho đang chạy thật | Thấp |
| `index.js` | Thêm 2 route điểm danh | Thấp — route mới, không sửa route cũ |

**`package.json` KHÔNG đổi** ⇒ dùng lại `node_modules` của release cũ, không cần `npm install`.

### Migration 009 làm gì

Đúng một câu `CREATE TABLE IF NOT EXISTS player_attendance`. **Không đụng bảng nào đang có,
không sửa dòng dữ liệu nào.** Chạy lại lần nữa cũng bỏ qua (`migratePostgres.js` có bảng
`schema_migrations`). Prod đang ở 008 nên 009 sẽ chạy.

Người chơi đang có tiến độ điểm danh trên máy sẽ **bắt đầu lại từ Ngày 1** sau đợt này — vì trước
giờ server không hề biết họ đã điểm danh mấy ngày, không có gì để chuyển sang. Không cứu được, và
cũng chỉ ảnh hưởng tài khoản test hiện tại.

### ⚠️ Một thay đổi cần biết trước khi bấm

`store.js` trước đây chấm ngày theo **UTC**, giờ đổi sang **Asia/Ho_Chi_Minh** cho khớp
`postgresStore.js`. **Prod chạy postgres nên không đổi gì cả** — mốc reset vẫn đúng nửa đêm giờ VN
như trước. Chỉ máy dev chạy kho JSON mới thấy khác (và đó chính là cái đang sai).

Muốn đổi múi giờ vận hành thì đặt biến môi trường `GAME_TIMEZONE`, đừng sửa code.

### Sau khi deploy được gì

- Vặn đồng hồ / đổi múi giờ điện thoại **không** còn ăn thêm ngày điểm danh.
- Cài lại game / đổi máy **không** làm mất tiến độ, cũng không quay vòng ăn lại 15 ngày.
- Nghỉ một ngày là mất chuỗi, về Ngày 1 (khách chốt 31/07) — nhưng quà đã lĩnh thì không trả lại.

---

## 1. Trên máy dev (Windows) — đóng gói

```powershell
cd D:\LamGameUnity\BaChuKhuRung3D
```

```powershell
git archive --format=tar.gz -o D:\ywonder_release_3f457ce5.tar.gz 3f457ce5 server/
```

Kiểm tra gói có đủ file mới không — phải thấy đủ **3 dòng**:

```powershell
tar -tzf D:\ywonder_release_3f457ce5.tar.gz | Select-String "009_player_attendance|attendanceRules|gameDay"
```

Đẩy lên server:

```powershell
scp D:\ywonder_release_3f457ce5.tar.gz root@42.96.18.14:/tmp/
```

---

## 2. Trên VPS — xem hiện trạng TRƯỚC khi đụng gì

```bash
readlink -f /opt/ywonder-game/current
```

Phải ra `/opt/ywonder-game/releases/77a28406`. **Chép lại đường dẫn này — đó là nút quay lui.**

```bash
systemctl status ywonder-game-server --no-pager | head -12
```

```bash
curl -s https://api.ywonder.net/game-api/health
```

Phải thấy `"ok":true` và **postgres**. Ra JSON thì **dừng, hỏi lại**.

---

## 3. Backup DB (bắt buộc, chạy dưới `ywonder_game`)

DB dùng peer-auth qua socket nên **phải** chạy dưới đúng user; chạy root sẽ lỗi
`role "root" does not exist`.

```bash
sudo -u ywonder_game pg_dump "postgresql:///ywonder_game?host=/var/run/postgresql" -Fc -f /tmp/ywonder_game_pre3f457ce5.dump
```

```bash
ls -lh /tmp/ywonder_game_pre3f457ce5.dump
```

File phải > 0 byte. Chưa có backup thì **không đi tiếp**.

---

## 4. Dựng release mới (chưa đổi symlink — chưa ảnh hưởng gì)

```bash
mkdir -p /opt/ywonder-game/releases/3f457ce5
```

```bash
tar -xzf /tmp/ywonder_release_3f457ce5.tar.gz -C /opt/ywonder-game/releases/3f457ce5 --strip-components=1
```

```bash
cp -a /opt/ywonder-game/current/node_modules /opt/ywonder-game/releases/3f457ce5/node_modules
```

```bash
chown -R ywonder_game:ywonder_game /opt/ywonder-game/releases/3f457ce5
```

Xác nhận file mới đã nằm đúng chỗ — phải in ra **15** rồi **OK**:

```bash
node -e "const r=require('/opt/ywonder-game/releases/3f457ce5/attendanceRules.js');console.log(r.ATTENDANCE_TOTAL_DAYS);console.log(require('/opt/ywonder-game/releases/3f457ce5/gameDay.js').gameDayKey()==new Date().toLocaleDateString('sv-SE',{timeZone:'Asia/Ho_Chi_Minh'})?'OK ngay game dung':'SAI NGAY');"
```

---

## 5. Chạy migration 009

```bash
sudo -u ywonder_game env DATABASE_URL="postgresql:///ywonder_game?host=/var/run/postgresql" node /opt/ywonder-game/releases/3f457ce5/scripts/migratePostgres.js
```

Mong đợi: bỏ qua 001–008, chạy 009. Xác nhận bảng đã tạo — phải ra đúng **5 dòng**:
`claimed_days`, `last_claim_date`, `max_rewarded_day`, `player_id`, `updated_at`.

```bash
sudo -u ywonder_game psql "postgresql:///ywonder_game?host=/var/run/postgresql" -c "select column_name,data_type from information_schema.columns where table_name='player_attendance' order by column_name;"
```

Không thấy bảng thì **dừng, đừng đổi symlink**.

---

## 6. Đổi symlink + khởi động lại

```bash
ln -sfn /opt/ywonder-game/releases/3f457ce5 /opt/ywonder-game/current
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

Endpoint mới phải đòi đăng nhập — mong đợi **401**, KHÔNG được là 404:

```bash
curl -s -o /dev/null -w "%{http_code}\n" https://api.ywonder.net/game-api/player/attendance
```

**Test trong game (anh làm, cần APK mới):**
- [ ] Mở Điểm danh → bấm nhận Ngày 1 → được **+26 Point**, nút đổi thành "Đã điểm danh hôm nay"
- [ ] Đóng game, mở lại → vẫn là "Đã điểm danh hôm nay" (trước đây mở lại là bấm được tiếp)
- [ ] **Vặn ngày điện thoại tiến 1 ngày** → vẫn "Đã điểm danh hôm nay" ← đây là phép thử quan trọng nhất
- [ ] Xoá dữ liệu ứng dụng rồi đăng nhập lại → tiến độ **vẫn còn**, không về 0

---

## 8. Quay lui nếu hỏng

```bash
ln -sfn /opt/ywonder-game/releases/77a28406 /opt/ywonder-game/current
```

```bash
systemctl restart ywonder-game-server
```

Migration 009 **để nguyên, không cần lùi**. Nó chỉ thêm một bảng mới; code cũ (`77a28406`) không
biết bảng đó tồn tại nên bảng thừa nằm im, vô hại.

Hỏng nặng tới mức phải phục hồi DB:

```bash
sudo -u ywonder_game pg_restore -d "postgresql:///ywonder_game?host=/var/run/postgresql" --clean --if-exists /tmp/ywonder_game_pre3f457ce5.dump
```

---

## 9. Dọn dẹp (sau khi chạy ổn vài ngày)

```bash
rm /tmp/ywonder_release_3f457ce5.tar.gz
```

Giữ lại release cũ và file dump ít nhất một tuần.

---

## Ghi chú

- ⚠️ Trên VPS có 3 service tên na ná. Đợt này **chỉ đụng `ywonder-game-server.service`**.
  `greenxland.service` là web Next.js, `thodung-api.service` là dự án khác hoàn toàn — đừng restart nhầm.
- ⚠️ Mật khẩu root của VPS này **đã lộ dạng chữ thường trong log Codex** và tới giờ vẫn chưa xác nhận
  đã đổi. Nhân lúc SSH vào, anh cân nhắc đổi mật khẩu + chuyển sang khoá SSH.
- 📌 Còn nợ một món cho đợt deploy server sau: `PUT /player/farm-state` thiếu `idempotency_key`
  (mục 3 trong `task.md`). Đợt này **không** kèm theo.
