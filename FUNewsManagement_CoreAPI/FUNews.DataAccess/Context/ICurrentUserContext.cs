namespace FUNews.DataAccess.Context;

public interface ICurrentUserContext
{
    int? UserId { get; }
    string? UserName { get; }
}
