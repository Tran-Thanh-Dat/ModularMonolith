namespace BuildingBlocks.Application.Exceptions;

public abstract class BusinessException : Exception
{
    protected BusinessException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
