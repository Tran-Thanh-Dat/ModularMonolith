namespace Users.Application.Users.GetUserById;

public sealed class UserDetailResponse
{
    public Guid Id { get; init; }

    public string UserName { get; init; } = default!;

    public string Email { get; init; } = default!;

    public string FullName { get; init; } = default!;

    public bool IsActive { get; init; }

    public DateTimeOffset? LastLoginAt { get; init; }

    public IReadOnlyCollection<RoleResponse> Roles { get; init; } = [];

    public IReadOnlyCollection<PermissionResponse> Permissions { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class RoleResponse
{
    public Guid Id { get; init; }

    public string Code { get; init; } = default!;

    public string Name { get; init; } = default!;
}

public sealed class PermissionResponse
{
    public Guid Id { get; init; }

    public string Code { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string Module { get; init; } = default!;
}
