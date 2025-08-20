using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using PetAssistant.Models;
using PetAssistant.Services;
using Xunit;

namespace PetAssistant.Tests.Services;

public class GroqServiceTests
{
    private readonly Mock<ILogger<GroqService>> _mockLogger;
    private readonly Mock<IErrorHandlingService> _mockErrorHandlingService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly IConfiguration _configuration;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly GroqService _sut;

    public GroqServiceTests()
    {
        _mockLogger = new Mock<ILogger<GroqService>>();
        _mockErrorHandlingService = new Mock<IErrorHandlingService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.groq.com")
        };

        // Setup configuration using in-memory configuration
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"GroqSettings:ApiKey", "test-api-key"},
            {"GroqSettings:ApiUrl", "https://api.groq.com/openai/v1/chat/completions"},
            {"GroqSettings:Model", "qwen/qwen3-32b"},
            {"GroqSettings:IsThinking", "true"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _sut = new GroqService(_httpClient, _configuration, _mockLogger.Object, 
            _mockErrorHandlingService.Object, _mockCacheService.Object);
    }

    [Fact]
    public async Task GetPetAdviceAsync_NewSession_CreatesSessionAndReturnsResponse()
    {
        // Arrange
        var userMessage = "My dog is not eating";
        var expectedResponse = "It's concerning when a dog stops eating. This could be due to various reasons...";
        
        SetupSuccessfulHttpResponse(expectedResponse);
        _mockCacheService.Setup(x => x.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<PetProfile?>()))
            .ReturnsAsync((string?)null);
        _mockCacheService.Setup(x => x.SetCachedResponseAsync(It.IsAny<string>(), It.IsAny<PetProfile?>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        // Act
        var (response, thinking, sessionId) = await _sut.GetPetAdviceAsync(userMessage);

        // Assert
        response.Should().Be(expectedResponse);
        thinking.Should().BeNull();
        sessionId.Should().NotBeNullOrEmpty();
        sessionId.Should().HaveLength(32); // GUID without hyphens
        
        _mockCacheService.Verify(x => x.GetCachedResponseAsync(userMessage, null), Times.Once);
        _mockCacheService.Verify(x => x.SetCachedResponseAsync(userMessage, null, expectedResponse, null), Times.Once);
    }

    [Fact]
    public async Task GetPetAdviceAsync_ExistingSession_MaintainsConversationHistory()
    {
        // Arrange
        var sessionId = "test-session-123";
        var firstMessage = "My cat is sneezing";
        var secondMessage = "Should I take her to the vet?";
        var firstResponse = "Sneezing in cats can be caused by...";
        var secondResponse = "Yes, if the sneezing persists...";

        // First call
        SetupSuccessfulHttpResponse(firstResponse);
        await _sut.GetPetAdviceAsync(firstMessage, sessionId);

        // Second call
        SetupSuccessfulHttpResponse(secondResponse);
        
        // Act
        var (response, _, returnedSessionId) = await _sut.GetPetAdviceAsync(secondMessage, sessionId);

        // Assert
        response.Should().Be(secondResponse);
        returnedSessionId.Should().Be(sessionId);
        
        // Verify the second call didn't check cache (ongoing conversation)
        _mockCacheService.Verify(x => x.GetCachedResponseAsync(secondMessage, null), Times.Never);
    }

    [Fact]
    public async Task GetPetAdviceAsync_WithPetProfile_IncludesProfileInSystemPrompt()
    {
        // Arrange
        var userMessage = "What food is best?";
        var petProfile = new PetProfile("123", "Max", "Dog", "Golden Retriever", 5, "Male");
        var expectedResponse = "For a 5-year-old Golden Retriever like Max...";
        
        SetupSuccessfulHttpResponse(expectedResponse);
        _mockCacheService.Setup(x => x.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<PetProfile?>()))
            .ReturnsAsync((string?)null);

        string? capturedRequestContent = null;
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (request, _) =>
            {
                capturedRequestContent = await request.Content!.ReadAsStringAsync();
            })
            .ReturnsAsync(CreateSuccessResponse(expectedResponse));

        // Act
        var (response, _, _) = await _sut.GetPetAdviceAsync(userMessage, null, petProfile);

        // Assert
        response.Should().Be(expectedResponse);
        capturedRequestContent.Should().Contain("Max");
        capturedRequestContent.Should().Contain("Golden Retriever");
        capturedRequestContent.Should().Contain("5 years old");
    }

    [Fact]
    public async Task GetPetAdviceAsync_CacheHit_ReturnsCachedResponse()
    {
        // Arrange
        var userMessage = "How often should I feed my puppy?";
        var cachedResponse = "Puppies should be fed 3-4 times a day...";
        
        _mockCacheService.Setup(x => x.GetCachedResponseAsync(userMessage, null))
            .ReturnsAsync(cachedResponse);

        // Act
        var (response, thinking, sessionId) = await _sut.GetPetAdviceAsync(userMessage);

        // Assert
        response.Should().Be(cachedResponse);
        thinking.Should().BeNull();
        sessionId.Should().NotBeNullOrEmpty();
        
        // Verify HTTP call was never made
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetPetAdviceAsync_ApiReturnsError_HandlesGracefully()
    {
        // Arrange
        var userMessage = "Test message";
        var errorResponse = new ErrorResponse("Service unavailable", "GROQ_ERROR", "12345");
        
        SetupFailedHttpResponse(HttpStatusCode.ServiceUnavailable);
        _mockErrorHandlingService.Setup(x => x.HandleGroqApiError(HttpStatusCode.ServiceUnavailable, It.IsAny<string>()))
            .Returns(errorResponse);
        _mockCacheService.Setup(x => x.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<PetProfile?>()))
            .ReturnsAsync((string?)null);

        // Act
        var (response, thinking, sessionId) = await _sut.GetPetAdviceAsync(userMessage);

        // Assert
        response.Should().Be(errorResponse.Message);
        thinking.Should().BeNull();
        sessionId.Should().NotBeNullOrEmpty();
        
        _mockErrorHandlingService.Verify(x => x.HandleGroqApiError(HttpStatusCode.ServiceUnavailable, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetPetAdviceAsync_HttpTimeout_ReturnsErrorMessage()
    {
        // Arrange
        var userMessage = "Test message";
        var errorResponse = new ErrorResponse("Request timed out", "TIMEOUT", "12345");
        
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());
        
        _mockErrorHandlingService.Setup(x => x.HandleGenericError(It.IsAny<Exception>()))
            .Returns(errorResponse);
        _mockCacheService.Setup(x => x.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<PetProfile?>()))
            .ReturnsAsync((string?)null);

        // Act
        var (response, thinking, sessionId) = await _sut.GetPetAdviceAsync(userMessage);

        // Assert
        response.Should().Be(errorResponse.Message);
        thinking.Should().BeNull();
        sessionId.Should().NotBeNullOrEmpty();
        
        _mockErrorHandlingService.Verify(x => x.HandleGenericError(It.IsAny<TaskCanceledException>()), Times.Once);
    }

    [Fact]
    public async Task GetPetAdviceAsync_ThinkingModel_ParsesThinkingContent()
    {
        // Arrange
        var userMessage = "Complex medical question";
        var fullResponse = "<think>Let me analyze this medical condition...</think>Based on the symptoms, I recommend...";
        var expectedThinking = "Let me analyze this medical condition...";
        var expectedResponse = "Based on the symptoms, I recommend...";
        
        // Setup thinking model configuration
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"GroqSettings:ApiKey", "test-api-key"},
            {"GroqSettings:ApiUrl", "https://api.groq.com/openai/v1/chat/completions"},
            {"GroqSettings:Model", "llama3-8b-8192"},
            {"GroqSettings:IsThinking", "true"}
        };

        var thinkingConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        
        // Create a new HttpClient for this test to avoid header conflicts
        var mockHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://api.groq.com")
        };
        
        var sutWithThinking = new GroqService(httpClient, thinkingConfiguration, _mockLogger.Object, 
            _mockErrorHandlingService.Object, _mockCacheService.Object);
        
        // Setup the mock handler
        var groqResponse = new GroqResponse
        {
            Choices = new List<GroqChoice>
            {
                new GroqChoice
                {
                    Message = new GroqMessage
                    {
                        Role = "assistant",
                        Content = fullResponse
                    }
                }
            }
        };
        
        var json = JsonConvert.SerializeObject(groqResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        
        _mockCacheService.Setup(x => x.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<PetProfile?>()))
            .ReturnsAsync((string?)null);

        // Act
        var (actualResponse, thinking, _) = await sutWithThinking.GetPetAdviceAsync(userMessage);

        // Assert
        actualResponse.Should().Be(expectedResponse);
        thinking.Should().Be(expectedThinking);
    }

    [Fact]
    public async Task GetPetAdviceAsync_EmptyApiResponse_ReturnsDefaultMessage()
    {
        // Arrange
        var userMessage = "Test message";
        
        var groqResponse = new GroqResponse
        {
            Choices = new List<GroqChoice>()
        };
        
        SetupHttpResponse(groqResponse);
        _mockCacheService.Setup(x => x.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<PetProfile?>()))
            .ReturnsAsync((string?)null);

        // Act
        var (response, _, _) = await _sut.GetPetAdviceAsync(userMessage);

        // Assert
        response.Should().Be("I couldn't find a proper response. Please try rephrasing your question.");
    }

    [Fact]
    public async Task GetPetAdviceAsync_InvalidApiKey_ReturnsAuthError()
    {
        // Arrange
        var userMessage = "Test message";
        var errorResponse = new ErrorResponse("Authentication failed", "AUTH_ERROR", "12345");
        
        SetupFailedHttpResponse(HttpStatusCode.Unauthorized);
        _mockErrorHandlingService.Setup(x => x.HandleGroqApiError(HttpStatusCode.Unauthorized, It.IsAny<string>()))
            .Returns(errorResponse);
        _mockCacheService.Setup(x => x.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<PetProfile?>()))
            .ReturnsAsync((string?)null);

        // Act
        var (response, _, _) = await _sut.GetPetAdviceAsync(userMessage);

        // Assert
        response.Should().Be(errorResponse.Message);
        _mockErrorHandlingService.Verify(x => x.HandleGroqApiError(HttpStatusCode.Unauthorized, It.IsAny<string>()), Times.Once);
    }

    // Helper methods
    private void SetupSuccessfulHttpResponse(string content)
    {
        var groqResponse = new GroqResponse
        {
            Choices = new List<GroqChoice>
            {
                new GroqChoice
                {
                    Message = new GroqMessage
                    {
                        Role = "assistant",
                        Content = content
                    }
                }
            }
        };
        
        SetupHttpResponse(groqResponse);
    }

    private void SetupHttpResponse(GroqResponse groqResponse)
    {
        var json = JsonConvert.SerializeObject(groqResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupFailedHttpResponse(HttpStatusCode statusCode)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("Error", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private HttpResponseMessage CreateSuccessResponse(string content)
    {
        var groqResponse = new GroqResponse
        {
            Choices = new List<GroqChoice>
            {
                new GroqChoice
                {
                    Message = new GroqMessage
                    {
                        Role = "assistant",
                        Content = content
                    }
                }
            }
        };
        
        var json = JsonConvert.SerializeObject(groqResponse);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}