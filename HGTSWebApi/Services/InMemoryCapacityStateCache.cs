using HGTSWebApi.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace HGTSWebApi.Services
{
    public class InMemoryCapacityStateCache : ICapacityStateCache
    {
        private readonly IMemoryCache _memoryCache;

        public InMemoryCapacityStateCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public Task<CapacityNotificationDto?> GetAsync(Guid tripId, CancellationToken cancellationToken = default)
        {
            _memoryCache.TryGetValue(GetCacheKey(tripId), out CapacityNotificationDto? notification);
            return Task.FromResult(notification);
        }

        public Task SetAsync(CapacityNotificationDto notification, CancellationToken cancellationToken = default)
        {
            _memoryCache.Set(
                GetCacheKey(notification.TripId),
                notification,
                TimeSpan.FromHours(6));

            return Task.CompletedTask;
        }

        private static string GetCacheKey(Guid tripId) => $"capacity:{tripId}";
    }
}
