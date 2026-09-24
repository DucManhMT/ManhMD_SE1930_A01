# FUNews implementation decisions

Các quyết định dưới đây cho phép AI tiếp tục triển khai mà không hỏi lại từng lựa chọn. Chúng chưa phải xác nhận của giảng viên. Nếu người dùng hoặc giảng viên thay đổi, cập nhật tài liệu và test liên quan.

| ID | Điểm cần quyết định | Phương án mặc định và lý do |
|---|---|---|
| D01 | Admin full control so với Staff Only | Theo quyền chi tiết: Admin account/report; Staff category/news/tag. Không cho Admin tạo bài vì không có AccountID. Nếu cần full control phải thiết kế audit/FK trước |
| D02 | Tag có mô tả một bài so với junction table | Nhiều–nhiều theo NewsTag và mục quản lý liên kết |
| D03 | Singleton | Configuration provider bất biến; DbContext Scoped |
| D04 | FE 3 tầng | DataAccess FE là HTTP API adapters, không kết nối SQL |
| D05 | MVC hay Razor Pages | Razor Pages + Bootstrap 5 theo yêu cầu người dùng; bảo đảm 3 tầng FE: PageModel -> Client.BusinessLogic -> Client.DataAccess -> BE API; AJAX modal dùng named handlers |
| D06 | Tag management không ghi actor | Staff ghi; public/Lecturer chỉ metadata và bài Active dùng tag |
| D07 | Staff ownership | Staff sửa/xóa mọi bài theo quyền quản lý; /mine và profile chỉ bản thân |
| D08 | Category inactive ảnh hưởng tin | Public lọc NewsStatus=true; không tự ẩn bài Active chỉ vì category inactive. Tạo/đổi category chỉ chọn active; sửa bài cũ được giữ category inactive hiện tại |
| D09 | Password update | Tách endpoint đổi mật khẩu và kiểm tra mật khẩu hiện tại của tài khoản bị đổi. Không tạo admin reset bỏ qua xác minh; Admin chỉ sửa metadata qua form thường |
| D10 | Xóa category có category con | Chặn và trả lỗi dễ hiểu; không xóa dây chuyền |
| D11 | Account chỉ xuất hiện UpdatedBy | Cũng chặn xóa để giữ audit, dù điều kiện tối thiểu của đề chỉ nói CreatedBy |
| D12 | Duplicate | Sao chép trường nội dung/category/tags; ID mới; Inactive; tác giả hiện tại; ngày mới; UpdatedBy/ModifiedDate=NULL |
| D13 | Múi giờ | Asia/Ho_Chi_Minh, local datetime theo đề; dịch vụ clock hỗ trợ timezone Linux/Windows |
| D14 | Đề yêu cầu chỉnh schema | Giữ SQL gốc; dùng patch trên DB dev/test, ghi tác động; không tự chạy trên DB có dữ liệu thực |
| D15 | Password seed plaintext | Chuyển seed demo sang hash bằng bước provisioning rõ ràng; không giữ fallback plaintext khi đăng nhập |
| D16 | Global search role | Public/Lecturer chỉ Active; quản lý Staff được lọc cả hai trạng thái; report Admin được xem cả hai |
| D17 | Nội dung bài viết | Plain text bảo toàn xuống dòng, encode output. Chưa thêm rich text/HTML editor |
| D18 | Report sorting | Aggregate theo category/author/status; bảng chi tiết riêng sort CreatedDate desc, ID phụ. Không sort nhóm bằng trường ngày không có trong nhóm |

## Schema patch đề xuất

- Sửa các parent tự tham chiếu của seed về NULL sau khi kiểm tra dữ liệu.
- Thay cascade xóa từ account/category sang NewsArticle bằng NO ACTION.
- Chuẩn hóa email/tag name và thêm unique index; category unique theo parent và tên, bao gồm nhóm parent NULL. Preflight tìm trùng trước khi thêm index.
- Mở AccountPassword đủ chứa hash do thư viện đã chọn tạo ra, ví dụ nvarchar(512); hash sinh bằng code, không giả lập trong SQL.
- Dùng SQL sequence cho AccountID và TagID vì không identity; khởi tạo lớn hơn ID hiện có, giới hạn smallint với AccountID. Không dùng MAX+1 ở mỗi request.
- NewsArticleID dùng sequence riêng định dạng N + số thứ tự, tối đa 20 ký tự; không cắt GUID và giả định sẽ luôn duy nhất.
- Chưa bắt buộc thêm FK UpdatedByID; Service validate và dùng left join khi đọc dữ liệu legacy.

## Cách ghi thay đổi quyết định

Mỗi thay đổi có ngày, lý do, nguồn yêu cầu, file/code/test bị ảnh hưởng. Không dùng Decisions để hạ yêu cầu bắt buộc hoặc hợp thức hóa test đang fail.
