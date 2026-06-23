using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;
using Identity.Domain.Permissions;
using Identity.Domain.Users;

namespace Identity.Domain.Roles;

public sealed class Role : Entity
{
    private Role()
    {
    }

    private Role(Guid id, string code, string name, string? description)
        : base(id)
    {
        Code = code;
        Name = name;
        Description = description;
        IsActive = true;
    }

    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public ICollection<Permission> Permissions { get; private set; } = [];

    public ICollection<User> Users { get; private set; } = [];

    public static Role Create(string code, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Role code is required.", "Role.InvalidCode");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Role name is required.", "Role.InvalidName");
        }

        return new Role(Guid.NewGuid(), code.Trim(), name.Trim(), description?.Trim());
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void AddPermission(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        if (Permissions.All(p => p.Id != permission.Id))
        {
            Permissions.Add(permission);
        }
    }

    public void RemovePermission(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        var existing = Permissions.FirstOrDefault(p => p.Id == permission.Id);
        if (existing is not null)
        {
            Permissions.Remove(existing);
        }
    }
}
