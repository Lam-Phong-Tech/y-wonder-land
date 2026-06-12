# Kiến Trúc Hệ Thống — Y WONDER GREEN FARM
# Cập nhật: 2026-06-01

## Tech Stack
- **Engine:** Unity 6 (6000.3.15f1) - URP
- **Language:** C#
- **UI Framework:** Unity UI Toolkit (UXML + USS)
- **Design System:** The Tangible Playground (xem docs/DESIGN.md)
- **Backend:** Unity Gaming Services (UGS)
- **Target Platform:** PC + Mobile (Responsive)
- **Version Control:** Git

## UGS Services

| Service | Package | Mục đích |
|---|---|---|
| **Authentication** | `com.unity.services.authentication` | Đăng ký, đăng nhập, anonymous login |
| **Cloud Save** | `com.unity.services.cloudsave` | Lưu tiến trình, inventory, settings |
| **Economy** | `com.unity.services.economy` | Tiền ảo, shop items, bundles |
| **Leaderboards** | `com.unity.services.leaderboards` | Bảng xếp hạng theo mùa |
| **Friends** | `com.unity.services.friends` | Danh sách bạn bè, online status |
| **Lobby** | `com.unity.services.lobby` | Tạo/tham gia phòng chơi |
| **Analytics** | `com.unity.services.analytics` | Tracking hành vi người chơi |
| **Push Notifications** | `com.unity.services.push-notifications` | Thông báo đẩy |

## Cấu Trúc Thư Mục

```
Assets/
├── Animations/         ← Animation clips, Animator Controllers
├── Materials/          ← Vật liệu (Materials, Shaders)
├── Models/             ← 3D models (.fbx), prefabs
├── Scenes/             ← Unity scenes
├── Scripts/
│   ├── Managers/       ← GameManager, AudioManager, NetworkManager
│   ├── Player/         ← PlayerController, PlayerStats
│   ├── UGS/            ← UGS service wrappers (Auth, CloudSave, Economy...)
│   └── Utils/          ← Helper functions, Extensions
├── UI/
│   ├── Styles/         ← .uss files (DesignSystem, LoginScreen, GameHUD...)
│   ├── *.uxml          ← UI layouts
│   └── *Controller.cs  ← UI controllers
├── Prefabs/            ← Reusable prefabs
└── Resources/          ← Runtime-loaded assets
```

## Sơ Đồ Hệ Thống

```
┌─────────────────────────────────────────────────────┐
│              Unity Client (PC / Mobile)              │
│                                                     │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────┐  │
│  │ UI Layer │  │ Game     │  │ UGS Manager      │  │
│  │ (UXML/  │  │ Logic    │  │ (Service Wrapper) │  │
│  │  USS)    │  │          │  │                   │  │
│  └────┬─────┘  └────┬─────┘  └────────┬──────────┘  │
│       │             │                 │              │
│       └─────────────┴─────────────────┘              │
│                     │                                │
│              GameManager.cs                          │
│         (State Machine: Login→Menu→Game)             │
└─────────────────────┬───────────────────────────────┘
                      │ HTTPS (encrypted)
                      ▼
┌─────────────────────────────────────────────────────┐
│            Unity Gaming Services Cloud               │
│                                                     │
│  ┌────────────────┐  ┌────────────────────────────┐ │
│  │ Authentication │  │ Cloud Save                 │ │
│  │ • Sign Up      │  │ • Player Profile           │ │
│  │ • Sign In      │  │ • Inventory                │ │
│  │ • Anonymous    │  │ • Quest Progress           │ │
│  └────────────────┘  └────────────────────────────┘ │
│                                                     │
│  ┌────────────────┐  ┌────────────────────────────┐ │
│  │ Economy        │  │ Leaderboards               │ │
│  │ • Currencies   │  │ • Season Rankings          │ │
│  │ • Virtual Shop │  │ • Player Scores            │ │
│  │ • Bundles      │  │                            │ │
│  └────────────────┘  └────────────────────────────┘ │
│                                                     │
│  ┌────────────────┐  ┌────────────────────────────┐ │
│  │ Friends        │  │ Lobby & Matchmaking        │ │
│  │ • Friend List  │  │ • Create Room              │ │
│  │ • Status       │  │ • Join Room                │ │
│  └────────────────┘  └────────────────────────────┘ │
│                                                     │
│  ┌────────────────┐  ┌────────────────────────────┐ │
│  │ Analytics      │  │ Push Notifications         │ │
│  │ • Events       │  │ • Alerts                   │ │
│  │ • Funnels      │  │ • Promotions               │ │
│  └────────────────┘  └────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

## Game Flow (State Machine)

```
[App Launch]
    ↓
[Initialize UGS] → Fail? → [Offline Mode]
    ↓
[Login Screen] → Sign In / Sign Up / Anonymous
    ↓
[Character Selection] → Chọn giới tính + nhân vật
    ↓
[Cutscene] (nếu lần đầu)
    ↓
[Gameplay] ← Main game loop
    ├── HUD hiển thị (Player Info, Currency, Quest, Joystick, Actions)
    ├── Interact với world
    ├── Mở Settings/Inventory/Leaderboard (popup)
    └── Auto-save progress → Cloud Save
```

## Environment

| Environment | Mục đích | UGS Project |
|---|---|---|
| **Development** | Local testing, debug | UGS Dev environment |
| **Staging** | QC testing, beta | UGS Staging environment |
| **Production** | Live cho players | UGS Production environment |

> **Quan trọng:** KHÔNG BAO GIỜ test trên Production environment. Luôn dùng Dev hoặc Staging.
