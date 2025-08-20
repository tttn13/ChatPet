namespace PetAssistant.Services;

public interface IConversationCleanupService
{
    void CleanupOldSessions();
    void CleanupSession(string sessionId);
    ConversationStats GetStats();
}

public class ConversationCleanupService : BackgroundService, IConversationCleanupService
{
    private readonly ILogger<ConversationCleanupService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Timer _cleanupTimer;
    
    // These should be injected from the GroqService, but for simplicity, we'll use a static reference
    // In production, consider using a shared conversation store
    private static readonly Dictionary<string, ConversationMetadata> _conversationMetadata = new();
    private static readonly object _lock = new();
    
    private readonly int _maxConversationsPerSession;
    private readonly TimeSpan _sessionTimeout;
    private readonly TimeSpan _cleanupInterval;

    public ConversationCleanupService(ILogger<ConversationCleanupService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        // Configuration with defaults
        _maxConversationsPerSession = _configuration.GetValue<int>("ConversationSettings:MaxMessagesPerSession", 50);
        _sessionTimeout = TimeSpan.FromHours(_configuration.GetValue<double>("ConversationSettings:SessionTimeoutHours", 24));
        _cleanupInterval = TimeSpan.FromMinutes(_configuration.GetValue<double>("ConversationSettings:CleanupIntervalMinutes", 30));
        
        _cleanupTimer = new Timer(
            callback: _ => CleanupOldSessions(),
            state: null,
            dueTime: _cleanupInterval,
            period: _cleanupInterval);
    }

    public void CleanupOldSessions()
    {
        lock (_lock)
        {
            var cutoffTime = DateTime.UtcNow - _sessionTimeout;
            var sessionsToRemove = new List<string>();

            foreach (var kvp in _conversationMetadata)
            {
                if (kvp.Value.LastActivity < cutoffTime)
                {
                    sessionsToRemove.Add(kvp.Key);
                }
            }

            var removedCount = 0;
            foreach (var sessionId in sessionsToRemove)
            {
                _conversationMetadata.Remove(sessionId);
                removedCount++;
                
                // Signal to GroqService to remove this session
                // In a real implementation, you'd use an event or shared service
                _logger.LogDebug("Marked session for cleanup: {SessionId}", sessionId);
            }

            if (removedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} inactive sessions", removedCount);
            }
        }
    }

    public void CleanupSession(string sessionId)
    {
        lock (_lock)
        {
            _conversationMetadata.Remove(sessionId);
            _logger.LogDebug("Cleaned up session: {SessionId}", sessionId);
        }
    }

    public ConversationStats GetStats()
    {
        lock (_lock)
        {
            var totalSessions = _conversationMetadata.Count;
            var activeSessions = _conversationMetadata.Count(kvp => 
                DateTime.UtcNow - kvp.Value.LastActivity < TimeSpan.FromHours(1));
            
            var totalMessages = _conversationMetadata.Values.Sum(m => m.MessageCount);
            var memoryEstimateMB = totalMessages * 0.5; // Rough estimate: 0.5KB per message

            return new ConversationStats(
                TotalSessions: totalSessions,
                ActiveSessions: activeSessions,
                TotalMessages: totalMessages,
                EstimatedMemoryMB: Math.Round(memoryEstimateMB, 2)
            );
        }
    }

    public static void UpdateSessionActivity(string sessionId, int messageCount)
    {
        lock (_lock)
        {
            _conversationMetadata[sessionId] = new ConversationMetadata(
                LastActivity: DateTime.UtcNow,
                MessageCount: messageCount
            );
        }
    }

    public static List<string> GetExpiredSessions(TimeSpan timeout)
    {
        lock (_lock)
        {
            var cutoffTime = DateTime.UtcNow - timeout;
            return _conversationMetadata
                .Where(kvp => kvp.Value.LastActivity < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Conversation cleanup service started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
                CleanupOldSessions();
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during conversation cleanup");
            }
        }
        
        _logger.LogInformation("Conversation cleanup service stopped");
    }

    public override void Dispose()
    {
        _cleanupTimer?.Dispose();
        base.Dispose();
    }

    private record ConversationMetadata(DateTime LastActivity, int MessageCount);
}

public record ConversationStats(
    int TotalSessions,
    int ActiveSessions,
    int TotalMessages,
    double EstimatedMemoryMB
);