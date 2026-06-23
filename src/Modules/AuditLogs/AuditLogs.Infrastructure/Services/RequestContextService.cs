using AuditLogs.Application.Abstractions;
using BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace AuditLogs.Infrastructure.Services;

public sealed class RequestContextService : IRequestContextService
{
    private const string SystemUserName = "system";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUserService _currentUserService;

    public RequestContextService(
        IHttpContextAccessor httpContextAccessor,
        ICurrentUserService currentUserService)
    {
        _httpContextAccessor = httpContextAccessor;
        _currentUserService = currentUserService;
    }

    public Guid? UserId => _currentUserService.UserId;

    public string? UserName =>
        _currentUserService.UserName
        ?? (_httpContextAccessor.HttpContext is null ? SystemUserName : null);

    public string? RequestPath =>
        _httpContextAccessor.HttpContext?.Request.Path.Value ?? "/system";

    public string? HttpMethod =>
        _httpContextAccessor.HttpContext?.Request.Method ?? "SYSTEM";

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "system";

    public string? UserAgent =>
        _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "system";

    public string? TraceId =>
        _httpContextAccessor.HttpContext?.TraceIdentifier ?? "system";
}
