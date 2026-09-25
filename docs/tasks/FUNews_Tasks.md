# FUNews task backlog

Mọi task bắt đầu TODO; chưa có code được xác minh. Thứ tự ID là lộ trình đề xuất, dependencies là điều kiện tối thiểu. Làm một task mỗi lượt. FUN-022 tùy chọn, không tự làm trước nghiệm thu.

## Quy tắc card

Actor/input/route đọc thêm Database_API_Contract. Mọi card CRUD gồm server/client validation, quyền API, modal AJAX, loading/empty/error/success và kiểm thử phù hợp. DONE cần evidence; nếu code xong chưa chạy SQL/UI, dùng CODE_COMPLETE_UNVERIFIED. Mỗi card có một vùng ghi bằng chứng, không đánh dấu hoàn thành theo phỏng đoán.

## Chỉ mục

| Task | Nội dung | Dependencies | Trạng thái |
|---|---|---|---|
| FUN-001 | Khởi tạo hai solution | Không | DONE |
| FUN-002 | Database và tầng truy cập | 001 | DONE |
| FUN-003 | HTTP contract và OData nền tảng | 002 | DONE |
| FUN-004 | Đăng nhập và shell theo role | 003 | DONE |
| FUN-005 | Danh sách và thêm tài khoản | 004 | DONE |
| FUN-006 | Sửa và xóa tài khoản | 005 | DONE |
| FUN-007 | Hồ sơ và đổi mật khẩu | 004 | DONE |
| FUN-008 | Danh sách và thêm danh mục | 004 | DONE |
| FUN-009 | Sửa trạng thái và xóa danh mục | 008 | DONE |
| FUN-010 | Quản lý tag | 004 | DONE |
| FUN-011 | Danh sách quản lý bài viết | 008,010 | DONE |
| FUN-012 | Tạo bài và gắn nhiều tags | 011 | DONE |
| FUN-013 | Sửa và xóa bài viết | 012 | DONE |
| FUN-014 | Nhân bản bài viết | 013 | TODO |
| FUN-015 | Lịch sử bài do mình tạo | 013 | TODO |
| FUN-016 | Trang tin công khai và chi tiết | 013 | TODO |
| FUN-017 | Tìm kiếm nâng cao | 016 | TODO |
| FUN-018 | Tin liên quan | 016 | TODO |
| FUN-019 | Báo cáo và audit cuối | 013 | TODO |
| FUN-020 | Kiểm thử tích hợp và UI | 006,007,009,014,015,017,018,019 | TODO |
| FUN-021 | README và bộ nộp | 020 | TODO |
| FUN-022 | Export Excel tùy chọn | 019,021 | TODO |

## FUN-001 — Khởi tạo hai solution

- Trạng thái: DONE
- Dependencies: Không
- Actor: Developer
- Điểm vào/phạm vi file: Cấu trúc backend/frontend trong Architecture
- Mục tiêu: Tạo project references, cấu hình mẫu, test projects và README chạy sơ bộ.

### Acceptance criteria

1. Hai file .sln đúng mẫu, net8.0.
2. Build cả BE và FE thành công.
3. FE không reference EF/DB.
4. Chưa tạo CRUD ngoài task.

### Bàn giao

- File thay đổi:
  - Backend: `ManhMD_SE1930_A01_BE.sln`, `ManhMD_SE1930_A01_BE.csproj`, `FUNews.DataAccess/FUNews.DataAccess.csproj`, `FUNews.BusinessLogic/FUNews.BusinessLogic.csproj`, `appsettings.json`, `appsettings.Example.json`
  - Frontend: `ManhMD_SE1930_A01_FE.sln`, `ManhMD_SE1930_A01_FE.csproj`, `FUNews.Client.DataAccess/FUNews.Client.DataAccess.csproj`, `FUNews.Client.BusinessLogic/FUNews.Client.BusinessLogic.csproj`, `Controllers/HomeController.cs`, `Views/`, `wwwroot/css/site.css`, `appsettings.json`, `appsettings.Example.json`
  - Tests: `tests/FUNews.Tests/` (unit & architecture tests BE), `tests/FUNews.Client.Tests/` (architecture boundary tests FE)
  - Root: `README.md`, `database/patches/`, `docs/reference/`
- Lệnh và kết quả build/test:
  - `dotnet build ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln` -> Build succeeded (0 Warning, 0 Error)
  - `dotnet build ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln` -> Build succeeded (0 Warning, 0 Error)
  - `dotnet test ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln` -> Passed! (Passed: 2, Failed: 0)
  - `dotnet test ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln` -> Passed! (Passed: 2, Failed: 0; Frontend_ShouldNotReference_EntityFrameworkOrDatabase PASS)
- UI/SQL/API evidence: Đã kiểm tra build và unit tests chạy thành công. FE MVC cấu hình Bootstrap 5 và CSS token theo Design Standard. Chưa can thiệp SQL hay CRUD nghiệp vụ ngoài task.
- Blocker/giả định phát sinh: Không có.

## FUN-002 — Database và tầng truy cập

- Trạng thái: DONE
- Dependencies: 001
- Actor: Developer
- Điểm vào/phạm vi file: SQL gốc; database/patches; DataAccess BE
- Mục tiêu: Import dev DB, preflight patch, scaffold, mapping cột sai chính tả; repository/DAO từng entity và ID generators.

### Acceptance criteria

1. Giữ SQL gốc.
2. Không cascade xóa account/category.
3. Parent seed hợp lệ.
4. Unique constraints.
5. Sequence không trùng.
6. Seed hash an toàn.
7. DbContext Scoped và config Singleton.
8. Đọc được dữ liệu thật.

### Bàn giao

- File thay đổi:
  - Database patches: `database/patches/001_preflight_check.sql`, `database/patches/002_apply_patches.sql`, `database/patches/003_create_sequences.sql`, `database/patches/004_verify_patches.sql`
  - Backend DataAccess:
    - Entities: `Category.cs` (map `CategoryDesciption`), `SystemAccount.cs`, `Tag.cs`, `NewsArticle.cs`, `NewsTag.cs`
    - Context: `FUNewsDbContext.cs`
    - Sequences: `ISqlSequenceService.cs`, `SqlSequenceService.cs`
    - Security & Seeding: `IPasswordHasher.cs`, `BcryptPasswordHasher.cs`, `IPasswordSeeder.cs`, `PasswordSeeder.cs`
    - DAOs: `CategoryDAO.cs`, `SystemAccountDAO.cs`, `TagDAO.cs`, `NewsArticleDAO.cs`, `NewsTagDAO.cs`
    - Repositories: `ICategoryRepository.cs` / `CategoryRepository.cs`, `ISystemAccountRepository.cs` / `SystemAccountRepository.cs`, `ITagRepository.cs` / `TagRepository.cs`, `INewsArticleRepository.cs` / `NewsArticleRepository.cs`, `INewsTagRepository.cs` / `NewsTagRepository.cs`
    - Extensions: `DataAccessServiceCollectionExtensions.cs`
  - Backend BusinessLogic:
    - Options: `DefaultAdminOptions.cs`, `JwtOptions.cs`, `AppOptions.cs`
    - Extensions: `BusinessLogicServiceCollectionExtensions.cs`
  - Backend API:
    - `Program.cs` cấu hình DbContext Scoped, DAOs/Repositories Scoped, Config Options Singleton, và PasswordSeeder lúc khởi động.
  - Tests:
    - `tests/FUNews.Tests/DatabaseAndDataAccessTests.cs` (11 tests pass toàn bộ 8 tiêu chí).
- Lệnh và kết quả build/test:
  - `sqlcmd -S "localhost" -d "FUNewsManagement" -I -E -i "database\patches\001_preflight_check.sql"` -> Pass (0 warning, 0 duplicate, phát hiện 5 dòng self-reference và 2 CASCADE FK)
  - `sqlcmd -S "localhost" -d "FUNewsManagement" -I -E -i "database\patches\002_apply_patches.sql"` -> Pass (Updated parent IDs to NULL, trimmed strings, expanded AccountPassword, FK NO ACTION, 4 filtered unique indexes)
  - `sqlcmd -S "localhost" -d "FUNewsManagement" -I -E -i "database\patches\003_create_sequences.sql"` -> Pass (Created Seq_AccountID, Seq_TagID, Seq_NewsArticleID)
  - `sqlcmd -S "localhost" -d "FUNewsManagement" -I -E -i "database\patches\004_verify_patches.sql"` -> Pass (Tất cả ràng buộc, sequence, index và data type đã xác minh)
  - `dotnet build ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln` -> Build succeeded (0 Warning, 0 Error)
  - `dotnet test tests/FUNews.Tests/FUNews.Tests.csproj` -> Passed! (Passed: 11, Failed: 0, Skipped: 0)
  - `dotnet test ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln` -> Passed! (Passed: 2, Failed: 0)
