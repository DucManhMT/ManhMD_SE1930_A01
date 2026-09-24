using System.ComponentModel.DataAnnotations;
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

public class CategoriesRazorPageTests
{
    private class FakeCategoryClientService : ICategoryClientService
    {
        public List<CategoryApiModel> Categories { get; set; } = new()
        {
            new CategoryApiModel
            {
                CategoryId = 1,
                CategoryName = "Tin trong nước",
                CategoryDescription = "Các sự kiện nổi bật trong nước",
                ParentCategoryId = null,
                ParentCategoryName = null,
                IsActive = true,
                ArticleCount = 5
            },
            new CategoryApiModel
            {
                CategoryId = 2,
                CategoryName = "Tin quốc tế",
                CategoryDescription = "Các sự kiện toàn cầu",
                ParentCategoryId = null,
                ParentCategoryName = null,
                IsActive = true,
                ArticleCount = 3
            },
            new CategoryApiModel
            {
                CategoryId = 3,
                CategoryName = "Công nghệ thông tin",
                CategoryDescription = "Tin tức CNTT và AI",
                ParentCategoryId = 2,
                ParentCategoryName = "Tin quốc tế",
                IsActive = true,
                ArticleCount = 2
            }
        };

        public bool ShouldFailGet { get; set; }
        public bool ShouldFailCreate { get; set; }
        public string? FailCreateMessage { get; set; }
        public Dictionary<string, string[]>? FailCreateErrors { get; set; }

