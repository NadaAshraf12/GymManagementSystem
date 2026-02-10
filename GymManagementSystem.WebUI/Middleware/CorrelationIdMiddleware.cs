using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GymManagementSystem.WebUI.Middleware;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            _logger.LogInformation("Request {Method} {Path} User={UserId}", context.Request.Method, context.Request.Path, userId);
            await _next(context);

            if (IsImportantDomainEvent(context.Request, context.Response))
            {
                _logger.LogInformation("DomainEvent {Method} {Path} Status={StatusCode} User={UserId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    userId);
            }
        }
    }

    private static bool IsImportantDomainEvent(HttpRequest request, HttpResponse response)
    {
        if (!request.Path.StartsWithSegments("/api"))
        {
            return false;
        }

        if (response.StatusCode != StatusCodes.Status200OK && response.StatusCode != StatusCodes.Status201Created)
        {
            return false;
        }

        return HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method) || HttpMethods.IsDelete(request.Method);
    }
}
