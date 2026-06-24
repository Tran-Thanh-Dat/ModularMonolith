using FluentValidation.TestHelper;
using MasterData.Application.Lookups;
using MasterData.Domain.Constants;
using MasterData.Domain.Enums;
using MasterData.Domain.Errors;
using Xunit;

namespace MasterData.Infrastructure.Tests;

public sealed class MasterDataApplicationValidatorTests
{
    [Fact]
    public void GetLookups_WhenTooManyGroupCodes_HasValidationError()
    {
        var codes = string.Join(',', Enumerable.Range(1, MasterDataConstants.MaxBatchGroupCodes + 1).Select(i => $"G{i}"));
        var validator = new GetLookupsQueryValidator();

        var result = validator.TestValidate(new GetLookupsQuery(
            codes,
            MasterDataScope.Global,
            null,
            null,
            false,
            null,
            true));

        result.ShouldHaveValidationErrorFor(q => q.GroupCodes)
            .WithErrorCode(LookupErrors.TooManyGroupCodes);
    }

    [Fact]
    public void GetLookups_WhenGroupCodeWhitespaceOnly_HasValidationError()
    {
        var validator = new GetLookupsQueryValidator();

        var result = validator.TestValidate(new GetLookupsQuery(
            " , , ",
            MasterDataScope.Global,
            null,
            null,
            false,
            null,
            true));

        result.ShouldHaveValidationErrorFor(q => q.GroupCodes);
    }
}
