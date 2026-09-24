# FUNewsManagement AI Development Kit

Phiên bản 1.0 · 17/09/2026. Bộ hướng dẫn triển khai PRN232 Assignment 01 bằng AI, theo cách làm từng feature full-stack của Petlify.

## Bắt đầu

1. Giải nén, chép `AGENTS.md` và thư mục `docs/` vào root repository. Giữ `README_AI.md` để hướng dẫn AI; README chạy ứng dụng được tạo khi triển khai.
2. Chép hai nguồn gốc vào `docs/reference/PRN232Assignment01.docx` và `docs/reference/FUNewsManagement.sql`. Bộ này không chứa lại hai file nguồn.
3. Đọc các quyết định tạm trong `docs/architecture/FUNews_Decisions.md`. Chúng là phương án đề xuất, chưa phải xác nhận của giảng viên.
4. Điền tên sinh viên và mã lớp trong task FUN-001. Nếu chưa có, dùng placeholder và ghi rõ cần đổi tên trước khi nộp; không suy đoán mã lớp.
5. Mở repository trong Gemini Antigravity/Codex. Dán master prompt ở `docs/prompts/FUNews_Antigravity_Fullstack_Prompt.md`, chọn FUN-001.
6. Mỗi lần làm một task đủ dependencies; review bằng `docs/prompts/FUNews_Feature_Prompts.md`, rồi cập nhật trạng thái trong backlog.

## Danh mục tài liệu

| File | Vai trò |
|---|---|
| `AGENTS.md` | Luật làm việc và giới hạn kiến trúc |
| `docs/requirements/FUNews_Requirements.md` | Phạm vi, quyền, yêu cầu nghiệm thu |
| `docs/architecture/FUNews_Architecture.md` | Hai solution, tầng, xác thực, luồng dữ liệu |
| `docs/architecture/FUNews_Database_API_Contract.md` | Schema, DTO, API, OData, quy tắc dữ liệu |
| `docs/architecture/FUNews_Decisions.md` | Giải quyết điểm chưa thống nhất và giả định |
| `docs/design/FUNews_Web_Design_Standard.md` | Chuẩn UI và blueprint từng màn hình |
| `docs/tasks/FUNews_Tasks.md` | 22 task có thứ tự, đầu vào và tiêu chí nghiệm thu |
| `docs/prompts/FUNews_Antigravity_Fullstack_Prompt.md` | Prompt triển khai một feature từ UI đến database |
| `docs/prompts/FUNews_Feature_Prompts.md` | Prompt bootstrap, review, sửa lỗi và bàn giao |
| `docs/testing/FUNews_Test_Plan.md` | Kiểm thử quyền, nghiệp vụ, dữ liệu và demo |

## Quy tắc sử dụng

- Không bảo AI làm toàn bộ backlog một lần. Dùng ID task để duy trì phạm vi.
- Tất cả task ban đầu là TODO. Đây là tài liệu triển khai, chưa phải website đã code hoặc đã test.
- Một task nghiệp vụ phải hoàn chỉnh cả API và giao diện. Task nền tảng có đầu ra riêng theo card.
- Khi thiếu SQL Server hoặc trình duyệt, AI ghi UNVERIFIED/BLOCKED theo mức ảnh hưởng; không báo PASS giả.
- Thời gian ước lượng: 12–15 ngày làm việc cho một người, không phải cam kết deadline.
- Không mang cấu trúc Razor Pages/PostgreSQL hay module thú cưng của Petlify sang bài này.
