# Thiết Kế Kiến Trúc Hệ Thống Phân Tán — PRN232 Assignment 02

Kiến trúc hệ thống được thiết kế theo đúng yêu cầu đề bài: **Hệ thống phân tán 4 Solutions độc lập** (Distributed System), áp dụng mô hình N-Tier truyền thống quen thuộc trong môn học PRN232 (.NET 8, ASP.NET Core Web API, Razor Pages, Entity Framework Core).

---

## 1. Sơ đồ Kiến trúc Tổng thể (Distributed Architecture Diagram)

```
                               ┌────────────────────────────────────────┐
                               │         TRÌNH DUYỆT (BROWSER)          │
                               └────────────────────┬───────────────────┘
                                                    │
                                         HTTP / SignalR WebSocket
                                                    │
                                                    ▼
┌────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ 1. FUNewsManagement_FE.sln (Frontend - ASP.NET Core Razor Pages / MVC)                                  │
│                                                                                                        │
│  ├── Pages / Controllers     : Giao diện người dùng, Bootstrap 5 Modals, Chart.js Dashboard, Views     │
│  ├── Client Business Services: Quản lý nghiệp vụ phía Client, ViewModel, Xử lý Session State           │
│  ├── Typed HttpClients       :                                                                         │
│  │    ├── CoreApiClient      : Gọi Core API (Auth, CRUD, Media, Audit)                                 │
│  │    ├── AnalyticsApiClient : Gọi Analytics API (Dashboard, Trending, Recommend, Export Excel)        │
│  │    └── AiApiClient        : Gọi AI API (Suggest Tags, Learning Feedback)                            │
│  ├── Cross-Cutting Components:                                                                         │
│  │    ├── DelegatingHandler  : Tự động đính kèm Bearer Token, kiểm tra hạn token trước khi gửi         │
│  │    ├── Polly Retry Policy : Retry với exponential backoff cho các request đọc an toàn               │
│  │    ├── CacheWorker (HostedService): Chạy ngầm định kỳ 6h/lần lưu snapshot dữ liệu                   │
│  │    ├── OfflineStoreManager: Quản lý cache JSON cục bộ khi API offline                               │
│  │    └── NotificationHub    : Hub SignalR nhận thông báo bài viết mới thời gian thực                  │
└──────────────┬───────────────────────────────┬────────────────────────────────┬────────────────────────┘
               │                               │                                │
          HTTP REST                       HTTP REST                        HTTP REST
        (Core Client)                (Analytics Client)                   (AI Client)
               │                               │                                │
               ▼                               ▼                                ▼
┌──────────────────────────────┐┌──────────────────────────────┐┌──────────────────────────────┐
│ 2. FUNewsManagement_CoreAPI  ││ 3. FUNewsManagement_        ││ 4. FUNewsManagement_AIAPI    │
│    (Core Web API)            ││    AnalyticsAPI              ││    (AI Web API)              │
│                              ││    (Analytics Web API)       ││                              │
│ ├── Controllers:             ││ ├── Controllers:             ││ ├── Controllers:             │
│ │   • Auth (Login, Refresh)  ││   • Dashboard (Cat, Status)  ││   • Tag Suggestion           │
│ │   • News & Media Upload    ││   • Trending (Most-viewed)   ││   • Tag Feedback             │
│ │   • Category & Tag CRUD    ││   • Recommend (/recommend)  ││ ├── Services:                │
│ │   • Account & Profile      ││   • Export Excel             ││   • Keyword Extractor        │
│ │   • AuditLog API           ││ ├── Services:                ││     (Mô phỏng / OpenAI)      │
│ ├── Business Services:       ││   • AnalyticsService         ││   • LearningCacheManager     │
│ │   • TokenService (JWT/Ref) ││   • ExcelExportService       ││     (Bộ nhớ / JSON cache     │
│ │   • AuditService           ││   • CoreDataClient (HTTP)    ││      ghi nhận tần suất tag)  │
│ │   • NewsService & Rules    ││ └── DTOs & Export Models     ││ └── DTOs & AI Models         │
│ └── Data Access (EF Core):   │└──────────────────────────────┘└──────────────────────────────┘
│     • FUNewsDbContext        │
│     • DAOs & Repositories    │
│     • Sequences & Migrations │
└──────────────┬───────────────┘
               │
          ADO.NET / EF Core
               │
               ▼
┌──────────────────────────────┐
│   DATABASE (SQL SERVER)      │
│                              │
│ • SystemAccount              │
│ • Category                   │
│ • Tag                        │
│ • NewsArticle & NewsTag      │
│ • AuditLog                   │
│ • RefreshToken               │
│ • NewsView                   │
└──────────────────────────────┘
```

---

## 2. Chi tiết 4 Solutions độc lập

### Solution 1: `FUNewsManagement_CoreAPI.sln`
- **Vai trò:** Trung tâm dữ liệu và nghiệp vụ cốt lõi của toàn hệ thống.
- **Thành phần:**
  - `Controllers/`: Endpoints RESTful cho Auth (`/api/auth/login`, `/api/auth/refresh`), Account, Category, Tag, NewsArticle, Image Upload (`/api/news/{id}/image`), Audit Log (`/api/auditlog`).
  - `FUNews.BusinessLogic/`: Xử lý nghiệp vụ, băm mật khẩu BCrypt, sinh JWT Token & quản lý Refresh Token, ghi nhận Audit Log Before/After, kiểm tra ràng buộc không cho xóa dữ liệu có quan hệ phụ thuộc.
  - `FUNews.DataAccess/`: Entity Framework Core 8, DbContext có Scoped Lifetime, Mapping Fluent API, Repositories / DAOs.

