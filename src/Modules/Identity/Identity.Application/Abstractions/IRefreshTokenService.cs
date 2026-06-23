namespace Identity.Application.Abstractions;

public interface IRefreshTokenService
{
    string GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);

    bool VerifyRefreshToken(string refreshToken, string refreshTokenHash);
}
