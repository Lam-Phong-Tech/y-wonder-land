# 🔧 TECHNICAL DESIGN DOCUMENT (TDD) — Y WONDER GREEN FARM

> Tài liệu kỹ thuật **nội bộ team** (anh Lâm + bé). Mô tả kiến trúc backend thật theo **REST API riêng** (KHÔNG dùng UGS).
> Phiên bản: 0.1 — 16/06/2026. Trạng thái: **Đợt 1 (Profile + Tutorial) đã chạy thật**; các đợt sau là đề xuất.
> Thay thế phần kiến trúc lỗi thời trong `docs/ARCHITECTURE.md` (vốn viết cho UGS).

---

## 1. Mục tiêu & nguyên tắc
- **Lưu trữ thật trên server** thay cho PlayerPrefs local rải rác ở 6+ manager.
- **Offline-first**: mọi lời gọi mạng có timeout + try/catch; mất mạng/server lỗi → fallback cache local, game **không crash**, không chặn luồng chơi.
- **Server-authoritative cho dữ liệu nhạy cảm** (tiền, inventory, giao dịch) — chống cheat (chi tiết ở `SECURITY.md`).
- **Không phá QC**: chạm file PROTECTED (GameManager, TutorialManager) tối thiểu, có khoanh vùng.
- **Tái dùng cái có sẵn**: `UnityWebRequest` + `Newtonsoft.Json` + `Awaitable` (Unity 6) — KHÔNG cài package mới.

> **Bổ sung 06/07/2026:** Hành trình rõ ràng cho phần web-account -> game-account -> backend gameplay nằm ở `docs/WEB_GAME_BACKEND_JOURNEY.md`. Tài liệu đó là nguồn đọc trước khi nối economy/inventory/shop/daily limit vào server-authoritative, vì kịch bản cũ chưa nói rõ web hiện có sẽ trở thành nguồn tài khoản game như thế nào.
> **Bổ sung sau phỏng vấn 06/07/2026:** Yêu cầu trước mắt của sếp là khách đăng nhập bằng tài khoản web/cấp sẵn và chơi online realtime ở đảo công cộng. Realtime public chỉ gồm `city`/`mine`/đảo non-farm; farm là private state theo account. MVP sắp tới chưa làm nạp/rút; `Point`/web wallet là phase sau, còn lát gần nhất tập trung account + realtime.

## 2. Tổng quan kiến trúc

```
┌─────────────────────────── UNITY CLIENT ───────────────────────────┐
│  Game Systems (Economy, Inventory, Farm, Animal, Tutorial...)       │
│        │ gọi                                                        │
│  Backend Services Layer                                             │
│   ├─ AuthService            (token, userId — PlayerPrefs)           │
│   ├─ PlayerProfileService   (profile + tutorialCompleted)          │
│   └─ [Đợt sau] Economy/Inventory/FarmSyncService                    │
│        │ dùng                                                       │
│   ApiClient  (UnityWebRequest + Newtonsoft, try/catch + timeout)    │
│        │ fallback khi offline                                       │
│   LocalCache (PlayerPrefs: YW_Profile_Cache, YW_Auth_Token...)      │
└──────────────────────────────┬─────────────────────────────────────┘
                               │ HTTP/JSON (Bearer token)
┌──────────────────────────────▼─────────────────────────────────────┐
│  BACKEND (hiện: server stub Node/Express — sau: production)         │
│   Routes: /auth/register, /auth/login, GET|PUT /player/profile     │
│   Auth: JWT (TTL 30d) · Hash: bcrypt                               │
│   Store: hiện file JSON (data.json) → sau: PostgreSQL/MongoDB       │
└────────────────────────────────────────────────────────────────────┘
```

## 3. Stack — hiện tại vs cần chốt

| Tầng | Hiện tại (stub) | Cần khách/team chốt cho production |
|---|---|---|
| Ngôn ngữ server | Node.js + Express | Node / Go / Python — **CHỜ CHỐT** |
| Lưu trữ | File JSON (`data.json`) | **PostgreSQL** (khuyến nghị — quan hệ rõ) hoặc MongoDB — **CHỜ CHỐT** |
| Auth | JWT secret cứng, TTL 30d | JWT + refresh token, secret qua ENV; cân nhắc OAuth/social — **CHỜ CHỐT** (G3) |
| Mật khẩu | bcrypt (cost 8) | bcrypt cost ≥10 |
| Host | localhost:3000 | Máy case/VPS + HTTPS + domain; cần auto-start, backup, log, monitoring |
| Realtime | WebSocket MVP trong `server/` | Giữ WebSocket cho chat/presence public islands; Photon/Mirror chỉ cân nhắc nếu gameplay realtime phức tạp hơn |

> **Khuyến nghị của bé:** Node + Express + PostgreSQL cho MVP. Nếu sếp yêu cầu đặt server ở máy case, có thể chạy trên máy case trước nhưng phải có HTTPS, WebSocket Upgrade, backup DB hằng ngày, service auto-start và test từ điện thoại ngoài LAN. JSON store chỉ dùng dev/local.

