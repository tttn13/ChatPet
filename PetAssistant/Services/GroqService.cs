using PetAssistant.Services.Redis;
namespace PetAssistant.Services;

public interface IGroqService
{
    Task<(string response, string? thinking, string sessionId)> GetPetAdviceAsync(string userMessage, string? sessionId = null);
}

public class GroqService : BaseService, IGroqService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IErrorHandlingService _errorHandlingService;
    private readonly IConversationHistoryStorage _conversationStorage;

    public GroqService(HttpClient httpClient, IConfiguration configuration, ILogger<GroqService> logger, IErrorHandlingService errorHandlingService, IConversationHistoryStorage convoStorage)
        : base(logger)
    {
        _httpClient = httpClient;
        _config = configuration;
        _errorHandlingService = errorHandlingService;
        _conversationStorage = convoStorage;
        _httpClient.BaseAddress = new Uri(_config["Groq:ApiUrl"]);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config["Groq:ApiKey"]}");
        _httpClient.Timeout = TimeSpan.FromSeconds(30); // Set reasonable timeout
    }

    private async Task<ApiResponse> GetGroqResponseAsync(GroqRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            var errorResponse = _errorHandlingService.HandleGroqApiError(response.StatusCode, errorContent);
            LogWarning("Groq API returned error: {StatusCode}", response.StatusCode);
            return new Error(errorResponse);
        }

        var groqResponse = await response.Content.ReadFromJsonAsync<GroqResponse>();
        LogDebug("Groq API response received successfully");
        return new Success(groqResponse);
    }
    
    public async Task<(string response, string? thinking, string sessionId)> GetPetAdviceAsync(string userMessage, string? sessionId = null)
    {
        try
        {
            sessionId ??= Guid.NewGuid().ToString("N");

            // Load conversation history from Redis or initialize new one
            var conversationHistory = await _conversationStorage.LoadAsync(sessionId);
            if (conversationHistory == null || conversationHistory.Count == 0)
            {
                conversationHistory = _conversationStorage.Initialize();
            }

            conversationHistory.Add(new GroqMessage { Role = "user", Content = userMessage });

            var request = new GroqRequest
            {
                Model = _config["Groq:Model"],
                Messages = conversationHistory
            };

            LogInfo("Sending to Groq API: SessionId='{SessionId}', MessageCount={MessageCount}, Model='{Model}'",
                sessionId, conversationHistory.Count, _config["Groq:Model"]);

            var groqResponse = await GetGroqResponseAsync(request);

            switch (groqResponse)
            {
                case Success success:
                    var fullMessage = success.Response.Choices.FirstOrDefault()?.Message?.Content;
                    if (fullMessage == null)
                    {
                        return ("I couldn't find a proper response. Please try rephrasing your question.", null, sessionId);
                    }
                    
                    var (assistantResponse, thinking) = ParseThinkingContent(fullMessage);

                    await _conversationStorage.MaintainAsync(sessionId, conversationHistory, assistantResponse);

                    return (assistantResponse, thinking, sessionId);
                case Error error:
                    return (error.Response.Message, null, sessionId);
            }           
        }
            catch (Exception ex)
            {
                var errorResponse = _errorHandlingService.HandleGenericError(ex);
                return (errorResponse.Message, null, sessionId ?? Guid.NewGuid().ToString("N"));
            }
       
        return ("An unexpected error occurred.", null, sessionId ?? Guid.NewGuid().ToString("N"));
    }
    
    private (string assistantResponse, string? thinking) ParseThinkingContent(string fullMessage)
    {
        string? thinking = null;
        string assistantResponse = fullMessage;
        
        if (bool.Parse(_config["Groq:IsThinking"]))
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
        
        return (assistantResponse, thinking);
    }
}