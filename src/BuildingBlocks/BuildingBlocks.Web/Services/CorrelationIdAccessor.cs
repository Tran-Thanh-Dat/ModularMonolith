using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Web.Constants;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Web.Services;

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private const string HttpContextItemKey = "CorrelationId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CorrelationId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return string.Empty;
            }

            if (httpContext.Items.TryGetValue(HttpContextItemKey, out var value) && value is string correlationId)
            {
                return correlationId;
            }

            if (httpContext.Request.Headers.TryGetValue(HeaderNames.XCorrelationId, out var headerValue))
            {
                return headerValue.ToString();
            }

            return string.Empty;
        }
    }
}