- UI/SQL/API evidence:
  - SQL Server `localhost` database `FUNewsManagement` đã áp dụng đầy đủ 4 patch.
  - Các tests kiểm tra trực tiếp trên DB thật `FUNewsManagement`: đọc được Category (5 rows), NewsArticle (5 rows), Tag (9 rows), SystemAccount (5 rows), NewsTag (18 rows).
  - PasswordSeeder đã hash an toàn mật khẩu demo `@1` sang BCrypt format (`$2a$`), xác nhận không re-hash khi chạy lại.
  - Kiểm tra duplicate email trên DB thật ném `DbUpdateException`.
  - Sequences hoạt động đúng quy tắc và không trùng lặp.
- Blocker/giả định phát sinh: Không có.

## FUN-003 — HTTP contract và OData nền tảng

- Trạng thái: DONE
- Dependencies: 002
- Actor: Developer
- Điểm vào/phạm vi file: API middleware/DTO/query; FE typed API clients
- Mục tiêu: Error contract, pagination, OData allowlist, route conventions, API client error mapping.

### Acceptance criteria

1. JSON DTO không password.
2. OData filter/orderby/top/skip/count hoạt động trên query thử.
3. Query invalid bị chặn.
4. FE đọc được API thật.
5. Không controller nào gọi DbContext.

### Bàn giao

- File thay đổi:
  - Backend DataAccess:
    - `DAOs/CategoryDAO.cs`, `DAOs/TagDAO.cs`, `DAOs/SystemAccountDAO.cs`: Bổ sung `GetQueryable()`.
    - `Repositories/ICategoryRepository.cs` & `CategoryRepository.cs`, `Repositories/ITagRepository.cs` & `TagRepository.cs`, `Repositories/ISystemAccountRepository.cs` & `SystemAccountRepository.cs`: Bổ sung `GetQueryable()`.
  - Backend BusinessLogic:
    - Exceptions: `NotFoundException.cs`, `ValidationException.cs`, `ConflictException.cs`, `ForbiddenException.cs`, `UnauthorizedException.cs`.
    - DTOs: `CategoryDto.cs`, `TagDto.cs`, `NewsArticleDto.cs`, `NewsArticleListDto.cs`, `AccountDto.cs` (an toàn, không chứa trường password hay hash), `ODataResponse.cs` (envelope `@odata.count` và `value`), `ApiErrorResponse.cs`.
    - Services: `ICategoryService.cs` / `CategoryService.cs`, `ITagService.cs` / `TagService.cs`, `INewsArticleService.cs` / `NewsArticleService.cs`, `IAccountService.cs` / `AccountService.cs`.
    - Extensions: `BusinessLogicServiceCollectionExtensions.cs` đăng ký các Scoped application services.
  - Backend API:
    - Middleware: `Middleware/ExceptionHandlingMiddleware.cs` chuẩn hóa RFC 7807 ProblemDetails / ValidationProblemDetails, correlation traceId, ẩn SQL/stack trace khi gặp lỗi bất ngờ.
    - OData: `OData/ODataModelBuilder.cs` (EDM model với lowerCamelCase), `OData/ODataQueryHelper.cs` (thực thi OData validation MaxTop 100, chặn expand, phân trang và tính `@odata.count`).
    - Controllers: `CategoryController.cs`, `TagController.cs`, `NewsController.cs`, `AccountController.cs` (kế thừa ControllerBase, không gọi DbContext, chỉ inject Scoped Services).
    - `Program.cs`: Cấu hình OData, JSON camelCase, EDM model middleware, ExceptionHandlingMiddleware, và `public partial class Program { }`.
  - Frontend:
    - DataAccess: `FUNews.Client.DataAccess.csproj` (bổ sung Microsoft.Extensions.Http 8.0.1), `Models/ODataEnvelope.cs`, `Models/CategoryApiModel.cs`, `Models/TagApiModel.cs`, `Models/NewsArticleApiModel.cs`, `Models/AccountApiModel.cs`, `Models/ApiProblemDetails.cs`, `Exceptions/FUNewsApiException.cs`, `Clients/IFUNewsApiClient.cs` & `Clients/FUNewsApiClient.cs`, `Extensions/ClientDataAccessServiceCollectionExtensions.cs`.
    - BusinessLogic: `FUNews.Client.BusinessLogic.csproj` (Microsoft.Extensions.DependencyInjection.Abstractions 8.0.2), `Services/ICategoryClientService.cs` & `CategoryClientService.cs`, `Services/INewsClientService.cs` & `NewsClientService.cs`, `Services/ITagClientService.cs` & `TagClientService.cs`, `Services/IAccountClientService.cs` & `AccountClientService.cs`, `Extensions/ClientBusinessLogicServiceCollectionExtensions.cs`.
    - Presentation: `Program.cs` đăng ký Client DataAccess & BusinessLogic; `Pages/Index.cshtml.cs` và `Pages/Index.cshtml` kết nối đọc API thật và hiển thị chuyên mục nổi bật.
  - Tests:
    - `tests/FUNews.Tests/FUNews.Tests.csproj` (bổ sung Microsoft.AspNetCore.Mvc.Testing 8.0.11 và reference sang Client.DataAccess).
    - `tests/FUNews.Tests/HttpContractAndODataTests.cs`: 11 tests kiểm thử toàn diện toàn bộ 5 acceptance criteria (kiểm tra DTO/JSON không password, OData filter/orderby/top/skip/count, chặn expand/invalid properties/password filter/top 100+, typed API client đọc API thật, và kiến trúc Controller không gọi DbContext).
- Lệnh và kết quả build/test:
  - `dotnet build ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln` -> Build succeeded (0 Warning, 0 Error).
  - `dotnet build ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln` -> Build succeeded (0 Warning, 0 Error).
  - `dotnet test tests/FUNews.Tests/FUNews.Tests.csproj` -> Passed! (Passed: 26, Failed: 0, Skipped: 0).
  - `dotnet test tests/FUNews.Client.Tests/FUNews.Client.Tests.csproj` -> Passed! (Passed: 2, Failed: 0, Skipped: 0).
- UI/SQL/API evidence:
  - Backend API OData endpoints `/api/category`, `/api/tag`, `/api/news`, `/api/account` hoạt động chuẩn RFC 7807 ProblemDetails và envelope OData `{"@odata.count": ..., "value": [...]}`.
  - Account API và Account DTO hoàn toàn không lộ password/hash, truy vấn `$filter` trên mật khẩu bị chặn với 400 Bad Request.
  - `$expand` bị chặn chặt chẽ với 400 Bad Request.
  - Frontend Typed API Client kết nối thành công tới Backend API TestServer và đọc dữ liệu thực từ SQL Server database.
  - Kiểm tra kiến trúc: 100% Controllers tuân thủ nghiêm ngặt, không chứa tham chiếu tới `DbContext` hay `FUNewsDbContext`.
- Blocker/giả định phát sinh: Không có.

## FUN-004 — Đăng nhập và shell theo role

- Trạng thái: DONE
- Dependencies: 003
- Actor: Admin/Staff/Lecturer
- Điểm vào/phạm vi file: /login; /api/auth/login
- Mục tiêu: Login/logout, JWT BE, cookie/session FE, role policies và layout navigation.

### Acceptance criteria

1. Ba role login đúng.
2. Sai password có lỗi.
3. Token server-side.
4. Logout hủy session.
5. Antiforgery.
6. Lecturer không được gọi API ghi.
7. Admin không được gán AccountID giả.

### Bàn giao

