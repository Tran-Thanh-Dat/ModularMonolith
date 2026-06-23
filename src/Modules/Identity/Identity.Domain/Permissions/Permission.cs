using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace Identity.Domain.Permissions;

public sealed class Permission : Entity
{
    private Permission()
    {
    }

    private Permission(Guid id, string code, string name, string module, string? description)
        : base(id)
    {
        Code = code;
        Name = name;
        Module = module;
        Description = description;
    }

    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string Module { get; private set; } = default!;

    public string? Description { get; private set; }

    public static Permission Create(string code, string name, string module, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Permission code is required.", "Permission.InvalidCode");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Permission name is required.", "Permission.InvalidName");
        }

        if (string.IsNullOrWhiteSpace(module))
        {
            throw new DomainException("Permission module is required.", "Permission.InvalidModule");
        }

        return new Permission(Guid.NewGuid(), code.Trim(), name.Trim(), module.Trim(), description?.Trim());
    }
}
