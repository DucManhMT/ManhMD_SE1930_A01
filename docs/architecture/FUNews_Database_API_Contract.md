# FUNews database and API contract

Các type và chiều dài dưới đây lấy từ SQL gốc. API required là quyết định nghiệp vụ và có thể chặt hơn nullability legacy. AI phải kiểm tra DB thực tế trước khi scaffold.

## Schema

| Bảng | Khóa và trường chính |
|---|---|
| Category | CategoryID smallint identity; CategoryName nvarchar(100) NOT NULL; CategoryDesciption nvarchar(250) NOT NULL; ParentCategoryID smallint NULL; IsActive bit NULL |
| SystemAccount | AccountID smallint không identity; AccountName nvarchar(100), AccountEmail nvarchar(70), AccountRole int, AccountPassword nvarchar(70): nullable trong SQL gốc |
| NewsArticle | NewsArticleID nvarchar(20); NewsTitle nvarchar(400) NULL; Headline nvarchar(150) NOT NULL; NewsContent nvarchar(4000), NewsSource nvarchar(400) NULL; CategoryID/CreatedByID/UpdatedByID smallint NULL; NewsStatus bit NULL; CreatedDate/ModifiedDate datetime NULL |
| Tag | TagID int không identity; TagName nvarchar(50), Note nvarchar(400) NULL |
| NewsTag | PK kép NewsArticleID nvarchar(20) + TagID int; FK đến NewsArticle và Tag |

Map CategoryDescription trong C# về cột CategoryDesciption. Không tự đổi tên cột trong script gốc. Quan hệ account-author và category-news đang cascade delete, cần xử lý theo Decisions. NewsTag FK không cascade, xóa liên kết tường minh.

## Input validation

- AccountCreate: name required ≤100, email required hợp lệ ≤70, role chỉ 1/2, password required. Chính sách độ mạnh password chưa được đề định lượng: trước khi thêm giới hạn phải ghi quyết định; không âm thầm từ chối seed. AccountUpdate không nhận AccountID, hash hay mật khẩu mới.
- ChangePassword: currentPassword, newPassword, confirmPassword; kiểm tra password hiện tại và confirmation. Đổi mật khẩu bản thân dùng endpoint me; Admin đổi mật khẩu tài khoản khác nếu có UI thì cũng phải xác minh currentPassword của tài khoản đó, không có reset bypass.
- Category: name required ≤100, description required ≤250, parentId nullable tồn tại và không tạo cycle, isActive bool required. Cấm đổi parent khi có bài; unique tên đã trim trong cùng parent, kể cả NULL.
- Tag: name required ≤50, note optional ≤400; trim và so trùng không phân biệt hoa thường theo chính sách collation đã chốt.
- News: title required ≤400, headline required ≤150, content required ≤4000, source optional ≤400, categoryId required tồn tại, newsStatus bool required, tagIds tập hợp distinct, mỗi ID tồn tại. Không nhận audit fields từ client. Source có thể là mô tả như Internet/N/A, không ép URL.
- Nội dung news required là mặc định của bộ kit cho dữ liệu mới; dữ liệu legacy NULL phải đọc an toàn. NewsStatus NULL không phải Active, thống kê riêng Unknown nếu gặp thay vì gán sai vào Inactive.
- Validate length sau chuẩn hóa, không silently truncate. ID ngoài range trả 400. Gặp bản ghi liên quan được tạo đồng thời trước delete phải map FK conflict thành 409.

## Endpoint contract

Base `/api`; DTO JSON camelCase. Các URL dưới đây là contract dự kiến, phải có integration test khi cấu hình OData route conventions.

