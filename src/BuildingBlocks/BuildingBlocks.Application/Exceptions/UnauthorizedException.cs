using BuildingBlocks.Application.Errors;

namespace BuildingBlocks.Application.Exceptions;

public class UnauthorizedException : BusinessException
{
    public UnauthorizedException(string message)
        : this(CommonErrors.Unauthorized, message)
    {
    }

    public UnauthorizedException(string code, string message)
        : base(code, message)
    {
    }
}
