using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public interface IAiClientService
{
    Task<List<TagSuggestionApiModel>> SuggestTagsAsync(string content, CancellationToken cancellationToken = default);
    Task SubmitTagFeedbackAsync(int tagId, string? articleId = null, CancellationToken cancellationToken = default);
    Task SubmitMultipleTagFeedbacksAsync(IEnumerable<int> tagIds, string? articleId = null, CancellationToken cancellationToken = default);
}
