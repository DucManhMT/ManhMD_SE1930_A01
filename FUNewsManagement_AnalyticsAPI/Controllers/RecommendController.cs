using FUNewsManagement_AnalyticsAPI.Models;
using FUNewsManagement_AnalyticsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsManagement_AnalyticsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class RecommendController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public RecommendController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<List<RecommendedArticleDto>>> GetRecommendations(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetRecommendedArticlesAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy bài viết",
                Detail = $"Không tìm thấy bài viết với mã '{id}' để thực hiện gợi ý nội dung liên quan."
            });
        }

        return Ok(result);
    }
}
