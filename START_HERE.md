# FUNews A02 AI Implementation Kit — Khởi Động Nhanh

Bộ tài liệu hướng dẫn và thực thi cho **Assignment 02 — Distributed and Intelligent News Management System (PRN232)**.
Kiến trúc chuẩn theo đề bài: **4 Solutions phân tán độc lập (.NET 8, Web API, Razor Pages, EF Core, SQL Server)**.

---

## 1. Cách sử dụng bộ AI Kit này

1. Toàn bộ tài liệu chi tiết nằm tại thư mục `docs/a02/`.
2. Mọi quy tắc và ràng buộc kỹ thuật được định nghĩa tại `AGENTS.md`.
3. Khi bạn muốn AI thực hiện bất kỳ task nào, bạn chỉ cần copy prompt bên dưới và điền đúng mã Task ID (`A02-001` đến `A02-017`).

---

## 2. Master Prompt dành cho AI Agent

```text
Hãy đọc AGENTS.md, README_AI.md và docs/a02/prompts/MASTER_PROMPT.md.
Triển khai TASK ID: [ĐIỀN_TASK_ID_VÀO_ĐÂY, ví dụ: A02-001] theo hướng dẫn tại docs/a02/tasks/[ĐIỀN_TASK_ID_VÀO_ĐÂY].md.

Ràng buộc kiến trúc:
- Tuân thủ đúng kiến trúc hệ thống 4 Solutions theo đề bài PRN232 Assignment 02 (Core API, Analytics API, AI API, Frontend Razor Pages).
- Không áp dụng Clean Architecture phức tạp; áp dụng mô hình N-Tier (Controllers/Pages - Business Services - Data Access / HttpClients).
- Tái sử dụng tối đa mã nguồn hiện có từ Assignment 01 (ManhMD_SE1930_A01_BE và ManhMD_SE1930_A01_FE).
- Triển khai code hoàn chỉnh, kiểm thử thực tế và cập nhật tiến độ vào docs/a02/PROGRESS.md.
```

---

## 3. Danh mục tài liệu tham chiếu nhanh

| Tài liệu | Nội dung & Mục đích |
|---|---|
| [AGENTS.md](AGENTS.md) | Luật cốt lõi và các bất biến công nghệ của Assignment 02 |
| [README_AI.md](README_AI.md) | Thứ tự đọc và sơ đồ 17 tasks triển khai |
| [docs/a02/REQUIREMENTS.md](docs/a02/REQUIREMENTS.md) | 21 yêu cầu đề bài (R01 – R21), ma trận phân quyền và tính năng thừa kế |
| [docs/a02/ARCHITECTURE.md](docs/a02/ARCHITECTURE.md) | Thiết kế 4 Solutions độc lập và sơ đồ luồng dữ liệu phân tán |
| [docs/a02/API_CONTRACT.md](docs/a02/API_CONTRACT.md) | Đặc tả toàn bộ API Endpoints, DTOs, HTTP Status Codes |
| [docs/a02/CROSS_CUTTING.md](docs/a02/CROSS_CUTTING.md) | Giải pháp: JWT Refresh, Polly, HostedService 6h, Offline, SignalR |
| [docs/a02/DATABASE_MIGRATION.md](docs/a02/DATABASE_MIGRATION.md) | Kịch bản SQL mở rộng (AuditLog, NewsImage, NewsView, TagLearning) |
| [docs/a02/UI_STANDARD.md](docs/a02/UI_STANDARD.md) | Chuẩn 12 màn hình giao diện, Bootstrap Modals, Chart.js, Alerts |
| [docs/a02/TASKS.md](docs/a02/TASKS.md) | Danh sách 17 Task Cards có ước lượng và dependencies |
| [docs/a02/TEST_PLAN.md](docs/a02/TEST_PLAN.md) | Kế hoạch 24 kịch bản kiểm thử T01 – T24 |
| [docs/a02/GIT_COMMIT_CONVENTION.md](docs/a02/GIT_COMMIT_CONVENTION.md) | Quy chuẩn đặt tên Git Commit theo chuẩn Conventional Commits |
| [docs/a02/PROGRESS.md](docs/a02/PROGRESS.md) | Bảng theo dõi tiến độ hoàn thành các task |
| [docs/a02/RUNBOOK.md](docs/a02/RUNBOOK.md) | Hướng dẫn build, chạy demo và đóng gói bài nộp |
