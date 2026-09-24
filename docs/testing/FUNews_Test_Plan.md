# FUNews verification plan

## Môi trường và evidence

Dùng SQL Server dev/test tách biệt, seed có ít nhất 5 record mỗi bảng. Tạo hai Staff, một Lecturer, Admin cấu hình, bài Active/Inactive và nhiều ngày. Seed SQL có Lecturer tác giả cũ không được dùng để suy luận quyền tạo bài hiện tại. Test transaction phải dùng provider SQL Server hoặc môi trường tương đương về constraint; EF InMemory không chứng minh FK/transaction.

Mỗi kết quả ghi task, commit nếu có, command, môi trường, expected/actual, PASS/FAIL/UNVERIFIED. Screenshot phải từ ứng dụng đang chạy. Không lưu secret/token trong evidence.

## Test cases

| ID | Thao tác | Kỳ vọng |
|---|---|---|
| T01 | Login Admin cấu hình, Staff, Lecturer; password sai | Role đúng; sai trả lỗi chung, không lộ account tồn tại |
| T02 | Lecturer gọi POST news; Staff gọi account/report | 403; không thay đổi DB |
| T03 | Khách truy cập bài Inactive bằng ID/filter/related/count | Không lộ nội dung hoặc số lượng Inactive |
| T04 | FE mutation không antiforgery, API không JWT | Bị từ chối; không ghi |
| T05 | Add/update email trùng khác hoa/thường; 2 request đồng thời | Một bản ghi duy nhất; lỗi 409/field error rõ |
| T06 | Delete account có CreatedBy; account chỉ có UpdatedBy | Chặn theo Decisions; news/audit còn nguyên |
| T07 | Đổi password sai current; đúng current | Sai không đổi; đúng login bằng password mới, không trả hash |
| T08 | Staff sửa profile có payload role/AccountID người khác | Không nâng quyền hoặc sửa sai tài khoản |
| T09 | Category name trùng cùng parent, khác parent | Cùng parent bị chặn; khác parent được phép |
| T10 | Parent=self/cycle; đổi parent category có bài | Bị chặn; dữ liệu không đổi |
| T11 | Xóa category có bài/con; xóa category trống | Chặn khi liên kết; trống xóa được; không cascade news |
| T12 | Duplicate tag name; delete referenced tag | Chặn; Note update hoạt động |
| T13 | Tạo news, client giả CreatedBy/CreatedDate | Server gán đúng user và local time |
| T14 | Tạo/sửa news có một TagID không hợp lệ | Toàn bộ transaction rollback |
| T15 | Edit thêm/gỡ tag, trùng tagIds | Bộ tag đúng, không duplicate composite key |
| T16 | Delete news | NewsTag tương ứng xóa, Tag dùng chung còn nguyên |
| T17 | Duplicate article | ID mới, Inactive, audit mới, nội dung/tag đúng, bản gốc không đổi |
| T18 | Staff A vào /mine; giả filter CreatedBy Staff B | Chỉ dữ liệu Staff A hoặc không kết quả |
| T19 | Update news bởi Staff khác | UpdatedBy/ModifiedDate đúng, CreatedBy/Date không đổi |
| T20 | Search kết hợp keyword/category/tag/author/date + paging | Kết quả đúng, sort ổn định, count đúng toàn tập |
| T21 | OData top quá lớn, property password, expand tùy ý | Từ chối hoặc giới hạn theo contract, không lộ dữ liệu |
| T22 | Related cùng category hoặc tag, trùng cả hai | Tối đa 3 duy nhất, Active, loại chính bài |
| T23 | Report ngày đầu/cuối, end<start, tập rỗng | Ngày cuối được tính hết; invalid 400; rỗng hiện 0 hợp lệ |
| T24 | Report group + totals + paged details | Aggregate toàn tập khớp DB, details desc, last editor đúng |
| T25 | Title/content dài, null legacy, chuỗi HTML/script | Length validation; không crash; encode, không thực thi script |
| T26 | Modal invalid/network error/double click | Input giữ, lỗi rõ, loading kết thúc, không báo success giả |
| T27 | Keyboard/focus và 390/768/1024/1440px | Thao tác được, không mất CTA hoặc tràn toàn trang |
| T28 | Hai session Staff đồng thời | Token/identity không rò chéo qua HttpClient |
| T29 | Restart và setup mới theo README | BE/FE kết nối được, seed lặp không phá dữ liệu |
| T30 | Export nếu triển khai | Chỉ Admin; dữ liệu cùng filter; bảo vệ công thức spreadsheet từ text người dùng |

## Demo 8 bước

1. Khách đọc tin Active và tìm kiếm.
2. Lecturer đăng nhập, thử truy cập quản trị và bị chặn.
3. Admin tạo/sửa/lọc account; thử xóa tác giả có bài.
4. Staff tạo category/tag bằng modal; minh họa validate trùng.
5. Staff tạo bài nhiều tag, sửa tag và thử duplicate.
6. Minh họa chặn xóa category/tag đang dùng; xóa bài và xác minh liên kết.
7. Staff xem lịch sử/hồ sơ; Admin xem report theo ngày và người sửa cuối.
8. Chạy kiểm tra theo README, trình bày kiến trúc, patch và evidence.

## Gate nộp bài

Build hai solution PASS; critical tests quyền/transaction/FK PASS; UI CRUD/search/filter chạy thật; tối thiểu 5 record mỗi bảng; SQL/setup/README/credential demo/screenshots đầy đủ. Export là tùy chọn. Nếu không chạy được SQL/UI, ghi UNVERIFIED và không đánh task liên quan DONE.