- File thay đổi:
  - Backend:
    - BusinessLogic: `DTOs/LoginRequestDto.cs` (namespace `Models`), `DTOs/UserInfoDto.cs`, `DTOs/LoginResponseDto.cs`, `Security/IJwtTokenService.cs` & `Security/JwtTokenService.cs`, `Services/IAuthService.cs` & `Services/AuthService.cs`, `Extensions/BusinessLogicServiceCollectionExtensions.cs`, `FUNews.BusinessLogic.csproj` (System.IdentityModel.Tokens.Jwt).
    - API: `Controllers/AuthController.cs`, `Controllers/AccountController.cs` (`[Authorize(Roles = "Admin")]`), `Controllers/CategoryController.cs`, `Controllers/TagController.cs`, `Controllers/NewsController.cs` (phân quyền ghi `[Authorize(Roles = "Staff")]`), `Middleware/ExceptionHandlingMiddleware.cs` (UnsafeRelaxedJsonEscaping), `Program.cs` (JWT Bearer auth, authorization policies, Swagger security definition).
  - Frontend:
    - DataAccess: `Models/LoginRequestApiModel.cs`, `Models/UserInfoApiModel.cs`, `Models/LoginResponseApiModel.cs`, `Clients/ITokenProvider.cs`, `Clients/AuthHeaderHandler.cs`, `Clients/IFUNewsApiClient.cs` & `Clients/FUNewsApiClient.cs` (phương thức `LoginAsync`), `Extensions/ClientDataAccessServiceCollectionExtensions.cs`.
    - BusinessLogic: `Services/IAuthClientService.cs` & `Services/AuthClientService.cs`, `Extensions/ClientBusinessLogicServiceCollectionExtensions.cs`.
    - Presentation: `Program.cs` (Cookie Auth, Session, Antiforgery, Policies), `Services/HttpContextTokenProvider.cs`, `Pages/Login.cshtml` & `Pages/Login.cshtml.cs`, `Pages/Logout.cshtml` & `Pages/Logout.cshtml.cs`, `Pages/AccessDenied.cshtml` & `Pages/AccessDenied.cshtml.cs`, `Pages/Shared/_Layout.cshtml` (Role-based navigation shell cho Anonymous, Lecturer, Staff, Admin), `Pages/Shared/_ValidationScriptsPartial.cshtml`.
  - Tests:
    - Backend: `tests/FUNews.Tests/AuthenticationAndRoleTests.cs` (kiểm thử 8/8 tiêu chí xác thực, JWT, phân quyền role, và chặn Lecturer ghi dữ liệu), `tests/FUNews.Tests/HttpContractAndODataTests.cs` (cập nhật token xác thực admin cho regression check).
    - Frontend: `tests/FUNews.Client.Tests/ClientAuthenticationTests.cs` (kiểm tra gắn Bearer token qua DelegatingHandler, validation model), `tests/FUNews.Client.Tests/LoginRazorPageTests.cs` (kiểm tra luồng đăng nhập PageModel, session, và claims không sinh ID giả cho Admin).
- Lệnh và kết quả build/test:
  - `dotnet build ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln` -> Build succeeded (0 Warning, 0 Error).
  - `dotnet build ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln` -> Build succeeded (0 Warning, 0 Error).
  - `dotnet test ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln` -> Passed! (Passed: 39, Failed: 0, Skipped: 0).
  - `dotnet test ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln` -> Passed! (Passed: 10, Failed: 0, Skipped: 0).
- UI/SQL/API evidence:
  - Cả 3 vai trò (Admin config, Staff DB, Lecturer DB) đăng nhập thành công với JWT token ký cryptographic HMAC-SHA256, đầy đủ claim vai trò và định danh an toàn.
  - Sai tài khoản hoặc mật khẩu trả về 401 Unauthorized với RFC 7807 ProblemDetails ("Email hoặc mật khẩu không chính xác.").
  - Admin tuyệt đối không có AccountID trong JWT token hay DTO response (`AccountId = null`), không gán ID giả 0 hay số âm.
  - Phân quyền API: Token Giảng viên (Lecturer) gọi các endpoint ghi (POST/PUT/DELETE Category, Tag, News, Account) bị chặn với 403 Forbidden. Token Nhân viên (Staff) được ủy quyền gọi ghi Category/Tag/News. Token Admin truy cập được endpoint Account.
  - FE Shell điều hướng thay đổi linh hoạt theo vai trò: Admin thấy Quản lý tài khoản / Báo cáo; Staff thấy Tin tức / Quản lý bài viết / Chuyên mục / Thẻ tin / Tin của tôi / Hồ sơ; Lecturer dùng public shell với badge Giảng viên; Chưa đăng nhập hiển thị nút Đăng nhập / Tìm kiếm.
  - Form Đăng nhập và Đăng xuất bảo vệ bằng Antiforgery token, hỗ trợ client validation, tự động xóa mật khẩu khi gặp lỗi.
- Blocker/giả định phát sinh: Không có.

## FUN-005 — Danh sách và thêm tài khoản

- Trạng thái: DONE
- Dependencies: 004
- Actor: Admin
- Điểm vào/phạm vi file: /admin/accounts; /api/account
- Mục tiêu: List/search name/email/role; role filter; modal tạo với name/email/role/password.

### Acceptance criteria

1. Email trùng bị chặn client/server.
2. Hash lưu DB.
3. List không hash.
4. Tạo thành công cập nhật AJAX.
5. Role ngoài 1/2 bị từ chối.
6. Role khác không truy cập API.

### Bàn giao

- File thay đổi:
  - BE DTOs & Logic: [CreateAccountRequestDto.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Models/CreateAccountRequestDto.cs), [IAccountService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/IAccountService.cs), [AccountService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/AccountService.cs), [AccountController.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/Controllers/AccountController.cs), [SqlSequenceService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.DataAccess/Sequences/SqlSequenceService.cs).
  - FE DataAccess & Logic: [CreateAccountApiModel.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Models/CreateAccountApiModel.cs), [IFUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/IFUNewsApiClient.cs), [FUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/FUNewsApiClient.cs), [IAccountClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/IAccountClientService.cs), [AccountClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/AccountClientService.cs).
  - FE Presentation (Razor Page): [Accounts.cshtml](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Admin/Accounts.cshtml), [Accounts.cshtml.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Admin/Accounts.cshtml.cs).
  - Tests: [AccountManagementTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Tests/AccountManagementTests.cs), [AccountsRazorPageTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Client.Tests/AccountsRazorPageTests.cs).
- Lệnh và kết quả build/test:
  - `dotnet build ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln`: Succeeded (0 Warnings, 0 Errors).
  - `dotnet build ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln`: Succeeded (0 Warnings, 0 Errors).
  - `dotnet test tests/FUNews.Tests/FUNews.Tests.csproj`: 47/47 Passed (100%) — gồm 1 regression test M1.
  - `dotnet test tests/FUNews.Client.Tests/FUNews.Client.Tests.csproj`: 26/26 Passed (100%) — gồm 5 regression tests M2.
- UI/SQL/API evidence:
  - Kiểm tra 6/6 acceptance criteria qua test tự động và kiểm định luồng dữ liệu:
    1. Email trùng bị chặn ở client-side (blur gọi server `/admin/accounts?handler=CheckEmail` cover toàn bộ tài khoản, không chỉ ≤100 row trên bảng) và server-side (400 ProblemDetails với field error `AccountEmail`); duy trì index `UQ_SystemAccount_AccountEmail` chống race condition.
    2. Password được hash bằng BCrypt qua `IPasswordHasher` trước khi lưu vào DB SQL Server (xác thực trực tiếp bằng `VerifyPassword`).
    3. List và DTO không chứa trường password hay hash, DTO reflection test xác nhận toàn bộ DTO không chứa token nhạy cảm.
    4. Modal AJAX thực hiện POST kèm antiforgery header `X-CSRF-TOKEN`, khi tạo thành công đóng modal, chèn dòng mới vào bảng bằng DOM injection, cập nhật badge số lượng và hiển thị Toast thông báo mà không reload toàn bộ trang; khi có lỗi giữ nguyên input người dùng và highlight trường lỗi.
    5. Role ngoài 1 (Staff) và 2 (Lecturer) (0, 3, -1, null) bị từ chối với 400 Bad Request cả tầng DTO DataAnnotations lẫn Service Validation.
    6. Endpoint API `/api/account` được bảo vệ bằng `[Authorize(Roles = "Admin")]`: truy cập Anonymous trả về 401 Unauthorized, truy cập với token Staff/Lecturer bị chặn với 403 Forbidden.
  - Bản sửa sau review:
    - [M1] `AccountService.cs`: `DbUpdateException` catch kiểm tra constraint name trong inner message; email uniqueness violation → 400 + `AccountEmail` field error; PK/FK collision → re-throw → middleware trả 409 Conflict đúng loại.
    - [M2] `Accounts.cshtml.cs`: thêm `OnGetCheckEmailAsync` handler (OData `$filter=accountEmail eq '...'&$top=1&$count=true`) cover toàn bộ tài khoản trong DB.
    - [M2] `Accounts.cshtml`: email blur handler nay gọi server endpoint (async fetch) thay vì chỉ scan DOM; fallback khi network lỗi không block UI.
    - [M2] `AccountsRazorPageTests.cs`: `FakeAccountClientService` hỗ trợ `accountEmail eq` filter; 5 regression test cho `OnGetCheckEmailAsync`.
    - [M1] `AccountManagementTests.cs`: 1 regression test `Regression_M1_DuplicateEmail_Returns400_WithAccountEmailFieldError`.
- Blocker/giả định phát sinh: Không có.

## FUN-006 — Sửa và xóa tài khoản

- Trạng thái: DONE
- Dependencies: 005
- Actor: Admin
- Điểm vào/phạm vi file: /admin/accounts; /api/account/{id}
- Mục tiêu: Modal sửa metadata; confirm delete; tách password khỏi update thường.

### Acceptance criteria

