using System.ComponentModel.DataAnnotations.Schema;

namespace FUNews.DataAccess.Entities;

public class Category
{
    public short CategoryID { get; set; }

    public string CategoryName { get; set; } = null!;

    [Column("CategoryDesciption")]
    public string CategoryDescription { get; set; } = null!;

    public short? ParentCategoryID { get; set; }

    public bool? IsActive { get; set; }

    // Navigation properties
    public virtual Category? ParentCategory { get; set; }

    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();

    public virtual ICollection<NewsArticle> NewsArticles { get; set; } = new List<NewsArticle>();
}
