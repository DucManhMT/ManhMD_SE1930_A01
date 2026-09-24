using System.ComponentModel.DataAnnotations;

namespace FUNews.BusinessLogic.Models;

public class UpdateCategoryRequestDto
{
    [Required(ErrorMessage = "Tên chuyên mục là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Tên chuyên mục không được vượt quá 100 ký tự.")]
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả chuyên mục là bắt buộc.")]
    [StringLength(250, ErrorMessage = "Mô tả chuyên mục không được vượt quá 250 ký tự.")]
    public string CategoryDescription { get; set; } = string.Empty;

    public short? ParentCategoryId { get; set; }

    public bool? IsActive { get; set; }
}
