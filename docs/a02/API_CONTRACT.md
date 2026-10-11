# Quy Chuẩn Giao Tiếp API (API Contract) — PRN232 Assignment 02

Toàn bộ quy chuẩn về Route, HTTP Method, Request Body, Response Body, Status Codes và Phân quyền truy cập cho cả 3 dịch vụ Web API và SignalR Hub.

---

## 1. Core API (`FUNewsManagement_CoreAPI`)

### 1.1 Xác thực & Quản lý Token (Authentication)

| Endpoint | Method | Phân quyền | Request Body / Params | Response Body | Status Codes |
|:---|:---:|:---:|:---|:---|:---:|
| `/api/auth/login` | `POST` | Public | `{"email": "...", "password": "..."}` | `{"accessToken": "...", "refreshToken": "...", "user": {...}}` | `200`, `400`, `401` |
| `/api/auth/refresh` | `POST` | Public | `{"refreshToken": "..."}` | `{"accessToken": "...", "refreshToken": "...", "user": {...}}` | `200`, `400`, `401` |
| `/api/auth/logout` | `POST` | Authenticated | `{"refreshToken": "..."}` | `{"message": "Logged out successfully"}` | `200`, `204`, `400` |

### 1.2 Quản lý Tài khoản (Account Management)

| Endpoint | Method | Phân quyền | Request Body / Params | Response Body | Status Codes |
|:---|:---:|:---:|:---|:---|:---:|
| `/api/account` | `GET` | `Admin` | OData Query (`$filter`, `$top`, `$skip`) | Danh sách tài khoản (không lộ mật khẩu) | `200`, `401`, `403` |
| `/api/account` | `POST` | `Admin` | `{"accountName": "...", "accountEmail": "...", "accountRole": 1, "accountPassword": "..."}` | Thông tin tài khoản vừa tạo | `201`, `400`, `409` |
| `/api/account/{id}` | `PUT` | `Admin` | `{"accountName": "...", "accountEmail": "...", "accountRole": 1}` | Thông tin tài khoản sau khi sửa | `200`, `400`, `404` |
| `/api/account/{id}` | `DELETE` | `Admin` | `{id}` trong URL | `204 No Content` *(chặn xóa nếu đã tạo bài)* | `204`, `400`, `404`, `409` |
| `/api/profile` | `GET` | `Staff, Lecturer` | Lấy từ Claims Token | Thông tin cá nhân đang đăng nhập | `200`, `401` |
| `/api/profile/change-password` | `POST` | `Staff, Lecturer` | `{"oldPassword": "...", "newPassword": "...", "confirmPassword": "..."}` | `{"message": "Password changed successfully"}` | `200`, `400`, `401` |

### 1.3 Quản lý Chuyên mục & Thẻ tin (Category & Tag)

| Endpoint | Method | Phân quyền | Request Body / Params | Response Body | Status Codes |
|:---|:---:|:---:|:---|:---|:---:|
| `/api/category` | `GET` | Public | OData Query | Danh sách Category kèm `articleCount` | `200` |
| `/api/category` | `POST` | `Staff` | `{"categoryName": "...", "categoryDescription": "...", "parentCategoryID": null, "isActive": true}` | Category vừa tạo | `201`, `400`, `409` |
| `/api/category/{id}` | `PUT` | `Staff` | `{"categoryName": "...", "categoryDescription": "...", "parentCategoryID": null, "isActive": true}` | Category sau khi sửa | `200`, `400`, `404` |
| `/api/category/{id}` | `DELETE` | `Staff` | `{id}` trong URL | `204 No Content` *(chặn nếu chứa bài)* | `204`, `400`, `404`, `409` |
| `/api/tag` | `GET` | Public | OData Query, search keyword | Danh sách Tag kèm số bài viết sử dụng | `200` |
| `/api/tag` | `POST` | `Staff` | `{"tagName": "...", "note": "..."}` | Tag vừa tạo *(chặn trùng tên)* | `201`, `400`, `409` |
| `/api/tag/{id}` | `PUT` | `Staff` | `{"tagName": "...", "note": "..."}` | Tag sau khi sửa | `200`, `400`, `404`, `409` |
| `/api/tag/{id}` | `DELETE` | `Staff` | `{id}` trong URL | `204 No Content` *(chặn nếu đang gắn vào bài)* | `204`, `400`, `404`, `409` |

### 1.4 Quản lý Bài viết & Upload Ảnh (NewsArticle & Media)