| Method | Route | Quyền và hành vi |
|---|---|---|
| POST | /auth/login | Public; email/password; trả token, expiry, safe identity |
| GET/POST | /account | Admin; list OData / create |
| GET/PUT/DELETE | /account/{id} | Admin; metadata CRUD; chặn xóa CreatedBy hoặc UpdatedBy đang dùng |
| GET/PUT | /account/me | Staff; profile bản thân, không đổi role |
| POST | /account/me/change-password | Staff; verify current password |
| POST | /account/{id}/change-password | Admin; verify current password của target; không bắt buộc có nút nếu chưa dùng |
| GET/POST | /category | GET metadata public; POST Staff |
| GET/PUT/DELETE | /category/{id} | GET metadata public; ghi Staff; gồm isActive trong PUT |
| GET/POST | /tag | GET public; POST Staff |
| GET/PUT/DELETE | /tag/{id} | GET public; ghi Staff |
| GET | /tag/{id}/news | Khách/Lecturer chỉ Active; Staff có quyền quản lý |
| GET/POST | /news | GET role-scoped; POST Staff |
| GET/PUT/DELETE | /news/{id} | GET role-scoped; ghi Staff |
| GET | /news/mine | Staff; CreatedByID từ token; đặt literal route trước/khác ID route |
| POST | /news/{id}/duplicate | Staff; bản sao Inactive và audit mới |
| GET | /news/{id}/related | Public; tối đa 3 Active, bỏ chính bài và trùng |
| GET | /report | Admin; startDate/endDate, groupBy category/author/status |
| GET | /report/export | Admin; chỉ nếu FUN-022 được chọn |

Public category/tag response không lộ số bài Inactive. Số bài toàn bộ category chỉ dành cho Staff; anonymous trả số Active hoặc DTO metadata riêng. DTO report chứa tác giả/editor cuối an toàn, không password.

## OData và response

GET collections dùng envelope OData gồm `value`, `@odata.count` khi count được yêu cầu; detail trả DTO. POST trả 201 + Location; PUT trả DTO 200; DELETE 204. Lỗi dùng ProblemDetails với field errors nếu có. FE không tự giả định response là array.

NewsListDto có newsArticleId, newsTitle, headline, categoryId, categoryName, authorId, authorName, newsStatus, createdDate và tags an toàn khi cần. Search tag dùng điều kiện collection được allowlist hoặc query adapter đã test; không mở navigation EF tùy ý.

Ví dụ trên projection camelCase:
`/api/news?$filter=categoryId eq 2 and newsStatus eq true and contains(newsTitle,'AI')&$orderby=createdDate desc,newsArticleId desc&$top=10&$skip=0&$count=true`

FE encode query đúng chuẩn, escape literal OData và không ghép SQL. Allowlist property/operator, giới hạn độ phức tạp, MaxTop 100, chặn $expand. Visibility predicate chạy trước mọi filter/count; count không được lộ tổng Inactive. Stable sort theo ngày rồi ID. Account query không truy cập password/hash kể cả qua $filter/$orderby.

## Quy tắc thời gian và báo cáo

Datetime SQL lưu local time Việt Nam theo đề, không trộn UTC với local khi ghi. API serialize nhất quán có offset +07:00; map date boundary rõ trong query. StartDate/EndDate là ngày local: CreatedDate >= StartDate 00:00 và < ngày sau EndDate 00:00. Chặn StartDate > EndDate.

Report gồm totals (active/inactive/unknown nếu legacy có), groups (key/label/count), details có createdDate, authorName, lastEditorName, modifiedDate. Chi tiết sắp ngày giảm dần, phân trang nếu lớn; totals/groups tính trên toàn bộ tập lọc, không chỉ một trang. LEFT JOIN cho trường nullable/legacy và ghi rõ Chưa ghi nhận.

## Dữ liệu demo

SQL gốc có 5 category, 5 account, 5 news, 9 tag và 18 liên kết. Bổ sung bản ghi nhiều ngày, Active/Inactive và Staff tác giả đúng role. Giữ tối thiểu 5 bản ghi có ý nghĩa mỗi bảng sau điều chỉnh. Không dùng credential thật. Seed hash có quy trình lặp an toàn; không hash lại hash hoặc overwrite mật khẩu đã đổi khi chạy lại seed.
