# AI Entry Point — FUNews Assignment 02

## 1. Thứ tự đọc tài liệu
1. [AGENTS.md](AGENTS.md): Quy tắc làm việc và ràng buộc kiến trúc phân tán 4 Solutions.
2. [START_HERE.md](START_HERE.md): Khởi động nhanh và Master Prompt điều phối.
3. [docs/a02/REQUIREMENTS.md](docs/a02/REQUIREMENTS.md): Bảng ánh xạ 21 yêu cầu đề bài (R01 – R21) vào Task và Test.
4. [docs/a02/ARCHITECTURE.md](docs/a02/ARCHITECTURE.md): Kiến trúc hệ thống phân tán 4 Solutions theo đề bài.
5. [docs/a02/API_CONTRACT.md](docs/a02/API_CONTRACT.md): Toàn bộ danh mục Endpoints, DTO và quyền truy cập.
6. [docs/a02/CROSS_CUTTING.md](docs/a02/CROSS_CUTTING.md): Hướng dẫn chi tiết JWT Refresh, Polly, HostedService 6h, Offline Mode, SignalR.
7. [docs/a02/TASKS.md](docs/a02/TASKS.md): Danh sách 17 task cards tinh gọn.

---

## 2. Thứ tự triển khai 17 Task Cards
Thực hiện tuần tự từng task, không nhảy cóc:
```
A02-001 (Baseline A01)
   ↓
A02-002 (Cấu trúc 4 Solutions)
   ↓
A02-003 (JWT Refresh Token - Core)
   ↓
A02-004 (HttpClients + DelegatingHandler + Polly - FE)
   ↓
A02-005 (Database Migration Schema)
   ↓
A02-006 (Audit Logging - Core)
   ↓
A02-007 (Upload Ảnh bài viết - Core & FE)
   ↓
A02-008 (Analytics API: Dashboard, Trending, Recommend)
   ↓
A02-009 (Dashboard Chart.js & Excel Export - FE & Analytics)
   ↓
A02-010 (AI API: Tag Suggestion & Learning Cache)
   ↓
A02-011 (Frontend: AI Tag Badges/Chips & Feedback)
   ↓
A02-012 (Background Worker HostedService 6h - FE)
   ↓
A02-013 (Offline Mode: Cache/JSON Fallback & Banner - FE)
   ↓
A02-014 (Màn hình Admin Audit Log & Quản lý Account - FE)
   ↓
A02-015 (SignalR Notification Realtime Hub - Core & FE)
   ↓
A02-016 (Hoàn thiện UX/UI, Modals, Loading, Accessibility)
   ↓
A02-017 (Đóng gói bộ nộp bài, Script SQL, README.md)
```

---

## 3. Tiêu chí hoàn thành (Definition of Done)
- Hoàn thiện mã nguồn đầy đủ theo Acceptance Criteria của từng task card.
- Build solution thành công không lỗi biên dịch.
- Chạy test kiểm thử thành công theo `docs/a02/TEST_PLAN.md`.
- Cập nhật trạng thái `DONE` và ghi chú kết quả vào `docs/a02/PROGRESS.md`.
