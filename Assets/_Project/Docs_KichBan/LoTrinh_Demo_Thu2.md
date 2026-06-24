# 🎯 LỘ TRÌNH DEMO — Build APK Thứ 2

> Mục tiêu DUY NHẤT: **APK chơi được VÒNG LẶP NÔNG TRẠI cốt lõi — OFFLINE, 1 đảo, model nào thiếu thì dùng khối tạm.**
> Sếp cần THẤY game chạy, KHÔNG phải sản phẩm hoàn chỉnh. Mọi thứ ngoài vòng lặp cốt lõi → Phase 2.

---

## 📊 TIẾN ĐỘ (cập nhật 21/06 — sau PHIÊN 2)
**Code XONG (bé) — vòng lặp lõi + review gần đủ:**
- ✅ Economy số thật + **vật nuôi SỐNG theo thời gian** (HP đói, ra SP, làm thịt vụ cuối) · ✅ cho ăn đúng tài liệu
- ✅ **Thời gian thực** (GameTimeConfig 1 ngày=24h) + **tưới-gate-lớn** + thanh nước cây + **múc nước**
- ✅ Cây mọc từ dưới lên · ✅ khung 10 cây lâu năm · ✅ chặt cây GIỮ-ĐỂ-CHẶT
- ✅ **Build cost = vật liệu** (ruộng free/chuồng gỗ, SerializeField) · ✅ respawn cây+đá (SerializeField)
- ✅ **Câu cá + đào đá CITY-ONLY** · ✅ khoá map khác · ✅ popup "đang phát triển"
- ✅ Tutorial bỏ đào đá + fix 2 bug · ✅ tách tab túi/shop · ✅ wire shop đầy đủ
- ✅ Mobile #1/#2/#3 · ✅ cap FPS 60 · ✅ đổi đảo không ngập

**⚠️ Việc Editor của anh (NÚT THẮT — làm để demo chạy):**
- ⬜ **Chạy 4 generator**: `Generate Mock Items` → `Crop Data` → `Animal Data` → `Shop Data`
- ⬜ Gắn model cây (Crop Prefab) + 10 thú (`AnimalPrefabLibrary`)
- ⬜ Gắn `WaterSource`+collider lên ao · `ShopZoneTrigger`+collider nhà NPC · `HarvestableResource` cây(Tree)+đá(Rock, ở City)
- ⬜ **#1 City**: thêm collider + FishingSpot cho biển thành phố (đã có water plane)
- ⬜ Set `respawnTimeSec`=60 cho cây/đá cũ · đặt object lên map · **TẮT `giveTestLoadoutOnStart`** khi build
- ⬜ Mobile mức 2 (Static/shadow/texture) → **build APK** test máy thật

**Còn lại Phase 2 (không chặn demo):** EXP/level + HUD số · persistence DateTime offline · cây lâu năm thu-nhiều-lần · mobile #4/#5 · âm thanh · REST đợt 2-3.

---

## 🔁 VÒNG LẶP CỐT LÕI (cái phải chạy được)
```
Cuốc đất → Trồng hạt → Tưới (xô) → Cây lớn (mốc thời gian) → Thu hoạch (+nông sản +EXP)
   → Bán nông sản (+POS) → Mua hạt/thú → Thả thú vào chuồng → Cho ăn → Thu sản phẩm → Bán
Chặt cây (+gỗ) → Xây (đất/rào/chuồng)
```

---

## 🟢 LÀM (theo thứ tự ưu tiên)

### P0 — BẮT BUỘC (thiếu là demo không chạy)
1. **Economy số thật** — đổ data 8 cây + 10 thú vào generator (giá, sản lượng, EXP, POS). 🤖 Bé.
2. **Vòng trồng end-to-end** — cuốc→trồng→tưới→lớn→thu hoạch chạy mượt. (Đã có, cần gắn model cây + test.)
3. **Mua/bán** — tối thiểu 2 shop: **Farm Shop** (mua hạt+thú) + **Mini Garden** (bán nông sản). Gắn ShopZoneTrigger.
4. **Chăn nuôi cơ bản** — mua thú → thả chuồng → cho ăn → thu sản phẩm → bán. (Đã có.)
5. **Chặt cây lấy gỗ** — gắn `HarvestableResource` (type Tree) vào prefab cây mới. 🤖 Bé hướng dẫn / 👨‍💻 Anh gắn.
6. **Đặt lại object lên map** — vài ô đất, cây, NPC shop. 👨‍💻 Anh (Editor).
7. **Build APK không crash.** 👨‍💻 Anh.

### P1 — NÊN (nếu kịp)
- Đập đá lấy đá → **dùng khối đá tạm (primitive)**, không chờ 3D. Gắn HarvestableResource (type Stone).
- Tutorial dẫn dắt (đã có — sửa bỏ bước đào đá ở nông trại).
- Thêm shop: Hai Lúa, Thú Y.
- HUD hiện đúng số (POS/EXP/level).

### P2 — CÓ THÌ TỐT (cắt không tiếc nếu thiếu giờ)
- Câu cá (đã có, phụ).
- Chuyển sang đảo Thành phố (demo 1 đảo nông trại là đủ).
- VFX/SFX/polish.
- **Múc nước tưới cây** (khách 21/06): hành động "Múc nước" ở ao/hồ trên đảo (trigger + tag riêng) → item "Nước tưới cây" vào túi → tưới cây TỐN nước (thay xô auto). Tăng chiều sâu; vòng lặp lõi không cần. Chi tiết ở `task.md`.

---

