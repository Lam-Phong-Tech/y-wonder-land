# 🔄 Context Recovery — YWONDERLAND (Bá Chủ Khu Rừng 3D)

# Dùng khi bắt đầu conversation MỚI với AI

## Cách dùng

Copy đoạn prompt bên dưới → paste vào chat mới → AI sẽ tự đọc và hiểu dự án.

---

## Prompt khởi động (copy từ đây)

```
Tôi đang phát triển game Unity 3D online tên YWONDERLAND (Bá Chủ Khu Rừng 3D).
Workspace: d:\LamGameUnity\BaChuKhuRung3D
Engine: Unity 6 (6000.3.15f1) — URP. Backend: REST API riêng (KHÔNG dùng UGS).

Hãy đọc các file sau THEO THỨ TỰ để hiểu dự án:

1. RULES.md — Quy tắc tuyệt đối + QC Pass
2. docs/DESIGN.md — Hệ thống thiết kế UI "The Tangible Playground"
3. docs/ARCHITECTURE.md — Kiến trúc Unity 6 + REST backend
4. docs/TECHNICAL_DESIGN.md — TDD: kiến trúc backend REST chi tiết
5. docs/DB_SCHEMA.md — ERD + lược đồ DB (thay DATA_SCHEMA cũ)
6. docs/API_CONTRACTS.md — Endpoint REST
7. docs/CHANGELOG.md — Lịch sử phát triển (xem module nào đã xong)
8. MEMORY.md — các kinh nghiệm được đúc kết

Sau khi đọc xong, cho tôi biết bạn đã hiểu gì về dự án.
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
