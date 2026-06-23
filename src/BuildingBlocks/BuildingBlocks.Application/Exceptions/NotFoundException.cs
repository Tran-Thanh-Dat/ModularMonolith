using BuildingBlocks.Application.Errors;

namespace BuildingBlocks.Application.Exceptions;

public class NotFoundException : BusinessException
{
    public NotFoundException(string message)
        : this(CommonErrors.NotFound, message)
    {
    }

    public NotFoundException(string code, string message)
        : base(code, message)
    {
    }

    public NotFoundException(string resourceName, object resourceId)
        : this(CommonErrors.NotFound, $"{resourceName} with id '{resourceId}' was not found.")
    {
    }
}
