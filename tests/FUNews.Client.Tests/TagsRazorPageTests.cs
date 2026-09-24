using System.Reflection;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using ManhMD_SE1930_A01_FE.Pages.Staff;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FUNews.Client.Tests;

public class TagsRazorPageTests
{
    private class FakeTagClientService : ITagClientService
    {
        public List<TagApiModel> Tags { get; set; } = new()
        {
            new TagApiModel { TagId = 1, TagName = "AI", Note = "Trí tuệ nhân tạo", ArticleCount = 3 },
            new TagApiModel { TagId = 2, TagName = "DotNet", Note = ".NET 8 ecosystem", ArticleCount = 2 },
            new TagApiModel { TagId = 3, TagName = "EmptyTag", Note = "Chưa gắn bài", ArticleCount = 0 }
        };

        public List<NewsArticleApiModel> Articles { get; set; } = new()
        {
            new NewsArticleApiModel
            {
                NewsArticleId = "NEWS_01",
                NewsTitle = "Khám phá AI 2026",
                Headline = "Đột phá mới trong xử lý ngôn ngữ",
                CategoryName = "Công nghệ",
                NewsStatus = true,
                CreatedDate = DateTime.Now
            }
        };

        public bool ShouldFailGet { get; set; }
        public bool ShouldFailCreate { get; set; }
        public string? FailCreateMessage { get; set; }
        public Dictionary<string, string[]>? FailCreateErrors { get; set; }

        public bool ShouldFailUpdate { get; set; }
        public string? FailUpdateMessage { get; set; }
        public Dictionary<string, string[]>? FailUpdateErrors { get; set; }

        public bool ShouldFailDelete { get; set; }
        public string? FailDeleteMessage { get; set; }

