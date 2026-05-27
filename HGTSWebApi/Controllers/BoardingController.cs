using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;
using HGTSWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("api/boarding")]
    public class BoardingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BoardingController> _logger;

        public BoardingController(AppDbContext context, ILogger<BoardingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // POST /api/boarding/validate
        [HttpPost("validate")]
        [AllowAnonymous]
        public async Task<ActionResult<BoardingValidateResponseDto>> Validate([FromBody] BoardingValidateRequestDto request)
        {
            try
            {
                _logger.LogInformation("Validate - UID: {CredentialUid}, Device: {DeviceId}, TripId: {TripId}, RouteId: {RouteId}",
                    request.CredentialUid, request.DeviceId, request.TripId, request.RouteId);

                var credential = await _context.NFCCredentials
                    .Include(c => c.Student)
                        .ThenInclude(s => s != null ? s.Residence : null)
                    .FirstOrDefaultAsync(c => c.CredentialUid == request.CredentialUid && c.IsActive);

                if (credential == null)
                {
                    var deniedLog = new BoardingLog
                    {
                        LogId = Guid.NewGuid(),
                        CredentialId = null,
                        TripId = !string.IsNullOrEmpty(request.TripId) ? Guid.Parse(request.TripId) : null,
                        DeviceId = request.DeviceId,
                        CredentialUid = request.CredentialUid,
                        ClientTimestamp = request.Timestamp,
                        ServerTimestamp = DateTime.UtcNow,
                        Allowed = false,
                        RouteMismatch = false,
                        Result = "UNKNOWN_CREDENTIAL",
                        Reason = "Access Denied - Unknown or Inactive Credential",
                        IsOffline = false
                    };
                    _context.BoardingLogs.Add(deniedLog);
                    await _context.SaveChangesAsync();

                    return Ok(new BoardingValidateResponseDto
                    {
                        AccessGranted = false,
                        ResultCode = "UNKNOWN_CREDENTIAL",
                        Message = "Access Denied - Unknown or Inactive Credential",
                        RouteId = request.RouteId
                    });
                }

                if (credential.Student?.Status != "Active")
                {
                    var deniedLog = new BoardingLog
                    {
                        LogId = Guid.NewGuid(),
                        CredentialId = credential.CredentialId,
                        TripId = !string.IsNullOrEmpty(request.TripId) ? Guid.Parse(request.TripId) : null,
                        DeviceId = request.DeviceId,
                        CredentialUid = request.CredentialUid,
                        ClientTimestamp = request.Timestamp,
                        ServerTimestamp = DateTime.UtcNow,
                        Allowed = false,
                        RouteMismatch = false,
                        Result = "INACTIVE_STUDENT",
                        Reason = "Access Denied - Account Inactive",
                        IsOffline = false
                    };
                    _context.BoardingLogs.Add(deniedLog);
                    await _context.SaveChangesAsync();

                    return Ok(new BoardingValidateResponseDto
                    {
                        AccessGranted = false,
                        ResultCode = "INACTIVE_STUDENT",
                        Message = "Access Denied - Account Inactive",
                        RouteId = request.RouteId
                    });
                }

                var alreadyBoarded = await _context.BoardingLogs
                    .AnyAsync(l => l.CredentialId == credential.CredentialId
                        && l.TripId.ToString() == request.TripId
                        && l.Allowed == true);

                if (alreadyBoarded)
                {
                    var duplicateLog = new BoardingLog
                    {
                        LogId = Guid.NewGuid(),
                        CredentialId = credential.CredentialId,
                        TripId = !string.IsNullOrEmpty(request.TripId) ? Guid.Parse(request.TripId) : null,
                        DeviceId = request.DeviceId,
                        CredentialUid = request.CredentialUid,
                        ClientTimestamp = request.Timestamp,
                        ServerTimestamp = DateTime.UtcNow,
                        Allowed = false,
                        RouteMismatch = false,
                        Result = "ALREADY_BOARDED",
                        Reason = "Student already boarded this trip",
                        IsOffline = false
                    };
                    _context.BoardingLogs.Add(duplicateLog);
                    await _context.SaveChangesAsync();

                    return Ok(new BoardingValidateResponseDto
                    {
                        AccessGranted = false,
                        ResultCode = "ALREADY_BOARDED",
                        Message = "Student already boarded this trip",
                        StudentId = credential.StudentId.ToString(),
                        RouteId = request.RouteId
                    });
                }

                Guid? tripIdGuid = !string.IsNullOrEmpty(request.TripId) ? Guid.Parse(request.TripId) : (Guid?)null;
                Trip? activeTrip = null;

                if (tripIdGuid.HasValue)
                {
                    activeTrip = await _context.Trips
                        .Include(t => t.Vehicle)
                        .FirstOrDefaultAsync(t => t.TripId == tripIdGuid.Value);
                }

                // Check vehicle capacity
                int vehicleCapacity = activeTrip?.Vehicle?.Capacity ?? 50;
                int currentPassengers = await _context.BoardingLogs
                    .CountAsync(l => l.TripId == tripIdGuid && l.Allowed == true);

                if (currentPassengers >= vehicleCapacity)
                {
                    var overloadLog = new BoardingLog
                    {
                        LogId = Guid.NewGuid(),
                        CredentialId = credential.CredentialId,
                        TripId = tripIdGuid,
                        DeviceId = request.DeviceId,
                        CredentialUid = request.CredentialUid,
                        ClientTimestamp = request.Timestamp,
                        ServerTimestamp = DateTime.UtcNow,
                        Allowed = false,
                        RouteMismatch = false,
                        Result = "OVERLOAD",
                        Reason = "Vehicle at maximum capacity",
                        IsOffline = false
                    };
                    _context.BoardingLogs.Add(overloadLog);
                    await _context.SaveChangesAsync();

                    return Ok(new BoardingValidateResponseDto
                    {
                        AccessGranted = false,
                        ResultCode = "OVERLOAD",
                        Message = "Bus is full. Please wait for next trip.",
                        RouteId = request.RouteId
                    });
                }

                Guid? studentResidenceId = credential.Student?.ResidenceId;
                Guid? tripResidenceId = activeTrip?.ResidenceId;
                bool isRouteMatch = (studentResidenceId == tripResidenceId);

                _logger.LogInformation("Route Check - Student Residence: {StudentResidence}, Trip Residence: {TripResidence}, Match: {IsMatch}",
                    studentResidenceId, tripResidenceId, isRouteMatch);

                bool accessGranted = true;

                // Check if this is the first boarding of the trip
                var isFirstBoarding = !await _context.BoardingLogs
                    .AnyAsync(l => l.TripId == tripIdGuid && l.Allowed == true);

                if (isFirstBoarding && activeTrip != null && !activeTrip.ActualStartTime.HasValue)
                {
                    activeTrip.ActualStartTime = DateTime.UtcNow;
                    _logger.LogInformation("Trip {TripId} actual start time set to {ActualStartTime}",
                        activeTrip.TripId, activeTrip.ActualStartTime);
                    await _context.SaveChangesAsync();
                }

                credential.LastSeenAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var boardingLog = new BoardingLog
                {
                    LogId = Guid.NewGuid(),
                    CredentialId = credential.CredentialId,
                    TripId = tripIdGuid,
                    DeviceId = request.DeviceId,
                    CredentialUid = request.CredentialUid,
                    ClientTimestamp = request.Timestamp,
                    ServerTimestamp = DateTime.UtcNow,
                    Allowed = accessGranted,
                    RouteMismatch = !isRouteMatch,
                    Result = accessGranted ? "SUCCESS" : "DENIED",
                    Reason = isRouteMatch ? "Access granted" : "Access granted - Route mismatch",
                    IsOffline = false
                };

                _context.BoardingLogs.Add(boardingLog);
                await _context.SaveChangesAsync();

                var response = new BoardingValidateResponseDto
                {
                    AccessGranted = accessGranted,
                    ResultCode = isRouteMatch ? "SUCCESS" : "ROUTE_MISMATCH",
                    Message = isRouteMatch
                        ? $"Welcome {credential.Student.FullName}"
                        : $"Welcome {credential.Student.FullName} - You are on the wrong Vehicle!",
                    StudentId = credential.StudentId.ToString(),
                    RouteId = request.RouteId ?? "",
                    ResidenceId = studentResidenceId?.ToString() ?? ""
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in validate");
                return Ok(new BoardingValidateResponseDto
                {
                    AccessGranted = false,
                    ResultCode = "ERROR",
                    Message = "System error"
                });
            }
        }

        // POST /api/boarding/logs
        [HttpPost("logs")]
        [AllowAnonymous]
        public async Task<IActionResult> Log([FromBody] BoardingLogRequestDto request)
        {
            try
            {
                _logger.LogInformation("Log - UID: {Uid}, Device: {DeviceId}", request.Uid, request.DeviceId);

                var credential = await _context.NFCCredentials
                    .FirstOrDefaultAsync(c => c.CredentialUid == request.Uid);

                var boardingLog = new BoardingLog
                {
                    LogId = Guid.NewGuid(),
                    CredentialId = credential?.CredentialId,
                    TripId = null,
                    DeviceId = request.DeviceId.ToString(),
                    CredentialUid = request.Uid,
                    ClientTimestamp = request.Timestamp,
                    ServerTimestamp = DateTime.UtcNow,
                    Allowed = request.AccessGranted,
                    RouteMismatch = false,
                    Result = request.AccessGranted ? "Success" : "Denied",
                    Reason = request.Message,
                    IsOffline = false
                };

                _context.BoardingLogs.Add(boardingLog);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Logged successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in log");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /api/boarding/offline-sync/single
        [HttpPost("offline-sync/single")]
        [AllowAnonymous]
        public async Task<IActionResult> SyncOfflineTap([FromBody] OfflineTapDto request)
        {
            try
            {
                _logger.LogInformation("Offline sync - UID: {Uid}, Result: {ResultCode}, TripId: {TripId}",
                    request.Uid, request.ResultCode, request.TripId);

                long tapTimeMillis = 0;
                long.TryParse(request.TapTime, out tapTimeMillis);
                var tapDateTime = DateTimeOffset.FromUnixTimeMilliseconds(tapTimeMillis).UtcDateTime;

                var credential = await _context.NFCCredentials
                    .FirstOrDefaultAsync(c => c.CredentialUid == request.Uid && c.IsActive);

                Guid? tripIdGuid = null;
                if (!string.IsNullOrEmpty(request.TripId) && Guid.TryParse(request.TripId, out var parsedTripId))
                {
                    tripIdGuid = parsedTripId;
                    _logger.LogInformation("Offline tap has TripId: {TripId}", request.TripId);
                }
                else
                {
                    _logger.LogWarning("Offline tap has NO TripId!");
                }

                if (credential == null)
                {
                    var unknownLog = new BoardingLog
                    {
                        LogId = Guid.NewGuid(),
                        CredentialId = null,
                        TripId = tripIdGuid,
                        DeviceId = request.DeviceId,
                        CredentialUid = request.Uid,
                        ClientTimestamp = tapDateTime,
                        ServerTimestamp = DateTime.UtcNow,
                        Allowed = false,
                        RouteMismatch = false,
                        Result = "UNKNOWN_CREDENTIAL",
                        Reason = "Credential not found during offline sync",
                        IsOffline = true
                    };
                    _context.BoardingLogs.Add(unknownLog);
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Logged as unknown credential", synced = true });
                }

                if (credential.Student?.Status != "Active")
                {
                    var inactiveLog = new BoardingLog
                    {
                        LogId = Guid.NewGuid(),
                        CredentialId = credential.CredentialId,
                        TripId = tripIdGuid,
                        DeviceId = request.DeviceId,
                        CredentialUid = request.Uid,
                        ClientTimestamp = tapDateTime,
                        ServerTimestamp = DateTime.UtcNow,
                        Allowed = false,
                        RouteMismatch = false,
                        Result = "INACTIVE_STUDENT",
                        Reason = "Student account is inactive",
                        IsOffline = true
                    };
                    _context.BoardingLogs.Add(inactiveLog);
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Logged as inactive student", synced = true });
                }

                bool granted = (request.ResultCode == "SUCCESS" || request.ResultCode == "ROUTE_MISMATCH");
                bool routeMismatch = (request.ResultCode == "ROUTE_MISMATCH");

                var boardingLog = new BoardingLog
                {
                    LogId = Guid.NewGuid(),
                    CredentialId = credential.CredentialId,
                    TripId = tripIdGuid,
                    DeviceId = request.DeviceId,
                    CredentialUid = request.Uid,
                    ClientTimestamp = tapDateTime,
                    ServerTimestamp = DateTime.UtcNow,
                    Allowed = granted,
                    RouteMismatch = routeMismatch,
                    Result = request.ResultCode ?? (granted ? "SUCCESS" : "DENIED"),
                    Reason = request.Message ?? (granted ? "Offline sync - boarding confirmed" : "Offline sync - access denied"),
                    IsOffline = true
                };

                _context.BoardingLogs.Add(boardingLog);

                if (granted && credential != null)
                {
                    credential.LastSeenAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Offline tap synced: {ResultCode} for credential {CredentialId} with TripId: {TripId}",
                    request.ResultCode, credential.CredentialId, tripIdGuid);

                return Ok(new { message = "Synced successfully", logId = boardingLog.LogId, synced = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing offline tap for uid: {Uid}", request.Uid);
                return StatusCode(500, new { error = "Internal server error", synced = false });
            }
        }

        // POST /api/boarding/offline-sync/bulk
        [HttpPost("offline-sync/bulk")]
        [AllowAnonymous]
        public async Task<IActionResult> SyncOfflineTapsBulk([FromBody] OfflineSyncRequestDto request)
        {
            try
            {
                _logger.LogInformation("Offline bulk sync - Processing {Count} taps", request.Taps?.Count ?? 0);

                if (request.Taps == null || request.Taps.Count == 0)
                {
                    return Ok(new OfflineSyncResponseDto { SyncedCount = 0, Message = "No taps to sync" });
                }

                int syncedCount = 0;
                int errorCount = 0;

                foreach (var tap in request.Taps)
                {
                    try
                    {
                        long tapTimeMillis = 0;
                        long.TryParse(tap.TapTime, out tapTimeMillis);
                        var tapDateTime = DateTimeOffset.FromUnixTimeMilliseconds(tapTimeMillis).UtcDateTime;

                        var credential = await _context.NFCCredentials
                            .FirstOrDefaultAsync(c => c.CredentialUid == tap.Uid && c.IsActive);

                        Guid? tripIdGuid = null;
                        if (!string.IsNullOrEmpty(tap.TripId) && Guid.TryParse(tap.TripId, out var parsedTripId))
                        {
                            tripIdGuid = parsedTripId;
                        }

                        if (credential == null)
                        {
                            var unknownLog = new BoardingLog
                            {
                                LogId = Guid.NewGuid(),
                                CredentialId = null,
                                TripId = tripIdGuid,
                                DeviceId = tap.DeviceId,
                                CredentialUid = tap.Uid,
                                ClientTimestamp = tapDateTime,
                                ServerTimestamp = DateTime.UtcNow,
                                Allowed = false,
                                RouteMismatch = false,
                                Result = "UNKNOWN_CREDENTIAL",
                                Reason = "Credential not found during offline sync",
                                IsOffline = true
                            };
                            _context.BoardingLogs.Add(unknownLog);
                            syncedCount++;
                            continue;
                        }

                        if (credential.Student?.Status != "Active")
                        {
                            var inactiveLog = new BoardingLog
                            {
                                LogId = Guid.NewGuid(),
                                CredentialId = credential.CredentialId,
                                TripId = tripIdGuid,
                                DeviceId = tap.DeviceId,
                                CredentialUid = tap.Uid,
                                ClientTimestamp = tapDateTime,
                                ServerTimestamp = DateTime.UtcNow,
                                Allowed = false,
                                RouteMismatch = false,
                                Result = "INACTIVE_STUDENT",
                                Reason = "Student account is inactive",
                                IsOffline = true
                            };
                            _context.BoardingLogs.Add(inactiveLog);
                            syncedCount++;
                            continue;
                        }

                        bool granted = (tap.ResultCode == "SUCCESS" || tap.ResultCode == "ROUTE_MISMATCH");
                        bool routeMismatch = (tap.ResultCode == "ROUTE_MISMATCH");

                        var boardingLog = new BoardingLog
                        {
                            LogId = Guid.NewGuid(),
                            CredentialId = credential.CredentialId,
                            TripId = tripIdGuid,
                            DeviceId = tap.DeviceId,
                            CredentialUid = tap.Uid,
                            ClientTimestamp = tapDateTime,
                            ServerTimestamp = DateTime.UtcNow,
                            Allowed = granted,
                            RouteMismatch = routeMismatch,
                            Result = tap.ResultCode ?? (granted ? "SUCCESS" : "DENIED"),
                            Reason = tap.Message ?? (granted ? "Offline sync - boarding confirmed" : "Offline sync - access denied"),
                            IsOffline = true
                        };

                        _context.BoardingLogs.Add(boardingLog);

                        if (credential != null && granted)
                        {
                            credential.LastSeenAt = DateTime.UtcNow;
                        }

                        syncedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing individual offline tap: {Uid}", tap.Uid);
                        errorCount++;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Offline bulk sync completed: {SyncedCount} taps synced, {ErrorCount} errors",
                    syncedCount, errorCount);

                return Ok(new OfflineSyncResponseDto
                {
                    SyncedCount = syncedCount,
                    Message = $"Successfully synced {syncedCount} of {request.Taps.Count} taps (Errors: {errorCount})"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in offline bulk sync");
                return StatusCode(500, new OfflineSyncResponseDto
                {
                    SyncedCount = 0,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        // GET /api/boarding/logs
        [HttpGet("logs")]
        public async Task<ActionResult<BoardingLogListResponseDto>> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? uid = null,
            [FromQuery] string? tripId = null,
            [FromQuery] bool? routeMismatch = null,
            [FromQuery] bool? accessGranted = null,
            [FromQuery] string? resultCode = null,
            [FromQuery] bool? isOffline = null,
            [FromQuery] Guid? residenceId = null)
        {
            try
            {
                var query = _context.BoardingLogs
                    .Include(l => l.Credential)
                        .ThenInclude(c => c != null ? c.Student : null)
                            .ThenInclude(s => s != null ? s.Residence : null)
                    .Include(l => l.Trip)
                        .ThenInclude(t => t != null ? t.BusRoute : null)
                    .AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(l => l.ClientTimestamp >= fromDate.Value);
                if (toDate.HasValue)
                    query = query.Where(l => l.ClientTimestamp <= toDate.Value);
                if (!string.IsNullOrEmpty(uid))
                    query = query.Where(l => l.Credential != null && l.Credential.CredentialUid == uid);
                if (!string.IsNullOrEmpty(tripId) && Guid.TryParse(tripId, out var tripGuid))
                    query = query.Where(l => l.TripId == tripGuid);
                if (routeMismatch.HasValue)
                    query = query.Where(l => l.RouteMismatch == routeMismatch.Value);
                if (accessGranted.HasValue)
                    query = query.Where(l => l.Allowed == accessGranted.Value);
                if (!string.IsNullOrEmpty(resultCode))
                    query = query.Where(l => l.Result == resultCode);
                if (isOffline.HasValue)
                    query = query.Where(l => l.IsOffline == isOffline.Value);
                if (residenceId.HasValue)
                {
                    query = query.Where(l => l.Credential != null &&
                                             l.Credential.Student != null &&
                                             l.Credential.Student.ResidenceId == residenceId.Value);
                }

                var totalCount = await query.CountAsync();

                var logs = await query
                    .OrderByDescending(l => l.ServerTimestamp)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new BoardingLogDto
                    {
                        LogId = l.LogId,
                        CredentialId = l.CredentialId,
                        CredentialToken = l.Credential != null ? l.Credential.CredentialUid : null,
                        CredentialType = l.Credential != null ? l.Credential.CredentialType : null,
                        StudentName = l.Credential != null && l.Credential.Student != null ? l.Credential.Student.FullName : null,
                        StudentNumber = l.Credential != null && l.Credential.Student != null ? l.Credential.Student.StudentNumber : null,
                        DeviceId = l.DeviceId,
                        TripId = l.TripId,
                        RouteName = l.Trip != null && l.Trip.BusRoute != null ? l.Trip.BusRoute.RouteName : null,
                        ClientTimestamp = l.ClientTimestamp,
                        ServerTimestamp = l.ServerTimestamp,
                        AccessGranted = l.Allowed,
                        RouteMismatch = l.RouteMismatch,
                        Result = l.Result,
                        Message = l.Reason,
                        IsOffline = l.IsOffline,
                        ResidenceId = l.Credential != null && l.Credential.Student != null ? l.Credential.Student.ResidenceId : null,
                        ResidenceName = l.Credential != null && l.Credential.Student != null && l.Credential.Student.Residence != null ? l.Credential.Student.Residence.ResidenceName : null
                    })
                    .ToListAsync();

                var response = new BoardingLogListResponseDto
                {
                    Logs = logs,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting boarding logs");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /api/boarding/logs/by-residence/{residenceId} - ONLY BOARDED STUDENTS (GRANTED ONLY)
        [HttpGet("logs/by-residence/{residenceId:guid}")]
        public async Task<ActionResult<object>> GetBoardingLogsByResidence(Guid residenceId)
        {
            try
            {
                // Verify residence exists
                var residence = await _context.Residences
                    .FirstOrDefaultAsync(r => r.ResidenceId == residenceId);

                if (residence == null)
                    return NotFound(new { error = "Residence not found" });

                // Get ONLY boarded students (students with boarding logs where Allowed = true)
                var logs = await _context.BoardingLogs
                    .Include(l => l.Credential)
                        .ThenInclude(c => c != null ? c.Student : null)
                    .Include(l => l.Trip)
                        .ThenInclude(t => t.BusRoute)
                            .ThenInclude(r => r != null ? r.PickupZone : null)
                    .Include(l => l.Trip)
                        .ThenInclude(t => t != null ? t.Vehicle : null)
                    .Where(l => l.Credential != null
                        && l.Credential.Student != null
                        && l.Credential.Student.ResidenceId == residenceId
                        && l.Allowed == true)  // Only granted/boarded students
                    .OrderByDescending(l => l.ClientTimestamp)
                    .Select(l => new
                    {
                        // CogId (Credential ID)
                        CogId = l.Credential != null ? l.Credential.CredentialId.ToString() : null,

                        // PickUpZoneName
                        PickUpZoneName = l.Trip != null && l.Trip.BusRoute != null && l.Trip.BusRoute.PickupZone != null
                            ? l.Trip.BusRoute.PickupZone.Name
                            : null,

                        // TripId
                        TripId = l.TripId,

                        // ResidenceId
                        ResidenceId = l.Credential.Student.ResidenceId,

                        // RouteName
                        RouteName = l.Trip != null && l.Trip.BusRoute != null
                            ? l.Trip.BusRoute.RouteName
                            : null,

                        // ClientTimestamp
                        ClientTimestamp = l.ClientTimestamp,

                        // Trip.StartTime (Actual start time if available)
                        TripStartTime = l.Trip.ActualStartTime,

                        // Vehicle.RegistrationNumber
                        VehicleRegistrationNumber = l.Trip != null && l.Trip.Vehicle != null
                            ? l.Trip.Vehicle.RegistrationNumber
                            : null,

                        // StudentName
                        StudentName = l.Credential.Student.FullName,

                        // StudentNumber
                        StudentNumber = l.Credential.Student.StudentNumber,

                        // StudentCredentialId
                        StudentCredentialId = l.Credential.CredentialId,

                        // StudentTimeZone (from residence)
                        StudentTimeZone = l.Credential.Student.Residence != null
                            ? (l.Credential.Student.Residence.Timezone ?? "UTC")
                            : "UTC",

                        // Vehicle.Capacity
                        VehicleCapacity = l.Trip != null && l.Trip.Vehicle != null
                            ? l.Trip.Vehicle.Capacity
                            : (int?)null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    ResidenceId = residenceId,
                    ResidenceName = residence.ResidenceName,
                    TotalBoardingAttempts = logs.Count,
                    Logs = logs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting boarding logs by residence {ResidenceId}", residenceId);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // GET /api/boarding/logs/trip/{tripId}
        [HttpGet("logs/trip/{tripId:guid}")]
        public async Task<ActionResult<object>> GetTripReport(Guid tripId)
        {
            try
            {
                var trip = await _context.Trips
                    .Include(t => t.BusRoute)
                    .Include(t => t.Residence)
                    .FirstOrDefaultAsync(t => t.TripId == tripId);

                if (trip == null)
                    return NotFound(new { error = "Trip not found" });

                var logs = await _context.BoardingLogs
                    .Include(l => l.Credential)
                        .ThenInclude(c => c != null ? c.Student : null)
                    .Where(l => l.TripId == tripId)
                    .ToListAsync();

                var report = new
                {
                    Trip = new
                    {
                        trip.TripId,
                        ScheduledStartTime = trip.ScheduledStartTime,
                        ActualStartTime = trip.ActualStartTime,  // ADD THIS
                        StartTime = trip.StartTime,
                        EndTime = trip.EndTime,
                        Status = trip.Status,
                        Route = trip.BusRoute != null ? new { trip.BusRoute.RouteId, trip.BusRoute.RouteName, trip.BusRoute.RouteCode } : null,
                        ServingResidence = trip.Residence != null ? new { trip.Residence.ResidenceId, trip.Residence.ResidenceName } : null
                    },
                    BoardingStats = new
                    {
                        TotalAttempts = logs.Count,
                        Granted = logs.Count(l => l.Allowed == true),
                        Denied = logs.Count(l => l.Allowed == false),
                        CorrectRoute = logs.Count(l => !l.RouteMismatch && l.Allowed == true),
                        RouteMismatch = logs.Count(l => l.RouteMismatch),
                        OfflineSyncs = logs.Count(l => l.IsOffline)
                    },
                    ResultsBreakdown = logs
                        .GroupBy(l => l.Result)
                        .Select(g => new { Result = g.Key, Count = g.Count() }),
                    Students = logs
                        .Where(l => l.Allowed == true)
                        .Select(l => new
                        {
                            StudentId = l.Credential?.StudentId,
                            StudentName = l.Credential?.Student?.FullName,
                            StudentNumber = l.Credential?.Student?.StudentNumber,
                            RouteMismatch = l.RouteMismatch,
                            BoardedAt = l.ClientTimestamp,
                            IsOffline = l.IsOffline
                        })
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trip report");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /api/boarding/stats
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats()
        {
            try
            {
                var query = _context.BoardingLogs.AsQueryable();

                var today = DateTime.UtcNow.Date;
                var stats = new
                {
                    Today = new
                    {
                        Total = await query.CountAsync(l => l.ServerTimestamp.Date == today),
                        Granted = await query.CountAsync(l => l.ServerTimestamp.Date == today && l.Allowed),
                        Denied = await query.CountAsync(l => l.ServerTimestamp.Date == today && !l.Allowed),
                        RouteMismatch = await query.CountAsync(l => l.ServerTimestamp.Date == today && l.RouteMismatch),
                        Offline = await query.CountAsync(l => l.ServerTimestamp.Date == today && l.IsOffline)
                    },
                    AllTime = new
                    {
                        Total = await query.CountAsync(),
                        Granted = await query.CountAsync(l => l.Allowed),
                        Denied = await query.CountAsync(l => !l.Allowed),
                        RouteMismatch = await query.CountAsync(l => l.RouteMismatch),
                        Offline = await query.CountAsync(l => l.IsOffline)
                    },
                    ByResultCode = await query
                        .GroupBy(l => l.Result)
                        .Select(g => new { ResultCode = g.Key, Count = g.Count() })
                        .ToListAsync()
                };
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /api/boarding/events
        [HttpPost("events")]
        public async Task<ActionResult<object>> ReportBoardingEvent([FromBody] BoardingLogRequestDto request)
        {
            try
            {
                var credential = await _context.NFCCredentials
                    .FirstOrDefaultAsync(c => c.CredentialUid == request.Uid);

                var boardingLog = new BoardingLog
                {
                    LogId = Guid.NewGuid(),
                    CredentialId = credential?.CredentialId,
                    TripId = null,
                    DeviceId = request.DeviceId.ToString(),
                    CredentialUid = request.Uid,
                    ClientTimestamp = request.Timestamp,
                    ServerTimestamp = DateTime.UtcNow,
                    Allowed = request.AccessGranted,
                    RouteMismatch = false,
                    Result = request.AccessGranted ? "MANUAL_GRANTED" : "MANUAL_DENIED",
                    Reason = request.Message,
                    IsOffline = false
                };

                _context.BoardingLogs.Add(boardingLog);
                await _context.SaveChangesAsync();

                return Ok(new { Success = true, LogId = boardingLog.LogId, Message = "Boarding event recorded" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting boarding event");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }
    }
}