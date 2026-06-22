# 📱 CHECKLIST TỐI ƯU MOBILE (cheap wins) — Y WONDER GREEN FARM

> Mục tiêu: vài nghìn cube đất/đá chạy mượt trên điện thoại mà KHÔNG đụng logic/sự kiện.
> Tất cả bước dưới chỉ giảm **cách VẼ**, không tắt object → cây vẫn mọc, thú vẫn đói.
> Làm lúc **KHÔNG bấm Play** (Edit mode).

---

## 0. ĐO TRƯỚC (để biết có cải thiện thật)

1. Bấm **Play**.
2. Góc trên-phải **Game view** → bấm nút **`Stats`**.
3. Ghi lại 3 số: **Batches** · **SetPass calls** · **Tris** (số tam giác).
   - **Batches / SetPass calls ≈ số lệnh vẽ** → đây là số cần KÉO XUỐNG.
4. Bấm **Stop**. Làm các bước dưới. Đo lại để so.

> Vài nghìn cube riêng lẻ thường cho Batches vài nghìn → mục tiêu kéo về vài chục–vài trăm.

---

## 1. ⭐ GPU INSTANCING cho material cube (ăn nhiều nhất, 1 click)

**Tìm material cube:**
1. Trong **Scene**, bấm vào 1 khối cube đất.
2. Inspector → **Mesh Renderer** → mục **Materials** → bấm vào material đang gán (vd "Grass…", "Cube…").
3. Material đó sáng lên trong **Project** → bấm vào nó.

**Bật instancing:**
4. Inspector của material → tick **`Enable GPU Instancing`** ✅.
5. Làm tương tự cho **material đá** (bấm 1 khối stone → tìm material → tick).

> ⚠️ Để instancing ăn: cube **ĐỪNG mark Static** (xem mục 2). Vài nghìn cube GIỐNG HỆT nhau + cùng 1 material + cùng 1 mesh → Unity gộp còn vài lệnh vẽ.

---

## 2. STATIC cho vật KHÁC (nhà, cây nền, đá trang trí) — KHÔNG cho cube

Quy tắc đơn giản:
- **Cube đất/đá** (giống hệt, vài nghìn) → **GPU Instancing** (mục 1), **ĐỪNG** tick Static.
- **Nhà / cây nền / vật trang trí đứng yên** (mỗi cái 1 kiểu) → chọn chúng → góc phải-trên Inspector tick **`Static`** ✅ → Unity gộp tĩnh.

> Lý do tách: cube giống nhau hợp Instancing (nhẹ RAM); vật đa dạng hợp Static Batching. Tick Static cho cube sẽ TẮT instancing + ngốn RAM khổng lồ.

---

## 3. SHADOW (đổ bóng) — nặng nhất với mobile

**Trong URP asset** (`Assets/_Project/Settings/Mobile_RPAsset.asset` → bấm vào):
1. Mục **Shadows**:
   - **Max Distance**: hạ xuống **20–30** (mặc định ~50).
   - **Cascade Count**: để **1** hoặc **2** (đừng 4).
   - **Soft Shadows**: tắt (hoặc Low).
2. Đảm bảo URP asset Mobile này đang được DÙNG: **Project Settings ▸ Quality** → chọn tier **Mobile/Medium** trỏ tới `Mobile_RPAsset`.

**Cube nền không cần đổ bóng** (làm qua Prefab nếu cube là prefab):
3. Mở prefab cube → **Mesh Renderer** → **Cast Shadows = Off** · **Receive Shadows = bỏ tick**.
   - (Nếu cube là vật rời trong scene: chọn nhiều cùng lúc → đổi 1 lần.)

---

## 4. URP MOBILE asset — vài tick nhẹ máy

Bấm `Mobile_RPAsset.asset`:
- **HDR**: tắt (nếu không cần màu rực).
- **MSAA**: **2x** hoặc **Disabled** (đừng 8x).
- **Lighting ▸ Additional Lights**: **Disabled** hoặc **Per Vertex** (chỉ giữ 1 đèn mặt trời chính).
- **Render Scale**: 1.0 (máy yếu có thể 0.8).

---

## 5. TEXTURE — nén ASTC (giảm RAM/băng thông)

