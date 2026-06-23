using BuildingBlocks.Application.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace BuildingBlocks.Application.Tests.Logging;

public sealed class SensitiveDataMaskerTests
{
    [Fact]
    public void MaskJson_WhenCookieFieldPresent_MasksValue()
    {
        var masker = CreateMasker();

        var masked = masker.MaskJson("""{"cookie":"session=abc123","userName":"demo"}""");

        Assert.Contains("\"cookie\":\"******\"", masked);
        Assert.Contains("\"userName\":\"demo\"", masked);
    }

    [Fact]
    public void MaskJson_WhenSetCookieFieldPresent_MasksValue()
    {
        var masker = CreateMasker();

        var masked = masker.MaskJson("""{"set-cookie":"session=abc123"}""");

        Assert.Contains("\"set-cookie\":\"******\"", masked);
    }

    [Theory]
    [InlineData("Authorization")]
    [InlineData("Cookie")]
    [InlineData("Set-Cookie")]
    public void IsSensitiveHeader_WhenKnownHeader_ReturnsTrue(string headerName)
    {
        Assert.True(SensitiveHeaderGuard.IsSensitiveHeader(headerName));
    }

    private static SensitiveDataMasker CreateMasker() =>
        new(Options.Create(new SensitiveDataOptions()));
}