## 🔍 BỔ SUNG TỪ RÀ SOÁT 21/06 (anh chơi thử) — chi tiết ở `task.md`
> Review thực tế lôi ra vài lỗ hổng chưa có trong P0-P2 gốc. Xếp ưu tiên:

**P1 (nên làm cho demo chỉn chu):**
- `[ ]` **#10 Popup "Tính năng đang phát triển"** cho NPC chưa dùng được (VIP/Maid/Pet/Game/Gift).
- `[ ]` **#7 Xây ruộng FREE** (cost 0) + **#6 Xây chuồng tốn GỖ** (không phải tiền) — đúng logic kinh tế.
- `[ ]` **#9 Tài nguyên tái sinh**: có code (respawn 1h) → chỉnh ngắn cho demo (Editor/game-time).
- `[x]` **#11 Khoá map khác** (chỉ Nông trại + Thành phố) — ĐÃ LÀM.
- `[ ]` **TẮT loadout test** trước khi build (đang BẬT tặng 100k+đồ mỗi lần vào).

**P2 / Editor:**
- `[ ]` **#1 Thành phố thiếu biển** → CityScene cần water plane riêng + FishingSpot (vì biển ở `farmOnlyObjects`, ẩn khi sang city).
- `[ ]` **EXP/level system + HUD số** — chưa có hệ thống.
- `[ ]` **Âm thanh/nhạc nền** — hỏi sếp demo có cần.
- `[ ]` **Persistence DateTime offline** (khi đổi sang 24h thật, để cây/thú lớn-bù khi đóng app).

---

## ✂️ CẮT HẲN — Phase 2 (NÓI RÕ VỚI SẾP)
| Cắt | Lý do |
|---|---|
| **Online / REST API / tích hợp web** | Demo offline-first, không cần server |
| **Tối ưu / LOD / hiệu năng** | Để sau khi gameplay xong |
| **4 NPC feature** (KNX/Game/Maid/Gift) | Mỗi cái 1 popup+logic riêng, không thuộc vòng lặp |
| **Bạn bè / Chat / Leaderboard** | Tính năng xã hội, phase sau |
| **Cosmetic / VIP / Pet / Maid** | Trang trí, không phải core |
| **Mining Scene (Lv10) / Đảo Hải Phú / Mộc Nhi** | Endgame content |

---

## 📱 MOBILE — tách 3 mức (đừng gộp)
| Mức | Việc | Build thứ 2? |
|---|---|---|
| **1. Điều khiển cảm ứng** | Joystick ✅ · còn #3 Pointer, #4 camera kéo, #5 safe area | ✅ PHẢI (vài tiếng) |
| **2. "Chạy được" (cheap settings, 1-2h)** | Cap FPS 60 ✅ · tắt/giảm shadow · nén texture (ASTC) · mark Static + batching · 1 directional light · `farmOnlyObjects` tắt cảnh xa ✅ | ✅ NÊN |
| **3. Tối ưu SÂU** | Profiling, LOD, draw-call, memory pool, atlas | ❌ PHASE 2 (hàng tuần) |

> Demo chỉ cần **CHẠY ĐƯỢC mượt trên 1 máy thật**, KHÔNG cần tối ưu hoàn hảo. Mức 3 để sau khi gameplay xong.

### Checklist "chạy được" (Mức 2 — Editor, bấm chọn là xong)
- [x] Cap FPS 60 (đã code trong SystemsBootstrapper)
- [ ] Project Settings ▸ Quality: chọn tier Medium/Mobile, **Shadow Distance thấp** (20-30) hoặc tắt
- [ ] URP Asset: giảm Shadow Cascades = 1, tắt HDR nếu không cần, MSAA 2x/off
- [ ] Texture model artist: Import ▸ Max Size 1024/512, Compression ASTC
- [ ] Mark **Static** cho cảnh vật không di chuyển (nhà, đá, cây nền) → batching
- [ ] Player Settings: IL2CPP + ARM64, Strip Engine Code
- [ ] Test trên **1 điện thoại thật** (đừng chỉ test Editor)

## 👥 PHÂN VAI
| 🤖 Bé (code/data — làm từ xa được) | 👨‍💻 Anh (Editor/scene — bé không làm từ xa được) |
|---|---|
| Đổ data economy vào generator | Gắn model vào prefab/CropDefinition |
| Viết/sửa script glue, bug | Đặt object (cây/đá/đất/NPC) lên map |
| Logic chặt cây/đập đá | Kéo thả ShopZoneTrigger / asset / Water |
| Script còn thiếu | Bake NavMesh, Build APK, test máy thật |

---

## 🗓️ MỐC THỜI GIAN (1.5 ngày)
- **Tối nay:** 🤖 Bé đổ data economy + viết script chặt cây/đá. 👨‍💻 Anh đặt cây/đất lên map + gắn model cây.
- **Mai sáng:** Vòng TRỒNG chạy end-to-end. 👨‍💻 Anh wiring shop + water + NPC.
- **Mai chiều:** Vòng CHĂN NUÔI + chặt cây. Test full loop, fix bug.
- **CN/Thứ 2 sáng:** Build APK, test trên điện thoại thật, vá lỗi build.

---

## 🚀 3 VIỆC BẮT ĐẦU NGAY
1. 🤖 Bé **đổ data economy** (cây + thú) vào generator → economy chuẩn số khách.
2. 🤖 Bé chuẩn bị **script + hướng dẫn gắn chặt cây/đập đá** (đá dùng khối tạm).
3. 👨‍💻 Anh **đặt lại object lên map** (đất + cây + 2 NPC shop) song song.
