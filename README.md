# FUNewsManagement System — Hướng Dẫn Vận Hành & Nghiệm Thu

**Môn học:** PRN232 - Advanced C# Programming (.NET 8.0)  
**Bài tập:** Assignment 01  
**Sinh viên thực hiện:** ManhMD  
**Lớp:** SE1930  
**Nền tảng:** .NET 8.0 SDK (`net8.0`), C# 12, Microsoft SQL Server  

---

## 1. Kiến Trúc Hệ Thống & Cấu Trúc Hai Solution

Hệ thống được thiết kế theo đúng kiến trúc 3 tầng (3-tier Architecture) với 2 solution hoàn toàn độc lập:

```
├── ManhMD_SE1930_A01_BE/                 # Solution Backend (Web API)
│   ├── ManhMD_SE1930_A01_BE.sln
│   ├── Controllers/                      # Presentation Layer (REST & OData Endpoints)
│   ├── FUNews.BusinessLogic/             # Business Logic Layer (Services, DTOs, Security, Validators)
│   └── FUNews.DataAccess/                # Data Access Layer (EF Core, Scoped DbContext, DAOs, Repositories)
│
├── ManhMD_SE1930_A01_FE/                 # Solution Frontend (Razor Pages)
│   ├── ManhMD_SE1930_A01_FE.sln
│   ├── Pages/                            # Presentation Layer (Razor Pages UI, Modals, Bootstrap 5)
│   ├── FUNews.Client.BusinessLogic/      # Client BLL (Client Services, ViewModel formatting)
│   └── FUNews.Client.DataAccess/         # Client DAL (Typed HttpClient, Request/Response ApiModels)
│
├── database/                             # Database Patches & Migrations
│   └── patches/                          # Script vá lỗi dữ liệu gốc và thiết lập Sequences
├── docs/                                 # Tài liệu kỹ thuật, Test Plan & Verification Evidence
└── tests/                                # Test Projects
    ├── FUNews.Tests/                     # 122 Tests Tích Hợp Backend (xUnit)
    └── FUNews.Client.Tests/              # 100 Tests Đơn Vị Frontend (xUnit)
```

> [!IMPORTANT]
> **Quy tắc cô lập Frontend:**  
> Solution Frontend (`ManhMD_SE1930_A01_FE`) **hoàn toàn không tham chiếu** `FUNews.DataAccess`, `Microsoft.EntityFrameworkCore` hay kết nối trực tiếp đến Database. Mọi giao tiếp dữ liệu đều thông qua `Typed HttpClient` đến REST API của Backend.

---

## 2. Tài Khoản Demo Hệ Thống

| Vai trò (Role) | Email | Mật khẩu | Nguồn dữ liệu | Quyền hạn trong hệ thống |
|:---|:---|:---|:---|:---|
| **Admin** | `admin@FUNewsManagementSystem.org` | `@@abc123@@` | `appsettings.json` (Không có AccountID trong DB) | Quản lý tài khoản (CRUD/Search), Phân quyền, Báo cáo thống kê & Audit người sửa cuối. |
| **Staff** | `IsabellaDavid@FUNewsManagement.org` | `@1` | Bảng `SystemAccount` (AccountID: 3, Role: 1) | Quản lý bài viết (CRUD, Duplicate, Gán thẻ), Quản lý Chuyên mục, Quản lý Thẻ tin, Xem lịch sử tin mình tạo, Cập nhật hồ sơ cá nhân. |
| **Lecturer** | `Alexander@FUNewsManagement.org` | `@1` | Bảng `SystemAccount` (AccountID: 4, Role: 2) | Đăng nhập hệ thống, Đọc tin tức công khai, Xem tin theo chuyên mục/thẻ, Tìm kiếm tin tức. *(Bị chặn tuyệt đối các API ghi/quản trị)* |
| **Anonymous** | *(Khách vãng lai)* | *(Không cần)* | Public | Đọc tin bài Active, Tìm kiếm tin tức, Xem bài viết liên quan. |

---

## 3. Tổng Quan Danh Mục API (Backend API Overview)

