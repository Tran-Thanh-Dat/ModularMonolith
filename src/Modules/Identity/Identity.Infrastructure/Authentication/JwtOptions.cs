namespace Identity.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = default!;

    public string Audience { get; set; } = default!;

    public string Secret { get; set; } = default!;

    public int AccessTokenExpirationMinutes { get; set; } = 30;
}
