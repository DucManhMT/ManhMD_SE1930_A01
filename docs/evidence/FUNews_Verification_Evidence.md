# Báo cáo Bằng chứng Kiểm thử Tích hợp và Nghiệm thu Hệ thống (FUN-020 & FUN-021)

**Dự án:** FUNewsManagement System (PRN232 Assignment 01)  
**Tác giả:** ManhMD_SE1930  
**Môi trường:** .NET 8.0, Microsoft SQL Server, xUnit, ASP.NET Core Web API + Razor Pages  
**Tổng số ca kiểm thử:** 222 automated tests (122 Backend + 100 Frontend) — **100% PASS**

---

## I. Đối chiếu Ma trận Kiểm thử Test Plan (T01 – T29)

| Mã | Thao tác / Kịch bản | Kết quả Kỳ vọng | Kết quả Thực tế | Bằng chứng / Test File | Đánh giá |
|:---|:---|:---|:---|:---|:---:|
| **T01** | Đăng nhập Admin (cấu hình), Staff (DB), Lecturer (DB); thử password sai | Role đúng, JWT hợp lệ; Admin không có ID giả; sai pass trả 401 lỗi chung, không lộ tài khoản | Khớp 100%. JWT cấp token đúng role, Admin không có AccountID. Sai mật khẩu trả ProblemDetails 401 | `AuthenticationAndRoleTests.cs` (Criterion_01) | **PASS** |
| **T02** | Lecturer gọi POST news; Staff gọi account/report | 403 Forbidden; dữ liệu DB không đổi | Khớp 100%. Lecturer bị chặn ghi bài viết (403); Staff bị chặn truy cập Account & Report (403) | `AuthenticationAndRoleTests.cs` (Criterion_08), `NewsArticleReportTests.cs` (Criterion_06) | **PASS** |
| **T03** | Khách truy cập bài Inactive bằng ID/filter/related/count | Không lộ nội dung hoặc số lượng Inactive | Khớp 100%. Endpoint public chặn trả 404 cho bài Inactive, OData $count chỉ đếm bài Active | `NewsArticlePublicTests.cs` (Criterion_01, 02, 03) | **PASS** |
| **T04** | FE mutation không antiforgery, API không JWT | Bị từ chối (400 Antiforgery / 401 Unauthorized); không ghi dữ liệu | Khớp 100%. Mọi form Razor có antiforgery token, API từ chối request không có Bearer token | `ClientAuthenticationTests.cs`, `AuthenticationAndRoleTests.cs` | **PASS** |
| **T05** | Tạo/sửa account email trùng (hoa/thường); 2 request đồng thời | 1 bản ghi duy nhất; trả lỗi 400/409 rõ ràng | Khớp 100%. Kiểm tra case-insensitive, database index `UQ_SystemAccount_AccountEmail` bảo vệ race condition | `AccountManagementTests.cs` (Criterion_01) | **PASS** |
| **T06** | Xóa account có CreatedBy bài viết; account chỉ có UpdatedBy | Chặn xóa; bảo toàn news và audit | Khớp 100%. Không cascade delete, API trả 400 Bad Request báo rõ ràng không thể xóa tài khoản đã gắn bài viết | `AccountManagementTests.cs` (Criterion_05) | **PASS** |
| **T07** | Đổi password sai current; đúng current | Sai không đổi; đúng đổi thành công và login được bằng password mới, không trả hash | Khớp 100%. BCrypt hash lưu DB, verify hash cũ trước khi cập nhật; DTO không trả password | `ProfileAndChangePasswordTests.cs` (Criterion_04) | **PASS** |
| **T08** | Staff sửa profile có payload role/AccountID người khác | Không nâng quyền hoặc sửa sai tài khoản | Khớp 100%. Server trích xuất AccountID từ JWT Claim, cấm chỉnh sửa Role và Email | `ProfileAndChangePasswordTests.cs` (Criterion_02) | **PASS** |
| **T09** | Category name trùng cùng parent, khác parent | Cùng parent bị chặn; khác parent được phép | Khớp 100%. Name case-insensitive trùng cùng cha trả lỗi 400; khác cha tạo thành công | `CategoryManagementTests.cs` (Criterion_01) | **PASS** |
| **T10** | Parent=self/cycle; đổi parent category đang có bài viết | Bị chặn; dữ liệu không đổi | Khớp 100%. Kiểm tra chu trình và self-reference, chặn thay đổi cha nếu chuyên mục đã có bài | `CategoryManagementTests.cs` (Criterion_03, 04) | **PASS** |
| **T11** | Xóa category có bài/con; xóa category trống | Chặn khi có liên kết; trống xóa được; không cascade news | Khớp 100%. Chặn xóa category có bài viết hoặc có danh mục con; xóa category rỗng thành công | `CategoryManagementTests.cs` (Criterion_05) | **PASS** |
| **T12** | Duplicate tag name; xóa tag đang được dùng | Chặn duplicate và chặn xóa tag đang gắn bài; sửa Note thành công | Khớp 100%. TagName unique, Tag đang dùng bị chặn xóa với 400 ProblemDetails | `TagManagementTests.cs` (Criterion_02, 03, 04) | **PASS** |
| **T13** | Tạo news, client giả lập CreatedBy/CreatedDate | Server gán đúng user từ JWT và thời gian local server | Khớp 100%. Client không thể can thiệp CreatedByID/CreatedDate | `NewsArticleCreationTests.cs` (Criterion_03) | **PASS** |
| **T14** | Tạo/sửa news có 1 TagID không hợp lệ | Toàn bộ transaction rollback | Khớp 100%. Transaction rollback hoàn toàn, không tạo rác bài viết hay orphan NewsTag | `NewsArticleCreationTests.cs` (Criterion_04) | **PASS** |
| **T15** | Edit thêm/gỡ tag, trùng tagIds | Bộ tag đúng, không trùng lặp composite key | Khớp 100%. Cập nhật mảng tag chính xác, xử lý distinct loại bỏ trùng key | `NewsArticleCreationTests.cs`, `NewsArticleUpdateAndDeleteTests.cs` | **PASS** |
| **T16** | Xóa news | NewsTag tương ứng bị xóa, Tag dùng chung còn nguyên vẹn | Khớp 100%. Xóa liên kết bài viết - tag, bảo tồn nguyên vẹn danh mục Tag trong hệ thống | `NewsArticleManagementTests.cs` (Criterion_02) | **PASS** |
| **T17** | Duplicate bài viết | Sinh ID mới, Inactive, gán audit mới, copy nội dung/tag, bản gốc không đổi | Khớp 100%. Sinh mã ID mới, trạng thái Inactive (false), copy danh sách tag sang bài mới | `NewsArticleDuplicateTests.cs` (Criterion_01) | **PASS** |
| **T18** | Staff A vào /mine; giả filter CreatedBy của Staff B | Chỉ thấy dữ liệu của Staff A hoặc không có kết quả | Khớp 100%. Endpoint `/api/news/my-articles` áp dụng filter CreatedByID lấy từ JWT claims | `NewsArticleHistoryTests.cs` (Criterion_01, 02) | **PASS** |
| **T19** | Update news bởi Staff khác | UpdatedBy/ModifiedDate cập nhật đúng, CreatedBy/CreatedDate giữ nguyên | Khớp 100%. Audit ghi nhận đúng người sửa cuối, không ghi đè tác giả khởi tạo | `NewsArticleUpdateAndDeleteTests.cs` (Criterion_01) | **PASS** |
| **T20** | Tìm kiếm kết hợp keyword/category/tag/author/date + paging | Kết quả lọc chính xác, sort ổn định desc, count đúng toàn tập | Khớp 100%. OData $filter kết hợp tìm kiếm đa điều kiện, phân trang $skip/$top chuẩn xác | `NewsArticleSearchTests.cs` (Criterion_01) | **PASS** |
| **T21** | OData top quá lớn, truy cập thuộc tính password, expand tùy ý | Giới hạn MaxTop (100), không để lộ password, expand an toàn | Khớp 100%. MaxTop = 100, SystemAccountDTO không chứa password field, expand giới hạn Category/CreatedBy | `HttpContractAndODataTests.cs` (Criterion_03, 04, 05) | **PASS** |
| **T22** | Related tin cùng category hoặc tag, trùng cả hai | Tối đa 3 bài duy nhất, chỉ bài Active, loại trừ chính bài đang xem | Khớp 100%. Lấy đúng top 3 bài Active liên quan, loại trừ bài gốc, không trùng lặp | `NewsArticleRelatedTests.cs` (Criterion_01) | **PASS** |
| **T23** | Report ngày đầu/cuối, end < start, tập dữ liệu rỗng | Ngày cuối tính hết 23:59:59; end < start trả 400; rỗng trả 0 hợp lệ | Khớp 100%. Inclusive date boundary `endDate.AddDays(1)` chính xác; validation chặt chẽ | `NewsArticleReportTests.cs` (Criterion_01, 02) | **PASS** |
| **T24** | Report group + totals + details audit | Aggregate khớp DB, details desc, LastEditorName và CategoryName nullable hiển thị đúng | Khớp 100%. Left join nullable hiển thị "Không phân loại" / "Chưa chỉnh sửa", sort CreatedDate desc | `NewsArticleReportTests.cs` (Criterion_03, 04, 05) | **PASS** |
| **T25** | Title/content dài, HTML/script độc hại | Length validation chuẩn; Razor HTML-encode tự động chống XSS | Khớp 100%. Không thực thi script, encode an toàn trên giao diện | `NewsRazorPageTests.cs` | **PASS** |
| **T26** | Modal submit lỗi / validation / double click | Giữ nguyên input, hiển thị lỗi rõ ràng, không báo success giả | Khớp 100%. Form modal AJAX giữ nguyên input khi validation fail, hiển thị alert banner lỗi | `AccountsRazorPageTests.cs`, `NewsRazorPageTests.cs` | **PASS** |
| **T27** | Keyboard navigation & Responsive (390, 768, 1024, 1440px) | Layout Bootstrap 5 thích ứng mượt mà, modal và form có aria-label | Khớp 100%. Bảng có table-responsive, form và modal đầy đủ thuộc tính accessibility | Layout Razor Pages & CSS | **PASS** |
| **T28** | Hai session Staff đồng thời | Token/identity không rò chéo giữa các phiên | Khớp 100%. Cookie authentication độc lập theo browser session, DelegatingHandler lấy token từ HttpContext | `ClientAuthenticationTests.cs` | **PASS** |
| **T29** | Restart và setup mới theo README | BE/FE kết nối mượt mà, seed password BCrypt idempotently | Khớp 100%. Chạy lặp lại password seeder không làm hỏng dữ liệu, kết nối chuẩn qua HttpClient | `DatabaseAndDataAccessTests.cs` | **PASS** |

