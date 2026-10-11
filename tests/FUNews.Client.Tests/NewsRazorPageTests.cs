using FUNews.Client.BusinessLogic.Helpers;
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

public class NewsRazorPageTests
{
    private class FakeNewsClientService : INewsClientService
    {
        public List<NewsArticleApiModel> Articles { get; set; } = new()
        {
            new NewsArticleApiModel
            {
                NewsArticleId = "ART_01",
                NewsTitle = "Khám phá AI 2026",
                Headline = "Đột phá mới trong xử lý ngôn ngữ",
                CategoryId = 1,
                CategoryName = "Công nghệ",
                NewsStatus = true,
                CreatedById = 3,
                AuthorName = "Isabella David",
                CreatedDate = DateTime.Now
            },
            new NewsArticleApiModel
            {
                NewsArticleId = "ART_02",
                NewsTitle = "Hội thảo .NET 8",
                Headline = "Cập nhật tính năng C# 12",
                CategoryId = 2,
                CategoryName = "Sự kiện",
                NewsStatus = false,
                CreatedById = 3,
                AuthorName = "Isabella David",
                CreatedDate = DateTime.Now.AddDays(-1)
            }
        };

        public bool ShouldFailGet { get; set; }
        public bool ShouldFailCreate { get; set; }
        public bool ShouldFailUpdate { get; set; }
        public Dictionary<string, string[]>? UpdateValidationErrors { get; set; }