        public Task<ODataEnvelope<CategoryApiModel>> GetCategoriesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            if (ShouldFailGet)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.InternalServerError, "Không thể tải danh sách chuyên mục.");
            }

            var result = new ODataEnvelope<CategoryApiModel>
            {
                Count = Categories.Count,
                Value = new List<CategoryApiModel>(Categories)
            };

            return Task.FromResult(result);
        }

        public Task<CategoryApiModel?> GetByIdAsync(short id, CancellationToken cancellationToken = default)
        {
            var cat = Categories.FirstOrDefault(c => c.CategoryId == id);
            return Task.FromResult(cat);
        }

        public Task<CategoryApiModel> CreateCategoryAsync(CreateCategoryApiModel request, CancellationToken cancellationToken = default)
        {
            if (ShouldFailCreate)
            {
                var problem = new ApiProblemDetails
                {
                    Title = "Dữ liệu không hợp lệ",
                    Detail = FailCreateMessage ?? "Tên danh mục đã tồn tại trong cùng danh mục cha.",
                    Errors = FailCreateErrors
                };
                throw new FUNewsApiException(System.Net.HttpStatusCode.BadRequest, FailCreateMessage ?? "Tên danh mục đã tồn tại trong cùng danh mục cha.", problem);
            }

            var newCategory = new CategoryApiModel
            {
                CategoryId = (short)(Categories.Count + 1),
                CategoryName = request.CategoryName,
                CategoryDescription = request.CategoryDescription,
                ParentCategoryId = request.ParentCategoryId,
                ParentCategoryName = request.ParentCategoryId.HasValue
                    ? Categories.FirstOrDefault(c => c.CategoryId == request.ParentCategoryId.Value)?.CategoryName
                    : null,
                IsActive = request.IsActive,
                ArticleCount = 0
            };

            Categories.Add(newCategory);
            return Task.FromResult(newCategory);
        }
    }

    private static CategoriesModel CreatePageModel(FakeCategoryClientService fakeService)
    {
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        var pageContext = new PageContext(actionContext);

        return new CategoriesModel(fakeService, NullLogger<CategoriesModel>.Instance)
        {
            PageContext = pageContext
        };
    }

    private static object? GetPropertyValue(object? obj, string propName)
    {
        if (obj == null) return null;
        var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return prop?.GetValue(obj);
    }

    [Fact]
    public async Task OnGetAsync_LoadsCategories_And_ParentCategories_Successfully()
    {
        var fakeService = new FakeCategoryClientService();
        var pageModel = CreatePageModel(fakeService);

        await pageModel.OnGetAsync();

        Assert.NotNull(pageModel.Categories);
        Assert.Equal(3, pageModel.Categories.Count);
        Assert.Equal(3, pageModel.TotalCount);
        Assert.NotNull(pageModel.ParentCategories);
        Assert.Equal(3, pageModel.ParentCategories.Count);
        Assert.Null(pageModel.ErrorMessage);
    }

    [Fact]
    public async Task OnGetAsync_HandlesApiError_SetsErrorMessage_Gracefully()
    {
        var fakeService = new FakeCategoryClientService { ShouldFailGet = true };
        var pageModel = CreatePageModel(fakeService);

        await pageModel.OnGetAsync();

        Assert.Empty(pageModel.Categories);
        Assert.Equal(0, pageModel.TotalCount);
        Assert.NotNull(pageModel.ErrorMessage);
        Assert.Contains("Không thể tải danh sách chuyên mục", pageModel.ErrorMessage);
    }

    [Fact]
    public async Task OnGetListAsync_ReturnsJsonWithCategories()
    {
        var fakeService = new FakeCategoryClientService();
        var pageModel = CreatePageModel(fakeService);

        var result = await pageModel.OnGetListAsync("công nghệ", "active");

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var success = GetPropertyValue(jsonResult.Value, "success");
        Assert.Equal(true, success);

        var total = GetPropertyValue(jsonResult.Value, "totalCount");
        Assert.Equal(3, total);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithValidData_ReturnsSuccessJson()
    {
        var fakeService = new FakeCategoryClientService();
        var pageModel = CreatePageModel(fakeService);

        var input = new CategoriesModel.CreateCategoryInputModel
        {
            CategoryName = "Trí tuệ nhân tạo",
            CategoryDescription = "Các nghiên cứu và ứng dụng AI",
            ParentCategoryId = 3,
            IsActive = true
        };

        var result = await pageModel.OnPostCreateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var success = GetPropertyValue(jsonResult.Value, "success");
        Assert.Equal(true, success);

        var message = GetPropertyValue(jsonResult.Value, "message") as string;
        Assert.NotNull(message);
        Assert.Contains("Trí tuệ nhân tạo", message);

        var created = GetPropertyValue(jsonResult.Value, "category") as CategoryApiModel;
        Assert.NotNull(created);
        Assert.Equal("Trí tuệ nhân tạo", created.CategoryName);
        Assert.Equal((short?)3, created.ParentCategoryId);
        Assert.Equal("Công nghệ thông tin", created.ParentCategoryName);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithInvalidModelState_ReturnsValidationErrorJson()
    {
        var fakeService = new FakeCategoryClientService();
        var pageModel = CreatePageModel(fakeService);

        pageModel.ModelState.AddModelError("CategoryName", "Tên chuyên mục là bắt buộc.");

        var input = new CategoriesModel.CreateCategoryInputModel
        {
            CategoryName = "",
            CategoryDescription = "Mô tả",
            IsActive = true
        };

        var result = await pageModel.OnPostCreateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var success = GetPropertyValue(jsonResult.Value, "success");
        Assert.Equal(false, success);

        var message = GetPropertyValue(jsonResult.Value, "message") as string;
        Assert.Equal("Vui lòng kiểm tra lại thông tin nhập liệu.", message);

        var errors = GetPropertyValue(jsonResult.Value, "errors") as Dictionary<string, string[]>;
        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("CategoryName"));
    }

    [Fact]
    public async Task OnPostCreateAsync_WithDuplicateName_HandlesApiException_ReturnsErrorJson()
    {
        var fakeService = new FakeCategoryClientService
        {
            ShouldFailCreate = true,
            FailCreateMessage = "Tên danh mục đã tồn tại trong cùng danh mục cha.",
            FailCreateErrors = new Dictionary<string, string[]>
            {
                { "CategoryName", new[] { "Tên danh mục đã tồn tại trong cùng danh mục cha." } }
            }
        };

        var pageModel = CreatePageModel(fakeService);

        var input = new CategoriesModel.CreateCategoryInputModel
        {
            CategoryName = "Tin trong nước",
            CategoryDescription = "Mô tả trùng",
            ParentCategoryId = null,
            IsActive = true
        };

        var result = await pageModel.OnPostCreateAsync(input, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var success = GetPropertyValue(jsonResult.Value, "success");
        Assert.Equal(false, success);

        var message = GetPropertyValue(jsonResult.Value, "message") as string;
        Assert.Equal("Tên danh mục đã tồn tại trong cùng danh mục cha.", message);

        var errors = GetPropertyValue(jsonResult.Value, "errors") as Dictionary<string, string[]>;
        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("CategoryName"));
    }
}
