using AspNetCoreRateLimit;
using PetAssistant.Endpoints;
using PetAssistant.Extensions;
using PetAssistant.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ===== Service Registration =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Clean service registration using extension methods
builder.Services.AddPetAssistantServices();
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddCustomCors(builder.Environment);
builder.Services.AddProductionSecurity(builder.Environment);
builder.Services.AddCustomHealthChecks();

/* Original service registration (commented out - now using extensions):
// Core Services
builder.Services.AddHttpClient<IGroqService, GroqService>();
builder.Services.AddSingleton<IGroqService, GroqService>();
builder.Services.AddSingleton<IPetProfileService, PetProfileService>();
builder.Services.AddSingleton<IValidationService, ValidationService>();
builder.Services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<IConversationCleanupService, ConversationCleanupService>();
builder.Services.AddHostedService<ConversationCleanupService>();
builder.Services.AddSingleton<ITelemetryService, TelemetryService>();

// Rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("RestrictedCors", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins("https://yourdomain.com") // Replace with actual domain
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// HTTPS redirection in production
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpsRedirection(options =>
    {
        options.RedirectStatusCode = StatusCodes.Status301MovedPermanently;
        options.HttpsPort = 443;
    });
}

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<GroqApiHealthCheck>("groq-api")
    .AddCheck("memory", () =>
    {
        var gc = GC.GetTotalMemory(false);
        return gc > 100 * 1024 * 1024 // 100MB threshold
            ? HealthCheckResult.Degraded($"High memory usage: {gc / 1024 / 1024}MB")
            : HealthCheckResult.Healthy($"Memory usage: {gc / 1024 / 1024}MB");
    });
*/

// ===== Build Application =====
var app = builder.Build();

// ===== Configure Pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

// Middleware pipeline (order matters!)
app.UseSecurityMiddleware();
app.UseIpRateLimiting();
app.UseCors("RestrictedCors");

// ===== Map Endpoints =====

// Root endpoint
app.MapGet("/", () => "Pet Assistant API - Your Virtual Veterinary Assistant")
    .ExcludeFromDescription();

// API endpoint groups
var api = app.MapGroup("/api")
    .WithOpenApi(); //automatically includes the endpoint in your Swagger/OpenAPI documentation.

// Chat endpoints
api.MapGroup("/chat")
   .MapChatEndpoints();

// Pet profile CRUD endpoints
api.MapGroup("/pet-profile")
   .MapPetProfileEndpoints();

// Health and monitoring endpoints (not under /api)
app.MapHealthEndpoints();

app.Run();

// Make Program class accessible for integration testing
public partial class Program { }