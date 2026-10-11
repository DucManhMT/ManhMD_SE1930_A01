# FUNewsManagement System — Hướng Dẫn Vận Hành & Nghiệm Thu (Assignment 02)

**Môn học:** PRN232 - Advanced C# Programming (.NET 8.0)  
**Bài tập:** Assignment 02 (Distributed Architecture — 4 Independent Solutions)  
**Sinh viên thực hiện:** ManhMD  
**Lớp:** SE1930  
**Nền tảng:** .NET 8.0 SDK (`net8.0`), C# 12, Entity Framework Core 8, Microsoft SQL Server, SignalR, Chart.js  

---

## 1. Kiến Trúc Hệ Thống: 4 Solutions Độc Lập (Mục 6 Đề Bài)

Hệ thống được thiết kế theo đúng mô hình kiến trúc phân tán với **4 Solution độc lập**, áp dụng mô hình N-Tier truyền thống (Presentation - Business Logic - Data Access / HttpClients), hoàn toàn không áp dụng Clean Architecture phức tạp:

```
├── FUNewsManagement_CoreAPI.sln          # Solution 1: Core API Nghiệp vụ chính & Database EF Core
│   ├── FUNewsManagement_CoreAPI/         # Presentation Layer: Controllers (REST/OData), Auth, Uploads, Audit
│   ├── FUNews.BusinessLogic/             # Business Logic Layer: Services, JWT, Audit Logging, Validations
│   └── FUNews.DataAccess/                # Data Access Layer: Scoped DbContext, Entities, Repositories, DAOs
│
├── FUNewsManagement_AnalyticsAPI.sln     # Solution 2: Analytics & Reporting Web API
│   └── FUNewsManagement_AnalyticsAPI/    # Dashboard số liệu, Trending, Gợi ý bài liên quan, Xuất file Excel (.xlsx)
│
├── FUNewsManagement_AIAPI.sln            # Solution 3: AI Tag Engine & Learning Cache
│   └── FUNewsManagement_AIAPI/           # Gợi ý thẻ tin (Keyword Matcher + Confidence) & Learning Cache (Weight boost)
│
├── FUNewsManagement_FE.sln               # Solution 4: Frontend Web App (Razor Pages + SignalR)
│   ├── FUNewsManagement_FE/              # Presentation UI: Razor Pages, SignalR Hubs, Toast, Modals, Bootstrap 5
│   ├── FUNews.Client.BusinessLogic/      # Client BLL: Client Services, ViewModel formatting, OfflineStoreManager
│   └── FUNews.Client.DataAccess/         # Client DAL: Typed HttpClients, DelegatingHandler (Refresh), Polly Retry
│
├── FUNewsManagement.sql                  # File CSDL tổng hợp (Schema, Constraints, A02 Tables, Seed Data)
├── Screenshots/                          # Thư mục ảnh chụp minh chứng 7 chức năng nghiệm thu
├── docs/a02/                             # Tài liệu chi tiết: Architecture, Requirements, Tasks, Evidence, Test Plan
└── tests/                                # Bộ kiểm thử tự động 289 Tests (xUnit)
    ├── FUNews.Tests/                     # 154 Tests Backend (Core API, Analytics API, AI API, Token Rotation)
    └── FUNews.Client.Tests/              # 135 Tests Frontend (Clients, Polly, HostedService, SignalR, Offline, UI/UX)
```

> [!IMPORTANT]
> **Quy tắc cô lập Frontend:**  
> Solution Frontend (`FUNewsManagement_FE`) **hoàn toàn không kết nối trực tiếp CSDL**, không chứa `DbContext`, không tham chiếu `Microsoft.EntityFrameworkCore`. Mọi giao tiếp dữ liệu đều thông qua `Typed HttpClient` + `DelegatingHandler` tự động làm mới Token khi gặp mã HTTP 401.

---

## 2. Cấu Hình Cổng Mạng & Swagger URLs

| Dự án (Project) | Giao thức / Cổng mạng (Ports) | Swagger / Giao diện chính |
|:---|:---|:---|
| **Frontend Web App** | `https://localhost:7000` / `http://localhost:5000` | `https://localhost:7000` (Giao diện Web) |
| **Core Web API** | `https://localhost:7001` / `http://localhost:5001` | `https://localhost:7001/swagger` |
| **Analytics Web API** | `https://localhost:7002` / `http://localhost:5002` | `https://localhost:7002/swagger` |
| **AI Web API** | `https://localhost:7003` / `http://localhost:5003` | `https://localhost:7003/swagger` |

