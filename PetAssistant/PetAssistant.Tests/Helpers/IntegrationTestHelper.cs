using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PetAssistant.Models;
using PetAssistant.Services;
using System.Net;

namespace PetAssistant.Tests.Helpers;

/// <summary>
/// Helper class for integration tests that provides consistent service mocking
/// across all endpoint tests. Uses composition over inheritance for flexibility.
/// </summary>
public class IntegrationTestHelper
{
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationTestHelper(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Creates an HTTP client with all required services mocked.
    /// Pass custom mocks to override default mock behaviors for specific tests.
    /// </summary>
    public HttpClient CreateTestClient(params (Type serviceType, object mock)[] customMocks)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all real service implementations
                RemoveAllApplicationServices(services);

                // Add default mocks for all services
                AddDefaultServiceMocks(services);

                // Override with custom mocks if provided
                foreach (var (serviceType, mock) in customMocks)
                {
                    RemoveService(services, serviceType);
                    services.AddSingleton(serviceType, mock);
                }
            });
        }).CreateClient();
    }

    /// <summary>
    /// Creates a preconfigured Mock<IGroqService> with common setup.
    /// </summary>
    public static Mock<IGroqService> CreateGroqServiceMock(
        string response = "Default test response",
        string? thinking = null,
        string sessionId = "test-session")
    {
        var mock = new Mock<IGroqService>();
        mock.Setup(x => x.GetPetAdviceAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<PetProfile?>()))
            .ReturnsAsync((response, thinking, sessionId));
        return mock;
    }

    /// <summary>
    /// Creates a preconfigured Mock<IValidationService> with common setup.
    /// </summary>
    public static Mock<IValidationService> CreateValidationServiceMock(
        bool isValid = true,
        List<string>? errors = null)
    {
        var mock = new Mock<IValidationService>();
        
        // Default sanitize behavior - return input as-is
        mock.Setup(x => x.SanitizeInput(It.IsAny<string>()))
            .Returns<string>(input => input ?? "");
        
        // Default validation behaviors
        mock.Setup(x => x.ValidateCreatePetProfile(It.IsAny<CreatePetProfileRequest>()))
            .Returns(new ValidationResult(isValid, errors ?? new List<string>()));
        
        mock.Setup(x => x.ValidateUpdatePetProfile(It.IsAny<UpdatePetProfileRequest>()))
            .Returns(new ValidationResult(isValid, errors ?? new List<string>()));
        
        return mock;
    }

    /// <summary>
    /// Creates a preconfigured Mock<IPetProfileService> with common setup.
    /// </summary>
    public static Mock<IPetProfileService> CreatePetProfileServiceMock(PetProfile? profile = null)
    {
        var mock = new Mock<IPetProfileService>();
        
        if (profile != null)
        {
            mock.Setup(x => x.GetProfileAsync(It.IsAny<string>()))
                .ReturnsAsync(profile);
            
            mock.Setup(x => x.CreateProfileAsync(It.IsAny<string>(), It.IsAny<CreatePetProfileRequest>()))
                .ReturnsAsync(profile);
            
            mock.Setup(x => x.UpdateProfileAsync(It.IsAny<string>(), It.IsAny<UpdatePetProfileRequest>()))
                .ReturnsAsync(profile);
        }
        
        return mock;
    }

    /// <summary>
    /// Creates a preconfigured Mock<ITelemetryService> with common setup.
    /// </summary>
    public static Mock<ITelemetryService> CreateTelemetryServiceMock()
    {
        var mock = new Mock<ITelemetryService>();
        // Telemetry methods typically don't return values, just track calls
        return mock;
    }

    /// <summary>
    /// Creates a preconfigured Mock<IErrorHandlingService> with common setup.
    /// </summary>
    public static Mock<IErrorHandlingService> CreateErrorHandlingServiceMock()
    {
        var mock = new Mock<IErrorHandlingService>();
        mock.Setup(x => x.HandleGroqApiError(It.IsAny<HttpStatusCode>(), It.IsAny<string?>()))
            .Returns(new ErrorResponse("An error occurred", "API_ERROR", Guid.NewGuid().ToString(), DateTime.UtcNow));
        mock.Setup(x => x.HandleGenericError(It.IsAny<Exception>()))
            .Returns(new ErrorResponse("An unexpected error occurred", "GENERIC_ERROR", Guid.NewGuid().ToString(), DateTime.UtcNow));
        return mock;
    }

    /// <summary>
    /// Creates a preconfigured Mock<ICacheService> with common setup.
    /// </summary>
    public static Mock<ICacheService> CreateCacheServiceMock()
    {
        var mock = new Mock<ICacheService>();
        mock.Setup(x => x.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<PetProfile?>()))
            .ReturnsAsync((string?)null); // Default: no cache hit
        return mock;
    }

    /// <summary>
    /// Creates a preconfigured Mock<IConversationCleanupService> with common setup.
    /// </summary>
    public static Mock<IConversationCleanupService> CreateConversationCleanupServiceMock()
    {
        var mock = new Mock<IConversationCleanupService>();
        // Background service, typically no methods called directly
        return mock;
    }

    /// <summary>
    /// Removes all application services from the DI container.
    /// </summary>
    private static void RemoveAllApplicationServices(IServiceCollection services)
    {
        RemoveService<IGroqService>(services);
        RemoveService<IPetProfileService>(services);
        RemoveService<IValidationService>(services);
        RemoveService<ITelemetryService>(services);
        RemoveService<IErrorHandlingService>(services);
        RemoveService<ICacheService>(services);
        RemoveService<IConversationCleanupService>(services);
    }

    /// <summary>
    /// Adds default mock implementations for all services.
    /// These provide minimal "do nothing" behavior to prevent DI failures.
    /// </summary>
    private static void AddDefaultServiceMocks(IServiceCollection services)
    {
        services.AddSingleton(CreateGroqServiceMock().Object);
        services.AddSingleton(CreatePetProfileServiceMock().Object);
        services.AddSingleton(CreateValidationServiceMock().Object);
        services.AddSingleton(CreateTelemetryServiceMock().Object);
        services.AddSingleton(CreateErrorHandlingServiceMock().Object);
        services.AddSingleton(CreateCacheServiceMock().Object);
        services.AddSingleton(CreateConversationCleanupServiceMock().Object);
    }

    /// <summary>
    /// Removes a service from the service collection by type.
    /// </summary>
    public static void RemoveService<TService>(IServiceCollection services)
    {
        RemoveService(services, typeof(TService));
    }

    /// <summary>
    /// Removes a service from the service collection by type.
    /// </summary>
    public static void RemoveService(IServiceCollection services, Type serviceType)
    {
        var descriptors = services.Where(d => d.ServiceType == serviceType).ToList();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }
}