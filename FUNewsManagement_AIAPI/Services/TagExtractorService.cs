using System.Text.RegularExpressions;
using FUNewsManagement_AIAPI.Models;

namespace FUNewsManagement_AIAPI.Services;

public class TagExtractorService : ITagExtractorService
{
    private readonly ITagCatalogClient _tagCatalogClient;
    private readonly ILearningCacheService _learningCacheService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TagExtractorService> _logger;

    // Danh sách Stop-words tiếng Việt và tiếng Anh phổ biến
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "và", "là", "của", "những", "các", "có", "trong", "được", "với", "cho", "về", "khi",
        "để", "như", "này", "đó", "người", "ra", "vào", "lại", "thì", "mà", "ở", "tại",
        "theo", "sau", "từ", "nhiều", "nhất", "một", "hai", "ba", "trên", "dưới", "qua",
        "đã", "đang", "sẽ", "phải", "nên", "bởi", "vì", "nếu", "nhưng", "tuy", "dù",
        "the", "a", "an", "and", "or", "in", "on", "at", "to", "for", "with", "is",
        "are", "was", "were", "of", "it", "this", "that", "from", "by", "as", "be"
    };

    public TagExtractorService(
        ITagCatalogClient tagCatalogClient,
        ILearningCacheService learningCacheService,
        IConfiguration configuration,
        ILogger<TagExtractorService> logger)
    {
        _tagCatalogClient = tagCatalogClient ?? throw new ArgumentNullException(nameof(tagCatalogClient));
        _learningCacheService = learningCacheService ?? throw new ArgumentNullException(nameof(learningCacheService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<TagSuggestionDto>> SuggestTagsAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new List<TagSuggestionDto>();
        }

        // 1. Kiểm tra cấu hình OpenAI nếu người dùng bật
        bool useOpenAi = _configuration.GetValue<bool>("AiSettings:UseOpenAI");
        string? openAiApiKey = _configuration["AiSettings:OpenAIApiKey"];

        if (useOpenAi && !string.IsNullOrWhiteSpace(openAiApiKey))
        {
            try
            {
                var openAiResult = await TrySuggestWithOpenAiAsync(content, openAiApiKey, cancellationToken);
                if (openAiResult != null && openAiResult.Count > 0)
                {
                    return openAiResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI API call failed. Falling back to local keyword extraction engine.");
            }
        }

        // 2. Thuật toán phân tích từ khóa cục bộ (Local C# Keyword Extraction Engine)
        return await ExtractTagsLocallyAsync(content, cancellationToken);
    }

    private async Task<List<TagSuggestionDto>> ExtractTagsLocallyAsync(string content, CancellationToken cancellationToken)
    {
        var availableTags = await _tagCatalogClient.GetAvailableTagsAsync(cancellationToken);
        if (availableTags == null || availableTags.Count == 0)
        {
            _logger.LogWarning("Tag catalog is empty. Returning no suggested tags.");
            return new List<TagSuggestionDto>();
        }

        // Chuẩn hóa văn bản đầu vào
        var normalizedContent = NormalizeText(content);
        var suggestions = new List<TagSuggestionDto>();

        foreach (var tag in availableTags)
        {
            if (string.IsNullOrWhiteSpace(tag.TagName)) continue;

            var normalizedTagName = NormalizeText(tag.TagName);
            if (string.IsNullOrWhiteSpace(normalizedTagName)) continue;

            // Đếm tần suất xuất hiện chính xác của tag trong văn bản
            int frequency = CountOccurrences(normalizedContent, normalizedTagName);

            if (frequency > 0)
            {
                // Tính điểm cơ sở dựa trên tần suất và độ dài cụm từ
                double baseScore = 0.50 + Math.Min(0.35, frequency * 0.10);
                if (normalizedTagName.Contains(' ') || normalizedTagName.Length >= 5)
                {
                    baseScore += 0.05; // Cụm từ có trọng số cao hơn
                }

                // Cộng điểm thưởng từ Learning Cache
                double learningBonus = _learningCacheService.GetBonusWeight(tag.TagId);
                double totalConfidence = Math.Min(1.0, baseScore + learningBonus);

                suggestions.Add(new TagSuggestionDto
                {
                    TagId = tag.TagId,
                    TagName = tag.TagName,
                    Confidence = Math.Round(totalConfidence, 2)
                });
            }
            else
            {
                // Nếu tag không xuất hiện chính xác nhưng có từ khóa quan trọng trùng khớp
                var tagWords = normalizedTagName
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => !StopWords.Contains(w))
                    .ToList();

                if (tagWords.Count > 1)
                {
                    int matchingWords = tagWords.Count(w => normalizedContent.Contains(w, StringComparison.OrdinalIgnoreCase));
                    if (matchingWords >= tagWords.Count - 1 && matchingWords > 0)
                    {
                        double baseScore = 0.40 + (matchingWords * 0.05);
                        double learningBonus = _learningCacheService.GetBonusWeight(tag.TagId);
                        double totalConfidence = Math.Min(0.85, baseScore + learningBonus);

                        suggestions.Add(new TagSuggestionDto
                        {
                            TagId = tag.TagId,
                            TagName = tag.TagName,
                            Confidence = Math.Round(totalConfidence, 2)
                        });
                    }
                }
            }
        }

        // Sắp xếp theo Confidence giảm dần, lấy tối đa 8 tag tốt nhất
        return suggestions
            .OrderByDescending(s => s.Confidence)
            .ThenBy(s => s.TagName)
            .Take(8)
            .ToList();
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Chuyển chữ thường, thay thế xuống dòng và dấu câu bằng khoảng trắng
        var lower = text.ToLowerInvariant();
        return Regex.Replace(lower, @"[^\w\s\+\#\.\-]", " ");
    }

    private static int CountOccurrences(string text, string pattern)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(pattern)) return 0;

        // Kiểm tra pattern với ranh giới từ hoặc cụm từ
        var escaped = Regex.Escape(pattern);
        return Regex.Matches(text, $@"(?:^|\s){escaped}(?:$|\s)", RegexOptions.IgnoreCase).Count;
    }

    private Task<List<TagSuggestionDto>?> TrySuggestWithOpenAiAsync(string content, string apiKey, CancellationToken cancellationToken)
    {
        // Có thể mở rộng kết nối HttpClient với api.openai.com/v1/chat/completions khi cần
        return Task.FromResult<List<TagSuggestionDto>?>(null);
    }
}
