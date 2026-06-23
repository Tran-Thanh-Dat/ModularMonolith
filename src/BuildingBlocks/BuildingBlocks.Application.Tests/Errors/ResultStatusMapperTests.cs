using BuildingBlocks.Application.Errors;
using Xunit;

namespace BuildingBlocks.Application.Tests.Errors;

public sealed class ResultStatusMapperTests
{
    [Theory]
    [InlineData(CommonErrors.ValidationError, 400)]
    [InlineData(CommonErrors.BadRequest, 400)]
    [InlineData(CommonErrors.Unauthorized, 401)]
    [InlineData(AuthErrors.InvalidCredentials, 401)]
    [InlineData(CommonErrors.Forbidden, 403)]
    [InlineData(AuthErrors.PermissionDenied, 403)]
    [InlineData(NotificationErrors.Forbidden, 403)]
    [InlineData(CommonErrors.NotFound, 404)]
    [InlineData(CommonErrors.Conflict, 409)]
    [InlineData(CategoryErrors.CodeAlreadyExists, 409)]
    [InlineData(CommonErrors.UnknownError, 500)]
    public void MapStatusCode_ReturnsExpectedHttpStatus(string code, int expectedStatus)
    {
        var status = ResultStatusMapper.MapStatusCode(code);

        Assert.Equal(expectedStatus, status);
    }

    [Fact]
    public void MapStatusCode_Returns500ForEmptyCode()
    {
        Assert.Equal(500, ResultStatusMapper.MapStatusCode(string.Empty));
    }
}
