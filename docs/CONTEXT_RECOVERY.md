# 🔄 Context Recovery — Y WONDER LAND
# Dùng khi bắt đầu conversation MỚI với AI

## Cách dùng
Copy đoạn prompt bên dưới → paste vào chat mới → AI sẽ tự đọc và hiểu dự án.

---

## Prompt khởi động (copy từ đây)

```
Tôi đang phát triển game Unity 3D online tên Y WONDER LAND (Bá Chủ Khu Rừng 3D).
Workspace: d:\LamGameUnity\BaChuKhuRung3D

Hãy đọc các file sau THEO THỨ TỰ để hiểu dự án:

1. RULES.md — Quy tắc tuyệt đối + QC Pass + UGS rules
2. docs/DESIGN.md — Hệ thống thiết kế UI "The Tangible Playground"
3. docs/ARCHITECTURE.md — Kiến trúc Unity 2022 LTS + UGS
4. docs/DATA_SCHEMA.md — Cấu trúc dữ liệu Cloud Save + Economy
5. docs/API_CONTRACTS.md — Blueprint tích hợp 8 UGS services
6. docs/CHANGELOG.md — Lịch sử phát triển (xem module nào đã xong)

Sau khi đọc xong, cho tôi biết bạn đã hiểu gì về dự án.
```

---

## Prompt nâng cao (nếu cần AI hiểu workflow)

```
Sau khi đọc 6 file trên, đọc thêm:
7. unity-ai-workflow/docs/CODING_STANDARDS.md — Chuẩn code C#
8. unity-ai-workflow/docs/NAMING_CONVENTIONS.md — Quy tắc đặt tên

Async pattern: UniTask (KHÔNG dùng Awaitable)
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
