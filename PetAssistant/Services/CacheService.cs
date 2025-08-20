using Microsoft.Extensions.Caching.Memory;
using PetAssistant.Models;
using System.Security.Cryptography;
using System.Text;

namespace PetAssistant.Services;

public interface ICacheService
{
    Task<string?> GetCachedResponseAsync(string userMessage, PetProfile? petProfile);
    Task SetCachedResponseAsync(string userMessage, PetProfile? petProfile, string response, string? thinking);
    void ClearCache();
    CacheStatistics GetStatistics();
}

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1); // Cache for 1 hour
    private long _hits = 0;
    private long _misses = 0;

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<string?> GetCachedResponseAsync(string userMessage, PetProfile? petProfile)
    {
        var cacheKey = GenerateCacheKey(userMessage, petProfile);
        
        if (_cache.TryGetValue(cacheKey, out CachedResponse? cachedResponse))
        {
            Interlocked.Increment(ref _hits);
            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey[..8] + "...");
            return Task.FromResult<string?>(cachedResponse.Response);
        }

        Interlocked.Increment(ref _misses);
        _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey[..8] + "...");
        return Task.FromResult<string?>(null);
    }

    public Task SetCachedResponseAsync(string userMessage, PetProfile? petProfile, string response, string? thinking)
    {
        var cacheKey = GenerateCacheKey(userMessage, petProfile);
        
        var cachedResponse = new CachedResponse(response, thinking, DateTime.UtcNow);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            SlidingExpiration = TimeSpan.FromMinutes(30), // Extend if accessed within 30 min
            Priority = CacheItemPriority.Normal,
            Size = EstimateSize(response, thinking) // Estimate memory usage
        };

        _cache.Set(cacheKey, cachedResponse, cacheEntryOptions);
        _logger.LogDebug("Cached response for key: {CacheKey}, Size: ~{Size}KB", 
            cacheKey[..8] + "...", cacheEntryOptions.Size);

        return Task.CompletedTask;
    }

    public void ClearCache()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Clear();
        }
        
        Interlocked.Exchange(ref _hits, 0);
        Interlocked.Exchange(ref _misses, 0);
        
        _logger.LogInformation("Cache cleared");
    }

    public CacheStatistics GetStatistics()
    {
        var totalRequests = _hits + _misses;
        var hitRatio = totalRequests > 0 ? (double)_hits / totalRequests * 100 : 0;
        
        return new CacheStatistics(
            Hits: _hits,
            Misses: _misses,
            HitRatio: Math.Round(hitRatio, 2)
        );
    }

    private string GenerateCacheKey(string userMessage, PetProfile? petProfile)
    {
        // Normalize the message for better cache hits
        var normalizedMessage = NormalizeMessage(userMessage);
        
        // Include pet profile in cache key for personalized responses
        var profileKey = petProfile != null 
            ? $"{petProfile.Species}:{petProfile.Age}:{petProfile.Gender}" 
            : "no-profile";

        var combinedKey = $"{normalizedMessage}:{profileKey}";
        
        // Hash the key to keep it short and consistent
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedKey));
        return Convert.ToBase64String(hashBytes)[..16]; // Use first 16 chars
    }

    private string NormalizeMessage(string message)
    {
        // Normalize for better cache hits on similar questions
        return message.ToLowerInvariant()
            .Trim()
            .Replace("  ", " ") // Multiple spaces to single
            .Replace("?", "")    // Remove question marks
            .Replace(".", "")    // Remove periods
            .Replace(",", "")    // Remove commas
            .Replace("!", "");   // Remove exclamations
    }

    private int EstimateSize(string response, string? thinking)
    {
        // Rough estimate: 2 bytes per char (UTF-16) + object overhead
        var responseSize = response.Length * 2;
        var thinkingSize = thinking?.Length * 2 ?? 0;
        var overhead = 100; // Object overhead
        
        return (responseSize + thinkingSize + overhead) / 1024; // Return KB
    }

    private record CachedResponse(string Response, string? Thinking, DateTime CreatedAt);
}

public record CacheStatistics(long Hits, long Misses, double HitRatio);