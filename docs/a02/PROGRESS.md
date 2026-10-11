# Bảng Theo Dõi Tiến Độ Triển Khai (Progress Tracker) — PRN232 A02

| Mã Task | Tên Nhiệm Vụ | Trạng Thái | Người Thực Hiện | Ngày Hoàn Thành | Ghi Chú / Bằng Chứng |
|:---:|:---|:---:|:---:|:---:|:---|
| **A02-001** | Baseline và đồng bộ tài liệu từ A01 | `DONE` | AI Agent | 2026-10-11 | Build 2 solutions 0 error, 100 client tests passed, tạo báo cáo hiện trạng |
| **A02-002** | Khởi tạo cấu trúc 4 Solutions độc lập | `DONE` | AI Agent | 2026-10-11 | Chuẩn hóa 4 .sln theo Option B, phân port 7000-7003, build thành công 0 error |
| **A02-003** | Core API: JWT Refresh Token Flow | `DONE` | AI Agent | 2026-10-11 | Endpoint /api/auth/refresh, Token Rotation, bảng RefreshToken, 3/3 tests pass |
| **A02-004** | FE: HttpClients, DelegatingHandler & Polly | `DONE` | AI Agent | 2026-10-11 | 3 Typed HttpClients, DelegatingHandler refresh 401, Polly Retry GET, 105 tests pass |
| **A02-005** | Database: Migration Schema bổ sung A02 | `DONE` | AI Agent | 2026-10-11 | Bảng AuditLog, NewsView, TagLearning, cột NewsImage, 5/5 tests pass |
| **A02-006** | Core API: Audit Logging & Ràng buộc toàn vẹn | `DONE` | AI Agent | 2026-10-11 | Tự động ghi Before/After JSON, GET /api/auditlog, chặn xóa quan hệ, 3/3 tests pass |
| **A02-007** | Core API & FE: Upload và hiển thị ảnh bài viết | `DONE` | AI Agent | 2026-10-11 | Upload PNG/JPG < 5MB, chặn 413/415, magic bytes, StaticFiles, preview modal & hiển thị bài viết |
| **A02-008** | Analytics API: Dashboard, Trending, Recommend | `DONE` | AI Agent | 2026-10-11 | Dashboard stats, Trending, Recommend 3 bài, Export ClosedXML, 7 tests pass |
| **A02-009** | FE & Analytics: Dashboard Chart.js & Export Excel | `DONE` | AI Agent | 2026-10-11 | Pie & Bar Chart.js, KPI Cards, Ajax Filter, Export Excel, 5 tests pass |
| **A02-010** | AI API: Tag Suggestions & Learning Cache | `DONE` | AI Agent | 2026-10-11 | Keyword engine, confidence score, Learning Cache weight boost, 7 tests pass |
| **A02-011** | FE: AI Tag Badges/Chips & Feedback Loop | `DONE` | AI Agent | 2026-10-11 | Badges/Chips chọn nhanh tag trên modal, Feedback loop AI, 5 tests pass |
| **A02-012** | FE: Background Worker HostedService 6h | `DONE` | AI Agent | 2026-10-11 | Worker nạp cache định kỳ 6h, OfflineStoreManager JSON, 3 tests pass |
| **A02-013** | FE: Chế độ hoạt động ngoại tuyến (Offline Mode) | `DONE` | AI Agent | 2026-10-11 | Banner Offline, tự động đọc cache snapshot, khóa cứng CRUD, 3 tests pass |
| **A02-014** | FE & Core: Màn hình Admin Audit Log & Account | `DONE` | AI Agent | 2026-10-11 | UI xem Before/After JSON, lọc Role Admin, chặn xóa Account có bài viết, 4 tests pass |
| **A02-015** | SignalR: Thông báo thời gian thực khi có bài mới | `DONE` | AI Agent | 2026-10-11 | Hub `/hubs/notifications`, Toast và chuông 🔔, 10 tin gần nhất, 3 tests pass |
| **A02-016** | Hoàn thiện UI/UX toàn hệ thống | `TODO` | AI Agent | — | Modals, Loading spinner, Alert, Accessibility |
| **A02-017** | Đóng gói bộ nộp bài và tài liệu hướng dẫn | `TODO` | AI Agent | — | 4 .sln, file SQL, README.md, Screenshots |

---

### Quy ước Trạng thái:
- `TODO`: Chưa thực hiện.
- `IN_PROGRESS`: Đang triển khai.
- `CODE_COMPLETE_UNVERIFIED`: Đã viết code nhưng chưa chạy kiểm thử thực tế.
- `DONE`: Hoàn thành toàn diện (đã build thành công, chạy kiểm thử pass tiêu chí).
- `BLOCKED`: Tạm dừng do thiếu phụ thuộc hoặc lỗi môi trường.
