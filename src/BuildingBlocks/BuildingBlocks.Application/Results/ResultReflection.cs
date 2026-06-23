namespace BuildingBlocks.Application.Results;

internal static class ResultReflection
{
    public static bool IsFailedResult(object? response)
    {
        if (response is null)
        {
            return false;
        }

        var type = response.GetType();

        if (type == typeof(Result))
        {
            return ((Result)response).IsFailure;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var isSuccess = (bool)type.GetProperty(nameof(Result.IsSuccess))!.GetValue(response)!;
            return !isSuccess;
        }

        return false;
    }

    public static object CreateFailure(Type responseType, string code, string message)
    {
        if (responseType == typeof(Result))
        {
            return Result.Failure(code, message);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var dataType = responseType.GetGenericArguments()[0];
            var failureMethod = typeof(Result<>)
                .MakeGenericType(dataType)
                .GetMethod(nameof(Result<object>.Failure), [typeof(string), typeof(string)]);

            return failureMethod!.Invoke(null, [code, message])!;
        }

        throw new InvalidOperationException($"Cannot convert business exception to {responseType.Name}.");
    }
}
