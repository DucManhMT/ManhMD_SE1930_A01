# Yêu Cầu & Truy Vết — PRN232 Assignment 02

Tài liệu ánh xạ trực tiếp từ Đề bài **PRN232 – ASSIGNMENT 02: Distributed and Intelligent News Management System**.

---

## 1. Bảng ánh xạ Yêu cầu Đề bài (Requirements Traceability Matrix)

| ID | Yêu cầu Đề bài (PDF) | Task phụ trách | Test ID kiểm thử |
|:---:|:---|:---:|:---:|
| **R01** | Thiết kế hệ thống phân tán gồm 4 project/solution độc lập: Core API, Analytics API, AI API, Frontend (.NET 8, EF Core, Web API, Razor Pages) | `A02-001`, `A02-002`, `A02-017` | `T01`, `T02`, `T24` |
| **R02** | CRUD Account, Category, Tag, NewsArticle sử dụng EF Core; Thực thi business rule ngăn chặn xóa các bản ghi có quan hệ phụ thuộc | `A02-005`, `A02-006`, `A02-014`, `A02-016` | `T03`, `T04`, `T07` |
| **R03** | Hỗ trợ JWT Authentication và cấp phát Refresh Token qua endpoint `/api/auth/refresh`; Tự động làm mới token trước khi hết hạn | `A02-003`, `A02-004` | `T05`, `T06` |
| **R04** | Cho phép upload ảnh cho bài viết, kiểm tra hợp lệ định dạng file (PNG, JPG, WebP) và kích thước file | `A02-007` | `T08` |
| **R05** | Audit Logging ghi lại thao tác CRUD của người dùng (User, Action, Entity, Before/After JSON, Timestamp); Màn hình quản trị lọc theo User/Entity | `A02-006`, `A02-014` | `T07`, `T09` |
| **R06** | Dashboard Analytics: `/api/analytics/dashboard` hiển thị tổng số bài viết theo category và status; Hỗ trợ lọc theo ngày, category, tác giả | `A02-008`, `A02-009` | `T10`, `T11` |
| **R07** | Trending Analytics: `/api/analytics/trending` liệt kê các bài viết thịnh hành (được xem nhiều nhất) | `A02-005`, `A02-008` | `T12` |
| **R08** | Gợi ý bài viết liên quan: `/api/recommend/{id}` gợi ý tối đa 3 bài viết dựa trên category hoặc tags | `A02-008` | `T13` |
| **R09** | Xuất báo cáo Excel từ Dashboard qua `GET /api/analytics/export` | `A02-009` | `T14` |
| **R10** | AI API: Endpoint `/api/ai/suggest-tags`, input `{"content": "..."}`, gợi ý tag kèm confidence scores; Hỗ trợ OpenAI hoặc mô phỏng keyword extraction; Duy trì **Learning Cache** | `A02-010`, `A02-011` | `T15`, `T16` |
| **R11** | Frontend: Quản lý nhiều HttpClient instances riêng biệt cho Core, Analytics, AI qua `HttpClientFactory` + `DelegatingHandler`; Tích hợp Polly Retry Policy | `A02-004` | `T02`, `T17` |
| **R12** | Frontend Background Worker (`HostedService`): Định kỳ 6 giờ/lần gọi API để nạp và làm mới dữ liệu cache | `A02-012` | `T18` |
| **R13** | Frontend Offline Mode: Tự động phát hiện khi API mất kết nối, hiển thị banner "Offline Mode", đọc dữ liệu từ cache/JSON, vô hiệu hóa thao tác CRUD | `A02-013` | `T19` |
| **R14** | Frontend Dashboard: Dùng Chart.js hiển thị biểu đồ Pie, Bar; Form CRUD thao tác qua Bootstrap Modals | `A02-009`, `A02-011`, `A02-016` | `T11`, `T20` |
| **R15** | SignalR Notification System: Hub `/hubs/notifications`, hiển thị thông báo thời gian thực (Toast hoặc chuông) khi có bài viết mới; Lưu ít nhất 10 thông báo gần nhất | `A02-015` | `T21` |
| **R16** | Màn hình News List (Staff): Phân trang, tìm kiếm, sắp xếp theo ngày tạo; Cột Tiêu đề, Category, Ngày tạo, Status; Chỉ báo màu cho Active/Inactive | `A02-001`, `A02-016` | `T03`, `T20` |
| **R17** | Quản lý Category & Tag: Ngăn trùng tên Tag; Tìm kiếm Tag; Xem số bài của Category, toggle IsActive; Xem các bài dùng Tag | `A02-006`, `A02-016` | `T04`, `T20` |
| **R18** | Quản lý Account (Admin): Lọc theo role (Admin/Staff); Chặn xóa tài khoản đã tạo bài viết; Đổi mật khẩu yêu cầu mật khẩu cũ | `A02-003`, `A02-014` | `T04`, `T06`, `T09` |
| **R19** | Tiêu chuẩn UX: Loading Indicator cho mọi thao tác API; Toast/Alert kết quả; Confirmation prompt cho mọi CRUD; Giao diện Responsive | `A02-004`, `A02-016` | `T20`, `T22` |
| **R20** | Tiêu chuẩn NFR: Log mọi API request (endpoint, thời gian, status); Hỗ trợ Accessibility (keyboard navigation, high contrast); Response time API < 1s | `A02-004`, `A02-016` | `T22`, `T23` |
| **R21** | Hướng dẫn nộp bài: 4 solutions (`.sln`), file SQL database (`FUNewsManagement.sql`), `README.md` (hướng dẫn setup, URLs, test accounts) và Screenshots | `A02-017` | `T24` |

