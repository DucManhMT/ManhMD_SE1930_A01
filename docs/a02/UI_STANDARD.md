# Tiêu Chuẩn Giao Diện & Trải Nghiệm Người Dùng (UI/UX Standards) — PRN232 A02

Tài liệu quy chuẩn chi tiết cho **12 Màn hình Chức năng** và **Quy tắc Trải nghiệm Người dùng (UX)** theo đúng Mục 4 & Mục 5 trong Đề bài Assignment 02.

---

## 1. Danh Mục 12 Màn Hình Chức Năng (Screen-Level Requirements)

### 4.1 Màn hình Đăng nhập (Login Page)
- **Chức năng:** Người dùng nhập Email và Mật khẩu để đăng nhập hệ thống.
- **APIs:** `POST /api/auth/login`, `POST /api/auth/refresh`.
- **Yêu cầu:**
  - Lưu `access_token` và `refresh_token` vào Session máy chủ khi đăng nhập thành công.
  - Hiển thị Toast / Alert thông báo lỗi chi tiết khi đăng nhập thất bại.
  - Tự động làm mới token trước khi hết hạn.

### 4.2 Màn hình Báo cáo Thống kê (Dashboard - Admin)
- **Chức năng:** Hiển thị số liệu thống kê bài viết, chuyên mục, tác giả cho Quản trị viên.
- **APIs:** `GET /api/analytics/dashboard`, `GET /api/analytics/trending`.
- **Yêu cầu:**
  - Hiển thị biểu đồ đồ họa sinh động sử dụng thư viện **Chart.js** (Biểu đồ tròn Pie Chart, Biểu đồ cột Bar Chart).
  - Thanh công cụ bộ lọc: Lọc theo khoảng ngày (`startDate` – `endDate`), Chuyên mục, Trạng thái (Active/Inactive).
  - Nút bấm **"Export Excel"** tải về báo cáo định dạng `.xlsx` qua `GET /api/analytics/export`.

### 4.3 Màn hình Danh sách Bài viết (News List - Staff)
- **Chức năng:** Liệt kê toàn bộ bài viết trong hệ thống cho Nhân viên biên tập.
- **API:** `GET /api/news`.
- **Yêu cầu:**
  - Hỗ trợ phân trang (Pagination), tìm kiếm theo từ khóa, sắp xếp theo ngày tạo.
  - Các cột hiển thị: Tiêu đề, Chuyên mục, Ngày tạo, Trạng thái.
  - Sử dụng **Badges màu sắc** trực quan để phân biệt trạng thái: Màu xanh lá cho `Active`, Màu xám/vàng cho `Inactive`.

### 4.4 Form Thêm mới / Cập nhật Bài viết (Create / Edit News)
- **Chức năng:** Tạo mới hoặc chỉnh sửa nội dung bài viết.
- **APIs:** `POST /api/news`, `PUT /api/news/{id}`, `POST /api/news/{id}/image`.
- **Yêu cầu:**
  - Sử dụng **Bootstrap Modals** cho toàn bộ form nhập liệu (không điều hướng rời trang).
  - Kiểm tra hợp lệ dữ liệu (Validation) các trường: Tiêu đề, Nội dung, Chuyên mục, Thẻ tin.
  - Tích hợp nút bấm **"Gợi ý Thẻ tin bằng AI"** để tự động nhận đề xuất tag từ nội dung.
  - Hỗ trợ chọn và tải lên ảnh bài viết (xem trước ảnh trước khi lưu).

### 4.5 Màn hình Chi tiết Bài viết (News Detail)
- **Chức năng:** Đọc toàn bộ nội dung bài viết và hiển thị tin liên quan.
- **APIs:** `GET /api/news/{id}`, `GET /api/recommend/{id}`.
- **Yêu cầu:**
  - Hiển thị tối đa **3 bài viết liên quan** dựa trên chuyên mục hoặc tag chung.
  - Giao diện đọc bài viết thoáng đãng, chuẩn Responsive trên cả thiết bị di động và máy tính.

### 4.6 Màn hình Quản lý Chuyên mục (Category Management - Staff)
- **Chức năng:** Quản lý danh mục bài viết.
- **APIs:** `GET /api/category`, `POST /api/category`, `PUT /api/category/{id}`, `DELETE /api/category/{id}`.
- **Yêu cầu:**
  - Chặn tuyệt đối hành động xóa nếu chuyên mục đang chứa bài viết.
  - Nút chuyển đổi nhanh trạng thái kích hoạt (`IsActive`).
  - Hiển thị số lượng bài viết trực thuộc mỗi chuyên mục.

