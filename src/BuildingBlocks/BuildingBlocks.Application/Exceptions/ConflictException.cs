namespace BuildingBlocks.Application.Exceptions;

public class ConflictException : BusinessException
{
    public ConflictException(string code, string message)
        : base(code, message)
    {
    }
}
