using System.ComponentModel.DataAnnotations;

namespace FUNews.BusinessLogic.DTOs;

public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "RefreshToken là bắt buộc.")]
    public string RefreshToken { get; set; } = string.Empty;
}
