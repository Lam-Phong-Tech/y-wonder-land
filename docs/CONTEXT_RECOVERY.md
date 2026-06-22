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

## 📌 TRẠNG THÁI MỚI NHẤT (cập nhật 22/06/2026 — PHIÊN 3)

### 🎯 ĐANG CRUNCH DEMO — đã BUILD APK thử (đang tối ưu dung lượng + chờ khách chốt giá bán)
- **Mục tiêu:** APK chơi được vòng lặp Nông trại + Thành phố, OFFLINE. Đã build thử (1.2GB → đang giảm texture).
- **Team:** 1 dev + AI. Anh tự kiêm QC (được sửa thẳng module QC khi yêu cầu).

### Vừa hoàn thành — PHIÊN 3 (22/06) — xem CHANGELOG entry 22/06 cho đầy đủ
- **Toast** mọi hành động (thu hoạch/chặt-đào/mua-bán/câu cá). **EXP/Level tối giản** (`ExperienceManager` + HUD số thật). **Âm thanh KHUNG** (`AudioManager`, cần thả file `Resources/Audio/`).
- **Mobile #4** (camera kéo 1 ngón — `LookZone`) + **#5** (`UISafeArea`). **Resume người chơi cũ** (có save → bỏ Login+Cutscene vào thẳng game ở vị trí cũ).
- **Câu cá BẢN TẠM** (ẩn popup, 8.7s tự +1 cá). **Tưới không spam** (kiểm `IsBusy`). **Tutorial chống kẹt** (auto-nhảy bước đi-theo-NPC 90s). **Map khóa đảo Mỏ** + đổi thông báo "Chưa đủ điều kiện để di chuyển". **Ẩn nút cheat** trong Map.
- **Áp GIÁ KHÁCH CHỐT (22/06):** giá MUA con giống = cột **USDT** (+ thêm 3 con vịt/ngỗng/rùa); nông sản ngắn ngày = **THỨC ĂN không bán**. Chặt cây=**10 gỗ**/đào đá=**10 đá**.

### ⚠️ Việc Editor đang CHỜ ANH (nút thắt build)
- **Chạy lại 4 generator** (số giá mới): `Generate Mock Items` → `Crop Data` → `Animal Data` → `Shop Data`.
- **Gắn model**: cây (Crop Prefab) · 10 thú (`AnimalPrefabLibrary`). **Gắn**: `WaterSource`/`ShopZoneTrigger`/`HarvestableResource` (Tree/Rock) · `UISafeArea` lên GameHUD · City: collider+FishingSpot.
- **TỐI ƯU APK (đang làm):** GPU Instancing (xong) · Static map1/stonemap+nhà city (xong) · **GIẢM TEXTURE NPC 2048→512+ASTC** (APK 1.2GB do texture NPC `.glb` = 96%, 4 con 256MB) — dùng menu `Tools/Tối ưu Mobile/Nén Texture` hoặc chỉnh tay → **build lại đo**.
- Player Settings: Switch Platform Android · IL2CPP+ARM64 · package name · **Landscape (xong)** · thêm **CityScene vào Build Settings**. Xem `Docs_KichBan/TruocKhiBuild_Checklist.md`.

### Còn lại / Phase 2 (không chặn demo)
- **Exploit kinh tế** (phá chuồng dupe · 2 hệ chuồng AnimalPen-vs-BuildSurfaceCell · thú chết kẹt ô · đổi giờ máy reset lượt câu · POS lưu `(int)` tràn). Xem memory [[qc-audit-blindspots]].
- **Persistence DateTime offline** (cây/thú lớn-bù). **Thêm Chanh dây + dọn 7 cây lâu năm thừa** (khách chốt còn 3).
- **Settings volume/graphics chưa áp** · nhiều popup chỉ log không trao thưởng · **CHỜ KHÁCH chốt giá BÁN sản phẩm + cân bằng lời** (phiếu `PhieuHoi_Khach_GiaCa.docx`).

### 🔧 Lưu ý dev QUAN TRỌNG (đọc kỹ)
- **`giveTestLoadoutOnStart = false`** (ĐÃ TẮT cho build). Muốn test có sẵn đồ thì tạm đổi `true`, build để `false`.
- **Độ "lời":** ĐỪNG nói "kinh tế thủng/lời 300-500 lần" — SAI (bỏ qua thức ăn + 9 tháng + giới hạn lần thu). Lời thật ~250-400%. Giá BÁN chờ khách chốt.
- **`GameTimeConfig.SecondsPerGameDay = 60f`** (demo). Đổi 86400 cho bản thật + persistence DateTime.
- **Test cây/thú NGOÀI tutorial** (tutorial ép cây lớn 5s). Tắt `Force Run Tutorial For Testing`.
- **KHÔNG đụng code scale model cây** trong FarmTile (bù `cropParentLossy` đang đúng).
- **Module QC đã sửa phiên này** (báo rõ): `GameHUDController`, `TutorialManager`, `FarmInteractionController`, `FishingOverlay*`. NPC dùng glTF importer (không có nút Extract Textures).
