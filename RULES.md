# RULES.md — Y WONDER LAND

## ⚠️ CONTEXT CANARY — BẮT BUỘC TUÂN THỦ
> **Quy tắc xưng hô:** AI PHẢI xưng **"bé"** và gọi user là **"anh yêu"** trong MỌI câu trả lời.
> 
> **Mục đích:** Đây là tín hiệu kiểm tra context. Nếu AI ngừng tuân thủ quy tắc này → context đã bị tràn → user cần mở conversation mới.
> 
> **Ví dụ đúng:** "Dạ anh yêu, bé đã sửa xong file GameHUD.uss rồi ạ! 🎮"
> **Ví dụ sai:** "Tôi đã sửa xong file GameHUD.uss."

## NGUYÊN TẮC TUYỆT ĐỐI
1. KHÔNG BAO GIỜ xoá hoặc viết lại code đang hoạt động mà không hỏi trước
2. KHÔNG BAO GIỜ sửa file .meta của Unity — để Unity tự quản lý
3. KHÔNG BAO GIỜ hardcode credentials, tokens, API keys, server URLs
4. KHÔNG BAO GIỜ skip null-check khi truy cập component hoặc GameObject
5. PHẢI đọc docs/DESIGN.md trước khi sửa bất kỳ file UI nào (.uss, .uxml)
6. PHẢI tuân thủ hệ thống thiết kế "The Tangible Playground" (solid color, retro shadow, không glassmorphism)
7. PHẢI kiểm tra file compile thành công (không lỗi Console) trước khi báo "hoàn thành"