| Endpoint | Method | Phân quyền | Request Body / Params | Response Body | Status Codes |
|:---|:---:|:---:|:---|:---|:---:|
| `/api/news` | `GET` | Public / All | OData, search, paging. Public chỉ nhận Active | Danh sách bài viết | `200` |
| `/api/news/{id}` | `GET` | Public / All | `{id}` trong URL | Chi tiết bài viết (kèm danh sách Tags, ảnh) | `200`, `404` |
| `/api/news` | `POST` | `Staff` | `{"newsTitle": "...", "headline": "...", "newsContent": "...", "newsSource": "...", "categoryID": 1, "newsStatus": true, "tagIDs": [1, 2]}` | Bài viết vừa tạo | `201`, `400` |
| `/api/news/{id}` | `PUT` | `Staff` | DTO cập nhật bài viết + tagIDs | Bài viết sau khi cập nhật | `200`, `400`, `404` |
| `/api/news/{id}` | `DELETE` | `Staff` | `{id}` trong URL | `204 No Content` | `204`, `404` |
| `/api/news/{id}/image` | `POST` | `Staff` | `multipart/form-data` chứa file ảnh (`file`) | `{"imageUrl": "/uploads/news/xyz.jpg"}` | `200`, `400`, `413`, `415` |
| `/api/news/{id}/duplicate` | `POST` | `Staff` | `{id}` trong URL | Bài viết mới bản sao (Status: Inactive) | `201`, `404` |
| `/api/news/mine` | `GET` | `Staff` | Token của Staff | Danh sách bài do chính Staff tạo | `200`, `401` |

### 1.5 Nhật ký Hệ thống & Ghi nhận lượt xem (AuditLog & Views)

| Endpoint | Method | Phân quyền | Request Body / Params | Response Body | Status Codes |
|:---|:---:|:---:|:---|:---|:---:|
| `/api/auditlog` | `GET` | `Admin` | Query params: `userId`, `entityType`, `page`, `pageSize` | Danh sách lịch sử thay đổi (Before/After JSON) | `200`, `401`, `403` |
| `/api/news/{id}/view` | `POST` | Public | `{id}` trong URL | `200 OK` (Tăng view cho bài viết) | `200`, `404` |

---

## 2. Analytics API (`FUNewsManagement_AnalyticsAPI`)

| Endpoint | Method | Phân quyền | Request Body / Params | Response Body | Status Codes |
|:---|:---:|:---:|:---|:---|:---:|
| `/api/analytics/dashboard` | `GET` | `Admin` | Query: `startDate`, `endDate`, `categoryId`, `authorId`, `status` | `{"totalArticles": 100, "byCategory": [...], "byStatus": [...]}` | `200`, `401`, `403` |
| `/api/analytics/trending` | `GET` | `Admin` | Query: `limit` (mặc định 10 bài) | Danh sách bài xem nhiều nhất `[{"newsArticleId": "...", "newsTitle": "...", "viewCount": 500}]` | `200`, `401`, `403` |
| `/api/recommend/{id}` | `GET` | Public | `{id}` bài viết hiện tại | Danh sách tối đa 3 bài liên quan theo Category / Tag | `200`, `404` |
| `/api/analytics/export` | `GET` | `Admin` | Query tương tự Dashboard | File binary Excel (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`) | `200`, `401`, `403` |

---

## 3. AI API (`FUNewsManagement_AIAPI`)

| Endpoint | Method | Phân quyền | Request Body / Params | Response Body | Status Codes |
|:---|:---:|:---:|:---|:---|:---:|
| `/api/ai/suggest-tags` | `POST` | `Staff` | `{"content": "Nội dung bài viết..."}` | `{"suggestedTags": [{"tagId": 1, "tagName": "AI", "confidence": 0.95}, ...]}` | `200`, `400` |
| `/api/ai/tag-feedback` | `POST` | `Staff` | `{"selectedTagIds": [1, 2], "articleId": "..."}` | `{"message": "Feedback recorded in Learning Cache"}` | `200`, `400` |

---

## 4. SignalR Notification Hub (`FUNewsManagement_FE`)

- **Hub URL:** `/hubs/notifications`
- **Giao thức:** WebSockets / Long Polling qua thư viện `@microsoft/signalr`.
- **Client Listeners:**
  - `ReceiveNotification(notification)`: Nhận thông báo khi có bài viết mới được xuất bản.
    - Dữ liệu payload: `{"articleId": "N123", "title": "...", "author": "...", "createdDate": "...", "category": "..."}`.
- **Client Actions:**
  - `GetRecentNotifications()`: Gọi lên Hub để nhận lại danh sách tối thiểu 10 thông báo gần nhất khi vừa tải trang.
