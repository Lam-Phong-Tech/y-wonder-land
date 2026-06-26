# 🚀 BUILD & RELEASE RUNBOOK — Y WONDER GREEN FARM (Android)

> Quy trình build + phát hành Android lên Google Play Console. Tài liệu kỹ thuật **nội bộ team**.
> Phiên bản: 0.1 — 16/06/2026. Engine: **Unity 6 (6000.3.15f1)**, URP.
> ⚠️ Nhiều mục **CHỜ CHỐT** (đánh dấu) — cần khách/BA cung cấp (xem `Docs_KichBan/DiemMu_CanXinKhach.md` mục G4).

---

## 0. Thông tin cần chốt TRƯỚC khi build (G4)
| Hạng mục | Giá trị | Trạng thái |
|---|---|---|
| Package name (ApplicationIdentifier) | vd `com.ywonderland.farm` | **CHỜ CHỐT** |
| Tên app hiển thị | **Y WONDER GREEN FARM** | ✅ Đã chốt (18/06) |
| Min SDK Android | đề xuất API 24 (Android 7.0) | **CHỜ CHỐT** |
| Target SDK | mới nhất Play yêu cầu (API 34+) | theo Play |
| Tài khoản Google Play Console | đã đăng ký ($25)? | **CHỜ CHỐT** — ai sở hữu |
| Máy test thật | model nào | **CHỜ CHỐT** |
| Hướng màn hình | Landscape / Portrait | **CHỜ CHỐT** |
| Icon + Splash | asset thật | cần art |

---

## 1. Chuẩn bị môi trường (1 lần)
- Unity 6000.3.15f1 + module **Android Build Support** (SDK, NDK, OpenJDK — cài qua Unity Hub).
- Xác nhận: `Edit > Preferences > External Tools` → SDK/NDK/JDK trỏ đúng (mặc định Unity quản lý OK).

## 2. Tạo Keystore (⚠️ LÀM SỚM — mất = không update được app)
> Keystore ký app. Mất hoặc đổi keystore → **không thể cập nhật** app đã đăng trên Play. Phải backup an toàn.

```powershell
# Dùng keytool đi kèm JDK của Unity (đường dẫn ví dụ — chỉnh theo máy):
keytool -genkeypair -v `
  -keystore ywonderland.keystore `
  -alias ywonderland `
  -keyalg RSA -keysize 2048 -validity 10000
```
- Lưu **keystore + mật khẩu** vào nơi an toàn (password manager + backup offline). **KHÔNG commit vào git.**
- Khuyến nghị: bật **Google Play App Signing** (Google giữ khoá ký phát hành, mình giữ upload key) — an toàn hơn nếu lỡ mất upload key.

## 3. Player Settings (Unity)
`Edit > Project Settings > Player > Android`:
- **Company/Product Name**, **Package Name** (mục 0).
- **Version** (hiển thị, vd `0.1.0`) + **Bundle Version Code** (số nguyên tăng dần mỗi lần lên Play — **bắt buộc tăng**).
- **Minimum API Level** (mục 0).
- **Scripting Backend: IL2CPP** + **Target Architectures: ARM64** (bắt buộc cho Play).
- **Publishing Settings** → trỏ keystore + alias + nhập mật khẩu.
- (Khuyến nghị) tắt log thừa, bật **Strip Engine Code**.

## 4. Build AAB (định dạng Play yêu cầu)
- `File > Build Settings > Android` → **Build App Bundle (Google Play)** ✅.
- Chọn **Release** (không phải Development Build).
- Build ra file `.aab`.
- (Tuỳ chọn test nhanh trên máy: build `.apk` Development để cài trực tiếp qua USB trước khi làm AAB release.)

## 5. Test trên máy thật trước khi đăng
- Cài APK qua USB (`adb install app.apk`) hoặc Unity **Build And Run**.
- Kiểm: khởi động, đăng nhập (server đợt 1), tutorial, vòng chơi cơ bản, FPS, RAM, không crash.
- Kiểm offline: tắt mạng → game vẫn vào được (fallback cache).

## 6. Lên Google Play Console (Internal Testing trước)
1. Tạo app mới trong Play Console (chọn ngôn ngữ, loại Game, miễn phí/có phí).
2. Khai báo: Privacy Policy URL, Content Rating, Target Audience, Data Safety (khai dùng dữ liệu gì — quan trọng vì có account/tiền).
3. **Internal Testing** → tạo release → upload `.aab` → thêm email tester.
4. Gửi link opt-in cho tester → cài qua Play Store.
5. Lên dần: Internal → Closed → Open → Production.

## 7. Versioning mỗi lần phát hành
- Tăng **Bundle Version Code** (+1) — Play từ chối nếu trùng/thấp hơn.
- Cập nhật Version hiển thị theo semver (major.minor.patch).
- Ghi changelog (release notes) cho tester.

## 8. Checklist phát hành nhanh
- [ ] Keystore đã tạo + backup an toàn (mục 2)
- [ ] Package name + version code đúng, version code đã tăng
- [ ] IL2CPP + ARM64 + Release
- [ ] AAB build thành công
- [ ] Test máy thật OK (online + offline)
- [ ] Backend production URL đã cấu hình trong `BackendConfig` (không còn localhost) — **CHỜ G2**
- [ ] Data Safety + Content Rating khai báo
- [ ] Upload Internal Testing → tester cài được

## 9. Rủi ro & lưu ý
- **Mất keystore = chết app** → backup ngay khi tạo.
- `BackendConfig.baseUrl` còn `localhost:3000` → build release sẽ **không kết nối được**; phải đổi sang URL production (chờ G2).
- IL2CPP build lâu hơn Mono — tính thời gian.
- Lần đầu Play Console duyệt có thể mất vài giờ–vài ngày.
- Min SDK quá cao loại bớt máy; quá thấp tốn công support → chốt với khách.

## 10. Liên quan
- `TECHNICAL_DESIGN.md` — cấu hình backend URL.
- `Docs_KichBan/DiemMu_CanXinKhach.md` — G4 (thông tin publish chờ khách).