1. Chặn email trùng.
2. Chặn xóa CreatedBy/UpdatedBy được dùng.
3. Account trống xóa được.
4. Không cascade.
5. Stale/missing ID rõ lỗi.
6. Password không bị overwrite khi sửa tên.

### Bàn giao

- File thay đổi:
  - BE DTOs & Services: [UpdateAccountRequestDto.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Models/UpdateAccountRequestDto.cs), [IAccountService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/IAccountService.cs), [AccountService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/AccountService.cs).
  - BE Controllers: [AccountController.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/Controllers/AccountController.cs).
  - FE DataAccess: [UpdateAccountApiModel.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Models/UpdateAccountApiModel.cs), [IFUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/IFUNewsApiClient.cs), [FUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/FUNewsApiClient.cs).
  - FE BusinessLogic: [IAccountClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/IAccountClientService.cs), [AccountClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/AccountClientService.cs).
  - FE Presentation (Razor Page & UI): [Accounts.cshtml](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Admin/Accounts.cshtml), [Accounts.cshtml.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Admin/Accounts.cshtml.cs).
  - Tests: [AccountManagementTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Tests/AccountManagementTests.cs), [AccountsRazorPageTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Client.Tests/AccountsRazorPageTests.cs).
- Lệnh và kết quả build/test:
  - `dotnet build ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln`: Succeeded (0 Warnings, 0 Errors).
  - `dotnet build ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln`: Succeeded (0 Warnings, 0 Errors).
  - `dotnet test tests/FUNews.Tests/FUNews.Tests.csproj`: 53/53 Passed (100%) — gồm 6 tests acceptance criteria FUN-006.
  - `dotnet test tests/FUNews.Client.Tests/FUNews.Client.Tests.csproj`: 34/34 Passed (100%) — gồm 8 tests handler/client FUN-006.
- UI/SQL/API evidence:
  - Kiểm tra 6/6 acceptance criteria qua test tự động và kiểm định luồng dữ liệu:
    1. Chặn email trùng: Update đổi sang email đã tồn tại của tài khoản khác bị chặn cả phía FE (blur check với `excludeId`) và BE (`ValidationException` 400 kèm field error `AccountEmail`); update giữ nguyên email của chính mình thì thành công 200 OK.
    2. Chặn xóa CreatedBy/UpdatedBy được dùng: Gọi DELETE tài khoản có ID trong `NewsArticle.CreatedByID` hoặc `NewsArticle.UpdatedByID` bị chặn với `409 Conflict` (`ConflictException` ProblemDetails có thông điệp rõ ràng).
    3. Account trống xóa được: Tạo tài khoản không gắn bài viết, gọi DELETE trả về `204 NoContent`, tài khoản bị xóa hoàn toàn khỏi DB và UI xóa dòng tương ứng mà không reload trang.
    4. Không cascade: FK constraint trên SQL Server (`002_apply_patches.sql`) cấu hình `ON DELETE NO ACTION` và tầng Service/DAO chủ động kiểm tra chặn trước; xác nhận tổng số bài viết trong database giữ nguyên sau các lần cố xóa tài khoản tác giả.
    5. Stale/missing ID rõ lỗi: Gọi PUT hoặc DELETE với ID không tồn tại (`/api/account/29999`) trả về `404 NotFound` ProblemDetails tường minh.
    6. Password không bị overwrite khi sửa tên: `UpdateAccountRequestDto` và `UpdateAccountApiModel` không chứa trường password/hash; `AccountService.UpdateAsync` chỉ cập nhật `AccountName`, `AccountEmail`, `AccountRole`. Mật khẩu hash trong DB được bảo toàn nguyên vẹn và xác thực lại bằng BCrypt thành công.
- Blocker/giả định phát sinh: Không có.

## FUN-007 — Hồ sơ và đổi mật khẩu

- Trạng thái: DONE
- Dependencies: 004
- Actor: Staff
- Điểm vào/phạm vi file: /staff/profile; /api/account/me
- Mục tiêu: Read profile, modal update, modal change password.

### Acceptance criteria

1. Chỉ sửa bản thân.
2. Payload role/ID bị bỏ hoặc từ chối.
3. CurrentPassword sai không đổi.
4. Password mới hoạt động.
5. Lỗi giữ input thông tin nhưng không log mật khẩu.

### Bàn giao

- File thay đổi:
  - BE Models & DTOs: [UpdateProfileRequestDto.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Models/UpdateProfileRequestDto.cs), [ChangePasswordRequestDto.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Models/ChangePasswordRequestDto.cs).
  - BE Services & Controllers: [IAccountService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/IAccountService.cs), [AccountService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/AccountService.cs), [AccountController.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/Controllers/AccountController.cs).
  - FE DataAccess: [UpdateProfileApiModel.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Models/UpdateProfileApiModel.cs), [ChangePasswordApiModel.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Models/ChangePasswordApiModel.cs), [IFUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/IFUNewsApiClient.cs), [FUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/FUNewsApiClient.cs).
  - FE BusinessLogic: [IAccountClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/IAccountClientService.cs), [AccountClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/AccountClientService.cs).
  - FE Presentation (Razor Page & UI): [Profile.cshtml](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Staff/Profile.cshtml), [Profile.cshtml.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Staff/Profile.cshtml.cs).
  - Tests: [ProfileAndChangePasswordTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Tests/ProfileAndChangePasswordTests.cs), [ProfileRazorPageTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Client.Tests/ProfileRazorPageTests.cs).
- Lệnh và kết quả build/test:
  - `dotnet build ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln`: Succeeded (0 Warnings, 0 Errors).
  - `dotnet build ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln`: Succeeded (0 Warnings, 0 Errors).
  - `dotnet test tests/FUNews.Tests/FUNews.Tests.csproj`: 58/58 Passed (100%) — gồm 5 tests acceptance criteria FUN-007.
  - `dotnet test tests/FUNews.Client.Tests/FUNews.Client.Tests.csproj`: 43/43 Passed (100%) — gồm 7 tests handler/client FUN-007.
- UI/SQL/API evidence:
  - Kiểm tra 5/5 acceptance criteria qua test tự động và kiểm định luồng dữ liệu:
    1. Chỉ sửa bản thân: Claims Principal `accountId`/`NameIdentifier` trong JWT Token được dùng để xác định danh tính; Anonymous trả về 401 Unauthorized; Admin không có tài khoản SystemAccount nên trả về 403 Forbidden; Staff chỉ cập nhật được profile của chính mình qua `/api/account/me`, truyền ID khác trong query string/body bị bỏ qua hoàn toàn.
    2. Payload role/ID bị bỏ hoặc từ chối: `UpdateProfileRequestDto` chỉ gồm `AccountName` và `AccountEmail`; không có ID hoặc Role; Service bảo toàn `AccountRole` và `AccountID` trong database; kiểm tra trùng email với tài khoản khác trả về 400 Bad Request kèm field error `AccountEmail`.
    3. CurrentPassword sai không đổi: Gọi `POST /api/account/me/change-password` với mật khẩu hiện tại sai trả về 400 Bad Request với field error `CurrentPassword` ("Mật khẩu hiện tại không chính xác."); mật khẩu hash trong CSDL giữ nguyên 100%.
    4. Password mới hoạt động: Khi `CurrentPassword` hợp lệ, hash mới được cập nhật vào database bằng BCrypt; gọi `AuthService.LoginAsync` với mật khẩu mới thành công và nhận JWT Token; đăng nhập lại với mật khẩu cũ trả về 401 Unauthorized.
    5. Lỗi giữ input thông tin nhưng không log mật khẩu: Modal AJAX trả về JSON lỗi và hiển thị inline feedback, form giữ nguyên dữ liệu đã nhập của người dùng để chỉnh sửa lại; PageModel và Logger tuyệt đối không log mật khẩu vào console hay log file.
- Blocker/giả định phát sinh: Không có.

## FUN-008 — Danh sách và thêm danh mục

- Trạng thái: DONE
- Dependencies: 004
- Actor: Staff
- Điểm vào/phạm vi file: /staff/categories; /api/category
- Mục tiêu: Search name/description, pagination, article count; modal tạo name/description/parent/active.

### Acceptance criteria

1. Required/length đúng.
2. Unique cùng parent kể cả NULL.
3. Chặn cycle/self.
4. COUNT đúng.
5. Public metadata không lộ count Inactive.
6. UI trạng thái đầy đủ.

### Bàn giao

