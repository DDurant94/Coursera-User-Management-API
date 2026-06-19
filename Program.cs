using Microsoft.OpenApi.Models;
using UserManagementAPI.Middleware;
using UserManagementAPI.Services;
using UserManagementAPI.Utils;

// Load .env before anything else so environment variables are available
DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User Management API",
        Version = "v1",
        Description = "A CRUD API for managing company users."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "Enter your AUTH_TOKEN value (without the 'Bearer ' prefix)."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    options.EnableAnnotations();
});
builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Swagger UI at /swagger — spec served at /swagger/v1/swagger.json
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Management API v1"));

    // Diagnostic endpoint — triggers the error handling middleware (development only)
    app.MapGet("/api/diagnostic/error", () =>
    {
        throw new InvalidOperationException("Test exception triggered by diagnostic endpoint.");
    });
}

app.UseHttpsRedirection();

// Middleware pipeline (order is significant):
app.UseErrorHandling();       // 1. Outermost: catches all unhandled exceptions
app.UseTokenAuthentication(); // 2. Rejects requests with missing/invalid tokens
app.UseRequestLogging();      // 3. Logs method, path, and response status code

app.MapControllers();

await app.RunAsync();