---

## 2. Ma trận Phân quyền Hệ thống (Role Matrix)

| Chức năng / Endpoint | Khách vãng lai (Public) | Giảng viên (Lecturer) | Nhân viên (Staff) | Quản trị viên (Admin) |
|:---|:---:|:---:|:---:|:---:|
| Đọc tin tức công khai (Active) & Tin liên quan | Có | Có | Có | Có |
| Xem tin tức nội bộ / Trạng thái Inactive | Không | Không | Có | Có (Xem) |
| Tạo, sửa, xóa, nhân bản bài viết (News CRUD) | Không | Không | **Có** | Không |
| Upload ảnh bài viết | Không | Không | **Có** | Không |
| Gọi AI gợi ý Tag (`/api/ai/suggest-tags`) | Không | Không | **Có** | Không |
| Quản lý Chuyên mục (Category CRUD) | Xem | Xem | **Có** | Xem |
| Quản lý Thẻ tin (Tag CRUD) | Xem | Xem | **Có** | Xem |
| Xem lịch sử bài viết của chính mình (`/api/news/mine`) | Không | Không | **Có** | Không |
| Cập nhật hồ sơ & Đổi mật khẩu cá nhân | Không | **Có** | **Có** | Config |
| Xem Dashboard thống kê (Chart.js) | Không | Không | Không | **Có** |
| Xuất báo cáo Excel (`/api/analytics/export`) | Không | Không | Không | **Có** |
| Quản lý Tài khoản người dùng (Account CRUD) | Không | Không | Không | **Có** |
| Xem nhật ký thay đổi dữ liệu (Audit Log) | Không | Không | Không | **Có** |
| Nhận thông báo Real-time (SignalR bài viết mới) | Có | Có | Có | Có |

---

## 3. Bảo toàn các chức năng hoàn thành từ Assignment 01

Hệ thống A02 là bước nâng cấp trên nền A01, phải giữ vững hoạt động của:
- Tài khoản quản trị Admin cố định từ cấu hình `appsettings.json` (`admin@FUNewsManagementSystem.org` / `@@abc123@@`).
- Tài khoản Staff (`IsabellaDavid@FUNewsManagement.org` / `@1`) và Lecturer (`Alexander@FUNewsManagement.org` / `@1`) trong CSDL.
- Bộ mã hóa mật khẩu bảo mật BCrypt.
- Cơ chế OData Filter/Paging/Orderby trên các danh mục Category, Tag, News.
