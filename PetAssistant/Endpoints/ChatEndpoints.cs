using PetAssistant.Models;
using PetAssistant.Services;

namespace PetAssistant.Endpoints;

public static class ChatEndpoints
{
    public static RouteGroupBuilder MapChatEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            ChatRequest request,
            IGroqService groqService,
            IPetProfileService petProfileService,
            IValidationService validationService,
            ITelemetryService telemetryService,
            ILogger<Program> logger) =>
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                // Sanitize inputs
                request = new ChatRequest
                {
                    Message = validationService.SanitizeInput(request.Message),
                    SessionId = string.IsNullOrEmpty(request.SessionId) ? null : validationService.SanitizeInput(request.SessionId)
                };

                // Log the incoming request (without sensitive data)
                logger.LogInformation("Received chat request: MessageLength={Length}, SessionId='{SessionId}'",
                    request.Message.Length, request.SessionId ?? "null");

                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    logger.LogWarning("Empty message received");
                    telemetryService.RecordApiCall("/api/chat", stopwatch.Elapsed, false);
                    return Results.BadRequest(new { error = "Message cannot be empty" });
                }

                // Get pet profile if session exists
                PetProfile? petProfile = null;
                if (!string.IsNullOrEmpty(request.SessionId))
                {
                    petProfile = await petProfileService.GetProfileAsync(request.SessionId);
                }

                // Record user request
                telemetryService.RecordUserRequest(request.SessionId ?? "anonymous", petProfile != null);

                var (response, thinking, actualSessionId) = await groqService.GetPetAdviceAsync(request.Message, request.SessionId, petProfile);

                logger.LogInformation("Sending chat response: SessionId='{SessionId}', ResponseLength={ResponseLength}, HasThinking={HasThinking}",
                    actualSessionId, response.Length, thinking != null);

                telemetryService.RecordApiCall("/api/chat", stopwatch.Elapsed, true);

                return Results.Ok(new ChatResponse(
                    Response: response,
                    Thinking: thinking,
                    SessionId: actualSessionId,
                    Timestamp: DateTime.UtcNow
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in chat endpoint");
                telemetryService.RecordApiCall("/api/chat", stopwatch.Elapsed, false);
                throw;
            }
        })
        .WithName("Chat")
        .WithOpenApi()
        .WithSummary("Send a message to the pet health assistant")
        .WithDescription("Ask questions about pet health, behavior, nutrition, and general care");

        return group;
    }
}