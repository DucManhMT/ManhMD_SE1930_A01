# Các Luồng Kỹ Thuật Xuyên Suốt (Cross-Cutting Concerns) — PRN232 A02

Tài liệu hướng dẫn chi tiết cách triển khai 6 thành phần kỹ thuật cốt lõi theo đúng yêu cầu đề bài.

---

## 1. Cơ Chế JWT Authentication & Refresh Token Flow

### Mô tả bài toán:
- JWT Access Token có thời gian sống ngắn (ví dụ: 15–30 phút) để bảo mật.
- Khi Access Token hết hạn, hệ thống cần tự động sử dụng Refresh Token để lấy cặp Token mới thông qua endpoint `/api/auth/refresh` mà không làm gián đoạn trải nghiệm của người dùng.

### Quy trình thực hiện:
1. **Lưu trữ Token an toàn:**
   - Phía Frontend, lưu `access_token` và `refresh_token` trong **Server-side Session** (hoặc `HttpContext.Session`), tuyệt đối không để lộ trong biến JavaScript toàn cục hoặc `localStorage` trần nhằm chống tấn công XSS.
2. **Cơ chế Refresh Token tự động tại `DelegatingHandler`:**
   - Xây dựng `AuthTokenDelegatingHandler` kế thừa từ `DelegatingHandler`.
   - Trước khi gửi request ra ngoài, kiểm tra thời hạn của Access Token.
   - Nếu Access Token sắp hết hạn (còn dưới 60 giây) hoặc nhận về mã HTTP `401 Unauthorized` từ API:
     - Gửi request `POST /api/auth/refresh` kèm `refreshToken` hiện tại lên Core API.
     - Khi nhận được cặp token mới, cập nhật lại Session.
     - Đính kèm Access Token mới vào header `Authorization: Bearer <token>` và thực hiện lại request ban đầu.
3. **Cơ chế thu hồi (Revocation):**
   - Khi người dùng bấm Đăng xuất (`Logout`) hoặc đổi mật khẩu, Refresh Token sẽ được đánh dấu vô hiệu hóa trong Database (`IsRevoked = true`).

---

## 2. Quản Lý HttpClients, DelegatingHandler & Polly Retry Policy

### Mô tả bài toán:
- Frontend không được kết nối trực tiếp đến Database mà phải gọi qua 3 Web APIs độc lập.
- Cần quản lý vòng đời kết nối qua `IHttpClientFactory` và xử lý chịu lỗi khi mạng chập chờn bằng `Polly`.

### Quy trình thực hiện:
1. **Đăng ký 3 Typed HttpClients trong `Program.cs` của Frontend:**
   ```csharp
   builder.Services.AddHttpClient<ICoreApiClient, CoreApiClient>(client => {
       client.BaseAddress = new Uri(builder.Configuration["ApiEndpoints:CoreApi"]);
   }).AddHttpMessageHandler<AuthTokenDelegatingHandler>()
     .AddPolicyHandler(GetRetryPolicy());

   builder.Services.AddHttpClient<IAnalyticsApiClient, AnalyticsApiClient>(client => {
       client.BaseAddress = new Uri(builder.Configuration["ApiEndpoints:AnalyticsApi"]);
   }).AddHttpMessageHandler<AuthTokenDelegatingHandler>()
     .AddPolicyHandler(GetRetryPolicy());

   builder.Services.AddHttpClient<IAiApiClient, AiApiClient>(client => {
       client.BaseAddress = new Uri(builder.Configuration["ApiEndpoints:AiApi"]);
   }).AddHttpMessageHandler<AuthTokenDelegatingHandler>()
     .AddPolicyHandler(GetRetryPolicy());
   ```
