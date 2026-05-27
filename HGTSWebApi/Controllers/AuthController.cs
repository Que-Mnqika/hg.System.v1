using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;
using HGTSWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthService _authService;

        public AuthController(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<AuthController> logger,
            IAuthService authService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _authService = authService;
        }

        // POST /api/auth/login - Supports BOTH Admin Portal and Mobile App
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var login = NormalizeLogin(request.Username);

                if (string.IsNullOrEmpty(login))
                {
                    return Unauthorized(new { message = "Invalid username or password" });
                }

                _logger.LogInformation("Login attempt for username: {Username}", login);

                // ================================================================
                // METHOD 1: Check Dashboard Users (Admin Portal)
                // ================================================================
                var dashboardUser = await _context.DashboardUsers
                    .FirstOrDefaultAsync(u =>
                        u.IsActive &&
                        (u.Username.ToLower() == login || u.Email.ToLower() == login));

                if (dashboardUser != null)
                {
                    _logger.LogInformation("Found dashboard user: {Username}", dashboardUser.Username);

                    // Verify password
                    if (!PasswordsMatch(request.Password, dashboardUser.PasswordHash))
                    {
                        _logger.LogWarning("Invalid password for dashboard user: {Username}", login);
                        return Unauthorized(new { message = "Invalid username or password" });
                    }

                    // Update last login
                    dashboardUser.LastLogin = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Generate JWT token for Dashboard User
                    var token = GenerateJwtTokenForDashboardUser(dashboardUser);

                    return Ok(new LoginResponseDto
                    {
                        Success = true,
                        Token = token,
                        RefreshToken = dashboardUser.RefreshToken,
                        UserId = dashboardUser.UserId.ToString(),
                        Username = dashboardUser.Username,
                        Email = dashboardUser.Email ?? "",
                        FullName = dashboardUser.FullName ?? "",
                        Role = dashboardUser.Role ?? "Admin",
                        UserCategory = dashboardUser.UserCategory ?? "ADMIN",
                        IsAdminPortal = true
                    });
                }

                // ================================================================
                // METHOD 2: Check Student Auth (Mobile App)
                // ================================================================
                var studentAuth = await _context.StudentAuths
                    .Include(sa => sa.Student)
                    .FirstOrDefaultAsync(sa =>
                        sa.IsActive &&
                        sa.Username != null &&
                        sa.Username.ToLower() == login);

                if (studentAuth != null && studentAuth.Student != null)
                {
                    _logger.LogInformation("Found student user: {Username}", studentAuth.Username);

                    // Verify password
                    if (!PasswordsMatch(request.Password, studentAuth.PasswordHash))
                    {
                        _logger.LogWarning("Invalid password for student: {Username}", login);
                        return Unauthorized(new { message = "Invalid username or password" });
                    }

                    // Update last login
                    studentAuth.LastLogin = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Generate JWT token for Student
                    var token = GenerateJwtTokenForStudent(studentAuth);

                    return Ok(new LoginResponseDto
                    {
                        Success = true,
                        Token = token,
                        UserId = studentAuth.StudentId.ToString(),
                        Username = studentAuth.Username,
                        Email = studentAuth.Student.Email ?? "",
                        FullName = studentAuth.Student.FullName,
                        StudentNumber = studentAuth.Student.StudentNumber,
                        Role = "Student",
                        UserCategory = "STUDENT",
                        IsAdminPortal = false
                    });
                }

                // Log that no user was found
                _logger.LogWarning("No user found for username: {Username}", login);

                // No user found in either table
                return Unauthorized(new { message = "Invalid username or password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<CurrentUserDto>> GetCurrentUserMe()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                var authType = User.FindFirst("AuthType")?.Value;

                if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
                {
                    return Unauthorized(new { message = "Invalid user context" });
                }

                if (authType == "Dashboard")
                {
                    var user = await _context.DashboardUsers
                        //.Include(u => u.Organization)
                        .FirstOrDefaultAsync(u => u.UserId == parsedUserId && u.IsActive);

                    if (user == null)
                    {
                        return NotFound(new { message = "User not found" });
                    }

                    return Ok(new CurrentUserDto
                    {
                        UserId = user.UserId.ToString(),
                        Username = user.Username,
                        Email = user.Email ?? string.Empty,
                        FullName = user.FullName ?? string.Empty,
                        Role = user.Role ?? "Admin",
                        UserCategory = user.UserCategory ?? "ADMIN",
                        //OrganizationId = user.OrganizationId?.ToString(),
                        //OrganizationName = user.Organization?.Name,
                        PermissionLevel = "READ_WRITE",
                        IsAdminPortal = true
                    });
                }

                if (authType == "Student")
                {
                    var studentAuth = await _context.StudentAuths
                        .Include(sa => sa.Student)
                        .FirstOrDefaultAsync(sa => sa.StudentId == parsedUserId && sa.IsActive);

                    if (studentAuth?.Student == null)
                    {
                        return NotFound(new { message = "User not found" });
                    }

                    return Ok(new CurrentUserDto
                    {
                        UserId = studentAuth.StudentId.ToString(),
                        Username = studentAuth.Username ?? string.Empty,
                        Email = studentAuth.Student.Email ?? string.Empty,
                        FullName = studentAuth.Student.FullName,
                        Role = "Student",
                        UserCategory = "STUDENT",
                        OrganizationId = studentAuth.Student.InstitutionId.ToString(),
                        PermissionLevel = "READ_WRITE",
                        IsAdminPortal = false
                    });
                }

                return BadRequest(new { message = "Invalid auth type" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logged out successfully" });
        }

        // POST /api/auth/register-student - Register a student for mobile app
        [HttpPost("register-student")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterStudent([FromBody] RegisterStudentRequestDto request)
        {
            try
            {
                // Check if student exists
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentNumber == request.StudentNumber);

                if (student == null)
                {
                    return BadRequest(new { message = "Student not found. Please contact your institution." });
                }

                // Check if already registered
                var existingAuth = await _context.StudentAuths
                    .FirstOrDefaultAsync(sa => sa.StudentId == student.StudentId);

                if (existingAuth != null)
                {
                    return BadRequest(new { message = "Student already registered. Please login." });
                }

                // Create username (use provided or generate from student number)
                string username = !string.IsNullOrEmpty(request.Username)
                    ? request.Username
                    : student.StudentNumber;

                // Create student auth
                var studentAuth = new StudentAuth
                {
                    AuthId = Guid.NewGuid(),
                    StudentId = student.StudentId,
                    Username = username,
                    PasswordHash = _authService.HashPassword(request.Password), // HASH THE PASSWORD!
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.StudentAuths.Add(studentAuth);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Student registered successfully. You can now login." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering student");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST /api/auth/change-password - Change password for authenticated user
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                var authType = User.FindFirst("AuthType")?.Value;

                if (authType == "Dashboard")
                {
                    var user = await _context.DashboardUsers
                        .FirstOrDefaultAsync(u => u.UserId.ToString() == userId);

                    if (user == null)
                        return NotFound(new { message = "User not found" });

                    if (!PasswordsMatch(request.CurrentPassword, user.PasswordHash))
                        return BadRequest(new { message = "Current password is incorrect" });

                    user.PasswordHash = _authService.HashPassword(request.NewPassword);
                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Password changed successfully" });
                }
                else if (authType == "Student")
                {
                    var studentAuth = await _context.StudentAuths
                        .FirstOrDefaultAsync(sa => sa.StudentId.ToString() == userId);

                    if (studentAuth == null)
                        return NotFound(new { message = "Student not found" });

                    if (!PasswordsMatch(request.CurrentPassword, studentAuth.PasswordHash))
                        return BadRequest(new { message = "Current password is incorrect" });

                    studentAuth.PasswordHash = _authService.HashPassword(request.NewPassword);
                    studentAuth.PasswordUpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Password changed successfully" });
                }

                return BadRequest(new { message = "Invalid user type" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // ================================================================
        // PRIVATE METHODS FOR JWT TOKEN GENERATION
        // ================================================================

        private string GenerateJwtTokenForDashboardUser(DashboardUser user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "1Q2W3E4R5T6Y7U8I9O0PAZSXDCFVGBHN"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UserId", user.UserId.ToString()),
                new Claim("Username", user.Username),
                new Claim("FullName", user.FullName ?? ""),
                new Claim("Role", user.Role ?? "User"),
                new Claim("UserCategory", user.UserCategory ?? "USER"),
                //new Claim("OrganizationId", user.OrganizationId?.ToString() ?? ""),
                new Claim("AuthType", "Dashboard")
            };

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateJwtTokenForStudent(StudentAuth studentAuth)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "1Q2W3E4R5T6Y7U8I9O0PAZSXDCFVGBHN"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var student = studentAuth.Student;
            var email = student?.Email ?? "";
            var fullName = student?.FullName ?? "";
            var studentNumber = student?.StudentNumber ?? "";
            var institutionId = student?.InstitutionId.ToString() ?? "";

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, studentAuth.StudentId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UserId", studentAuth.StudentId.ToString()),
                new Claim("Username", studentAuth.Username ?? ""),
                new Claim("FullName", fullName),
                new Claim("StudentNumber", studentNumber),
                new Claim("Role", "Student"),
                new Claim("UserCategory", "STUDENT"),
                new Claim("InstitutionId", institutionId),
                new Claim("AuthType", "Student")
            };

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool PasswordsMatch(string password, string storedPasswordHash)
        {
            // First try the proper hash verification
            if (_authService.VerifyPassword(password, storedPasswordHash))
            {
                return true;
            }

            // Backwards compatibility for older records still storing plain-text passwords
            if (string.Equals(storedPasswordHash, password, StringComparison.Ordinal))
            {
                _logger.LogWarning("User logged in with plain-text password. Consider rehashing.");
                return true;
            }

            return false;
        }

        private static string NormalizeLogin(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return string.Empty;

            return username.Trim().ToLowerInvariant();
        }
    }
}