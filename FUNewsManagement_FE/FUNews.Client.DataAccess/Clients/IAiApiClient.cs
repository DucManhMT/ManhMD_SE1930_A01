using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.DataAccess.Clients;

public interface IAiApiClient
{
    Task<TagSuggestionResponseApiModel> SuggestTagsAsync(string content, CancellationToken cancellationToken = default);
    Task SubmitTagFeedbackAsync(int tagId, string? articleId = null, CancellationToken cancellationToken = default);
}
