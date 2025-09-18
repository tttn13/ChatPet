using StackExchange.Redis;
using PetAssistant.Services.Auth;

namespace PetAssistant.Services.Redis;

public interface IJwtTokenCacheService
{
    Task CacheTokenAsync(string jwtId, string userId, TimeSpan expiration);
    Task<bool> IsTokenExisted(string token);
    Task RevokeTokenAsync(string jwtId);
}

public class JwtTokenCacheService : IJwtTokenCacheService
{
    private readonly IRedisService _redis;
    private readonly IDatabase _database;
    private readonly ILogger<JwtTokenCacheService> _logger;
    private readonly IJwtTokenUsageService _tokenUsage;
    private const string TokenKeyPrefix = "jwt_token:";
    private const string UserTokenKeyPrefix = "user_tokens:";

    public JwtTokenCacheService(IRedisService redisService, IJwtTokenUsageService tokenUsage, ILogger<JwtTokenCacheService> logger)
    {
        _redis = redisService;
        _database = _redis.GetDatabase(RedisDbIdx.USER_SESSIONS_DB);
        _tokenUsage = tokenUsage;
        _logger = logger;
    }

    public async Task CacheTokenAsync(string jwtId, string userId, TimeSpan expiration)
    {
        var tokenKey = GetFullTokenKey(true, jwtId);
        var userTokensKey = GetFullTokenKey(false, userId);

        _logger.LogInformation("Caching token {JwtId} for user {UserId} with expiration {Expiration}", jwtId, userId, expiration);

        await _redis.CreateKeyValueAsync(_database, tokenKey, userId, expiration);
        _logger.LogDebug("Stored token key: {TokenKey} -> {UserId}", tokenKey, userId);

        // Add token to user's token set for bulk revocation support with expiration
        await _redis.AddKeyValueToSetAsync(_database, userTokensKey, jwtId, expiration.Add(TimeSpan.FromHours(1)));
        _logger.LogDebug("Added token {JwtId} to user set: {UserTokensKey}", jwtId, userTokensKey);
    }

    public async Task<bool> IsTokenExisted(string token)
    {
        var jwtId = _tokenUsage.GetJwtId(token);
        var tokenKey = GetFullTokenKey(true, jwtId);
        var exists = await _redis.ExistsAsync(_database, tokenKey);
        _logger.LogDebug("Token {JwtId} existence check: {Exists} (key: {TokenKey})", jwtId, exists, tokenKey);
        return exists;
    }

    public async Task RevokeTokenAsync(string jwtId)
    {
        var tokenKey = GetFullTokenKey(true, jwtId);

        var userId = await _redis.GetAsync<string>(_database, tokenKey);
        _logger.LogInformation("Revoking token {JwtId} for user {UserId}", jwtId, userId ?? "unknown");

        await _redis.RemoveKeyAsync(_database, tokenKey);
        _logger.LogDebug("Removed token key: {TokenKey}", tokenKey);

        // Remove from user's token set if userId exists
        if (!string.IsNullOrEmpty(userId))
        {
            await _redis.RemoveFromSetAsync(_database, GetFullTokenKey(false, userId), jwtId);
            _logger.LogDebug("Removed token {JwtId} from user {UserId} token set", jwtId, userId);
        }
    }
    private string GetFullTokenKey(bool isTokenKey, string id)
    {
        return isTokenKey ? $"{TokenKeyPrefix}{id}" : $"{UserTokenKeyPrefix}{id}";
    }
}