using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;

namespace BuildingBlocks.Application.Exceptions;

public class ValidationException : BusinessException
{
    public IReadOnlyDictionary<string, string[]> ErrorGroups { get; }

    public IReadOnlyList<Error> Errors { get; }

    public ValidationException(Dictionary<string, string[]> errors)
        : base(CommonErrors.ValidationError, "One or more validation errors occurred.")
    {
        ErrorGroups = errors;
        Errors = errors
            .SelectMany(pair => pair.Value.Select(message => new Error(
                CommonErrors.ValidationError,
                message,
                string.IsNullOrWhiteSpace(pair.Key) ? null : pair.Key)))
            .ToList();
    }

    public ValidationException(IReadOnlyList<Error> errors)
        : base(CommonErrors.ValidationError, "One or more validation errors occurred.")
    {
        Errors = errors;
        ErrorGroups = errors
            .GroupBy(error => error.Field ?? string.Empty)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Message).Distinct().ToArray());
    }
}
