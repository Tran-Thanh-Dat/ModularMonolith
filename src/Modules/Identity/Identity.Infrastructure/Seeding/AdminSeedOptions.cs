namespace Identity.Infrastructure.Seeding;

public sealed class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public bool Enabled { get; set; } = true;

    public string UserName { get; set; } = "admin";

    public string Email { get; set; } = "admin@example.com";

    public string FullName { get; set; } = "System Administrator";

    public string? Password { get; set; }
}
