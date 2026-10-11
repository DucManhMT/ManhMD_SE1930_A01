# Kế Hoạch & Kịch Bản Kiểm Thử (Test Plan) — PRN232 Assignment 02

Bao quát toàn bộ 24 kịch bản kiểm thử (Test Cases `T01` – `T24`) để xác minh 100% các tiêu chí trong Đề bài Assignment 02.

---

## Danh Mục 24 Kịch Bản Kiểm Thử

| Test ID | Tên Kịch Bản Kiểm Thử | Yêu cầu / Tiêu chí Đánh giá | Trạng thái Mong đợi (Pass Criteria) |
|:---:|:---|:---|:---|
| **T01** | **Build & Chạy 4 Solutions độc lập** | Kiểm tra biên dịch và khởi chạy 4 solution độc lập (.NET 8): Core API, Analytics API, AI API, Frontend. | Cả 4 solution build thành công 0 error; chạy trên các port mạng riêng biệt. |
| **T02** | **Frontend cô lập hoàn toàn Database** | Kiểm tra references của Frontend (`FUNewsManagement_FE.sln`). | Frontend không chứa `DbContext`, không tham chiếu `Microsoft.EntityFrameworkCore`, gọi API qua `HttpClientFactory`. |
| **T03** | **Kế thừa & Chạy CRUD NewsArticle** | Thêm, sửa, xóa, nhân bản (duplicate), xem chi tiết bài viết với Staff. | Dữ liệu cập nhật chính xác; nhân bản tạo bài mới ở trạng thái Inactive; Public chỉ thấy bài Active. |
| **T04** | **Ràng buộc toàn vẹn & Chặn xóa dữ liệu** | Xóa Category đang có bài viết; Xóa Account đã từng tạo bài; Xóa Tag đang được gắn vào bài; Tạo Tag trùng tên. | Hệ thống chặn thao tác, trả về mã lỗi HTTP 409/400 kèm thông báo tiếng Việt rõ ràng, không làm hỏng CSDL. |
| **T05** | **JWT Authentication & Đăng nhập các Role** | Đăng nhập Admin (config), Staff (DB), Lecturer (DB); kiểm tra Claims và Session Token Store. | Cấp JWT Token hợp lệ chứa đúng Role, lưu trữ an toàn trong Session phía máy chủ, không lộ token ra client. |
| **T06** | **Refresh Token Flow tự động** | Giả lập Access Token hết hạn; gửi request qua `DelegatingHandler`. | Tự động gọi `POST /api/auth/refresh`, nhận token mới và tiếp tục request thành công mà không bắt đăng nhập lại. |
| **T07** | **Audit Logging tự động** | Thực hiện thao tác thêm/sửa/xóa bài viết, category, tag hoặc account. | Bản ghi `AuditLog` được lưu tự động trong CSDL, ghi rõ User, Action, Entity, Timestamp và JSON Before/After. |
| **T08** | **Upload & Hiển thị ảnh bài viết** | Upload file ảnh hợp lệ (PNG, JPG < 5MB) và upload file không hợp lệ (EXE, file > 10MB). | File hợp lệ lưu thành công và hiển thị ảnh trên trang; file không hợp lệ bị từ chối với mã lỗi 415/413. |
| **T09** | **Xem nhật ký Audit Log trên UI Admin** | Truy cập màn hình Admin Audit Log, lọc theo Người dùng và Loại đối tượng. | Hiển thị bảng nhật ký rõ ràng, mở modal xem JSON Before/After chi tiết và đẹp mắt. |
| **T10** | **Analytics API: Dashboard số liệu** | Gọi `GET /api/analytics/dashboard` với các bộ lọc ngày, category, status. | Trả về JSON tổng số bài viết phân nhóm chính xác theo danh mục và trạng thái. |
| **T11** | **Frontend Dashboard: Chart.js** | Truy cập màn hình Dashboard Admin với biểu đồ Pie Chart và Bar Chart. | Biểu đồ Chart.js vẽ mượt mà, đổi số liệu tương ứng khi thay đổi bộ lọc ngày/chuyên mục. |
| **T12** | **Analytics API: Trending articles** | Đọc bài viết nhiều lần, sau đó gọi `GET /api/analytics/trending`. | Trả về danh sách các bài viết có lượt view cao nhất theo thứ tự giảm dần. |
| **T13** | **Gợi ý bài viết liên quan (Recommend)** | Gọi `GET /api/recommend/{id}` trên một bài viết cụ thể. | Trả về tối đa 3 bài viết Active có chung Category hoặc Tags với bài viết gốc. |
| **T14** | **Xuất báo cáo Excel từ Dashboard** | Nhấn nút "Export Excel" (`GET /api/analytics/export`). | Tải về file `.xlsx` mở được bằng Excel, chứa đầy đủ các sheet/cột thống kê theo đúng bộ lọc. |
| **T15** | **AI API: Gợi ý Tag từ nội dung** | Gửi văn bản bài viết tới `POST /api/ai/suggest-tags`. | Nhận về danh sách các thẻ tin phù hợp kèm điểm tin cậy `confidence` (0.0 – 1.0). |
| **T16** | **AI Tag Chips & Learning Cache** | Nhân viên chọn một tag gợi ý vào bài viết; kiểm tra phản hồi lưu Learning Cache. | Tag hiển thị dạng Badges/Chips trên Modal; khi chọn tag, `TagLearning` tăng biến đếm và ưu tiên tag này ở lần sau. |
| **T17** | **Polly Retry Policy khi mạng chập chờn** | Giả lập Core API tạm thời phản hồi mã 503 cho request GET tin tức. | `HttpClient` tự động retry tối đa 3 lần với khoảng thời gian tăng dần trước khi báo lỗi cho người dùng. |
| **T18** | **Background Worker HostedService 6h** | Kích hoạt worker nạp dữ liệu cache định kỳ. | Worker chạy ngầm không gây block ứng dụng, tải danh mục và bài viết lưu vào bộ đệm snapshot thành công. |
| **T19** | **Chế độ Offline Mode** | Tắt dịch vụ Core API, truy cập lại trang Frontend. | Phát hiện API down ngay lập tức, hiển thị Banner "Offline Mode", đọc bài từ cache, toàn bộ nút CRUD bị vô hiệu hóa. |
| **T20** | **Bootstrap Modals & Xác nhận thao tác** | Mở form Thêm/Sửa tin, bấm nút Xóa. | Form hiển thị trong Bootstrap Modal không reload trang; thao tác Xóa hiển thị pop-up Confirm bắt buộc. |
| **T21** | **SignalR Realtime Notification** | Mở 2 tab trình duyệt: Tab 1 tạo bài viết mới, quan sát Tab 2. | Tab 2 lập tức nhảy pop-up Toast bài viết mới và Icon quả chuông 🔔 tăng số đếm; lưu trữ ≥ 10 tin gần nhất. |
| **T22** | **Loading Indicator & Toast Alert** | Thực hiện bất kỳ thao tác API nào. | Luôn có Spinner xoay tròn trong lúc đợi API; hiển thị Toast thông báo kết quả xanh (thành công) / đỏ (thất bại). |
| **T23** | **Trợ năng (Accessibility) & Hiệu năng** | Kiểm tra điều hướng phím Tab, độ tương phản màu sắc và thời gian phản hồi API. | Có thể thao tác bàn phím đầy đủ, màu sắc độ tương phản cao, API phản hồi < 1 giây. |
| **T24** | **Nghiệm thu toàn diện bộ nộp bài** | Kiểm tra 4 solutions, file SQL, tài liệu `README.md` và ảnh chụp minh chứng. | Đầy đủ 4 `.sln`, script SQL chạy trơn tru, README mô tả rõ URLs và tài khoản test, screenshots đầy đủ các chức năng. |
