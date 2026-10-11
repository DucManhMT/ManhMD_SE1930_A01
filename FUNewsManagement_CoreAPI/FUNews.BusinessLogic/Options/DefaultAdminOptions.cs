namespace FUNews.BusinessLogic.Options;

public class DefaultAdminOptions
{
    public const string SectionName = "DefaultAdmin";

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
