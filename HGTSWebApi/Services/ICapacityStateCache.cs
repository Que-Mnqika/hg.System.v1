using HGTSWebApi.DTOs;

namespace HGTSWebApi.Services
{
    public interface ICapacityStateCache
    {
        Task<CapacityNotificationDto?> GetAsync(Guid tripId, CancellationToken cancellationToken = default);
        Task SetAsync(CapacityNotificationDto notification, CancellationToken cancellationToken = default);
    }
}