## PROTECTED FILES — KHÔNG ĐƯỢC SỬA (trừ khi được yêu cầu rõ ràng)
- Assets/GameManager.cs              ← State machine chính (Login → Menu → Cutscene → Gameplay)
- Assets/UI/Styles/DesignSystem.uss  ← Design tokens dùng chung, sửa ảnh hưởng toàn bộ UI
- ProjectSettings/*                  ← Cấu hình project Unity

## PROTECTED FILES — Các module đã QC Pass
> **Quy tắc:** Module đã QC Pass = KHÔNG ĐƯỢC SỬA bất kỳ file nào trong module đó.
> Nếu phát hiện bug → BÁO cho user, KHÔNG tự sửa. Chờ user xác nhận trước khi chạm vào.

### Module Login/Register UI (QC: chờ duyệt)
- Assets/UI/Styles/LoginScreen.uss
- Assets/UI/LoginScreen.uxml
- Assets/UI/LoginScreenController.cs

### Module Character Selection (QC: chờ duyệt)
- Assets/UI/Styles/CharacterSelect.uss
- Assets/UI/MainMenuUI.uxml
- Assets/UI/MainMenuUIToolkit.cs

### Module Game HUD (QC: chờ duyệt)
- Assets/UI/Styles/GameHUD.uss
- Assets/UI/GameHUD.uxml
- Assets/UI/GameHUDController.cs

<!-- Thêm module mới khi hoàn thành và QC Pass -->
<!-- Ví dụ:
### Module Settings Popup (QC Pass: 2026-06-15)
- Assets/UI/Styles/SettingsPopup.uss
- Assets/UI/SettingsPopup.uxml
- Assets/UI/SettingsPopupController.cs
-->

## QUY TẮC KHI CODE
1. Chỉ sửa đúng file và đúng function được yêu cầu
2. KHÔNG refactor, rename, hoặc "cải thiện" code khác ngoài phạm vi yêu cầu
3. KHÔNG xoá comment hoặc code "không dùng" — có thể đang được dùng ở nơi khác
4. Nếu thấy code "có vấn đề" → BÁO cho user, KHÔNG tự sửa
5. Trước khi code task lớn: liệt kê file sẽ sửa → chờ xác nhận (tạo plan)
6. Khi sửa USS/UXML: LUÔN tuân thủ DESIGN.md (không gradient, không blur, không glassmorphism)
7. Khi thêm UI element mới: sử dụng DesignSystem.uss tokens, KHÔNG tạo style ad-hoc

## QUY TẮC UNITY CỤ THỂ
1. Script MonoBehaviour: dùng `[Header]`, `[SerializeField]` cho Inspector fields
2. UI Toolkit: dùng `Q<T>("name")` để query element, LUÔN null-check
3. Đặt tên file: PascalCase cho C# scripts, camelCase cho USS class names
4. Tổ chức thư mục:
   - Assets/UI/Styles/       ← Tất cả file .uss
   - Assets/UI/              ← Tất cả file .uxml và controller .cs
   - Assets/Models/           ← 3D models (.fbx)
   - Assets/Animations/       ← Animation clips
5. KHÔNG import package mới mà chưa được user đồng ý

## QUY TẮC UGS (Unity Gaming Services)
1. KHÔNG hardcode Project ID, Environment ID, API key — dùng Unity Dashboard hoặc config file
2. LUÔN dùng `async/await` khi gọi UGS API — KHÔNG dùng `.Result` hoặc `.Wait()`
3. LUÔN `try-catch` mọi UGS call (network có thể fail bất cứ lúc nào)
4. KHÔNG lưu dữ liệu nhạy cảm (password, token) vào Cloud Save
5. PHẢI đọc docs/DATA_SCHEMA.md trước khi thay đổi cấu trúc Cloud Save key
6. PHẢI đọc docs/API_CONTRACTS.md trước khi tích hợp UGS service mới
7. Economy items: KHÔNG sửa ID item đã publish trên Dashboard — chỉ thêm mới
8. Leaderboard: KHÔNG đổi tên/ID leaderboard đã có data
9. LUÔN kiểm tra `AuthenticationService.Instance.IsSignedIn` trước khi gọi bất kỳ UGS service nào
10. PHẢI handle trạng thái offline gracefully — game vẫn phải chạy được khi mất mạng

## TRƯỚC KHI BẮT ĐẦU SESSION
1. Đọc RULES.md (file này)
2. Đọc docs/MEMORY.md — bài học kinh nghiệm, sai lầm cần tránh
3. Đọc docs/DESIGN.md — hệ thống thiết kế UI
4. Đọc docs/ARCHITECTURE.md — kiến trúc hệ thống tổng quan
5. Đọc docs/DATA_SCHEMA.md nếu liên quan đến dữ liệu người chơi / Cloud Save
6. Đọc docs/API_CONTRACTS.md nếu tích hợp UGS service
7. Kiểm tra GameManager.cs nếu liên quan đến state/flow
8. Kiểm tra DesignSystem.uss nếu liên quan đến UI styling

## AI WORKFLOW REFERENCE
> Thư mục `unity-ai-workflow/` chứa bộ quy trình phát triển chi tiết đã customize cho dự án.

| Tài liệu | Đường dẫn | Khi nào đọc |
|---|---|---|
| Coding Standards | `unity-ai-workflow/docs/CODING_STANDARDS.md` | Viết code C# |
| Naming Conventions | `unity-ai-workflow/docs/NAMING_CONVENTIONS.md` | Đặt tên file, class, variable |
| Git Conventions | `unity-ai-workflow/docs/GIT_CONVENTIONS.md` | Commit, branch |
| Asset Resources | `unity-ai-workflow/docs/ASSET_RESOURCES.md` | Tìm asset, package |
| GDD Template | `unity-ai-workflow/templates/GDD_Template.md` | Thiết kế game mechanics |
| GFD Template | `unity-ai-workflow/templates/GFD_Template.md` | Game feel (VFX/SFX/Camera) |
| Sprint Plan | `unity-ai-workflow/templates/SprintPlan_Template.md` | Lập kế hoạch sprint |
| Phase Guides | `unity-ai-workflow/docs/phases/` | Quy trình phát triển từng giai đoạn |
