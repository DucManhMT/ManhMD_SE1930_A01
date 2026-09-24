# FUNewsManagement requirements

Nguồn: PRN232Assignment01.docx và FUNewsManagement.sql được cung cấp. Đây là diễn giải yêu cầu để triển khai; xem Decisions cho chỗ chưa thống nhất.

## Phạm vi và quyền

| Actor | Quyền mặc định của bộ tài liệu |
|---|---|
| Anonymous | Đọc/tìm tin Active, chi tiết, tag/category phục vụ đọc tin, related |
| Lecturer (2) | Đăng nhập, các quyền đọc như khách |
| Staff (1) | CRUD/search category, news, tag; hồ sơ bản thân; lịch sử tin mình tạo |
| Admin (cấu hình) | CRUD/search account; lọc role; báo cáo; xem editor cuối |

Không có public registration. Admin không có AccountID trong SystemAccount; không gán ID giả vào FK. Không suy ra Staff chỉ sửa bài mình: đề chỉ giới hạn lịch sử và hồ sơ về bản thân.

## Yêu cầu có mã

| Mã | Nội dung bắt buộc | Task |
|---|---|---|
| R01 | .NET 8, SQL Server, hai solution, 3 tầng, Repository + DAO + Singleton phù hợp | FUN-001–003 |
| R02 | Email/password; Admin trong appsettings.json; Staff/Lecturer trong DB | FUN-004 |
| R03 | Account CRUD/search name/email/role, email duy nhất, chặn xóa tác giả | FUN-005–006 |
| R04 | Hồ sơ bản thân; đổi password phải kiểm tra password hiện tại | FUN-007 |
| R05 | Category CRUD/search name/description, required tên/mô tả, unique cùng cha | FUN-008–009 |
| R06 | Category: số bài, active toggle, cấm đổi cha khi đã được dùng, chặn xóa khi có bài | FUN-008–009 |
| R07 | Tag CRUD/search, sửa Note, unique tên, chặn xóa tag được dùng, xem bài theo tag | FUN-010 |
| R08 | News CRUD/search title/author/category/status; lọc ngày; giảm dần theo ngày | FUN-011–013 |
| R09 | NewsTag nhiều–nhiều; thêm/gỡ tags; không trùng cặp; xóa liên kết khi xóa bài | FUN-012–013 |
| R10 | Duplicate Article với ID mới | FUN-014 |
| R11 | CreatedBy/CreatedDate tự gán; UpdatedBy/ModifiedDate tự gán | FUN-012–014 |
| R12 | Staff xem tin do mình tạo; Admin xem người sửa cuối; không tạo bảng audit | FUN-015,019 |
| R13 | Khách đọc Active không cần đăng nhập | FUN-016 |
| R14 | Tìm kiếm title/headline/content và category/tag/author/date; OData kết hợp | FUN-017 |
| R15 | Tối đa 3 bài liên quan cùng category hoặc tag, chỉ Active, loại bài hiện tại | FUN-018 |
| R16 | Report Admin theo khoảng ngày, nhóm category/author/status, tổng Active/Inactive | FUN-019 |
| R17 | Create/update modal, confirm delete, responsive, loading/toast, phân trang news/category | Mọi task UI |
| R18 | Validate client/server; HTTP error rõ; JSON; OData filter/orderby/top/skip | FUN-003 và mọi API |
| R19 | Test cả FE/BE, ít nhất 5 record ý nghĩa mỗi bảng, README/credentials/screenshots | FUN-020–021 |
| R20 | Export Excel | FUN-022, tùy chọn |

## Ngoài phạm vi

Không xây approval workflow, scheduling, upload ảnh, bình luận, AI, email reset password, refresh token, đăng ký công khai hoặc triển khai cloud tự động. Các chức năng này không phải deliverable bắt buộc chỉ vì được gợi nhắc trong phần giới thiệu.

## Điều kiện hoàn thành

Các R01–R19 có implementation và bằng chứng kiểm tra. FE hoạt động với API thật và SQL Server thật; không dùng mock để báo hoàn thành tích hợp. Ghi các phần chưa kiểm tra trong README. Không bắt buộc R20 trước khi nộp.
