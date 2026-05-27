using HGTSWebApi.DTOs;
using HGTSWebApi.Hubs;
using HGTSWebApi.Models;
using Microsoft.AspNetCore.SignalR;

namespace HGTSWebApi.Services
{
    public class CapacityNotificationService : ICapacityNotificationService
    {
        private readonly IHubContext<CapacityHub> _hubContext;
        private readonly ICapacityStateCache _capacityStateCache;

        public CapacityNotificationService(
            IHubContext<CapacityHub> hubContext,
            ICapacityStateCache capacityStateCache)
        {
            _hubContext = hubContext;
            _capacityStateCache = capacityStateCache;
        }

        public async Task PublishAsync(
            Trip trip,
            int boardedCount,
            int capacity,
            string resultCode,
            string deviceId,
            string? studentId = null,
            string? studentName = null,
            CancellationToken cancellationToken = default)
        {
            var notification = new CapacityNotificationDto
            {
                TripId = trip.TripId,
                RouteId = trip.RouteId,
                RouteName = trip.BusRoute?.RouteName ?? string.Empty,
                RouteCode = trip.BusRoute?.RouteCode ?? string.Empty,
                DeviceId = deviceId,
                BoardedCount = boardedCount,
                Capacity = capacity,
                RemainingCapacity = Math.Max(capacity - boardedCount, 0),
                CapacityBand = GetCapacityBand(boardedCount, capacity),
                LastResultCode = resultCode,
                StudentId = studentId,
                StudentName = studentName,
                TimestampUtc = DateTime.UtcNow
            };

            await _capacityStateCache.SetAsync(notification, cancellationToken);

            var tripGroup = CapacityHub.GetTripGroupName(trip.TripId.ToString());
            var routeGroup = CapacityHub.GetRouteGroupName(trip.RouteId.ToString());

            await Task.WhenAll(
                _hubContext.Clients.Group(tripGroup).SendAsync("capacityUpdated", notification, cancellationToken),
                _hubContext.Clients.Group(routeGroup).SendAsync("capacityUpdated", notification, cancellationToken));
        }

        public Task<CapacityNotificationDto?> GetSnapshotAsync(Guid tripId, CancellationToken cancellationToken = default)
        {
            return _capacityStateCache.GetAsync(tripId, cancellationToken);
        }

        private static string GetCapacityBand(int boardedCount, int capacity)
        {
            if (capacity <= 0)
            {
                return "GREEN";
            }

            var percentage = (double)boardedCount / capacity * 100;
            if (percentage >= 80) return "RED";
            if (percentage >= 50) return "YELLOW";
            return "GREEN";
        }
    }
}
