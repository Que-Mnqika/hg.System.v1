using HGTSWebApi.DTOs;
using HGTSWebApi.Services;
using Microsoft.AspNetCore.SignalR;

namespace HGTSWebApi.Hubs
{
    public class CapacityHub : Hub
    {
        private readonly ICapacityStateCache _capacityStateCache;

        public CapacityHub(ICapacityStateCache capacityStateCache)
        {
            _capacityStateCache = capacityStateCache;
        }

        public async Task SubscribeToTrip(string tripId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetTripGroupName(tripId));

            if (Guid.TryParse(tripId, out var parsedTripId))
            {
                var snapshot = await _capacityStateCache.GetAsync(parsedTripId);
                if (snapshot != null)
                {
                    await Clients.Caller.SendAsync("capacityUpdated", snapshot);
                }
            }
        }

        public Task UnsubscribeFromTrip(string tripId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetTripGroupName(tripId));
        }

        public Task SubscribeToRoute(string routeId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, GetRouteGroupName(routeId));
        }

        public Task UnsubscribeFromRoute(string routeId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetRouteGroupName(routeId));
        }

        public static string GetTripGroupName(string tripId) => $"trip:{tripId}";
        public static string GetRouteGroupName(string routeId) => $"route:{routeId}";
    }
}
