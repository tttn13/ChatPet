using AspNetCoreRateLimit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PetAssistant.HealthChecks;
using PetAssistant.Services;

namespace PetAssistant.Extensions;
//This custom class is created to organize our service registration and extends the built in IServiceCollection with custom methods.   
//Extension methods MUST be in static classes
public static class ServiceCollectionExtensions
{
    // IServiceCollection is an built-in interface for service container
    public static IServiceCollection AddPetAssistantServices(this IServiceCollection services)
    {
        // Core Services
        
        // HttpClient services - Scoped by default with HttpClientFactory
        services.AddHttpClient<IGroqService, GroqService>();
        // for eg, add a separate AddHttpClient registration for each service that needs an HttpClient.
        // Each service gets its own configured instance because of different settings, named clients (the factory tracks each service's http separately),
        // isolation (if 1 service has issues it doesn't affect others)

          //services.AddHttpClient<IGroqService, GroqService>();
          //services.AddHttpClient<IWeatherService, WeatherService>();
          //services.AddHttpClient<IPaymentService, PaymentService>();

        // User-specific services - Scoped (one per request)
        services.AddScoped<IPetProfileService, PetProfileService>();
        
        // Stateless utility services - Singleton
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
        
        // Shared cache service - Singleton (properly keyed by user/session)
        services.AddSingleton<ICacheService, CacheService>();
        
        // Background services - Singleton
        services.AddSingleton<IConversationCleanupService, ConversationCleanupService>();
        services.AddHostedService<ConversationCleanupService>();
        
        // Application-wide telemetry - Singleton
        services.AddSingleton<ITelemetryService, TelemetryService>();

        return services;
    }
    // MUST be static method with 'this' keyword
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        return services;
    }

    public static IServiceCollection AddCustomCors(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("RestrictedCors", policy =>
            {
                if (environment.IsDevelopment())
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

        return services;
    }

    public static IServiceCollection AddProductionSecurity(this IServiceCollection services, IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = StatusCodes.Status301MovedPermanently;
                options.HttpsPort = 443;
            });
        }

        return services;
    }

    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            // Original health check (implements IHealthCheck directly)
            .AddCheck<GroqApiHealthCheck>("groq-api-original")
            
            // New improved version (uses abstract base class)
            .AddCheck<ImprovedGroqApiHealthCheck>("groq-api-improved")
            
            // Cache health check (uses abstract base class)
            .AddCheck<CacheHealthCheck>("cache")
            
            // Database health check (uses abstract base class)
            .AddCheck<DatabaseHealthCheck>("database")
            
            // Memory check (inline lambda)
            .AddCheck("memory", () =>
            {
                var gc = GC.GetTotalMemory(false);
                return gc > 100 * 1024 * 1024 // 100MB threshold
                    ? HealthCheckResult.Degraded($"High memory usage: {gc / 1024 / 1024}MB")
                    : HealthCheckResult.Healthy($"Memory usage: {gc / 1024 / 1024}MB");
            });

        return services;
    }
}

// Why Static?

//   - No instance needed - just utility methods
//   - Extension method requirement - C# language rule
//   - Performance - no object allocation
//   - Intellisense - appears as if it's part of IServiceCollection