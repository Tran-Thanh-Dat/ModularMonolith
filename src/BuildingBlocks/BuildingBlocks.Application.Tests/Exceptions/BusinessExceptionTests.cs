using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using Xunit;

namespace BuildingBlocks.Application.Tests.Exceptions;

public sealed class BusinessExceptionTests
{
    [Fact]
    public void NotFoundException_CarriesCode()
    {
        var exception = new NotFoundException(CategoryErrors.NotFound, "Category not found.");

        Assert.Equal(CategoryErrors.NotFound, exception.Code);
        Assert.Equal("Category not found.", exception.Message);
    }

    [Fact]
    public void ConflictException_CarriesCode()
    {
        var exception = new ConflictException(CategoryErrors.CodeAlreadyExists, "Duplicate code.");

        Assert.Equal(CategoryErrors.CodeAlreadyExists, exception.Code);
    }

    [Fact]
    public void UnauthorizedException_CarriesCode()
    {
        var exception = new UnauthorizedException(CommonErrors.Unauthorized, "Unauthorized.");

        Assert.Equal(CommonErrors.Unauthorized, exception.Code);
    }

    [Fact]
    public void ForbiddenException_CarriesCode()
    {
        var exception = new ForbiddenException(CommonErrors.Forbidden, "Forbidden.");

        Assert.Equal(CommonErrors.Forbidden, exception.Code);
    }

    [Fact]
    public void ValidationException_CarriesErrors()
    {
        var exception = new ValidationException(
            [new Error(CommonErrors.ValidationError, "Name required.", "Name")]);

        Assert.Equal(CommonErrors.ValidationError, exception.Code);
        Assert.Single(exception.Errors);
    }
}
