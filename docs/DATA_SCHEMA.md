# Data Schema — Y WONDER GREEN FARM
# Cập nhật: 2026-06-01
# Backend: Unity Gaming Services (Cloud Save + Economy)

## Quy Tắc Dữ Liệu
1. KHÔNG xoá Cloud Save key đang có data — đánh dấu `_deprecated` và tạo key mới
2. KHÔNG đổi tên key — chỉ thêm mới
3. Mọi object PHẢI có: `version`, `updatedAt`
4. Dùng JSON cho complex data, primitive cho simple data
5. Key naming convention: `snake_case` (ví dụ: `player_profile`, `quest_progress`)
6. Mọi thay đổi schema phải cập nhật file này trước khi code
7. PHẢI có migration plan khi thay đổi cấu trúc data đang live

---

## Cloud Save Keys

### Key: `player_profile`
**Mục đích:** Thông tin cơ bản người chơi

```json
{
    "version": 1,
    "name": "string — Tên hiển thị (3-20 ký tự)",
    "gender": "string — 'male' | 'female'",
    "avatarId": "string — ID avatar đã chọn",
    "level": "int — Level hiện tại (bắt đầu từ 1)",
    "exp": "float — EXP hiện tại (0.00 - 100.00, đạt 100 = lên level)",
    "tutorialCompleted": "bool — Đã xem cutscene intro chưa",
    "createdAt": "string — ISO 8601 datetime",
    "updatedAt": "string — ISO 8601 datetime"
}
```

| Field | Ràng buộc | Mặc định |
|---|---|---|
| name | 3-20 ký tự, không ký tự đặc biệt | "Player" |
| gender | "male" hoặc "female" | — (bắt buộc chọn) |
| level | >= 1 | 1 |
| exp | 0.00 - 100.00 | 0.00 |

---

### Key: `player_inventory`
**Mục đích:** Túi đồ người chơi

```json
{
    "version": 1,
    "items": [
        {
            "itemId": "string — ID từ Economy Dashboard",
            "quantity": "int — Số lượng",
            "acquiredAt": "string — ISO 8601"
        }
    ],
    "maxSlots": "int — Số slot tối đa",
    "updatedAt": "string — ISO 8601 datetime"
}
```

| Field | Ràng buộc | Mặc định |
|---|---|---|
| items | Array, max 100 items | [] |
| maxSlots | >= 20 | 20 |

---

### Key: `quest_progress`
**Mục đích:** Tiến trình nhiệm vụ

```json
{
    "version": 1,
    "activeQuests": [
        {
            "questId": "string — ID nhiệm vụ",
            "status": "string — 'active' | 'completed' | 'failed'",
            "progress": "int — Tiến trình (0-100%)",
            "startedAt": "string — ISO 8601"
        }
    ],
    "completedQuestIds": ["string — Danh sách ID đã hoàn thành"],
    "updatedAt": "string — ISO 8601 datetime"
}
```

---

### Key: `player_settings`
**Mục đích:** Cài đặt người chơi (đồng bộ cross-device)

```json
{
    "version": 1,
    "musicVolume": "float — 0.0 - 1.0",
    "sfxVolume": "float — 0.0 - 1.0",
    "language": "string — 'vi' | 'en'",
    "notificationsEnabled": "bool",
    "updatedAt": "string — ISO 8601 datetime"
}
```

| Field | Ràng buộc | Mặc định |
|---|---|---|
| musicVolume | 0.0 - 1.0 | 0.8 |
| sfxVolume | 0.0 - 1.0 | 1.0 |
| language | "vi" hoặc "en" | "vi" |

---

### Key: `player_social`
**Mục đích:** Dữ liệu xã hội (friends list được UGS quản lý riêng, đây là metadata bổ sung)

```json
{
    "version": 1,
    "lastOnline": "string — ISO 8601 datetime",
    "statusMessage": "string — Trạng thái (max 50 ký tự)",
    "updatedAt": "string — ISO 8601 datetime"
}
```

---

## Economy — Currencies

| Currency ID | Tên | Loại | Mô tả |
|---|---|---|---|
| `GEMS` | Gem | **Premium** | Mua bằng tiền thật, dùng mua item hiếm |
| `GOLD` | Gold | **Soft** | Kiếm trong game, dùng mua item cơ bản |

> **Lưu ý:** Hiện tại HUD chỉ hiển thị 1 loại currency. Khi tích hợp Economy, sẽ cập nhật HUD.

---

## Economy — Virtual Items

| Item ID | Tên | Category | Giá | Mô tả |
|---|---|---|---|---|
| (Chưa định nghĩa) | — | — | — | Sẽ bổ sung khi thiết kế shop |

> **Quy tắc:** Item ID một khi đã publish lên Dashboard thì KHÔNG ĐƯỢC đổi. Chỉ thêm item mới.

---

## Leaderboards

| Leaderboard ID | Tên | Sort | Reset | Mô tả |
|---|---|---|---|---|
| `lb_level` | Level Ranking | Descending | Không reset | Xếp hạng theo level |
| `lb_season` | Season Score | Descending | Mỗi mùa (30 ngày) | Điểm mùa giải |

---

## Quan Hệ Dữ Liệu

```
Player (UGS Auth)
├── 1:1 → player_profile (Cloud Save)
├── 1:1 → player_inventory (Cloud Save)
├── 1:1 → quest_progress (Cloud Save)
├── 1:1 → player_settings (Cloud Save)
├── 1:1 → player_social (Cloud Save)
├── 1:N → Economy Balances (UGS Economy)
├── 1:N → Leaderboard Entries (UGS Leaderboards)
└── N:N → Friends (UGS Friends)
```