## 4. Client — các lớp (đã code, file thật)
| Lớp | File | Vai trò |
|---|---|---|
| `BackendConfig` | `Scripts/Backend/BackendConfig.cs` | ScriptableObject: `baseUrl`, `requestTimeoutSec=5`, `useOfflineFallback`. **Không hardcode URL.** |
| `ApiClient` | `Scripts/Backend/ApiClient.cs` | `GetAsync/PostAsync/PutAsync<T>` → trả `ApiResult<T>{ok,data,status,error}`. Không bao giờ ném exception. |
| `AuthService` | `Scripts/Backend/AuthService.cs` | Đăng nhập/đăng ký ngầm; lưu `YW_Auth_Token`/`YW_Auth_UserId`. |
| `PlayerProfileService` | `Scripts/Backend/PlayerProfileService.cs` | Load (server→cache→default), Save (cache ngay→push server), `characterCreated`, `SetTutorialCompleted`. |

## 5. Luồng chính (đợt 1 — đã chạy)

**Đăng nhập + nạp hồ sơ**:
```
LoginScreenController.OnLoginClicked():
  AuthService.LoginAsync(username, password)
    → POST /auth/login
  PlayerProfileService.LoadProfileAsync()
    → GET /player/profile (Bearer)  ── lỗi ──▶ dùng YW_Profile_Cache
  nếu characterCreated=true → GameManager.StartGameFromProfile() → SetGameState(Cutscene)
  nếu characterCreated=false → SetGameState(Menu/CharacterSelect)

CharacterSelectController.OnConfirmYes():
  GameManager.StartGame()
    → ApplyCharacterInfo(name, gender, markCreated=true)
    → PUT /player/profile
    → SetGameState(Cutscene)
```

**Ghi nhớ tutorial** (tại `TutorialManager`):
```
StartTutorial(): nếu Profile.tutorialCompleted == true → bỏ qua tutorial
... cuối tutorial (OnTutorialItemUpgraded):
  PlayerProfileService.SetTutorialCompleted(true)
    → ghi cache + PUT /player/profile (best-effort)
```

## 6. Xử lý offline & lỗi
- `ApiClient`: timeout mặc định 5s, mọi nhánh lỗi → `ok=false` (không throw).
- `LoadProfileAsync`: server fail → đọc `YW_Profile_Cache`; rỗng → profile mặc định.
- `SaveProfileAsync`: **ghi cache TRƯỚC** rồi mới push (không mất dữ liệu); push fail chỉ log warning.
- **Cần làm thêm (đợt sau):** cờ "dirty" + hàng đợi retry khi có mạng lại; merge conflict khi 2 thiết bị ghi đè (hiện server `PUT` merge `{...current, ...incoming}` — last-write-wins, chưa đủ cho tiền).

## 7. Lộ trình mở rộng (đề xuất — khớp kế hoạch các đợt)
| Đợt | Service client thêm | Endpoint thêm | Ghi chú |
|---|---|---|---|
| 1 ✅ | Auth, PlayerProfile | /auth/*, /player/profile | Đã chạy thật |
| 2 | EconomyService, InventoryService | GET/PUT /player/economy, /player/inventory | **Server-authoritative tiền** — client KHÔNG tự cộng |
| 2 | Nối UI Login/Register thật vào REST | — | Đợt 1 đang đăng nhập ngầm |
| 3 | FarmSyncService, AnimalSyncService | /player/farm, /player/animals | Sync khi đổi scene + theo chu kỳ |
| 4 | Realtime public islands | WebSocket `/realtime` | Chat, presence, remote player state ở `city`/`mine`; không gồm farm |

## 8. Rủi ro kỹ thuật đã biết
- **POS đang lưu `int`** (EconomyManager) → tràn ở ~2.1 tỉ. Production nên dùng `long`/`bigint`.
- **PiggyBank tách rời EconomyManager** — gửi/rút chưa trừ tiền thật; phải nối trước khi lên server.
- **Lãi Heo Đất phải tính server-side** (chống chỉnh giờ máy) — cần TimeManager đồng bộ giờ server.
- **JWT secret đang cứng trong code stub** — production phải đưa vào ENV.
- Toàn bộ Economy/Inventory/Farm/Animal/Fishing **đang PlayerPrefs local** → là việc đợt 2–3.

## 9. Tài liệu liên quan
- `docs/DB_SCHEMA.md` — ERD + lược đồ bảng (B2).
- `docs/SECURITY.md` — anti-cheat, server-authoritative (B3 — sắp viết).
- `docs/BUILD_RELEASE.md` — quy trình lên Play Console (B4 — sắp viết).
- `Docs_KichBan/DiemMu_CanXinKhach.md` — số liệu cần xin khách.
