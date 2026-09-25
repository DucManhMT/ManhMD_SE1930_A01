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

public class HistoryRazorPageTests
{
    private class FakeHistoryNewsService : INewsClientService
    {
        public List<NewsArticleApiModel> Articles { get; set; } = new()
        {
            new NewsArticleApiModel
            {
                NewsArticleId = "MY_01",
                NewsTitle = "Bài viết cá nhân 1",
                Headline = "Tóm tắt bài 1",
                CategoryId = 1,
                CategoryName = "Công nghệ",
                NewsStatus = true,
                CreatedById = 3,
                AuthorName = "Isabella David",
                CreatedDate = DateTime.Now.AddDays(-2),
                UpdatedById = 4,
                LastEditorName = "Michael Charlotte",
                ModifiedDate = DateTime.Now.AddDays(-1)
            },
            new NewsArticleApiModel
            {
                NewsArticleId = "MY_02",
                NewsTitle = "Bài viết cá nhân 2",
                Headline = "Tóm tắt bài 2",
                CategoryId = 2,
                CategoryName = "Sự kiện",
                NewsStatus = false,
                CreatedById = 3,
                AuthorName = "Isabella David",
                CreatedDate = DateTime.Now.AddDays(-5),
                UpdatedById = null,
                LastEditorName = null,
                ModifiedDate = null
            }
        };

        public bool ShouldFail { get; set; }

        public Task<ODataEnvelope<NewsArticleApiModel>> GetNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ODataEnvelope<NewsArticleApiModel> { Count = Articles.Count, Value = Articles });
        }

        public Task<NewsArticleApiModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Articles.FirstOrDefault(a => a.NewsArticleId == id));
        }

        public Task<NewsArticleApiModel> CreateNewsArticleAsync(CreateNewsArticleApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<NewsArticleApiModel> UpdateNewsArticleAsync(string id, UpdateNewsArticleApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteNewsArticleAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<NewsArticleApiModel> DuplicateNewsArticleAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<ODataEnvelope<NewsArticleApiModel>> GetMyNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            if (ShouldFail)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.InternalServerError, "Không thể kết nối đến máy chủ.");
            }

            return Task.FromResult(new ODataEnvelope<NewsArticleApiModel>
            {
                Count = Articles.Count,
                Value = Articles
            });
        }
    }

    private class FakeHistoryCategoryService : ICategoryClientService
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

    private HistoryModel CreatePageModel(INewsClientService newsService, ICategoryClientService? catService = null)
    {
        catService ??= new FakeHistoryCategoryService();
        var model = new HistoryModel(newsService, catService, NullLogger<HistoryModel>.Instance);

        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        model.PageContext = new PageContext(actionContext);

        return model;
    }

    [Fact]
    public async Task OnGetAsync_LoadsCategoriesAndCurrentStaffArticles()
    {
        var fakeNews = new FakeHistoryNewsService();
        var pageModel = CreatePageModel(fakeNews);

        await pageModel.OnGetAsync(CancellationToken.None);

        Assert.Equal(2, pageModel.Categories.Count);
        Assert.Equal(2, pageModel.NewsArticles.Count);
        Assert.Equal(2, pageModel.TotalCount);
        Assert.Null(pageModel.ErrorMessage);

        // Verify last modified info is loaded
        var artWithEdit = pageModel.NewsArticles.First(a => a.NewsArticleId == "MY_01");
        Assert.Equal("Michael Charlotte", artWithEdit.LastEditorName);
        Assert.NotNull(artWithEdit.ModifiedDate);
    }

    [Fact]
    public async Task OnGetAsync_WhenStartDateAfterEndDate_SetsErrorMessageAndEmptyList()
    {
        var fakeNews = new FakeHistoryNewsService();
        var pageModel = CreatePageModel(fakeNews);

        pageModel.StartDate = new DateTime(2026, 5, 10);
        pageModel.EndDate = new DateTime(2026, 5, 1);

        await pageModel.OnGetAsync(CancellationToken.None);

        Assert.NotNull(pageModel.ErrorMessage);
        Assert.Contains("Khoảng ngày không hợp lệ", pageModel.ErrorMessage);
        Assert.Empty(pageModel.NewsArticles);
        Assert.Equal(0, pageModel.TotalCount);
    }

    [Fact]
    public async Task OnGetDetailAsync_WithValidId_ReturnsArticleJson()
    {
        var fakeNews = new FakeHistoryNewsService();
        var pageModel = CreatePageModel(fakeNews);

        var result = await pageModel.OnGetDetailAsync("MY_01", CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        Assert.NotNull(successProp);
        Assert.Equal(true, successProp.GetValue(jsonResult.Value));

        var articleProp = type.GetProperty("article");
        Assert.NotNull(articleProp);
        var art = articleProp.GetValue(jsonResult.Value) as NewsArticleApiModel;
        Assert.NotNull(art);
        Assert.Equal("MY_01", art.NewsArticleId);
        Assert.Equal("Bài viết cá nhân 1", art.NewsTitle);
    }

    [Fact]
    public async Task OnGetDetailAsync_WithEmptyId_ReturnsInvalidIdJson()
    {
        var fakeNews = new FakeHistoryNewsService();
        var pageModel = CreatePageModel(fakeNews);

        var result = await pageModel.OnGetDetailAsync("", CancellationToken.None);

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
}
