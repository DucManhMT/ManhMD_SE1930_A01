# Bảng Theo Dõi Tiến Độ Triển Khai (Progress Tracker) — PRN232 A02

| Mã Task | Tên Nhiệm Vụ | Trạng Thái | Người Thực Hiện | Ngày Hoàn Thành | Ghi Chú / Bằng Chứng |
|:---:|:---|:---:|:---:|:---:|:---|
| **A02-001** | Baseline và đồng bộ tài liệu từ A01 | `DONE` | AI Agent | 2026-10-11 | Build 2 solutions 0 error, 100 client tests passed, tạo báo cáo hiện trạng |
| **A02-002** | Khởi tạo cấu trúc 4 Solutions độc lập | `DONE` | AI Agent | 2026-10-11 | Chuẩn hóa 4 .sln theo Option B, phân port 7000-7003, build thành công 0 error |
| **A02-003** | Core API: JWT Refresh Token Flow | `TODO` | AI Agent | — | Endpoint `/api/auth/refresh` và bảng RefreshToken |
| **A02-004** | FE: HttpClients, DelegatingHandler & Polly | `TODO` | AI Agent | — | 3 Typed HttpClients và Retry Policy |
| **A02-005** | Database: Migration Schema bổ sung A02 | `TODO` | AI Agent | — | Chạy script SQL mở rộng |
| **A02-006** | Core API: Audit Logging & Ràng buộc toàn vẹn | `TODO` | AI Agent | — | Tự động ghi Before/After JSON và chặn xóa dữ liệu |
| **A02-007** | Core API & FE: Upload và hiển thị ảnh bài viết | `TODO` | AI Agent | — | Xử lý multipart/form-data ảnh và hiển thị UI |
| **A02-008** | Analytics API: Dashboard, Trending, Recommend | `TODO` | AI Agent | — | Endpoints tính toán số liệu và trending |
| **A02-009** | FE & Analytics: Dashboard Chart.js & Export Excel | `TODO` | AI Agent | — | Vẽ biểu đồ Pie/Bar và tải file Excel `.xlsx` |
| **A02-010** | AI API: Tag Suggestions & Learning Cache | `TODO` | AI Agent | — | Trích xuất từ khóa và lưu trọng số tag |
| **A02-011** | FE: AI Tag Badges/Chips & Feedback Loop | `TODO` | AI Agent | — | Badges/Chips chọn nhanh tag trên modal |
| **A02-012** | FE: Background Worker HostedService 6h | `TODO` | AI Agent | — | Worker chạy ngầm lưu snapshot cache định kỳ |
| **A02-013** | FE: Chế độ hoạt động ngoại tuyến (Offline Mode) | `TODO` | AI Agent | — | Banner Offline, đọc cache và khóa CRUD |
| **A02-014** | FE & Core: Màn hình Admin Audit Log & Account | `TODO` | AI Agent | — | UI xem lịch sử thay đổi và quản lý tài khoản |
| **A02-015** | SignalR: Thông báo thời gian thực khi có bài mới | `TODO` | AI Agent | — | Hub `/hubs/notifications`, Toast và chuông 🔔 |
| **A02-016** | Hoàn thiện UI/UX toàn hệ thống | `TODO` | AI Agent | — | Modals, Loading spinner, Alert, Accessibility |
| **A02-017** | Đóng gói bộ nộp bài và tài liệu hướng dẫn | `TODO` | AI Agent | — | 4 .sln, file SQL, README.md, Screenshots |

---

### Quy ước Trạng thái:
- `TODO`: Chưa thực hiện.
- `IN_PROGRESS`: Đang triển khai.
- `CODE_COMPLETE_UNVERIFIED`: Đã viết code nhưng chưa chạy kiểm thử thực tế.
- `DONE`: Hoàn thành toàn diện (đã build thành công, chạy kiểm thử pass tiêu chí).
- `BLOCKED`: Tạm dừng do thiếu phụ thuộc hoặc lỗi môi trường.
