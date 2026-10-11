# Master Prompt — Điều Phối Thực Hiện Từng Task (AI Coding Agent)

Dùng prompt này để chỉ đạo AI Agent thực hiện tuần tự từng task trong backlog Assignment 02:

```text
Hãy triển khai hoàn chỉnh task được giao trong repository FUNewsManagement hiện tại.
TASK ID: [ĐIỀN MÃ TASK, ví dụ: A02-001]

HƯỚNG DẪN ĐỌC TÀI LIỆU:
1. Đọc kỹ AGENTS.md, README_AI.md và docs/a02/tasks/TASK-ID.md tương ứng.
2. Đọc docs/a02/REQUIREMENTS.md, docs/a02/ARCHITECTURE.md, docs/a02/API_CONTRACT.md và docs/a02/CROSS_CUTTING.md theo phạm vi của task.
3. Khảo sát mã nguồn thực tế hiện tại trước khi thực hiện thay đổi.

MỤC TIÊU VÀ NGUYÊN TẮC:
- Nâng cấp từ Assignment 01 lên Assignment 02 trên nền tảng repository hiện có.
- Tuân thủ cấu trúc phân tán 4 Solutions theo đề bài: FUNewsManagement_CoreAPI, FUNewsManagement_AnalyticsAPI, FUNewsManagement_AIAPI, FUNewsManagement_FE.
- Áp dụng mô hình N-Tier truyền thống (Controllers/Pages - Business Logic Services - Data Access / HttpClients), KHÔNG áp dụng Clean Architecture phức tạp.
- Frontend giao tiếp hoàn toàn qua HttpClientFactory + DelegatingHandler, tuyệt đối không truy cập trực tiếp Database.
- Giữ nguyên các chức năng cốt lõi đã hoàn thành từ Assignment 01 (CRUD, OData, BCrypt, phân quyền Admin/Staff/Lecturer).
- Không dừng lại ở pseudocode hay class stub; phải triển khai code chạy được thực tế.

CÁC BƯỚC THỰC HIỆN:
1. Inspect: Kiểm tra mã nguồn hiện tại, xác định phần tái sử dụng và phần cần viết mới.
2. Implement: Viết code hoàn chỉnh cho task, tuân thủ đúng API contract và business rules.
3. Verify: Chạy build solution tương ứng và kiểm thử theo các Test IDs trong docs/a02/TEST_PLAN.md.
4. Report: Cập nhật trạng thái vào docs/a02/PROGRESS.md và báo cáo kết quả chi tiết cho người dùng.
```
