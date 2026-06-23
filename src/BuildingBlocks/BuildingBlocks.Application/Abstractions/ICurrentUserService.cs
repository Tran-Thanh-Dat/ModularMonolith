namespace BuildingBlocks.Application.Abstractions;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? UserName { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Roles { get; }

    IReadOnlyCollection<string> Permissions { get; }
}
