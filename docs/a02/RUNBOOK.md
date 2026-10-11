# Sổ Tay Vận Hành & Hướng Dẫn Nghiệm Thu (Runbook) — PRN232 A02

Tài liệu hướng dẫn khởi chạy toàn bộ 4 components của hệ thống, kiểm thử và chuẩn bị bộ hồ sơ nộp bài Assignment 02 theo chuẩn Mục 6 Đề bài.

---

## 1. Yêu Cầu Môi Trường (Prerequisites)

- **.NET SDK:** Phiên bản 8.0 trở lên (`dotnet --version` >= 8.0.x).
- **Hệ quản trị CSDL:** Microsoft SQL Server 2019/2022 hoặc LocalDB.
- **IDE:** Visual Studio 2022 / JetBrains Rider / VS Code.
- **Trình duyệt:** Google Chrome / Microsoft Edge.

---

## 2. Chuẩn Bị Cơ Sở Dữ Liệu

1. Khởi tạo Database `FUNewsManagement` từ script gốc của Assignment 01 (nếu chưa có).
2. Chạy file script vá mở rộng cho Assignment 02:
   ```powershell
   sqlcmd -S localhost -d FUNewsManagement -i database/patches/005_assignment02_migration.sql
   ```
   *Script này sẽ tự động tạo các bảng mới: `AuditLog`, `RefreshToken`, `NewsView`, `TagLearning` và thêm cột `NewsImage` vào bảng `NewsArticle`.*

---

## 3. Khởi Chạy 4 Solutions Cục Bộ (Local Running)

Để chạy trọn vẹn hệ thống, mở 4 cửa sổ terminal riêng biệt và chạy lần lượt 4 dự án theo thứ tự:

### Cửa sổ 1: Khởi chạy Core API
```powershell
cd FUNewsManagement_CoreAPI
dotnet run --urls="https://localhost:7001;http://localhost:5001"
```
*Truy cập Swagger Core API: `https://localhost:7001/swagger`*

### Cửa sổ 2: Khởi chạy Analytics API
```powershell
cd FUNewsManagement_AnalyticsAPI
dotnet run --urls="https://localhost:7002;http://localhost:5002"
```
*Truy cập Swagger Analytics API: `https://localhost:7002/swagger`*

### Cửa sổ 3: Khởi chạy AI API
```powershell
cd FUNewsManagement_AIAPI
dotnet run --urls="https://localhost:7003;http://localhost:5003"
```
*Truy cập Swagger AI API: `https://localhost:7003/swagger`*

### Cửa sổ 4: Khởi chạy Frontend Web App
```powershell
cd FUNewsManagement_FE
dotnet run --urls="https://localhost:7000;http://localhost:5000"
```
*Truy cập giao diện Web chính thức: `https://localhost:7000`*

---

## 4. Tài Khoản Kiểm Thử Hệ Thống (Test Accounts)

| Vai trò (Role) | Email | Mật khẩu | Phạm vi quyền hạn |
|:---|:---|:---|:---|
| **Admin** | `admin@FUNewsManagementSystem.org` | `@@abc123@@` | Dashboard Chart.js, Xuất Excel, Quản lý tài khoản, Xem Audit Log |
| **Staff** | `IsabellaDavid@FUNewsManagement.org` | `@1` | CRUD Bài viết, Upload ảnh, Gợi ý AI Tag, Quản lý Category/Tag |
| **Lecturer** | `Alexander@FUNewsManagement.org` | `@1` | Đọc tin tức, Xem bài liên quan, Đổi mật khẩu cá nhân |
| **Public** | *(Không cần đăng nhập)* | — | Đọc tin tức Active, Nhận thông báo bài mới |

---

## 5. Quy Trình Chuẩn Bị Bộ Nộp Bài (Submission Checklist)

Theo Mục 6 Đề bài, gói nộp bài phải bao gồm đầy đủ:
- [ ] `FUNewsManagement_CoreAPI.sln`
- [ ] `FUNewsManagement_AnalyticsAPI.sln`
- [ ] `FUNewsManagement_AIAPI.sln`
- [ ] `FUNewsManagement_FE.sln`
- [ ] `FUNewsManagement.sql`: Script CSDL hoàn chỉnh chứa toàn bộ bảng, dữ liệu mẫu và các bản vá A02.
- [ ] `README.md`: Hướng dẫn cài đặt, danh sách URLs các API, tài khoản kiểm thử.
- [ ] Thư mục `Screenshots/`: Ảnh chụp kết quả API responses, AI Tag Suggestion, Dashboard Chart.js, Audit Log, Offline Mode và SignalR Toast.