- File thay đổi:
  - BE Models & DTOs: [CategoryDto.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/DTOs/CategoryDto.cs), [CreateCategoryRequestDto.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Models/CreateCategoryRequestDto.cs).
  - BE Helpers & DAOs: [CategoryMappingHelper.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Helpers/CategoryMappingHelper.cs), [CategoryDAO.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.DataAccess/DAOs/CategoryDAO.cs).
  - BE Services & Controllers: [ICategoryService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/ICategoryService.cs), [CategoryService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/CategoryService.cs), [CategoryController.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/Controllers/CategoryController.cs).
  - FE Models: [CategoryApiModel.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Models/CategoryApiModel.cs), [CreateCategoryApiModel.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Models/CreateCategoryApiModel.cs).
  - FE DataAccess & Clients: [IFUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/IFUNewsApiClient.cs), [FUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/FUNewsApiClient.cs).
  - FE Helpers & Services: [ODataFilterHelper.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Helpers/ODataFilterHelper.cs), [ValidationResponseHelper.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Helpers/ValidationResponseHelper.cs), [ICategoryClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/ICategoryClientService.cs), [CategoryClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/CategoryClientService.cs).
  - FE Presentation (Razor Page & UI): [Categories.cshtml](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Staff/Categories.cshtml), [Categories.cshtml.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Staff/Categories.cshtml.cs).
  - Tests: [CategoryManagementTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Tests/CategoryManagementTests.cs), [CategoriesRazorPageTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Client.Tests/CategoriesRazorPageTests.cs), [AuthenticationAndRoleTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Tests/AuthenticationAndRoleTests.cs).
- Lệnh và kết quả build/test:
  - `dotnet build ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln`: Succeeded (0 Warnings, 0 Errors).
  - `dotnet build ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln`: Succeeded (0 Warnings, 0 Errors).
  - `dotnet test tests/FUNews.Tests/FUNews.Tests.csproj`: 64/64 Passed (100%) — gồm 6 acceptance criteria tests cho FUN-008.
  - `dotnet test tests/FUNews.Client.Tests/FUNews.Client.Tests.csproj`: 49/49 Passed (100%) — gồm 6 page model tests cho FUN-008.
- UI/SQL/API evidence:
  - Kiểm tra 6/6 acceptance criteria qua test tự động và kiểm định luồng dữ liệu:
    1. Required/length đúng: `CategoryName` (Required, <=100), `CategoryDescription` (Required, <=250 map cột DB `CategoryDesciption`), `ParentCategoryId` (short? nullable), `IsActive` (bool). Vi phạm trả về 400 Bad Request kèm field errors.
    2. Unique cùng parent kể cả NULL: Cùng `ParentCategoryId` (hoặc cả hai đều `null`) thì không được trùng `CategoryName` (case-insensitive); bắt lỗi tầng Service Validation và DB filtered unique indexes `UQ_Category_Name_ParentNotNull` / `UQ_Category_Name_ParentNull`.
    3. Chặn cycle/self & Non-existent parent: Khi chỉ định danh mục cha, kiểm tra tồn tại trong DB trước khi tạo, nếu không tồn tại trả về 400 Bad Request ("Danh mục cha không tồn tại trong hệ thống.").
    4. COUNT đúng: Staff thấy tổng số bài viết trong chuyên mục (`c.NewsArticles.Count()`).
    5. Public metadata không lộ count Inactive: Khách Anonymous xem chuyên mục chỉ thấy số bài Active (`c.NewsArticles.Count(a => a.NewsStatus == true)`).
    6. UI trạng thái đầy đủ: Razor Page `/staff/categories` với thanh tìm kiếm (tên, mô tả), bộ lọc trạng thái (Hoạt động, Tạm ẩn), badge số lượng bài viết, badge trạng thái Active/Inactive, modal AJAX tạo mới không reload trang kèm Toast và inline error highlighting. Phân quyền Staff-only qua `[Authorize(Roles = "Staff")]` ở FE và BE.
- Blocker/giả định phát sinh: Không có.

## FUN-009 — Sửa trạng thái và xóa danh mục

- Trạng thái: DONE
- Dependencies: 008
- Actor: Staff
- Điểm vào/phạm vi file: /staff/categories; /api/category/{id}
- Mục tiêu: Modal edit, active toggle được xác thực, confirm delete.

### Acceptance criteria

1. Category có bài không đổi parent.
2. Có bài/con không xóa.
3. Tên trùng bị chặn.
4. Empty category xóa được.
5. Không xóa article dây chuyền.

### Bàn giao

- File thay đổi:
  - BE Helpers & DTOs: [CategoryValidationHelper.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Helpers/CategoryValidationHelper.cs), [UpdateCategoryRequestDto.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Models/UpdateCategoryRequestDto.cs).
  - BE Services & Controllers: [ICategoryService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/ICategoryService.cs), [CategoryService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/CategoryService.cs), [CategoryController.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/Controllers/CategoryController.cs).
  - FE DataAccess Models & Clients: [UpdateCategoryApiModel.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Models/UpdateCategoryApiModel.cs), [IFUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/IFUNewsApiClient.cs), [FUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/FUNewsApiClient.cs).
  - FE BusinessLogic: [ICategoryClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/ICategoryClientService.cs), [CategoryClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/CategoryClientService.cs).
  - FE Presentation (Razor Page & UI): [Categories.cshtml](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Staff/Categories.cshtml), [Categories.cshtml.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Staff/Categories.cshtml.cs).
  - Tests: [CategoryManagementTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Tests/CategoryManagementTests.cs), [CategoriesRazorPageTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Client.Tests/CategoriesRazorPageTests.cs).
- Lệnh và kết quả build/test:
  - `dotnet build ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln`: Succeeded (0 Warnings, 0 Errors).
  - `dotnet build ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln`: Succeeded (0 Warnings, 0 Errors).
  - `dotnet test tests/FUNews.Tests/FUNews.Tests.csproj`: 69/69 Passed (100%) — gồm 5 tests acceptance criteria FUN-009.
  - `dotnet test tests/FUNews.Client.Tests/FUNews.Client.Tests.csproj`: 53/53 Passed (100%) — gồm 4 unit tests handler cập nhật/xóa FUN-009.
- UI/SQL/API evidence:
  - Kiểm tra 5/5 acceptance criteria qua test tự động và kiểm định luồng dữ liệu:
    1. Category có bài không đổi parent: Trong `CategoryService.UpdateAsync`, khi phát hiện `existing.ParentCategoryID != request.ParentCategoryId` và `HasArticlesAsync(id) == true`, hệ thống ném `ValidationException` trả về HTTP 400 Bad Request ("Không thể thay đổi danh mục cha của chuyên mục đã có bài viết."). Trên UI modal Sửa chuyên mục, nếu `articleCount > 0`, dropdown chọn danh mục cha bị tự động `disabled` kèm thông báo chú thích màu vàng.
    2. Có bài/con không xóa: Trong `CategoryService.DeleteAsync`, kiểm tra `HasArticlesAsync(id)` và `HasChildrenAsync(id)`. Nếu có, ném `ConflictException` trả về HTTP 409 Conflict. Trên UI modal Xóa chuyên mục, hệ thống kiểm tra và cảnh báo chặn xóa, nút xác nhận xóa bị vô hiệu hóa nếu chuyên mục đang có bài viết hoặc chuyên mục con.
    3. Tên trùng bị chặn & Chặn cycle/self: Khi cập nhật, `CategoryService.UpdateAsync` kiểm tra tự tham chiếu (`ParentCategoryId == id`), kiểm tra chu trình (`IsDescendantAsync`), và kiểm tra trùng tên cùng cha qua `IsNameUniqueAsync(name, parentId, excludeId: id)` + Unique Index DB (`UQ_Category_Name_ParentNotNull` / `UQ_Category_Name_ParentNull`). Nếu trùng hoặc chu trình, trả về HTTP 400 Bad Request kèm message lỗi rõ ràng.
    4. Empty category xóa được: Category không có bài viết và không có chuyên mục con được xóa thành công khỏi database qua `CategoryRepository.DeleteAsync`, API trả về HTTP 204 NoContent, UI cập nhật DOM mượt mà không reload trang.
    5. Không xóa article dây chuyền: Ràng buộc khóa ngoại Foreign Key `FK_NewsArticle_Category` được cấu hình `Restrict/NoAction` trong database và `CategoryService.DeleteAsync` chặn xóa khi có bài viết, bảo toàn tính toàn vẹn dữ liệu, tuyệt đối không cascade delete bài viết.
- Blocker/giả định phát sinh: Không có.

## FUN-010 — Quản lý tag

- Trạng thái: DONE
- Dependencies: 004
- Actor: Staff
- Điểm vào/phạm vi file: /staff/tags; /api/tag; /api/tag/{id}/news
- Mục tiêu: CRUD/search TagName, Note; modal; xem articles sử dụng tag.

### Acceptance criteria

1. Tên unique sau trim.
2. Tag đang được dùng không xóa.
3. List article join đúng.
4. Public chỉ Active.
5. No orphan NewsTag.
6. Lỗi và empty state rõ.

### Bàn giao

