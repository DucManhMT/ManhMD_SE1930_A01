using System.Net.Http.Json;
using System.Text.Json;
using FUNewsManagement_AIAPI.Models;
using Microsoft.Extensions.Caching.Memory;

namespace FUNewsManagement_AIAPI.Services;

public class TagCatalogClient : ITagCatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<TagCatalogClient> _logger;

    private const string CacheKey = "AI_AvailableTags";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TagCatalogClient(HttpClient httpClient, IMemoryCache memoryCache, ILogger<TagCatalogClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<CoreTagDto>> GetAvailableTagsAsync(CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue(CacheKey, out List<CoreTagDto>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            using var response = await _httpClient.GetAsync("api/tag", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var odata = await response.Content.ReadFromJsonAsync<CoreODataResponse<CoreTagDto>>(JsonOptions, cancellationToken);
                var tags = odata?.Value ?? new List<CoreTagDto>();
                if (tags.Count > 0)
                {
                    _memoryCache.Set(CacheKey, tags, CacheDuration);
                    return tags;
                }
            }
            else
            {
                _logger.LogWarning("Failed to retrieve tags from Core API. Status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while calling Core API for tags catalog");
        }

        return new List<CoreTagDto>();
    }
}
