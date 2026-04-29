using System.Text.Json;
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware
{
    /// <summary>
    /// Maps <see cref="ValidationException"/>s into the standard error envelope
    /// (<c>{ type, error, detail }</c>) defined in <c>/.doc/general-api.md</c>.
    /// </summary>
    public class ValidationExceptionMiddleware
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly RequestDelegate _next;

        public ValidationExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                await HandleValidationExceptionAsync(context, ex);
            }
        }

        private static Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            var detail = string.Join("; ", exception.Errors.Select(e => e.ErrorMessage));
            var response = new ApiErrorResponse
            {
                Type = "ValidationError",
                Error = "Invalid input data",
                Detail = string.IsNullOrEmpty(detail) ? exception.Message : detail
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }
}
