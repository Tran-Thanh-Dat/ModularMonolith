using BuildingBlocks.Application.Errors;

namespace BuildingBlocks.Application.Exceptions;

public class BadRequestException : BusinessException
{
    public BadRequestException(string message)
        : this(CommonErrors.BadRequest, message)
    {
    }

    public BadRequestException(string code, string message)
        : base(code, message)
    {
    }
}
