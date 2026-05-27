using HGTSWebApi.DTOs;
using HGTSWebApi.Models;

namespace HGTSWebApi.Services
{
    public interface ICapacityNotificationService
    {
        Task PublishAsync(
            Trip trip,
            int boardedCount,
            int capacity,
            string resultCode,
            string deviceId,
            string? studentId = null,
            string? studentName = null,
            CancellationToken cancellationToken = default);

        Task<CapacityNotificationDto?> GetSnapshotAsync(Guid tripId, CancellationToken cancellationToken = default);
    }
}
