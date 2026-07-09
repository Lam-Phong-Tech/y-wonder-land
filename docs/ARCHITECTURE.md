# Kiến Trúc Hệ Thống — Y WONDER GREEN FARM (Bá Chủ Khu Rừng 3D)
# Cập nhật: 2026-06-16

> **Backend = REST API riêng** (Node/Express stub → production) + DB, theo kịch bản khách. **KHÔNG dùng UGS.**
> Chi tiết kỹ thuật xem `docs/TECHNICAL_DESIGN.md` (kiến trúc), `docs/WEB_GAME_BACKEND_JOURNEY.md` (hành trình Web account -> Game account) và `docs/DB_SCHEMA.md` (ERD/DB).
> *(Lịch sử: tài liệu này từng là blueprint UGS — đã thay thế ngày 16/06/2026.)*

## Tech Stack
- **Engine:** Unity 6 (6000.3.15f1) — URP
- **Language:** C#
- **UI Framework:** Unity UI Toolkit (UXML + USS)
- **Async:** Awaitable (Unity 6) — đã dùng ở Backend services, IslandTravelManager, ScenePortal
- **JSON:** Newtonsoft.Json (`com.unity.nuget.newtonsoft-json` — có sẵn)
- **Design System:** The Tangible Playground (xem `docs/DESIGN.md`)
- **Backend:** REST API riêng — hiện server stub **Node/Express** (`server/`); production/staging khuyến nghị Node + PostgreSQL. Realtime MVP hiện dùng WebSocket trong `server/` cho đảo công cộng `city`/`mine`; farm không join realtime public room. Push: Firebase FCM (chờ chốt).
- **Target Platform:** Android (MVP) — iOS/PC cân nhắc sau
- **Version Control:** Git

## Backend Services (REST) — thay cho UGS
> Client gọi qua tầng `Assets/_Project/Scripts/Backend/`. Mọi lời gọi offline-first (try/catch + timeout + fallback cache).

| Lớp client | File | Vai trò | Trạng thái |
|---|---|---|---|
| `BackendConfig` | `Scripts/Backend/BackendConfig.cs` | baseUrl/timeout (không hardcode URL) | ✅ Đợt 1 |
| `ApiClient` | `Scripts/Backend/ApiClient.cs` | GET/POST/PUT, không ném exception | ✅ Đợt 1 |
| `AuthService` | `Scripts/Backend/AuthService.cs` | đăng nhập/đăng ký ngầm, lưu token | ✅ Đợt 1 |
| `PlayerProfileService` | `Scripts/Backend/PlayerProfileService.cs` | profile + characterCreated + tutorialCompleted | ✅ Đợt 1 |
| EconomyService / InventoryService | (đợt 2) | tiền & túi đồ server-authoritative | ⏳ |
| FarmSyncService / AnimalSyncService | (đợt 3) | đồng bộ nông trại/vật nuôi | ⏳ |

Endpoint hiện có thêm nhóm backend MVP: `/auth/web-login`, `/player/bootstrap`, economy/inventory/daily-limits/farm-state và WebSocket `/realtime`. Xem `docs/API_CONTRACTS.md`.

## Cấu Trúc Thư Mục (thực tế)

```
Assets/_Project/
├── Scripts/
│   ├── Backend/        ← REST: BackendConfig, ApiClient, AuthService, PlayerProfileService
│   ├── Managers/       ← GameManager, EconomyManager, InventoryManager, ToolManager...
│   ├── Player/         ← PlayerController, EquipmentManager
│   ├── Camera/         ← ThirdPersonCamera
│   ├── Environment/    ← Farm, FarmTile, Harvestable, Animal, TilePlacement, HammerBuild
│   ├── Core/           ← SystemsBootstrapper
│   └── Tutorial/       ← TutorialManager
├── UI/                 ← *.uxml, Styles/*.uss, *Controller.cs
├── _Scenes/            ← Unity scenes
└── Resources/          ← Runtime-loaded assets (BackendConfig...)
server/                 ← REST stub (Node/Express) — NGOÀI Assets/, Unity không import
```

## Sơ Đồ Hệ Thống

```
┌─────────────────────────────────────────────────────┐
│              Unity Client (Android)                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────┐  │
│  │ UI Layer │  │ Game     │  │ Backend Services │  │
│  │ (UXML/  │  │ Logic    │  │ (Auth/Profile…)  │  │
│  │  USS)    │  │          │  │   → ApiClient     │  │
│  └────┬─────┘  └────┬─────┘  └────────┬──────────┘  │
│       └─────────────┴─────────────────┘              │
│              GameManager.cs (State Machine)          │
│         + LocalCache (PlayerPrefs, offline-first)    │
└─────────────────────┬───────────────────────────────┘
                      │ HTTP/JSON (Bearer token, HTTPS ở production)
                      ▼
┌─────────────────────────────────────────────────────┐
│        REST Backend (stub Node/Express → prod)      │
│  /auth/web-login · /player/bootstrap · shop/economy  │
│  WebSocket /realtime for public islands              │
│  JWT · Store: JSON file → PostgreSQL                 │
└─────────────────────────────────────────────────────┘
```

## Game Flow (State Machine)

```
[App Launch]
    ↓
[Login/Character Selection] → chọn giới tính + nhân vật
    ↓
[StartGame] → đăng nhập ngầm (Auth) + nạp profile (server→cache) ── lỗi ──▶ [Offline: dùng cache]
    ↓
[Cutscene] (nếu lần đầu / chưa hoàn thành tutorial)
    ↓
[Gameplay] ← Main game loop
    ├── HUD (Player Info, Currency, Quest, Joystick, Actions)
    ├── Interact với world (Farm, NPC, Shop…)
    ├── Mở Settings/Inventory/Leaderboard (popup)
    └── Auto-save → cache local NGAY + đẩy server best-effort
```

## Environment

| Environment | Mục đích | Backend |
|---|---|---|
| **Development** | Local testing, debug | server stub `localhost:3000` |
| **Staging** | QC testing, beta | REST staging (chờ dựng) |
| **Production** | Live cho players | REST production + HTTPS (chờ chốt host) |

> **Quan trọng:** Build release phải đổi `BackendConfig.baseUrl` khỏi `localhost`. KHÔNG test trên Production.

## Tài liệu liên quan
- `docs/TECHNICAL_DESIGN.md` — kiến trúc backend chi tiết, stack, lộ trình đợt 2–4.
- `docs/WEB_GAME_BACKEND_JOURNEY.md` — hành trình web account trở thành game account, loop server-authoritative và cách kiểm tra.
- `docs/DB_SCHEMA.md` — ERD + lược đồ bảng.
- `docs/SECURITY.md` — anti-cheat, server-authoritative.
- `docs/BUILD_RELEASE.md` — quy trình build Android + Play Console.
- `docs/API_CONTRACTS.md` — endpoint REST.
