using StackExchange.Redis;
using System.Text.Json;

namespace Repository.Helper
{
    public class RedisCacheService
    {
        private readonly IDatabase _db;

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var jsonData = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, jsonData, expiry ?? TimeSpan.FromMinutes(30));
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var data = await _db.StringGetAsync(key);
            if (data.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(data);
        }

        public async Task RemoveAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }
    }
}
