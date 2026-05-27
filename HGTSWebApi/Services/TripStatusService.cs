using HGTSWebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace HGTSWebApi.Services
{
    public class TripStatusService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TripStatusService> _logger;

        public TripStatusService(IServiceScopeFactory scopeFactory,
                                  ILogger<TripStatusService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TripStatusService started");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateTripStatuses();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in TripStatusService");
                }

                // Check every 30 seconds
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task UpdateTripStatuses()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;
            bool changed = false;

            // 1. Assigned (Booked) → Active when ScheduledStartTime is reached
            var toActive = await db.Trips
                .Where(t => t.Status == "Assigned"
                         && t.ScheduledStartTime.HasValue
                         && t.ScheduledStartTime.Value <= now)
                .ToListAsync();

            foreach (var trip in toActive)
            {
                trip.Status = "Active";
                trip.StartTime = now;
                _logger.LogInformation("Trip {TripId} → Active at {Time} (Scheduled start: {ScheduledTime})",
                    trip.TripId, now, trip.ScheduledStartTime);
                changed = true;
            }

            // 2. Active → InProgress (1 minute after scheduled start time)
            var toInProgress = await db.Trips
                .Where(t => t.Status == "Active"
                         && t.ScheduledStartTime.HasValue
                         && t.ScheduledStartTime.Value.AddMinutes(1) <= now)
                .ToListAsync();

            foreach (var trip in toInProgress)
            {
                trip.Status = "InProgress";
                _logger.LogInformation("Trip {TripId} → InProgress at {Time} (1 min after start)",
                    trip.TripId, now);
                changed = true;
            }

            // 3. InProgress → Completed when ScheduledEndTime is reached
            var toCompleted = await db.Trips
                .Where(t => t.Status == "InProgress"
                         && t.ScheduledEndTime.HasValue
                         && t.ScheduledEndTime.Value <= now)
                .ToListAsync();

            foreach (var trip in toCompleted)
            {
                trip.Status = "Completed";
                trip.EndTime = now;
                _logger.LogInformation("Trip {TripId} → Completed at {Time} (Scheduled end: {ScheduledTime})",
                    trip.TripId, now, trip.ScheduledEndTime);
                changed = true;
            }

            if (changed)
            {
                await db.SaveChangesAsync();
                _logger.LogInformation("Trip statuses updated successfully");
            }
        }
    }
}