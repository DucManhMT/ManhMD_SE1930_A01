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

    private NewsModel CreatePageModel(INewsClientService newsService, ICategoryClientService? catService = null)
    {
        catService ??= new FakeCategoryClientService();
        var model = new NewsModel(newsService, catService, NullLogger<NewsModel>.Instance);

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
}
