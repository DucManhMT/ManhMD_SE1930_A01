namespace FUNewsManagement_AIAPI.Services;

public interface ILearningCacheService
{
    int GetFrequencyCount(int tagId);
    double GetBonusWeight(int tagId);
    Task RecordFeedbackAsync(IEnumerable<int> tagIds, CancellationToken cancellationToken = default);
    Dictionary<int, int> GetAllCounts();
}
