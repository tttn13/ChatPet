using StackExchange.Redis;
using Newtonsoft.Json;
namespace PetAssistant.Services.Redis;

public interface IRedisService
{
    IDatabase GetDatabase(int dbIdx);
    Task<T?> GetAsync<T>(IDatabase db, string key);
    Task<RedisValue[]> GetAllSetMembersAsync(IDatabase db, string key);
    Task CreateKeyValueAsync<T>(IDatabase db, string key, T value, TimeSpan? expiry = null);
    Task RemoveKeyAsync(IDatabase db, string key);
    Task RemoveFromSetAsync(IDatabase db, string key, string value);
    Task<bool> ExistsAsync(IDatabase db, string key);
    Task<bool> AddKeyValueToSetAsync(IDatabase db, string key, string value, TimeSpan? expiry = null);
}

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public IDatabase GetDatabase(int dbIdx)
    {
        return _redis.GetDatabase(dbIdx);
    }
    public async Task<T?> GetAsync<T>(IDatabase db, string key)
    {
        var value = await db.StringGetAsync(key);
        if (!value.HasValue)
        {
            return default;
        }

        // If T is string, return the value directly without deserialization
        if (typeof(T) == typeof(string))
        {
            return (T)(object)value.ToString();
        }

        return JsonConvert.DeserializeObject<T>(value!);
    }
    public async Task<RedisValue[]> GetAllSetMembersAsync(IDatabase db, string key)
    {
        var members = await db.SetMembersAsync(key);

        return members;
    }
    public async Task CreateKeyValueAsync<T>(IDatabase db, string key, T value, TimeSpan? expiry = null)
    {
        string serializedValue;
        if (value is string stringValue)
        {
            serializedValue = stringValue;
        }
        else
        {
            serializedValue = JsonConvert.SerializeObject(value);
        }
        await db.StringSetAsync(key, serializedValue, expiry);
    }

    public async Task RemoveKeyAsync(IDatabase db, string key)
    {
        await db.KeyDeleteAsync(key);
    }
    public async Task RemoveFromSetAsync(IDatabase db, string key, string value)
    {
        await db.SetRemoveAsync(key, value);
    }

    public async Task<bool> ExistsAsync(IDatabase db, string key)
    {
        return await db.KeyExistsAsync(key);
    }

    public async Task<bool> AddKeyValueToSetAsync(IDatabase db, string key, string value, TimeSpan? expiry = null)
    {
        var result = await db.SetAddAsync(key, value);
        if (expiry.HasValue && result)
        {
            await db.KeyExpireAsync(key, expiry.Value);
        }
        return result;
    }
}