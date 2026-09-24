using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Options;
using Microsoft.IdentityModel.Tokens;

namespace FUNews.BusinessLogic.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenService(JwtOptions jwtOptions)
    {
        _jwtOptions = jwtOptions ?? throw new ArgumentNullException(nameof(jwtOptions));
        if (string.IsNullOrWhiteSpace(_jwtOptions.SigningKey) || Encoding.UTF8.GetByteCount(_jwtOptions.SigningKey) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey phải có độ dài ít nhất 32 bytes (256 bits).");
        }
    }

    public string GenerateToken(UserInfoDto user, out DateTime expiresAt)
    {
        ArgumentNullException.ThrowIfNull(user);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        expiresAt = DateTime.UtcNow.AddHours(8);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, user.AccountEmail),
            new(ClaimTypes.Name, user.AccountName),
            new(ClaimTypes.Role, user.RoleName)
        };

        // Tuân thủ Acceptance Criteria 7: Admin cấu hình từ appsettings, KHÔNG có AccountID trong DB và KHÔNG được gán AccountID giả.
        if (user.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.AccountEmail));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.AccountEmail));
        }
        else if (user.AccountId.HasValue)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.AccountId.Value.ToString()));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.AccountId.Value.ToString()));
            claims.Add(new Claim("accountId", user.AccountId.Value.ToString()));
            if (user.AccountRole.HasValue)
            {
                claims.Add(new Claim("roleId", user.AccountRole.Value.ToString()));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
