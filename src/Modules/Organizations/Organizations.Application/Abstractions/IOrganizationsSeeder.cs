namespace Organizations.Application.Abstractions;

public interface IOrganizationsSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
