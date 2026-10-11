using Polly;
using Polly.Extensions.Http;

namespace FUNews.Client.DataAccess.Policies;

public static class PollyPolicies
{
    private static readonly Random Jitterer = new();

    /// <summary>
    /// Chính sách Retry cho các request GET: Tối đa 3 lần với exponential backoff (1s, 2s, 4s) kèm jitter ngẫu nhiên
    /// khi gặp lỗi mạng tạm thời hoặc các mã lỗi HTTP 5xx, 408.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetGetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1))
                    + TimeSpan.FromMilliseconds(Jitterer.Next(0, 100))
            );
    }

    /// <summary>
    /// Chỉ áp dụng retry cho HTTP GET an toàn (idempotent), không retry các thao tác ghi (POST/PUT/DELETE).
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetSelectableRetryPolicy(HttpRequestMessage request)
    {
        if (request.Method == HttpMethod.Get)
        {
            return GetGetRetryPolicy();
        }

        return Policy.NoOpAsync<HttpResponseMessage>();
    }
}
