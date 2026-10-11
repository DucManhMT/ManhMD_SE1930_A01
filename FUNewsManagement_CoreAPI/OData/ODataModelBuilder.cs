using FUNews.BusinessLogic.DTOs;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace ManhMD_SE1930_A01_BE.OData;

public static class ODataModelBuilder
{
    public static IEdmModel GetEdmModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EnableLowerCamelCase();

        var categorySet = builder.EntitySet<CategoryDto>("category");
        categorySet.EntityType.HasKey(c => c.CategoryId);

        var tagSet = builder.EntitySet<TagDto>("tag");
        tagSet.EntityType.HasKey(t => t.TagId);

        var newsSet = builder.EntitySet<NewsArticleDto>("news");
        newsSet.EntityType.HasKey(n => n.NewsArticleId);

        var accountSet = builder.EntitySet<AccountDto>("account");
        accountSet.EntityType.HasKey(a => a.AccountId);

        return builder.GetEdmModel();
    }
}
