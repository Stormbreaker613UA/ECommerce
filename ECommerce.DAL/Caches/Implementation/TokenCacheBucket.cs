using ECommerce.DAL.Caches.Interface;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerce.DAL.Caches.Implementation
{
    public class TokenCacheBucket(IMemoryCache cache) : ITokenCacheBucket
    {
        private readonly IMemoryCache _cache = cache;
        public void SetToken(Guid userId, string token, TimeSpan lifetime)
        {
            var cacheKey = userId.ToString(); _cache.Set(cacheKey, token, lifetime);
        }
        public string? GetToken(Guid userId)
        {
            var cacheKey = userId.ToString(); _cache.TryGetValue(cacheKey, out string? token);
            return token;
        }
        public void RemoveToken(Guid userId)
        {
            var cacheKey = userId.ToString(); _cache.Remove(cacheKey);
        }
    }
}