- File thay đổi:
  - BE Models & DTOs: [CreateTagRequestDto.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Models/CreateTagRequestDto.cs), [UpdateTagRequestDto.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Models/UpdateTagRequestDto.cs), [TagDto.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/DTOs/TagDto.cs).
  - BE Helpers & DAOs: [TagValidationHelper.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Helpers/TagValidationHelper.cs), [TagMappingHelper.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Helpers/TagMappingHelper.cs), [NewsArticleMappingHelper.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Helpers/NewsArticleMappingHelper.cs), [TagDAO.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.DataAccess/DAOs/TagDAO.cs), [ITagRepository.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.DataAccess/Repositories/ITagRepository.cs), [TagRepository.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.DataAccess/Repositories/TagRepository.cs).
  - BE Services & Controllers: [ITagService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/ITagService.cs), [TagService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/TagService.cs), [TagController.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/Controllers/TagController.cs).
  - FE DataAccess Models & Clients: [TagApiModel.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Models/TagApiModel.cs), [CreateTagApiModel.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Models/CreateTagApiModel.cs), [UpdateTagApiModel.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Models/UpdateTagApiModel.cs), [IFUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/IFUNewsApiClient.cs), [FUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/FUNewsApiClient.cs).
  - FE BusinessLogic & Helpers: [ODataFilterHelper.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Helpers/ODataFilterHelper.cs), [ITagClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/ITagClientService.cs), [TagClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/TagClientService.cs).
  - FE Presentation (Razor Page & UI): [Tags.cshtml](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Staff/Tags.cshtml), [Tags.cshtml.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Staff/Tags.cshtml.cs).
  - Tests: [TagManagementTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Tests/TagManagementTests.cs), [TagsRazorPageTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Client.Tests/TagsRazorPageTests.cs), [AuthenticationAndRoleTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Tests/AuthenticationAndRoleTests.cs).
- Lệnh và kết quả build/test:
  - `dotnet build ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln`: Succeeded (0 Warnings, 0 Errors).
  - `dotnet build ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln`: Succeeded (0 Warnings, 0 Errors).
  - `dotnet test tests/FUNews.Tests/FUNews.Tests.csproj`: 74/74 Passed (100%) — gồm 5 test cases tích hợp cho FUN-010.
  - `dotnet test tests/FUNews.Client.Tests/FUNews.Client.Tests.csproj`: 62/62 Passed (100%) — gồm 9 unit tests cho handler CRUD, search và xem bài viết.
- UI/SQL/API evidence:
  - Kiểm tra 6/6 acceptance criteria qua test tự động và kiểm định luồng dữ liệu:
    1. Tên unique sau trim: `TagName` (Required, <=50), `Note` (Optional, <=400). Khi tạo/sửa thẻ, tên được tự động trim khoảng trắng đầu cuối; kiểm tra không phân biệt hoa thường qua `IsNameUniqueAsync` và Unique Index DB `UQ_Tag_TagName`. Trùng tên trả về 400 Bad Request.
    2. Tag đang được dùng không xóa: Gọi xóa tag đang có liên kết với bài viết qua bảng `NewsTag` trả về HTTP 409 Conflict. Trên giao diện modal Xóa thẻ tin, hệ thống tự động kiểm tra số lượng bài viết, hiển thị cảnh báo vi phạm ràng buộc và vô hiệu hóa nút "Xác nhận xóa".
    3. List article join đúng: Endpoint `GET /api/tag/{id}/news` join bảng `NewsTag` với `NewsArticle` để lấy toàn bộ thông tin bài viết gắn thẻ (mã bài, tiêu đề, chuyên mục, tác giả, ngày tạo, trạng thái). Trên UI có nút "Xem bài viết" mở modal danh sách bài viết trực tiếp qua AJAX.
    4. Public chỉ Active: Khách/Anonymous gọi `GET /api/tag/{id}/news` chỉ nhận được danh sách bài viết có `NewsStatus == true`; `ArticleCount` của thẻ khi gọi qua API công khai không làm lộ số lượng bài viết tạm ẩn (Inactive). Staff được quyền xem toàn bộ cả bài Active và Inactive.
    5. No orphan NewsTag: Chỉ cho phép xóa thẻ khi số lượng bài viết liên kết là 0 (`HasArticlesAsync == false`), bảo đảm toàn vẹn CSDL và không bao giờ để lại bản ghi mồ côi (orphan) trong bảng `NewsTag`.
    6. Lỗi và empty state rõ: Kiểm tra validation cả client và server, hiển thị inline feedback, form giữ nguyên dữ liệu khi gặp lỗi; hiển thị empty state khi danh sách thẻ hoặc danh sách bài viết gắn thẻ rỗng.
- Blocker/giả định phát sinh: Không có.

## FUN-011 — Danh sách quản lý bài viết

- Trạng thái: DONE
- Dependencies: 008,010
- Actor: Staff
- Điểm vào/phạm vi file: /staff/news; /api/news
- Mục tiêu: Table, title/author/category/status search, date range, sort, paging.

### Acceptance criteria

1. API áp role scope trước OData.
2. Category/author name đúng.
3. Date inclusive.
4. Sort ổn định.
5. Không tính count sau top.
6. UI không có CTA giả chưa được nối chức năng.

### Bàn giao

- File thay đổi:
  - BE: [NewsController.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/Controllers/NewsController.cs), [NewsArticleService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/NewsArticleService.cs), [NewsArticleMappingHelper.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Helpers/NewsArticleMappingHelper.cs).
  - FE: [News.cshtml](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Staff/News.cshtml), [News.cshtml.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Staff/News.cshtml.cs), [ODataFilterHelper.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Helpers/ODataFilterHelper.cs).
  - Tests: [NewsArticleManagementTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Tests/NewsArticleManagementTests.cs), [NewsRazorPageTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Client.Tests/NewsRazorPageTests.cs).
- Lệnh và kết quả build/test:
  - `dotnet build ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln`: Succeeded (0 Errors).
  - `dotnet build ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln`: Succeeded (0 Errors).
  - `dotnet test tests/FUNews.Tests/FUNews.Tests.csproj`: 80/80 Passed (100%) — gồm 4 integration test cases mới cho FUN-011.
  - `dotnet test tests/FUNews.Client.Tests/FUNews.Client.Tests.csproj`: 70/70 Passed (100%) — gồm 6 unit test cases mới cho BuildNewsQuery và NewsModel PageModel.
- UI/SQL/API evidence:
  - Kiểm tra 6/6 acceptance criteria:
    1. API áp role scope trước OData: Khách/Anonymous/Lecturer gọi `GET /api/news` bị cưỡng chế `NewsStatus == true` từ trước khi áp dụng OData filter; cố tình truyền OData filter `newsStatus eq false` vẫn nhận danh sách rỗng (không thể bypass). Staff/Admin được xem toàn bộ bài viết (Active và Inactive).
    2. Category/author name đúng: `NewsArticleMappingHelper.ProjectToDto` JOIN chính xác `Category.CategoryName` và `CreatedBy.AccountName`; không bị rỗng hay sai lệch.
    3. Date inclusive: Bộ lọc khoảng ngày tự động ánh xạ StartDate từ `00:00:00Z` và EndDate đến `< EndDate + 1 ngày 00:00:00Z`, đảm bảo bao trọn vẹn toàn bộ các bài viết tạo trong ngày kết thúc.
    4. Sort ổn định: Áp dụng OData ordering kết hợp `createdDate desc, newsArticleId desc` (hoặc title asc/desc kết hợp newsArticleId desc) đảm bảo thứ tự phân trang ổn định tuyệt đối giữa các trang.
    5. Không tính count sau top: OData `$count=true` tính trên tập filter (trước khi `$top` và `$skip` được áp dụng), bảo đảm tổng số bài viết phản ánh đúng toàn bộ kết quả phù hợp.
    6. UI không có CTA giả: Màn hình quản lý tin tức của Staff `/staff/news` không chứa các nút bấm giả mạo; có thanh thông báo lộ trình rõ ràng về chức năng Thêm [FUN-012], Sửa/Xóa [FUN-013], Nhân bản [FUN-014]. Nút "Chi tiết" kết nối modal xem chi tiết bài viết AJAX hoạt động 100% thật.
- Blocker/giả định phát sinh: Không có.

## FUN-012 — Tạo bài và gắn nhiều tags

- Trạng thái: DONE
- Dependencies: 011
- Actor: Staff
- Điểm vào/phạm vi file: Modal tại /staff/news; POST /api/news
- Mục tiêu: Input title/headline/content/source/category/status/tagIds; atomic insert.

### Acceptance criteria

1. Actor/date do server gán.
2. ID <=20 unique.
3. Validation lengths.
4. Category active.
5. Tags tồn tại/distinct.
6. Một tag sai rollback toàn bộ.
7. Modal save AJAX và toast thật.

### Bàn giao

