# ✅ CHECKLIST TRƯỚC KHI BUILD APK — Y WONDER GREEN FARM

> Làm theo thứ tự. Cái nào ⚠️ là **dễ quên + gây lỗi demo**.

---

## A. DỌN CỜ TEST (quan trọng — kẻo demo lộ/sai)

- ✅ **ĐÃ TẮT loadout test:** `InventoryManager.cs` dòng 41 → `giveTestLoadoutOnStart = false` (bé sửa 22/06). *(Test lại có sẵn đồ thì tạm đổi true, build để false.)*
- **Tutorial:** `TutorialManager` → `Force Run Tutorial For Testing = false` (Inspector).
- **Map:** `MapPopupController` → `Show Cheat Buttons = false` (mặc định đã false).
- **Resume / mở đầu:** muốn demo CHẠY LẠI CUTSCENE từ đầu cho sếp xem → `GameManager` chuột phải ▸ **"Clear Save"** (hoặc tick `Always Start Fresh = true`). Muốn demo "người chơi cũ vào thẳng" thì để mặc định.

## B. CHẠY GENERATOR (để số mới vào asset)

Menu trên thanh Unity, chạy theo thứ tự:
1. `Generate Mock Items`  ← giá thú USDT mới + nông sản feed-only
2. `Generate Crop Data`
3. `Generate Animal Data`  ← chu kỳ/sản lượng (bake số `Days()`, hết 25s/40s cũ)
4. `Generate Shop Data`  ← shop bỏ bán nông sản, thú đủ 10 con đúng giá

## C. WIRING EDITOR CÒN THIẾU (nút thắt)

- Gắn **model**: cây (Crop Prefab) · 10 thú (`AnimalPrefabLibrary`).
- Gắn component: `HarvestableResource` (cây=Tree, đá=Rock) · `WaterSource`+collider lên ao · `ShopZoneTrigger`+collider nhà NPC.
- **City:** collider + `FishingSpot` cho biển thành phố · kéo Water+cảnh farm vào `farmOnlyObjects`.
- Gắn **`UISafeArea`** vào GameObject có UIDocument GameHUD (né tai thỏ).
- Set `respawnTimeSec` = 60 cho cây/đá cũ.

## D. TỐI ƯU MOBILE (phần lớn ĐÃ làm — kiểm lại)

- ✅ GPU Instancing trên material cube/cây/đá.
- ✅ Static cho `map1` + `stonemap` + nhà city.
- ✅ Far Clip ~150-200 + Fog.
- URP `Mobile_RPAsset`: Shadow Distance thấp, Cascade 1, MSAA 2x/off, HDR off.
- Texture: Android ▸ ASTC, Max Size 512/1024.
- Chỉ 1 Directional Light. Quality tier = Mobile/Medium.

## E. PLAYER SETTINGS (Android)

- ⚠️ **File ▸ Build Settings ▸ Android ▸ Switch Platform** *(lần đầu reimport lâu — làm SỚM).* Cần module **Android Build Support** (+ SDK/NDK) trong Unity Hub.
- Player Settings:
  - **Company Name** + **Product Name**.
  - **Package Name**: `com.<tencongty>.ywondergreenfarm` (chữ thường, không dấu).
  - **Default Orientation = Landscape** (đã làm).
  - **Minimum API Level**: Android 7.0 (API 24) trở lên.
  - **Scripting Backend = IL2CPP** · **Target Architectures = ARMv7 + ARM64** (ARM64 bắt buộc cho CH Play).
  - **Icon** game.

## F. SCENES IN BUILD (⚠️ hay quên → crash đổi đảo)

- **Build Settings ▸ Scenes In Build**: thêm **scene NỀN (Farm)** ở **index 0** + **CityScene**.
- *(Đổi đảo load CityScene additive — thiếu nó trong list = lỗi khi sang thành phố.)*

## G. TEST TRONG EDITOR (trước khi tốn thời gian build)

- ⚠️ **Console SẠCH lỗi đỏ** (compile + runtime).
- Chơi thử trọn vòng: cuốc→trồng→tưới→thu hoạch · mua thú→thả→cho ăn→thu→bán · chặt cây/đào đá · câu cá (city) · đổi đảo Farm↔City.
- Kiểm fix gần đây: toast, tưới không spam, EXP tăng, resume, map khóa.

## H. BUILD + CÀI MÁY THẬT

- **Build And Run** (cắm điện thoại, bật USB Debugging) hoặc **Build** ra APK rồi copy vào máy cài.
- ⏳ Build IL2CPP lần đầu **lâu (10-30 phút)** — bình thường.
- Test trên **điện thoại thật**: FPS (Stats), điều khiển cảm ứng, UI có bị tai thỏ che không.

---

## ⚠️ 4 LỖI HAY GẶP NHẤT
1. **Quên TẮT loadout test** → người chơi giàu sẵn.
2. **Quên thêm CityScene vào Build Settings** → sang thành phố crash.
3. **Console còn lỗi đỏ** → build fail hoặc chạy sai (code cũ).
4. **Chưa Switch Platform sang Android** / thiếu module Android trong Unity Hub.

> Keystore (ký số) chỉ cần khi nộp CH Play (AAB). Build APK test thì Unity tự ký debug — chưa cần lo.
