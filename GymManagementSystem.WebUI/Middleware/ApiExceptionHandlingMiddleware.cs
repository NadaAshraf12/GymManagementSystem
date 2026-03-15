using FluentValidation;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymManagementSystem.WebUI.Middleware;

public class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

    public ApiExceptionHandlingMiddleware(RequestDelegate next, ILogger<ApiExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (IsApiRequest(context.Request))
        {
            await HandleApiExceptionAsync(context, ex);
        }
    }

    private async Task HandleApiExceptionAsync(HttpContext context, Exception ex)
    {
        var (status, message, errors) = MapException(ex);
        var correlationId = context.Items[CorrelationIdMiddleware.HeaderName]?.ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = context.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        }

        _logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = status;

        var response = ApiResponse<object>.Fail(message, status, errors);
        response.CorrelationId = correlationId;
        await context.Response.WriteAsJsonAsync(response);
    }

    private static (int Status, string Message, List<string> Errors) MapException(Exception ex)
    {
        switch (ex)
        {
            case UnauthorizedException:
                return (StatusCodes.Status401Unauthorized, ex.Message, new List<string> { ex.Message });
            case NotFoundException:
                return (StatusCodes.Status404NotFound, ex.Message, new List<string> { ex.Message });
            case AppValidationException:
                return (StatusCodes.Status400BadRequest, ex.Message, new List<string> { ex.Message });
            case UnauthorizedAccessException:
                return (StatusCodes.Status401Unauthorized, ex.Message, new List<string> { ex.Message });
            case KeyNotFoundException:
                return (StatusCodes.Status404NotFound, ex.Message, new List<string> { ex.Message });
            case ValidationException ve:
                var errs = ve.Errors.Select(e => e.ErrorMessage).ToList();
                return (StatusCodes.Status400BadRequest, "Validation failed.", errs);
            case DbUpdateConcurrencyException:
                return (StatusCodes.Status409Conflict, "The record was modified by another operation. Please retry.", new List<string> { ex.Message });
            default:
                return (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", new List<string> { ex.Message });
        }
    }

    private static bool IsApiRequest(HttpRequest request)
    {
        if (request.Path.StartsWithSegments("/api"))
        {
            return true;
        }

        var contentType = request.ContentType ?? string.Empty;
        var accept = request.Headers.Accept.ToString();

        return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)
               || accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }
}
