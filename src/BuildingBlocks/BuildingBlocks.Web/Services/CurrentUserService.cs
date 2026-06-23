using System.Security.Claims;
using BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Web.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var userId = User?.FindFirstValue("sub")
                         ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
        }
    }

    public string? UserName =>
        User?.FindFirstValue(ClaimTypes.Name)
        ?? User?.FindFirstValue("name");

    public string? Email =>
        User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue("email");

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyCollection<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray() ?? [];

    public IReadOnlyCollection<string> Permissions =>
        User?.FindAll("permission").Select(claim => claim.Value).ToArray() ?? [];
}
