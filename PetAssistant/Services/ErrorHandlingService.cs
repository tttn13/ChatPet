using System.Net;

namespace PetAssistant.Services;

public interface IErrorHandlingService
{
    ErrorResponse HandleGroqApiError(HttpStatusCode statusCode, string? errorContent);
    ErrorResponse HandleGenericError(Exception exception);
}

public class ErrorHandlingService : IErrorHandlingService
{
    private readonly ILogger<ErrorHandlingService> _logger;

    public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
    {
        _logger = logger;
    }

    public ErrorResponse HandleGroqApiError(HttpStatusCode statusCode, string? errorContent)
    {
        var errorId = Guid.NewGuid().ToString("N")[..8]; // Short error ID for tracking
        
        var response = statusCode switch
        {
            HttpStatusCode.BadRequest => new ErrorResponse(
                "Invalid request format. Please check your message and try again.",
                "GROQ_BAD_REQUEST",
                errorId
            ),
            HttpStatusCode.Unauthorized => new ErrorResponse(
                "Authentication failed. Please contact support if this continues.",
                "GROQ_AUTH_FAILED",
                errorId
            ),
            HttpStatusCode.TooManyRequests => new ErrorResponse(
                "Too many requests. Please wait a moment and try again.",
                "GROQ_RATE_LIMITED",
                errorId
            ),
            HttpStatusCode.InternalServerError => new ErrorResponse(
                "The AI service is temporarily unavailable. Please try again in a few minutes.",
                "GROQ_SERVER_ERROR",
                errorId
            ),
            HttpStatusCode.ServiceUnavailable => new ErrorResponse(
                "The AI service is currently under maintenance. Please try again later.",
                "GROQ_MAINTENANCE",
                errorId
            ),
            HttpStatusCode.RequestTimeout => new ErrorResponse(
                "Request timed out. Please try with a shorter message.",
                "GROQ_TIMEOUT",
                errorId
            ),
            _ => new ErrorResponse(
                "Unable to process your request right now. Please try again.",
                "GROQ_UNKNOWN_ERROR",
                errorId
            )
        };

        _logger.LogWarning("Groq API error: Status={StatusCode}, ErrorId={ErrorId}, Content={Content}", 
            statusCode, errorId, errorContent);

        return response;
    }

    public ErrorResponse HandleGenericError(Exception exception)
    {
        var errorId = Guid.NewGuid().ToString("N")[..8];
        
        var response = exception switch
        {
            HttpRequestException httpEx => new ErrorResponse(
                "Network connection failed. Please check your internet connection and try again.",
                "NETWORK_ERROR",
                errorId
            ),
            TaskCanceledException timeoutEx => new ErrorResponse(
                "Request took too long to process. Please try again with a shorter message.",
                "REQUEST_TIMEOUT",
                errorId
            ),
            ArgumentException argEx => new ErrorResponse(
                "Invalid input provided. Please check your message and try again.",
                "INVALID_INPUT",
                errorId
            ),
            InvalidOperationException opEx => new ErrorResponse(
                "Service is temporarily unavailable. Please try again in a few minutes.",
                "SERVICE_UNAVAILABLE",
                errorId
            ),
            _ => new ErrorResponse(
                "An unexpected error occurred. Please try again or contact support if this continues.",
                "UNEXPECTED_ERROR",
                errorId
            )
        };

        _logger.LogError(exception, "Unhandled error: ErrorId={ErrorId}, Type={ExceptionType}", 
            errorId, exception.GetType().Name);

        return response;
    }
}

public record ErrorResponse(
    string Message,
    string ErrorCode,
    string ErrorId,
    DateTime Timestamp = default
)
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}