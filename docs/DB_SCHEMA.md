# 🗄️ DATABASE SCHEMA / ERD — Y WONDER GREEN FARM (REST)

> Lược đồ DB thật theo **REST API riêng** (thay blueprint UGS cũ trong `docs/DATA_SCHEMA.md`).
> Phiên bản: 0.1 — 16/06/2026. **Đợt 1** (users, profiles) đã hiện thực ở server stub bằng file JSON; phần còn lại là **đề xuất** cho PostgreSQL production.
> Quy ước: mọi bảng dữ liệu người chơi có `version` (migration) + `updated_at`. Tiền & số lượng dùng `BIGINT` (tránh tràn `int`).
> Cập nhật 06/07/2026: account game production phải map từ tài khoản web/cấp sẵn; 1 account = 1 nhân vật; MVP sắp tới chưa làm nạp/rút, ưu tiên online + realtime. `Point` vẫn là tiền game và có thể là tiền nạp web ở phase sau; admin/sếp có thể chỉnh dữ liệu nhưng phải ghi audit log.

---

## 1. ERD tổng quan

```
                 ┌──────────────┐
                 │    users     │ (định danh + auth)
                 │ id (PK)      │
                 └──────┬───────┘
                        │ 1
        ┌───────────────┼───────────────┬────────────────┐
        │ 1             │ 1             │ 1              │ 1
   ┌────▼─────┐   ┌─────▼──────┐   ┌────▼──────┐   ┌─────▼──────┐
   │ profiles │   │  economy   │   │ inventory │   │   farm     │
   │ (1-1)    │   │  (1-1)     │   │  (1-N)    │   │  (1-N ô)   │
   └──────────┘   └────────────┘   └───────────┘   └────────────┘
        │ 1               │ 1            │ 1             │ 1
   ┌────▼──────┐   ┌──────▼──────┐ ┌─────▼──────┐   ┌────▼───────┐
   │  quests   │   │ daily_limits│ │  animals   │   │ piggy_bank │
   │  (1-N)    │   │  (1-N)      │ │  (1-N)     │   │  (1-N)     │
   └───────────┘   └─────────────┘ └────────────┘   └────────────┘

  (Tham chiếu danh mục TĨNH — do team/khách định nghĩa, không phải dữ liệu người chơi):
   item_catalog · crop_catalog · animal_catalog · shop_catalog
```

---

## 2. Bảng ĐÃ HIỆN THỰC (Đợt 1)

### `game_players` — mapping tài khoản web sang người chơi game
> Hướng mới 01/07/2026: web giữ VPS riêng và là nguồn tài khoản; game backend có DB riêng, map bằng `web_user_id`.

| Cột | Kiểu | Ghi chú |
|---|---|---|
| `id` | TEXT/UUID PK | `player_id` nội bộ game |
| `web_user_id` | TEXT UNIQUE | ID ổn định do web trả về |
| `username` | TEXT | username/email/phone hoặc tên web |
| `display_name` | TEXT | tên hiển thị ban đầu |
| `auth_source` | TEXT | `mock`, `web`, ... |
| `account_status` | TEXT | `active`, `locked`, `soft_deleted`; mirror trạng thái từ web |
| `locked_at` / `soft_deleted_at` | TIMESTAMP NULL | phục vụ chặn game khi web khóa/xóa mềm |
| `last_login_at` | TIMESTAMP NULL | audit đăng nhập |
| `created_at` / `updated_at` | TIMESTAMP | |

> MVP hiện có `server/webAuthProvider.js` dạng adapter. Khi bên web có API login/verify thật, chỉ thay adapter, không đổi schema game. `web_user_id` phải UNIQUE để bảo đảm 1 web account = 1 game player.

### `users` — định danh + xác thực
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `id` | TEXT/UUID PK | hiện stub: `u_<timestamp>_<rand>` |
| `username` | TEXT UNIQUE | đợt 1 = tên nhân vật. **Cần chốt:** email/social (G3) |
| `password_hash` | TEXT | bcrypt |
| `created_at` | TIMESTAMP | |

### `profiles` — hồ sơ người chơi (1-1 với users)
> Khớp class `PlayerProfile` (client) + `makeDefaultProfile()` (server).

