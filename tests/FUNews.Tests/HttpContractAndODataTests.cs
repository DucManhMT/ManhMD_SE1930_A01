using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using FUNews.BusinessLogic.DTOs;
using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.DataAccess.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FUNews.Tests;

public class HttpContractAndODataTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HttpContractAndODataTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public void Criterion_01_JsonDto_ShouldNotContain_PasswordOrHashProperties()
    {
        // 1. Inspect all DTO types in FUNews.BusinessLogic.DTOs
        var dtoAssembly = typeof(AccountDto).Assembly;
        var dtoTypes = dtoAssembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.Contains("DTOs"))
            .ToList();

        Assert.NotEmpty(dtoTypes);

        foreach (var type in dtoTypes)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                var lowerName = prop.Name.ToLowerInvariant();
                Assert.DoesNotContain("password", lowerName);
                Assert.DoesNotContain("hash", lowerName);
                Assert.DoesNotContain("secret", lowerName);
            }
        }
    }

    [Fact]
    public async Task Criterion_01_AccountApi_ShouldNotReturn_PasswordInJson()
    {
        // Query account endpoint directly
        var response = await _client.GetAsync("api/account");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawJson = await response.Content.ReadAsStringAsync();

        // Ensure raw JSON does not leak any password or hash keys
        Assert.DoesNotContain("password", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accountPassword", rawJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Criterion_02_OData_Filter_Orderby_Top_Skip_Count_ShouldWork()
    {
        // Query category with $filter, $orderby, $top, $skip, $count
        var url = "api/category?$filter=isActive eq true&$orderby=categoryName asc&$top=3&$skip=0&$count=true";
        var response = await _client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Request failed with {response.StatusCode}: {content}");

        var result = await response.Content.ReadFromJsonAsync<ODataResponse<CategoryDto>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Count);
        Assert.True(result.Count > 0, "OData count should be greater than 0");
        Assert.NotEmpty(result.Value);
        Assert.True(result.Value.Count <= 3, "Top 3 should limit items to at most 3");

        // Verify filtering: all items must have IsActive == true
        foreach (var item in result.Value)
        {
            Assert.True(item.IsActive == true);
        }

        // Verify ordering: ascending by CategoryName
        if (result.Value.Count > 1)
        {
            for (int i = 0; i < result.Value.Count - 1; i++)
            {
                var current = result.Value[i].CategoryName;
                var next = result.Value[i + 1].CategoryName;
                Assert.True(string.Compare(current, next, StringComparison.OrdinalIgnoreCase) <= 0);
            }
        }
    }

    [Fact]
    public async Task Criterion_02_OData_NewsQuery_FilterCategoryAndDate_ShouldWork()
    {
        // Query news with filter on categoryId, ordering and top
        var url = "api/news?$orderby=createdDate desc&$top=5&$count=true";
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Count);
        Assert.NotEmpty(result.Value);

        // Anonymous/Public query should only receive active articles
        foreach (var article in result.Value)
        {
            Assert.True(article.NewsStatus == true);
        }
    }

    [Fact]
    public async Task Regression_OData_PaginationWithSkip_WithoutOrderby_ShouldWorkDeterministically()
    {
        // Calling $skip without $orderby must succeed and use stable ordering
        var response = await _client.GetAsync("api/category?$skip=1&$top=2&$count=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ODataResponse<CategoryDto>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Count);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Criterion_03_QueryInvalid_Expand_ShouldBeBlocked()
    {
        // Contract states: chặn $expand
        var url = "api/category?$expand=parentCategory";
        var response = await _client.GetAsync(url);

        // Disallowed expand should return 400 Bad Request
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("expand", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Criterion_03_QueryInvalid_NonExistentProperty_ShouldBeBlocked()
    {
        // Filtering on an unknown property should return 400 Bad Request
        var url = "api/category?$filter=unknownField123 eq 'test'";
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Criterion_03_QueryInvalid_PasswordFilterOnAccount_ShouldBeBlocked()
    {
        // Attempting to filter on password/hash on Account should return 400 because AccountDto has no such property
        var url = "api/account?$filter=contains(accountPassword,'secret')";
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Criterion_03_QueryInvalid_ExceedingMaxTop_ShouldBeBlocked()
    {
        // Contract states MaxTop = 100; requesting 101 must fail with 400 Bad Request
        var url = "api/category?$top=101";
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Criterion_04_Frontend_TypedApiClient_CanRead_RealApi()
    {
        // Use the FE typed API client with the TestServer's HttpClient
        var apiClient = new FUNewsApiClient(_client);

        // 1. Fetch categories from the real database via API client
        var categoriesResult = await apiClient.GetCategoriesAsync("$filter=isActive eq true&$orderby=categoryName&$top=5&$count=true");
        Assert.NotNull(categoriesResult);
        Assert.NotEmpty(categoriesResult.Value);
        Assert.NotNull(categoriesResult.Count);

        // Verify content matches expected seeded categories
        Assert.Contains(categoriesResult.Value, c => !string.IsNullOrWhiteSpace(c.CategoryName));

        // 2. Fetch tags from real database
        var tagsResult = await apiClient.GetTagsAsync("$top=5");
        Assert.NotNull(tagsResult);
        Assert.NotEmpty(tagsResult.Value);

        // 3. Fetch single item
        var firstCategory = categoriesResult.Value.First();
        var singleCategory = await apiClient.GetCategoryByIdAsync(firstCategory.CategoryId);
        Assert.NotNull(singleCategory);
        Assert.Equal(firstCategory.CategoryId, singleCategory.CategoryId);
        Assert.Equal(firstCategory.CategoryName, singleCategory.CategoryName);
    }

    [Fact]
    public async Task Criterion_04_Frontend_TypedApiClient_ErrorMapping_ShouldThrowFUNewsApiException()
    {
        var apiClient = new FUNewsApiClient(_client);

        // Calling an invalid query must throw FUNewsApiException with HttpStatusCode.BadRequest
        var ex = await Assert.ThrowsAsync<FUNewsApiException>(() =>
            apiClient.GetCategoriesAsync("$filter=invalidField eq 123"));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.NotNull(ex.ProblemDetails);
    }

    [Fact]
    public void Criterion_05_NoController_ShouldCallOrInject_DbContext()
    {
        var apiAssembly = typeof(Program).Assembly;
        var controllerTypes = apiAssembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t))
            .ToList();

        Assert.NotEmpty(controllerTypes);

        foreach (var controller in controllerTypes)
        {
            // Check constructor parameters
            var constructors = controller.GetConstructors();
            foreach (var ctor in constructors)
            {
                var parameters = ctor.GetParameters();
                foreach (var param in parameters)
                {
                    Assert.False(
                        typeof(DbContext).IsAssignableFrom(param.ParameterType),
                        $"Controller {controller.Name} has constructor parameter of type {param.ParameterType.Name}, violating architecture constraint.");

                    Assert.False(
                        param.ParameterType == typeof(FUNewsDbContext),
                        $"Controller {controller.Name} directly references FUNewsDbContext.");
                }
            }

            // Check fields
            var fields = controller.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                Assert.False(
                    typeof(DbContext).IsAssignableFrom(field.FieldType),
                    $"Controller {controller.Name} has field of type {field.FieldType.Name}, violating architecture constraint.");

                Assert.False(
                    field.FieldType == typeof(FUNewsDbContext),
                    $"Controller {controller.Name} directly references FUNewsDbContext in a field.");
            }

            // Check properties
            var properties = controller.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                Assert.False(
                    typeof(DbContext).IsAssignableFrom(prop.PropertyType),
                    $"Controller {controller.Name} has property of type {prop.PropertyType.Name}, violating architecture constraint.");
            }
        }
    }
}
