namespace BuildingBlocks.Application.Results;

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, Error error, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, error, errors)
    {
        _value = value;
    }

    public T? Data => IsSuccess ? _value : default;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value) => new(value, true, Error.None);

    public new static Result<T> Failure(Error error) => new(default, false, error);

    public new static Result<T> Failure(string code, string message) => new(default, false, new Error(code, message));

    public new static Result<T> ValidationFailure(IReadOnlyList<Error> errors)
    {
        var primary = errors.FirstOrDefault() ?? new Error("Common.ValidationError", "Validation failed.");
        return new Result<T>(default, false, primary, errors);
    }
}