| Cột | Kiểu | Mặc định | Ghi chú |
|---|---|---|---|
| `user_id` | FK PK | | |
| `version` | INT | 1 | migration |
| `name` | TEXT | "Player" | |
| `gender` | TEXT | "male" | |
| `avatar_id` | TEXT | "" | |
| `level` | INT | 1 | |
| `exp` | REAL | 0 | curve EXP **chờ khách (A2)** |
| `character_created` | BOOL | false | account đã hoàn tất tạo nhân vật; login cũ bỏ qua màn tạo nhân vật |
| `tutorial_completed` | BOOL | false | ✅ đang dùng thật |
| `created_at` / `updated_at` | TIMESTAMP | now | |

> Stub hiện lưu cả profile dạng `data_json` trong 1 file; production tách cột như trên.

---

## 3. Bảng ĐỀ XUẤT (Đợt 2 — Economy & Inventory)

### `economy` — ví tiền (1-1, **server-authoritative**)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `user_id` | FK PK | |
| `point` / `pos` | BIGINT | `Point`; MVP online/realtime dùng như tiền game/server demo, phase sau mới nối tiền nạp web; hiện code nội bộ còn dùng tên `pos` |
| `upoint` / `upos` | BIGINT | `UPoint`; vai trò còn cần sếp/web team chốt |
| `version` / `updated_at` | INT / TIMESTAMP | |

> Client KHÔNG ghi trực tiếp; mọi thay đổi qua endpoint giao dịch (xem `SECURITY.md`). MVP sắp tới chưa làm nạp/rút. Nếu phase sau web là wallet authority của `Point`, game-server phải gọi web wallet API khi cộng/trừ tiền nạp và vẫn ghi transaction mirror để đối soát.

### `inventory` — túi đồ (1-N)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `user_id` | FK | |
| `item_id` | TEXT | → `item_catalog.id` |
| `quantity` | INT | |
| `slot_tab` | TEXT | Tool/Material/Seed/Food/Outfit/Special (6 tab UI) |
| `equipped` | BOOL | cho dụng cụ/trang phục |
| `durability` | INT NULL | dụng cụ (nếu có độ bền) |
| | | PK kép (user_id, item_id) hoặc id riêng nếu stack tách |

### `transactions` — sổ cái giao dịch (audit + chống cheat)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `id` | PK | |
| `user_id` | FK | |
| `type` | TEXT | buy/sell/reward/upgrade/piggy_deposit... |
| `delta_pos` / `delta_upos` | BIGINT | |
| `ref` | TEXT | item_id / shop_id / quest_id |
| `idempotency_key` | TEXT UNIQUE | chống double-spend |
| `external_ref` | TEXT NULL | mã giao dịch/web wallet ref nếu có |
| `status` | TEXT | `pending`, `committed`, `failed`, `reversed` |
| `created_at` | TIMESTAMP | |

### `player_daily_limits` — giới hạn lượt theo ngày (1-N)
> Đã thêm vào `server/schema.sql` ngày 06/07/2026 để đưa câu cá/đào đá 10 lượt/ngày lên server khi online.

| Cột | Kiểu | Ghi chú |
|---|---|---|
| `player_id` | FK | |
| `limit_key` | TEXT | ví dụ `fishing`, `mining` |
| `period_key` | TEXT | ngày theo timezone server dạng `YYYY-MM-DD`; khuyến nghị `Asia/Saigon`, cần chốt trước production |
| `used_count` | INT | số lượt đã dùng trong kỳ |
| `max_count` | INT | mặc định 10 |
| `version` / `updated_at` | INT / TIMESTAMP | |

PK kép: `(player_id, limit_key, period_key)`.
Client gửi ý định consume qua `/player/daily-limits/consume`; server kiểm còn lượt và ghi transaction với `idempotency_key`.

### `admin_audit_logs` — nhật ký chỉnh sửa admin/sếp
> Cần cho dashboard online. Không chỉnh tiền/item/farm/daily limit mà không ghi dấu vết.