### Solution 2: `FUNewsManagement_AnalyticsAPI.sln`
- **Vai trò:** Cung cấp số liệu thống kê, danh sách bài viết thịnh hành, gợi ý nội dung và xuất báo cáo.
- **Thành phần:**
  - `Controllers/`:
    - `GET /api/analytics/dashboard`: Tổng số bài viết theo category và status (hỗ trợ lọc theo khoảng ngày, category, author).
    - `GET /api/analytics/trending`: Danh sách bài viết được xem nhiều nhất.
    - `GET /api/recommend/{id}`: Gợi ý tối đa 3 bài viết liên quan theo Category hoặc Tag.
    - `GET /api/analytics/export`: Xuất file Excel báo cáo thống kê (`.xlsx`).
  - `Services/`:
    - `AnalyticsService`: Tính toán thống kê dữ liệu.
    - `ExcelExportService`: Sử dụng thư viện `ClosedXML` tạo workbook Excel chuẩn format.
    - `CoreDataClient`: Giao tiếp với Core API qua `HttpClient` để lấy dữ liệu tổng hợp.

### Solution 3: `FUNewsManagement_AIAPI.sln`
- **Vai trò:** Trích xuất từ khóa tự động và gợi ý Thẻ tin (Tags) thông minh cho bài viết.
- **Thành phần:**
  - `Controllers/`:
    - `POST /api/ai/suggest-tags`: Nhận input JSON `{"content": "..."}`, trả về danh sách tags gợi ý kèm điểm tin cậy (confidence score).
    - `POST /api/ai/tag-feedback`: Nhận phản hồi về các tag người dùng thực tế đã chọn gắn vào bài viết.
  - `Services/`:
    - `TagExtractorService`: Thuật toán bóc tách từ khóa (Keyword Extraction) xử lý tiếng Việt Unicode, loại bỏ stop-words, tính tần suất cụm từ; hỗ trợ cấu hình chuyển sang OpenAI API khi cần.
    - `LearningCacheService`: Lưu trữ và cập nhật trọng số các tag thường xuyên được chọn (Learning Cache) vào bộ nhớ / file JSON.

### Solution 4: `FUNewsManagement_FE.sln`
- **Vai trò:** Giao diện Web tương tác hoàn chỉnh cho người dùng (Khách, Giảng viên, Nhân viên, Quản trị viên).
- **Thành phần:**
  - `Pages/`: Giao diện Razor Pages + Bootstrap 5, Bootstrap Modals cho các form CRUD, Chart.js cho biểu đồ Dashboard, Badges/Chips cho gợi ý AI Tag.
  - `FUNews.Client.BusinessLogic/`: ViewModel, xử lý logic hiển thị, chuyển đổi trạng thái giao diện, kiểm soát quyền.
  - `FUNews.Client.DataAccess/`:
    - Quản lý 3 Typed HttpClients riêng biệt (`CoreApiClient`, `AnalyticsApiClient`, `AiApiClient`).
    - `AuthTokenDelegatingHandler`: Tự động gắn Bearer Token và chủ động gọi refresh token trước khi gửi request.
    - `PollyPolicies`: Chiến lược Retry Policy an toàn cho các thao tác đọc.
    - `CacheRefreshHostedService`: Background Worker (`IHostedService`) tự động cập nhật cache mỗi 6 giờ.
    - `OfflineModeService`: Phát hiện mất kết nối API, chuyển sang chế độ Offline đọc cache/JSON và khóa CRUD.
    - `NotificationHub`: Kết nối SignalR hiển thị thông báo bài viết mới real-time.

---

## 3. Quy tắc giao tiếp dữ liệu giữa các dịch vụ

1. **Tuyệt đối không chia sẻ kết nối Database:**
   - Chỉ duy nhất `FUNewsManagement_CoreAPI` có quyền kết nối trực tiếp đến SQL Server.
   - Frontend, Analytics API và AI API **hoàn toàn không chứa** chuỗi kết nối DB hay thư viện EF Core truy cập bảng dữ liệu gốc.
2. **Giao tiếp hoàn toàn qua HTTP REST & JSON:**
   - Mọi luồng đọc/ghi dữ liệu của Frontend đều đi qua các Typed HttpClients.
   - Header `Authorization: Bearer <token>` được tự động bổ sung bởi `DelegatingHandler`.
3. **Phân tách cổng mạng (Port Allocation) chuẩn khi chạy cục bộ:**
   - Frontend: `https://localhost:7000` (hoặc `http://localhost:5000`)
   - Core API: `https://localhost:7001` (hoặc `http://localhost:5001`)
   - Analytics API: `https://localhost:7002` (hoặc `http://localhost:5002`)
   - AI API: `https://localhost:7003` (hoặc `http://localhost:5003`)
