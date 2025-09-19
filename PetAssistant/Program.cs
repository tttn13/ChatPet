using AspNetCoreRateLimit;
using PetAssistant.Endpoints;
using PetAssistant.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ===== Service Registration =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Clean service registration using extension methods
builder.Services.AddPetAssistantServices();
builder.Services.AddRedisService(builder.Configuration);
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddCustomCors(builder.Configuration,builder.Environment);
builder.Services.AddProductionSecurity(builder.Environment);
builder.Services.AddJwtAuthentication(builder.Configuration);


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
    // Don't use HTTPS redirection when deployed to Render
    // Render handles SSL termination at the proxy level
    // app.UseHttpsRedirection();
}

// Middleware pipeline (order matters!)
app.UseIpRateLimiting();

// Add CORS headers to ALL responses
app.Use(async (context, next) =>
{
    // Add CORS headers to every response
    context.Response.Headers.Add("Access-Control-Allow-Origin", "https://chat-pet-seven.vercel.app");
    context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");

    if (context.Request.Method == "OPTIONS")
    {
        // For preflight requests, add additional headers and return immediately
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With, Cookie");
        context.Response.Headers.Add("Access-Control-Max-Age", "86400");
        context.Response.StatusCode = 200;
        return;
    }

    await next();
});

app.UseCors("RestrictedCors");
app.UseAuthentication();
app.UseAuthorization();

// ===== Map Endpoints =====

// Root endpoint
app.MapGet("/", () => "Pet Assistant API - Your Virtual Veterinary Assistant")
    .ExcludeFromDescription();

// API endpoint groups
var api = app.MapGroup("/api").WithOpenApi(); //automatically includes the endpoint in your Swagger/OpenAPI documentation.

// Chat endpoints
api.MapGroup("/chat").RequireAuthorization().MapChatEndpoints();

// Pet profile CRUD endpoints - commented out as PetProfileEndpoints.cs was deleted
// api.MapGroup("/pet-profile").RequireAuthorization().MapPetProfileEndpoints();

api.MapGroup("/auth").MapAuthEndpoints();

// Health and monitoring endpoints (not under /api)
app.MapHealthEndpoints();

app.Run();

// Make Program class accessible for integration testing
public partial class Program { }