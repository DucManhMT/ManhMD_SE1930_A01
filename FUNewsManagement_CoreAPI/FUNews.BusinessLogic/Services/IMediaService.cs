namespace FUNews.BusinessLogic.Services;

public interface IMediaService
{
    Task<string> UploadNewsImageAsync(
        string articleId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileLength,
        CancellationToken cancellationToken = default);
}
