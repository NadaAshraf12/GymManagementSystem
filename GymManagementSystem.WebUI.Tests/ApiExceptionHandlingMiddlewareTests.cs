using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.WebUI.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GymManagementSystem.WebUI.Tests;

public class ApiExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task Returns_401_ApiResponse_With_CorrelationId_For_Unauthorized()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.ContentType = "application/json";
        context.Items[CorrelationIdMiddleware.HeaderName] = "corr-123";
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "corr-123";
        context.Response.Body = new MemoryStream();

        var middleware = new ApiExceptionHandlingMiddleware(
            _ => throw new UnauthorizedAccessException("nope"),
            NullLogger<ApiExceptionHandlingMiddleware>.Instance);

        await middleware.Invoke(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<ApiResponse<object>>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(response);
        Assert.False(response!.Success);
        Assert.Equal("corr-123", response.CorrelationId);
    }

    [Fact]
    public async Task Returns_400_For_ValidationException()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.ContentType = "application/json";
        context.Response.Body = new MemoryStream();

        var middleware = new ApiExceptionHandlingMiddleware(
            _ => throw new ValidationException("bad"),
            NullLogger<ApiExceptionHandlingMiddleware>.Instance);

        await middleware.Invoke(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }
}
