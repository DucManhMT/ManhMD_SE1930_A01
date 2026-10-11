# Repository Instructions — FUNews Assignment 02 (PRN232)

Kiến trúc chuẩn theo Đề bài PRN232 Assignment 02: **Hệ thống phân tán 4 Solutions độc lập** (Distributed System), áp dụng mô hình N-Tier truyền thống (Presentation - Business Logic - Data Access), **KHÔNG** áp dụng Clean Architecture phức tạp.

---

## 1. Mục tiêu & Nguyên tắc hệ thống (Invariants)

1. **Nền tảng công nghệ:**
   - .NET 8 SDK (`net8.0`), C# 12, Entity Framework Core 8, Microsoft SQL Server.
   - Frontend: ASP.NET Core Razor Pages / MVC kết hợp Bootstrap 5, Chart.js.
2. **Cấu trúc 4 Solutions độc lập (Mục 6 đề bài):**
   - `FUNewsManagement_CoreAPI.sln`: Quản lý nghiệp vụ chính, truy cập Database qua EF Core, cấp JWT + Refresh Token, Audit Log, Upload ảnh.
   - `FUNewsManagement_AnalyticsAPI.sln`: Web API độc lập xử lý Dashboard thống kê, bài viết Trending, gợi ý bài liên quan và xuất báo cáo Excel (`.xlsx`).
   - `FUNewsManagement_AIAPI.sln`: Web API độc lập gợi ý Tag từ nội dung bài viết và duy trì Learning Cache.
   - `FUNewsManagement_FE.sln`: Web App giao diện người dùng, tuyệt đối không kết nối trực tiếp DB, mọi giao tiếp qua `HttpClientFactory` + `DelegatingHandler`.
3. **Quy tắc giao tiếp phân tán:**
   - Frontend và các API độc lập giao tiếp qua REST API (JSON).
   - Core API là nguồn dữ liệu duy nhất sở hữu kết nối SQL Server và quản lý dữ liệu gốc.
   - Analytics API và AI API không kết nối DB Core trực tiếp mà giao tiếp qua HTTP hoặc duy trì bộ nhớ/cache riêng.
4. **Bảo mật & Phiên làm việc:**
   - JWT Authentication với Refresh Token flow (`POST /api/auth/refresh`).
   - Lưu trữ Access Token & Refresh Token an toàn trong Server-side Session Token Store (không nhúng token vào JS hay localStorage trần).
   - Tự động làm mới token trước khi hết hạn.
5. **Độ ổn định & Chịu lỗi:**
   - Áp dụng **Polly Retry Policy** cho các request GET an toàn qua `HttpClient`.
   - **Background Worker (`HostedService`)** định kỳ 6 giờ/lần gọi API đồng bộ dữ liệu vào cache.
   - **Offline Mode**: Khi API không thể truy cập, hiển thị banner cảnh báo, đọc dữ liệu từ cache/JSON cục bộ và vô hiệu hóa các thao tác CRUD.
6. **Thời gian thực & Trải nghiệm:**
   - **SignalR Notification System** (`/hubs/notifications`): Bắn thông báo thời gian thực khi có bài viết mới, lưu tối thiểu 10 thông báo gần nhất.
   - Mọi thao tác CRUD đều có modal xác nhận (Confirmation prompt).
   - Toàn bộ lời gọi API đều có Loading Indicator và Toast thông báo kết quả.

---

## 2. Quy trình làm việc (Task Workflow)

1. **Tuần tự thực hiện 17 Task Cards** (`A02-001` → `A02-017` trong `docs/a02/tasks/`).
2. **Mỗi lượt làm việc chỉ tập trung 1 task duy nhất:**
   - Đọc kỹ file mô tả `docs/a02/tasks/{TASK-ID}.md`.
   - Kiểm tra mã nguồn hiện tại, xác định phần tái sử dụng từ A01 và phần cần viết mới.
   - Triển khai code hoàn chỉnh (không để placeholder/pseudocode).
   - Chạy build và chạy test tương ứng trong `docs/a02/TEST_PLAN.md`.
   - Cập nhật tiến độ vào `docs/a02/PROGRESS.md`.
3. **Không phá vỡ các chức năng đã hoàn thành từ Assignment 01:**
   - Giữ nguyên các chức năng CRUD Account, Category, Tag, NewsArticle và tính năng phân quyền (Admin, Staff, Lecturer).
   - Giữ nguyên các tính năng nâng cao đã có: Duplicate tin tức, Lịch sử tin của bản thân Staff, Đổi mật khẩu tài khoản cá nhân.

---

## 3. Bản đồ tài liệu hướng dẫn

- `README_AI.md`: Điểm bắt đầu và luồng làm việc.
- `START_HERE.md`: Hướng dẫn kích hoạt kèm Master Prompt.
- `docs/a02/REQUIREMENTS.md`: Chi tiết yêu cầu đề bài (R01 – R21) và ma trận phân quyền.
- `docs/a02/ARCHITECTURE.md`: Thiết kế kiến trúc phân tán 4 Solutions.
- `docs/a02/API_CONTRACT.md`: Quy chuẩn toàn bộ endpoints, DTOs và HTTP Status Codes.
- `docs/a02/CROSS_CUTTING.md`: Kỹ thuật chuyên sâu: Refresh Token, Polly, HostedService 6h, Offline, SignalR, AI Cache.
- `docs/a02/DATABASE_MIGRATION.md`: Schema CSDL mở rộng cho Audit, Images, Views, Learning.
- `docs/a02/UI_STANDARD.md`: Tiêu chuẩn 12 màn hình giao diện và trạng thái UX.
- `docs/a02/TASKS.md`: Backlog 17 task cards.
- `docs/a02/TEST_PLAN.md`: Bộ kiểm thử 24 test cases (T01 – T24).
- `docs/a02/GIT_COMMIT_CONVENTION.md`: Quy chuẩn đặt tên Git Commit cho từng task.
- `docs/a02/RUNBOOK.md`: Hướng dẫn build, chạy và nghiệm thu sản phẩm.
