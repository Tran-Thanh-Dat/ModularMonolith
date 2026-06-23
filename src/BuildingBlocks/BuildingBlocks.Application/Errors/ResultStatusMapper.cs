namespace BuildingBlocks.Application.Errors;

public static class ResultStatusMapper
{
    public static int MapStatusCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return 500;
        }

        return code switch
        {
            CommonErrors.ValidationError or CommonErrors.BadRequest => 400,
            CommonErrors.Unauthorized
                or AuthErrors.InvalidCredentials
                or AuthErrors.UserInactive
                or AuthErrors.TokenExpired
                or AuthErrors.RefreshTokenInvalid
                or AuthErrors.RefreshTokenReuseDetected => 401,
            CommonErrors.Forbidden or AuthErrors.PermissionDenied => 403,
            NotificationErrors.Forbidden
                or NotificationErrors.ViewAllRequired
                or NotificationErrors.ManageRequired => 403,
            CommonErrors.NotFound => 404,
            CommonErrors.Conflict => 409,
            CommonErrors.UnknownError => 500,
            EmailErrors.SendFailed => 502,
            _ when code.EndsWith(".NotFound", StringComparison.Ordinal) => 404,
            _ when code.EndsWith(".AlreadyExists", StringComparison.Ordinal) => 409,
            _ when code.EndsWith("AlreadyExists", StringComparison.Ordinal) => 409,
            _ when code.EndsWith(".Invalid", StringComparison.Ordinal) => 400,
            _ => 400
        };
    }
}
