# 📱 TỐI ƯU UI ĐA MÀN HÌNH — Checklist test (Device Simulator)

> Mục tiêu: UI vừa & không bị tai thỏ/đục lỗ che trên mọi loại màn hình phổ biến.
> Cập nhật 22/06 — sau khi làm: Safe Area toàn cục + tinh chỉnh PanelSettings + clamp popup.

---

## 🧱 3 LỚP đang bảo vệ UI (hiểu để debug)

1. **Mật độ/size (phone↔tablet)** → PanelSettings = **Scale With Screen Size**, Reference **1280×720**, Match **0.5**.
2. **Tỉ lệ khung (phone dài↔tablet)** → UI **neo mép** (HUD) + popup **căn giữa** overlay full-screen.
3. **Tai thỏ / đục lỗ / bo góc** → **Safe Area** tự động: `SafeAreaInstaller` gắn `UISafeArea` cho MỌI UIDocument lúc chạy (không cần thao tác Editor).

---

## ▶️ CÁCH MỞ DEVICE SIMULATOR

1. Cửa sổ **Game** ▸ menu xổ góc trên-trái ▸ **Simulator** (hoặc `Window ▸ General ▸ Device Simulator`).
2. Chọn **device profile** ở thanh trên (đục lỗ giữa/trái/phải, tablet...).
3. Bật hiển thị **Safe Area** (icon overlay trong Simulator) để thấy vùng an toàn.
4. Bấm **Play** → UI thật mới áp Safe Area (Installer bật `ApplyInEditor` cho mọi popup).

> Ở Game view THƯỜNG (không Simulator): `Screen.safeArea` = full màn → padding = 0 → UI y như cũ. Bình thường.

---

## ✅ MA TRẬN TEST (chạy thử từng cái, Play rồi mở vài popup)

| Profile | Tỉ lệ | Kiểm |
|---|---|---|
| **Punch Hole Center** (1440×3088) | dài ~21:9 | Lỗ giữa cạnh ngắn không che currency/settings; HUD né lỗ |
| **Punch Hole Left** (1080×2340) | dài 19.5:9 | Khi landscape, lỗ ở mép trái/phải không che joystick/nút búa |
| **Punch Hole Right** (1080×2280) | dài 19:9 | Như trên, mép đối diện |
| **Wide Pill Cutout** (1200×2640) | rất dài | Vùng "pill" to không che nút góc |
| **Tablet Small** (1536×2048) | 4:3 | UI không quá to/thưa; popup căn giữa đẹp |
| **Tablet Large** (2048×2732) | 4:3 | Như trên; chữ không vỡ layout |

**Mỗi profile, mở thử:** Túi đồ · Shop (chạm nhà NPC) · Map · Settings · Build Mode (nút búa) → xem **nút X / tiêu đề / nội dung** có bị lỗ camera che hay tràn mép không.

**Điều khiển cần với tay (landscape):** joystick (dưới-trái), nút búa/Túi/Settings, Sprint ⚡, Jump ⬆️, nút X — đều nằm trong Safe Area, không bị lỗ che.

---

## 🩹 NẾU 1 POPUP BỊ TRÀN/CẮT trên màn nào đó

Popup đã căn giữa sẵn. Nếu panel cao/rộng hơn màn (thường máy landscape ngắn 720px), thêm **2 dòng** vào class `.xxx-panel` của popup đó (chỉ kích hoạt khi quá khổ, không đổi hình thường):

```css
max-width: 96%; max-height: 92%;
```

### Trạng thái clamp các popup (panel px cố định)
- ✅ ĐÃ thêm: `.inventory-panel` (900×600), `.shop-panel` (1024×600).
- ⏳ CHƯA thêm (thêm khi test thấy cắt): `.event-panel` (780×520), `.ap-panel` (Animal info), `.confirm-panel` (320), `.charselect-panel`, `.chat-expanded-panel`, các popup còn lại (Map/Settings/Quest/Friends/Mailbox/Leaderboard/Profile/PiggyBank/Workshop/Reward/Mục thư...).

> Phần lớn popup với Reference 1280×720 + Match 0.5 đã vừa trên màn ≥720px chiều cao. Clamp chỉ là lưới an toàn cho máy ngắn bất thường.

---

## ⚠️ Lưu ý
- `UISafeArea.cs` (đã có) + `SafeAreaInstaller.cs` (mới) — **không cần gắn tay** vào popup nào nữa; Installer tự quét. (GameHUD nếu đã gắn tay UISafeArea thì Installer bỏ qua, không trùng.)
- Đổi Reference Resolution ảnh hưởng **toàn bộ** scale UI — 1200×800→1280×720 thay đổi rất nhẹ (~2%) trên phone, chủ yếu sửa cho đúng tỉ lệ landscape.
- Nếu muốn UI tổng thể to/nhỏ hơn nữa: giảm/tăng Reference Resolution (số nhỏ hơn → UI to hơn).
