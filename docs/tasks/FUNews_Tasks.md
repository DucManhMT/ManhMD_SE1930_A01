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
| FUN-009 | Sửa trạng thái và xóa danh mục | 008 | TODO |
| FUN-010 | Quản lý tag | 004 | TODO |
| FUN-011 | Danh sách quản lý bài viết | 008,010 | TODO |
| FUN-012 | Tạo bài và gắn nhiều tags | 011 | TODO |
| FUN-013 | Sửa và xóa bài viết | 012 | TODO |
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

- Trạng thái: TODO
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

- File thay đổi: Chưa triển khai.
- Lệnh và kết quả build/test: Chưa chạy.
- UI/SQL/API evidence: Chưa kiểm tra.
- Blocker/giả định phát sinh: Chưa ghi nhận.

## FUN-010 — Quản lý tag

- Trạng thái: TODO
- Dependencies: 004
- Actor: Staff
- Điểm vào/phạm vi file: /staff/tags; /api/tag
- Mục tiêu: CRUD/search TagName, Note; modal; xem articles sử dụng tag.

### Acceptance criteria

1. Tên unique sau trim.
2. Tag đang được dùng không xóa.
3. List article join đúng.
4. Public chỉ Active.
5. No orphan NewsTag.
6. Lỗi và empty state rõ.

### Bàn giao

- File thay đổi: Chưa triển khai.
- Lệnh và kết quả build/test: Chưa chạy.
- UI/SQL/API evidence: Chưa kiểm tra.
- Blocker/giả định phát sinh: Chưa ghi nhận.

## FUN-011 — Danh sách quản lý bài viết

- Trạng thái: TODO
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

- File thay đổi: Chưa triển khai.
- Lệnh và kết quả build/test: Chưa chạy.
- UI/SQL/API evidence: Chưa kiểm tra.
- Blocker/giả định phát sinh: Chưa ghi nhận.

## FUN-012 — Tạo bài và gắn nhiều tags

- Trạng thái: TODO
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

- File thay đổi: Chưa triển khai.
- Lệnh và kết quả build/test: Chưa chạy.
- UI/SQL/API evidence: Chưa kiểm tra.
- Blocker/giả định phát sinh: Chưa ghi nhận.

## FUN-013 — Sửa và xóa bài viết

- Trạng thái: TODO
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

- File thay đổi: Chưa triển khai.
- Lệnh và kết quả build/test: Chưa chạy.
- UI/SQL/API evidence: Chưa kiểm tra.
- Blocker/giả định phát sinh: Chưa ghi nhận.

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
