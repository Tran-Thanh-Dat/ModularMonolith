namespace Users.Api.Contracts;

public sealed class CreateUserRequest
{
    public string UserName { get; init; } = default!;

    public string Email { get; init; } = default!;

    public string FullName { get; init; } = default!;

    public string Password { get; init; } = default!;

    public IReadOnlyCollection<Guid>? RoleIds { get; init; }
}

public sealed class UpdateUserRequest
{
    public string Email { get; init; } = default!;

    public string FullName { get; init; } = default!;
}

public sealed class AssignRolesRequest
{
    public IReadOnlyCollection<Guid> RoleIds { get; init; } = [];
}

public sealed class AssignPermissionsRequest
{
    public IReadOnlyCollection<Guid> PermissionIds { get; init; } = [];
}
