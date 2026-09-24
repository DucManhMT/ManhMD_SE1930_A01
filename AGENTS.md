# FUNewsManagement repository instructions

## Phạm vi và thứ tự đọc

Triển khai bài PRN232 Assignment 01, .NET 8, ASP.NET Core Web API OData, ASP.NET Core MVC và SQL Server. Trước khi sửa, đọc README_AI.md, Requirements, Decisions, Architecture, Database_API_Contract, Design Standard và card task tương ứng. Kiểm tra hướng dẫn nằm gần folder định sửa.

Yêu cầu hiện tại của người dùng và đề bài là nguồn yêu cầu. SQL gốc là bằng chứng schema ban đầu; code/schema hiện có là bằng chứng trạng thái đã triển khai. Khi khác nhau, ghi rõ chênh lệch, không mặc nhiên coi code cũ là đúng yêu cầu. Các quyết định có nhãn tạm trong Decisions được dùng để tiếp tục công việc; không tự đổi chúng hoặc trình bày chúng là yêu cầu gốc.

## Quy tắc bắt buộc

- Hai solution BE/FE theo mẫu StudentName_ClassCode_A01_BE.sln và StudentName_ClassCode_A01_FE.sln; target net8.0. Không tự nâng .NET hoặc thay FE bằng SPA.
- BE: Controller → Service → Repository → DAO → DbContext. Không truy cập DB từ controller, FE hoặc JavaScript.
- Mỗi entity có repository interface/implementation. NewsTag cần thao tác quan hệ và composite key, không cần một màn hình CRUD riêng.
- DbContext Scoped, async xuyên suốt. Singleton chỉ dành cho cấu hình bất biến phù hợp. Không static DbContext và không giữ Scoped service trong Singleton.
- FE: Presentation → BusinessLogic → DataAccess API clients. Fetch gọi MVC endpoint; HttpClient của FE gọi BE. Không trộn thêm đường browser→BE cho cùng flow.
- Chỉ bind DTO/InputModel có allowlist; không nhận role, tác giả hay audit từ client khi task không cho phép. Không trả password/hash.
- Kiểm tra role/policy trên API, kể cả direct URL và OData. GET không ghi dữ liệu. Cookie mutation cần antiforgery.
- Khách/Lecturer chỉ đọc Active. Staff quản lý nội dung; Admin quản lý tài khoản/báo cáo theo Decisions.
- Thêm/sửa qua modal và cập nhật AJAX; không full reload sau save. Xóa luôn xác nhận; giữ form khi validation lỗi.
- Validate client và server, đúng chiều dài/nullability/schema. Rule uniqueness cần chống race tại database nếu có điều chỉnh schema được tài liệu hóa.
- Giao dịch ghi bài và NewsTag phải atomic. Chặn xóa account/category/tag đang được dùng.
- Giữ SQL gốc nguyên trạng. Schema bổ sung nằm trong database/patches, có thứ tự, kiểm tra trước khi chạy và cách xác minh. Không reset/drop DB có dữ liệu để sửa lỗi.
- Không sửa generated entities tùy tiện; dùng mapping/partial và quy trình scaffold được ghi lại. Không tự thêm bảng audit, upload, approval, scheduling, AI hoặc payment.
- Giữ thay đổi hợp lệ đang có. Không tạo module trùng, không reset git và không push/deploy nếu chưa được yêu cầu.
- Không log token, mật khẩu, connection string có secret. Tài khoản Admin mẫu chỉ phục vụ bài tập local; JWT secret đặt qua user secrets/env.

## Làm một task

1. Inspect file thật, dependencies, route, DTO, schema và UI tái sử dụng.
2. Nêu ngắn phạm vi và giả định; thực hiện tiếp với quyết định đã có.
3. Triển khai đầy đủ phần cần cho acceptance criteria. Không thêm chức năng chỉ để làm UI đẹp.
4. Chạy build, test nghiệp vụ/quyền và kiểm tra UI phù hợp rủi ro. Ghi chính xác lệnh và kết quả.
5. Cập nhật card: TODO / IN_PROGRESS / BLOCKED / CODE_COMPLETE_UNVERIFIED / DONE. DONE cần evidence cho tiêu chí bắt buộc.
6. Báo file đã đổi, hành vi, kiểm tra, giới hạn và bước tiếp theo. Không báo deploy/test thành công khi chưa chạy.

## Quy ước

C# PascalCase cho type/property, camelCase cho biến; async suffix cho method async. UI và giải thích tiếng Việt, identifier tiếng Anh. Không hardcode dữ liệu demo vào màn hình production. Không yêu cầu người dùng xác nhận lặp lại các lựa chọn đã ghi trong tài liệu; chỉ hỏi khi xung đột chưa có quyết định ảnh hưởng đáng kể hoặc cần hành động phá hủy dữ liệu.
