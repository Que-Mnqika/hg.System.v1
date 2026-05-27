using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HGTSWebApi.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/trips")]
    public class TripsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TripsController> _logger;

        public TripsController(AppDbContext context, ILogger<TripsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/trips
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TripDto>>> GetTrips(
            [FromQuery] string? orgId,
            [FromQuery] string? status,
            [FromQuery] DateTime? date,
            [FromQuery] string? routeId)
        {
            try
            {
                var query = _context.Trips
                    .Include(t => t.BusRoute).ThenInclude(r => r.Institution)
                    .Include(t => t.Device)
                    .Include(t => t.Vehicle)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    var statusMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "assigned",    "Assigned"   },
                        { "scheduled",   "Assigned"   },   // legacy alias
                        { "in_progress", "InProgress" },
                        { "inprogress",  "InProgress" },
                        { "completed",   "Completed"  },
                        { "cancelled",   "Cancelled"  }
                    };
                    var mapped = statusMap.ContainsKey(status) ? statusMap[status] : status;
                    query = query.Where(t => t.Status == mapped);
                }

                if (date.HasValue)
                {
                    var start = date.Value.Date;
                    var end = start.AddDays(1);
                    query = query.Where(t =>
                        (t.ScheduledStartTime.HasValue
                            ? t.ScheduledStartTime.Value >= start && t.ScheduledStartTime.Value < end
                            : t.StartTime >= start && t.StartTime < end));
                }

                if (!string.IsNullOrEmpty(routeId) && Guid.TryParse(routeId, out var routeGuid))
                    query = query.Where(t => t.RouteId == routeGuid);

                var trips = await query
                    .OrderByDescending(t => t.ScheduledStartTime ?? t.StartTime)
                    .Select(t => new TripDto
                    {
                        TripId = t.TripId,
                        RouteId = t.RouteId,
                        RouteName = t.BusRoute != null ? t.BusRoute.RouteName : null,
                        RouteCode = t.BusRoute != null ? t.BusRoute.RouteCode : null,
                        InstitutionId = t.BusRoute != null ? t.BusRoute.InstitutionId : (Guid?)null,
                        InstitutionName = t.BusRoute != null && t.BusRoute.Institution != null
                            ? t.BusRoute.Institution.InstitutionName : null,
                        DeviceId = t.DeviceId,
                        DeviceIdentifier = t.Device != null ? t.Device.DeviceIdentifier : null,
                        VehicleId = t.VehicleId,
                        VehicleLabel = t.Vehicle != null ? t.Vehicle.RegistrationNumber : null,
                        ScheduledStartTime = t.ScheduledStartTime,
                        ScheduledEndTime = t.ScheduledEndTime,
                        ActualStartTime = t.ActualStartTime,
                        EndTime = t.EndTime,
                        Status = t.Status,
                        BoardingCount = _context.BoardingLogs.Count(l => l.TripId == t.TripId)
                    })
                    .ToListAsync();

                return Ok(trips);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trips");
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // GET /api/trips/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TripDto>> GetTrip(Guid id)
        {
            try
            {
                var trip = await _context.Trips
                    .Include(t => t.BusRoute).ThenInclude(r => r.Institution)
                    .Include(t => t.Device)
                    .Include(t => t.Vehicle)
                    .FirstOrDefaultAsync(t => t.TripId == id);

                if (trip == null) return NotFound();

                return Ok(new TripDto
                {
                    TripId = trip.TripId,
                    RouteId = trip.RouteId,
                    RouteName = trip.BusRoute?.RouteName,
                    RouteCode = trip.BusRoute?.RouteCode,
                    InstitutionId = trip.BusRoute?.InstitutionId,
                    InstitutionName = trip.BusRoute?.Institution?.InstitutionName,
                    DeviceId = trip.DeviceId,
                    DeviceIdentifier = trip.Device?.DeviceIdentifier,
                    VehicleId = trip.VehicleId,
                    VehicleLabel = trip.Vehicle?.RegistrationNumber,
                    ScheduledStartTime = trip.ScheduledStartTime,
                    ScheduledEndTime = trip.ScheduledEndTime,
                    StartTime = (DateTime)trip.StartTime,
                    EndTime = trip.EndTime,
                    Status = trip.Status,
                    BoardingCount = await _context.BoardingLogs.CountAsync(l => l.TripId == id)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trip {Id}", id);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // ============================================================================
        // POST /api/trips
        //
        // Creates a trip in "Assigned" status with a scheduled time window.
        // Example body:
        // {
        //   "routeId":            "...",
        //   "deviceId":           "...",
        //   "vehicleId":          "...",
        //   "scheduledStartTime": "2024-11-01T17:00:00Z",
        //   "scheduledEndTime":   "2024-11-01T17:30:00Z"
        // }
        //
        // TripStatusService (runs every 30 s) will automatically:
        //   → set Status = "InProgress" when ScheduledStartTime is reached
        //   → set Status = "Completed"  when ScheduledEndTime  is reached
        // ============================================================================
        [HttpPost]
        public async Task<ActionResult<TripDto>> CreateTrip([FromBody] CreateTripDto dto)
        {
            try
            {
                // Validate scheduled window
                if (dto.ScheduledEndTime <= dto.ScheduledStartTime)
                    return BadRequest(new { error = "ScheduledEndTime must be after ScheduledStartTime" });

                var route = await _context.VehicleRoutes
                    .Include(r => r.Institution)
                    .FirstOrDefaultAsync(r => r.RouteId == dto.RouteId);
                if (route == null)
                    return BadRequest(new { error = "Route not found" });

                var device = await _context.Devices.FindAsync(dto.DeviceId);
                if (device == null)
                    return BadRequest(new { error = "Device not found" });

                var vehicle = await _context.Vehicles.FindAsync(dto.VehicleId);
                if (vehicle == null)
                    return BadRequest(new { error = "Vehicle not found" });

                var trip = new Trip
                {
                    TripId = Guid.NewGuid(),
                    RouteId = dto.RouteId,
                    DeviceId = dto.DeviceId,
                    VehicleId = dto.VehicleId,

                    // Scheduled window — drives automatic status transitions
                    ScheduledStartTime = dto.ScheduledStartTime.ToUniversalTime(),
                    ScheduledEndTime = dto.ScheduledEndTime.ToUniversalTime(),

                    // StartTime mirrors ScheduledStartTime at creation;
                    // TripStatusService overwrites it with the actual moment it flips InProgress
                    StartTime = dto.ScheduledStartTime.ToUniversalTime(),
                    EndTime = dto.ScheduledEndTime.ToUniversalTime(),

                    // Always Assigned on creation — never let the client set InProgress
                    Status = "Assigned",

                    ResidenceId = route.ResidenceId
                };

                _context.Trips.Add(trip);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Trip {TripId} created — Assigned [{Start} → {End}]",
                    trip.TripId,
                    trip.ScheduledStartTime,
                    trip.ScheduledEndTime);

                return Ok(new TripDto
                {
                    TripId = trip.TripId,
                    RouteId = trip.RouteId,
                    RouteName = route.RouteName,
                    RouteCode = route.RouteCode,
                    InstitutionId = route.InstitutionId,
                    InstitutionName = route.Institution?.InstitutionName,
                    DeviceId = trip.DeviceId,
                    DeviceIdentifier = device.DeviceIdentifier,
                    VehicleId = trip.VehicleId,
                    VehicleLabel = vehicle.RegistrationNumber,
                    ScheduledStartTime = trip.ScheduledStartTime,
                    ScheduledEndTime = trip.ScheduledEndTime,
                    StartTime = (DateTime)trip.StartTime,
                    EndTime = trip.EndTime,
                    Status = trip.Status,
                    BoardingCount = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trip");
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // ============================================================================
        // PATCH /api/trips/{id}
        // Allows rescheduling or manual status override.
        // Rescheduling is only valid while status is still Assigned.
        // ============================================================================
        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> UpdateTrip(Guid id, [FromBody] UpdateTripDto dto)
        {
            try
            {
                var trip = await _context.Trips.FindAsync(id);
                if (trip == null) return NotFound();

                // Reschedule start time (only while Assigned)
                if (dto.ScheduledStartTime.HasValue)
                {
                    if (trip.Status != "Assigned")
                        return BadRequest(new
                        {
                            error = "Cannot reschedule start time — trip is already " + trip.Status
                        });
                    trip.ScheduledStartTime = dto.ScheduledStartTime.Value.ToUniversalTime();
                    trip.StartTime = dto.ScheduledStartTime.Value.ToUniversalTime();
                }

                // Reschedule end time (Assigned or InProgress)
                if (dto.ScheduledEndTime.HasValue)
                {
                    if (trip.Status == "Completed" || trip.Status == "Cancelled")
                        return BadRequest(new
                        {
                            error = "Cannot reschedule end time — trip is already " + trip.Status
                        });
                    trip.ScheduledEndTime = dto.ScheduledEndTime.Value.ToUniversalTime();
                    trip.EndTime = dto.ScheduledEndTime.Value.ToUniversalTime();
                }

                // Manual status override (emergency use / ops dashboard)
                if (!string.IsNullOrEmpty(dto.Status))
                {
                    var statusMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "assigned",    "Assigned"   },
                        { "in_progress", "InProgress" },
                        { "inprogress",  "InProgress" },
                        { "completed",   "Completed"  },
                        { "cancelled",   "Cancelled"  }
                    };
                    trip.Status = statusMap.ContainsKey(dto.Status) ? statusMap[dto.Status] : dto.Status;

                    // If manually set to InProgress, record actual start time
                    if (trip.Status == "InProgress" && trip.StartTime == trip.ScheduledStartTime)
                        trip.StartTime = DateTime.UtcNow;

                    // If manually set to Completed, record actual end time
                    if (trip.Status == "Completed")
                        trip.EndTime = DateTime.UtcNow;
                }

                // Legacy field support
                if (dto.StartTime.HasValue)
                    trip.StartTime = dto.StartTime.Value.ToUniversalTime();
                if (dto.EndTime.HasValue)
                    trip.EndTime = dto.EndTime.Value.ToUniversalTime();

                await _context.SaveChangesAsync();
                return Ok(new { message = "Trip updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trip {Id}", id);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // POST /api/trips/{id}/start - IMPROVED VERSION
        [HttpPost("{id:guid}/start")]
        public async Task<IActionResult> StartTrip(Guid id)
        {
            try
            {
                var trip = await _context.Trips.FindAsync(id);
                if (trip == null) return NotFound();

                // Prevent starting already completed or cancelled trips
                if (trip.Status == "Completed")
                    return BadRequest(new { error = "Cannot start a completed trip" });

                if (trip.Status == "Cancelled")
                    return BadRequest(new { error = "Cannot start a cancelled trip" });

                // If already InProgress, just return success without changing
                if (trip.Status == "InProgress")
                    return Ok(new { message = "Trip already in progress", status = trip.Status });

                trip.Status = "InProgress";
                trip.StartTime = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Trip started successfully", startTime = trip.StartTime });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting trip {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /api/trips/{id}/end - IMPROVED VERSION
        [HttpPost("{id:guid}/end")]
        public async Task<IActionResult> EndTrip(Guid id)
        {
            try
            {
                var trip = await _context.Trips.FindAsync(id);
                if (trip == null) return NotFound();

                // Prevent ending already completed or cancelled trips
                if (trip.Status == "Completed")
                    return BadRequest(new { error = "Trip already completed" });

                if (trip.Status == "Cancelled")
                    return BadRequest(new { error = "Cannot end a cancelled trip" });

                // If not started, optionally start it before ending
                if (trip.Status == "Assigned" || trip.Status == "Active")
                {
                    trip.Status = "InProgress";
                    trip.StartTime = DateTime.UtcNow;
                }

                trip.Status = "Completed";
                trip.EndTime = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Trip ended successfully", endTime = trip.EndTime });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending trip {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // DELETE /api/trips/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTrip(Guid id)
        {
            try
            {
                var trip = await _context.Trips
                    .Include(t => t.BoardingLogs)
                    .FirstOrDefaultAsync(t => t.TripId == id);

                if (trip == null) return NotFound();

                if (trip.BoardingLogs.Any())
                    return BadRequest(new { error = "Cannot delete trip with existing boarding logs" });

                _context.Trips.Remove(trip);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Trip deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting trip {Id}", id);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }
    }
}