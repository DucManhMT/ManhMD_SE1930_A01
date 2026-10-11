# Các Quyết Định Kỹ Thuật (Architecture & Technical Decisions) — PRN232 A02

Tài liệu ghi lại các quyết định thiết kế mặc định cho hệ thống, đảm bảo thống nhất giữa các thành viên và công cụ AI trong suốt quá trình phát triển.

---

## 1. Quyết định về Kiến trúc & Công nghệ (Architecture Decisions)

- **D01: Kiến trúc 4 Solutions độc lập:**
  - Không gộp chung tất cả vào 1 solution lớn mà tạo đúng 4 solution riêng theo mục 6 đề bài:
    - `FUNewsManagement_CoreAPI.sln`
    - `FUNewsManagement_AnalyticsAPI.sln`
    - `FUNewsManagement_AIAPI.sln`
    - `FUNewsManagement_FE.sln`
- **D02: Không áp dụng Clean Architecture phức tạp:**
  - Áp dụng kiến trúc N-Tier truyền thống (Controllers/Pages - Business Logic Services - Data Access / HttpClients) quen thuộc của môn học PRN232, giúp mã nguồn tường minh, dễ chấm điểm và đúng chuẩn rubric.
- **D03: Tính cô lập của Cơ sở dữ liệu:**
  - Chỉ duy nhất `Core API` kết nối trực tiếp đến Database qua Entity Framework Core 8.
  - Frontend, Analytics API và AI API tuyệt đối không chứa connection string hay EF DbContext kết nối đến CSDL Core. Mọi tương tác dữ liệu đều thực hiện qua REST API.

---

## 2. Quyết định về Xác thực & Phân quyền (Auth & Security)

- **D04: Lưu trữ Token phía Frontend:**
  - Không lưu Access Token hoặc Refresh Token trong `localStorage` hay biến JavaScript toàn cục (tránh rủi ro XSS).
  - Sử dụng **Server-side Session Token Store** (qua `HttpContext.Session`). Frontend tự quản lý việc refresh token phía máy chủ trước khi gửi HTTP request tới các API.
- **D05: Token Rotation & Thu hồi:**
  - Mỗi lần gọi `/api/auth/refresh`, Refresh Token cũ sẽ bị thu hồi và một Refresh Token mới được sinh ra (One-time use / Token rotation).
  - Đăng xuất (`Logout`) sẽ hủy bỏ toàn bộ chuỗi token đang hoạt động.

---

## 3. Quyết định về Giao diện & Độ ổn định (UI & Resilience)

- **D06: Bootstrap Modals cho CRUD:**
  - Toàn bộ các thao tác Thêm mới và Chỉnh sửa (Bài viết, Chuyên mục, Thẻ tin, Tài khoản) được thực hiện bên trong Bootstrap Modals trên cùng một trang, không điều hướng chuyển trang.
- **D07: Giới hạn số lượng tin gợi ý (Related Articles):**
  - Giữ đúng yêu cầu đề bài: Hiển thị tối đa **3 bài viết liên quan** (`GET /api/recommend/{id}`) dựa trên Category hoặc Tag.
- **D08: Chiến lược Background Worker & Cache Offline:**
  - Worker chạy bằng `IHostedService` và `PeriodicTimer` định kỳ 6 giờ/lần.
  - Bản chụp dữ liệu (Snapshot) được lưu dưới dạng file JSON hoặc bộ nhớ đệm cục bộ `MemoryCache`.
  - Khi hệ thống chuyển sang Offline Mode, toàn bộ các nút thao tác ghi (Thêm, Sửa, Xóa) đều bị khóa cứng (`disabled`).
- **D09: AI Tag Suggestion:**
  - Ưu tiên sử dụng bộ trích xuất từ khóa tự động (Deterministic Keyword Extractor) viết bằng C# hỗ trợ Unicode tiếng Việt, không phụ thuộc bắt buộc vào OpenAI API Key bên ngoài để đảm bảo bài nộp luôn chạy được cục bộ 100% trong phòng thi/chấm bài. Hỗ trợ sẵn cờ cấu hình chuyển sang OpenAI API khi người dùng cung cấp API Key.
