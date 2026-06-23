using Identity.Application.Abstractions;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public sealed class IdentityUserRepository : IIdentityUserRepository
{
    private readonly IdentityUnitOfWork _unitOfWork;

    public IdentityUserRepository(IdentityUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<User?> FindActiveByUserNameOrEmailAsync(
        string normalizedUserNameOrEmail,
        CancellationToken cancellationToken = default) =>
        _unitOfWork.Repository<User, Guid>()
            .Query()
            .Include(u => u.Roles)
            .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(
                u => !u.IsDeleted &&
                     u.IsActive &&
                     (u.UserName.ToLower() == normalizedUserNameOrEmail ||
                      u.Email.ToLower() == normalizedUserNameOrEmail),
                cancellationToken);

    public Task<User?> FindActiveByIdWithRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _unitOfWork.Repository<User, Guid>()
            .Query()
            .Include(u => u.Roles)
            .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(
                u => u.Id == userId && !u.IsDeleted && u.IsActive,
                cancellationToken);
}
