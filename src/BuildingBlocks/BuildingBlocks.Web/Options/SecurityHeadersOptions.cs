namespace BuildingBlocks.Web.Options;

public sealed class SecurityHeadersOptions
{
    public const string SectionName = "SecurityHeaders";

    public bool Enabled { get; set; } = true;

    public bool EnableStrictTransportSecurity { get; set; }

    public int StrictTransportSecurityMaxAgeSeconds { get; set; } = 31_536_000;

    public bool IncludeSubDomainsInStrictTransportSecurity { get; set; } = true;

    public bool EnableContentSecurityPolicy { get; set; } = true;

    public string ContentSecurityPolicy { get; set; } =
        "default-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    public string SwaggerContentSecurityPolicy { get; set; } =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
}
