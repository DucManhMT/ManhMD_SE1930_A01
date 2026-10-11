# Danh Sách 17 Nhiệm Vụ (Task Backlog) — PRN232 Assignment 02

Toàn bộ 17 Task Cards được tinh gọn và bám sát trực tiếp vào đề bài, **loại bỏ các task Clean Architecture phức tạp không cần thiết**. Thực hiện tuần tự từ `A02-001` đến `A02-017`.

---

## Bảng Tổng Hợp 17 Nhiệm Vụ

| Mã Task | Tên Nhiệm Vụ | Hạng Mục / Solution | Dependencies | Test IDs |
|:---:|:---|:---|:---:|:---:|
| [A02-001](tasks/A02-001.md) | Khảo sát Baseline và đồng bộ tài liệu từ A01 | Khảo sát / Setup | Không | `T01`, `T03` |
| [A02-002](tasks/A02-002.md) | Khởi tạo cấu trúc 4 Solutions độc lập theo chuẩn đề bài | Cấu trúc Solution | `A02-001` | `T01`, `T02` |
| [A02-003](tasks/A02-003.md) | Core API: JWT Authentication & Refresh Token Flow | Core API | `A02-002` | `T05`, `T06` |
| [A02-004](tasks/A02-004.md) | Frontend: Quản lý 3 HttpClients, DelegatingHandler & Polly | Frontend | `A02-003` | `T02`, `T17` |
| [A02-005](tasks/A02-005.md) | Database: Triển khai Migration Schema bổ sung cho A02 | Database & EF Core | `A02-002` | `T04`, `T07` |
| [A02-006](tasks/A02-006.md) | Core API: Triển khai Audit Logging & Ràng buộc toàn vẹn | Core API | `A02-005` | `T04`, `T07` |
| [A02-007](tasks/A02-007.md) | Core API & FE: Upload và hiển thị ảnh bài viết | Core & Frontend | `A02-004`, `A02-006` | `T08` |
| [A02-008](tasks/A02-008.md) | Analytics API: Dashboard stats, Trending & Recommend articles | Analytics API | `A02-004`, `A02-005` | `T10`, `T12`, `T13` |
| [A02-009](tasks/A02-009.md) | Frontend & Analytics: Dashboard Chart.js & Xuất báo cáo Excel | Frontend & Analytics | `A02-008` | `T11`, `T14` |
| [A02-010](tasks/A02-010.md) | AI API: Bóc tách từ khóa gợi ý Tag & Learning Cache | AI API | `A02-004`, `A02-005` | `T15`, `T16` |
| [A02-011](tasks/A02-011.md) | Frontend: Tích hợp AI Tag dạng Badges/Chips & Feedback Loop | Frontend | `A02-010` | `T16`, `T20` |
| [A02-012](tasks/A02-012.md) | Frontend: Xây dựng Background Worker (HostedService 6h) | Frontend | `A02-004`, `A02-008` | `T18` |
| [A02-013](tasks/A02-013.md) | Frontend: Chế độ hoạt động ngoại tuyến (Offline Mode) | Frontend | `A02-012` | `T19` |
| [A02-014](tasks/A02-014.md) | Frontend & Core: Màn hình Quản trị Audit Log & Quản lý Account | Frontend & Core | `A02-004`, `A02-006` | `T09`, `T20` |
| [A02-015](tasks/A02-015.md) | SignalR: Hệ thống thông báo bài viết mới thời gian thực | Frontend & Core | `A02-004`, `A02-006` | `T21` |
| [A02-016](tasks/A02-016.md) | Hoàn thiện UI/UX toàn hệ thống (Modals, Loading, Accessibility)| Frontend & Toàn hệ thống | `A02-007`, `A02-015` | `T20`, `T22`, `T23` |
| [A02-017](tasks/A02-017.md) | Đóng gói bộ nộp bài: 4 Solutions, SQL, README, Screenshots | Nghiệm thu | `A02-016` | `T24` |

---

## Phân Chia Thành 5 Giai Đoạn (Milestones)

- **Milestone 1 (Nền tảng & Xác thực):** `A02-001` → `A02-002` → `A02-003` → `A02-004` → `A02-005`
  - Thiết lập xong 4 solutions, chạy thông luồng JWT Authentication kèm Refresh Token giữa Frontend và Core API qua `HttpClientFactory` + `DelegatingHandler`.
- **Milestone 2 (Dữ liệu nâng cao & Đa phương tiện):** `A02-006` → `A02-007`
  - Hoàn thành Audit Logging tự động ghi nhận JSON Before/After và tính năng upload/xem ảnh bài viết.
- **Milestone 3 (Dịch vụ Thông minh - Analytics & AI):** `A02-008` → `A02-009` → `A02-010` → `A02-011`
  - Hoàn thành Analytics API (Dashboard, Trending, Recommend, Xuất Excel) và AI API (Gợi ý Tag, Badges/Chips, Learning Cache).
- **Milestone 4 (Tính năng Chịu lỗi & Thời gian thực):** `A02-012` → `A02-013` → `A02-014` → `A02-015`
  - Hoàn thành Background Worker 6h, Offline Mode đọc cache, Màn hình Admin Audit Log và SignalR Realtime Notifications.
- **Milestone 5 (Đánh bóng UX & Đóng gói nộp bài):** `A02-016` → `A02-017`
  - Rà soát toàn bộ UI/UX (Modals, Loading indicator, Toast, Accessibility), xuất script SQL cuối cùng, viết README.md và lưu ảnh chụp minh chứng.
