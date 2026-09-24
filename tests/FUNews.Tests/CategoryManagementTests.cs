using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Models;
using FUNews.BusinessLogic.Security;
using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FUNews.Tests;

public class CategoryManagementTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IJwtTokenService _jwtTokenService;

    public CategoryManagementTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;

        using var scope = factory.Services.CreateScope();
        _jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
    }

    public async Task InitializeAsync()
    {
        await CleanupTestCategoriesAndArticlesAsync();
    }

    public async Task DisposeAsync()
    {
        await CleanupTestCategoriesAndArticlesAsync();
    }

    private async Task CleanupTestCategoriesAndArticlesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        // Remove test articles created for test categories
        var testArticles = await db.NewsArticles
            .Where(a => a.NewsArticleID.StartsWith("TEST_") || (a.CategoryID.HasValue && a.CategoryID.Value > 5))
            .ToListAsync();
        if (testArticles.Count > 0)
        {
            db.NewsArticles.RemoveRange(testArticles);
            await db.SaveChangesAsync();
        }

        // Remove test categories (ID > 5)
        var testCategories = await db.Categories
            .Where(c => c.CategoryID > 5)
            .ToListAsync();
        if (testCategories.Count > 0)
        {
            db.Categories.RemoveRange(testCategories);
            await db.SaveChangesAsync();
        }
    }

    private HttpClient CreateStaffClient()
    {
        var staffUser = new UserInfoDto
        {
            AccountId = 3,
            AccountEmail = "IsabellaDavid@FUNewsManagement.org",
            AccountName = "Isabella David",
            AccountRole = 1,
            RoleName = "Staff"
        };

        var token = _jwtTokenService.GenerateToken(staffUser, out _);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private HttpClient CreateLecturerClient()
    {
        var lecturerUser = new UserInfoDto
        {
            AccountId = 2,
            AccountEmail = "AlexanderJohn@FUNewsManagement.org",
            AccountName = "Alexander John",
            AccountRole = 2,
            RoleName = "Lecturer"
        };

        var token = _jwtTokenService.GenerateToken(lecturerUser, out _);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Criterion_01_Validation_Required_And_Length_Enforced()
    {
        var client = CreateStaffClient();

        // 1. Missing / Empty CategoryName
        var emptyNameResponse = await client.PostAsJsonAsync("api/category", new CreateCategoryRequestDto
        {
            CategoryName = "   ",
            CategoryDescription = "Mô tả hợp lệ",
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, emptyNameResponse.StatusCode);

        // 2. CategoryName > 100 characters
        var longNameResponse = await client.PostAsJsonAsync("api/category", new CreateCategoryRequestDto
        {
            CategoryName = new string('A', 101),
            CategoryDescription = "Mô tả hợp lệ",
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, longNameResponse.StatusCode);

        // 3. Missing / Empty CategoryDescription
        var emptyDescResponse = await client.PostAsJsonAsync("api/category", new CreateCategoryRequestDto
        {
            CategoryName = "Tên hợp lệ",
            CategoryDescription = "   ",
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, emptyDescResponse.StatusCode);

        // 4. CategoryDescription > 250 characters
        var longDescResponse = await client.PostAsJsonAsync("api/category", new CreateCategoryRequestDto
        {
            CategoryName = "Tên hợp lệ",
            CategoryDescription = new string('D', 251),
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, longDescResponse.StatusCode);
    }

    [Fact]
    public async Task Criterion_02_Unique_CategoryName_Per_Parent_Including_Null()
    {
        var client = CreateStaffClient();
        var uniqueName = $"UniCat_{Guid.NewGuid():N}"[..25];

        // 1. Tạo category root (ParentCategoryId = null)
        var createRoot1 = await client.PostAsJsonAsync("api/category", new CreateCategoryRequestDto
        {
            CategoryName = uniqueName,
            CategoryDescription = "Chuyên mục gốc lần 1",
            ParentCategoryId = null,
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Created, createRoot1.StatusCode);

        // 2. Tạo trùng tên cùng parent (null) -> Bị chặn
        var createRoot2 = await client.PostAsJsonAsync("api/category", new CreateCategoryRequestDto
        {
            CategoryName = uniqueName,
            CategoryDescription = "Chuyên mục gốc trùng tên",
            ParentCategoryId = null,
            IsActive = true
        });
        Assert.True(createRoot2.StatusCode == HttpStatusCode.BadRequest || createRoot2.StatusCode == HttpStatusCode.Conflict);

        // 3. Tạo cùng tên nhưng khác parent (ParentCategoryID = 1 từ seed) -> Thành công
        var createWithParent1 = await client.PostAsJsonAsync("api/category", new CreateCategoryRequestDto
        {
            CategoryName = uniqueName,
            CategoryDescription = "Chuyên mục con thuộc parent 1",
            ParentCategoryId = 1,
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Created, createWithParent1.StatusCode);

        // 4. Tạo trùng tên cùng parent 1 -> Bị chặn
        var createWithParent2 = await client.PostAsJsonAsync("api/category", new CreateCategoryRequestDto
        {
            CategoryName = uniqueName,
            CategoryDescription = "Chuyên mục con trùng tên dưới parent 1",
            ParentCategoryId = 1,
            IsActive = true
        });
        Assert.True(createWithParent2.StatusCode == HttpStatusCode.BadRequest || createWithParent2.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Criterion_03_Parent_Category_Must_Exist()
    {
        var client = CreateStaffClient();

        // Chỉ định parent không tồn tại (9999)
        var response = await client.PostAsJsonAsync("api/category", new CreateCategoryRequestDto
        {
            CategoryName = "Chuyên mục cha không tồn tại",
            CategoryDescription = "Mô tả kiểm tra parent tồn tại",
            ParentCategoryId = 9999,
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Criterion_04_And_05_ArticleCount_Staff_Vs_Public()
    {
        short testCategoryId;

        // 1. Tạo category thử nghiệm và 2 bài viết (1 Active, 1 Inactive)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

            var cat = new Category
            {
                CategoryName = $"CountTest_{Guid.NewGuid():N}"[..25],
                CategoryDescription = "Test count articles",
                IsActive = true
            };
            db.Categories.Add(cat);
            await db.SaveChangesAsync();
            testCategoryId = cat.CategoryID;

            var activeArticle = new NewsArticle
            {
                NewsArticleID = $"TEST_ACT_{Guid.NewGuid():N}"[..20],
                NewsTitle = "Active News Article",
                Headline = "Active Headline",
                CreatedDate = DateTime.UtcNow,
                NewsContent = "Content for active article",
                NewsSource = "FUNews",
                CategoryID = testCategoryId,
                NewsStatus = true,
                CreatedByID = 1
            };

            var inactiveArticle = new NewsArticle
            {
                NewsArticleID = $"TEST_INA_{Guid.NewGuid():N}"[..20],
                NewsTitle = "Inactive News Article",
                Headline = "Inactive Headline",
                CreatedDate = DateTime.UtcNow,
                NewsContent = "Content for inactive article",
                NewsSource = "FUNews",
                CategoryID = testCategoryId,
                NewsStatus = false,
                CreatedByID = 1
            };

            db.NewsArticles.AddRange(activeArticle, inactiveArticle);
            await db.SaveChangesAsync();
        }

        // 2. Anonymous / Public gọi GET /api/category/{id} -> Chỉ thấy số bài Active (1)
        var anonymousClient = _factory.CreateClient();
        var publicResponse = await anonymousClient.GetAsync($"api/category/{testCategoryId}");
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);

        var publicCategory = await publicResponse.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(publicCategory);
        Assert.Equal(1, publicCategory.ArticleCount); // Khách chỉ thấy bài active

        // 3. Staff gọi GET /api/category/{id} -> Thấy toàn bộ bài viết (2)
        var staffClient = CreateStaffClient();
        var staffResponse = await staffClient.GetAsync($"api/category/{testCategoryId}");
        Assert.Equal(HttpStatusCode.OK, staffResponse.StatusCode);

        var staffCategory = await staffResponse.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(staffCategory);
        Assert.Equal(2, staffCategory.ArticleCount); // Staff thấy tổng số bài (kể cả inactive)
    }

    [Fact]
    public async Task Criterion_06_Staff_Can_Create_Category_Successfully()
    {
        var client = CreateStaffClient();
        var categoryName = $"CNTT_{Guid.NewGuid():N}"[..20];

        var response = await client.PostAsJsonAsync("api/category", new CreateCategoryRequestDto
        {
            CategoryName = categoryName,
            CategoryDescription = "Chuyên ngành Công nghệ thông tin và Truyền thông",
            ParentCategoryId = null,
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(created);
        Assert.True(created.CategoryId > 0);
        Assert.Equal(categoryName, created.CategoryName);
        Assert.True(created.IsActive);
        Assert.Equal(0, created.ArticleCount);
    }

    [Fact]
    public async Task Role_Authorization_Enforced_For_Category_Creation()
    {
        // 1. Anonymous -> 401 Unauthorized
        var anonymousClient = _factory.CreateClient();
        var anonResponse = await anonymousClient.PostAsJsonAsync("api/category", new CreateCategoryRequestDto
        {
            CategoryName = "Hacker Category",
            CategoryDescription = "Test anonymous block",
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Unauthorized, anonResponse.StatusCode);

        // 2. Lecturer -> 403 Forbidden
        var lecturerClient = CreateLecturerClient();
        var lecturerResponse = await lecturerClient.PostAsJsonAsync("api/category", new CreateCategoryRequestDto
        {
            CategoryName = "Lecturer Category",
            CategoryDescription = "Test lecturer block",
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Forbidden, lecturerResponse.StatusCode);
    }

    [Fact]
    public async Task FUN009_Criterion_01_Category_With_Articles_Cannot_Change_Parent()
    {
        var client = CreateStaffClient();
        short catId;

        // 1. Tạo category có bài viết
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
            var cat = new Category
            {
                CategoryName = $"CatArt_{Guid.NewGuid():N}"[..20],
                CategoryDescription = "Testing parent change restriction",
                ParentCategoryID = null,
                IsActive = true
            };
            db.Categories.Add(cat);
            await db.SaveChangesAsync();
            catId = cat.CategoryID;

            var article = new NewsArticle
            {
                NewsArticleID = $"TEST_AR_{Guid.NewGuid():N}"[..20],
                NewsTitle = "Parent Restriction Article",
                Headline = "Testing Parent Change",
                CreatedDate = DateTime.UtcNow,
                NewsContent = "Content",
                NewsSource = "FUNews",
                CategoryID = catId,
                NewsStatus = true,
                CreatedByID = 1
            };
            db.NewsArticles.Add(article);
            await db.SaveChangesAsync();
        }

        // 2. Cố gắng đổi parent từ null sang 1 -> Bị chặn với 400 Bad Request
        var changeParentResponse = await client.PutAsJsonAsync($"api/category/{catId}", new UpdateCategoryRequestDto
        {
            CategoryName = "Updated Name",
            CategoryDescription = "Updated Desc",
            ParentCategoryId = 1, // Đổi parent
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, changeParentResponse.StatusCode);

        // 3. Giữ nguyên parent (null) và đổi tên/mô tả -> Thành công (200 OK)
        var sameParentResponse = await client.PutAsJsonAsync($"api/category/{catId}", new UpdateCategoryRequestDto
        {
            CategoryName = "Updated Name Same Parent",
            CategoryDescription = "Updated Desc",
            ParentCategoryId = null, // Giữ nguyên parent
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.OK, sameParentResponse.StatusCode);

        var updated = await sameParentResponse.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Name Same Parent", updated.CategoryName);
    }

    [Fact]
    public async Task FUN009_Criterion_02_Category_With_Articles_Or_Children_Cannot_Be_Deleted()
    {
        var client = CreateStaffClient();
        short parentCatId;
        short childCatId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

            // Tạo parent và child category
            var parentCat = new Category
            {
                CategoryName = $"ParCat_{Guid.NewGuid():N}"[..20],
                CategoryDescription = "Parent category for delete test",
                ParentCategoryID = null,
                IsActive = true
            };
            db.Categories.Add(parentCat);
            await db.SaveChangesAsync();
            parentCatId = parentCat.CategoryID;

            var childCat = new Category
            {
                CategoryName = $"ChiCat_{Guid.NewGuid():N}"[..20],
                CategoryDescription = "Child category for delete test",
                ParentCategoryID = parentCatId,
                IsActive = true
            };
            db.Categories.Add(childCat);
            await db.SaveChangesAsync();
            childCatId = childCat.CategoryID;

            // Gắn bài viết vào child category
            var article = new NewsArticle
            {
                NewsArticleID = $"TEST_DEL_{Guid.NewGuid():N}"[..20],
                NewsTitle = "Delete Restriction Article",
                Headline = "Testing Delete",
                CreatedDate = DateTime.UtcNow,
                NewsContent = "Content",
                NewsSource = "FUNews",
                CategoryID = childCatId,
                NewsStatus = true,
                CreatedByID = 1
            };
            db.NewsArticles.Add(article);
            await db.SaveChangesAsync();
        }

        // 1. Xóa parent category (đang có child) -> Bị chặn với 409 Conflict
        var deleteParentResponse = await client.DeleteAsync($"api/category/{parentCatId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteParentResponse.StatusCode);

        // 2. Xóa child category (đang có article) -> Bị chặn với 409 Conflict
        var deleteChildResponse = await client.DeleteAsync($"api/category/{childCatId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteChildResponse.StatusCode);
    }

    [Fact]
    public async Task FUN009_Criterion_03_Duplicate_Name_And_Self_Parent_Blocked_On_Update()
    {
        var client = CreateStaffClient();
        short catAId;
        short catBId;
        var nameA = $"CatA_{Guid.NewGuid():N}"[..20];
        var nameB = $"CatB_{Guid.NewGuid():N}"[..20];

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

            var catA = new Category
            {
                CategoryName = nameA,
                CategoryDescription = "Cat A desc",
                ParentCategoryID = null,
                IsActive = true
            };
            var catB = new Category
            {
                CategoryName = nameB,
                CategoryDescription = "Cat B desc",
                ParentCategoryID = null,
                IsActive = true
            };

            db.Categories.AddRange(catA, catB);
            await db.SaveChangesAsync();
            catAId = catA.CategoryID;
            catBId = catB.CategoryID;
        }

        // 1. Cập nhật Cat B đổi tên thành Cat A (cùng parent null) -> Bị chặn 400 Bad Request
        var duplicateResponse = await client.PutAsJsonAsync($"api/category/{catBId}", new UpdateCategoryRequestDto
        {
            CategoryName = nameA,
            CategoryDescription = "Updated Desc",
            ParentCategoryId = null,
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);

        // 2. Cập nhật Cat B chọn chính mình làm danh mục cha -> Bị chặn 400 Bad Request
        var selfParentResponse = await client.PutAsJsonAsync($"api/category/{catBId}", new UpdateCategoryRequestDto
        {
            CategoryName = nameB,
            CategoryDescription = "Updated Desc",
            ParentCategoryId = catBId,
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, selfParentResponse.StatusCode);
    }

    [Fact]
    public async Task FUN009_Criterion_04_And_05_Empty_Category_Can_Be_Deleted_Without_Cascade()
    {
        var client = CreateStaffClient();
        short emptyCatId;

        // 1. Tạo category trống (không bài viết, không chuyên mục con)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

            var emptyCat = new Category
            {
                CategoryName = $"EmptyCat_{Guid.NewGuid():N}"[..20],
                CategoryDescription = "Empty category to be deleted",
                ParentCategoryID = null,
                IsActive = true
            };
            db.Categories.Add(emptyCat);
            await db.SaveChangesAsync();
            emptyCatId = emptyCat.CategoryID;
        }

        // 2. Staff xóa category trống -> Thành công 204 NoContent
        var deleteResponse = await client.DeleteAsync($"api/category/{emptyCatId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 3. Kiểm tra CSDL: category đã bị xóa hoàn toàn
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
            var exists = await db.Categories.AnyAsync(c => c.CategoryID == emptyCatId);
            Assert.False(exists);
        }

        // 4. Xóa category không tồn tại -> 404 NotFound
        var notFoundResponse = await client.DeleteAsync("api/category/9999");
        Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);
    }

    [Fact]
    public async Task FUN009_Role_Authorization_Enforced_For_Update_And_Delete()
    {
        var anonymousClient = _factory.CreateClient();
        var lecturerClient = CreateLecturerClient();

        // 1. Anonymous PUT -> 401 Unauthorized
        var anonPut = await anonymousClient.PutAsJsonAsync("api/category/1", new UpdateCategoryRequestDto
        {
            CategoryName = "Anon Update",
            CategoryDescription = "Desc",
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Unauthorized, anonPut.StatusCode);

        // 2. Lecturer PUT -> 403 Forbidden
        var lecturerPut = await lecturerClient.PutAsJsonAsync("api/category/1", new UpdateCategoryRequestDto
        {
            CategoryName = "Lecturer Update",
            CategoryDescription = "Desc",
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Forbidden, lecturerPut.StatusCode);

        // 3. Anonymous DELETE -> 401 Unauthorized
        var anonDelete = await anonymousClient.DeleteAsync("api/category/1");
        Assert.Equal(HttpStatusCode.Unauthorized, anonDelete.StatusCode);

        // 4. Lecturer DELETE -> 403 Forbidden
        var lecturerDelete = await lecturerClient.DeleteAsync("api/category/1");
        Assert.Equal(HttpStatusCode.Forbidden, lecturerDelete.StatusCode);
    }
}
