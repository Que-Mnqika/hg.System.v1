using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BoardingLogController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BoardingLogController> _logger;

        public BoardingLogController(AppDbContext context, ILogger<BoardingLogController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /boardinglog
        [HttpGet]
        public async Task<ActionResult<BoardingLogResponseDto>> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? uid = null,
            [FromQuery] bool? accessGranted = null,
            [FromQuery] Guid? residenceId = null)   // <-- NEW FILTER
        {
            try
            {
                var query = _context.BoardingLogs
                    .Include(l => l.Credential)
                    .ThenInclude(c => c != null ? c.Student : null)
                        .ThenInclude(s => s != null ? s.Residence : null) // include Residence
                    .Include(l => l.Trip)
                    .AsQueryable();

                // Apply filters
                if (fromDate.HasValue)
                    query = query.Where(l => l.ClientTimestamp >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(l => l.ClientTimestamp <= toDate.Value);

                if (!string.IsNullOrEmpty(uid))
                    query = query.Where(l => l.Credential != null && l.Credential.CredentialUid == uid);

                if (accessGranted.HasValue)
                    query = query.Where(l => l.Allowed == accessGranted.Value);

                // NEW: Filter by residence ID
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
                        CredentialToken = l.Credential != null ? l.Credential.CredentialUid : "Unknown",
                        CredentialType = l.Credential != null ? l.Credential.CredentialType : "Unknown",
                        StudentName = l.Credential != null && l.Credential.Student != null ? l.Credential.Student.FullName : null,
                        StudentNumber = l.Credential != null && l.Credential.Student != null ? l.Credential.Student.StudentNumber : null,
                        DeviceId = 0.ToString(),
                        TripId = l.TripId,
                        ClientTimestamp = l.ClientTimestamp,
                        ServerTimestamp = l.ServerTimestamp,
                        AccessGranted = l.Allowed,
                        Result = l.Result,
                        Message = l.Reason,
                        IsOffline = l.IsOffline,
                        // NEW: Residence fields (add to DTO first)
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

        // GET /boardinglog/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<BoardingLogResponseDto>> GetById(Guid id)
        {
            try
            {
                var log = await _context.BoardingLogs
                    .Include(l => l.Credential)
                    .ThenInclude(c => c != null ? c.Student : null)
                        .ThenInclude(s => s != null ? s.Residence : null)
                    .Include(l => l.Trip)
                    .FirstOrDefaultAsync(l => l.LogId == id);

                if (log == null)
                    return NotFound();

                var dto = new BoardingLogResponseDto
                {
                    LogId = log.LogId,
                    CredentialUid = log.Credential != null ? log.Credential.CredentialUid : "Unknown",
                    CredentialType = log.Credential != null ? log.Credential.CredentialType : "Unknown",
                    StudentName = log.Credential != null && log.Credential.Student != null ? log.Credential.Student.FullName : null,
                    StudentNumber = log.Credential != null && log.Credential.Student != null ? log.Credential.Student.StudentNumber : null,
                    DeviceId = 0,
                    TripId = log.TripId,
                    ClientTimestamp = log.ClientTimestamp,
                    ServerTimestamp = log.ServerTimestamp,
                    AccessGranted = log.Allowed,
                    Result = log.Result,
                    Message = log.Reason,
                    IsOffline = log.IsOffline,
                    // NEW: Residence fields
                    ResidenceId = log.Credential != null && log.Credential.Student != null ? log.Credential.Student.ResidenceId : null,
                    ResidenceName = log.Credential != null && log.Credential.Student != null && log.Credential.Student.Residence != null ? log.Credential.Student.Residence.ResidenceName : null
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting boarding log");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /boardinglog/stats
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats()
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                var stats = new
                {
                    TotalToday = await _context.BoardingLogs
                        .CountAsync(l => l.ServerTimestamp.Date == today),
                    GrantedToday = await _context.BoardingLogs
                        .CountAsync(l => l.ServerTimestamp.Date == today && l.Allowed),
                    DeniedToday = await _context.BoardingLogs
                        .CountAsync(l => l.ServerTimestamp.Date == today && !l.Allowed),
                    TotalAllTime = await _context.BoardingLogs.CountAsync(),
                    OfflineCount = await _context.BoardingLogs
                        .CountAsync(l => l.IsOffline)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting boarding log stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}