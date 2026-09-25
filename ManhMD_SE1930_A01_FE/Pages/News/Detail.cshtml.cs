using FUNews.Client.BusinessLogic.Helpers;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages.News;

[AllowAnonymous]
public class DetailModel : PageModel
{
    private readonly INewsClientService _newsClientService;
    private readonly ILogger<DetailModel> _logger;

    public DetailModel(
        INewsClientService newsClientService,
        ILogger<DetailModel> logger)
    {
        _newsClientService = newsClientService ?? throw new ArgumentNullException(nameof(newsClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public NewsArticleApiModel Article { get; set; } = null!;
    public List<NewsArticleApiModel> RelatedArticles { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        try
        {
            var article = await _newsClientService.GetByIdAsync(id, cancellationToken);

            // FUN-016: AC 2 - Inactive detail trả 404
            if (article == null || article.NewsStatus != true)
            {
                return NotFound();
            }

            Article = article;

            // Tải bài viết liên quan cùng chuyên mục (tối đa 3 bài, loại trừ bài hiện tại)
            if (article.CategoryId.HasValue && article.CategoryId.Value > 0)
            {
                try
                {
                    var safeId = ODataFilterHelper.EscapeStringLiteral(article.NewsArticleId);
                    var relatedQuery = $"$filter=categoryId eq {article.CategoryId.Value} and newsArticleId ne '{safeId}' and newsStatus eq true&$top=3&$orderby=createdDate desc";
                    var relatedEnv = await _newsClientService.GetNewsArticlesAsync(relatedQuery, cancellationToken);
                    RelatedArticles = relatedEnv?.Value ?? new List<NewsArticleApiModel>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load related articles for article {Id}.", id);
                    RelatedArticles = new List<NewsArticleApiModel>();
                }
            }

            return Page();
        }
        catch (FUNewsApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // FUN-016: AC 2 - Inactive detail hoặc không tồn tại trả 404
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading news article detail {Id}.", id);
            return NotFound();
        }
    }
}
