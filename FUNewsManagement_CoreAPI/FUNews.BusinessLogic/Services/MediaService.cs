using FUNews.BusinessLogic.Exceptions;
using FUNews.DataAccess.Repositories;

namespace FUNews.BusinessLogic.Services;

public class MediaService : IMediaService
{
    private readonly INewsArticleRepository _articleRepository;
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    public MediaService(INewsArticleRepository articleRepository)
    {
        _articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
    }

    public async Task<string> UploadNewsImageAsync(
        string articleId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileLength,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articleId);
        ArgumentNullException.ThrowIfNull(fileStream);

        // 1. Kiểm tra kích thước file
        if (fileLength <= 0)
        {
            throw new ValidationException("File", "Vui lòng chọn file ảnh để tải lên.");
        }

        if (fileLength > MaxFileSize)
        {
            throw new PayloadTooLargeException("Dung lượng file vượt quá giới hạn cho phép (tối đa 5MB).");
        }

        // 2. Kiểm tra phần mở rộng file (Extension)
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        {
            throw new UnsupportedMediaTypeException("Định dạng file không được hỗ trợ. Chỉ chấp nhận các file ảnh PNG, JPG, JPEG, WEBP.");
        }

        // 3. Kiểm tra Magic Bytes thực tế
        ValidateImageHeader(fileStream, ext);

        // 4. Kiểm tra bài viết tồn tại trong hệ thống
        var article = await _articleRepository.GetByIdAsync(articleId, cancellationToken);
        if (article == null)
        {
            throw new NotFoundException($"Bài viết với mã {articleId} không tồn tại.");
        }

        // 5. Chuẩn bị thư mục lưu trữ
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "news");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        var uniqueFileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var filePath = Path.Combine(uploadsDir, uniqueFileName);

        // 6. Ghi file ra ổ đĩa
        if (fileStream.CanSeek)
        {
            fileStream.Seek(0, SeekOrigin.Begin);
        }

        using (var destStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await fileStream.CopyToAsync(destStream, cancellationToken);
        }

        // 7. Cập nhật đường dẫn ảnh vào bài viết
        var relativeUrl = $"/uploads/news/{uniqueFileName}";
        article.NewsImage = relativeUrl;
        await _articleRepository.UpdateAsync(article, cancellationToken);

        return relativeUrl;
    }

    private static void ValidateImageHeader(Stream stream, string ext)
    {
        if (!stream.CanRead) return;

        var header = new byte[8];
        var originalPos = stream.CanSeek ? stream.Position : 0;

        var bytesRead = stream.Read(header, 0, header.Length);
        if (stream.CanSeek)
        {
            stream.Seek(originalPos, SeekOrigin.Begin);
        }

        if (bytesRead < 4)
        {
            throw new UnsupportedMediaTypeException("File không đủ dung lượng để xác định định dạng ảnh hợp lệ.");
        }

        // PNG: 89 50 4E 47
        var isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;

        // JPG: FF D8 FF
        var isJpg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

        // WEBP: RIFF (52 49 46 46)
        var isWebp = header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46;

        if (!isPng && !isJpg && !isWebp)
        {
            throw new UnsupportedMediaTypeException("Nội dung file không khớp với định dạng ảnh hợp lệ.");
        }
    }
}
