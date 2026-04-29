using System.Text.Json;
using Ambev.DeveloperEvaluation.WebApi.Common;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware;

/// <summary>
/// Translates domain / application exceptions into the standard error envelope
/// (<c>{ type, error, detail }</c>) defined in <c>/.doc/general-api.md</c>.
/// Sits after <see cref="ValidationExceptionMiddleware"/> in the pipeline so
/// 400 validation responses go through the dedicated handler.
/// </summary>
public class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteAsync(context, StatusCodes.Status404NotFound, "ResourceNotFound", "Resource not found", ex.Message);
        }
        catch (DomainException ex)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest, "DomainRuleViolation", "Domain rule violation", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteAsync(context, StatusCodes.Status409Conflict, "Conflict", "Operation conflict", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "InternalServerError", "Internal server error", ex.Message);
        }
    }

    private static Task WriteAsync(HttpContext context, int statusCode, string type, string error, string detail)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ApiErrorResponse
        {
            Type = type,
            Error = error,
            Detail = detail
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