Với các texture nặng (model, đất, nhà):
1. Bấm file texture → Inspector → tab **Android** (icon Android).
2. **Max Size**: 1024 (hoặc 512 cho đất nền).
3. **Format**: **ASTC 6x6** (hoặc ASTC 8x8 cho nền).
4. Bấm **Apply**.

---

## 6. ÁNH SÁNG + QUALITY

- Scene chỉ nên có **1 Directional Light** (mặt trời). Tắt/xóa đèn realtime thừa.
- **Project Settings ▸ Quality**: build Android chọn đúng tier Mobile/Medium.

---

## 7. ĐO LẠI + TEST MÁY THẬT

1. Play → xem **Stats**: **Batches / SetPass calls** phải tụt MẠNH so với mục 0.
2. Build APK → test **điện thoại thật** (Editor không phản ánh đúng FPS mobile).

> Nếu sau cheap wins vẫn giật/nhiều batch → bước tiếp: **gộp mesh theo chunk** (bé code) — gộp cube thành cụm, giữ nguyên collider + BuildSurfaceCell + sự kiện.

---

---

## 🏙️ RIÊNG CHO THÀNH PHỐ (CityScene) — nhà/đường đa dạng, KHÁC material

> City KHÔNG phải cube giống nhau → **không dùng Instancing**. Dùng bộ khác:

**1. Mark Static hàng loạt** (Static Batching gộp được cả vật KHÁC material/khác hình, miễn đứng yên):
- Ctrl chọn hết nhà + đường + vật trang trí đứng yên (hoặc chọn nhóm cha) → tick **`Static`** → "Yes, change children".

**2. ⭐ Occlusion Culling (mạnh nhất cho city — nhà che nhau thì không vẽ cái khuất):**
- Đã tick Static là đủ cờ Occluder/Occludee.
- **Window ▸ Rendering ▸ Occlusion Culling** → tab **Bake** → bấm **Bake**.

**3. Giới hạn tầm nhìn xa:**
- Main Camera ▸ **Clipping Planes ▸ Far** giảm còn ~150–300.
- Bật **Fog** (Lighting ▸ Environment ▸ Fog) che chỗ cắt.

**4. (Sau, polish):** gộp texture thành **atlas** để giảm SỐ material → batching gộp được nhiều hơn (mỗi material vẫn tốn ≥1 lệnh vẽ).

> Cây chặt được / thú / nhân vật ở city → **vẫn KHÔNG Static**.

---

---

## 🧩 VẬT ĐỘNG / ĐẶC BIỆT (không Static được)

| Vật | Static? | Instancing? | Tối ưu chính |
|---|:--:|:--:|---|
| **Cây/đá KHAI THÁC** | ❌ | ✅ | Bật GPU Instancing trên material (vẫn chặt/ẩn/respawn từng cái) + LOD cây xa |
| **NPC** | ❌ | ❌ (skinned khó) | Animator ▸ **Culling Mode = Cull Update Transforms** (ngoài màn hình ngừng tính anim); ít NPC thì kệ |
| **Mặt nước** (shader động) | ❌ | ❌ | Tối ưu SHADER: material nước bản mobile, tắt reflection/refraction, **giới hạn diện tích**. 1 mặt nước = 1 draw call, lo overdraw chứ không lo batch |
| **Nhân vật / thú** | ❌ | ❌ | Vài con → kệ |

> ⭐ KEY: **Instancing dùng được cho vật ĐỘNG + ẩn-hiện** (cây chặt) — chỉ cần GIỐNG HỆT nhau. KHÁC với Static (phải đứng yên + không ẩn).

---

## 📌 GHI NHỚ
- **Culling tự có sẵn** — vật ngoài khung hình KHÔNG vẽ; sự kiện vẫn chạy. ĐỪNG tự `SetActive(false)` để "unload" (sẽ chết sự kiện).
- Instancing & Static & combine = chỉ gộp *cách vẽ*, **không đụng logic**.
- **Instancing** = vật GIỐNG HỆT (cùng mesh+material). **Static Batching** = vật ĐỨNG YÊN (khác material vẫn được). **Occlusion** = không vẽ cái bị che.
- Đo bằng **Stats / Profiler**, đừng đoán. Test máy thật.
