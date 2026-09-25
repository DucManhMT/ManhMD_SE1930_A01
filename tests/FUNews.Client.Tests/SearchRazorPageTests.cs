using System.Security.Claims;
using FUNews.Client.BusinessLogic.Helpers;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Models;
using ManhMD_SE1930_A01_FE.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FUNews.Client.Tests;

public class SearchRazorPageTests
{
    private class FakeNewsClientService : INewsClientService
    {
        public string? LastCapturedQuery { get; private set; }
        public int CallCount { get; private set; }
        public ODataEnvelope<NewsArticleApiModel> ResponseToReturn { get; set; } = new()
        {
            Count = 0,
            Value = new List<NewsArticleApiModel>()
        };

        public Task<ODataEnvelope<NewsArticleApiModel>> GetNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastCapturedQuery = odataQuery;
            return Task.FromResult(ResponseToReturn);
        }

        public Task<ODataEnvelope<NewsArticleApiModel>> GetMyNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ODataEnvelope<NewsArticleApiModel>());

        public Task<NewsArticleApiModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<NewsArticleApiModel?>(null);

        public Task<NewsArticleApiModel> CreateNewsArticleAsync(CreateNewsArticleApiModel request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new NewsArticleApiModel());

        public Task<NewsArticleApiModel> UpdateNewsArticleAsync(string id, UpdateNewsArticleApiModel request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new NewsArticleApiModel());

        public Task DeleteNewsArticleAsync(string id, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<NewsArticleApiModel> DuplicateNewsArticleAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(new NewsArticleApiModel());
    }

    private class FakeCategoryClientService : ICategoryClientService
    {
        public Task<ODataEnvelope<CategoryApiModel>> GetCategoriesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ODataEnvelope<CategoryApiModel>
            {
                Value = new List<CategoryApiModel>
                {
                    new() { CategoryId = 1, CategoryName = "Công nghệ", IsActive = true },
                    new() { CategoryId = 2, CategoryName = "Đời sống", IsActive = true }
                }
            });
        }

        public Task<CategoryApiModel?> GetByIdAsync(short id, CancellationToken cancellationToken = default) => Task.FromResult<CategoryApiModel?>(null);
        public Task<CategoryApiModel> CreateCategoryAsync(CreateCategoryApiModel request, CancellationToken cancellationToken = default) => Task.FromResult(new CategoryApiModel());
        public Task<CategoryApiModel> UpdateCategoryAsync(short id, UpdateCategoryApiModel request, CancellationToken cancellationToken = default) => Task.FromResult(new CategoryApiModel());
        public Task DeleteCategoryAsync(short id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class FakeTagClientService : ITagClientService
    {
        public Task<ODataEnvelope<TagApiModel>> GetTagsAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ODataEnvelope<TagApiModel>
            {
                Value = new List<TagApiModel>
                {
                    new() { TagId = 1, TagName = "AI" },
                    new() { TagId = 2, TagName = "Cloud" }
                }
            });
        }

        public Task<TagApiModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<TagApiModel?>(null);
        public Task<TagApiModel> CreateTagAsync(CreateTagApiModel request, CancellationToken cancellationToken = default) => Task.FromResult(new TagApiModel());
        public Task<TagApiModel> UpdateTagAsync(int id, UpdateTagApiModel request, CancellationToken cancellationToken = default) => Task.FromResult(new TagApiModel());
        public Task DeleteTagAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<List<NewsArticleApiModel>> GetArticlesByTagAsync(int tagId, CancellationToken cancellationToken = default) => Task.FromResult(new List<NewsArticleApiModel>());
    }

    [Fact]
    public void BuildNewsQuery_WithContentAndTag_GeneratesCorrectODataQuery()
    {
        // AC 1, AC 2: Tìm kiếm nâng cao kết hợp keyword trên content, tagId và escape dấu nháy đơn
        var query = ODataFilterHelper.BuildNewsQuery(
            searchTerm: "O'Reilly & AI",
            categoryId: 1,
            statusFilter: "active",
            authorTerm: "Nguyễn Văn A",
            startDate: new DateTime(2026, 5, 1),
            endDate: new DateTime(2026, 5, 10),
            sortBy: "title_asc",
            top: 10,
            skip: 0,
            tagId: 2,
            includeContent: true
        );

        // Kiểm tra escape dấu nháy đơn
        Assert.Contains("O''Reilly & AI", query);
        // Kiểm tra tìm kiếm trên cả 3 trường title, headline và newsContent
        Assert.Contains("contains(newsTitle,'O''Reilly & AI')", query);
        Assert.Contains("contains(headline,'O''Reilly & AI')", query);
        Assert.Contains("contains(newsContent,'O''Reilly & AI')", query);
        // Kiểm tra categoryId
        Assert.Contains("categoryId eq 1", query);
        // Kiểm tra tag filter
        Assert.Contains("tags/any(t: t/tagId eq 2)", query);
        // Kiểm tra status
        Assert.Contains("newsStatus eq true", query);
        // Kiểm tra author
        Assert.Contains("contains(authorName,'Nguyễn Văn A')", query);
        // Kiểm tra date range
        Assert.Contains("createdDate ge 2026-05-01T00:00:00Z", query);
        Assert.Contains("createdDate lt 2026-05-11T00:00:00Z", query);
        // Kiểm tra sort ổn định
        Assert.Contains("$orderby=newsTitle asc,newsArticleId asc", query);
    }

    [Fact]
    public async Task SearchModel_AnonymousUser_EnforcesActiveStatus()
    {
        // AC 4: Anonymous người dùng có truyền statusFilter = "inactive" cũng bị cưỡng chế thành "active"
        var fakeNews = new FakeNewsClientService();
        var model = new SearchModel(
            fakeNews,
            new FakeCategoryClientService(),
            new FakeTagClientService(),
            NullLogger<SearchModel>.Instance)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext() // Anonymous user (no identity)
            },
            SearchTerm = "Machine Learning",
            StatusFilter = "inactive" // Thử cố tình truyền inactive
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.False(model.IsStaff);
        Assert.NotNull(fakeNews.LastCapturedQuery);
        // Truy vấn gửi xuống API phải bị ép thành newsStatus eq true
        Assert.Contains("newsStatus eq true", fakeNews.LastCapturedQuery);
        Assert.DoesNotContain("newsStatus eq false", fakeNews.LastCapturedQuery);
    }

    [Fact]
    public async Task SearchModel_StaffUser_AllowsStatusFiltering()
    {
        // AC 1: Staff có thể chọn lọc bài Inactive
        var fakeNews = new FakeNewsClientService
        {
            ResponseToReturn = new ODataEnvelope<NewsArticleApiModel>
            {
                Count = 1,
                Value = new List<NewsArticleApiModel>
                {
                    new() { NewsArticleId = "N01", NewsTitle = "Bài viết tạm ẩn", NewsStatus = false }
                }
            }
        };

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "Staff User"),
            new Claim(ClaimTypes.Role, "Staff")
        }, "TestAuth"));

        var httpContext = new DefaultHttpContext { User = user };

        var model = new SearchModel(
            fakeNews,
            new FakeCategoryClientService(),
            new FakeTagClientService(),
            NullLogger<SearchModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            StatusFilter = "inactive"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.True(model.IsStaff);
        Assert.NotNull(fakeNews.LastCapturedQuery);
        // Truy vấn gửi xuống API phải chứa newsStatus eq false
        Assert.Contains("newsStatus eq false", fakeNews.LastCapturedQuery);
        Assert.Equal(1, model.TotalCount);
    }

    [Fact]
    public async Task SearchModel_InvalidDateRange_SetsErrorMessage_AndDoesNotCallApi()
    {
        // Validation: Ngày bắt đầu > Ngày kết thúc
        var fakeNews = new FakeNewsClientService();
        var model = new SearchModel(
            fakeNews,
            new FakeCategoryClientService(),
            new FakeTagClientService(),
            NullLogger<SearchModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            StartDate = new DateTime(2026, 6, 20),
            EndDate = new DateTime(2026, 6, 10) // Nhỏ hơn StartDate
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.NotNull(model.ErrorMessage);
        Assert.Contains("Khoảng ngày không hợp lệ", model.ErrorMessage);
        // Không được gọi GetNewsArticlesAsync khi validate thất bại
        Assert.Equal(0, fakeNews.CallCount);
    }

    [Fact]
    public async Task SearchModel_Pagination_CalculatesPropertiesCorrectly()
    {
        // AC 3, AC 6: Phân trang tính toán đúng
        var fakeNews = new FakeNewsClientService
        {
            ResponseToReturn = new ODataEnvelope<NewsArticleApiModel>
            {
                Count = 25,
                Value = new List<NewsArticleApiModel>
                {
                    new() { NewsArticleId = "N01" },
                    new() { NewsArticleId = "N02" }
                }
            }
        };

        var model = new SearchModel(
            fakeNews,
            new FakeCategoryClientService(),
            new FakeTagClientService(),
            NullLogger<SearchModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            PageIndex = 2,
            PageSize = 10
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(25, model.TotalCount);
        Assert.Equal(3, model.TotalPages); // 25 / 10 = 3 pages
        Assert.True(model.HasPreviousPage);
        Assert.True(model.HasNextPage);
    }
}
