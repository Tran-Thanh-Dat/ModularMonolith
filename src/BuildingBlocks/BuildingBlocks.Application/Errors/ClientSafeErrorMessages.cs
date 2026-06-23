namespace BuildingBlocks.Application.Errors;

public static class ClientSafeErrorMessages
{
    public const string UnexpectedError = "An unexpected error occurred.";

    public const string EmailDeliveryFailed = "Email delivery failed.";

    public static string SanitizeForProduction(string? message, bool isDevelopment) =>
        isDevelopment && !string.IsNullOrWhiteSpace(message)
            ? message
            : UnexpectedError;
}
