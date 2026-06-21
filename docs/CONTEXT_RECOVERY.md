# 🔄 Context Recovery — Y WONDER GREEN FARM (Bá Chủ Khu Rừng 3D)

# Dùng khi bắt đầu conversation MỚI với AI

## Cách dùng

Copy đoạn prompt bên dưới → paste vào chat mới → AI sẽ tự đọc và hiểu dự án.

---

## Prompt khởi động (copy từ đây)

```
Tôi đang phát triển game Unity 3D online tên Y WONDER GREEN FARM (Bá Chủ Khu Rừng 3D).
Workspace: d:\LamGameUnity\BaChuKhuRung3D
Engine: Unity 6 (6000.3.15f1) — URP. Backend: REST API riêng (KHÔNG dùng UGS).

Hãy đọc các file sau THEO THỨ TỰ để hiểu dự án:

1. RULES.md — Quy tắc tuyệt đối + QC Pass
2. Assets/_Project/Docs_KichBan/LoTrinh_Demo_Thu2.md — ⭐ LỘ TRÌNH demo + tiến độ (ƯU TIÊN khi đang crunch)
3. task.md — backlog + việc đã làm/đang chờ Editor/chờ khách
4. CHANGELOG.md — lịch sử phát triển (entry mới nhất = trạng thái gần nhất)
5. docs/DESIGN.md — Hệ thống thiết kế UI "The Tangible Playground"
6. Assets/_Project/Docs_KichBan/ThietKe_NPCShop.md — thiết kế hệ NPC shop
7. docs/ARCHITECTURE.md + docs/TECHNICAL_DESIGN.md — kiến trúc (đọc khi đụng backend)

(MEMORY.md auto-load mỗi phiên — đã có sẵn các kinh nghiệm/quyết định đúc kết.)

Sau khi đọc xong, cho tôi biết bạn đã hiểu gì về dự án + trạng thái lộ trình.
```

---

## Prompt nâng cao (nếu cần AI hiểu workflow)

```
Sau khi đọc các file trên, đọc thêm:
- unity-ai-workflow/docs/CODING_STANDARDS.md — Chuẩn code C#
- unity-ai-workflow/docs/NAMING_CONVENTIONS.md — Quy tắc đặt tên
- docs/SECURITY.md — anti-cheat, server-authoritative
- docs/BUILD_RELEASE.md — quy trình build Android + Play Console

Async pattern: Awaitable (Unity 6) — dùng thoải mái, KHÔNG cần UniTask
Brace style: Allman (dấu { xuống dòng mới)
UI: Unity UI Toolkit (UXML + USS), manual Q<T>() binding
Design: "The Tangible Playground" — solid colors, retro shadow, không blur
```

---

## Nếu đang làm dở task cụ thể

Thêm vào prompt:

```
Task đang làm dở: [mô tả task]
File đang sửa: [danh sách files]
Trạng thái: [đã xong gì, còn gì]
```

---

## 📌 TRẠNG THÁI MỚI NHẤT (cập nhật 21/06/2026)

### 🎯 ĐANG CRUNCH DEMO — deadline build APK THỨ 2 (xem `Docs_KichBan/LoTrinh_Demo_Thu2.md`)
- **Mục tiêu:** APK chơi được vòng lặp Nông trại + Thành phố, OFFLINE, model tạm chỗ thiếu. CẮT: online/API/web, tối ưu sâu, 4 NPC feature (KNX/Game/Maid/Gift), bạn bè/chat, cosmetic/VIP/Pet, mỏ/đảo endgame.
- **Team:** 1 dev + AI. Anh tự kiêm QC (được sửa thẳng module QC khi yêu cầu).

