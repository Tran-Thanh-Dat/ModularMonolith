using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using Xunit;

namespace BuildingBlocks.Application.Tests.Results;

public sealed class ResultTests
{
    [Fact]
    public void Success_ReturnsSuccessfulResultWithoutErrors()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(string.Empty, result.Code);
    }

    [Fact]
    public void Failure_ReturnsFailedResultWithCodeAndMessage()
    {
        var result = Result.Failure("Test.Failed", "Something failed.");

        Assert.True(result.IsFailure);
        Assert.Equal("Test.Failed", result.Code);
        Assert.Equal("Something failed.", result.Message);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void ValidationFailure_ReturnsFailedResultWithMultipleErrors()
    {
        var errors = new[]
        {
            new Error(CommonErrors.ValidationError, "Name is required.", "Name"),
            new Error(CommonErrors.ValidationError, "Code is required.", "Code")
        };

        var result = Result.ValidationFailure(errors);

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrors.ValidationError, result.Code);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void GenericSuccess_ReturnsData()
    {
        var result = Result<string>.Success("payload");

        Assert.True(result.IsSuccess);
        Assert.Equal("payload", result.Data);
    }

    [Fact]
    public void GenericFailure_HasNoData()
    {
        var result = Result<string>.Failure("Test.Failed", "Failed.");

        Assert.True(result.IsFailure);
        Assert.Null(result.Data);
    }
}
