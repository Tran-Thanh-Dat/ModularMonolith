using BuildingBlocks.Domain.Events;
using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;
using Identity.Domain.Permissions;
using Identity.Domain.Roles;

namespace Identity.Domain.Users;

public sealed class User : SoftDeletableEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private User()
    {
    }

    private User(
        Guid id,
        string userName,
        string email,
        string passwordHash,
        string fullName,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        UserName = userName;
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        IsActive = true;
        SetCreated(createdBy, createdAt);
    }

    public string UserName { get; private set; } = default!;

    public string Email { get; private set; } = default!;

    public string PasswordHash { get; private set; } = default!;

    public string FullName { get; private set; } = default!;

    public bool IsActive { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public ICollection<Role> Roles { get; private set; } = [];

    public ICollection<Permission> DirectPermissions { get; private set; } = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static User Create(
        string userName,
        string email,
        string passwordHash,
        string fullName,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new DomainException("User name is required.", "User.InvalidUserName");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.", "User.InvalidEmail");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.", "User.InvalidPasswordHash");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("Full name is required.", "User.InvalidFullName");
        }

        var user = new User(
            Guid.NewGuid(),
            userName.Trim(),
            email.Trim().ToLowerInvariant(),
            passwordHash,
            fullName.Trim(),
            createdAt,
            createdBy);

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id, user.UserName, user.Email));

        return user;
    }

    public void UpdateProfile(string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("Full name is required.", "User.InvalidFullName");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.", "User.InvalidEmail");
        }

        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
    }

    public void ChangePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.", "User.InvalidPasswordHash");
        }

        PasswordHash = passwordHash;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void MarkLoggedIn(DateTimeOffset loggedInAt) => LastLoginAt = loggedInAt;

    public void AssignRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (Roles.All(r => r.Id != role.Id))
        {
            Roles.Add(role);
        }
    }

    public void RemoveRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        var existing = Roles.FirstOrDefault(r => r.Id == role.Id);
        if (existing is not null)
        {
            Roles.Remove(existing);
        }
    }

    public void AssignDirectPermission(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        if (DirectPermissions.All(p => p.Id != permission.Id))
        {
            DirectPermissions.Add(permission);
        }
    }

    public void RemoveDirectPermission(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        var existing = DirectPermissions.FirstOrDefault(p => p.Id == permission.Id);
        if (existing is not null)
        {
            DirectPermissions.Remove(existing);
        }
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
