using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;

namespace HGTSWebApi.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/devices")]
    public class DeviceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(AppDbContext context, ILogger<DeviceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/devices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DeviceDto>>> GetAll(
            [FromQuery] string? orgId,
            [FromQuery] string? status)
        {
            try
            {
                var query = _context.Devices.AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    var statusLower = status.ToLower();
                    if (statusLower == "online")
                        query = query.Where(d => d.IsOnline == true && d.Status == "Online");
                    else if (statusLower == "offline")
                        query = query.Where(d => d.IsOnline == false || d.Status == "Offline");
                    else if (statusLower == "maintenance")
                        query = query.Where(d => d.Status == "Maintenance");
                }

                var devices = await query
                    .OrderBy(d => d.DeviceIdentifier)
                    .Select(d => new DeviceDto
                    {
                        DeviceId = d.DeviceId,
                        DeviceIdentifier = d.DeviceIdentifier ?? d.DeviceId.ToString(),
                        DeviceName = d.DeviceName ?? "Unnamed Device",
                        FirmwareVersion = d.FirmwareVersion,
                        HardwareVersion = d.HardwareVersion,
                        LastSeen = d.LastSeen,
                        IsOnline = d.IsOnline,
                        Status = d.Status ?? (d.IsOnline ? "Online" : "Offline"),
                        RegisteredDate = d.RegisteredDate,
                        Location = d.Location,
                        Description = d.Description,
                        ActiveTripCount = _context.Trips.Count(t => t.DeviceId == d.DeviceId && (t.Status == "Active" || t.Status == "InProgress")),
                        TotalBoardingCount = _context.BoardingLogs.Count(l => l.DeviceId == d.DeviceId.ToString()),
                        TripDurationHours = d.TripDurationHours
                    })
                    .ToListAsync();

                return Ok(devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting devices");
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // GET /api/devices/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DeviceDto>> GetById(Guid id)
        {
            try
            {
                var device = await _context.Devices
                    .FirstOrDefaultAsync(d => d.DeviceId == id);

                if (device == null)
                    return NotFound();

                return Ok(new DeviceDto
                {
                    DeviceId = device.DeviceId,
                    DeviceIdentifier = device.DeviceIdentifier ?? device.DeviceId.ToString(),
                    DeviceName = device.DeviceName ?? "Unnamed Device",
                    FirmwareVersion = device.FirmwareVersion,
                    HardwareVersion = device.HardwareVersion,
                    LastSeen = device.LastSeen,
                    IsOnline = device.IsOnline,
                    Status = device.Status ?? (device.IsOnline ? "Online" : "Offline"),
                    RegisteredDate = device.RegisteredDate,
                    Location = device.Location,
                    Description = device.Description,
                    ActiveTripCount = await _context.Trips.CountAsync(t => t.DeviceId == id && (t.Status == "Active" || t.Status == "InProgress")),
                    TotalBoardingCount = await _context.BoardingLogs.CountAsync(l => l.DeviceId == id.ToString()),
                    TripDurationHours = device.TripDurationHours
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device {Id}", id);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // POST /api/devices
        [HttpPost]
        public async Task<ActionResult<DeviceDto>> Create([FromBody] CreateDeviceDto dto)
        {
            try
            {
                // Check if device identifier already exists
                var existing = await _context.Devices
                    .FirstOrDefaultAsync(d => d.DeviceIdentifier == dto.DeviceIdentifier);

                if (existing != null)
                    return BadRequest(new { error = "Device identifier already exists" });

                var device = new Device
                {
                    DeviceId = Guid.NewGuid(),
                    DeviceIdentifier = dto.DeviceIdentifier,
                    DeviceName = dto.DeviceName ?? "Unnamed Device",
                    FirmwareVersion = dto.FirmwareVersion,
                    HardwareVersion = dto.HardwareVersion,
                    Location = dto.Location,
                    Description = dto.Description,
                    LastSeen = DateTime.UtcNow,
                    IsOnline = true,
                    Status = "Online",
                    RegisteredDate = DateTime.UtcNow,
                    TripDurationHours = dto.TripDurationHours ?? 6
                };

                _context.Devices.Add(device);
                await _context.SaveChangesAsync();

                return Ok(new DeviceDto
                {
                    DeviceId = device.DeviceId,
                    DeviceIdentifier = device.DeviceIdentifier,
                    DeviceName = device.DeviceName,
                    FirmwareVersion = device.FirmwareVersion,
                    HardwareVersion = device.HardwareVersion,
                    LastSeen = device.LastSeen,
                    IsOnline = device.IsOnline,
                    Status = device.Status,
                    RegisteredDate = device.RegisteredDate,
                    Location = device.Location,
                    Description = device.Description,
                    ActiveTripCount = 0,
                    TotalBoardingCount = 0,
                    TripDurationHours = device.TripDurationHours
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating device");
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // PUT /api/devices/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDeviceDto dto)
        {
            try
            {
                var device = await _context.Devices.FindAsync(id);
                if (device == null)
                    return NotFound();

                // Check if device identifier is being changed and if it already exists
                if (!string.IsNullOrEmpty(dto.DeviceIdentifier) && dto.DeviceIdentifier != device.DeviceIdentifier)
                {
                    var existing = await _context.Devices
                        .FirstOrDefaultAsync(d => d.DeviceIdentifier == dto.DeviceIdentifier && d.DeviceId != id);

                    if (existing != null)
                        return BadRequest(new { error = "Device identifier already exists" });

                    device.DeviceIdentifier = dto.DeviceIdentifier;
                }

                // Update device name
                if (!string.IsNullOrEmpty(dto.DeviceName))
                    device.DeviceName = dto.DeviceName;

                // Update location
                if (dto.Location != null)
                    device.Location = dto.Location;

                // Update description
                if (dto.Description != null)
                    device.Description = dto.Description;

                // Update firmware version
                if (!string.IsNullOrEmpty(dto.FirmwareVersion))
                    device.FirmwareVersion = dto.FirmwareVersion;

                // Update hardware version
                if (!string.IsNullOrEmpty(dto.HardwareVersion))
                    device.HardwareVersion = dto.HardwareVersion;

                // Update status
                if (!string.IsNullOrEmpty(dto.Status))
                    device.Status = dto.Status;

                // Update trip duration hours
                if (dto.TripDurationHours.HasValue)
                {
                    if (dto.TripDurationHours.Value < 1 || dto.TripDurationHours.Value > 24)
                        return BadRequest(new { error = "Trip duration must be between 1 and 24 hours" });

                    device.TripDurationHours = dto.TripDurationHours.Value;
                    device.LastConfigUpdate = DateTime.UtcNow;
                }

                device.LastSeen = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Device updated successfully",
                    deviceId = device.DeviceId,
                    deviceIdentifier = device.DeviceIdentifier,
                    deviceName = device.DeviceName,
                    location = device.Location
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device {Id}", id);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // PATCH /api/devices/{id} - Partial update (alternative to PUT)
        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> PartialUpdate(Guid id, [FromBody] UpdateDeviceDto dto)
        {
            try
            {
                var device = await _context.Devices.FindAsync(id);
                if (device == null)
                    return NotFound();

                // Check if device identifier is being changed and if it already exists
                if (!string.IsNullOrEmpty(dto.DeviceIdentifier) && dto.DeviceIdentifier != device.DeviceIdentifier)
                {
                    var existing = await _context.Devices
                        .FirstOrDefaultAsync(d => d.DeviceIdentifier == dto.DeviceIdentifier && d.DeviceId != id);

                    if (existing != null)
                        return BadRequest(new { error = "Device identifier already exists" });

                    device.DeviceIdentifier = dto.DeviceIdentifier;
                }

                // Update device name (only if provided)
                if (!string.IsNullOrEmpty(dto.DeviceName))
                    device.DeviceName = dto.DeviceName;

                // Update location (only if provided)
                if (dto.Location != null)
                    device.Location = dto.Location;

                // Update description (only if provided)
                if (dto.Description != null)
                    device.Description = dto.Description;

                // Update firmware version (only if provided)
                if (!string.IsNullOrEmpty(dto.FirmwareVersion))
                    device.FirmwareVersion = dto.FirmwareVersion;

                // Update hardware version (only if provided)
                if (!string.IsNullOrEmpty(dto.HardwareVersion))
                    device.HardwareVersion = dto.HardwareVersion;

                // Update status (only if provided)
                if (!string.IsNullOrEmpty(dto.Status))
                    device.Status = dto.Status;

                // Update trip duration hours (only if provided)
                if (dto.TripDurationHours.HasValue)
                {
                    if (dto.TripDurationHours.Value < 1 || dto.TripDurationHours.Value > 24)
                        return BadRequest(new { error = "Trip duration must be between 1 and 24 hours" });

                    device.TripDurationHours = dto.TripDurationHours.Value;
                    device.LastConfigUpdate = DateTime.UtcNow;
                }

                device.LastSeen = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Device updated successfully",
                    deviceId = device.DeviceId,
                    deviceIdentifier = device.DeviceIdentifier,
                    deviceName = device.DeviceName,
                    location = device.Location,
                    status = device.Status,
                    tripDurationHours = device.TripDurationHours
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error partially updating device {Id}", id);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // DELETE /api/devices/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var device = await _context.Devices
                    .Include(d => d.Trips)
                    .FirstOrDefaultAsync(d => d.DeviceId == id);

                if (device == null)
                    return NotFound();

                // Check if device has any associated trips
                if (device.Trips != null && device.Trips.Any())
                    return BadRequest(new { error = "Cannot delete device with existing trips. Please reassign trips first." });

                _context.Devices.Remove(device);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Device deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting device {Id}", id);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // POST /api/devices/register (Keep existing)
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<DeviceDto>> RegisterDevice([FromBody] CreateDeviceDto dto)
        {
            try
            {
                var existing = await _context.Devices
                    .FirstOrDefaultAsync(d => d.DeviceIdentifier == dto.DeviceIdentifier);

                if (existing != null)
                {
                    existing.LastSeen = DateTime.UtcNow;
                    existing.IsOnline = true;
                    existing.Status = "Online";

                    if (!string.IsNullOrEmpty(dto.DeviceName)) existing.DeviceName = dto.DeviceName;
                    if (!string.IsNullOrEmpty(dto.FirmwareVersion)) existing.FirmwareVersion = dto.FirmwareVersion;
                    if (!string.IsNullOrEmpty(dto.HardwareVersion)) existing.HardwareVersion = dto.HardwareVersion;
                    if (!string.IsNullOrEmpty(dto.Location)) existing.Location = dto.Location;

                    await _context.SaveChangesAsync();

                    return Ok(new DeviceDto
                    {
                        DeviceId = existing.DeviceId,
                        DeviceIdentifier = existing.DeviceIdentifier,
                        DeviceName = existing.DeviceName,
                        FirmwareVersion = existing.FirmwareVersion,
                        HardwareVersion = existing.HardwareVersion,
                        LastSeen = existing.LastSeen,
                        IsOnline = existing.IsOnline,
                        Status = existing.Status,
                        RegisteredDate = existing.RegisteredDate,
                        Location = existing.Location,
                        Description = existing.Description
                    });
                }

                var device = new Device
                {
                    DeviceId = Guid.NewGuid(),
                    DeviceIdentifier = dto.DeviceIdentifier,
                    DeviceName = dto.DeviceName,
                    FirmwareVersion = dto.FirmwareVersion,
                    HardwareVersion = dto.HardwareVersion,
                    Location = dto.Location,
                    Description = dto.Description,
                    LastSeen = DateTime.UtcNow,
                    IsOnline = true,
                    Status = "Online",
                    RegisteredDate = DateTime.UtcNow
                };

                _context.Devices.Add(device);
                await _context.SaveChangesAsync();

                return Ok(new DeviceDto
                {
                    DeviceId = device.DeviceId,
                    DeviceIdentifier = device.DeviceIdentifier,
                    DeviceName = device.DeviceName,
                    FirmwareVersion = device.FirmwareVersion,
                    HardwareVersion = device.HardwareVersion,
                    LastSeen = device.LastSeen,
                    IsOnline = device.IsOnline,
                    Status = device.Status,
                    RegisteredDate = device.RegisteredDate,
                    Location = device.Location,
                    Description = device.Description
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /api/devices/{id}/active-trip (Keep existing)
        [HttpGet("{id}/active-trip")]
        [AllowAnonymous]
        public async Task<ActionResult<ActiveTripResponseDto>> GetActiveTripForDevice(string id)
        {
            try
            {
                Device device = null;

                if (Guid.TryParse(id, out Guid deviceGuid))
                {
                    device = await _context.Devices.FindAsync(deviceGuid);
                }

                if (device == null)
                {
                    device = await _context.Devices
                        .FirstOrDefaultAsync(d => d.DeviceIdentifier == id);
                }

                if (device == null)
                {
                    return Ok(new ActiveTripResponseDto
                    {
                        HasActiveTrip = false,
                        Message = "Device not found"
                    });
                }

                var now = DateTime.UtcNow;

                var activeTrip = await _context.Trips
                    .Include(t => t.BusRoute)
                    .Include(t => t.Vehicle)
                    .Include(t => t.Residence)
                    .FirstOrDefaultAsync(t => t.DeviceId == device.DeviceId
                        && (t.Status == "Active" || t.Status == "InProgress")
                        && t.Status != "Completed"
                        && t.Status != "Cancelled");

                if (activeTrip == null)
                {
                    _logger.LogInformation("No Active or InProgress trip found for device {DeviceId}", device.DeviceId);
                    return Ok(new ActiveTripResponseDto
                    {
                        HasActiveTrip = false,
                        Message = "No active trip for this device"
                    });
                }

                _logger.LogInformation("Found active trip {TripId} with status {Status} for device {DeviceId}",
                    activeTrip.TripId, activeTrip.Status, device.DeviceId);

                // Calculate duration
                int durationMinutes = 30;
                if (activeTrip.ScheduledStartTime.HasValue && activeTrip.ScheduledEndTime.HasValue)
                {
                    durationMinutes = (int)(activeTrip.ScheduledEndTime.Value - activeTrip.ScheduledStartTime.Value).TotalMinutes;
                }

                if (durationMinutes <= 0) durationMinutes = 30;

                // Get vehicle capacity
                int vehicleCapacity = activeTrip.Vehicle?.Capacity ?? 50;

                // Use ActualStartTime if available, otherwise use StartTime
                DateTime? effectiveStartTime = activeTrip.ActualStartTime ?? activeTrip.StartTime;

                return Ok(new ActiveTripResponseDto
                {
                    HasActiveTrip = true,
                    TripId = activeTrip.TripId.ToString(),
                    RouteId = activeTrip.RouteId.ToString(),
                    RouteName = activeTrip.BusRoute?.RouteName ?? "Unknown Route",
                    ResidenceId = activeTrip.ResidenceId?.ToString(),
                    ResidenceName = activeTrip.Residence?.ResidenceName ?? "Unknown Residence",
                    DurationHours = durationMinutes / 60,
                    DurationMinutes = durationMinutes,
                    VehicleCapacity = vehicleCapacity,
                    ScheduledEndTime = activeTrip.ScheduledEndTime,
                    StartTime = effectiveStartTime,
                    EndTime = activeTrip.EndTime,
                    Message = $"Active trip found - Status: {activeTrip.Status}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active trip for device {DeviceId}", id);
                return StatusCode(500, new ActiveTripResponseDto
                {
                    HasActiveTrip = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        // POST /api/devices/{id}/telemetry (Keep existing)
        [HttpPost("{id:guid}/telemetry")]
        [AllowAnonymous]
        public async Task<IActionResult> Telemetry(Guid id, [FromBody] TelemetryRequestDto request)
        {
            try
            {
                var device = await _context.Devices.FindAsync(id);
                if (device == null)
                {
                    device = new Device
                    {
                        DeviceId = id,
                        DeviceIdentifier = id.ToString(),
                        LastSeen = DateTime.UtcNow,
                        IsOnline = true,
                        Status = "Online",
                        RegisteredDate = DateTime.UtcNow
                    };
                    _context.Devices.Add(device);
                }
                else
                {
                    device.LastSeen = DateTime.UtcNow;
                    device.IsOnline = true;
                    device.Status = "Online";

                    if (!string.IsNullOrEmpty(request.FirmwareVersion))
                        device.FirmwareVersion = request.FirmwareVersion;
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Telemetry received" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in telemetry");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /api/devices/{id}/offline-sync/single (Keep existing)
        [HttpPost("{id:guid}/offline-sync/single")]
        [AllowAnonymous]
        public async Task<ActionResult<OfflineSyncResponseDto>> OfflineSyncSingle(
            Guid id, [FromBody] OfflineTapDto tap)
        {
            try
            {
                var device = await _context.Devices.FindAsync(id);
                if (device == null)
                {
                    device = new Device
                    {
                        DeviceId = id,
                        DeviceIdentifier = id.ToString(),
                        LastSeen = DateTime.UtcNow,
                        IsOnline = true,
                        Status = "Online",
                        RegisteredDate = DateTime.UtcNow
                    };
                    _context.Devices.Add(device);
                    await _context.SaveChangesAsync();
                }

                var credential = await _context.NFCCredentials
                    .FirstOrDefaultAsync(c => c.CredentialUid == tap.Uid);

                DateTime clientTimestamp;
                if (long.TryParse(tap.TapTime, out long milliseconds))
                    clientTimestamp = DateTime.UtcNow.AddMilliseconds(milliseconds - Environment.TickCount);
                else if (!DateTime.TryParse(tap.TapTime, out clientTimestamp))
                {
                    clientTimestamp = DateTime.UtcNow;
                    _logger.LogWarning("Failed to parse tap time '{TapTime}', using UTC now", tap.TapTime);
                }

                var boardingLog = new BoardingLog
                {
                    LogId = Guid.NewGuid(),
                    CredentialId = credential?.CredentialId,
                    DeviceId = device.DeviceId.ToString(),
                    CredentialUid = tap.Uid,
                    ClientTimestamp = clientTimestamp,
                    ServerTimestamp = DateTime.UtcNow,
                    Allowed = tap.AccessGranted,
                    Result = tap.AccessGranted ? "Success" : "Denied",
                    Reason = "Offline sync - single",
                    IsOffline = true
                };

                _context.BoardingLogs.Add(boardingLog);
                await _context.SaveChangesAsync();

                return Ok(new OfflineSyncResponseDto
                {
                    SyncedCount = 1,
                    Message = "Successfully synced 1 offline tap"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in single offline sync");
                return StatusCode(500, new OfflineSyncResponseDto
                {
                    SyncedCount = 0,
                    Message = $"Error during sync: {ex.Message}"
                });
            }
        }

        // POST /api/devices/{id}/offline-sync/bulk (Keep existing)
        [HttpPost("{id:guid}/offline-sync/bulk")]
        [AllowAnonymous]
        public async Task<ActionResult<OfflineSyncResponseDto>> OfflineSyncBulk(
            Guid id, [FromBody] OfflineSyncRequestDto request)
        {
            try
            {
                _logger.LogInformation("Bulk offline sync - Device: {DeviceId}, Taps: {Count}",
                    id, request.Taps?.Count ?? 0);

                if (request.Taps == null || request.Taps.Count == 0)
                    return Ok(new OfflineSyncResponseDto { SyncedCount = 0, Message = "No taps to sync" });

                var device = await _context.Devices.FindAsync(id);
                if (device == null)
                {
                    device = new Device
                    {
                        DeviceId = id,
                        DeviceIdentifier = id.ToString(),
                        LastSeen = DateTime.UtcNow,
                        IsOnline = true,
                        Status = "Online",
                        RegisteredDate = DateTime.UtcNow
                    };
                    _context.Devices.Add(device);
                    await _context.SaveChangesAsync();
                }

                var uids = request.Taps.Select(t => t.Uid).ToList();
                var credentials = await _context.NFCCredentials
                    .Where(c => uids.Contains(c.CredentialUid))
                    .ToDictionaryAsync(c => c.CredentialUid, c => c.CredentialId);

                var logs = new List<BoardingLog>();
                foreach (var tap in request.Taps)
                {
                    DateTime clientTimestamp;
                    if (long.TryParse(tap.TapTime, out long milliseconds))
                        clientTimestamp = DateTime.UtcNow.AddMilliseconds(milliseconds - Environment.TickCount);
                    else if (!DateTime.TryParse(tap.TapTime, out clientTimestamp))
                    {
                        clientTimestamp = DateTime.UtcNow;
                        _logger.LogWarning("Failed to parse tap time '{TapTime}'", tap.TapTime);
                    }

                    logs.Add(new BoardingLog
                    {
                        LogId = Guid.NewGuid(),
                        CredentialId = credentials.GetValueOrDefault(tap.Uid),
                        DeviceId = device.DeviceId.ToString(),
                        CredentialUid = tap.Uid,
                        ClientTimestamp = clientTimestamp,
                        ServerTimestamp = DateTime.UtcNow,
                        Allowed = tap.AccessGranted,
                        Result = tap.AccessGranted ? "Success" : "Denied",
                        Reason = "Offline sync - bulk",
                        IsOffline = true
                    });
                }

                _context.BoardingLogs.AddRange(logs);
                await _context.SaveChangesAsync();

                return Ok(new OfflineSyncResponseDto
                {
                    SyncedCount = logs.Count,
                    Message = $"Successfully synced {logs.Count} offline taps"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk offline sync");
                return StatusCode(500, new OfflineSyncResponseDto
                {
                    SyncedCount = 0,
                    Message = $"Error during sync: {ex.Message}"
                });
            }
        }

        // GET /api/devices/{id}/config (Keep existing)
        [HttpGet("{id:guid}/config")]
        [AllowAnonymous]
        public async Task<ActionResult<TripConfigDto>> GetDeviceConfig(Guid id)
        {
            try
            {
                var device = await _context.Devices.FindAsync(id);
                if (device == null)
                    return NotFound(new { error = "Device not found" });

                var config = new TripConfigDto
                {
                    TripDurationHours = device.TripDurationHours,
                    LastUpdated = device.LastConfigUpdate ?? device.RegisteredDate,
                    IsOnline = device.IsOnline,
                    DeviceName = device.DeviceName
                };

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device config for {DeviceId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PUT /api/devices/{id}/config (Keep existing)
        [HttpPut("{id:guid}/config")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateDeviceConfig(
            Guid id, [FromBody] UpdateTripConfigDto config)
        {
            try
            {
                var device = await _context.Devices.FindAsync(id);
                if (device == null)
                    return NotFound(new { error = "Device not found" });

                if (config.TripDurationHours < 1 || config.TripDurationHours > 24)
                    return BadRequest(new { error = "Trip duration must be between 1 and 24 hours" });

                device.TripDurationHours = config.TripDurationHours;
                device.LastConfigUpdate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Device configuration updated successfully",
                    tripDurationHours = device.TripDurationHours,
                    lastUpdated = device.LastConfigUpdate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device config for {DeviceId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /api/devices/{id}/config/refresh (Keep existing)
        [HttpPost("{id:guid}/config/refresh")]
        public async Task<IActionResult> ForceConfigRefresh(Guid id)
        {
            try
            {
                var device = await _context.Devices.FindAsync(id);
                if (device == null)
                    return NotFound(new { error = "Device not found" });

                device.LastConfigUpdate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Device will refresh configuration on next check" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forcing config refresh for {DeviceId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /api/devices/nfc — Register a new ESP32 NFC reader device (Keep existing)
        [HttpPost("nfc")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<DeviceDto>> RegisterNfcDevice(
            [FromBody] CreateDeviceDto request)
        {
            try
            {
                var existing = await _context.Devices
                    .FirstOrDefaultAsync(d => d.DeviceIdentifier == request.DeviceIdentifier);

                if (existing != null)
                    return BadRequest(new { error = "Device identifier already exists" });

                var device = new Device
                {
                    DeviceId = Guid.NewGuid(),
                    DeviceIdentifier = request.DeviceIdentifier,
                    DeviceName = request.DeviceName,
                    FirmwareVersion = request.FirmwareVersion,
                    HardwareVersion = request.HardwareVersion,
                    Location = request.Location,
                    Description = request.Description,
                    TripDurationHours = request.TripDurationHours ?? 6,
                    Status = "Online",
                    IsOnline = true,
                    RegisteredDate = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow
                };

                _context.Devices.Add(device);
                await _context.SaveChangesAsync();

                return Ok(new DeviceDto
                {
                    DeviceId = device.DeviceId,
                    DeviceIdentifier = device.DeviceIdentifier,
                    DeviceName = device.DeviceName,
                    FirmwareVersion = device.FirmwareVersion,
                    HardwareVersion = device.HardwareVersion,
                    LastSeen = device.LastSeen,
                    IsOnline = device.IsOnline,
                    Status = device.Status,
                    RegisteredDate = device.RegisteredDate,
                    Location = device.Location,
                    Description = device.Description,
                    TripDurationHours = device.TripDurationHours
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering NFC device");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}