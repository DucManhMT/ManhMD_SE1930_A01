using FUNewsManagement_AIAPI.Models;

namespace FUNewsManagement_AIAPI.Services;

public interface ITagExtractorService
{
    Task<List<TagSuggestionDto>> SuggestTagsAsync(string content, CancellationToken cancellationToken = default);
}
