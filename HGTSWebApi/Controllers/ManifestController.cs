using HGTSWebApi.Data;
using HGTSWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("api/manifest")]
    public class ManifestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ManifestController> _logger;

        public ManifestController(AppDbContext context,
                                   ILogger<ManifestController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ESP32 downloads this when trip becomes InProgress
        // GET /api/manifest/trip/{tripId}
        [HttpGet("trip/{tripId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTripManifest(Guid tripId)
        {
            var trip = await _context.Trips
                .Include(t => t.BusRoute)
                .Include(t => t.Residence)
                .FirstOrDefaultAsync(t => t.TripId == tripId
                                       && t.Status == "InProgress");

            if (trip == null)
                return NotFound(new { error = "No active trip found with this ID" });

            // Get all active students assigned to the trip's residence
            var passengers = await _context.Students
                .Include(s => s.Credentials.Where(c => c.IsActive))
                .Where(s => s.ResidenceId == trip.ResidenceId
                         && s.Status == "Active")
                .Select(s => new
                {
                    studentId = s.StudentId.ToString(),
                    fullName = s.FullName,
                    studentNumber = s.StudentNumber,
                    residenceId = s.ResidenceId.ToString(),
                    credentials = s.Credentials.Select(c => new
                    {
                        token = c.CredentialUid,
                        type = c.CredentialType
                    }).ToList()
                })
                .ToListAsync();

            // Flatten to a token lookup list for the ESP32
            var tokenList = passengers
                .SelectMany(p => p.credentials.Select(c => new
                {
                    token = c.token,
                    type = c.type,
                    studentId = p.studentId,
                    residenceId = p.residenceId,
                    fullName = p.fullName
                }))
                .ToList();

            return Ok(new
            {
                tripId = trip.TripId.ToString(),
                routeId = trip.RouteId.ToString(),
                routeName = trip.BusRoute?.RouteName,
                residenceId = trip.ResidenceId?.ToString(),
                residenceName = trip.Residence?.ResidenceName,
                scheduledStart = trip.ScheduledStartTime,
                scheduledEnd = trip.ScheduledEndTime,
                generatedAt = DateTime.UtcNow,
                passengerCount = passengers.Count,
                tokenCount = tokenList.Count,
                tokens = tokenList   // ← ESP32 stores this flat list
            });
        }

        // ESP32 uploads all cached offline taps when back online
        // POST /api/manifest/sync
        [HttpPost("sync")]
        [AllowAnonymous]
        public async Task<IActionResult> SyncManifestTaps(
            [FromBody] ManifestSyncRequestDto request)
        {
            if (request?.Taps == null || !request.Taps.Any())
                return BadRequest(new { error = "No taps provided" });

            var synced = 0;
            var duplicates = 0;
            var errors = 0;
            var results = new List<object>();

            foreach (var tap in request.Taps)
            {
                try
                {
                    // Parse client timestamp
                    DateTime clientTs;
                    if (!DateTime.TryParse(tap.ReadableTime, out clientTs))
                        clientTs = DateTimeOffset
                            .FromUnixTimeMilliseconds(tap.Timestamp)
                            .UtcDateTime;

                    var credential = await _context.NFCCredentials
                        .FirstOrDefaultAsync(c => c.CredentialUid == tap.Token);

                    // Duplicate check: same credential + same trip + same minute
                    bool isDuplicate = false;
                    if (credential != null && tap.TripId.HasValue)
                    {
                        isDuplicate = await _context.BoardingLogs.AnyAsync(l =>
                            l.CredentialId == credential.CredentialId
                            && l.TripId == tap.TripId
                            && Math.Abs((l.ClientTimestamp - clientTs).TotalSeconds) < 60);
                    }

                    if (isDuplicate)
                    {
                        duplicates++;
                        results.Add(new { tap.Token, status = "DUPLICATE_SKIPPED" });
                        continue;
                    }

                    var log = new BoardingLog
                    {
                        LogId = Guid.NewGuid(),
                        CredentialId = credential?.CredentialId,
                        TripId = tap.TripId,
                        DeviceId = tap.DeviceId,
                        CredentialUid = tap.RawUid,
                        ClientTimestamp = clientTs,
                        ServerTimestamp = DateTime.UtcNow,
                        Allowed = tap.AccessGranted,
                        RouteMismatch = tap.RouteMismatch,
                        Result = tap.ResultCode,
                        Reason = tap.Message,
                        IsOffline = true
                    };

                    _context.BoardingLogs.Add(log);
                    synced++;
                    results.Add(new { tap.Token, status = "SYNCED" });
                }
                catch (Exception ex)
                {
                    errors++;
                    results.Add(new { tap.Token, status = "ERROR", error = ex.Message });
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Manifest sync: {Synced} synced, {Dupes} duplicates, {Errors} errors",
                synced, duplicates, errors);

            return Ok(new
            {
                totalReceived = request.Taps.Count,
                synced,
                duplicates,
                errors,
                results
            });
        }
    }

    // DTO for manifest sync
    public class ManifestSyncRequestDto
    {
        public List<ManifestTapDto> Taps { get; set; } = new();
    }

    public class ManifestTapDto
    {
        public string Token { get; set; } = string.Empty;
        public string RawUid { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public Guid? TripId { get; set; }
        public bool AccessGranted { get; set; }
        public bool RouteMismatch { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public string ReadableTime { get; set; } = string.Empty;
    }
}