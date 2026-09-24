using System.ComponentModel.DataAnnotations;

namespace FUNews.BusinessLogic.Models;

public class CreateTagRequestDto
{
    [Required(ErrorMessage = "Tên thẻ tin là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Tên thẻ tin không được vượt quá 50 ký tự.")]
    public string TagName { get; set; } = string.Empty;

    [StringLength(400, ErrorMessage = "Ghi chú thẻ tin không được vượt quá 400 ký tự.")]
    public string? Note { get; set; }
}
