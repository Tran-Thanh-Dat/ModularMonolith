using Notifications.Application.Validation;
using Xunit;

namespace Notifications.Application.Tests;

public sealed class TemplateRendererTests
{
    [Fact]
    public void Render_EncodesHtmlValues_WhenRequested()
    {
        const string template = "Hello {{UserName}}";
        var data = new Dictionary<string, string>
        {
            ["UserName"] = "<script>alert(1)</script>"
        };

        var result = TemplateRenderer.Render(template, data, encodeHtmlValues: true);

        Assert.Equal("Hello &lt;script&gt;alert(1)&lt;/script&gt;", result);
        Assert.DoesNotContain("<script>", result);
    }

    [Fact]
    public void Render_DoesNotEncodePlainTextValues_ByDefault()
    {
        const string template = "Hello {{UserName}}";
        var data = new Dictionary<string, string>
        {
            ["UserName"] = "<script>alert(1)</script>"
        };

        var result = TemplateRenderer.Render(template, data, encodeHtmlValues: false);

        Assert.Equal("Hello <script>alert(1)</script>", result);
    }

    [Fact]
    public void Render_LeavesUnknownPlaceholderUntouched()
    {
        const string template = "Hello {{UserName}}";
        var result = TemplateRenderer.Render(template, new Dictionary<string, string>(), encodeHtmlValues: true);

        Assert.Equal("Hello {{UserName}}", result);
    }
}
