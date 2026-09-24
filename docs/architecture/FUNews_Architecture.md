# FUNews architecture

## Cấu trúc đề xuất

| Đường dẫn | Nội dung |
|---|---|
| `backend/StudentName_ClassCode_A01_BE.sln` | Solution Backend |
| `backend/FUNews.DataAccess/` | Entities, Context, Configurations, DAOs, Repositories |
| `backend/FUNews.BusinessLogic/` | Services, interfaces, validation, application DTOs |
| `backend/FUNews.Api/` | Controllers, auth, OData, DI, middleware; tầng Presentation |
| `frontend/StudentName_ClassCode_A01_FE.sln` | Solution Frontend |
| `frontend/FUNews.Client.DataAccess/` | Typed HttpClient adapters và transport models |
| `frontend/FUNews.Client.BusinessLogic/` | Client services và mapping kết quả API |
| `frontend/FUNews.Web/` | MVC Controllers, Views, ViewModels, wwwroot; Presentation |
| `tests/` | Unit/integration test projects, được thêm vào solution phù hợp |
| `database/patches/` | SQL patch có thứ tự |
| `docs/` | Tài liệu AI và evidence |

Mọi project target net8.0. Không tạo project Shared tham chiếu vòng giữa FE/BE. DTO wire phải có contract tests hoặc mapping rõ khi định nghĩa ở hai solution.

## Luồng request

Browser → MVC controller → Client service → typed API client → BE controller → BE service → Repository → DAO → Scoped DbContext → SQL Server.

FE cookie giữ danh tính tối thiểu; token JWT BE giữ trong session phía server và gắn vào outbound HttpClient cho từng request. Không đặt token ở localStorage hoặc biến JavaScript. Không gán token người dùng vào shared DefaultRequestHeaders có thể rò giữa session. Login/logout dùng antiforgery; khi BE trả 401, FE kết thúc phiên phù hợp. Không triển khai refresh token ở scope bài tập.

API public đọc tin không yêu cầu token. API ghi dùng JWT và role policy. BE là nơi quyết định quyền và trường audit; FE không được giả lập CreatedByID. Lỗi FE AJAX trả JSON/status, tránh redirect HTML login vào fetch mà UI không xử lý.

## DataAccess và nghiệp vụ

DAO thực hiện EF query/SaveChanges; Repository cung cấp interface nghiệp vụ dữ liệu. Service điều phối validation, quyền, transaction và quy tắc liên quan nhiều entity. Controller chỉ xử lý HTTP, gọi Service, map DTO/status.

Mọi DAO/Repository trong một request dùng chung context scoped khi tham gia một transaction. Thao tác news + tags lưu atomic; xóa NewsTag trước NewsArticle. Read-only dùng projection DTO và AsNoTracking khi thích hợp; không Include mọi navigation một cách máy móc.

OData chỉ áp dụng trên projection đọc an toàn sau visibility predicate và allowlist query. Query còn IQueryable không được enumerate trước phân trang khi không cần. Controller không được tự query DbContext để hỗ trợ OData. MaxTop=100, page default=10; không mở $expand/$select tùy ý hay property nhạy cảm.

## Cấu hình

ConnectionStrings:FUNewsManagement trong appsettings.json (giá trị local placeholder hợp lệ); Api:BaseUrl ở FE; DefaultAdmin:Email/Password cho tài khoản đề bài; Jwt:Issuer/Audience/SigningKey; App:TimeZone. Có file cấu hình mẫu, hướng dẫn user secrets/env override cho secret thật.

Admin local theo đề: admin@FUNewsManagementSystem.org / @@abc123@@. Đây là dữ liệu demo, không phải credential production. Admin identity dùng role Admin và subject riêng, không ép thành AccountID nhỏ.

Chọn package tương thích net8.0, EF Core 8.x và OData phù hợp; xác minh tài liệu/package thực tế khi cài đặt, ghi version đã dùng. Build bằng SDK hỗ trợ net8.0 và IDE phù hợp; không suy ra mọi Visual Studio 2019 đều chạy được .NET 8.

## Error handling

Validation → 400; chưa xác thực → 401; sai quyền → 403; resource không tồn tại hoặc public đọc Inactive → 404; unique/dependency conflict → 409. Lỗi bất ngờ → 500, thông báo chung và correlation ID. Không lộ stack trace, SQL, token hoặc hash. FE chuyển field errors vào modal và giữ input.