### 4.7 Màn hình Quản lý Thẻ tin (Tag Management - Staff)
- **Chức năng:** Quản lý danh sách các thẻ tin gắn kèm bài viết.
- **APIs:** `GET /api/tag`, `POST /api/tag`, `PUT /api/tag/{id}`, `DELETE /api/tag/{id}`.
- **Yêu cầu:**
  - Kiểm tra và ngăn chặn tạo tên Tag trùng lặp.
  - Hỗ trợ thanh tìm kiếm Tag nhanh.
  - Hiển thị danh sách các bài viết đang sử dụng mỗi Tag.

### 4.8 Màn hình Quản lý Tài khoản (Account Management - Admin)
- **Chức năng:** Quản trị danh sách người dùng hệ thống.
- **APIs:** `GET /api/account`, `POST /api/account`, `PUT /api/account/{id}`, `DELETE /api/account/{id}`.
- **Yêu cầu:**
  - Chặn xóa tài khoản đã từng khởi tạo bài viết trong CSDL.
  - Đổi mật khẩu bắt buộc phải nhập chính xác mật khẩu hiện tại.
  - Bộ lọc danh sách tài khoản theo Vai trò (Admin / Staff / Lecturer).

### 4.9 Thành phần Gợi ý Thẻ tin bằng AI (AI Tag Suggestion)
- **Chức năng:** Gợi ý tag tự động từ nội dung văn bản.
- **API:** `POST /api/ai/suggest-tags`.
- **Yêu cầu:**
  - Người dùng nhập nội dung bài viết → Nhấn nút *"Suggest Tag"*.
  - Hiển thị các tag gợi ý dưới dạng **Badges / Chips** kèm màu sắc theo độ tin cậy (Confidence).
  - Cho phép người dùng click nhanh vào Chip để tự động thêm tag đó vào bài viết.

### 4.10 Trung tâm Thông báo Thời gian thực (Notification Center)
- **Chức năng:** Bắn thông báo ngay khi có bài viết mới được xuất bản.
- **SignalR Hub:** `/hubs/notifications`.
- **Yêu cầu:**
  - Hiển thị pop-up **Toast** hoặc chấm đỏ trên **Icon Quả chuông 🔔** trên thanh điều hướng.
  - Lưu trữ và hiển thị danh sách **ít nhất 10 thông báo gần nhất**.

### 4.11 Chế độ Hoạt động Ngoại tuyến (Offline Mode)
- **Chức năng:** Duy trì hoạt động khi mất kết nối đến các Web API.
- **Nguồn dữ liệu:** Cache bộ nhớ hoặc file JSON cục bộ do Background Worker lưu trữ.
- **Yêu cầu:**
  - Tự động nhận diện khi không thể kết nối tới API.
  - Hiển thị Banner màu vàng nổi bật: **"Hệ thống đang chạy ở chế độ Ngoại Tuyến (Offline Mode)"**.
  - Cho phép đọc tin và danh mục từ cache, đồng thời vô hiệu hóa (`disabled`) toàn bộ các nút Thêm/Sửa/Xóa.

### 4.12 Màn hình Lịch sử Thay đổi Dữ liệu (Audit Log - Admin)
- **Chức năng:** Xem lịch sử mọi thao tác CRUD của người dùng.
- **API:** `GET /api/auditlog`.
- **Yêu cầu:**
  - Hiển thị các cột: Người thực hiện (User), Hành động (Action), Đối tượng (Entity), Thời gian (Timestamp).
  - Modal xem chi tiết dữ liệu Trước / Sau khi thay đổi ở định dạng JSON (`Before/After JSON`).
  - Hỗ trợ lọc theo Người dùng hoặc theo Loại đối tượng.

---

## 2. Tiêu Chuẩn Trải Nghiệm Người Dùng (UX & Functional Rules)

1. **Hộp thoại xác nhận (Confirmation Prompt):**
   - Mọi thao tác Xóa (Delete) hoặc Thay đổi trạng thái nhạy cảm đều phải hiển thị Modal xác nhận trước khi thực hiện.
2. **Chỉ báo đang tải (Loading Indicator):**
   - Mọi thao tác gọi API (Đăng nhập, Lưu bài, Tải bảng, Xuất Excel, Gợi ý AI) đều phải hiển thị biểu tượng loading xoay tròn (Spinner) hoặc thanh tiến trình để người dùng không bấm lặp.
3. **Thông báo Toast / Alert thống nhất:**
   - Thông báo màu xanh lá (Success) khi thao tác thành công.
   - Thông báo màu đỏ (Danger/Error) khi có lỗi kèm thông điệp rõ ràng, dễ hiểu.
4. **Chuẩn tương thích & Trợ năng (Accessibility):**
   - Thiết kế giao diện Responsive tương thích mượt mà trên cả Mobile và Desktop.
   - Hỗ trợ điều hướng bằng bàn phím (Phím Tab, Enter, Esc đóng modal).
   - Độ tương phản màu sắc cao (High color contrast) dễ nhìn.
5. **Hiệu năng (Performance):**
   - Thời gian phản hồi của các API đạt dưới 1 giây (< 1s); tận dụng cache để tối ưu tốc độ.
