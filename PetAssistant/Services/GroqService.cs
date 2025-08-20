using System.Text;
using Newtonsoft.Json;
using PetAssistant.Models;

namespace PetAssistant.Services;

public interface IGroqService
{
    Task<(string response, string? thinking, string sessionId)> GetPetAdviceAsync(string userMessage, string? sessionId = null, PetProfile? petProfile = null);
}

public class GroqService : IGroqService
{
    private readonly HttpClient _httpClient;
    private readonly GroqSettings _settings;
    private readonly ILogger<GroqService> _logger;
    private readonly IErrorHandlingService _errorHandlingService;
    private readonly ICacheService _cacheService;
    private readonly Dictionary<string, List<GroqMessage>> _conversationHistory = new();

    public GroqService(HttpClient httpClient, IConfiguration configuration, ILogger<GroqService> logger, IErrorHandlingService errorHandlingService, ICacheService cacheService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _errorHandlingService = errorHandlingService;
        _cacheService = cacheService;
        _settings = configuration.GetSection("GroqSettings").Get<GroqSettings>() 
            ?? throw new InvalidOperationException("GroqSettings not configured");
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
        _httpClient.Timeout = TimeSpan.FromSeconds(30); // Set reasonable timeout
    }

    public async Task<(string response, string? thinking, string sessionId)> GetPetAdviceAsync(string userMessage, string? sessionId = null, PetProfile? petProfile = null)
    {
        try
        {
            sessionId ??= Guid.NewGuid().ToString("N");

            // Check cache for single-message queries (not ongoing conversations)
            if (!_conversationHistory.ContainsKey(sessionId))
            {
                var cachedResponse = await _cacheService.GetCachedResponseAsync(userMessage, petProfile);
                if (!string.IsNullOrEmpty(cachedResponse))
                {
                    _logger.LogInformation("Returning cached response for SessionId='{SessionId}'", sessionId);
                    return (cachedResponse, null, sessionId);
                }
            }
            
            if (!_conversationHistory.ContainsKey(sessionId))
            {
                var systemPrompt = @"You are a knowledgeable and compassionate virtual veterinary assistant. 
                        You provide helpful advice about pet health, behavior, nutrition, and general care. 
                        Always remind users that for serious health concerns, they should consult with a real veterinarian. 
                        Be friendly, professional, and thorough in your responses.
                        Focus on common pets like dogs, cats, birds, rabbits, and small animals.
                        If asked about emergencies, always advise immediate veterinary care.";

                if (petProfile != null)
                {
                    systemPrompt += $@"

The user has a pet with the following profile:
- Name: {petProfile.Name}
- Species: {petProfile.Species}
- Breed: {petProfile.Breed ?? "Not specified"}
- Age: {(petProfile.Age.HasValue ? $"{petProfile.Age} years old" : "Not specified")}
- Gender: {petProfile.Gender ?? "Not specified"}

Please personalize your responses based on this pet's information when relevant.";
                }

                _conversationHistory[sessionId] = new List<GroqMessage>
                {
                    new GroqMessage 
                    { 
                        Role = "system", 
                        Content = systemPrompt
                    }
                };
            }

            _conversationHistory[sessionId].Add(new GroqMessage { Role = "user", Content = userMessage });

            var request = new GroqRequest
            {
                Model = _settings.Model,
                Messages = _conversationHistory[sessionId]
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Log the request being sent to Groq (without sensitive data)
            _logger.LogInformation("Sending to Groq API: SessionId='{SessionId}', MessageCount={MessageCount}, Model='{Model}'",
                sessionId, _conversationHistory[sessionId].Count, _settings.Model);

            var response = await _httpClient.PostAsync(_settings.ApiUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = _errorHandlingService.HandleGroqApiError(response.StatusCode, errorContent);
                return (errorResponse.Message, null, sessionId);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var groqResponse = JsonConvert.DeserializeObject<GroqResponse>(responseContent);

            if (groqResponse?.Choices?.FirstOrDefault()?.Message?.Content != null)
            {
                var fullMessage = groqResponse.Choices.First().Message.Content;
                string? thinking = null;
                string assistantResponse = fullMessage;

                // Parse thinking content if this is a thinking model
                if (_settings.IsThinking)
                {
                    var thinkingStart = fullMessage.IndexOf("<think>");
                    var thinkingEnd = fullMessage.IndexOf("</think>");
                    
                    if (thinkingStart >= 0 && thinkingEnd > thinkingStart)
                    {
                        thinking = fullMessage.Substring(thinkingStart + 7, thinkingEnd - thinkingStart - 7).Trim();
                        // Remove thinking tags from the response
                        assistantResponse = fullMessage.Remove(thinkingStart, thinkingEnd - thinkingStart + 8).Trim();
                    }
                }
                
                _conversationHistory[sessionId].Add(new GroqMessage { Role = "assistant", Content = assistantResponse });
                
                // Cache the response for new sessions only (to avoid caching conversation context)
                if (_conversationHistory[sessionId].Count == 3) // System prompt + user + assistant = 3 total
                {
                    await _cacheService.SetCachedResponseAsync(userMessage, petProfile, assistantResponse, thinking);
                }
                
                // Update activity tracking
                ConversationCleanupService.UpdateSessionActivity(sessionId, _conversationHistory[sessionId].Count);
                
                // Cleanup old messages to prevent memory growth
                if (_conversationHistory[sessionId].Count > 20)
                {
                    _conversationHistory[sessionId].RemoveRange(1, 2);
                }
                
                // Check for expired sessions and clean them up
                var expiredSessions = ConversationCleanupService.GetExpiredSessions(TimeSpan.FromHours(24));
                foreach (var expiredSession in expiredSessions)
                {
                    _conversationHistory.Remove(expiredSession);
                }

                return (assistantResponse, thinking, sessionId);
            }

            return ("I couldn't find a proper response. Please try rephrasing your question.", null, sessionId);
        }
        catch (Exception ex)
        {
            var errorResponse = _errorHandlingService.HandleGenericError(ex);
            return (errorResponse.Message, null, sessionId ?? Guid.NewGuid().ToString("N"));
        }
    }
}