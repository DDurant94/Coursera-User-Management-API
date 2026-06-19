using Microsoft.AspNetCore.Http;
using UserManagementAPI.Middleware;

namespace UserManagementAPI.Tests.Middleware;

// Each xUnit test gets a fresh class instance, so console capture is per-test safe
public class MiddlewareTests : IDisposable
{
    private readonly StringWriter _console = new();
    private readonly TextWriter _originalOut = Console.Out;
    private readonly TextWriter _originalError = Console.Error;

    public MiddlewareTests()
    {
        Console.SetOut(_console);
        Console.SetError(_console);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
        _console.Dispose();
    }

    // Creates an HttpContext with a writable, seekable response body
    private static DefaultHttpContext CreateContext(string method = "GET", string path = "/api/users")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(context.Response.Body).ReadToEndAsync();
    }

    // -------------------------------------------------------------------------
    // ErrorHandlingMiddleware
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ErrorHandling_Returns500_WhenNextThrows()
    {
        var context = CreateContext();
        var middleware = new ErrorHandlingMiddleware(_ => throw new Exception("boom"));

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task ErrorHandling_ReturnsJsonBody_WhenNextThrows()
    {
        var context = CreateContext();
        var middleware = new ErrorHandlingMiddleware(_ => throw new Exception("boom"));

        await middleware.InvokeAsync(context);

        var body = await ReadBodyAsync(context);
        Assert.Contains("Internal server error.", body);
    }

    [Fact]
    public async Task ErrorHandling_SetsApplicationJsonContentType_WhenNextThrows()
    {
        var context = CreateContext();
        var middleware = new ErrorHandlingMiddleware(_ => throw new Exception("boom"));

        await middleware.InvokeAsync(context);

        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task ErrorHandling_CallsNext_WhenNoExceptionIsThrown()
    {
        var context = CreateContext();
        var nextCalled = false;
        var middleware = new ErrorHandlingMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task ErrorHandling_LogsErrorToConsole_WhenNextThrows()
    {
        var context = CreateContext("POST", "/api/users");
        var middleware = new ErrorHandlingMiddleware(_ => throw new Exception("test error message"));

        await middleware.InvokeAsync(context);

        var log = _console.ToString();
        Assert.Contains("[ERROR]", log);
        Assert.Contains("test error message", log);
    }

    // -------------------------------------------------------------------------
    // TokenAuthMiddleware
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TokenAuth_Returns401_WhenNoAuthorizationHeaderPresent()
    {
        var context = CreateContext();
        var middleware = new TokenAuthMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task TokenAuth_ReturnsRequiredMessage_WhenNoAuthorizationHeaderPresent()
    {
        var context = CreateContext();
        var middleware = new TokenAuthMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var body = await ReadBodyAsync(context);
        Assert.Contains("A valid bearer token is required", body);
    }

    [Fact]
    public async Task TokenAuth_Returns401_WhenHeaderMissesBearerPrefix()
    {
        var context = CreateContext();
        context.Request.Headers["Authorization"] = "not-bearer-format";
        var middleware = new TokenAuthMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task TokenAuth_Returns401_WhenTokenIsWrong()
    {
        Environment.SetEnvironmentVariable("AUTH_TOKEN", "correct-token");
        try
        {
            var context = CreateContext();
            context.Request.Headers["Authorization"] = "Bearer wrong-token";
            var middleware = new TokenAuthMiddleware(_ => Task.CompletedTask);

            await middleware.InvokeAsync(context);

            Assert.Equal(401, context.Response.StatusCode);
            var body = await ReadBodyAsync(context);
            Assert.Contains("Invalid token", body);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AUTH_TOKEN", null);
        }
    }

    [Fact]
    public async Task TokenAuth_Returns401_WhenAuthTokenEnvVarNotSet()
    {
        Environment.SetEnvironmentVariable("AUTH_TOKEN", null);
        var context = CreateContext();
        context.Request.Headers["Authorization"] = "Bearer any-token";
        var middleware = new TokenAuthMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task TokenAuth_CallsNext_WhenTokenIsValid()
    {
        Environment.SetEnvironmentVariable("AUTH_TOKEN", "correct-token");
        try
        {
            var context = CreateContext();
            context.Request.Headers["Authorization"] = "Bearer correct-token";
            var nextCalled = false;
            var middleware = new TokenAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AUTH_TOKEN", null);
        }
    }

    [Fact]
    public async Task TokenAuth_LogsMissingHeader_ToConsole()
    {
        var context = CreateContext("GET", "/api/users");
        var middleware = new TokenAuthMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var log = _console.ToString();
        Assert.Contains("[AUTH]", log);
        Assert.Contains("missing or malformed", log);
    }

    [Fact]
    public async Task TokenAuth_LogsInvalidToken_ToConsole()
    {
        Environment.SetEnvironmentVariable("AUTH_TOKEN", "correct-token");
        try
        {
            var context = CreateContext("GET", "/api/users");
            context.Request.Headers["Authorization"] = "Bearer bad-token";
            var middleware = new TokenAuthMiddleware(_ => Task.CompletedTask);

            await middleware.InvokeAsync(context);

            var log = _console.ToString();
            Assert.Contains("[AUTH]", log);
            Assert.Contains("invalid token", log);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AUTH_TOKEN", null);
        }
    }

    // -------------------------------------------------------------------------
    // RequestLoggingMiddleware
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RequestLogging_CallsNext()
    {
        var context = CreateContext();
        var nextCalled = false;
        var middleware = new RequestLoggingMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task RequestLogging_LogsIncomingMethodAndPath()
    {
        var context = CreateContext("POST", "/api/users");
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var log = _console.ToString();
        Assert.Contains("[LOG]", log);
        Assert.Contains("Incoming", log);
        Assert.Contains("POST", log);
        Assert.Contains("/api/users", log);
    }

    [Fact]
    public async Task RequestLogging_LogsOutgoingStatusCode()
    {
        var context = CreateContext("GET", "/api/users");
        var middleware = new RequestLoggingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        var log = _console.ToString();
        Assert.Contains("[LOG]", log);
        Assert.Contains("Outgoing", log);
        Assert.Contains("200", log);
    }

    [Fact]
    public async Task RequestLogging_LogsBothIncomingAndOutgoing()
    {
        var context = CreateContext("DELETE", "/api/users/1");
        var middleware = new RequestLoggingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 204;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        var log = _console.ToString();
        Assert.Contains("Incoming", log);
        Assert.Contains("Outgoing", log);
        Assert.Contains("DELETE", log);
        Assert.Contains("204", log);
    }
}