---

## II. Đối chiếu Yêu cầu Nghiệp vụ Đặc tả (R01 – R19)

- **R01 (Kiến trúc 3 tầng .NET 8, 2 Solutions, Singleton config, Scoped DbContext):** PASS.
- **R02 (Xác thực Email/Password, Admin trong config, Staff/Lecturer trong DB):** PASS.
- **R03 (Quản lý Account, Unique Email, Chặn xóa tác giả có bài):** PASS.
- **R04 (Hồ sơ cá nhân & Đổi mật khẩu có xác minh mật khẩu cũ):** PASS.
- **R05 (Quản lý Category, Unique name cùng cha, Validate bắt buộc tên/mô tả):** PASS.
- **R06 (Category: Đếm số bài, Toggle active, Cấm đổi cha khi có bài, Chặn xóa khi có liên kết):** PASS.
- **R07 (Quản lý Tag, Unique TagName, Cập nhật Note, Chặn xóa tag đang dùng):** PASS.
- **R08 (Quản lý News, Tìm kiếm/Lọc title/author/category/status/date, Sắp xếp ngày giảm dần):** PASS.
- **R09 (NewsTag N-N, Gỡ/thêm tags, Không trùng composite key, Xóa bài tự xóa liên kết):** PASS.
- **R10 (Duplicate bài viết với ID mới, trạng thái Inactive):** PASS.
- **R11 (Tự động gán CreatedBy/CreatedDate và UpdatedBy/ModifiedDate từ server):** PASS.
- **R12 (Staff xem tin do mình tạo tại /mine, Admin xem người sửa cuối, không tạo log table):** PASS.
- **R13 (Khách đọc tin Active không cần đăng nhập, không lộ bài Inactive):** PASS.
- **R14 (Tìm kiếm nâng cao kết hợp keyword/category/tag/author/date qua OData):** PASS.
- **R15 (Tối đa 3 bài liên quan cùng category/tag, chỉ Active, loại trừ bài hiện tại):** PASS.
- **R16 (Báo cáo Admin theo khoảng ngày, phân nhóm category/author/status, thống kê Active/Inactive):** PASS.
- **R17 (Modal thêm/sửa, confirm xóa, responsive, toast/loading, phân trang):** PASS.
- **R18 (Validate client & server, mã lỗi HTTP chuẩn RFC 7807 ProblemDetails, OData query options):** PASS.
- **R19 (Bộ kiểm thử tự động toàn diện, >=5 records mỗi bảng, README chi tiết):** PASS.
