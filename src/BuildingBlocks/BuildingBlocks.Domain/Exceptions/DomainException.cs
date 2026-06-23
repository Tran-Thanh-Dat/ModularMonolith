namespace BuildingBlocks.Domain.Exceptions;

public class DomainException : Exception
{
    public string? ErrorCode { get; }

    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public DomainException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
