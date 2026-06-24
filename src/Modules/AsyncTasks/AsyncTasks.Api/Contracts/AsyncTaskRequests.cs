namespace AsyncTasks.Api.Contracts;

public sealed class SubmitEmailDemoTaskRequest
{
    public string? EmailTo { get; init; }

    public string? Subject { get; init; }

    public string? Body { get; init; }

    public Guid? TenantId { get; init; }

    public Guid? OrganizationId { get; init; }
}

public sealed class SubmitFileProcessingDemoTaskRequest
{
    public Guid? FileId { get; init; }

    public string? FileName { get; init; }

    public Guid? TenantId { get; init; }

    public Guid? OrganizationId { get; init; }
}

public sealed class SubmitFailDemoTaskRequest
{
    public string? FailReason { get; init; }

    public bool ShouldAlwaysFail { get; init; } = true;

    public Guid? TenantId { get; init; }

    public Guid? OrganizationId { get; init; }
}

public sealed class SubmitLongRunningDemoTaskRequest
{
    public int DurationSeconds { get; init; } = 5;

    public int Steps { get; init; } = 5;

    public Guid? TenantId { get; init; }

    public Guid? OrganizationId { get; init; }
}
