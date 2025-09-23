using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PetAssistant.Services;
using PetAssistant.Services.Auth;
using PetAssistant.Services.Redis;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;

namespace PetAssistant.Extensions;
// This custom class is created to organize our service registration and extends the built in IServiceCollection with custom methods.
// Extension methods MUST be defined in static class, have their first param use 'this' modifier
public static class ServiceCollectionExtensions
{
    // IServiceCollection is an built-in interface for service container
    public static IServiceCollection AddPetAssistantServices(this IServiceCollection services) //tells the c# compiler and this is an extension method for the IserviceCollection type
    {

        // HttpClient services - Scoped 1/request by default with HttpClientFactory
        //add a separate AddHttpClient registration for each service that needs an HttpClient.
        //Each service gets its own configured instance because of different settings, named clients (the factory tracks each service's http separately),
        // isolation (if 1 service has issues it doesn't affect others)
        services.AddHttpClient<IGroqService, GroqService>();
        services.AddHttpClient<IDiscordApi, DiscordApi>();
      
        // Stateless utility services - Singleton
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();

        // Shared cache service - Singleton (properly keyed by user/session)
        services.AddSingleton<IJwtTokenCacheService, JwtTokenCacheService>();
        services.AddSingleton<IConversationHistoryStorage, ConversationHistoryStorage>();

        // Background services - Singleton
        services.AddSingleton<IConversationCleanupPetService, ConversationCleanupService>();
        services.AddHostedService<ConversationCleanupService>();

        // Application-wide telemetry - Singleton
        services.AddSingleton<ITelemetryPetService, TelemetryService>();

        // Authentication services
        services.AddSingleton<IJwtTokenUsageService, JwtTokenService>();
        services.AddSingleton<IAuthService, AuthenticationService>();

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
    public static IServiceCollection AddRedisService(this IServiceCollection services, IConfiguration config)
    {
        var redisConfig = config.GetSection("Redis");

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            ConfigurationOptions configOptions;

            configOptions = new ConfigurationOptions();

            var host = redisConfig["Host"];
            var port = redisConfig["Port"];

            configOptions.EndPoints.Add(host, int.Parse(port));
            configOptions.User = redisConfig["User"];
            configOptions.Password = redisConfig["Password"];
            // if (!string.IsNullOrEmpty(redisConfig["User"]))
            // {
            //     Console.WriteLine($"Redis User: {redisConfig["User"]}");
            //     configOptions.User = redisConfig["User"];
            // }

            // if (!string.IsNullOrEmpty(redisConfig["Password"]))
            // {
            //     configOptions.Password = redisConfig["Password"];
            //     Console.WriteLine("Redis Password is set (not shown for security)");
            // }

            var originalConnectionString = redisConfig["ConnectionString"] ?? "";
            if (originalConnectionString.StartsWith("rediss://"))
            {
                configOptions.Ssl = true;
                configOptions.SslHost = redisConfig["Host"];
                Console.WriteLine("SSL enabled for Redis connection");
            }

            configOptions.AbortOnConnectFail = false; // Allow retries
            configOptions.ConnectTimeout = int.Parse(redisConfig["ConnectTimeout"] ?? "15000");
            configOptions.SyncTimeout = int.Parse(redisConfig["SyncTimeout"] ?? "10000");
            Console.WriteLine($"Redis Connect Timeout: {configOptions.ConnectTimeout}ms, Sync Timeout: {configOptions.SyncTimeout}ms");

            try
            {
                var connection = ConnectionMultiplexer.Connect(configOptions);
                return connection;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"REDIS CONNECTION ERROR: {ex.Message}");
                Console.WriteLine($"INNER EXCEPTION: {ex.InnerException?.Message}");
                throw; 
            }
        });

        services.AddSingleton<IRedisService, RedisService>();
        return services;
    }
    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration config, IWebHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                string originUrl = config["REACT_APP_URL"];

                policy.WithOrigins(originUrl, "http://localhost:3000")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
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

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // Set to true in production
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.ASCII.GetBytes(configuration["JwtSettings:SecretKey"] ??
                    throw new InvalidOperationException("JWT SecretKey is not configured"))),
                ValidateIssuer = true,
                ValidIssuer = configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            // Configure JWT Bearer to also check cookies for token and validate revocation status
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = async context =>
                {
                    if (string.IsNullOrEmpty(context.Request.Headers.Authorization))
                    {
                        context.Token = context.Request.Cookies["X-Access-Token"];
                    }
                    await Task.Yield();
                },
                OnTokenValidated = async context =>
                {
                    var tokenService = context.HttpContext.RequestServices.GetRequiredService<IJwtTokenCacheService>();
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerHandler>>();

                    try
                    {
                        var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
                        if (string.IsNullOrEmpty(token))
                        {
                            token = context.Request.Cookies["X-Access-Token"];
                        }

                        logger.LogWarning("JWT Token validated for request: {Token}", token);
                        // Check if token has been revoked in Redis
                        var isValid = await tokenService.IsTokenExisted(token);

                        if (!isValid)
                        {
                            logger.LogWarning("JWT token has been revoked for user {UserId}",
                                context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                            context.Fail("The token has been revoked");
                        }

                    }
                    catch (Exception ex)
                    {
                        // Log the error but don't fail authentication if Redis is unavailable
                        // This ensures the system remains available even if Redis is down
                        logger.LogError(ex, "Error checking token revocation status. Allowing request to proceed.");
                    }
                }
            };
        });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddDiscordAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication()
            .AddDiscord(options =>
            {
                options.ClientId = configuration["Discord:ClientId"] ?? throw new InvalidOperationException("Discord ClientId is not configured");
                options.ClientSecret = configuration["Discord:ClientSecret"] ?? throw new InvalidOperationException("Discord ClientSecret is not configured");
                options.CallbackPath = "/api/auth/discord/callback";
                options.Scope.Add("identify");
                options.Scope.Add("email");
            });

        return services;
    }

}

// Why Static?

//   - No instance needed - just utility methods
//   - Extension method requirement - C# language rule
//   - Performance - no object allocation
//   - Intellisense - appears as if it's part of IServiceCollection