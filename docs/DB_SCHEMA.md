# 🗄️ DATABASE SCHEMA / ERD — Y WONDER GREEN FARM (REST)

> Lược đồ DB thật theo **REST API riêng** (thay blueprint UGS cũ trong `docs/DATA_SCHEMA.md`).
> Phiên bản: 0.1 — 16/06/2026. **Đợt 1** (users, profiles) đã hiện thực ở server stub bằng file JSON; phần còn lại là **đề xuất** cho PostgreSQL production.
> Quy ước: mọi bảng dữ liệu người chơi có `version` (migration) + `updated_at`. Tiền & số lượng dùng `BIGINT` (tránh tràn `int`).

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
        │ 1                              │ 1             │ 1
   ┌────▼──────┐                   ┌─────▼──────┐   ┌────▼───────┐
   │  quests   │                   │  animals   │   │ piggy_bank │
   │  (1-N)    │                   │  (1-N)     │   │  (1-N)     │
   └───────────┘                   └────────────┘   └────────────┘

  (Tham chiếu danh mục TĨNH — do team/khách định nghĩa, không phải dữ liệu người chơi):
   item_catalog · crop_catalog · animal_catalog · shop_catalog
```

---

## 2. Bảng ĐÃ HIỆN THỰC (Đợt 1)

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
| `pos` | BIGINT | tiền thường (hiện code `int` → đổi `long`) |
| `upos` | BIGINT | tiền premium (IAP) |
| `version` / `updated_at` | INT / TIMESTAMP | |

> Client KHÔNG ghi trực tiếp; mọi thay đổi qua endpoint giao dịch (xem `SECURITY.md`).

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
- **Tiền & lớn**: `BIGINT`; thời gian: `TIMESTAMP` UTC (ISO 8601, khớp `nowISO()` hiện tại).

## 7. Khoảng trống cần lấp (trước khi lên production)
1. Chốt **PostgreSQL vs MongoDB** (TDD mục 3).
2. **Item/crop/animal/shop catalog** chờ số liệu khách (A1/A3).
3. **Hệ toạ độ tile** thống nhất (điểm mù mục 4).
4. **Migration plan** từ file JSON stub → DB thật.
5. Endpoint cho economy/inventory/farm/animal (TDD mục 7) — đợt 2–3.