2. **Chính sách Polly Retry Policy:**
   - Chỉ áp dụng retry cho các HTTP request dạng **GET an toàn (idempotent)** khi gặp lỗi tạm thời (Transient Errors: lỗi mạng, timeout, mã lỗi HTTP 502, 503, 504, 408).
   - Tuyệt đối **không retry tự động** cho các thao tác ghi (POST/PUT/DELETE) để tránh tạo dữ liệu trùng lặp.
   - Sử dụng Exponential Backoff kèm Jitter ngẫu nhiên (ví dụ retry tối đa 3 lần với khoảng thời gian: 1s, 2s, 4s).

---

## 3. Background Worker (`HostedService`) Refresh Cache Mỗi 6 Giờ

### Mô tả bài toán:
- Đề bài yêu cầu: *"Include a Background Worker (HostedService) to refresh cached data every 6 hours."*
- Giúp hệ thống luôn có sẵn bản sao lưu trữ dữ liệu (Categories, Tags, Tin tức công khai) trong bộ nhớ đệm / file cục bộ.

### Quy trình thực hiện:
1. Tạo class `CacheRefreshWorker` kế thừa từ `BackgroundService`.
2. Sử dụng `PeriodicTimer` của .NET 8 để kích hoạt định kỳ mỗi 6 giờ:
   ```csharp
   public class CacheRefreshWorker : BackgroundService
   {
       private readonly IServiceProvider _serviceProvider;
       private readonly ILogger<CacheRefreshWorker> _logger;
       private readonly TimeSpan _period = TimeSpan.FromHours(6);

       public CacheRefreshWorker(IServiceProvider serviceProvider, ILogger<CacheRefreshWorker> logger)
       {
           _serviceProvider = serviceProvider;
           _logger = logger;
       }

       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           // Chạy ngay 1 lần khi ứng dụng vừa khởi động
           await RefreshCacheAsync(stoppingToken);

           using var timer = new PeriodicTimer(_period);
           while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
           {
               await RefreshCacheAsync(stoppingToken);
           }
       }

       private async Task RefreshCacheAsync(CancellationToken ct)
       {
           using var scope = _serviceProvider.CreateScope();
           var coreClient = scope.ServiceProvider.GetRequiredService<ICoreApiClient>();
           var offlineManager = scope.ServiceProvider.GetRequiredService<IOfflineStoreManager>();

           try
           {
               _logger.LogInformation("Background Worker: Bắt đầu nạp cache định kỳ...");
               var categories = await coreClient.GetCategoriesAsync(ct);
               var tags = await coreClient.GetTagsAsync(ct);
               var publicArticles = await coreClient.GetActiveArticlesAsync(ct);

               await offlineManager.SaveSnapshotAsync(categories, tags, publicArticles, ct);
               _logger.LogInformation("Background Worker: Cập nhật cache thành công.");
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Background Worker: Lỗi khi nạp dữ liệu cache từ API.");
           }
       }
   }
   ```

---

## 4. Chế Độ Hoạt Động Ngoại Tuyến (Offline Mode)

### Mô tả bài toán:
- Đề bài yêu cầu: *"Operate when the system loses API connectivity. Data Source: Cache or local JSON. Requirements: Detect when APIs are unreachable. Show banner 'Offline Mode'. Display cached data, but disable CRUD operations."*

### Quy trình thực hiện:
1. **Phát hiện mất kết nối API:**
   - Khi các lệnh gọi `HttpClient` ném ra ngoại lệ `HttpRequestException`, `TimeoutException` hoặc trả về mã 503 Service Unavailable, hệ thống tự động ghi nhận cờ trạng thái `IsOffline = true`.
2. **Hiển thị Banner cảnh báo:**
   - Trên file `_Layout.cshtml`, render một thanh thông báo cố định:
     ```html
     @if (OfflineManager.IsOffline)
     {
         <div class="alert alert-warning text-center mb-0" role="alert">
             ⚠️ <strong>Hệ thống đang hoạt động ở chế độ Ngoại Tuyến (Offline Mode)</strong>. Bạn chỉ có thể đọc dữ liệu từ bộ nhớ đệm, các tính năng thêm/sửa/xóa tạm thời bị khóa.
         </div>
     }
     ```