---

## 3. Tài Khoản Kiểm Thử Hệ Thống (Test Accounts)

| Vai trò (Role) | Email | Mật khẩu | Nguồn dữ liệu | Quyền hạn trong hệ thống |
|:---|:---|:---|:---|:---|
| **Admin** | `admin@FUNewsManagementSystem.org` | `@@abc123@@` | `appsettings.json` | Dashboard Chart.js, Xuất Excel, Quản lý tài khoản, Xem Audit Log JSON |
| **Staff** | `IsabellaDavid@FUNewsManagement.org` | `@1` | Bảng `SystemAccount` (AccountID: 3, Role: 1) | CRUD Bài viết, Upload ảnh, Nhân bản tin, Gợi ý AI Tag, Quản lý Category/Tag |
| **Lecturer** | `Alexander@FUNewsManagement.org` | `@1` | Bảng `SystemAccount` (AccountID: 1, Role: 2) | Đọc tin tức công khai, Xem bài viết liên quan, Đổi mật khẩu cá nhân |
| **Public** | *(Không cần đăng nhập)* | — | Public | Đọc tin Active, Nhận thông báo Realtime SignalR khi có bài viết mới |

---

## 4. Hướng Dẫn Khởi Chạy Nhanh (Quick Start)

### Bước 1: Khởi tạo Cơ sở dữ liệu Microsoft SQL Server
Chạy file script tổng hợp duy nhất tại thư mục gốc bằng SSMS hoặc CLI:
```powershell
sqlcmd -S localhost -i FUNewsManagement.sql
```
*Script sẽ tự động khởi tạo database `FUNewsManagement`, toàn bộ bảng gốc, các bảng bổ sung A02 (`AuditLog`, `RefreshToken`, `NewsView`, `TagLearning`), cột `NewsImage`, và nạp đầy đủ dữ liệu mẫu (Seed Data).*

### Bước 2: Khởi chạy 4 Solutions (Mở 4 cửa sổ Terminal)

```powershell
# Cửa sổ 1: Core API (Port 7001)
dotnet run --project FUNewsManagement_CoreAPI --urls="https://localhost:7001;http://localhost:5001"

# Cửa sổ 2: Analytics API (Port 7002)
dotnet run --project FUNewsManagement_AnalyticsAPI --urls="https://localhost:7002;http://localhost:5002"

# Cửa sổ 3: AI API (Port 7003)
dotnet run --project FUNewsManagement_AIAPI --urls="https://localhost:7003;http://localhost:5003"

# Cửa sổ 4: Frontend Web App (Port 7000)
dotnet run --project FUNewsManagement_FE --urls="https://localhost:7000;http://localhost:5000"
```

Mở trình duyệt truy cập: **`https://localhost:7000`**

---

## 5. Tổng Quan Các Tính Năng Nổi Bật (Assignment 02)

1. **JWT Authentication & Refresh Token Flow:**
   - Cơ chế cấp Access Token (15 phút) và Refresh Token (7 ngày) với chiến lược **Token Rotation** an toàn.
   - Frontend `DelegatingHandler` tự động chặn mã 401, gọi `POST /api/auth/refresh` lấy token mới và tiếp tục request trong suốt với người dùng.
2. **Audit Logging Tự Động:**
   - EF Core interceptor tự động ghi nhận mọi thay đổi (CREATE, UPDATE, DELETE) đối với Account, Category, Tag, NewsArticle vào bảng `AuditLog`.
   - Lưu trữ chi tiết `BeforeJSON` và `AfterJSON`, cung cấp giao diện Admin xem Side-by-Side Diff trực quan.
3. **Upload Ảnh Bài Viết:**
   - Hỗ trợ tải lên ảnh PNG/JPG với kích thước < 5MB, kiểm tra Magic Bytes chặt chẽ, ngăn chặn mã thực thi, hiển thị ảnh preview trực tiếp trên modal và bài viết.
