using Moq;
using PetAssistant.Models;
using PetAssistant.Services;
using System.Net;

namespace PetAssistant.Tests.Helpers;

/// <summary>
/// Factory class for creating mock objects used in integration tests.
/// Provides consistent mock setups across all test classes.
/// </summary>
public static class TestMockFactory
{
    /// <summary>
    /// Creates a mock IValidationService with optional sanitized output.
    /// </summary>
    public static Mock<IValidationService> CreateValidationService(string? sanitizedOutput = null)
    {
        var mock = new Mock<IValidationService>();
        mock.Setup(x => x.SanitizeInput(It.IsAny<string>()))
            .Returns<string>(input => sanitizedOutput ?? input);
        return mock;
    }

    /// <summary>
    /// Creates a mock ITelemetryService.
    /// </summary>
    public static Mock<ITelemetryService> CreateTelemetryService()
    {
        return new Mock<ITelemetryService>();
    }

    /// <summary>
    /// Creates a mock IGroqService with customizable responses.
    /// </summary>
    public static Mock<IGroqService> CreateGroqService(
        string response = "Default response",
        string? thinking = null,
        string sessionId = "session123")
    {
        var mock = new Mock<IGroqService>();
        mock.Setup(x => x.GetPetAdviceAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<PetProfile?>()))
            .ReturnsAsync((response, thinking, sessionId));
        return mock;
    }

    /// <summary>
    /// Creates a mock IPetProfileService with optional profile.
    /// </summary>
    public static Mock<IPetProfileService> CreatePetProfileService(PetProfile? profile = null)
    {
        var mock = new Mock<IPetProfileService>();
        if (profile != null)
        {
            mock.Setup(x => x.GetProfileAsync(It.IsAny<string>()))
                .ReturnsAsync(profile);
        }
        return mock;
    }

    /// <summary>
    /// Creates a mock ICacheService with optional cached response.
    /// </summary>
    public static Mock<ICacheService> CreateCacheService(string? cachedResponse = null)
    {
        var mock = new Mock<ICacheService>();
        mock.Setup(x => x.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<PetProfile?>()))
            .ReturnsAsync(cachedResponse);
        return mock;
    }

    /// <summary>
    /// Creates a mock IErrorHandlingService.
    /// </summary>
    public static Mock<IErrorHandlingService> CreateErrorHandlingService()
    {
        var mock = new Mock<IErrorHandlingService>();
        mock.Setup(x => x.HandleGroqApiError(It.IsAny<HttpStatusCode>(), It.IsAny<string?>()))
            .Returns(new ErrorResponse("An error occurred", "API_ERROR", Guid.NewGuid().ToString(), DateTime.UtcNow));
        mock.Setup(x => x.HandleGenericError(It.IsAny<Exception>()))
            .Returns(new ErrorResponse("An unexpected error occurred", "GENERIC_ERROR", Guid.NewGuid().ToString(), DateTime.UtcNow));
        return mock;
    }

    /// <summary>
    /// Creates a mock IConversationCleanupService.
    /// </summary>
    public static Mock<IConversationCleanupService> CreateConversationCleanupService()
    {
        return new Mock<IConversationCleanupService>();
    }

    /// <summary>
    /// Creates all common mocks needed for basic endpoint testing.
    /// Returns a tuple of mocks for easy destructuring.
    /// </summary>
    public static (Mock<IGroqService> groq, Mock<IPetProfileService> petProfile, 
                   Mock<ITelemetryService> telemetry, Mock<IValidationService> validation)
        CreateBasicMocks(PetProfile? profile = null)
    {
        return (
            CreateGroqService(),
            CreatePetProfileService(profile),
            CreateTelemetryService(),
            CreateValidationService()
        );
    }

    /// <summary>
    /// Creates all mocks including cache and error handling services.
    /// </summary>
    public static AllMocks CreateAllMocks(PetProfile? profile = null)
    {
        return new AllMocks
        {
            Groq = CreateGroqService(),
            PetProfile = CreatePetProfileService(profile),
            Telemetry = CreateTelemetryService(),
            Validation = CreateValidationService(),
            Cache = CreateCacheService(),
            ErrorHandling = CreateErrorHandlingService(),
            ConversationCleanup = CreateConversationCleanupService()
        };
    }

    /// <summary>
    /// Container class for all mock services.
    /// </summary>
    public class AllMocks
    {
        public Mock<IGroqService> Groq { get; init; } = null!;
        public Mock<IPetProfileService> PetProfile { get; init; } = null!;
        public Mock<ITelemetryService> Telemetry { get; init; } = null!;
        public Mock<IValidationService> Validation { get; init; } = null!;
        public Mock<ICacheService> Cache { get; init; } = null!;
        public Mock<IErrorHandlingService> ErrorHandling { get; init; } = null!;
        public Mock<IConversationCleanupService> ConversationCleanup { get; init; } = null!;
    }
}