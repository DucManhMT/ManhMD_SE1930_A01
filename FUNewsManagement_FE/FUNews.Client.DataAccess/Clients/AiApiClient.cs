using System.Net.Http.Json;
using System.Text.Json;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.DataAccess.Clients;

public class AiApiClient : IAiApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AiApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<TagSuggestionResponseApiModel> SuggestTagsAsync(string content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var payload = new { content };
        using var response = await _httpClient.PostAsJsonAsync("api/ai/suggest-tags", payload, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<TagSuggestionResponseApiModel>(JsonOptions, cancellationToken);
        return result ?? new TagSuggestionResponseApiModel();
    }

    public async Task SubmitTagFeedbackAsync(int tagId, string? articleId = null, CancellationToken cancellationToken = default)
    {
        var payload = new TagFeedbackRequestApiModel
        {
            TagId = tagId,
            ArticleId = articleId
        };

        using var response = await _httpClient.PostAsJsonAsync("api/ai/tag-feedback", payload, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }
    }

    private static async Task HandleErrorResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ApiProblemDetails? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(JsonOptions, cancellationToken);
        }
        catch
        {
            // Failed to parse ProblemDetails JSON
        }

        var message = problem?.Detail ?? problem?.Title ?? $"API error: {response.StatusCode}";
        throw new FUNewsApiException(response.StatusCode, message, problem);
    }
}
