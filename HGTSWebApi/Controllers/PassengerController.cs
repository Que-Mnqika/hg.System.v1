using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;
using HGTSWebApi.Services;

namespace HGTSWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/passenger")]
    public class PassengerController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PassengerController> _logger;
        private readonly IAuthService _authService;
        private readonly ICredentialService _credentialService;

        public PassengerController(
            AppDbContext context,
            ILogger<PassengerController> logger,
            IAuthService authService,
            ICredentialService credentialService)
        {
            _context = context;
            _logger = logger;
            _authService = authService;
            _credentialService = credentialService;
        }

        // ============ PROFILE ENDPOINTS ============

        /// <summary>
        /// GET /api/passenger/me - Get current passenger info
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<PassengerProfileDto>> GetCurrentPassenger()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userType = User.FindFirst("userType")?.Value;

                if (string.IsNullOrEmpty(userId) || userType != "student")
                    return Unauthorized();

                var studentId = Guid.Parse(userId);
                var student = await _context.Students
                    .Include(s => s.Residence)
                    .Include(s => s.Faculty)
                    .Include(s => s.Institution)
                    .FirstOrDefaultAsync(s => s.StudentId == studentId);

                if (student == null)
                    return NotFound();

                var hasAuth = await _context.StudentAuths.AnyAsync(sa => sa.StudentId == studentId);

                return Ok(new PassengerProfileDto
                {
                    Id = student.StudentId.ToString(),
                    FullName = student.FullName,
                    StudentNumber = student.StudentNumber,
                    Email = student.Email,
                    ResidenceName = student.Residence?.ResidenceName,
                    FacultyName = student.Faculty?.FacultyName,
                    InstitutionName = student.Institution?.InstitutionName,
                    HasCompletedOnboarding = hasAuth
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current passenger");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // ============ ONBOARDING ENDPOINTS ============

        /// <summary>
        /// POST /api/passenger/onboard/verify - First step for new users
        /// </summary>
        [HttpPost("onboard/verify")]
        [AllowAnonymous]
        public async Task<ActionResult<OnboardVerifyResponseDto>> VerifyEmail([FromBody] OnboardVerifyRequestDto request)
        {
            try
            {
                _logger.LogInformation("Verifying email: {Email}", request.Email);

                var student = await _context.Students
                    .Include(s => s.Residence)
                    .Include(s => s.Faculty)
                    .Include(s => s.Institution)
                    .FirstOrDefaultAsync(s => s.Email == request.Email && s.Status == "Active");

                if (student == null)
                {
                    return Ok(new OnboardVerifyResponseDto
                    {
                        Found = false,
                        Reason = "Email not found. Please contact your institution's transport coordinator."
                    });
                }

                var hasAuth = await _context.StudentAuths.AnyAsync(sa => sa.StudentId == student.StudentId);

                return Ok(new OnboardVerifyResponseDto
                {
                    Found = true,
                    IsReturningUser = hasAuth,
                    Profile = new PassengerProfileDto
                    {
                        Id = student.StudentId.ToString(),
                        FullName = student.FullName,
                        StudentNumber = student.StudentNumber,
                        Email = student.Email,
                        ResidenceName = student.Residence?.ResidenceName,
                        FacultyName = student.Faculty?.FacultyName,
                        InstitutionName = student.Institution?.InstitutionName,
                        HasCompletedOnboarding = hasAuth
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// POST /api/passenger/onboard/complete - Complete onboarding (set password)
        /// </summary>
        [HttpPost("onboard/complete")]
        [AllowAnonymous]
        public async Task<ActionResult<OnboardCompleteResponseDto>> CompleteOnboarding([FromBody] OnboardCompleteRequestDto request)
        {
            try
            {
                _logger.LogInformation("Completing onboarding for UserId: {UserId}", request.UserId);

                var student = await _context.Students
                    .Include(s => s.Residence)
                    .Include(s => s.Faculty)
                    .Include(s => s.Institution)
                    .FirstOrDefaultAsync(s => s.StudentId == Guid.Parse(request.UserId));

                if (student == null)
                {
                    return BadRequest(new { error = "Student not found" });
                }

                var existingAuth = await _context.StudentAuths
                    .FirstOrDefaultAsync(sa => sa.StudentId == student.StudentId);

                if (existingAuth != null)
                {
                    var authResult = await _authService.AuthenticateStudentAsync(
                        student.StudentNumber,
                        request.Password,
                        request.DeviceToken);

                    // FIX: Check if authResult is null
                    if (authResult == null)
                        return Unauthorized(new { error = "Invalid password" });

                    return Ok(new OnboardCompleteResponseDto
                    {
                        Success = true,
                        IsReturningUser = true,
                        Token = authResult.Token,
                        ExpiresAt = authResult.ExpiresAt,
                        User = new PassengerProfileDto
                        {
                            Id = student.StudentId.ToString(),
                            FullName = student.FullName,
                            StudentNumber = student.StudentNumber,
                            Email = student.Email,
                            ResidenceName = student.Residence?.ResidenceName,
                            FacultyName = student.Faculty?.FacultyName,
                            InstitutionName = student.Institution?.InstitutionName,
                            HasCompletedOnboarding = true
                        }
                    });
                }

                var studentAuth = new StudentAuth
                {
                    StudentId = student.StudentId,
                    PasswordHash = _authService.HashPassword(request.Password),
                    DeviceToken = request.DeviceToken,
                    IsActive = true,
                    LastLogin = DateTime.UtcNow
                };

                _context.StudentAuths.Add(studentAuth);
                await _context.SaveChangesAsync();

                var authResult2 = await _authService.AuthenticateStudentAsync(
                    student.StudentNumber,
                    request.Password,
                    request.DeviceToken);

                // FIX: Check if authResult2 is null
                if (authResult2 == null)
                    return StatusCode(500, new { error = "Authentication failed after password creation" });

                return Ok(new OnboardCompleteResponseDto
                {
                    Success = true,
                    IsReturningUser = false,
                    Token = authResult2.Token,
                    ExpiresAt = authResult2.ExpiresAt,
                    User = new PassengerProfileDto
                    {
                        Id = student.StudentId.ToString(),
                        FullName = student.FullName,
                        StudentNumber = student.StudentNumber,
                        Email = student.Email,
                        ResidenceName = student.Residence?.ResidenceName,
                        FacultyName = student.Faculty?.FacultyName,
                        InstitutionName = student.Institution?.InstitutionName,
                        HasCompletedOnboarding = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing onboarding");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // ============ CREDENTIAL ENDPOINTS ============

        /// <summary>
        /// GET /api/passenger/{id}/credentials - Get all credentials for student
        /// </summary>
        [HttpGet("{id}/credentials")]
        public async Task<ActionResult<List<CredentialDto>>> GetCredentials(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != id)
                    return Forbid();

                var studentId = Guid.Parse(id);
                var credentials = await _credentialService.GetCredentialsAsync(studentId);

                return Ok(credentials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credentials");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// POST /api/passenger/{id}/credentials/phone/assign - Assign stable phone credential
        /// </summary>
        [HttpPost("{id}/credentials/phone/assign")]
        public async Task<ActionResult<AssignPhoneCredentialResponseDto>> AssignPhoneCredential(
            string id,
            [FromBody] AssignPhoneCredentialRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != id)
                    return Forbid();

                var studentId = Guid.Parse(id);
                var result = await _credentialService.AssignPhoneCredentialAsync(studentId, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning phone credential");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// POST /api/passenger/{id}/credentials/phone/rebind - Rebind phone to new device (same token)
        /// </summary>
        [HttpPost("{id}/credentials/phone/rebind")]
        public async Task<ActionResult<AssignPhoneCredentialResponseDto>> RebindPhoneCredential(
            string id,
            [FromBody] RebindPhoneRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != id)
                    return Forbid();

                var studentId = Guid.Parse(id);
                var result = await _credentialService.RebindPhoneCredentialAsync(studentId, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rebinding phone credential");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// POST /api/passenger/{id}/credentials/card - Add physical card credential
        /// </summary>
        [HttpPost("{id}/credentials/card")]
        public async Task<ActionResult<CredentialDto>> AddCardCredential(
            string id,
            [FromBody] AddCardCredentialRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != id)
                    return Forbid();

                var studentId = Guid.Parse(id);
                var result = await _credentialService.AddCardCredentialAsync(studentId, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding card credential");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// PUT /api/passenger/{id}/credentials/{credentialId}/toggle - Toggle credential active status
        /// </summary>
        [HttpPut("{id}/credentials/{credentialId}/toggle")]
        public async Task<ActionResult> ToggleCredential(string id, Guid credentialId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != id)
                    return Forbid();

                var studentId = Guid.Parse(id);
                var result = await _credentialService.ToggleCredentialAsync(studentId, credentialId);

                if (!result)
                    return NotFound(new { error = "Credential not found" });

                return Ok(new { success = true, message = "Credential toggled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling credential");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// DELETE /api/passenger/{id}/credentials/{credentialId} - Revoke/delete credential
        /// </summary>
        [HttpDelete("{id}/credentials/{credentialId}")]
        public async Task<ActionResult> RevokeCredential(string id, Guid credentialId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != id)
                    return Forbid();

                var studentId = Guid.Parse(id);
                var result = await _credentialService.RevokeCredentialAsync(studentId, credentialId);

                if (!result)
                    return NotFound(new { error = "Credential not found" });

                return Ok(new { success = true, message = "Credential revoked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking credential");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // ============ FEED ENDPOINTS ============

        /// <summary>
        /// GET /api/passenger/{id}/feed - Get active trips
        /// </summary>
        [HttpGet("{id}/feed")]
        public async Task<ActionResult<List<PassengerTripDto>>> GetFeed(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != id)
                    return Forbid();

                var studentId = Guid.Parse(id);

                var student = await _context.Students
                    .Include(s => s.Residence)
                    .FirstOrDefaultAsync(s => s.StudentId == studentId);

                if (student == null)
                    return NotFound();

                var activeTrips = new List<PassengerTripDto>();

                if (student.Residence?.RouteId != null)
                {
                    var trips = await _context.Trips
                        .Include(t => t.BusRoute)
                        .Include(t => t.Vehicle)
                        .Include(t => t.BoardingLogs)
                        .Where(t => t.RouteId == student.Residence.RouteId
                                    && t.Status == "InProgress"
                                    && t.StartTime <= DateTime.UtcNow
                                    && (!t.EndTime.HasValue || t.EndTime > DateTime.UtcNow))
                        .Select(t => new PassengerTripDto
                        {
                            Id = t.TripId.ToString(),
                            Route = t.BusRoute != null ? t.BusRoute.RouteCode : "Unknown",
                            Status = t.Status,
                            Eta = CalculateEta(t.StartTime, t.EndTime),
                            Capacity = GetCapacityBand(t.BoardingLogs.Count, 50),
                            VehicleLabel = t.Vehicle != null ? t.Vehicle.RegistrationNumber : "Unknown"
                        })
                        .ToListAsync();

                    activeTrips.AddRange(trips);
                }

                return Ok(activeTrips);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feed");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// GET /api/passenger/{id}/history - Get boarding history
        /// </summary>
        [HttpGet("{id}/history")]
        public async Task<ActionResult<List<PassengerHistoryDto>>> GetHistory(string id, [FromQuery] int limit = 20)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != id)
                    return Forbid();

                var studentId = Guid.Parse(id);

                // Get all credential IDs for this student
                var credentialTokens = await _context.NFCCredentials
                    .Where(c => c.StudentId == studentId)
                    .Select(c => c.CredentialUid)
                    .ToListAsync();

                if (!credentialTokens.Any())
                {
                    return Ok(new List<PassengerHistoryDto>());
                }

                var history = await _context.BoardingLogs
                    .Include(l => l.Credential)
                    .Include(l => l.Trip)
                        .ThenInclude(t => t != null ? t.Vehicle : null)
                    .Include(l => l.Trip)
                        .ThenInclude(t => t != null ? t.BusRoute : null)
                    .Where(l => l.Credential != null && credentialTokens.Contains(l.Credential.CredentialUid))
                    .OrderByDescending(l => l.ClientTimestamp)
                    .Take(limit)
                    .Select(l => new PassengerHistoryDto
                    {
                        Id = l.LogId.ToString(),
                        Date = l.ClientTimestamp,
                        Route = l.Trip != null && l.Trip.BusRoute != null ? l.Trip.BusRoute.RouteCode : "Unknown",
                        VehicleLabel = l.Trip != null && l.Trip.Vehicle != null ? l.Trip.Vehicle.RegistrationNumber : "Unknown",
                        Status = l.Allowed ? "Success" : "Denied",
                        Method = l.Credential != null ? l.Credential.CredentialType : "Unknown",
                        AccessGranted = l.Allowed,
                        Message = l.Reason
                    })
                    .ToListAsync();

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// GET /api/passenger/hce/token - Get the HCE token for the current student
        /// </summary>
        [HttpGet("hce/token")]
        public async Task<ActionResult<HceTokenResponseDto>> GetHceToken()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userType = User.FindFirst("userType")?.Value;

                if (string.IsNullOrEmpty(userId) || userType != "student")
                    return Unauthorized();

                var studentId = Guid.Parse(userId);

                // Get or create the stable phone token
                var result = await _credentialService.GetOrCreatePhoneTokenAsync(studentId);

                return Ok(new HceTokenResponseDto
                {
                    Success = result.Success,
                    Token = result.CredentialToken,
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting HCE token");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// POST /api/passenger/hce/register-device - Register device info for HCE
        /// </summary>
        [HttpPost("hce/register-device")]
        public async Task<ActionResult<AssignPhoneCredentialResponseDto>> RegisterHceDevice([FromBody] AssignPhoneCredentialRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userType = User.FindFirst("userType")?.Value;

                if (string.IsNullOrEmpty(userId) || userType != "student")
                    return Unauthorized();

                var studentId = Guid.Parse(userId);
                var result = await _credentialService.AssignPhoneCredentialAsync(studentId, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering HCE device");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // ============ BULK IMPORT ENDPOINT ============

        /// <summary>
        /// POST /api/passenger/import - Bulk import passengers from CSV/Excel
        /// </summary>
        [HttpPost("import")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<object>> ImportPassengers([FromBody] List<CreateStudentDto> requests)
        {
            try
            {
                var created = new List<StudentDto>();
                var errors = new List<object>();
                int successCount = 0;

                for (int i = 0; i < requests.Count; i++)
                {
                    var request = requests[i];
                    try
                    {
                        // Check if student already exists
                        var existing = await _context.Students
                            .FirstOrDefaultAsync(s => s.StudentNumber == request.StudentNumber);

                        if (existing != null)
                        {
                            errors.Add(new { Row = i + 1, StudentNumber = request.StudentNumber, Error = "Student number already exists" });
                            continue;
                        }

                        // Check if institution exists
                        var institution = await _context.Institutions.FindAsync(request.InstitutionId);
                        if (institution == null)
                        {
                            errors.Add(new { Row = i + 1, StudentNumber = request.StudentNumber, Error = "Institution not found" });
                            continue;
                        }

                        var student = new Student
                        {
                            StudentId = Guid.NewGuid(),
                            FullName = request.FullName,
                            StudentNumber = request.StudentNumber,
                            Email = request.Email,
                            CellNumber = request.CellNumber,
                            InstitutionId = request.InstitutionId,
                            ResidenceId = request.ResidenceId,
                            FacultyId = request.FacultyId,
                            Status = request.Status ?? "Active"
                        };

                        _context.Students.Add(student);
                        await _context.SaveChangesAsync();

                        created.Add(new StudentDto
                        {
                            StudentId = student.StudentId,
                            FullName = student.FullName,
                            StudentNumber = student.StudentNumber,
                            Email = student.Email,
                            InstitutionId = student.InstitutionId,
                            InstitutionName = institution.InstitutionName,
                            Status = student.Status
                        });

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new { Row = i + 1, StudentNumber = request.StudentNumber, Error = ex.Message });
                    }
                }

                return Ok(new
                {
                    TotalProcessed = requests.Count,
                    SuccessfullyImported = successCount,
                    Failed = requests.Count - successCount,
                    Created = created,
                    Errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing passengers");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // ============ REGISTER NFC FOR PASSENGER ============

        /// <summary>
        /// POST /api/passenger/{passengerId}/register-nfc - Register virtual NFC card for mobile
        /// </summary>
        [HttpPost("{passengerId}/register-nfc")]
        public async Task<ActionResult<AssignPhoneCredentialResponseDto>> RegisterNfcForPassenger(
            Guid passengerId,
            [FromBody] RegisterPhoneCredentialDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != passengerId.ToString())
                    return Forbid();

                var student = await _context.Students.FindAsync(passengerId);
                if (student == null)
                    return NotFound(new { error = "Passenger not found" });

                // Check for existing phone credential
                var existingCredential = await _context.NFCCredentials
                    .FirstOrDefaultAsync(c => c.StudentId == passengerId && c.CredentialType == "PHONE");

                if (existingCredential != null)
                {
                    existingCredential.DeviceIdentifier = request.DeviceToken;
                    existingCredential.DeviceModel = request.Platform;
                    existingCredential.LastSeenAt = DateTime.UtcNow;
                    existingCredential.IsActive = true;

                    await _context.SaveChangesAsync();

                    return Ok(new AssignPhoneCredentialResponseDto
                    {
                        Success = true,
                        CredentialToken = existingCredential.CredentialUid,
                        IsNewCredential = false,
                        Message = "NFC credential updated successfully"
                    });
                }

                // Generate new credential token
                var token = Guid.NewGuid().ToString("N").ToUpper();

                var credential = new NFCCredential
                {
                    CredentialId = Guid.NewGuid(),
                    CredentialUid = token,
                    CredentialType = "PHONE",
                    StudentId = passengerId,
                    IssuedDate = DateTime.UtcNow,
                    IsActive = true,
                    DeviceIdentifier = request.DeviceToken,
                    DeviceModel = request.Platform,
                    LastSeenAt = DateTime.UtcNow
                };

                _context.NFCCredentials.Add(credential);
                await _context.SaveChangesAsync();

                return Ok(new AssignPhoneCredentialResponseDto
                {
                    Success = true,
                    CredentialToken = token,
                    IsNewCredential = true,
                    Message = "NFC credential registered successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering NFC for passenger {PassengerId}", passengerId);
                return StatusCode(500, new AssignPhoneCredentialResponseDto
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        // ============ HELPER METHODS ============

        private string CalculateEta(DateTime? startTime, DateTime? endTime)
        {
            if (endTime.HasValue)
            {
                var minutesLeft = (int)(endTime.Value - DateTime.UtcNow).TotalMinutes;
                if (minutesLeft > 0)
                    return $"{minutesLeft} min";
            }
            return "Arriving";
        }

        private string GetCapacityBand(int boardedCount, int capacity)
        {
            if (capacity <= 0) return "GREEN";
            var percentage = (double)boardedCount / capacity * 100;
            if (percentage >= 80) return "RED";
            if (percentage >= 50) return "YELLOW";
            return "GREEN";
        }
    }
}