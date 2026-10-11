namespace FUNews.DataAccess.Security;

public interface IPasswordHasher
{
    string HashPassword(string plainPassword);
    bool VerifyPassword(string plainPassword, string hashedPassword);
    bool IsHashed(string? password);
}
