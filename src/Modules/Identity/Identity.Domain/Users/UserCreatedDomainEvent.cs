using BuildingBlocks.Domain.Events;

namespace Identity.Domain.Users;

public sealed class UserCreatedDomainEvent : DomainEvent
{
    public UserCreatedDomainEvent(Guid userId, string userName, string email)
    {
        UserId = userId;
        UserName = userName;
        Email = email;
    }

    public Guid UserId { get; }

    public string UserName { get; }

    public string Email { get; }
}