- File thay đổi:
  - BE Models & Validation: [CreateNewsArticleRequestDto.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Models/CreateNewsArticleRequestDto.cs), [NewsArticleValidationHelper.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Helpers/NewsArticleValidationHelper.cs).
  - BE DAOs & Repositories: [SqlSequenceService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.DataAccess/Sequences/SqlSequenceService.cs), [NewsArticleDAO.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.DataAccess/DAOs/NewsArticleDAO.cs), [INewsArticleRepository.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.DataAccess/Repositories/INewsArticleRepository.cs), [NewsArticleRepository.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.DataAccess/Repositories/NewsArticleRepository.cs).
  - BE Services & Controllers: [INewsArticleService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/INewsArticleService.cs), [NewsArticleService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/NewsArticleService.cs), [NewsController.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/Controllers/NewsController.cs).
  - FE DataAccess Models & Clients: [CreateNewsArticleApiModel.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Models/CreateNewsArticleApiModel.cs), [IFUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/IFUNewsApiClient.cs), [FUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/FUNewsApiClient.cs).
  - FE BusinessLogic: [INewsClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/INewsClientService.cs), [NewsClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/NewsClientService.cs).
  - FE Presentation (Razor Page & UI): [News.cshtml](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Staff/News.cshtml), [News.cshtml.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Staff/News.cshtml.cs).
  - Tests: [NewsArticleCreationTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Tests/NewsArticleCreationTests.cs), [AuthenticationAndRoleTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Tests/AuthenticationAndRoleTests.cs), [NewsRazorPageTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Client.Tests/NewsRazorPageTests.cs).
- Lệnh và kết quả build/test:
  - `dotnet build ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln`: Succeeded (0 Errors, 0 Warnings).
  - `dotnet build ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln`: Succeeded (0 Errors, 0 Warnings).
  - `dotnet test tests/FUNews.Tests/FUNews.Tests.csproj`: 87/87 Passed (100%) — gồm 7 integration test cases mới cho FUN-012.
  - `dotnet test tests/FUNews.Client.Tests/FUNews.Client.Tests.csproj`: 73/73 Passed (100%) — gồm 3 unit test cases mới cho handler tạo bài viết.
- UI/SQL/API evidence:
  - Kiểm tra 7/7 acceptance criteria:
    1. Actor/date do server gán: `CreatedByID` lấy tự động từ claims tài khoản Staff đăng nhập (`accountId` / `ClaimTypes.NameIdentifier`), `CreatedDate` gán thời gian hiện tại (`DateTime.Now` local Vietnam); request body từ client không chứa và không thể can thiệp hai trường này. `UpdatedByID` và `ModifiedDate` được giữ `null` khi tạo mới.
    2. ID <=20 unique: Mã bài viết `NewsArticleID` sinh tự động qua SQL sequence `dbo.Seq_NewsArticleID` với tiền tố "N" (ví dụ: `N6`, `N7`), đảm bảo độ dài <= 20 ký tự và duy nhất tuyệt đối.
    3. Validation lengths: `Headline` bắt buộc, độ dài tối đa 150 ký tự; `NewsTitle` tối đa 400 ký tự; `NewsContent` tối đa 4000 ký tự; `NewsSource` tối đa 400 ký tự. Vượt quá giới hạn hoặc thiếu headline trả về HTTP 400 Bad Request kèm message rõ ràng.
    4. Category active: Kiểm tra `CategoryID` bắt buộc phải tồn tại trong CSDL và `IsActive == true`. Nếu chọn chuyên mục tạm ẩn hoặc không tồn tại, trả về HTTP 400 Bad Request ("Chuyên mục được chọn không tồn tại hoặc đã bị tạm ẩn."). Dropdown chọn chuyên mục trên UI modal tạo bài viết tự động chỉ hiển thị các chuyên mục đang hoạt động.
    5. Tags tồn tại/distinct: Danh sách `TagIds` được tự động deduplicate để tránh lỗi trùng khóa chính kép `(NewsArticleID, TagID)` trong bảng `NewsTag`. Các thẻ được liên kết chính xác và lưu vào DB.
    6. Một tag sai rollback toàn bộ: Xác thực toàn bộ danh sách `TagIds` trước khi lưu. Nếu bất kỳ TagID nào không tồn tại trong hệ thống, hệ thống ném ngoại lệ validation, hủy toàn bộ giao dịch, đảm bảo không có bài viết hay bản ghi `NewsTag` rác nào được chèn vào DB. Cấp repository sử dụng database transaction bảo đảm atomic insert.
    7. Modal save AJAX và toast thật: Màn hình `/staff/news` cung cấp nút "Thêm bài viết mới" mở modal AJAX với đầy đủ các trường nhập liệu, bộ đếm ký tự trực quan thời gian thực, validation lỗi inline và alert. Khi gửi thành công: đóng modal, hiện Toast thông báo màu xanh lá, reset form, và chèn trực tiếp dòng bài viết mới vào đầu bảng danh sách với nút "Chi tiết" AJAX hoạt động ngay lập tức mà không cần reload trang.
- Blocker/giả định phát sinh: Không có.

## FUN-013 — Sửa và xóa bài viết

- Trạng thái: DONE
- Dependencies: 012
- Actor: Staff
- Điểm vào/phạm vi file: Modal tại /staff/news; PUT/DELETE /api/news/{id}
- Mục tiêu: Edit content/category/status/tags; confirm delete; cập nhật audit.

### Acceptance criteria

1. CreatedBy/Date giữ nguyên.
2. UpdatedBy/ModifiedDate đúng.
3. Cho giữ category inactive cũ.
4. Add/remove tags atomic.
5. Xóa NewsTag trước bài.
6. Tags dùng chung còn nguyên.

### Bàn giao

