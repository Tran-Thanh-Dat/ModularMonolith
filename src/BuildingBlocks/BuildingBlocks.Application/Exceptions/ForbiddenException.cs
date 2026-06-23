using BuildingBlocks.Application.Errors;

namespace BuildingBlocks.Application.Exceptions;

public class ForbiddenException : BusinessException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : this(CommonErrors.Forbidden, message)
    {
    }

    public ForbiddenException(string code, string message)
        : base(code, message)
    {
    }
}
