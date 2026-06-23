namespace BuildingBlocks.Application.Results;

public sealed class Error : IEquatable<Error>
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public string Code { get; }

    public string Message { get; }

    public string? Field { get; }

    public Error(string code, string message, string? field = null)
    {
        Code = code;
        Message = message;
        Field = field;
    }

    public bool Equals(Error? other)
    {
        if (other is null)
        {
            return false;
        }

        return Code == other.Code && Message == other.Message && Field == other.Field;
    }

    public override bool Equals(object? obj) => obj is Error error && Equals(error);

    public override int GetHashCode() => HashCode.Combine(Code, Message, Field);

    public override string ToString() => Code;
}