| Endpoint | Method | Phân quyền (Role) | Mô tả chi tiết |
|:---|:---:|:---:|:---|
| `/api/auth/login` | `POST` | Public | Đăng nhập hệ thống (hỗ trợ Admin config, Staff DB, Lecturer DB), trả về JWT Token và UserInfo. |
| `/api/account` | `GET` | `Admin` | Truy vấn danh sách tài khoản hỗ trợ OData (`$filter`, `$orderby`, `$top`, `$skip`). DTO không lộ mật khẩu. |
| `/api/account` | `POST` | `Admin` | Tạo tài khoản mới (tự động hash mật khẩu BCrypt, sinh AccountID an toàn qua SQL Sequence). |
| `/api/account/{id}` | `PUT` | `Admin` | Cập nhật thông tin tài khoản (kiểm tra unique email, không cho phép đổi password tại đây). |
| `/api/account/{id}` | `DELETE` | `Admin` | Xóa tài khoản (chặn nếu tài khoản đã từng tạo bài viết). |
| `/api/profile` | `GET` | `Staff, Lecturer` | Lấy thông tin hồ sơ của tài khoản đang đăng nhập. |
| `/api/profile` | `PUT` | `Staff, Lecturer` | Cập nhật tên của tài khoản đang đăng nhập (chặn đổi Role/Email). |
| `/api/profile/change-password` | `POST` | `Staff, Lecturer` | Đổi mật khẩu (xác minh mật khẩu hiện tại trước khi hash lưu mật khẩu mới). |
| `/api/category` | `GET` | Public / All | Lấy danh sách chuyên mục (hỗ trợ OData). |
| `/api/category` | `POST` | `Staff` | Thêm chuyên mục mới (kiểm tra trùng tên cùng chuyên mục cha). |
| `/api/category/{id}` | `PUT` | `Staff` | Cập nhật chuyên mục (chặn đổi cha nếu đã có bài viết, chặn chu trình vòng). |
| `/api/category/{id}` | `DELETE` | `Staff` | Xóa chuyên mục (chặn nếu chuyên mục đang chứa bài viết hoặc có chuyên mục con). |
| `/api/tag` | `GET` | Public / All | Lấy danh sách thẻ tin (hỗ trợ OData). |
| `/api/tag` | `POST` | `Staff` | Thêm thẻ tin mới (tên không trùng lặp, sinh TagID qua SQL Sequence). |
| `/api/tag/{id}` | `PUT` | `Staff` | Cập nhật ghi chú (Note) của thẻ tin. |
| `/api/tag/{id}` | `DELETE` | `Staff` | Xóa thẻ tin (chặn nếu thẻ đang được gắn vào bài viết). |
| `/api/news` | `GET` | Public / All | Lấy danh sách tin tức (khách chỉ nhận bài Active; Staff/Admin xem được cả Active và Inactive). |
| `/api/news/{id}` | `GET` | Public / All | Xem chi tiết bài viết (khách truy cập bài Inactive trả về `404 Not Found`). |
| `/api/news` | `POST` | `Staff` | Tạo bài viết mới kèm danh sách TagID (server gán CreatedByID và CreatedDate). |
| `/api/news/{id}` | `PUT` | `Staff` | Cập nhật bài viết và bộ thẻ tin (server gán UpdatedByID và ModifiedDate). |
| `/api/news/{id}` | `DELETE` | `Staff` | Xóa bài viết (tự động xóa liên kết trong `NewsTag`, bảo toàn danh mục `Tag`). |
| `/api/news/{id}/duplicate` | `POST` | `Staff` | Nhân bản bài viết (sinh ID mới, trạng thái mặc định Inactive, copy danh sách tag). |
| `/api/news/my-articles` | `GET` | `Staff` | Lấy danh sách bài viết do chính Staff đang đăng nhập khởi tạo. |
| `/api/news/{id}/related` | `GET` | Public / All | Lấy tối đa 3 bài liên quan cùng Category hoặc Tag (chỉ bài Active, loại bài gốc). |
| `/api/report` | `GET` | `Admin` | Báo cáo thống kê theo khoảng ngày (`startDate <= endDate`), nhóm theo `category`, `author`, hoặc `status`. |

---

## 4. Hướng Dẫn Cài Đặt & Chạy Ứng Dụng Từ Đầu

### Bước 1: Chuẩn bị Cơ sở Dữ liệu SQL Server

