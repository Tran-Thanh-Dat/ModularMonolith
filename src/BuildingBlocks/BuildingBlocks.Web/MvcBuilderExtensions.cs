using BuildingBlocks.Application.Errors;
using BuildingBlocks.Web.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Web;

public static class MvcBuilderExtensions
{
    public static IMvcBuilder AddStandardApiBehavior(this IMvcBuilder builder)
    {
        return builder.ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var traceId = context.HttpContext.TraceIdentifier;
                var errors = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .SelectMany(entry => entry.Value!.Errors.Select(error => new ApiError
                    {
                        Code = CommonErrors.ValidationError,
                        Message = string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "The input is invalid."
                            : error.ErrorMessage,
                        Field = entry.Key
                    }))
                    .ToList();

                var response = ApiResponse<object>.Fail(
                    CommonErrors.ValidationError,
                    "One or more validation errors occurred.",
                    errors,
                    traceId);

                return new BadRequestObjectResult(response);
            };
        });
    }
}
