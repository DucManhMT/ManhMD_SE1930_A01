using System.Net;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using ManhMD_SE1930_A01_FE.Pages.News;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FUNews.Client.Tests;

public class NewsPublicPageTests
{
    private class FakePublicNewsService : INewsClientService
    {
        public List<NewsArticleApiModel> Articles { get; set; } = new()
        {
            new NewsArticleApiModel
            {
                NewsArticleId = "PUB_01",
                NewsTitle = "Hội thảo Công nghệ 2026",
                Headline = "Đột phá AI trong giáo dục",
                NewsContent = "Chi tiết nội dung hội thảo công nghệ 2026.",
                NewsSource = "Khoa CNTT",
                CategoryId = 1,
                CategoryName = "Công nghệ",
                NewsStatus = true, // Active
                CreatedById = 3,
                AuthorName = "Isabella David",
                CreatedDate = DateTime.Now.AddDays(-1),
                Tags = new List<TagApiModel> { new() { TagId = 1, TagName = "AI" } }
            },
            new NewsArticleApiModel
            {
                NewsArticleId = "PUB_02",
                NewsTitle = "Bản nháp chưa duyệt",
                Headline = "Tin nội bộ chưa công bố",
                NewsContent = "Nội dung bí mật.",
                NewsSource = "Nội bộ",
                CategoryId = 1,
                CategoryName = "Công nghệ",
                NewsStatus = false, // Inactive
                CreatedById = 3,
                AuthorName = "Isabella David",
                CreatedDate = DateTime.Now.AddDays(-2)
            }
        };

        public Task<ODataEnvelope<NewsArticleApiModel>> GetNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            // Public endpoint always returns Active articles only
            var active = Articles.Where(a => a.NewsStatus == true).ToList();
            return Task.FromResult(new ODataEnvelope<NewsArticleApiModel>
            {
                Count = active.Count,
                Value = active
            });
        }

        public Task<NewsArticleApiModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var art = Articles.FirstOrDefault(a => a.NewsArticleId == id);
            if (art == null)
            {
                throw new FUNewsApiException(HttpStatusCode.NotFound, "Không tìm thấy bài viết.");
            }
            return Task.FromResult<NewsArticleApiModel?>(art);
        }

        public Task<NewsArticleApiModel> CreateNewsArticleAsync(CreateNewsArticleApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<NewsArticleApiModel> UpdateNewsArticleAsync(string id, UpdateNewsArticleApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteNewsArticleAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<NewsArticleApiModel> DuplicateNewsArticleAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ODataEnvelope<NewsArticleApiModel>> GetMyNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakePublicCategoryService : ICategoryClientService
    {
        public Task<ODataEnvelope<CategoryApiModel>> GetCategoriesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ODataEnvelope<CategoryApiModel>
            {
                Count = 1,
                Value = new List<CategoryApiModel>
                {
                    new() { CategoryId = 1, CategoryName = "Công nghệ", IsActive = true }
                }
            });
        }

        public Task<CategoryApiModel?> GetByIdAsync(short id, CancellationToken cancellationToken = default) => Task.FromResult<CategoryApiModel?>(null);
        public Task<CategoryApiModel> CreateCategoryAsync(CreateCategoryApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CategoryApiModel> UpdateCategoryAsync(short id, UpdateCategoryApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteCategoryAsync(short id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private IndexModel CreateIndexModel(INewsClientService newsService, ICategoryClientService? catService = null)
    {
        catService ??= new FakePublicCategoryService();
        var model = new IndexModel(newsService, catService, NullLogger<IndexModel>.Instance);
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), new ModelStateDictionary());
        model.PageContext = new PageContext(actionContext);
        return model;
    }

    private DetailModel CreateDetailModel(INewsClientService newsService)
    {
        var model = new DetailModel(newsService, NullLogger<DetailModel>.Instance);
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), new ModelStateDictionary());
        model.PageContext = new PageContext(actionContext);
        return model;
    }

    [Fact]
    public async Task Index_OnGetAsync_LoadsOnlyActiveArticlesAndCategories()
    {
        var fakeNews = new FakePublicNewsService();
        var pageModel = CreateIndexModel(fakeNews);

        await pageModel.OnGetAsync(CancellationToken.None);

        Assert.Single(pageModel.Categories);
        Assert.Single(pageModel.NewsArticles);
        Assert.Equal("PUB_01", pageModel.NewsArticles.First().NewsArticleId);
        Assert.True(pageModel.NewsArticles.First().NewsStatus);
        Assert.Equal(1, pageModel.TotalCount);
    }

    [Fact]
    public async Task Detail_OnGetAsync_WhenArticleIsActive_ReturnsPageResult()
    {
        var fakeNews = new FakePublicNewsService();
        var pageModel = CreateDetailModel(fakeNews);

        var result = await pageModel.OnGetAsync("PUB_01", CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(pageModel.Article);
        Assert.Equal("PUB_01", pageModel.Article.NewsArticleId);
        Assert.True(pageModel.Article.NewsStatus);
    }

    [Fact]
    public async Task Detail_OnGetAsync_WhenArticleIsInactive_ReturnsNotFound()
    {
        // AC 2: Inactive detail trả 404
        var fakeNews = new FakePublicNewsService();
        var pageModel = CreateDetailModel(fakeNews);

        var result = await pageModel.OnGetAsync("PUB_02", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Detail_OnGetAsync_WhenArticleDoesNotExist_ReturnsNotFound()
    {
        // AC 2: Không tồn tại trả 404
        var fakeNews = new FakePublicNewsService();
        var pageModel = CreateDetailModel(fakeNews);

        var result = await pageModel.OnGetAsync("NON_EXISTING_ID", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