1. Sử dụng SQL Server Management Studio (SSMS) hoặc `sqlcmd` để tạo database `FUNewsManagement` từ script gốc được cung cấp.
2. Chạy các bản vá an toàn trong thư mục `database/patches/` theo đúng thứ tự:
   - `001_preflight_check.sql`: Kiểm tra dữ liệu bất thường.
   - `002_apply_patches.sql`: Sửa lỗi parent seed tự trỏ chính mình, loại bỏ khoảng trắng thừa, mở rộng cột password lên `nvarchar(512)`, đổi CASCADE DELETE sang NO ACTION.
   - `003_create_sequences.sql`: Khởi tạo SQL Sequences cho `AccountID`, `TagID`, `NewsArticleID`.
   - `004_verify_patches.sql`: Xác minh tính toàn vẹn của database sau khi vá.

### Bước 2: Cấu hình Kết nối

- **Backend (`ManhMD_SE1930_A01_BE/appsettings.json`):**
  Cập nhật chuỗi kết nối SQL Server của bạn tại:
  ```json
  "ConnectionStrings": {
    "FUNewsManagement": "Server=localhost;Database=FUNewsManagement;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
  ```
- **Frontend (`ManhMD_SE1930_A01_FE/appsettings.json`):**
  Trỏ `BaseUrl` về cổng chạy của Backend API (mặc định: `https://localhost:7001`).

### Bước 3: Khởi động Ứng dụng

Mở 2 cửa sổ terminal riêng biệt:

- **Khởi động Backend API:**
  ```powershell
  dotnet run --project ManhMD_SE1930_A01_BE
  ```
  *API lắng nghe tại: `https://localhost:7001` (Swagger UI: `https://localhost:7001/swagger`)*

- **Khởi động Frontend Web App:**
  ```powershell
  dotnet run --project ManhMD_SE1930_A01_FE
  ```
  *Web App chạy tại: `https://localhost:7002`*

---

## 5. Kiểm Thử Hệ Thống (Testing & Verification)

Dự án sở hữu bộ test tự động toàn diện với **222 test cases** bao phủ 100% logic nghiệp vụ, phân quyền và tích hợp:

```powershell
# Chạy toàn bộ 122 bài test Backend (Tích hợp API, Database, EF Core, Security, OData, Transaction)
dotnet test tests/FUNews.Tests/FUNews.Tests.csproj

# Chạy toàn bộ 100 bài test Frontend (Kiến trúc, Razor PageModel, Validation, Client Services)
dotnet test tests/FUNews.Client.Tests/FUNews.Client.Tests.csproj
```

**Kết quả kiểm thử:**
- **Backend:** 122 Passed, 0 Failed, 0 Skipped (100% Pass)
- **Frontend:** 100 Passed, 0 Failed, 0 Skipped (100% Pass)
- **Biên bản đối chiếu chi tiết Test Plan T01–T29 và R01–R19:** Xem tại [docs/evidence/FUNews_Verification_Evidence.md](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/docs/evidence/FUNews_Verification_Evidence.md).

---

## 6. Giả Định & Giới Hạn Hệ Thống (Assumptions & Limitations)

1. **Phân quyền Admin:** Admin hệ thống được cấu hình trong `appsettings.json` và không có bản ghi trong bảng `SystemAccount`. Khi tạo mới bài viết hoặc thao tác nghiệp vụ, Admin không được mạo danh gán ID số 0 hay ID âm vào các khoá ngoại FK.
2. **Quyền hạn Giảng viên (Lecturer):** Giảng viên trong hệ thống chỉ có quyền đăng nhập và đọc tin tức tương tự như khách, không được cấp quyền thêm/sửa/xóa bài viết hay chuyên mục.
3. **Phạm vi Ngoài đề (Out of Scope):** Không triển khai các tính năng ngoài yêu cầu như: Đăng ký tài khoản công khai, Quên mật khẩu qua Email, Refresh token, Upload hình ảnh vật lý, Hệ thống duyệt bài đa cấp (Workflow approval), Bình luận bài viết.
4. **Tính năng Xuất Excel (FUN-022):** Là tính năng tùy chọn (Optional), không làm ảnh hưởng đến điều kiện nghiệm thu cốt lõi.
