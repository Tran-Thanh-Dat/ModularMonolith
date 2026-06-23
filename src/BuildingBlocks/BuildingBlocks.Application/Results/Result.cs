namespace BuildingBlocks.Application.Results;

public class Result
{
    protected Result(bool isSuccess, Error error, IReadOnlyList<Error>? errors = null)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot contain an error.");
        }

        if (!isSuccess && error == Error.None && (errors is null || errors.Count == 0))
        {
            throw new InvalidOperationException("A failed result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? (error == Error.None ? [] : [error]);
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string Code => Error.Code;

    public string Message => Error.Message;

    public Error Error { get; }

    public IReadOnlyList<Error> Errors { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result Failure(string code, string message) => new(false, new Error(code, message));

    public static Result ValidationFailure(IReadOnlyList<Error> errors)
    {
        var primary = errors.FirstOrDefault() ?? new Error("Common.ValidationError", "Validation failed.");
        return new Result(false, primary, errors);
    }
}