### Vừa hoàn thành — PHIÊN 2 (21/06, RẤT lớn)
- **Vật nuôi SỐNG theo thời gian** (`FarmAnimal` viết lại): đói/ra sản phẩm theo MỐC THỜI GIAN; **thanh HP (no/đói)** billboard trên đầu; **vụ cuối LÀM THỊT** (con biến mất, trả ô chuồng). Logic 10 con đầy đủ theo VatNuoi (produce + maxHarvests + meat). Tạo 17 item sản phẩm/thịt.
- **Cho ăn ĐÚNG tài liệu**: mỗi con ăn đúng thức ăn (Bò=Cỏ Voi/Khoai Lang...), sai loại → báo. Sửa tên `Cỏ khô→Cỏ Voi`, `Rau cải→Bắp cải`; nông sản chuyển category `food`.
- **THỜI GIAN THỰC** (`GameTimeConfig.cs`): 1 ngày game = 24h thực; **`SecondsPerGameDay`** (DEMO 60f · THẬT 86400f) = 1 ĐIỂM chuyển. **TƯỚI-GATE-LỚN**: cây chỉ lớn khi còn nước.
- **Thanh nước cây** + **MÚC NƯỚC** (`WaterSource` + item `watering_water_01`; tưới tốn 1 xô). **Cây mọc từ dưới lên** (`AnchorBaseToGround`).
- **Khung 10 cây LÂU NĂM** (hạt + SP + CropDefinition, tạm 1-lần-thu).
- **Build cost = VẬT LIỆU** (Ruộng FREE · Đường 4 Đá · Chuồng 4 Gỗ/ô) — kiểm+trừ+hoàn; chi phí ra **SerializeField** (`penWoodCost`/`pathStoneCost` trên `BuildModeOverlayController`).
- **Câu cá + đào đá CITY-ONLY** (gate `IslandTravelManager.CurrentIslandId=="city"`). **Khoá map** Mỏ/Hải Phú/Mộc Nhi.
- **Tutorial**: bỏ bước đào đá (/13) + fix 2 bug (nhảy bước, thả thú). **Popup "đang phát triển"** cho NPC chưa làm (`ShopZoneTrigger.comingSoon`).
- **UI**: tách tab "Sản phẩm" khỏi "Thú nuôi"; shop ẩn tab filter không có hàng; fix popup thú tràn chữ.
- **Respawn** cây+đá ra SerializeField (`respawnTimeSec` default 60s). **Shop wire đầy đủ** (18 hạt + 10 con; Mini Garden mua hết SP).

### ⚠️ Việc Editor đang CHỜ ANH (quan trọng — nút thắt demo)
- **Chạy 4 generator**: `Generate Mock Items` → `Generate Crop Data` → `Generate Animal Data` → `Generate Shop Data`.
- **Gắn model**: cây (Crop Prefab) · 10 thú (`AnimalPrefabLibrary`).
- **Gắn trigger/component**: `WaterSource`+collider lên ao · `ShopZoneTrigger`(+collider) cho nhà NPC · `HarvestableResource` Tree=cây/Rock=đá (đá ở CityScene).
- **#1 Thành phố cần BIỂN riêng**: anh đã thêm water plane (tag Water, y=-6) — còn thêm **collider** + **FishingSpot** ở khu nước city.
- **Set `respawnTimeSec`** ngắn (60s) trên prefab/đối tượng cây+đá CŨ (đang lưu 3600 — chọn nhiều, sửa 1 lần).
- **Mark Static + shadow/texture** (mobile mức 2) → **build APK** test máy thật.

### Còn lại / Phase 2 (không chặn demo)
- **EXP/level system + HUD số** (chưa có hệ thống; phạt EXP đang log).
- **Persistence offline (DateTime)**: cây/thú lớn-bù khi đóng app vài ngày — CẦN khi đổi `SecondsPerGameDay`→86400.
- **Cây lâu năm thu-NHIỀU-lần** (hiện 1-lần). 3 item `duck/goose/turtle_01` chưa có ItemDefinition.
- **Mobile #4 camera kéo, #5 safe area**. **Âm thanh/nhạc**. **Lưu trữ REST đợt 2-3**.
- **Visual "héo" cây** (đổi màu) chưa làm — báo khát bằng thanh đỏ.

### 🔧 Lưu ý dev QUAN TRỌNG (đọc kỹ)
- **`giveTestLoadoutOnStart = true`** (InventoryManager, tạo runtime nên không có Inspector) → mỗi lần Play tặng 100k POS + cả kho đồ. **NHỚ đổi `false` trước khi build demo thật.**
- **`GameTimeConfig.SecondsPerGameDay = 60f`** (demo). Đổi 86400 cho bản thật + làm persistence DateTime.
- **Test cây/thú phải NGOÀI tutorial** (tutorial ép cây lớn 5s — `GetGrowthTime` override khi `TutorialManager.IsActive`). Tắt `Force Run Tutorial For Testing`.
- **KHÔNG đụng code scale model cây** trong FarmTile (bù `cropParentLossy` đang đúng) — chỉnh hình qua model/wrap empty.
- Warning `ApiClient ConnectionError` (offline) là BÌNH THƯỜNG. NPC chưa làm warn "ShopZone chưa gán" → giờ thành "đang phát triển".
- **Module QC** đã sửa phiên này (báo rõ): `FarmTile`, `TutorialManager`, `FarmInteractionController`, `HarvestableResource`.
