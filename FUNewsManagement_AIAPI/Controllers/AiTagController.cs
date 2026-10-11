using FUNewsManagement_AIAPI.Models;
using FUNewsManagement_AIAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsManagement_AIAPI.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize(Roles = "Staff,Admin")]
public class AiTagController : ControllerBase
{
    private readonly ITagExtractorService _tagExtractorService;
    private readonly ILearningCacheService _learningCacheService;

    public AiTagController(
        ITagExtractorService tagExtractorService,
        ILearningCacheService learningCacheService)
    {
        _tagExtractorService = tagExtractorService ?? throw new ArgumentNullException(nameof(tagExtractorService));
        _learningCacheService = learningCacheService ?? throw new ArgumentNullException(nameof(learningCacheService));
    }

    [HttpPost("suggest-tags")]
    public async Task<ActionResult<TagSuggestionResponseDto>> SuggestTags(
        [FromBody] AiTagRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dữ liệu không hợp lệ",
                Detail = "Nội dung bài viết để phân tích từ khóa không được để trống."
            });
        }

        var suggestions = await _tagExtractorService.SuggestTagsAsync(request.Content, cancellationToken);
        return Ok(new TagSuggestionResponseDto
        {
            SuggestedTags = suggestions
        });
    }

    [HttpPost("tag-feedback")]
    public async Task<ActionResult<TagFeedbackResponseDto>> TagFeedback(
        [FromBody] TagFeedbackRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dữ liệu không hợp lệ",
                Detail = "Yêu cầu phản hồi không được để trống."
            });
        }

        var tagIds = request.GetTagIds();
        if (tagIds.Count == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dữ liệu không hợp lệ",
                Detail = "Vui lòng cung cấp ít nhất một Tag ID hợp lệ để ghi nhận phản hồi học tập."
            });
        }

        await _learningCacheService.RecordFeedbackAsync(tagIds, cancellationToken);

        return Ok(new TagFeedbackResponseDto
        {
            Message = "Feedback recorded in Learning Cache",
            RecordedTagIds = tagIds
        });
    }
}