4. **Analytics API & Dashboard Chart.js:**
   - Thống kê bài viết theo danh mục (Pie Chart) và lượt xem xu hướng (Bar Chart).
   - Xuất dữ liệu thống kê ra file Excel `.xlsx` chất lượng cao thông qua ClosedXML.
   - Gợi ý tối đa 3 bài viết liên quan dựa trên chuyên mục và thẻ tin.
5. **AI Tag Suggestion & Learning Cache:**
   - Web API độc lập phân tích nội dung bài viết, tính điểm tin cậy `confidence` (0.0 - 1.0) và gợi ý các thẻ tin phù hợp.
   - Giao diện Badges/Chips trên Modal cho phép Staff chọn nhanh tag.
   - Feedback Loop: Khi nhân viên chọn tag, `TagLearning` tăng trọng số để ưu tiên gợi ý cho các bài viết tương lai.
6. **Độ Ổn Định & Chịu Lỗi (Polly & Offline Mode):**
   - Tích hợp **Polly Retry Policy** tự động thử lại 3 lần cho các request GET an toàn.
   - **Background Worker (`HostedService`)** định kỳ 6 giờ/lần nạp snapshot dữ liệu vào cache JSON cục bộ.
   - **Offline Mode**: Khi Core API tắt, ứng dụng tự động hiển thị Banner màu vàng, nạp tin từ cache, và khóa an toàn toàn bộ thao tác CRUD.
7. **SignalR Realtime Notification System:**
   - SignalR Hub tại `/hubs/notifications` trên Frontend.
   - Khi Staff tạo bài viết mới, hệ thống tự động bắn thông báo tức thời tới mọi client đang mở.
   - Hiển thị pop-up **Bootstrap Toast** và cập nhật Badge đếm trên **Icon Quả Chuông 🔔** (lưu trữ tối thiểu 10 tin gần nhất).
8. **UI/UX & Trợ Năng (Accessibility - a11y):**
   - 100% form sử dụng Bootstrap Modals; không có reload trang khi nhập liệu.
   - Thay thế toàn bộ popup mặc định của trình duyệt bằng **Bootstrap Confirmation Modal** chuẩn.
   - Global Loading Overlay spinner ngăn double click spam.
   - Hỗ trợ phím tắt `Escape` đóng modal, liên kết `Skip-to-content` và độ tương phản màu chuẩn WCAG.

---

## 6. Thư Mục Ảnh Minh Chứng Nghiệm Thu (Screenshots)

Thư mục `Screenshots/` đính kèm đầy đủ các ảnh chụp màn hình nghiệm thu:
- `01_Swagger_3APIs.png`: Giao diện Swagger UI của 3 APIs độc lập (Core, Analytics, AI).
- `02_Login_Token_Refresh.png`: Giao diện Đăng nhập và nhật ký làm mới Token tự động trong Network Console.
- `03_AI_Tag_Suggestions.png`: Modal tạo bài viết với các thẻ tin AI Chips & độ tin cậy Confidence.
- `04_Admin_Dashboard_Charts_Excel.png`: Dashboard Admin với biểu đồ Pie & Bar Chart.js cùng nút Export Excel.
- `05_Admin_Audit_Logs.png`: Màn hình nhật ký Audit Log và Modal so sánh JSON Before/After.
- `06_Offline_Mode_Banner.png`: Chế độ Ngoại tuyến với Banner cảnh báo và các nút CRUD bị khóa.
- `07_SignalR_Realtime_Notification.png`: Thông báo thời gian thực SignalR Toast và Icon quả chuông 🔔.

---

## 7. Kết Quả Kiểm Thử Tự Động (Test Results)

Hệ thống đạt **100% tỷ lệ vượt qua kiểm thử** trên toàn bộ 24 kịch bản kiểm thử (`T01` – `T24`):

```powershell
# Chạy toàn bộ Backend Tests (154 Tests)
dotnet test tests/FUNews.Tests/FUNews.Tests.csproj
# Kết quả: Passed! - Failed: 0, Passed: 154, Skipped: 0, Total: 154

# Chạy toàn bộ Frontend Tests (135 Tests)
dotnet test tests/FUNews.Client.Tests/FUNews.Client.Tests.csproj
# Kết quả: Passed! - Failed: 0, Passed: 135, Skipped: 0, Total: 135
```
**Tổng cộng: 289 / 289 tests Passed (100%)** — Biên dịch sạch sẽ 0 Error, 0 Warning.
