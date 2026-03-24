using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Project.Application.Common;
using Project.Domain.Entities;

namespace Project.Application.Services;

/// <summary>
/// Generates and validates JWT tokens.
/// </summary>
public interface IJwtTokenService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
    string GeneratePasswordResetToken(User user, TimeSpan lifetime);
    ClaimsPrincipal? ValidatePasswordResetToken(string token);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("employee_id", user.EmployeeId?.ToString() ?? string.Empty),
            new Claim("mustChangePassword", user.MustChangePassword ? "true" : "false"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: CreateSigningCredentials()
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public string GeneratePasswordResetToken(User user, TimeSpan lifetime)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim("purpose", "password_reset"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: CreateSigningCredentials()
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidatePasswordResetToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = CreateSecurityKey(),
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return principal.FindFirst("purpose")?.Value == "password_reset"
                ? principal
                : null;
        }
        catch
        {
            return null;
        }
    }

    private SymmetricSecurityKey CreateSecurityKey() =>
        new(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

    private SigningCredentials CreateSigningCredentials() =>
        new(CreateSecurityKey(), SecurityAlgorithms.HmacSha256);
}
