using HGTSWebApi.Data;
using HGTSWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HGTSWebApi.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/analytics")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(AppDbContext context, ILogger<AnalyticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/analytics/boarding
        [HttpGet("boarding")]
        public async Task<ActionResult<object>> GetBoardingAnalytics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? deviceId,
            [FromQuery] string? routeId)
        {
            try
            {
                var end = endDate ?? DateTime.UtcNow;
                var start = startDate ?? end.AddDays(-30);

                var query = _context.BoardingLogs
                    .Include(l => l.Trip)
                    .Where(l => l.ClientTimestamp >= start && l.ClientTimestamp <= end);

                if (!string.IsNullOrEmpty(deviceId))
                    query = query.Where(l => l.DeviceId == deviceId);

                if (!string.IsNullOrEmpty(routeId) && Guid.TryParse(routeId, out var routeGuid))
                {
                    query = query.Where(l => l.Trip != null && l.Trip.RouteId == routeGuid);
                }

                var logs = await query.ToListAsync();

                var totalBoardings = logs.Count;
                var successfulBoardings = logs.Count(l => l.Allowed);
                var deniedBoardings = logs.Count(l => !l.Allowed);
                var routeMismatches = logs.Count(l => l.RouteMismatch);
                var successRate = totalBoardings > 0 ? (double)successfulBoardings / totalBoardings * 100 : 0;

                var hourlyBreakdown = logs
                    .GroupBy(l => l.ClientTimestamp.Hour)
                    .Select(g => new { Hour = g.Key, Count = g.Count() })
                    .OrderBy(h => h.Hour)
                    .ToList();

                var dailyTrend = logs
                    .GroupBy(l => l.ClientTimestamp.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(d => d.Date)
                    .ToList();

                return Ok(new
                {
                    StartDate = start,
                    EndDate = end,
                    TotalBoardings = totalBoardings,
                    SuccessfulBoardings = successfulBoardings,
                    DeniedBoardings = deniedBoardings,
                    RouteMismatches = routeMismatches,
                    SuccessRate = Math.Round(successRate, 2),
                    HourlyBreakdown = hourlyBreakdown,
                    DailyTrend = dailyTrend
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting boarding analytics");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /api/analytics/reset-Vehicle-routes
        [HttpPost("reset-Vehicle-routes")]
        public async Task<IActionResult> ResetVehicleRoutes()
        {
            try
            {
                // Clear existing Vehicle routes
                var existingRoutes = _context.VehicleRoutes;
                _context.VehicleRoutes.RemoveRange(existingRoutes);

                // Define new routes
                var residences = _context.Residences.ToList();
                var pickupZones = _context.PickupZones.ToList();

                var newRoutes = new List<VehicleRoute>
                {
                    new VehicleRoute { RouteCode = "BEL-BV", RouteName = "Belpark Residence to CPUT Bellville", ResidenceId = residences.FirstOrDefault(r => r.ResidenceCode == "BEL")?.ResidenceId, PickupZoneId = pickupZones.FirstOrDefault(p => p.PickupZoneCode == "BV")?.PickupZoneId },
                    new VehicleRoute { RouteCode = "CPUTBV-BV", RouteName = "CPUT Bellville Campus to CPUT Bellville", ResidenceId = residences.FirstOrDefault(r => r.ResidenceCode == "CPUTBV")?.ResidenceId, PickupZoneId = pickupZones.FirstOrDefault(p => p.PickupZoneCode == "BV")?.PickupZoneId },
                    new VehicleRoute { RouteCode = "CPUTD6-D6", RouteName = "CPUT District Six Campus to CPUT District Six", ResidenceId = residences.FirstOrDefault(r => r.ResidenceCode == "CPUTD6")?.ResidenceId, PickupZoneId = pickupZones.FirstOrDefault(p => p.PickupZoneCode == "D6")?.PickupZoneId },
                    new VehicleRoute { RouteCode = "CPUTMB-MB", RouteName = "CPUT Mowbray Campus to CPUT Mowbray", ResidenceId = residences.FirstOrDefault(r => r.ResidenceCode == "CPUTMB")?.ResidenceId, PickupZoneId = pickupZones.FirstOrDefault(p => p.PickupZoneCode == "MB")?.PickupZoneId },
                    new VehicleRoute { RouteCode = "CPUTWL-WYN", RouteName = "CPUT Wellington Campus to The Wynne", ResidenceId = residences.FirstOrDefault(r => r.ResidenceCode == "CPUTWL")?.ResidenceId, PickupZoneId = pickupZones.FirstOrDefault(p => p.PickupZoneCode == "WYN")?.PickupZoneId }
                };

                // Add new routes
                await _context.VehicleRoutes.AddRangeAsync(newRoutes);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Vehicle routes have been reset and updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting Vehicle routes");
                return StatusCode(500, new { Message = "An error occurred while resetting Vehicle routes." });
            }
        }
    }
}