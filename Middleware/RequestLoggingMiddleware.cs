namespace UserManagementAPI.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        Console.WriteLine(
            $"[LOG] Incoming  >> {context.Request.Method} {context.Request.Path}");

        await _next(context);

        Console.WriteLine(
            $"[LOG] Outgoing  << {context.Request.Method} {context.Request.Path} {context.Response.StatusCode}");
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        => app.Use((context, next) => new RequestLoggingMiddleware(next).InvokeAsync(context));
}
