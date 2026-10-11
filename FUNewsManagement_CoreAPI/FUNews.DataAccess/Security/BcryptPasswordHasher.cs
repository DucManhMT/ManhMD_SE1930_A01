namespace FUNews.DataAccess.Security;

public class BcryptPasswordHasher : IPasswordHasher
{
    private const int DefaultWorkFactor = 11;

    public string HashPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
        {
            throw new ArgumentException("Password cannot be null or empty.", nameof(plainPassword));
        }

        return BCrypt.Net.BCrypt.HashPassword(plainPassword, DefaultWorkFactor);
    }

    public bool VerifyPassword(string plainPassword, string hashedPassword)
    {
        if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(hashedPassword))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
        }
        catch
        {
            return false;
        }
    }

    public bool IsHashed(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        // Standard BCrypt formats start with $2a$, $2b$, $2y$, or $2x$ and are 60 characters long
        return (password.StartsWith("$2a$") || 
                password.StartsWith("$2b$") || 
                password.StartsWith("$2y$") || 
                password.StartsWith("$2x$")) && password.Length >= 59;
    }
}