        public Task<ODataEnvelope<TagApiModel>> GetTagsAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            if (ShouldFailGet)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.InternalServerError, "Không thể kết nối đến máy chủ.");
            }

            return Task.FromResult(new ODataEnvelope<TagApiModel>
            {
                Value = Tags,
                Count = Tags.Count
            });
        }

        public Task<TagApiModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Tags.FirstOrDefault(t => t.TagId == id));
        }

        public Task<TagApiModel> CreateTagAsync(CreateTagApiModel request, CancellationToken cancellationToken = default)
        {
            if (ShouldFailCreate)
            {
                var problem = new ApiProblemDetails
                {
                    Title = "Dữ liệu không hợp lệ",
                    Detail = FailCreateMessage ?? "Tên thẻ tin đã tồn tại.",
                    Errors = FailCreateErrors
                };
                throw new FUNewsApiException(
                    System.Net.HttpStatusCode.BadRequest,
                    FailCreateMessage ?? "Tên thẻ tin đã tồn tại.",
                    problem);
            }

            var newTag = new TagApiModel
            {
                TagId = 100,
                TagName = request.TagName,
                Note = request.Note,
                ArticleCount = 0
            };
            Tags.Add(newTag);
            return Task.FromResult(newTag);
        }

        public Task<TagApiModel> UpdateTagAsync(int id, UpdateTagApiModel request, CancellationToken cancellationToken = default)
        {
            if (ShouldFailUpdate)
            {
                var problem = new ApiProblemDetails
                {
                    Title = "Dữ liệu không hợp lệ",
                    Detail = FailUpdateMessage ?? "Tên thẻ tin đã tồn tại.",
                    Errors = FailUpdateErrors
                };
                throw new FUNewsApiException(
                    System.Net.HttpStatusCode.BadRequest,
                    FailUpdateMessage ?? "Tên thẻ tin đã tồn tại.",
                    problem);
            }

            var existing = Tags.FirstOrDefault(t => t.TagId == id) ?? new TagApiModel { TagId = id };
            existing.TagName = request.TagName;
            existing.Note = request.Note;
            return Task.FromResult(existing);
        }

        public Task DeleteTagAsync(int id, CancellationToken cancellationToken = default)
        {
            if (ShouldFailDelete)
            {
                throw new FUNewsApiException(
                    System.Net.HttpStatusCode.Conflict,
                    FailDeleteMessage ?? "Không thể xóa thẻ tin vì đang được gắn với bài viết.");
            }

            Tags.RemoveAll(t => t.TagId == id);
            return Task.CompletedTask;
        }

        public Task<List<NewsArticleApiModel>> GetArticlesByTagAsync(int tagId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Articles);
        }
    }

    private static TagsModel CreatePageModel(ITagClientService tagService)
    {
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        var pageContext = new PageContext(actionContext);

        return new TagsModel(tagService, NullLogger<TagsModel>.Instance)
        {
            PageContext = pageContext
        };
    }

    private static object? GetPropertyValue(object obj, string propertyName)
    {
        var type = obj.GetType();
        var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return prop?.GetValue(obj);
    }

    [Fact]
    public async Task OnGetAsync_LoadsTags_Successfully()
    {
        var fakeService = new FakeTagClientService();
        var pageModel = CreatePageModel(fakeService);

        await pageModel.OnGetAsync();

        Assert.NotNull(pageModel.Tags);
        Assert.Equal(3, pageModel.Tags.Count);
        Assert.Equal(3, pageModel.TotalCount);
        Assert.Null(pageModel.ErrorMessage);
    }

    [Fact]
    public async Task OnGetAsync_HandlesApiError_SetsErrorMessage_Gracefully()
    {
        var fakeService = new FakeTagClientService { ShouldFailGet = true };
        var pageModel = CreatePageModel(fakeService);

        await pageModel.OnGetAsync();

        Assert.Empty(pageModel.Tags);
        Assert.Equal(0, pageModel.TotalCount);
        Assert.NotNull(pageModel.ErrorMessage);
        Assert.Contains("Không thể tải danh sách thẻ tin tức", pageModel.ErrorMessage);
    }

    [Fact]
    public async Task OnGetListAsync_ReturnsJsonWithTags()
    {
        var fakeService = new FakeTagClientService();
        var pageModel = CreatePageModel(fakeService);

        var result = await pageModel.OnGetListAsync("AI", CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var success = GetPropertyValue(jsonResult.Value, "success");
        Assert.Equal(true, success);

        var total = GetPropertyValue(jsonResult.Value, "totalCount");
        Assert.Equal(3, total);
    }

    [Fact]
    public async Task OnGetArticlesAsync_ReturnsJsonWithArticles()
    {
        var fakeService = new FakeTagClientService();
        var pageModel = CreatePageModel(fakeService);

        var result = await pageModel.OnGetArticlesAsync(1, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var success = GetPropertyValue(jsonResult.Value, "success");
        Assert.Equal(true, success);

        var articles = GetPropertyValue(jsonResult.Value, "articles") as List<NewsArticleApiModel>;
        Assert.NotNull(articles);
        Assert.Single(articles);
        Assert.Equal("NEWS_01", articles[0].NewsArticleId);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithValidData_ReturnsSuccessJson()
    {
        var fakeService = new FakeTagClientService();
        var pageModel = CreatePageModel(fakeService);

        var input = new TagsModel.CreateTagInputModel
        {
            TagName = "MachineLearning",
            Note = "ML và thống kê"
        };

        var result = await pageModel.OnPostCreateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var success = GetPropertyValue(jsonResult.Value, "success");
        Assert.Equal(true, success);

        var created = GetPropertyValue(jsonResult.Value, "tag") as TagApiModel;
        Assert.NotNull(created);
        Assert.Equal("MachineLearning", created.TagName);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithDuplicateName_HandlesApiException_ReturnsErrorJson()
    {
        var fakeService = new FakeTagClientService
        {
            ShouldFailCreate = true,
            FailCreateMessage = "Tên thẻ tin đã tồn tại trong hệ thống.",
            FailCreateErrors = new Dictionary<string, string[]>
            {
                { "TagName", new[] { "Tên thẻ tin đã tồn tại trong hệ thống." } }
            }
        };

        var pageModel = CreatePageModel(fakeService);

        var input = new TagsModel.CreateTagInputModel
        {
            TagName = "AI",
            Note = "Trùng tên"
        };

        var result = await pageModel.OnPostCreateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var success = GetPropertyValue(jsonResult.Value, "success");
        Assert.Equal(false, success);

        var message = GetPropertyValue(jsonResult.Value, "message") as string;
        Assert.Equal("Tên thẻ tin đã tồn tại trong hệ thống.", message);
    }

    [Fact]
    public async Task OnPostUpdateAsync_WithValidData_ReturnsSuccessJson()
    {
        var fakeService = new FakeTagClientService();
        var pageModel = CreatePageModel(fakeService);

        var input = new TagsModel.UpdateTagInputModel
        {
            TagId = 1,
            TagName = "AI & LLM",
            Note = "Mô tả mới"
        };

        var result = await pageModel.OnPostUpdateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var success = GetPropertyValue(jsonResult.Value, "success");
        Assert.Equal(true, success);

        var updated = GetPropertyValue(jsonResult.Value, "tag") as TagApiModel;
        Assert.NotNull(updated);
        Assert.Equal("AI & LLM", updated.TagName);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenHasArticles_ReturnsConflictErrorJson()
    {
        var fakeService = new FakeTagClientService
        {
            ShouldFailDelete = true,
            FailDeleteMessage = "Không thể xóa thẻ tin vì đang được gắn với bài viết."
        };

        var pageModel = CreatePageModel(fakeService);

        var result = await pageModel.OnPostDeleteAsync(1, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var success = GetPropertyValue(jsonResult.Value, "success");
        Assert.Equal(false, success);

        var message = GetPropertyValue(jsonResult.Value, "message") as string;
        Assert.Contains("Không thể xóa thẻ tin", message);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenEmpty_ReturnsSuccessJson()
    {
        var fakeService = new FakeTagClientService();
        var pageModel = CreatePageModel(fakeService);

        var result = await pageModel.OnPostDeleteAsync(3, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var success = GetPropertyValue(jsonResult.Value, "success");
        Assert.Equal(true, success);
    }

    [Fact]
    public void BuildTagsQuery_WithSearchTerm_GeneratesNullSafeNoteFilter()
    {
        var query = FUNews.Client.BusinessLogic.Helpers.ODataFilterHelper.BuildTagsQuery("tech");

        Assert.Contains("(contains(tagName,'tech') or (note ne null and contains(note,'tech')))", query);
        Assert.Contains("$orderby=tagId desc", query);
        Assert.Contains("$count=true", query);
    }

    [Fact]
    public void BuildTagsQuery_WithSpecialCharacters_EscapesQuotesCorrectly()
    {
        var query = FUNews.Client.BusinessLogic.Helpers.ODataFilterHelper.BuildTagsQuery("O'Reilly");

        Assert.Contains("O''Reilly", query);
        Assert.Contains("note ne null and contains(note,'O''Reilly')", query);
    }
}
