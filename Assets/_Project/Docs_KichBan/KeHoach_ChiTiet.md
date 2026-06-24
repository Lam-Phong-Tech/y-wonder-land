# 🗂️ KẾ HOẠCH CHI TIẾT (LỚN → TUẦN → NGÀY) — YWONDERLAND

> Đi kèm `KeHoach_BanGiao.md` (chiến lược tổng). File này để **làm theo & báo cáo hằng ngày**.
> Lập 16/06. Deadline phát hành ≈ **01/07**. *Giả định demo nội bộ tiếp theo: **CN 22/06** (anh chỉnh nếu khác).*

---

## TẦNG 1 — 3 TRỤ MỤC TIÊU (nhìn vào đây để không lạc)
1. **PHÁT HÀNH** — game lên được CH Play (Internal Testing), chạy ổn trên Android thật.
2. **ONLINE TỐI THIỂU** — tài khoản + lưu cloud (profile/inventory/tiền) + nối web khách. **KHÔNG** realtime/chat/bạn bè (hoãn).
3. **TRẢI NGHIỆM THẤY ĐƯỢC** — vòng lặp chơi mượt + các yêu cầu bổ sung NHỎ khách vừa nêu (để demo sau ghi điểm).

> ⚖️ Game online khó ở chỗ phải sync server — nên ta **giữ online ở mức tối thiểu**, đừng ôm realtime.

---

## TẦNG 2 — THỨ TỰ ƯU TIÊN (trả lời: kịch bản hay bổ sung trước?)
Làm theo thứ tự này, KHÔNG làm kịch bản full, KHÔNG làm hết bổ sung:

1. **Hạ tầng phát hành + online tối thiểu** (Trụ 1+2) — *nền của "thu tiền", làm SONG SONG ngay từ ngày 1.*
2. **Yêu cầu bổ sung NHỎ, nhìn thấy được** — vì khách vừa nêu, demo sau sẽ soi → ROI cao:
   ô gieo hạt · NPC bước-vào-tương-tác · nhân vật cao hơn · camera đỡ chóng mặt · thêm cá · Đảo 1 mua/bán.
3. **Vá ổn định vòng lặp cốt lõi** (phần kịch bản đã có) cho mượt trên mobile.
4. **HOÃN** (tối giản/khóa "Sắp ra mắt"): Cosmetic đầy đủ · Xã hội · Minimap · 3 đảo · Animal grid 4x4 · Workshop sâu.

> Vì sao bổ sung trước kịch bản-full? Phần lớn bổ sung là việc NHỎ + khách VỪA thấy → đáp ứng tạo niềm tin. Còn module kịch bản chưa đụng (Hải Phú, Cosmetic...) khách chưa soi ngay.

---

## TẦNG 3 — KẾ HOẠCH THEO NGÀY (đến demo 22/06)
> Ký hiệu: 🅰️ anh (Unity/Editor/build/nối web) · 🅱️ bé (code) · 🎨 artist. ⏳ = chờ thông tin ngoài.

### Ngày 1 — T3 16/06 (hôm nay)
- 🅰️🅱️ ✅ Xong nền backend đợt 1 (profile + tutorial lưu thật, server stub).
- 🅰️ Gửi BA 4 việc cần chốt (scope, **API web**, Google Play account, máy test). ⏳
- 🎨 Nhận brief model MVP ưu tiên (nhân vật, vài cây/vật nuôi, building Đảo 1).

### Ngày 2 — T4 17/06
- 🅰️ **Build APK thử & cài lên điện thoại Android thật** (lộ lỗi mobile sớm — quan trọng nhất).
- 🅱️ Mục dễ-ăn-điểm #1: **Ô vuông đánh dấu nơi gieo hạt** (FarmTile highlight).
- 🅰️ Player Settings cho Android (package name, orientation, min SDK).

### Ngày 3 — T5 18/06
- 🅱️ Mục #2: **Tương tác NPC bằng bước vào vùng** (trigger, bỏ bấm).
- 🅱️ Mục #3: **Nhân vật cao hơn** (chỉnh scale chuẩn, tránh scale nhân chồng).
- 🅰️ Tối ưu điều khiển cảm ứng (joystick/tap) trên máy thật.

### Ngày 4 — T6 19/06
- 🅱️ Mục #4: **Camera đỡ chóng mặt** + **giới hạn tầm nhìn** (far clip/culling) cho FPS.
- 🅱️ Mục #5: **Thêm item + hoạt ảnh thú ăn** (Feed VFX rắc thức ăn).
- 🎨 Giao đợt model 1.

> ⏳ **NỐI WEB: ĐỂ SANG BÊN** (chốt 16/06) — chờ BA gửi cơ chế đăng nhập web. Có thì chen vào làm ngay; không thì cứ làm mục khác. Server stub + lưu cloud của mình vẫn chạy độc lập, không bị chặn.

### Ngày 5 — T7 20/06
- 🅱️ **Đảo 1 (Farm) mua/bán sản phẩm** (tận dụng khung Shop + MerchantNPC).
- 🅱️ **Thêm cá** (data + nối model artist).
- 🅰️ Gắn model artist đợt 1 vào game.

### Ngày 6 — CN 21/06
- 🅰️🅱️ **QA toàn vòng lặp trên Android thật** + sửa bug chặn.
- 🅰️ Quay **video demo** vòng lặp chính (để báo cáo BA/khách).
- 🅱️ Polish nhẹ thứ nhìn thấy (âm thanh/hiệu ứng cơ bản).

### Ngày 7 — T2 22/06 — 🎯 DEMO NỘI BỘ
- 🅰️ Chốt bản demo: vào game → vòng lặp → lưu cloud → (nếu kịp) nối web.
- 🅰️ Báo cáo BA: đã xong / đang làm / rủi ro (dùng mẫu bên dưới).

### Tuần 2 (23/06 → 01/07) — rút gọn (chi tiết hóa sau demo)
- Nối nốt dữ liệu thật qua API · QA đa máy · build **AAB release** + keystore · **nộp Internal Testing chậm nhất ~27/06** · buffer.

---

## 📋 MẪU BÁO CÁO NGÀY (copy gửi BA — phi kỹ thuật)
```
[YWONDERLAND] Báo cáo ngày dd/mm
✅ Hôm nay xong: ...
🔄 Đang làm: ... (≈ %)
⛔ Vướng/cần hỗ trợ: ... (vd: cần API đăng nhập từ bên web)
➡️ Ngày mai: ...
📹 (đính kèm ảnh/video nếu có)
```

## 📋 MẪU BÁO CÁO TUẦN
```
[YWONDERLAND] Tuần dd/mm – dd/mm
- Tiến độ chung: ≈ % so với mục tiêu phát hành
- Đã đạt: ...
- Rủi ro tiến độ: ... + đề xuất xử lý
- Tuần tới: ...
```

> 💡 Nhờ BA dùng báo cáo này "bán" thông điệp với khách: *bản lên chợ đúng hạn, cải tiến cập nhật liên tục sau* — và mọi rủi ro đều có văn bản (tự bảo vệ anh).