        public Task<ODataEnvelope<NewsArticleApiModel>> GetNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            if (ShouldFailGet)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.InternalServerError, "Không thể kết nối đến máy chủ.");
            }

            return Task.FromResult(new ODataEnvelope<NewsArticleApiModel>
            {
                Count = Articles.Count,
                Value = Articles
            });
        }

        public Task<NewsArticleApiModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var art = Articles.FirstOrDefault(a => a.NewsArticleId == id);
            return Task.FromResult(art);
        }

        public Task<NewsArticleApiModel> CreateNewsArticleAsync(CreateNewsArticleApiModel request, CancellationToken cancellationToken = default)
        {
            if (ShouldFailCreate)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ.");
            }

            var created = new NewsArticleApiModel
            {
                NewsArticleId = $"N{Articles.Count + 1}",
                NewsTitle = request.NewsTitle,
                Headline = request.Headline,
                NewsContent = request.NewsContent,
                NewsSource = request.NewsSource,
                CategoryId = request.CategoryId,
                NewsStatus = request.NewsStatus ?? true,
                CreatedDate = DateTime.Now
            };
            Articles.Add(created);
            return Task.FromResult(created);
        }

        public Task<NewsArticleApiModel> UpdateNewsArticleAsync(string id, UpdateNewsArticleApiModel request, CancellationToken cancellationToken = default)
        {
            if (ShouldFailUpdate)
            {
                var problem = new ApiProblemDetails
                {
                    Status = 400,
                    Title = "Dữ liệu không hợp lệ",
                    Detail = "Không thể chuyển bài viết sang chuyên mục đã bị tạm ẩn.",
                    Errors = UpdateValidationErrors ?? new Dictionary<string, string[]>
                    {
                        ["CategoryId"] = new[] { "Không thể chuyển bài viết sang chuyên mục đã bị tạm ẩn." }
                    }
                };
                throw new FUNewsApiException(System.Net.HttpStatusCode.BadRequest, problem.Detail, problem);
            }
            var article = Articles.FirstOrDefault(a => a.NewsArticleId == id);
            if (article == null)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.NotFound, $"Không tìm thấy bài viết mã '{id}'.");
            }

            article.NewsTitle = request.NewsTitle;
            article.Headline = request.Headline;
            article.NewsContent = request.NewsContent;
            article.NewsSource = request.NewsSource;
            article.CategoryId = request.CategoryId;
            article.NewsStatus = request.NewsStatus ?? article.NewsStatus;
            article.ModifiedDate = DateTime.Now;
            return Task.FromResult(article);
        }

        public Task DeleteNewsArticleAsync(string id, CancellationToken cancellationToken = default)
        {
            var article = Articles.FirstOrDefault(a => a.NewsArticleId == id);
            if (article == null)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.NotFound, $"Không tìm thấy bài viết mã '{id}'.");
            }

            Articles.Remove(article);
            return Task.CompletedTask;
        }

        public Task<NewsArticleApiModel> DuplicateNewsArticleAsync(string id, CancellationToken cancellationToken = default)
        {
            var source = Articles.FirstOrDefault(a => a.NewsArticleId == id);
            if (source == null)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.NotFound, $"Không tìm thấy bài viết nguồn mã '{id}'.");
            }

            var duplicated = new NewsArticleApiModel
            {
                NewsArticleId = $"N{Articles.Count + 1}",
                NewsTitle = source.NewsTitle,
                Headline = source.Headline,
                NewsContent = source.NewsContent,
                NewsSource = source.NewsSource,
                CategoryId = source.CategoryId,
                CategoryName = source.CategoryName,
                NewsStatus = false, // Luôn Inactive theo AC 2
                CreatedById = 3,
                AuthorName = "Isabella David",
                CreatedDate = DateTime.Now,
                UpdatedById = null,
                ModifiedDate = null,
                Tags = source.Tags?.ToList() ?? new List<TagApiModel>()
            };
            Articles.Add(duplicated);
            return Task.FromResult(duplicated);
        }

        public Task<ODataEnvelope<NewsArticleApiModel>> GetMyNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            if (ShouldFailGet)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.InternalServerError, "Không thể kết nối đến máy chủ.");
            }

            // Only return articles created by current staff (3)
            var myArticles = Articles.Where(a => a.CreatedById == 3).ToList();
            return Task.FromResult(new ODataEnvelope<NewsArticleApiModel>
            {
                Count = myArticles.Count,
                Value = myArticles
            });
        }

        public Task<List<NewsArticleApiModel>> GetRelatedNewsArticlesAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<NewsArticleApiModel>());
        }

        public Task<string> UploadNewsImageAsync(string articleId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            var article = Articles.FirstOrDefault(a => a.NewsArticleId == articleId);
            var url = $"/uploads/news/{fileName}";
            if (article != null)
            {
                article.NewsImage = url;
            }
            return Task.FromResult(url);
        }
    }

    private class FakeCategoryClientService : ICategoryClientService
    {
        public Task<ODataEnvelope<CategoryApiModel>> GetCategoriesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ODataEnvelope<CategoryApiModel>
            {
                Count = 2,
                Value = new List<CategoryApiModel>
                {
                    new CategoryApiModel { CategoryId = 1, CategoryName = "Công nghệ", IsActive = true },
                    new CategoryApiModel { CategoryId = 2, CategoryName = "Sự kiện", IsActive = true }
                }
            });
        }

        public Task<CategoryApiModel?> GetByIdAsync(short id, CancellationToken cancellationToken = default) => Task.FromResult<CategoryApiModel?>(null);
        public Task<CategoryApiModel> CreateCategoryAsync(CreateCategoryApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CategoryApiModel> UpdateCategoryAsync(short id, UpdateCategoryApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteCategoryAsync(short id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeTagClientService : ITagClientService
    {
        public Task<ODataEnvelope<TagApiModel>> GetTagsAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ODataEnvelope<TagApiModel>
            {
                Count = 2,
                Value = new List<TagApiModel>
                {
                    new TagApiModel { TagId = 1, TagName = "AI" },
                    new TagApiModel { TagId = 2, TagName = "DotNet" }
                }
            });
        }

        public Task<TagApiModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<TagApiModel?>(null);
        public Task<TagApiModel> CreateTagAsync(CreateTagApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TagApiModel> UpdateTagAsync(int id, UpdateTagApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteTagAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<NewsArticleApiModel>> GetArticlesByTagAsync(int tagId, CancellationToken cancellationToken = default) => Task.FromResult(new List<NewsArticleApiModel>());
    }

    private class FakeAiClientService : IAiClientService
    {
        public List<TagSuggestionApiModel> SuggestionsToReturn { get; set; } = new()
        {
            new TagSuggestionApiModel { TagId = 1, TagName = "C#", Confidence = 0.95 },
            new TagSuggestionApiModel { TagId = 2, TagName = ".NET", Confidence = 0.85 }
        };

        public List<int> SubmittedFeedbackTagIds { get; } = new();

        public Task<List<TagSuggestionApiModel>> SuggestTagsAsync(string content, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(content)) return Task.FromResult(new List<TagSuggestionApiModel>());
            return Task.FromResult(SuggestionsToReturn);
        }

        public Task SubmitTagFeedbackAsync(int tagId, string? articleId = null, CancellationToken cancellationToken = default)
        {
            SubmittedFeedbackTagIds.Add(tagId);
            return Task.CompletedTask;
        }

        public Task SubmitMultipleTagFeedbacksAsync(IEnumerable<int> tagIds, string? articleId = null, CancellationToken cancellationToken = default)
        {
            if (tagIds != null) SubmittedFeedbackTagIds.AddRange(tagIds);
            return Task.CompletedTask;
        }
    }

    private NewsModel CreatePageModel(
        INewsClientService newsService,
        ICategoryClientService? catService = null,
        ITagClientService? tagService = null,
        IAiClientService? aiService = null)
    {
        catService ??= new FakeCategoryClientService();
        tagService ??= new FakeTagClientService();
        aiService ??= new FakeAiClientService();
        var model = new NewsModel(newsService, catService, tagService, aiService, NullLogger<NewsModel>.Instance);

        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        model.PageContext = new PageContext(actionContext);

        return model;
    }

    [Fact]
    public void BuildNewsQuery_WithAllFilters_GeneratesCorrectODataQuery()
    {
        var startDate = new DateTime(2026, 1, 10);
        var endDate = new DateTime(2026, 1, 20);

        var query = ODataFilterHelper.BuildNewsQuery(
            searchTerm: "AI",
            categoryId: 1,
            statusFilter: "active",
            authorTerm: "David",
            startDate: startDate,
            endDate: endDate,
            sortBy: "date_desc",
            top: 20,
            skip: 10
        );

        Assert.Contains("(contains(newsTitle,'AI') or contains(headline,'AI'))", query);
        Assert.Contains("categoryId eq 1", query);
        Assert.Contains("newsStatus eq true", query);
        Assert.Contains("(authorName ne null and contains(authorName,'David'))", query);
        Assert.Contains("createdDate ge 2026-01-10T00:00:00Z", query);
        Assert.Contains("createdDate lt 2026-01-21T00:00:00Z", query); // AC 3: Inclusive (endDate + 1)
        Assert.Contains("$orderby=createdDate desc,newsArticleId desc", query); // AC 4: Sort ổn định
        Assert.Contains("$top=20", query);
        Assert.Contains("$skip=10", query);
        Assert.Contains("$count=true", query); // AC 5: Count
    }

    [Fact]
    public void BuildNewsQuery_SortOptions_GenerateStableOrderings()
    {
        var qDesc = ODataFilterHelper.BuildNewsQuery(sortBy: "date_desc");
        Assert.Contains("$orderby=createdDate desc,newsArticleId desc", qDesc);

        var qAsc = ODataFilterHelper.BuildNewsQuery(sortBy: "date_asc");
        Assert.Contains("$orderby=createdDate asc,newsArticleId asc", qAsc);

        var qTitleAsc = ODataFilterHelper.BuildNewsQuery(sortBy: "title_asc");
        Assert.Contains("$orderby=newsTitle asc,newsArticleId asc", qTitleAsc);

        var qTitleDesc = ODataFilterHelper.BuildNewsQuery(sortBy: "title_desc");
        Assert.Contains("$orderby=newsTitle desc,newsArticleId desc", qTitleDesc);
    }

    [Fact]
    public async Task OnGetAsync_LoadsArticlesAndCategoriesSuccessfully()
    {
        var fakeNews = new FakeNewsClientService();
        var fakeCat = new FakeCategoryClientService();
        var pageModel = CreatePageModel(fakeNews, fakeCat);

        await pageModel.OnGetAsync(CancellationToken.None);

        Assert.Null(pageModel.ErrorMessage);
        Assert.Equal(2, pageModel.Categories.Count);
        Assert.Equal(2, pageModel.NewsArticles.Count);
        Assert.Equal(2, pageModel.TotalCount);
        Assert.Equal(1, pageModel.TotalPages);
    }

    [Fact]
    public async Task OnGetAsync_WhenStartDateAfterEndDate_SetsErrorMessage()
    {
        var fakeNews = new FakeNewsClientService();
        var pageModel = CreatePageModel(fakeNews);

        pageModel.StartDate = new DateTime(2026, 5, 20);
        pageModel.EndDate = new DateTime(2026, 5, 10); // Start > End

        await pageModel.OnGetAsync(CancellationToken.None);

        Assert.NotNull(pageModel.ErrorMessage);
        Assert.Contains("Khoảng ngày không hợp lệ", pageModel.ErrorMessage);
        Assert.Empty(pageModel.NewsArticles);
    }

    [Fact]
    public async Task OnGetDetailAsync_WhenArticleExists_ReturnsSuccessJson()
    {
        var fakeNews = new FakeNewsClientService();
        var pageModel = CreatePageModel(fakeNews);

        var result = await pageModel.OnGetDetailAsync("ART_01", CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(true, successProp.GetValue(jsonResult.Value));

        var articleProp = type.GetProperty("article");
        Assert.NotNull(articleProp);
        var article = articleProp.GetValue(jsonResult.Value) as NewsArticleApiModel;
        Assert.NotNull(article);
        Assert.Equal("ART_01", article.NewsArticleId);
        Assert.Equal("Khám phá AI 2026", article.NewsTitle);
    }

    [Fact]
    public async Task OnGetDetailAsync_WhenNotFound_ReturnsErrorJson()
    {
        var fakeNews = new FakeNewsClientService();
        var pageModel = CreatePageModel(fakeNews);

        var result = await pageModel.OnGetDetailAsync("NON_EXISTENT", CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(false, successProp.GetValue(jsonResult.Value));
    }

    [Fact]
    public async Task OnPostCreateAsync_WithValidInput_ReturnsSuccessJson()
    {
        var fakeNews = new FakeNewsClientService();
        var pageModel = CreatePageModel(fakeNews);

        var input = new CreateNewsArticleInputModel
        {
            Headline = "Tóm tắt bài viết mới",
            NewsTitle = "Tiêu đề bài viết mới",
            CategoryId = 1,
            NewsStatus = true,
            NewsSource = "FU News",
            NewsContent = "Nội dung bài viết chi tiết",
            TagIds = new List<int> { 1, 2 }
        };

        var result = await pageModel.OnPostCreateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(true, successProp.GetValue(jsonResult.Value));

        var articleProp = type.GetProperty("article");
        Assert.NotNull(articleProp);
        var created = articleProp.GetValue(jsonResult.Value) as NewsArticleApiModel;
        Assert.NotNull(created);
        Assert.Equal("Tiêu đề bài viết mới", created.NewsTitle);
        Assert.Equal("Tóm tắt bài viết mới", created.Headline);
        Assert.Equal((short)1, created.CategoryId);
        Assert.True(created.NewsStatus);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithInvalidModelState_ReturnsValidationErrorJson()
    {
        var fakeNews = new FakeNewsClientService();
        var pageModel = CreatePageModel(fakeNews);
        pageModel.ModelState.AddModelError("Headline", "Tiêu đề tóm tắt (Headline) là bắt buộc.");

        var input = new CreateNewsArticleInputModel
        {
            Headline = "",
            CategoryId = 1
        };

        var result = await pageModel.OnPostCreateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(false, successProp.GetValue(jsonResult.Value));

        var errorsProp = type.GetProperty("errors");
        Assert.NotNull(errorsProp);
        var errors = errorsProp.GetValue(jsonResult.Value) as IDictionary<string, string[]>;
        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("Headline") || errors.ContainsKey("headline"));
    }

    [Fact]
    public async Task OnPostCreateAsync_WhenApiThrowsException_ReturnsNormalizedErrors()
    {
        var fakeNews = new FakeNewsClientService { ShouldFailCreate = true };
        var pageModel = CreatePageModel(fakeNews);

        var input = new CreateNewsArticleInputModel
        {
            Headline = "Tóm tắt hợp lệ",
            CategoryId = 1
        };

        var result = await pageModel.OnPostCreateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(false, successProp.GetValue(jsonResult.Value));

        var msgProp = type.GetProperty("message");
        Assert.NotNull(msgProp);
        Assert.Contains("Dữ liệu không hợp lệ", msgProp.GetValue(jsonResult.Value)?.ToString());
    }

    [Fact]
    public async Task OnGetDetailAsync_WhenArticleExists_ReturnsArticleDetails()
    {
        var fakeNews = new FakeNewsClientService();
        var pageModel = CreatePageModel(fakeNews);

        var result = await pageModel.OnGetDetailAsync("ART_01", CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(true, successProp.GetValue(jsonResult.Value));

        var articleProp = type.GetProperty("article");
        Assert.NotNull(articleProp);
        var article = articleProp.GetValue(jsonResult.Value) as NewsArticleApiModel;
        Assert.NotNull(article);
        Assert.Equal("ART_01", article.NewsArticleId);
    }

    [Fact]
    public async Task OnPostUpdateAsync_WithValidModel_UpdatesAndReturnsSuccess()
    {
        var fakeNews = new FakeNewsClientService();
        var pageModel = CreatePageModel(fakeNews);

        var input = new UpdateNewsArticleInputModel
        {
            NewsArticleId = "ART_01",
            Headline = "Updated Headline Test",
            NewsTitle = "Updated Title Test",
            CategoryId = 1,
            NewsStatus = true,
            TagIds = new List<int> { 1, 2 }
        };

        var result = await pageModel.OnPostUpdateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(true, successProp.GetValue(jsonResult.Value));

        var articleProp = type.GetProperty("article");
        Assert.NotNull(articleProp);
        var updated = articleProp.GetValue(jsonResult.Value) as NewsArticleApiModel;
        Assert.NotNull(updated);
        Assert.Equal("Updated Headline Test", updated.Headline);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WithValidId_DeletesAndReturnsSuccess()
    {
        var fakeNews = new FakeNewsClientService();
        var pageModel = CreatePageModel(fakeNews);

        var input = new DeleteNewsArticleInputModel
        {
            NewsArticleId = "ART_01"
        };

        var result = await pageModel.OnPostDeleteAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(true, successProp.GetValue(jsonResult.Value));

        // Confirm deleted from service
        Assert.DoesNotContain(fakeNews.Articles, a => a.NewsArticleId == "ART_01");
    }

    [Fact]
    public async Task OnPostUpdateAsync_WhenModelStateIsInvalid_ReturnsValidationErrors()
    {
        var fakeNews = new FakeNewsClientService();
        var pageModel = CreatePageModel(fakeNews);
        pageModel.ModelState.AddModelError("Headline", "Tiêu đề tóm tắt (Headline) là bắt buộc.");

        var input = new UpdateNewsArticleInputModel
        {
            NewsArticleId = "ART_01",
            Headline = "",
            CategoryId = 1
        };

        var result = await pageModel.OnPostUpdateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(false, successProp.GetValue(jsonResult.Value));

        var errorsProp = type.GetProperty("errors");
        Assert.NotNull(errorsProp);
        var errors = errorsProp.GetValue(jsonResult.Value) as IDictionary<string, string[]>;
        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("Headline") || errors.ContainsKey("headline"));
    }

    [Fact]
    public async Task OnPostUpdateAsync_WhenApiThrowsException_ReturnsNormalizedErrors()
    {
        var fakeNews = new FakeNewsClientService { ShouldFailUpdate = true };
        var pageModel = CreatePageModel(fakeNews);

        var input = new UpdateNewsArticleInputModel
        {
            NewsArticleId = "ART_01",
            Headline = "Tiêu đề hợp lệ",
            CategoryId = 2
        };

        var result = await pageModel.OnPostUpdateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(false, successProp.GetValue(jsonResult.Value));

        var msgProp = type.GetProperty("message");
        Assert.NotNull(msgProp);
        Assert.Contains("Không thể chuyển bài viết", msgProp.GetValue(jsonResult.Value)?.ToString());

        var errorsProp = type.GetProperty("errors");
        Assert.NotNull(errorsProp);
        var errors = errorsProp.GetValue(jsonResult.Value) as IDictionary<string, string[]>;
        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("CategoryId") || errors.ContainsKey("categoryId"));
    }

    [Fact]
    public async Task OnPostDuplicateAsync_WithValidId_DuplicatesArticleAsInactiveAndReturnsSuccess()
    {
        var fakeNews = new FakeNewsClientService();
        var pageModel = CreatePageModel(fakeNews);

        var input = new DuplicateNewsArticleInputModel
        {
            NewsArticleId = "ART_01"
        };

        var result = await pageModel.OnPostDuplicateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(true, successProp.GetValue(jsonResult.Value));

        var articleProp = type.GetProperty("article");
        Assert.NotNull(articleProp);
        var duplicated = articleProp.GetValue(jsonResult.Value) as NewsArticleApiModel;
        Assert.NotNull(duplicated);

        // AC 1: ID mới
        Assert.NotEqual("ART_01", duplicated.NewsArticleId);
        // AC 2: Trạng thái Inactive
        Assert.False(duplicated.NewsStatus);
        // AC 3: Tác giả hiện tại
        Assert.Equal((short)3, duplicated.CreatedById);
        // AC 4: Audit update NULL
        Assert.Null(duplicated.UpdatedById);
        Assert.Null(duplicated.ModifiedDate);
        // Service lưu thêm bài mới
        Assert.Equal(3, fakeNews.Articles.Count);
    }

    [Fact]
    public async Task OnPostDuplicateAsync_WithEmptyId_ReturnsValidationError()
    {
        var fakeNews = new FakeNewsClientService();
        var pageModel = CreatePageModel(fakeNews);

        var input = new DuplicateNewsArticleInputModel
        {
            NewsArticleId = ""
        };

        var result = await pageModel.OnPostDuplicateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(false, successProp.GetValue(jsonResult.Value));

        var msgProp = type.GetProperty("message");
        Assert.NotNull(msgProp);
        Assert.Equal("Mã bài viết không hợp lệ.", msgProp.GetValue(jsonResult.Value));
    }

    [Fact]
    public async Task OnPostDuplicateAsync_WhenApiThrowsNotFound_ReturnsFailureJson()
    {
        var fakeNews = new FakeNewsClientService();
        var pageModel = CreatePageModel(fakeNews);

        var input = new DuplicateNewsArticleInputModel
        {
            NewsArticleId = "NON_EXISTING_ID"
        };

        var result = await pageModel.OnPostDuplicateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(false, successProp.GetValue(jsonResult.Value));

        var msgProp = type.GetProperty("message");
        Assert.NotNull(msgProp);
        Assert.Contains("Không tìm thấy", msgProp.GetValue(jsonResult.Value)?.ToString());
    }

    [Fact]
    public async Task OnPostSuggestTagsAsync_WithValidContent_ReturnsJsonWithSuggestions()
    {
        var fakeAi = new FakeAiClientService();
        var pageModel = CreatePageModel(new FakeNewsClientService(), aiService: fakeAi);

        var input = new AiSuggestTagsInputModel
        {
            Content = "Học lập trình C# và .NET 8 mới nhất năm 2026."
        };

        var result = await pageModel.OnPostSuggestTagsAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(true, successProp.GetValue(jsonResult.Value));

        var tagsProp = type.GetProperty("tags");
        Assert.NotNull(tagsProp);
        var tags = tagsProp.GetValue(jsonResult.Value) as List<TagSuggestionApiModel>;
        Assert.NotNull(tags);
        Assert.Equal(2, tags.Count);
        Assert.Equal("C#", tags[0].TagName);
        Assert.Equal(0.95, tags[0].Confidence);
    }

    [Fact]
    public async Task OnPostSuggestTagsAsync_WithEmptyContent_ReturnsValidationError()
    {
        var fakeAi = new FakeAiClientService();
        var pageModel = CreatePageModel(new FakeNewsClientService(), aiService: fakeAi);

        var input = new AiSuggestTagsInputModel
        {
            Content = "   "
        };

        var result = await pageModel.OnPostSuggestTagsAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(false, successProp.GetValue(jsonResult.Value));

        var msgProp = type.GetProperty("message");
        Assert.NotNull(msgProp);
        Assert.Contains("Vui lòng nhập nội dung", msgProp.GetValue(jsonResult.Value)?.ToString());
    }

    [Fact]
    public async Task OnPostTagFeedbackAsync_WithValidTagIds_SubmitsFeedback()
    {
        var fakeAi = new FakeAiClientService();
        var pageModel = CreatePageModel(new FakeNewsClientService(), aiService: fakeAi);

        var input = new AiFeedbackInputModel
        {
            TagIds = new List<int> { 1, 2 },
            ArticleId = "ART_01"
        };

        var result = await pageModel.OnPostTagFeedbackAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(true, successProp.GetValue(jsonResult.Value));

        Assert.Contains(1, fakeAi.SubmittedFeedbackTagIds);
        Assert.Contains(2, fakeAi.SubmittedFeedbackTagIds);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithSelectedTags_TriggersAiFeedbackLoop()
    {
        var fakeNews = new FakeNewsClientService();
        var fakeAi = new FakeAiClientService();
        var pageModel = CreatePageModel(fakeNews, aiService: fakeAi);

        var input = new CreateNewsArticleInputModel
        {
            NewsTitle = "Tin tức AI 2026",
            Headline = "Tiêu đề phụ bài viết AI",
            NewsContent = "Chi tiết nội dung...",
            CategoryId = 1,
            TagIds = new List<int> { 1, 2 }
        };

        var result = await pageModel.OnPostCreateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(true, successProp.GetValue(jsonResult.Value));

        Assert.Contains(1, fakeAi.SubmittedFeedbackTagIds);
        Assert.Contains(2, fakeAi.SubmittedFeedbackTagIds);
    }

    [Fact]
    public async Task OnPostUpdateAsync_WithSelectedTags_TriggersAiFeedbackLoop()
    {
        var fakeNews = new FakeNewsClientService();
        var fakeAi = new FakeAiClientService();
        var pageModel = CreatePageModel(fakeNews, aiService: fakeAi);

        var input = new UpdateNewsArticleInputModel
        {
            NewsArticleId = "ART_01",
            NewsTitle = "Cập nhật tin tức AI",
            Headline = "Tiêu đề phụ đã sửa",
            CategoryId = 1,
            TagIds = new List<int> { 3 }
        };

        var result = await pageModel.OnPostUpdateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(true, successProp.GetValue(jsonResult.Value));

        Assert.Contains(3, fakeAi.SubmittedFeedbackTagIds);
    }
}