3. **Đọc dữ liệu từ Cache / File JSON:**
   - Khi `IsOffline == true`, các hàm đọc dữ liệu trong Client Service sẽ đọc từ snapshot do `HostedService` vừa lưu trữ.
4. **Khóa các thao tác CRUD:**
   - Trên giao diện: Ẩn hoặc vô hiệu hóa (`disabled`) toàn bộ các nút "Thêm mới", "Chỉnh sửa", "Xóa", "Upload ảnh".
   - Phía Server: Các handler POST/PUT/DELETE từ chối xử lý và trả về thông báo lỗi: *"Hệ thống đang offline, không thể thực hiện thao tác này."*

---

## 5. Hệ Thống Thông Báo Thời Gian Thực (SignalR Notification System)

### Mô tả bài toán:
- Đề bài yêu cầu: *"Build a SignalR Notification System to display real-time notifications when new articles are created. Hub: /hubs/notifications. Display a Toast or 🔔 icon notification. Maintain at least 10 recent notifications."*

### Quy trình thực hiện:
1. **Thiết lập SignalR Hub:**
   - Tạo `NotificationHub` trên Frontend ánh xạ tới URL `/hubs/notifications`.
2. **Kích hoạt thông báo khi tạo bài viết mới:**
   - Khi Staff tạo thành công bài viết (`POST /api/news`), hệ thống gửi broadcast tới tất cả client đang kết nối qua SignalR method `ReceiveNotification`.
3. **Giao diện Client (JavaScript):**
   - Lắng nghe sự kiện `ReceiveNotification`:
     - Hiển thị pop-up **Bootstrap Toast** ở góc màn hình: *"Bài viết mới: [Tiêu đề bài viết]"*.
     - Cập nhật số đếm trên **Icon quả chuông 🔔** trên thanh Navbar.
     - Thêm bài viết mới vào danh sách dropdown thông báo (duy trì danh sách ít nhất 10 thông báo gần nhất bằng mảng JavaScript / LocalStorage).

---

## 6. Thuật Toán AI Gợi Ý Tag & Bộ Nhớ Học (Learning Cache)

### Mô tả bài toán:
- Đề bài yêu cầu: *"Endpoint: /api/ai/suggest-tags. Input: JSON { "content": "..." }. Output: Suggested tags (optionally with confidence scores). Can connect to the real OpenAI API or simulate keyword extraction manually. Maintain a Learning Cache to remember tags frequently selected by users."*

### Quy trình thực hiện:
1. **Thuật toán bóc tách từ khóa tự động (Keyword Extraction Engine):**
   - Chuẩn hóa văn bản: Chuyển về chữ thường, bỏ dấu câu, tách từ theo chuẩn Unicode tiếng Việt.
   - Lọc bỏ từ dừng (Stop-words tiếng Việt/Anh thông dụng như: "là", "và", "của", "những", "the", "and"...).
   - Đếm tần suất xuất hiện của các từ đơn (Unigrams) và cụm từ đôi (Bigrams) có ý nghĩa.
   - So khớp các cụm từ này với danh mục Tags hiện có trong hệ thống.
   - Tính điểm tin cậy `confidence` từ 0.0 đến 1.0 dựa trên tần suất xuất hiện trong bài viết.
2. **Duy trì Learning Cache:**
   - Xây dựng bảng hoặc file JSON `TagLearningCache` lưu cặp `(TagID, FrequencyCount)`.
   - Khi nhân viên biên tập thực sự chọn một tag được gợi ý để gắn vào bài viết (`POST /api/ai/tag-feedback`), hệ thống tăng `FrequencyCount` của tag đó lên 1.
   - Trong các lần gợi ý tiếp theo, những tag có `FrequencyCount` cao trong Learning Cache sẽ được cộng thêm trọng số ưu tiên xuất hiện ở đầu danh sách gợi ý.
3. **Hiển thị trên giao diện:**
   - Hiển thị các tag gợi ý dưới dạng **Badges / Chips** có thể click trực tiếp để thêm nhanh vào bài viết mà không cần gõ phím.
