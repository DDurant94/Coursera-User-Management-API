namespace UserManagementAPI.Middleware;

public class TokenAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _validToken;

    // Paths that bypass token authentication (docs and spec endpoints)
    private static readonly string[] _publicPaths = ["/swagger"];

    public TokenAuthMiddleware(RequestDelegate next)
    {
        _next = next;
        // Set the AUTH_TOKEN environment variable to secure the API
        _validToken = Environment.GetEnvironmentVariable("AUTH_TOKEN") ?? string.Empty;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Allow public paths through without a token
        if (_publicPaths.Any(p => context.Request.Path.StartsWithSegments(p)))
        {
            await _next(context);
            return;
        }
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) ||
            !authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine(
                $"[AUTH] Rejected {context.Request.Method} {context.Request.Path}: missing or malformed Authorization header.");

            await WriteUnauthorizedAsync(context, "Unauthorized. A valid bearer token is required.");
            return;
        }

        var token = authHeader.ToString()["Bearer ".Length..].Trim();

        if (string.IsNullOrWhiteSpace(_validToken) || token != _validToken)
        {
            Console.Error.WriteLine(
                $"[AUTH] Rejected {context.Request.Method} {context.Request.Path}: invalid token.");

            await WriteUnauthorizedAsync(context, "Unauthorized. Invalid token.");
            return;
        }

        await _next(context);
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync($"{{\"error\":\"{message}\"}}");
    }
}

public static class TokenAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenAuthentication(this IApplicationBuilder app)
        => app.Use((context, next) => new TokenAuthMiddleware(next).InvokeAsync(context));
}
