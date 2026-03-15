using System.IO;
using System.Threading.Tasks;
using GymManagementSystem.WebUI.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GymManagementSystem.WebUI.Tests;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Adds_CorrelationId_Header_When_Missing()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, NullLogger<CorrelationIdMiddleware>.Instance);
        await middleware.Invoke(context);

        Assert.True(context.Response.Headers.ContainsKey(CorrelationIdMiddleware.HeaderName));
        Assert.True(context.Items.ContainsKey(CorrelationIdMiddleware.HeaderName));
    }

    [Fact]
    public async Task Preserves_CorrelationId_Header_When_Present()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "abc123";
        context.Response.Body = new MemoryStream();

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, NullLogger<CorrelationIdMiddleware>.Instance);
        await middleware.Invoke(context);

        Assert.Equal("abc123", context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
        Assert.Equal("abc123", context.Items[CorrelationIdMiddleware.HeaderName]);
    }
}