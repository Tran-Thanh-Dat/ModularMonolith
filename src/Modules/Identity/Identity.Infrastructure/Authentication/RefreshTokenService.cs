using System.Security.Cryptography;
using System.Text;
using Identity.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Authentication;

public sealed class RefreshTokenOptions : IRefreshTokenSettings
{
    public const string SectionName = "RefreshToken";

    public string Secret { get; set; } = default!;

    public int ExpirationDays { get; set; } = 7;
}

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly RefreshTokenOptions _options;

    public RefreshTokenService(IOptions<RefreshTokenOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string refreshToken)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.Secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hash);
    }

    public bool VerifyRefreshToken(string refreshToken, string refreshTokenHash) =>
        HashRefreshToken(refreshToken) == refreshTokenHash;
}
