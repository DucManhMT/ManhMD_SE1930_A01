using System.ComponentModel.DataAnnotations;

namespace FUNews.BusinessLogic.Models;

public class CreateNewsArticleRequestDto
{
    [StringLength(400, ErrorMessage = "Tiêu đề bài viết không được vượt quá 400 ký tự.")]
    public string? NewsTitle { get; set; }

    [Required(ErrorMessage = "Tiêu đề tóm tắt (Headline) là bắt buộc.")]
    [StringLength(150, ErrorMessage = "Tiêu đề tóm tắt không được vượt quá 150 ký tự.")]
    public string Headline { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "Nội dung bài viết không được vượt quá 4000 ký tự.")]
    public string? NewsContent { get; set; }

    [StringLength(400, ErrorMessage = "Nguồn tin không được vượt quá 400 ký tự.")]
    public string? NewsSource { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn chuyên mục cho bài viết.")]
    public short CategoryId { get; set; }

    public bool? NewsStatus { get; set; } = true;

    public List<int> TagIds { get; set; } = new();
}
