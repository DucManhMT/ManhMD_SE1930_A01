namespace FUNews.BusinessLogic.DTOs;

public class CategoryDto
{
    public short CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryDescription { get; set; } = string.Empty;
    public short? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public bool? IsActive { get; set; }
}
