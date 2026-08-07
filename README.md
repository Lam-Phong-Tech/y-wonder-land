# Y WONDER GREEN FARM

> **A 3D farming / life‑sim game built in Unity, with a REST backend, for the Y Wonder Hub ecosystem.**
> **Game nông trại / mô phỏng cuộc sống 3D làm bằng Unity, có backend REST, thuộc hệ sinh thái Y Wonder Hub.**

This document is a **project handoff guide**. It explains what the project is, how the code is organized, and how each system works — written so that a new developer (or their AI assistant) can understand and continue the project without prior context.

Tài liệu này là **hướng dẫn bàn giao dự án**. Nó giải thích dự án là gì, mã nguồn tổ chức ra sao, và từng hệ thống hoạt động thế nào — viết để một lập trình viên mới (hoặc AI của họ) đọc là hiểu và tiếp tục được, không cần bối cảnh trước đó.

> 🇬🇧 **English** below · 🇻🇳 **Tiếng Việt** ở [nửa dưới](#-tiếng-việt) của tài liệu.

---

# 🇬🇧 English

## 1. What this project is

- **Game name (official):** *Y WONDER GREEN FARM*.
- **Genre:** 3rd‑person 3D farming & animal‑husbandry life‑sim with build mode, shops, fishing, mining, and light social/realtime features.
- **Target platform:** primarily **mobile (Android)**; also builds for Windows standalone and iOS (via CodeMagic CI).
- **Currency:** a single in‑game currency called **"Point"** (internally `POS`). Point is **real, purchasable money** tied to USDT on the Y Wonder web wallet — see the data & economy warnings before touching any number.
- **Architecture in one line:** an **offline‑first Unity client** talking to a **custom Node.js/Express REST backend** backed by **PostgreSQL**. It does **NOT** use Unity Gaming Services (UGS).

## 2. Tech stack

| Layer | Technology |
|---|---|
| Engine | **Unity `6000.3.15f1`** (Unity 6), Universal Render Pipeline (URP `17.3.0`) |
| UI | **UI Toolkit** (UXML/USS) — not uGUI |
| Input | Unity **Input System** `1.19.0` (mobile joystick + touch) |
| Serialization / net | Newtonsoft.Json, `UnityWebRequest` |
| Other packages | Addressables, Cinemachine, glTFast (NPC models), Localization, AI Navigation |
| Backend runtime | **Node.js + Express 4**, `ws` (WebSocket), `jsonwebtoken`, `bcryptjs` |
| Database | **PostgreSQL** (production) · JSON file store (`data.json`) for local dev |
| CI / build | CodeMagic (`codemagic.yaml`) for iOS; Unity Editor for Android/Windows |
| Code namespace | `YWonderLand.*` |

## 3. Repository layout

```
BaChuKhuRung3D/
├── Assets/
│   ├── _Project/               # ALL first-party project content
│   │   ├── Scripts/            # C# gameplay logic (see §6)
│   │   ├── UI/                 # UI Toolkit screens: *.uxml + *Controller.cs
│   │   ├── _Scenes/            # FarmScene (main), CityScene, MineScene
│   │   ├── Data/Shops/         # ShopDefinition assets
│   │   ├── Docs_KichBan/       # Customer design docs & spreadsheets (data source of truth)
│   │   └── Settings/           # URP + render settings
│   ├── Resources/              # Runtime-loaded assets (Resources.Load)
│   │   ├── Items/              # ~130 ItemDefinition / CropDefinition / AnimalDefinition assets
│   │   ├── ItemDatabase.asset  # central item DB
│   │   ├── CropDatabase.asset  # central crop DB
│   │   └── BackendConfig.asset # server URL + online/offline flags
│   └── _Recovery/              # Unity crash-recovery scenes (gitignored — ignore these)
├── server/                     # Node.js/Express backend (see §7)
├── docs/                       # Technical notes, CHANGELOG mirror, ADRs, recovery docs
├── ProjectSettings/            # Unity project config (protected)
├── Packages/manifest.json      # Unity package dependencies
├── codemagic.yaml              # iOS CI
├── RULES.md                    # AI-pairing conventions used while building this project
├── DESIGN.md / docs/DESIGN.md  # UI design system ("Cozy Dark Palia")
└── README.md                   # this file
```

## 4. Getting started

### Open the Unity client
```bash
git clone <repo-url>
cd BaChuKhuRung3D
git lfs pull            # large binary assets are in Git LFS
```
Open with **Unity Hub → Unity `6000.3.15f1`**. Open `Assets/_Project/_Scenes/FarmScene.unity` — this is the **main/base scene**; press Play.

- On first open, an Editor tool auto-generates mock data once (see §8). This is expected.
- If Unity asks to switch platform, only switch to the target you actually need (Android for APK, Windows for desktop, iOS only when exporting Xcode).

### Run the backend locally (optional for pure client testing)
```bash
cd server
npm install
cp .env.example .env       # then edit values
npm start                  # node index.js, listens on :3000 by default
```
Local dev defaults to the **JSON store** (`STORE_MODE=json`, writes `data.json`) so you don't need PostgreSQL to try the API. Production uses `STORE_MODE=postgres`.

### Test the client fully offline (no server)
Set `useOfflineFallback: 1` in `Assets/Resources/BackendConfig.asset`. The game then runs on local `PlayerPrefs` data. **Revert to `0` before any production build** (see §10).

## 5. Building

- **Android (APK/AAB):** Unity Editor with Android Build Support. Verify package name, orientation, and the pre-build flags in §10. Note: APK size is dominated by textures/NPC glTF models — see `TextureSizeReducer` and the build-report notes.
- **Windows:** Unity Editor → Standalone.
- **iOS:** two CodeMagic workflows in `codemagic.yaml` — `unity-ios-release` (Unity runs, exports Xcode, builds IPA) and `ios-testflight-from-exported-xcode` (expects a pre-exported `ios/Unity-iPhone.xcodeproj`). Apple signing is **not** stored in the repo; configure it in CodeMagic/Xcode.

## 6. Client architecture

### 6.1 Startup & game‑state flow
- **`Scripts/Core/SystemsBootstrapper.cs`** — a static class using `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`. Before any scene loads it sets `targetFrameRate=60`, disables vSync (mobile), and creates a `[Persistent Systems]` `DontDestroyOnLoad` object holding, **in order**: `AuthService → EconomyManager → InventoryManager → ToolManager → PlayerProfileService → PlayerBootstrapService`. Auth is first because gameplay managers key their local storage by player scope.
- **`Scripts/Managers/GameManager.cs`** — the central state machine (singleton). `GameState { Login, Menu, Cutscene, Gameplay }`. `SetGameState()` toggles cameras, UI panels, HUD, and chat per state.
  - **Returning player (resume):** if `PlayerScopedPrefs.GetInt("YW_HasSave")==1`, `ResumeGame()` runs. When online, it validates the session with the server (`PlayerBootstrapService`); a 401 sends the player back to Login. On success it spawns the character at the saved pose and enters `Gameplay` directly (no cutscene).
  - **New player:** `Login → character select (Menu) → StartGame() → boat cutscene → Gameplay → Tutorial`. Profile sign-in runs in the background during the ~15s cutscene.
  - Position is saved on pause/quit (only while on the "farm" island). `ClearSave()` (context menu) resets for testing.

### 6.2 Script folders (`Assets/_Project/Scripts`)
| Folder | Role |
|---|---|
| `Core/` | Bootstrap + global utilities: `SystemsBootstrapper`, `GameTimeConfig` (global wall-clock `RealNow`), `UIPopupTracker` |
| `Managers/` | Global singletons: `GameManager`, `EconomyManager` (Point), `InventoryManager` (50-slot bag), `ToolManager`, `FarmManager`, `AnimalManager` (legacy pens), `ExperienceManager`, `AudioManager`, `IslandTravelManager`, `ResourceSpawner` |
| `Backend/` | REST + sync (offline-first): `ApiClient`, `BackendConfig`, `AuthService`, `PlayerBootstrapService`, `PlayerScopedPrefs`, `FarmStateSync`, `GameplayMutationSync`, `FishingService`, `MiningService`, `WheelService`, `ShopTransactionService`, `AttendanceService` |
| `Environment/` | World gameplay (largest group): farm tiles, build mode, animals, pens, fishing, mining, portals, NPC shops, activity log — see §6.3 |
| `Data/` | ScriptableObject definitions: `ItemDefinition`+`ItemDatabase`, `CropDefinition`+`CropDatabase`, `AnimalDefinition`, `ShopDefinition`, `HiddenItems` |
| `Player/` | `PlayerController` (CharacterController + Input System), `EquipmentManager` |
| `Camera/` | `ThirdPersonCamera`, `BuildCameraController` |
| `Tutorial/` | `TutorialManager`, `GuideNPC` |
| `Realtime/` | `RealtimeClient` (WebSocket: global chat + other players in city/mine) |
| `Cutscenes/` | `BoatCutscene` |
| `Editor/` | **Data generators** (not in build): `ItemDataGenerator`, `CropDataGenerator`, `ShopDataGenerator` — see §8 |

### 6.3 Core gameplay systems
- **Farm tiles & build mode:** `FarmTile` is a per-plot state machine (`Soil→Plowed→Planted→Watered→Ripe`) carrying a `CropDefinition`. Plots are placed in **build mode** (`BuildModeOverlayController`, key **B**) as prefabs snapped onto **`BuildSurfaceCell`** cubes. Building **costs materials** (wood/stone), not Point; destroying refunds them. `BuildPersistence` saves placed structures.
- **Animals:** `FarmAnimal` runs hunger / production / disease timers off the wall-clock (`RealNow`), so time advances even while you're on another island. States: `Healthy/Hungry/Sick/Dead`. Products accumulate in `pendingProduct`. **Death is two-tier and intentional** (see §11). A pen is a cluster of adjacent fenced cells — `PenEnclosure` flood-fills "1 fence = 1 cell". `AnimalPrefabLibrary` maps `itemId → real prefab` dropped into the pen.
- **Crops:** grow over real time per `CropDefinition`; visuals & lifecycle live in `FarmTile`.
- **Economy & inventory:** `EconomyManager` holds Point (`long`, PlayerPrefs `YW_POS_Balance_Long`); `InventoryManager` is a 50-slot bag. Both expose `ApplyServerState()` from bootstrap. Shops: `ShopPopupController` + `ShopDefinition` + NPC `MerchantNPC`/`ShopZoneTrigger`.
- **Fishing & mining:** **server-authoritative** (`FishingService`, `MiningService`) to prevent cheating — the server decides the catch and enforces 10 free tries/day. These are **city/mine only** (gated by island id).
- **Tutorial:** `TutorialManager` drives the farm-island onboarding (chop → plow → plant → water → harvest → build pen), hooking `FarmTile` static events to avoid soft-locks.

### 6.4 UI
UI is **UI Toolkit** (UXML/USS). Each screen is a `*.uxml` + `*Controller.cs` pair in `Assets/_Project/UI`. `GameHUDController` (singleton) shows live Point/level/EXP and the interaction prompt system. Main popups: Inventory, Shop, Workshop, Quest, Map, Mailbox, Profile, Friends, Leaderboard, PiggyBank, Reward, Event (lucky wheel), Settings, Chat, plus gameplay overlays (Build, Fishing, Animal interaction). Follow `DESIGN.md` ("Cozy Dark Palia": flat graphics, large radii; glassmorphism only for HUD).

### 6.5 Scenes & islands
- **`FarmScene.unity`** — the **main/base scene** (never unloaded). Holds all managers, UI, and the player.
- **`CityScene.unity`**, **`MineScene.unity`** — loaded **Additive** on travel, unloaded on leave.
- **`IslandTravelManager`** manages travel (islands: `farm, city, mine, haiphu` = "Nam Du", `mocnhi` = "Phú Quốc"). Base scene never unloads, so managers/UI/player survive island changes. Local teleports use `ScenePortal`.

## 7. Backend architecture (`server/`)

- **Runtime:** Node.js + Express 4. Entry point **`server/index.js`** creates the app, mounts every route, attaches the WebSocket realtime server, and listens (default `:3000`).
- **Route mounting quirk:** routes are mounted **twice** — `app.use("/game-api", api)` and `app.use("/", api)` — so production `https://api.ywonder.net/game-api/...` and a bare path both work.
- **Storage:** `store.js` is a facade; `STORE_MODE=postgres` uses `postgresStore.js` (production), `json` uses a file store (dev). `/health` reports the active mode.
- **Auth:** **JWT Bearer** (HS256, 30‑day TTL) + **single active session** (each token carries a `sid`; a newer login invalidates the old one → `SESSION_REPLACED`). Three login flows: local (bcrypt), web-login bridge, and browser SSO (PKCE) where the user authenticates on the website and the game polls to exchange a token.
- **Migrations (`server/migrations/`):** `001` initial · `002` browser-auth · `003` active sessions · `004` single "Point" currency · `005` web top-up remainder · `006` point reservations · `007` point source ledger (FIFO, dormant) · `008` new account = 0 Point · `009` attendance (15-day check-in).
- **Key endpoints:** `GET /health`; `POST /auth/{register,login,web-login,logout}` + `/auth/browser/{start,approve,exchange}`; `GET/PUT /player/profile`; `GET /player/bootstrap` (profile+economy+inventory+farm_state+limits in one call); economy/inventory are **server-authoritative** (`POST /player/economy/apply`, `/player/inventory/adjust`, `/player/shop/transaction` — all require idempotency keys); server-decided rewards (`/player/fishing/catch`, `/player/mining/redeem-ticket`, `/player/wheel/spin`, `/player/attendance/claim`); farm sync (`GET/PUT /player/farm-state` with `expected_version` optimistic concurrency; `POST /player/farm/animals/place`).
- **Deploy:** systemd service **`ywonder-game-server.service`** on the production VPS, behind a reverse proxy → `api.ywonder.net` (loopback `127.0.0.1:3000`). Deploys are **symlink releases** (blue-green by hand): unpack → copy `node_modules` → run migrations → flip `current` symlink → restart; rollback = point the symlink back. DB is PostgreSQL via peer-auth as user `ywonder_game`; **always `pg_dump -Fc` before a migration**. Full step-by-step lives in `server/RUNBOOK_deploy_*.md`.

## 8. Data & content pipeline (read before changing any number)

```
Excel/MD design docs (Docs_KichBan)  →  numbers hand-typed into Editor generators (Scripts/Editor)
   →  run generator from Unity menu  →  writes .asset files in Resources/Items + ItemDatabase/CropDatabase
   →  runtime loads via Resources.Load
```

- **The numbers are NOT read from Excel at runtime.** The spreadsheets in `Assets/_Project/Docs_KichBan/` are the *reference*; the real numbers are **hand-typed in the generator C# code**. Running a generator **overwrites all generated assets** with the numbers in code.
- **Generators (`Scripts/Editor/`):**
  - `ItemDataGenerator.cs` → `ItemDatabase.asset` + ~130 `ItemDefinition` assets, and (via `GenerateAnimalData()`) the 10 `AnimalDefinition` assets. Menu: *Y WONDER GREEN FARM / Tools / Generate Mock Items* and *YWonderLand / Generate Animal Data*. Also auto-runs once, gated by EditorPref `YW_MockDataGenerated_v4`.
  - `CropDataGenerator.cs` → `CropDatabase.asset` + 19 `Crop_*` assets. Menu: *… / Generate Crop Data*.
  - `ShopDataGenerator.cs` → 8 `ShopDefinition` assets in `Assets/_Project/Data/Shops/`. Shops hold **only item-ID lists + access mode**; prices are looked up from `ItemDatabase` at runtime (single source of truth).
- **Runtime loading:** items via `ItemDatabase` (fallback `Resources.Load("Items/{id}")`); animals via `Resources.LoadAll<AnimalDefinition>`; crops via `CropDatabase`. There is **no** central `AnimalDatabase` or `ShopDatabase` asset — animals load by scan, shops are wired by Inspector reference.

## 9. Online / offline sync model

The client is **offline-first**. Every `ApiClient` call is wrapped in try/catch + timeout and never throws — failures return `ApiResult.ok=false` so the caller falls back to local `PlayerPrefs` (scoped per player by `PlayerScopedPrefs`). When online, the server is the source of truth.

- **`GameplayMutationSync`** — a **durable, per-player, on-disk queue** (`PlayerScopedPrefs` key `PendingMutationQueue_v1`, no token stored) of economy/inventory deltas. If the app closes with unsent deltas, they reload and flush **before** the next bootstrap. Each delta has an idempotency key so resends are safe.
- **`FarmStateSync`** — mirrors the four farm PlayerPrefs snapshots into one authoritative snapshot via `PUT /player/farm-state` with `expected_version` (optimistic concurrency). It has a durable outbox and careful 409 handling (distinguishes a benign resend from real data loss), plus a `ContentScore` guard so an empty farm never overwrites a real one.

## 10. Flags to check BEFORE a production build

| Location | Production value | Meaning |
|---|---|---|
| `Assets/Resources/BackendConfig.asset` → `useOfflineFallback` | **`0`** | `1` = run offline on local data (testing only). Must be `0` for online release. |
| `AnimalPrefabLibrary` component → `testTimeScale` | **`1`** | `<1` fast-forwards all animal + crop timers for testing. Must be `1` for real balance. |
| Demo/test loadout flags & demo accounts (`GameManager`) | off | `DemoRich01..05` / `DemoNew01` grant test loadouts and skip tutorial. |

Also verify: backend URL, app identifier, icons, build scenes, and platform player settings. See `TruocKhiBuild_Checklist.md` in `Docs_KichBan`.

## 11. Gotchas & hard-won lessons (the most valuable section)

- **NEVER `git stash -u` / `git reset` / `git clean` on Unity assets.** Real assets were lost twice doing this. Commit instead.
- **Economy numbers are customer-locked.** Use `VatNuoi2.xlsx` + `SuaLai4VatNuoi.xlsx` (the official set); **do NOT apply `VatNuoi3.xlsx`** (different exchange rate). `Docs_KichBan/VatNuoi_DoiChieu.md` is the authoritative comparison. Point ≈ real money on the web wallet; **changing the exchange rate is a financial decision, not game balance** — ask the web/owner first.
- **Running a generator overwrites hand-typed numbers.** To change a real value, edit the generator `.cs`, not the `.asset`. Bumping the `YW_MockDataGenerated_v4` EditorPref triggers a regenerate on open.
- **Death timers are two-tier and correct — don't collapse them.** Animals: 24h (no-feed) → 48h (fed); crops: 8h → 20h; turtle 5→10 days. The customer approved this; the "24h/8h" they mention is only the *fresh-placed* number.
- **1 real day = 1 game day** (`GameTimeConfig.SecondsPerGameDay = 86400`). Old docs mentioning `60f` demo seconds are stale.
- **Floating farm labels were removed for performance.** Rebuilding text meshes + `GetComponentsInChildren<Renderer>()` every frame per tile/animal caused the lag the customer reported. The **water/hunger bar redraw shares the same trap** and is *not* covered by the label-visibility flag — follow the fixed 0.25s-throttled pattern if adding any floating label.
- **Blocked player actions must show a toast** (`NotifyBlocked`) — never fail silently (except mid-animation, which has its own cancel bar).
- **Short-day crops are `canSell=false` on purpose** (they are animal feed). This blocks a buy-cheap/sell-high money exploit; don't "fix" them into sellable.
- **Bootstrapper singletons must `Destroy(this)`, not `Destroy(gameObject)`** — a shared object once destroyed `GameManager` by mistake.
- **`OnResourceHarvested` is a static event shared with the Tutorial.** A throwing handler blocks the whole chain — fire chop/dig toasts directly, don't `+=` the shared event.
- **`Assets/_Recovery/` is Unity crash-recovery junk**, now gitignored. Ignore it.
- **APK size is ~96% textures** (NPC glTF models are huge). Reduce via texture max-size/ASTC, not by Static/Instancing flags.

## 12. Security & handoff checklist

- **No secrets are stored in this repo** — no passwords, tokens, API keys, certificates, or DB credentials. Obtain them from the project owner.
- **On handoff, rotate every shared credential**: production VPS access (move to SSH keys), PostgreSQL, `JWT_SECRET`, Unity license vars, and Apple signing. Treat any credential that ever appeared in a chat/log as compromised and rotate it.
- iOS signing (bundle ID, team, certs/provisioning or App Store Connect API key) is configured **outside** the repo, in CodeMagic/Xcode.
- The production VPS runs **multiple services** — do not restart the wrong one (the game server is `ywonder-game-server.service`; the web and other projects are separate services).

## 13. Where to read next

| Doc | Why |
|---|---|
| `RULES.md` | Coding conventions & AI-pairing rules used to build this project |
| `DESIGN.md` / `docs/DESIGN.md` | UI design system ("Cozy Dark Palia") |
| `docs/CHANGELOG.md` | Dated, per-module change history |
| `docs/CONTEXT_RECOVERY.md` | Long historical engineering log (backend/wallet detail) |
| `Assets/_Project/Docs_KichBan/VatNuoi_DoiChieu.md` | The animal-data reconciliation & which spreadsheet is official |
| `Assets/_Project/Docs_KichBan/TruocKhiBuild_Checklist.md` | Pre-build checklist |
| `server/RUNBOOK_deploy_*.md` | Step-by-step server deployment (attendance runbook is the fullest template) |

---
---

# 🇻🇳 Tiếng Việt

## 1. Dự án này là gì

- **Tên game (chính thức):** *Y WONDER GREEN FARM*.
- **Thể loại:** mô phỏng nông trại & chăn nuôi 3D góc nhìn thứ ba, có chế độ xây dựng, cửa hàng, câu cá, đào khoáng, và tính năng xã hội/realtime nhẹ.
- **Nền tảng đích:** chủ yếu **mobile (Android)**; ngoài ra build được Windows và iOS (qua CI CodeMagic).
- **Tiền tệ:** một loại tiền duy nhất tên **"Point"** (nội bộ gọi `POS`). Point là **tiền thật, mua được**, quy đổi với USDT trên ví web Y Wonder — đọc kỹ phần cảnh báo dữ liệu/kinh tế trước khi đụng bất kỳ con số nào.
- **Kiến trúc một dòng:** **client Unity offline‑first** nói chuyện với **backend REST Node.js/Express tự viết**, dữ liệu lưu ở **PostgreSQL**. **KHÔNG** dùng Unity Gaming Services (UGS).

## 2. Công nghệ

| Tầng | Công nghệ |
|---|---|
| Engine | **Unity `6000.3.15f1`** (Unity 6), Universal Render Pipeline (URP `17.3.0`) |
| Giao diện | **UI Toolkit** (UXML/USS) — không phải uGUI |
| Nhập liệu | **Input System** `1.19.0` (joystick ảo + chạm) |
| Chuỗi hoá / mạng | Newtonsoft.Json, `UnityWebRequest` |
| Gói khác | Addressables, Cinemachine, glTFast (model NPC), Localization, AI Navigation |
| Backend | **Node.js + Express 4**, `ws` (WebSocket), `jsonwebtoken`, `bcryptjs` |
| CSDL | **PostgreSQL** (production) · kho file JSON (`data.json`) cho dev |
| CI / build | CodeMagic (`codemagic.yaml`) cho iOS; Unity Editor cho Android/Windows |
| Namespace mã | `YWonderLand.*` |

## 3. Bố cục repo

```
BaChuKhuRung3D/
├── Assets/
│   ├── _Project/               # TẤT CẢ nội dung tự làm của dự án
│   │   ├── Scripts/            # Mã C# logic gameplay (xem §6)
│   │   ├── UI/                 # Màn hình UI Toolkit: *.uxml + *Controller.cs
│   │   ├── _Scenes/            # FarmScene (chính), CityScene, MineScene
│   │   ├── Data/Shops/         # Asset ShopDefinition
│   │   ├── Docs_KichBan/       # Tài liệu thiết kế & bảng số của khách (nguồn sự thật)
│   │   └── Settings/           # URP + cấu hình render
│   ├── Resources/              # Asset nạp lúc chạy (Resources.Load)
│   │   ├── Items/              # ~130 asset ItemDefinition / CropDefinition / AnimalDefinition
│   │   ├── ItemDatabase.asset  # DB vật phẩm trung tâm
│   │   ├── CropDatabase.asset  # DB cây trồng trung tâm
│   │   └── BackendConfig.asset # URL server + cờ online/offline
│   └── _Recovery/              # Scene Unity tự sao lưu khi crash (đã gitignore — bỏ qua)
├── server/                     # Backend Node.js/Express (xem §7)
├── docs/                       # Ghi chú kỹ thuật, bản sao CHANGELOG, ADR, tài liệu recovery
├── ProjectSettings/            # Cấu hình project Unity (được bảo vệ)
├── Packages/manifest.json      # Phụ thuộc gói Unity
├── codemagic.yaml              # CI iOS
├── RULES.md                    # Quy ước làm việc với AI dùng khi dựng dự án
├── DESIGN.md / docs/DESIGN.md  # Hệ thống thiết kế UI ("Cozy Dark Palia")
└── README.md                   # file này
```

## 4. Bắt đầu

### Mở client Unity
```bash
git clone <repo-url>
cd BaChuKhuRung3D
git lfs pull            # asset nhị phân lớn nằm trong Git LFS
```
Mở bằng **Unity Hub → Unity `6000.3.15f1`**. Mở `Assets/_Project/_Scenes/FarmScene.unity` — đây là **scene chính/scene nền**; bấm Play.

- Lần mở đầu, một công cụ Editor **tự sinh dữ liệu mock một lần** (xem §8). Đây là bình thường.
- Nếu Unity hỏi đổi platform, chỉ đổi sang đúng nền tảng cần (Android để build APK, Windows cho desktop, iOS chỉ khi export Xcode).

### Chạy backend cục bộ (không bắt buộc nếu chỉ test client)
```bash
cd server
npm install
cp .env.example .env       # rồi sửa giá trị
npm start                  # node index.js, mặc định lắng nghe :3000
```
Dev cục bộ mặc định dùng **kho JSON** (`STORE_MODE=json`, ghi `data.json`) nên **không cần PostgreSQL** để thử API. Production dùng `STORE_MODE=postgres`.

### Test client hoàn toàn offline (không cần server)
Đặt `useOfflineFallback: 1` trong `Assets/Resources/BackendConfig.asset`. Game sẽ chạy bằng dữ liệu `PlayerPrefs` cục bộ. **Nhớ trả về `0` trước mọi bản build production** (xem §10).

## 5. Build

- **Android (APK/AAB):** Unity Editor có Android Build Support. Kiểm tên gói, hướng màn hình, và các cờ trước build ở §10. Lưu ý: dung lượng APK chủ yếu do texture/model NPC glTF — xem `TextureSizeReducer` và ghi chú build-report.
- **Windows:** Unity Editor → Standalone.
- **iOS:** hai workflow CodeMagic trong `codemagic.yaml` — `unity-ios-release` (Unity chạy, export Xcode, build IPA) và `ios-testflight-from-exported-xcode` (cần sẵn `ios/Unity-iPhone.xcodeproj` đã export). Chữ ký Apple **không** nằm trong repo; cấu hình trong CodeMagic/Xcode.

## 6. Kiến trúc client

### 6.1 Khởi động & luồng trạng thái game
- **`Scripts/Core/SystemsBootstrapper.cs`** — class static dùng `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`. Trước khi scene nào load: đặt `targetFrameRate=60`, tắt vSync (mobile), tạo object `[Persistent Systems]` `DontDestroyOnLoad` gắn **theo thứ tự**: `AuthService → EconomyManager → InventoryManager → ToolManager → PlayerProfileService → PlayerBootstrapService`. Auth trước tiên vì các manager gameplay khoá lưu trữ theo scope người chơi.
- **`Scripts/Managers/GameManager.cs`** — máy trạng thái trung tâm (singleton). `GameState { Login, Menu, Cutscene, Gameplay }`. `SetGameState()` bật/tắt camera, panel UI, HUD, chat theo state.
  - **Người chơi cũ (resume):** nếu `PlayerScopedPrefs.GetInt("YW_HasSave")==1`, chạy `ResumeGame()`. Khi online, xác thực phiên với server (`PlayerBootstrapService`); 401 → về Login. Thành công → spawn nhân vật tại vị trí đã lưu, vào thẳng `Gameplay` (không cutscene).
  - **Người chơi mới:** `Login → chọn nhân vật (Menu) → StartGame() → cutscene thuyền → Gameplay → Tutorial`. Đăng nhập hồ sơ chạy nền trong lúc cutscene (~15s).
  - Vị trí được lưu lúc pause/quit (chỉ khi đang ở đảo "farm"). `ClearSave()` (context menu) để test lại từ đầu.

### 6.2 Các thư mục Scripts (`Assets/_Project/Scripts`)
| Thư mục | Vai trò |
|---|---|
| `Core/` | Khởi động + tiện ích chung: `SystemsBootstrapper`, `GameTimeConfig` (đồng hồ thật `RealNow`), `UIPopupTracker` |
| `Managers/` | Singleton toàn cục: `GameManager`, `EconomyManager` (Point), `InventoryManager` (túi 50 ô), `ToolManager`, `FarmManager`, `AnimalManager` (chuồng cũ), `ExperienceManager`, `AudioManager`, `IslandTravelManager`, `ResourceSpawner` |
| `Backend/` | REST + đồng bộ (offline-first): `ApiClient`, `BackendConfig`, `AuthService`, `PlayerBootstrapService`, `PlayerScopedPrefs`, `FarmStateSync`, `GameplayMutationSync`, `FishingService`, `MiningService`, `WheelService`, `ShopTransactionService`, `AttendanceService` |
| `Environment/` | Gameplay thế giới (nhóm lớn nhất): ô ruộng, build mode, vật nuôi, chuồng, câu cá, đào, cổng dịch chuyển, NPC shop, nhật ký — xem §6.3 |
| `Data/` | Định nghĩa ScriptableObject: `ItemDefinition`+`ItemDatabase`, `CropDefinition`+`CropDatabase`, `AnimalDefinition`, `ShopDefinition`, `HiddenItems` |
| `Player/` | `PlayerController` (CharacterController + Input System), `EquipmentManager` |
| `Camera/` | `ThirdPersonCamera`, `BuildCameraController` |
| `Tutorial/` | `TutorialManager`, `GuideNPC` |
| `Realtime/` | `RealtimeClient` (WebSocket: chat toàn cục + người chơi khác ở city/mine) |
| `Cutscenes/` | `BoatCutscene` |
| `Editor/` | **Generator dữ liệu** (không vào build): `ItemDataGenerator`, `CropDataGenerator`, `ShopDataGenerator` — xem §8 |

### 6.3 Các hệ gameplay cốt lõi
- **Ô ruộng & build mode:** `FarmTile` là máy trạng thái từng ô (`Soil→Plowed→Planted→Watered→Ripe`) mang một `CropDefinition`. Ô được đặt trong **build mode** (`BuildModeOverlayController`, phím **B**) dưới dạng prefab snap vào các khối **`BuildSurfaceCell`**. Xây **tốn vật liệu** (gỗ/đá), không tốn Point; phá thì hoàn lại. `BuildPersistence` lưu công trình đã đặt.
- **Vật nuôi:** `FarmAnimal` chạy mốc đói / ra sản phẩm / bệnh theo đồng hồ thật (`RealNow`), nên thời gian vẫn trôi khi bạn đang ở đảo khác. Trạng thái `Healthy/Hungry/Sick/Dead`. Sản phẩm dồn vào `pendingProduct`. **Cơ chế chết 2 tầng, có chủ đích** (xem §11). Chuồng là cụm ô-rào liền kề — `PenEnclosure` flood-fill "1 hàng rào = 1 ô". `AnimalPrefabLibrary` map `itemId → prefab thật` thả vào chuồng.
- **Cây trồng:** lớn theo thời gian thật theo `CropDefinition`; trực quan & vòng đời nằm trong `FarmTile`.
- **Kinh tế & kho:** `EconomyManager` giữ Point (`long`, PlayerPrefs `YW_POS_Balance_Long`); `InventoryManager` là túi 50 ô. Cả hai có `ApplyServerState()` từ bootstrap. Cửa hàng: `ShopPopupController` + `ShopDefinition` + NPC `MerchantNPC`/`ShopZoneTrigger`.
- **Câu cá & đào khoáng:** **server-authoritative** (`FishingService`, `MiningService`) để chống hack — server quyết cá và giữ trần 10 lượt free/ngày. Chỉ chạy ở **city/mine** (khoá theo id đảo).
- **Tutorial:** `TutorialManager` dẫn dắt onboarding ở đảo nông trại (chặt → cuốc → trồng → tưới → thu → xây chuồng), bám các event tĩnh của `FarmTile` để chống kẹt bước.

### 6.4 UI
UI dùng **UI Toolkit** (UXML/USS). Mỗi màn hình là cặp `*.uxml` + `*Controller.cs` trong `Assets/_Project/UI`. `GameHUDController` (singleton) hiển thị Point/cấp/EXP thật và hệ prompt tương tác. Các popup chính: Inventory, Shop, Workshop, Quest, Map, Mailbox, Profile, Friends, Leaderboard, PiggyBank, Reward, Event (vòng quay), Settings, Chat, cùng overlay gameplay (Build, Fishing, tương tác vật nuôi). Tuân theo `DESIGN.md` ("Cozy Dark Palia": đồ hoạ phẳng, bo góc lớn; glass chỉ cho HUD).

### 6.5 Scene & đảo
- **`FarmScene.unity`** — **scene chính/scene nền** (không bao giờ unload). Chứa tất cả manager, UI, và người chơi.
- **`CityScene.unity`**, **`MineScene.unity`** — load **Additive** khi tới, unload khi rời.
- **`IslandTravelManager`** quản lý đi đảo (các đảo: `farm, city, mine, haiphu` = "Nam Du", `mocnhi` = "Phú Quốc"). Scene nền không unload nên manager/UI/người chơi không mất khi đổi đảo. Dịch chuyển cục bộ dùng `ScenePortal`.

## 7. Kiến trúc backend (`server/`)

- **Runtime:** Node.js + Express 4. Điểm vào **`server/index.js`** tạo app, mount mọi route, gắn WebSocket realtime, rồi lắng nghe (mặc định `:3000`).
- **Điểm đặc biệt khi mount route:** route được mount **hai lần** — `app.use("/game-api", api)` và `app.use("/", api)` — nên production `https://api.ywonder.net/game-api/...` lẫn đường trần đều chạy.
- **Lưu trữ:** `store.js` là facade; `STORE_MODE=postgres` dùng `postgresStore.js` (production), `json` dùng kho file (dev). `/health` báo mode đang chạy.
- **Xác thực:** **JWT Bearer** (HS256, TTL 30 ngày) + **phiên hoạt động duy nhất** (mỗi token mang `sid`; đăng nhập mới huỷ token cũ → `SESSION_REPLACED`). Ba luồng đăng nhập: local (bcrypt), cầu web-login, và SSO trình duyệt (PKCE) — người dùng xác thực trên website, game poll để đổi token.
- **Migration (`server/migrations/`):** `001` gốc · `002` browser-auth · `003` phiên hoạt động · `004` gộp tiền "Point" · `005` phần dư nạp web · `006` giữ chỗ điểm · `007` sổ nguồn điểm (FIFO, dormant) · `008` tài khoản mới = 0 Point · `009` điểm danh 15 ngày.
- **Endpoint chính:** `GET /health`; `POST /auth/{register,login,web-login,logout}` + `/auth/browser/{start,approve,exchange}`; `GET/PUT /player/profile`; `GET /player/bootstrap` (gộp profile+economy+inventory+farm_state+limits trong một lời gọi); kinh tế/kho **server-authoritative** (`POST /player/economy/apply`, `/player/inventory/adjust`, `/player/shop/transaction` — đều cần idempotency key); thưởng do server quyết (`/player/fishing/catch`, `/player/mining/redeem-ticket`, `/player/wheel/spin`, `/player/attendance/claim`); đồng bộ nông trại (`GET/PUT /player/farm-state` với `expected_version` optimistic concurrency; `POST /player/farm/animals/place`).
- **Deploy:** service systemd **`ywonder-game-server.service`** trên VPS production, sau reverse proxy → `api.ywonder.net` (loopback `127.0.0.1:3000`). Deploy kiểu **symlink release** (blue-green thủ công): giải nén → copy `node_modules` → chạy migration → đổi symlink `current` → restart; rollback = trỏ symlink về bản cũ. DB là PostgreSQL peer-auth dưới user `ywonder_game`; **luôn `pg_dump -Fc` trước migration**. Từng bước chi tiết ở `server/RUNBOOK_deploy_*.md`.

## 8. Pipeline dữ liệu & nội dung (đọc trước khi đổi số)

```
Tài liệu Excel/MD (Docs_KichBan)  →  số GÕ TAY vào generator (Scripts/Editor)
   →  chạy generator từ menu Unity  →  ghi file .asset trong Resources/Items + ItemDatabase/CropDatabase
   →  runtime nạp qua Resources.Load
```

- **Số KHÔNG được đọc từ Excel lúc chạy.** Bảng tính trong `Assets/_Project/Docs_KichBan/` là *tài liệu đối chiếu*; số thật **gõ tay trong code generator C#**. Chạy generator = **ghi đè toàn bộ asset sinh ra** bằng số trong code.
- **Generator (`Scripts/Editor/`):**
  - `ItemDataGenerator.cs` → `ItemDatabase.asset` + ~130 asset `ItemDefinition`, và (qua `GenerateAnimalData()`) 10 asset `AnimalDefinition`. Menu: *Y WONDER GREEN FARM / Tools / Generate Mock Items* và *YWonderLand / Generate Animal Data*. Cũng tự chạy một lần, gate bằng EditorPref `YW_MockDataGenerated_v4`.
  - `CropDataGenerator.cs` → `CropDatabase.asset` + 19 asset `Crop_*`. Menu: *… / Generate Crop Data*.
  - `ShopDataGenerator.cs` → 8 asset `ShopDefinition` ở `Assets/_Project/Data/Shops/`. Shop **chỉ chứa danh sách ID hàng + chế độ truy cập**; giá tra từ `ItemDatabase` lúc chạy (1 nguồn duy nhất).
- **Nạp lúc chạy:** vật phẩm qua `ItemDatabase` (fallback `Resources.Load("Items/{id}")`); vật nuôi qua `Resources.LoadAll<AnimalDefinition>`; cây qua `CropDatabase`. **Không có** asset `AnimalDatabase` hay `ShopDatabase` trung tâm — vật nuôi nạp bằng cách quét, shop nối bằng tham chiếu Inspector.

## 9. Mô hình đồng bộ online / offline

Client là **offline-first**. Mọi lời gọi `ApiClient` bọc try/catch + timeout và không bao giờ ném lỗi — thất bại trả `ApiResult.ok=false` để caller rơi về `PlayerPrefs` cục bộ (scope theo người chơi qua `PlayerScopedPrefs`). Khi online, server là nguồn sự thật.

- **`GameplayMutationSync`** — **hàng đợi bền vững, per-player, lưu xuống đĩa** (`PlayerScopedPrefs` key `PendingMutationQueue_v1`, không lưu token) chứa các delta kinh tế/kho. Nếu app đóng khi còn delta chưa gửi, chúng nạp lại và flush **trước** bootstrap kế tiếp. Mỗi delta có idempotency key nên gửi lại vẫn an toàn.
- **`FarmStateSync`** — mirror 4 snapshot PlayerPrefs nông trại thành một snapshot uy quyền qua `PUT /player/farm-state` với `expected_version` (optimistic concurrency). Có outbox bền vững và xử lý 409 tinh vi (phân biệt gửi-lại lành tính với mất dữ liệu thật), kèm `ContentScore` để farm rỗng không đè farm thật.

## 10. Cờ phải kiểm TRƯỚC khi build production

| Vị trí | Giá trị production | Ý nghĩa |
|---|---|---|
| `Assets/Resources/BackendConfig.asset` → `useOfflineFallback` | **`0`** | `1` = chạy offline bằng dữ liệu cục bộ (chỉ để test). Phải `0` khi ra bản online. |
| Component `AnimalPrefabLibrary` → `testTimeScale` | **`1`** | `<1` tua nhanh mọi mốc thời gian vật nuôi + cây để test. Phải `1` khi lấy số cân bằng thật. |
| Cờ loadout test & tài khoản demo (`GameManager`) | tắt | `DemoRich01..05` / `DemoNew01` phát loadout test và bỏ tutorial. |

Ngoài ra kiểm: URL backend, app id, icon, danh sách scene build, và player settings từng nền tảng. Xem `TruocKhiBuild_Checklist.md` trong `Docs_KichBan`.

## 11. Bẫy & bài học xương máu (phần giá trị nhất)

- **TUYỆT ĐỐI KHÔNG `git stash -u` / `git reset` / `git clean` trên asset Unity.** Đã mất asset thật hai lần vì việc này. Hãy commit thay vì stash.
- **Số liệu kinh tế là "customer-locked".** Dùng `VatNuoi2.xlsx` + `SuaLai4VatNuoi.xlsx` (bộ chính thức); **KHÔNG áp `VatNuoi3.xlsx`** (khác tỷ giá). `Docs_KichBan/VatNuoi_DoiChieu.md` là bản đối chiếu uy quyền. Point ≈ tiền thật trên ví web; **đổi tỷ giá là quyết định tài chính, không phải cân bằng game** — hỏi web/chủ dự án trước.
- **Chạy generator = ghi đè số gõ tay.** Muốn đổi số thật phải sửa generator `.cs`, không sửa `.asset`. Bump EditorPref `YW_MockDataGenerated_v4` sẽ kích regenerate khi mở Unity.
- **Mốc chết 2 tầng là đúng — đừng rút thành một.** Vật nuôi: 24h (không cho ăn) → 48h (đã cho ăn); cây: 8h → 20h; rùa 5→10 ngày. Khách đã đồng ý; con số "24h/8h" họ nhắc chỉ là mốc lúc *mới thả/trồng*.
- **1 ngày thật = 1 ngày game** (`GameTimeConfig.SecondsPerGameDay = 86400`). Tài liệu cũ ghi `60f` giây demo là đã lỗi thời.
- **Chữ nổi trên farm đã bị bỏ vì hiệu năng.** Dựng lại mesh chữ + `GetComponentsInChildren<Renderer>()` mỗi khung hình cho từng ô/con gây giật đúng như khách báo. **Thanh nước/đói dính CÙNG lỗi này** và *không* chịu ảnh hưởng của cờ ẩn chữ — nếu thêm nhãn nổi phải theo mẫu đã sửa (throttle 0.25s).
- **Chặn hành động người chơi phải có toast** (`NotifyBlocked`) — không được chặn im lặng (trừ lúc đang múa động tác, đã có thanh Huỷ riêng).
- **Nông sản ngắn ngày để `canSell=false` có chủ đích** (chúng là thức ăn chăn nuôi). Việc này chặn lỗ hổng mua-rẻ-bán-đắt in tiền; đừng "sửa" cho bán được.
- **Singleton của Bootstrapper phải `Destroy(this)`, không `Destroy(gameObject)`** — một object dùng chung từng huỷ nhầm `GameManager`.
- **`OnResourceHarvested` là event tĩnh dùng chung với Tutorial.** Một handler ném lỗi chặn cả chuỗi — bắn toast chặt/đào trực tiếp, đừng `+=` vào event chung.
- **`Assets/_Recovery/` là scene rác Unity tự sao lưu khi crash**, giờ đã gitignore. Bỏ qua nó.
- **Dung lượng APK ~96% là texture** (model NPC glTF rất nặng). Giảm bằng max-size/ASTC texture, không phải cờ Static/Instancing.

## 12. Bảo mật & danh mục bàn giao

- **Không có secret nào lưu trong repo** — không mật khẩu, token, API key, chứng chỉ, hay thông tin DB. Xin từ chủ dự án.
- **Khi bàn giao, đổi (rotate) mọi thông tin đăng nhập dùng chung**: truy cập VPS production (chuyển sang khoá SSH), PostgreSQL, `JWT_SECRET`, biến license Unity, và chữ ký Apple. Bất kỳ credential nào từng xuất hiện trong chat/log đều coi như đã lộ và phải đổi.
- Chữ ký iOS (bundle ID, team, cert/provisioning hoặc App Store Connect API key) cấu hình **ngoài** repo, trong CodeMagic/Xcode.
- VPS production chạy **nhiều service** — đừng restart nhầm (server game là `ywonder-game-server.service`; web và dự án khác là service riêng).

## 13. Nên đọc tiếp

| Tài liệu | Vì sao |
|---|---|
| `RULES.md` | Quy ước code & quy tắc làm việc với AI dùng để dựng dự án |
| `DESIGN.md` / `docs/DESIGN.md` | Hệ thống thiết kế UI ("Cozy Dark Palia") |
| `docs/CHANGELOG.md` | Lịch sử thay đổi theo ngày, theo module |
| `docs/CONTEXT_RECOVERY.md` | Nhật ký kỹ thuật lịch sử dài (chi tiết backend/ví điểm) |
| `Assets/_Project/Docs_KichBan/VatNuoi_DoiChieu.md` | Đối chiếu số liệu vật nuôi & bảng nào là chính thức |
| `Assets/_Project/Docs_KichBan/TruocKhiBuild_Checklist.md` | Danh mục kiểm trước khi build |
| `server/RUNBOOK_deploy_*.md` | Deploy server từng bước (runbook điểm danh là mẫu đầy đủ nhất) |

---

*Handoff README — Y WONDER GREEN FARM. Last structural survey of the codebase: 2026-08-07.*
