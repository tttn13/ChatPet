using StackExchange.Redis;

namespace PetAssistant.Services.Redis;

public interface IConversationHistoryStorage
{
    List<GroqMessage> Initialize();
    Task<List<GroqMessage>?> LoadAsync(string sessionId);
    Task SaveAsync(string sessionId, List<GroqMessage> conversationHistory);
    Task MaintainAsync(string sessionId, List<GroqMessage> conversationHistory, string assistantResponse);
}

public class ConversationHistoryStorage : IConversationHistoryStorage
{
    private readonly IRedisService _redisService;
    private readonly IDatabase _database;
    private readonly ILogger<ConversationHistoryStorage> _logger;
    private const string CONVERSATION_KEY_PREFIX = "conversation:";

    public ConversationHistoryStorage(IRedisService redisService, ILogger<ConversationHistoryStorage> logger)
    {
        _redisService = redisService;
        _database = _redisService.GetDatabase(RedisDbIdx.CONVERSATIONS_DB);
        _logger = logger;
    }
    public List<GroqMessage> Initialize()
    {
        var systemPrompt = BuildSystemPrompt();
        return new List<GroqMessage>
        {
            new GroqMessage
            {
                Role = "system",
                Content = systemPrompt
            }
        };
    }

    private string BuildSystemPrompt()
    {
        var basePrompt = @"You are a knowledgeable and compassionate virtual veterinary assistant. 
                        You provide helpful advice about pet health, behavior, nutrition, and general care. 
                        Always remind users that for serious health concerns, they should consult with a real veterinarian. 
                        Be friendly, professional, and thorough in your responses.
                        Focus on common pets like dogs, cats, birds, rabbits, and small animals.
                        If asked about emergencies, always advise immediate veterinary care.";

        

        return basePrompt ;
    }
    public async Task<List<GroqMessage>?> LoadAsync(string sessionId)
    {
        try
        {
            var key = $"{CONVERSATION_KEY_PREFIX}{sessionId}";
            var conversationHistory = await _redisService.GetAsync<List<GroqMessage>>(_database, key);
            _logger.LogDebug("Loaded conversation history for session {SessionId}: {MessageCount} messages",
                sessionId, conversationHistory?.Count ?? 0);
            return conversationHistory;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error loading conversation history from Redis for session {SessionId}: {Error}", sessionId, ex.Message);
            return null;
        }
    }
    public async Task SaveAsync(string sessionId, List<GroqMessage> conversationHistory)
    {
        try
        {
            var key = $"{CONVERSATION_KEY_PREFIX}{sessionId}";
            await _redisService.CreateKeyValueAsync(_database, key, conversationHistory, TimeSpan.FromDays(7));
             _logger.LogDebug("Saved conversation history for session {SessionId}: {MessageCount} messages",
                sessionId, conversationHistory.Count);
        }
        catch (Exception ex)
        {
             _logger.LogWarning("Error saving conversation history to Redis for session {SessionId}: {Error}", sessionId, ex.Message);
        }
    }
    public async Task MaintainAsync(string sessionId, List<GroqMessage> conversationHistory, string assistantResponse)
    {
        conversationHistory.Add(new GroqMessage { Role = "assistant", Content = assistantResponse });

        ConversationCleanupService.UpdateSessionActivity(sessionId, conversationHistory.Count);

        if (conversationHistory.Count > 20)
        {
            conversationHistory.RemoveRange(1, 2);
        }

        await SaveAsync(sessionId, conversationHistory);
    }

}