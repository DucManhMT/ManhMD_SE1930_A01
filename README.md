# FUNewsManagement System

**Môn học:** PRN232 - Kỹ thuật lập trình C# nâng cao  
**Bài tập:** Assignment 01  
**Sinh viên thực hiện:** ManhMD  
**Lớp:** SE1930  
**Nền tảng:** .NET 8.0 (`net8.0`), C# 12  
**Hệ quản trị cơ sở dữ liệu:** Microsoft SQL Server  

---

## 1. Cấu trúc Solution & Kiến trúc hệ thống

Dự án được phân tách thành 2 solution độc lập theo quy tắc 3 tầng (3-tier architecture):

### Backend (`ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln`)
- **Tầng Presentation (API):** `ManhMD_SE1930_A01_BE` (ASP.NET Core Web API, OData, JWT Authentication, Swagger/OpenAPI)
- **Tầng Business Logic:** `FUNews.BusinessLogic` (Services, DTOs, Validation, Business rules)
- **Tầng Data Access:** `FUNews.DataAccess` (Entity Framework Core 8, DbContext Scoped, DAOs, Repositories)
- **Kiểm thử Backend:** `tests/FUNews.Tests` (xUnit unit & architecture tests)

*Nguyên tắc luồng Backend:*  
`Controller` → `Service` → `Repository` → `DAO` → `Scoped DbContext` → `SQL Server`

### Frontend (`ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln`)
- **Tầng Presentation (Web):** `ManhMD_SE1930_A01_FE` (ASP.NET Core MVC, Bootstrap 5, AJAX/Fetch)
- **Tầng Business Logic:** `FUNews.Client.BusinessLogic` (Client Services, mapping kết quả API)
- **Tầng Data Access:** `FUNews.Client.DataAccess` (Typed HttpClient API clients, HTTP transport models)
- **Kiểm thử Frontend:** `tests/FUNews.Client.Tests` (xUnit unit & architecture boundary tests)

*Nguyên tắc luồng Frontend:*  
`Browser Fetch` → `MVC Controller` → `Client Service` → `Typed HttpClient` → `Backend API`  
*(Đảm bảo Frontend hoàn toàn không tham chiếu và không kết nối trực tiếp đến Entity Framework hoặc Database)*

---

## 2. Cấu hình mẫu (Configuration)

### Backend (`appsettings.json` / `appsettings.Example.json`)
```json
{
  "ConnectionStrings": {
    "FUNewsManagement": "Server=localhost;Database=FUNewsManagement;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "DefaultAdmin": {
    "Email": "admin@FUNewsManagementSystem.org",
    "Password": "@@abc123@@"
  },
  "Jwt": {
    "Issuer": "FUNewsApi",
    "Audience": "FUNewsWeb",
    "SigningKey": "FUNewsManagement_PRN232_Secret_Key_For_Jwt_Auth_2026_Secure!"
  },
  "App": {
    "TimeZone": "Asia/Ho_Chi_Minh"
  }
}
```

### Frontend (`appsettings.json` / `appsettings.Example.json`)
```json
{
  "Api": {
    "BaseUrl": "https://localhost:7001"
  },
  "App": {
    "TimeZone": "Asia/Ho_Chi_Minh"
  }
}
```

---

## 3. Hướng dẫn Build và Chạy Kiểm thử

### Build Backend Solution:
```bash
dotnet build ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln
```

### Build Frontend Solution:
```bash
dotnet build ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln
```

### Chạy Unit & Architecture Tests:
```bash
# Chạy test Backend
dotnet test ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln

# Chạy test Frontend
dotnet test ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln
```

---

## 4. Tiến độ Task (Backlog Status)

- **FUN-001 — Khởi tạo hai solution:** **DONE** (Đã khởi tạo đủ 2 solution đúng mẫu .NET 8, đủ các tầng DataAccess, BusinessLogic, Presentation, test projects, cấu hình mẫu và kiểm thử kiến trúc không tham chiếu EF/DB tại FE).
