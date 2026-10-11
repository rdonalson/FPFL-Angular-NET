using Fpfl.Api.Exceptions;
using Fpfl.Api.Health;
using Fpfl.Api.Mappers;
using Fpfl.Api.Responses;
using Fpfl.Common;
using Fpfl.Items;
using Fpfl.Items.Infrastructure.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------
// Connection string lives in user-secrets (dev) or an environment variable (prod):
//   dotnet user-secrets set "ConnectionStrings:Fpfl" "Host=localhost;Port=5432;Database=FPFL-V2;Username=postgres;Password=..." --project src/Fpfl.Api
//   ConnectionStrings__Fpfl=...
var connectionString = builder.Configuration.GetConnectionString("Fpfl")
    ?? throw new InvalidOperationException(
        "Connection string 'ConnectionStrings:Fpfl' is not configured. " +
        "Set it with 'dotnet user-secrets set' or the ConnectionStrings__Fpfl environment variable.");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// ---------------------------------------------------------------------------
// Bounded contexts
// ---------------------------------------------------------------------------
builder.Services.AddFpflCommon();
builder.Services.AddFpflItems(connectionString);

// ---------------------------------------------------------------------------
// API layer
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IApiResponseFactory, ApiResponseFactory>();
builder.Services.AddSingleton<ItemTypeMapper>();

builder.Services.AddControllers();

// Model-binding errors (bad JSON, wrong types) use the same envelope as every other error.
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = string.Join("; ", context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error =>
                string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? $"Invalid value for '{entry.Key}'"
                    : error.ErrorMessage)));

        return new BadRequestObjectResult(new ApiResponse<object?>(StatusCodes.Status400BadRequest, message));
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info ??= new();
    document.Info.Title = "FPFL API";
    document.Info.Description = "API documentation for FPFL backend services";
    document.Info.Version = "1.0.0";
    return Task.CompletedTask;
}));

builder.Services.AddHealthChecks()
    .AddCheck<ItemsDatabaseHealthCheck>("db");

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();

// Landing page: wwwroot/index.html at "/"
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors();
app.UseAuthorization();

// OpenAPI document + Swagger UI (linked from the landing page)
app.MapOpenApi();                       // /openapi/v1.json
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "FPFL API v1");
    options.RoutePrefix = "swagger-ui"; // /swagger-ui
    options.DocumentTitle = "FPFL API";
});

app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthResponseWriter.WriteAsync });

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
