using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public class AiClientService : IAiClientService
{
    private readonly IAiApiClient _aiApiClient;

    public AiClientService(IAiApiClient aiApiClient)
    {
        _aiApiClient = aiApiClient ?? throw new ArgumentNullException(nameof(aiApiClient));
    }

    public async Task<List<TagSuggestionApiModel>> SuggestTagsAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new List<TagSuggestionApiModel>();
        }

        var response = await _aiApiClient.SuggestTagsAsync(content, cancellationToken);
        return response?.Tags ?? new List<TagSuggestionApiModel>();
    }

    public Task SubmitTagFeedbackAsync(int tagId, string? articleId = null, CancellationToken cancellationToken = default)
    {
        if (tagId <= 0) return Task.CompletedTask;
        return _aiApiClient.SubmitTagFeedbackAsync(tagId, articleId, cancellationToken);
    }

    public async Task SubmitMultipleTagFeedbacksAsync(IEnumerable<int> tagIds, string? articleId = null, CancellationToken cancellationToken = default)
    {
        if (tagIds == null) return;

        foreach (var tagId in tagIds)
        {
            if (tagId > 0)
            {
                try
                {
                    await _aiApiClient.SubmitTagFeedbackAsync(tagId, articleId, cancellationToken);
                }
                catch
                {
                    // Ignore individual feedback failure so it does not block main article operations
                }
            }
        }
    }
}
