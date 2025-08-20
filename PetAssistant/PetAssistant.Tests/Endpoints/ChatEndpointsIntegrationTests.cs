using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using PetAssistant.Models;
using PetAssistant.Services;
using PetAssistant.Tests.Helpers;
using Xunit;

namespace PetAssistant.Tests.Endpoints;

public class ChatEndpointsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChatEndpointsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostChat_ValidRequest_ReturnsOk()
    {
        // Arrange
        var mockGroqService = TestMockFactory.CreateGroqService("This is a test response", null, "session123");
        var mockPetProfileService = TestMockFactory.CreatePetProfileService();
        var mockValidationService = TestMockFactory.CreateValidationService();
        var mockTelemetryService = TestMockFactory.CreateTelemetryService();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing services
                RemoveService<IGroqService>(services);
                RemoveService<IPetProfileService>(services);
                RemoveService<IValidationService>(services);
                RemoveService<ITelemetryService>(services);

                // Add mocked services
                services.AddSingleton(mockGroqService.Object);
                services.AddSingleton(mockPetProfileService.Object);
                services.AddSingleton(mockValidationService.Object);
                services.AddSingleton(mockTelemetryService.Object);
            });
        }).CreateClient();

        var request = new ChatRequest
        {
            Message = "How often should I feed my dog?"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", request);
        var content = await response.Content.ReadAsStringAsync();
        var chatResponse = JsonConvert.DeserializeObject<ChatResponse>(content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        chatResponse.Should().NotBeNull();
        chatResponse!.Response.Should().Be("This is a test response");
        chatResponse.SessionId.Should().Be("session123");
        chatResponse.Thinking.Should().BeNull();
        chatResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        mockGroqService.Verify(x => x.GetPetAdviceAsync("How often should I feed my dog?", null, null), Times.Once);
    }

    [Fact]
    public async Task PostChat_EmptyMessage_ReturnsBadRequest()
    {
        // Arrange
        var mockValidationService = TestMockFactory.CreateValidationService("");
        var mockTelemetryService = TestMockFactory.CreateTelemetryService();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IValidationService>(services);
                RemoveService<ITelemetryService>(services);
                services.AddSingleton(mockValidationService.Object);
                services.AddSingleton(mockTelemetryService.Object);
            });
        }).CreateClient();

        var request = new ChatRequest
        {
            Message = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("Message cannot be empty");
    }

    [Fact]
    public async Task PostChat_WithSessionId_MaintainsSession()
    {
        // Arrange
        var sessionId = "existing-session-123";
        var mockGroqService = TestMockFactory.CreateGroqService("Response with session", null, sessionId);
        var mockPetProfileService = TestMockFactory.CreatePetProfileService();
        var mockValidationService = TestMockFactory.CreateValidationService();
        var mockTelemetryService = TestMockFactory.CreateTelemetryService();

        // Override specific setup for session handling
        mockGroqService.Setup(x => x.GetPetAdviceAsync(It.IsAny<string>(), sessionId, It.IsAny<PetProfile?>()))
            .ReturnsAsync(("Response with session", null, sessionId));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IGroqService>(services);
                RemoveService<IPetProfileService>(services);
                RemoveService<IValidationService>(services);
                RemoveService<ITelemetryService>(services);

                services.AddSingleton(mockGroqService.Object);
                services.AddSingleton(mockPetProfileService.Object);
                services.AddSingleton(mockValidationService.Object);
                services.AddSingleton(mockTelemetryService.Object);
            });
        }).CreateClient();

        var request = new ChatRequest
        {
            Message = "Follow up question",
            SessionId = sessionId
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", request);
        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        chatResponse!.SessionId.Should().Be(sessionId);
        
        mockGroqService.Verify(x => x.GetPetAdviceAsync("Follow up question", sessionId, It.IsAny<PetProfile?>()), Times.Once);
    }

    [Fact]
    public async Task PostChat_WithPetProfile_UsesProfile()
    {
        // Arrange
        var sessionId = "profile-session";
        var petProfile = new PetProfile("pet123", "Buddy", "Dog", "Labrador", 3, "Male");
        
        var mockGroqService = TestMockFactory.CreateGroqService("Response for Buddy", null, sessionId);
        var mockPetProfileService = TestMockFactory.CreatePetProfileService(petProfile);
        var mockValidationService = TestMockFactory.CreateValidationService();
        var mockTelemetryService = TestMockFactory.CreateTelemetryService();

        // Override specific setup for profile handling
        mockGroqService.Setup(x => x.GetPetAdviceAsync(It.IsAny<string>(), sessionId, petProfile))
            .ReturnsAsync(("Response for Buddy", null, sessionId));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IGroqService>(services);
                RemoveService<IPetProfileService>(services);
                RemoveService<IValidationService>(services);
                RemoveService<ITelemetryService>(services);

                services.AddSingleton(mockGroqService.Object);
                services.AddSingleton(mockPetProfileService.Object);
                services.AddSingleton(mockValidationService.Object);
                services.AddSingleton(mockTelemetryService.Object);
            });
        }).CreateClient();

        var request = new ChatRequest
        {
            Message = "What's best for my pet?",
            SessionId = sessionId
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        mockPetProfileService.Verify(x => x.GetProfileAsync(sessionId), Times.Once);
        mockGroqService.Verify(x => x.GetPetAdviceAsync("What's best for my pet?", sessionId, petProfile), Times.Once);
    }

    [Fact]
    public async Task PostChat_MaliciousInput_Sanitizes()
    {
        // Arrange
        var maliciousMessage = "<script>alert('xss')</script>Hello";
        var sanitizedMessage = "Hello";
        
        var mockGroqService = TestMockFactory.CreateGroqService("Safe response", null, "session123");
        var mockPetProfileService = TestMockFactory.CreatePetProfileService();
        var mockValidationService = TestMockFactory.CreateValidationService(sanitizedMessage);
        var mockTelemetryService = TestMockFactory.CreateTelemetryService();

        // Override specific setup for malicious input
        mockValidationService.Setup(x => x.SanitizeInput(maliciousMessage))
            .Returns(sanitizedMessage);
        
        mockValidationService.Setup(x => x.SanitizeInput(It.Is<string>(s => s == null)))
            .Returns((string?)null);
        
        mockGroqService.Setup(x => x.GetPetAdviceAsync(sanitizedMessage, It.IsAny<string?>(), It.IsAny<PetProfile?>()))
            .ReturnsAsync(("Safe response", null, "session123"));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IGroqService>(services);
                RemoveService<IPetProfileService>(services);
                RemoveService<IValidationService>(services);
                RemoveService<ITelemetryService>(services);

                services.AddSingleton(mockGroqService.Object);
                services.AddSingleton(mockPetProfileService.Object);
                services.AddSingleton(mockValidationService.Object);
                services.AddSingleton(mockTelemetryService.Object);
            });
        }).CreateClient();

        var request = new ChatRequest
        {
            Message = maliciousMessage
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        mockValidationService.Verify(x => x.SanitizeInput(maliciousMessage), Times.Once);
        mockGroqService.Verify(x => x.GetPetAdviceAsync(sanitizedMessage, It.IsAny<string?>(), It.IsAny<PetProfile?>()), Times.Once);
    }

    [Fact]
    public async Task PostChat_ServiceError_Returns500()
    {
        // Arrange
        var mockGroqService = TestMockFactory.CreateGroqService();
        var mockPetProfileService = TestMockFactory.CreatePetProfileService();
        var mockValidationService = TestMockFactory.CreateValidationService();
        var mockTelemetryService = TestMockFactory.CreateTelemetryService();

        // Override to throw exception
        mockGroqService.Setup(x => x.GetPetAdviceAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<PetProfile?>()))
            .ThrowsAsync(new Exception("Service error"));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IGroqService>(services);
                RemoveService<IPetProfileService>(services);
                RemoveService<IValidationService>(services);
                RemoveService<ITelemetryService>(services);

                services.AddSingleton(mockGroqService.Object);
                services.AddSingleton(mockPetProfileService.Object);
                services.AddSingleton(mockValidationService.Object);
                services.AddSingleton(mockTelemetryService.Object);
            });
        }).CreateClient();

        var request = new ChatRequest
        {
            Message = "Test message"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        
        mockTelemetryService.Verify(x => x.RecordApiCall("/api/chat", It.IsAny<TimeSpan>(), false), Times.Once);
    }

    [Fact]
    public async Task PostChat_ThinkingModel_ReturnsThinkingContent()
    {
        // Arrange
        var thinking = "Let me analyze this question about pet health...";
        var mainResponse = "Based on my analysis, here's my recommendation...";
        
        var mockGroqService = TestMockFactory.CreateGroqService(mainResponse, thinking, "session123");
        var mockPetProfileService = TestMockFactory.CreatePetProfileService();
        var mockValidationService = TestMockFactory.CreateValidationService();
        var mockTelemetryService = TestMockFactory.CreateTelemetryService();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IGroqService>(services);
                RemoveService<IPetProfileService>(services);
                RemoveService<IValidationService>(services);
                RemoveService<ITelemetryService>(services);

                services.AddSingleton(mockGroqService.Object);
                services.AddSingleton(mockPetProfileService.Object);
                services.AddSingleton(mockValidationService.Object);
                services.AddSingleton(mockTelemetryService.Object);
            });
        }).CreateClient();

        var request = new ChatRequest
        {
            Message = "Complex medical question"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", request);
        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        chatResponse!.Response.Should().Be(mainResponse);
        chatResponse.Thinking.Should().Be(thinking);
    }

    [Fact]
    public async Task PostChat_TelemetryRecorded_OnSuccess()
    {
        // Arrange
        var mockGroqService = TestMockFactory.CreateGroqService("Response", null, "session123");
        var mockPetProfileService = TestMockFactory.CreatePetProfileService();
        var mockValidationService = TestMockFactory.CreateValidationService();
        var mockTelemetryService = TestMockFactory.CreateTelemetryService();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveService<IGroqService>(services);
                RemoveService<IPetProfileService>(services);
                RemoveService<IValidationService>(services);
                RemoveService<ITelemetryService>(services);

                services.AddSingleton(mockGroqService.Object);
                services.AddSingleton(mockPetProfileService.Object);
                services.AddSingleton(mockValidationService.Object);
                services.AddSingleton(mockTelemetryService.Object);
            });
        }).CreateClient();

        var request = new ChatRequest
        {
            Message = "Test message"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        mockTelemetryService.Verify(x => x.RecordUserRequest(It.IsAny<string>(), false), Times.Once);
        mockTelemetryService.Verify(x => x.RecordApiCall("/api/chat", It.IsAny<TimeSpan>(), true), Times.Once);
    }

    // Helper method to remove a service from the service collection
    private static void RemoveService<TService>(IServiceCollection services)
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(TService)).ToList();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

}