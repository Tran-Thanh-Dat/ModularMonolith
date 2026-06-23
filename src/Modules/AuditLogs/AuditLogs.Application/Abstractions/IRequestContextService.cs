namespace AuditLogs.Application.Abstractions;

public interface IRequestContextService
{
    Guid? UserId { get; }

    string? UserName { get; }

    string? RequestPath { get; }

    string? HttpMethod { get; }

    string? IpAddress { get; }

    string? UserAgent { get; }

    string? TraceId { get; }
}