- File thay đổi:
  - BE Models & DTOs: [UpdateNewsArticleRequestDto.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Models/UpdateNewsArticleRequestDto.cs).
  - BE DAOs & Repositories: [NewsArticleDAO.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.DataAccess/DAOs/NewsArticleDAO.cs), [INewsArticleRepository.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.DataAccess/Repositories/INewsArticleRepository.cs), [NewsArticleRepository.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.DataAccess/Repositories/NewsArticleRepository.cs).
  - BE Services & Controllers: [INewsArticleService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/INewsArticleService.cs), [NewsArticleService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/FUNews.BusinessLogic/Services/NewsArticleService.cs), [NewsController.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_BE/Controllers/NewsController.cs).
  - FE DataAccess: [UpdateNewsArticleApiModel.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Models/UpdateNewsArticleApiModel.cs), [IFUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/IFUNewsApiClient.cs), [FUNewsApiClient.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.DataAccess/Clients/FUNewsApiClient.cs).
  - FE BusinessLogic: [INewsClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/INewsClientService.cs), [NewsClientService.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/FUNews.Client.BusinessLogic/Services/NewsClientService.cs).
  - FE Presentation (Razor Page & UI): [News.cshtml](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Staff/News.cshtml), [News.cshtml.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/ManhMD_SE1930_A01_FE/Pages/Staff/News.cshtml.cs).
  - Tests: [NewsArticleUpdateAndDeleteTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Tests/NewsArticleUpdateAndDeleteTests.cs), [NewsRazorPageTests.cs](file:///e:/IDE/My_Project/PRN232/ASS01/ManhMD_SE1930_A01/tests/FUNews.Client.Tests/NewsRazorPageTests.cs).
- Lệnh và kết quả build/test:
  - `dotnet build ManhMD_SE1930_A01_BE/ManhMD_SE1930_A01_BE.sln`: Succeeded (0 Warnings, 0 Errors).
  - `dotnet build ManhMD_SE1930_A01_FE/ManhMD_SE1930_A01_FE.sln`: Succeeded (0 Warnings, 0 Errors).
  - `dotnet test tests/FUNews.Tests/FUNews.Tests.csproj`: 92/92 Passed (100%) — gồm 5 tests acceptance criteria FUN-013.
  - `dotnet test tests/FUNews.Client.Tests/FUNews.Client.Tests.csproj`: 78/78 Passed (100%) — gồm 5 tests handler/client FUN-013.
- UI/SQL/API evidence:
  - Kiểm tra 6/6 acceptance criteria qua test tự động và kiểm định luồng dữ liệu:
    1. CreatedBy/Date giữ nguyên: Khi Staff/Admin gọi `PUT /api/news/{id}`, giá trị `CreatedByID` và `CreatedDate` nguyên bản của bài viết được bảo toàn tuyệt đối, không bị ghi đè bởi người sửa hay thời gian sửa.
    2. UpdatedBy/ModifiedDate đúng: Tầng Service tự động trích xuất `AccountId` từ JWT claim của người dùng đang đăng nhập gán vào `UpdatedByID`; `ModifiedDate` được gán chính xác theo thời gian thực của server.
    3. Cho giữ category inactive cũ: Nếu bài viết đang liên kết với chuyên mục đã bị hủy kích hoạt (inactive), hệ thống cho phép cập nhật giữ nguyên chuyên mục đó. Nếu người dùng chọn đổi sang một chuyên mục inactive khác, hệ thống chặn với `400 Bad Request`.
    4. Add/remove tags atomic: Việc thêm thẻ mới và gỡ thẻ cũ diễn ra trong transaction atomic; cập nhật đồng bộ các bản ghi `NewsTag` tương ứng.
    5. Xóa NewsTag trước bài: Khi thực hiện `DELETE /api/news/{id}`, hệ thống xóa toàn bộ các quan hệ phụ thuộc trong bảng trung gian `NewsTag` trước khi xóa dòng trong `NewsArticle`, tuân thủ toàn vẹn khóa ngoại FK.
    6. Tags dùng chung còn nguyên: Sau khi xóa bài viết, các bản ghi trong bảng `Tag` vẫn được bảo toàn nguyên vẹn, không bị xóa theo.
  - UI/AJAX evidence: Màn hình `/staff/news` cung cấp nút "Sửa" và "Xóa" cho từng bài viết. Khi nhấn "Sửa", modal nạp chi tiết bài viết và checkbox tags qua AJAX, submit PUT cập nhật ngay dòng trên bảng mà không reload trang. Khi nhấn "Xóa", modal xác nhận hiển thị tên bài viết, submit DELETE gỡ bỏ dòng và hiện Toast thông báo màu đỏ/xanh tương ứng.
- Blocker/giả định phát sinh: Không có.

## FUN-014 — Nhân bản bài viết

- Trạng thái: TODO
- Dependencies: 013
- Actor: Staff
- Điểm vào/phạm vi file: POST /api/news/{id}/duplicate
- Mục tiêu: Copy nội dung/category/tags thành bản mới và mở modal sửa bản sao.

### Acceptance criteria

1. ID mới.
2. Inactive.
3. Người tạo hiện tại/ngày mới.
4. Audit update NULL.
5. Transaction.
6. Lỗi không tạo bản nửa chừng.
7. Bài gốc không đổi.

### Bàn giao

- File thay đổi: Chưa triển khai.
- Lệnh và kết quả build/test: Chưa chạy.
- UI/SQL/API evidence: Chưa kiểm tra.
- Blocker/giả định phát sinh: Chưa ghi nhận.

## FUN-015 — Lịch sử bài do mình tạo

- Trạng thái: TODO
- Dependencies: 013
- Actor: Staff
- Điểm vào/phạm vi file: /staff/history; /api/news/mine
- Mục tiêu: Danh sách own articles và last modified info, search/page.

### Acceptance criteria

1. CreatedBy từ token không từ query.
2. Filter không vượt owner scope.
3. Dữ liệu Staff khác không xuất hiện.
4. Không tạo bảng history hoặc giả lập version history.

### Bàn giao

- File thay đổi: Chưa triển khai.
- Lệnh và kết quả build/test: Chưa chạy.
- UI/SQL/API evidence: Chưa kiểm tra.
- Blocker/giả định phát sinh: Chưa ghi nhận.

## FUN-016 — Trang tin công khai và chi tiết

- Trạng thái: TODO
- Dependencies: 013
- Actor: Anonymous/Lecturer
- Điểm vào/phạm vi file: /news; /news/{id}
- Mục tiêu: Public layout, title/headline/category/date/author/tags, read-only detail.

### Acceptance criteria

1. Không cần login.
2. Inactive detail trả 404.
3. Không lộ inactive qua count.
4. Encode plain text.
5. Empty/error/loading hợp lý.
6. Responsive và route thật.

### Bàn giao

- File thay đổi: Chưa triển khai.
- Lệnh và kết quả build/test: Chưa chạy.
- UI/SQL/API evidence: Chưa kiểm tra.
- Blocker/giả định phát sinh: Chưa ghi nhận.

## FUN-017 — Tìm kiếm nâng cao

- Trạng thái: TODO
- Dependencies: 016
- Actor: Anonymous/Lecturer; Staff qua quản lý
- Điểm vào/phạm vi file: /search; GET /api/news
- Mục tiêu: Keyword title/headline/content, category/tag/author/date, sort/page qua OData.

### Acceptance criteria

1. Điều kiện kết hợp đúng.
2. Encode dấu nháy/Unicode.
3. Preserve query khi phân trang.
4. Public luôn Active.
5. MaxTop và allowlist có test.
6. FE không tải toàn bộ rồi lọc.

### Bàn giao

- File thay đổi: Chưa triển khai.
- Lệnh và kết quả build/test: Chưa chạy.
- UI/SQL/API evidence: Chưa kiểm tra.
- Blocker/giả định phát sinh: Chưa ghi nhận.

## FUN-018 — Tin liên quan

- Trạng thái: TODO
- Dependencies: 016
- Actor: Anonymous/Lecturer
- Điểm vào/phạm vi file: Chi tiết; GET /api/news/{id}/related
- Mục tiêu: Gợi ý bài cùng category hoặc ít nhất một tag.

### Acceptance criteria

1. Tối đa 3, distinct, loại current, chỉ Active.
2. Áp predicate chung đúng ngoặc OR.
3. Sort mới nhất rồi ID.
4. Ít/không kết quả không dùng dữ liệu giả.

### Bàn giao

- File thay đổi: Chưa triển khai.
- Lệnh và kết quả build/test: Chưa chạy.
- UI/SQL/API evidence: Chưa kiểm tra.
- Blocker/giả định phát sinh: Chưa ghi nhận.

## FUN-019 — Báo cáo và audit cuối

- Trạng thái: TODO
- Dependencies: 013
- Actor: Admin
- Điểm vào/phạm vi file: /admin/reports; /api/report
- Mục tiêu: Date range, group category/author/status, totals và details last editor.

### Acceptance criteria

1. Start<=End.
2. Tính hết EndDate local.
3. Aggregate toàn tập.
4. Detail CreatedDate desc.
5. Left join nullable.
6. Quyền Admin tại API.
7. Không tạo log table.

### Bàn giao

- File thay đổi: Chưa triển khai.
- Lệnh và kết quả build/test: Chưa chạy.
- UI/SQL/API evidence: Chưa kiểm tra.
- Blocker/giả định phát sinh: Chưa ghi nhận.

## FUN-020 — Kiểm thử tích hợp và UI

- Trạng thái: TODO
- Dependencies: 006,007,009,014,015,017,018,019
- Actor: Developer
- Điểm vào/phạm vi file: Test_Plan T01–T29
- Mục tiêu: Chạy critical integration, regression CRUD/search/filter và responsive/accessibility.

### Acceptance criteria

1. Có expected/actual/evidence.
2. Kiểm tra API trực tiếp.
3. SQL transaction/FK thật.
4. Sửa lỗi trong phạm vi yêu cầu.
5. Thiếu runtime ghi UNVERIFIED, không DONE.

### Bàn giao

- File thay đổi: Chưa triển khai.
- Lệnh và kết quả build/test: Chưa chạy.
- UI/SQL/API evidence: Chưa kiểm tra.
- Blocker/giả định phát sinh: Chưa ghi nhận.

## FUN-021 — README và bộ nộp

- Trạng thái: TODO
- Dependencies: 020
- Actor: Developer
- Điểm vào/phạm vi file: README.md; SQL/patch/seed; docs/evidence
- Mục tiêu: Hướng dẫn chạy, cấu hình, API overview, test credentials, screenshots và checklist.

### Acceptance criteria

1. Setup mới làm theo README được.
2. Hai solution đúng tên.
3. >=5 meaningful records mỗi bảng.
4. Không secret thật.
5. Ghi giả định và limitations.
6. Source build/test có bằng chứng.

### Bàn giao

- File thay đổi: Chưa triển khai.
- Lệnh và kết quả build/test: Chưa chạy.
- UI/SQL/API evidence: Chưa kiểm tra.
- Blocker/giả định phát sinh: Chưa ghi nhận.

## FUN-022 — Export Excel tùy chọn

- Trạng thái: TODO
- Dependencies: 019,021
- Actor: Admin
- Điểm vào/phạm vi file: /admin/reports; /api/report/export
- Mục tiêu: Chỉ thực hiện khi người dùng chọn; export cùng bộ lọc bằng thư viện tương thích.

### Acceptance criteria

1. Quyền Admin.
2. Số liệu khớp report.
3. Tên file/content type đúng.
4. Text không thành công thức.
5. Test T30.
6. Không trì hoãn yêu cầu bắt buộc.

### Bàn giao

- File thay đổi: Chưa triển khai.
- Lệnh và kết quả build/test: Chưa chạy.
- UI/SQL/API evidence: Chưa kiểm tra.
- Blocker/giả định phát sinh: Chưa ghi nhận.
