using System.Diagnostics;

namespace GymManagementSystem.WebUI.Middleware;

public class PerformanceTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceTimingMiddleware> _logger;

    public PerformanceTimingMiddleware(RequestDelegate next, ILogger<PerformanceTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        _logger.LogInformation(
            "RequestTiming Path={Path} Method={Method} StatusCode={StatusCode} ElapsedMs={ElapsedMs}",
            context.Request.Path,
            context.Request.Method,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds);
    }
}