| Cột | Kiểu | Ghi chú |
|---|---|---|
| `id` | PK | |
| `actor_admin_id` | TEXT | admin/super admin thực hiện |
| `target_player_id` | TEXT NULL | player bị chỉnh, nếu có |
| `action` | TEXT | ví dụ `adjust_point`, `edit_inventory`, `reset_demo_player` |
| `reason` | TEXT | lý do bắt buộc nhập trên dashboard |
| `before_json` / `after_json` | JSONB | snapshot phần bị chỉnh |
| `created_at` | TIMESTAMP | |

---

## 4. Bảng ĐỀ XUẤT (Đợt 3 — Farm & Animal)

### `farm_tiles` — ô đất (1-N)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `user_id` | FK | |
| `cell_x` / `cell_y` | INT | toạ độ ô (hệ tile **chờ chốt A4/mục 4 điểm mù**) |
| `state` | TEXT | soil/plowed/growing/ripe |
| `crop_id` | TEXT NULL | → `crop_catalog.id` |
| `planted_at` | TIMESTAMP | tính thời gian lớn server-side |
| `watered_at` | TIMESTAMP | tính héo |

### `animals` — vật nuôi (1-N)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `user_id` | FK | |
| `animal_id` | TEXT | → `animal_catalog.id` |
| `pen_cell` | INT | vị trí chuồng |
| `fed_at` | TIMESTAMP | tính đói/chết |
| `produce_ready_at` | TIMESTAMP | chu kỳ ra sản phẩm |
| `harvests_left` | INT | vòng đời |
| `health` | TEXT | healthy/sick/dead |

### `piggy_bank` — sổ tiết kiệm (1-N gói active)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `id` | PK | |
| `user_id` | FK | |
| `principal` | BIGINT | tiền gốc |
| `rate` | REAL | 2% / 6% / 45% (đang hardcode) |
| `term_days` | INT | 12 / 30 / 180 |
| `deposited_at` / `mature_at` | TIMESTAMP | **lãi tính server-side** |

### `quests` — nhiệm vụ (1-N)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `user_id` | FK | |
| `quest_id` | TEXT | → danh mục quest (chờ khách A6) |
| `progress` | INT | số đếm objective (không phải %) |
| `status` | TEXT | active/completed/claimed |

---

## 5. Bảng DANH MỤC TĨNH (catalog — team/khách định nghĩa, không phải dữ liệu người chơi)
> Đây chính là chỗ cần **số liệu khách cung cấp** (A1/A3). Có thể nạp từ ScriptableObject hoặc seed DB.

- **`item_catalog`** — id, tên, mô tả, category, buy_price, sell_price, can_sell, icon. (~45 item)
- **`crop_catalog`** — id, growth_time, water_interval, yield, exp_reward, pos_reward, stages.
- **`animal_catalog`** — id, buy_price, produce_item, produce_cycle, max_harvests, feed_interval.
- **`shop_catalog`** — shop_id, tên, NPC, danh sách item bán + giá riêng. (12–13 shop)

---

## 6. Quy tắc chung
- **Khoá ngoại** mọi bảng người chơi → `users.id` (ON DELETE CASCADE).
- **`updated_at`** cập nhật mỗi lần ghi; client gửi `version` để phát hiện xung đột.
- **Migration**: mỗi đổi schema có script versioned (hiện CHƯA có — cần lập khi chuyển stub→Postgres).
- **Index**: `users.username` (unique), `inventory(user_id)`, `farm_tiles(user_id)`, `transactions(idempotency_key)`.
- **Tiền & lớn**: `BIGINT`; thời gian lưu DB nên là `TIMESTAMP` UTC, nhưng daily reset hiển thị/tính `period_key` theo timezone server đã chốt.

## 7. Khoảng trống cần lấp (trước khi lên production)
1. Chốt **PostgreSQL** cho staging/production và chỉ giữ JSON cho dev/local.
2. **Item/crop/animal/shop catalog** chờ số liệu khách (A1/A3).
3. **Hệ toạ độ tile** thống nhất (điểm mù mục 4).
4. **Migration plan** từ file JSON stub → DB thật.
5. Endpoint cho economy/inventory/farm/animal (TDD mục 7) — đợt 2–3.
6. Phase sau MVP online/realtime: chốt web wallet API cho `Point`: balance, credit, debit/spend/reserve, transaction history, idempotency.
