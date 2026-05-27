using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;
using HGTSWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HGTSWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AppDbContext context, IAuthService authService, ILogger<UsersController> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        // GET /api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DashboardUserDto>>> GetUsers(
            [FromQuery] string? orgId,
            [FromQuery] string? q,
            [FromQuery] string? userCategory,
            [FromQuery] string? status)
        {
            try
            {
                var query = _context.DashboardUsers
                    //.Include(u => u.Organization)
                    .AsQueryable();

                //if (!string.IsNullOrEmpty(orgId) && Guid.TryParse(orgId, out var orgGuid))
                //    query = query.Where(u => u.OrganizationId == orgGuid);

                if (!string.IsNullOrEmpty(q))
                    query = query.Where(u => u.FullName.Contains(q) || u.Email.Contains(q));

                if (!string.IsNullOrEmpty(userCategory))
                    query = query.Where(u => u.UserCategory == userCategory);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(u => (status == "Active") ? u.IsActive : !u.IsActive);

                var users = await query
                    .OrderBy(u => u.FullName)
                    .Select(u => new DashboardUserDto
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        Email = u.Email ?? "",
                        FullName = u.FullName ?? "",
                        Role = u.Role ?? "",
                        UserCategory = u.UserCategory ?? "",
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        LastLogin = u.LastLogin,
                        //OrganizationId = u.OrganizationId == null ? null : u.OrganizationId.ToString(),
                        //OrganizationName = u.Organization == null ? null : u.Organization.Name,
                        Department = u.Department,
                        PhoneNumber = u.PhoneNumber
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /api/users/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DashboardUserDto>> GetUser(Guid id)
        {
            try
            {
                var user = await _context.DashboardUsers
                    //.Include(u => u.Organization)
                    .FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                    return NotFound();

                return Ok(new DashboardUserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email ?? "",
                    FullName = user.FullName ?? "",
                    Role = user.Role ?? "",
                    UserCategory = user.UserCategory ?? "",
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin,
                    //OrganizationId = user.OrganizationId == null ? null : user.OrganizationId.ToString(),
                    //OrganizationName = user.Organization == null ? null : user.Organization.Name,
                    Department = user.Department,
                    PhoneNumber = user.PhoneNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /api/users
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<DashboardUserDto>> CreateUser([FromBody] CreateDashboardUserRequestDto dto)
        {
            try
            {
                // Check if email already exists
                var existing = await _context.DashboardUsers
                    .FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (existing != null)
                    return BadRequest(new { error = "Email already exists" });

                // Check if organization exists
                //Guid? orgId = null;
                //if (!string.IsNullOrEmpty(dto.OrganizationId) && Guid.TryParse(dto.OrganizationId, out var orgGuid))
                //{
                //    var org = await _context.Organizations.FindAsync(orgGuid);
                //    if (org == null)
                //        return BadRequest(new { error = "Organization not found" });
                //    orgId = orgGuid;
                //}

                var user = new DashboardUser
                {
                    UserId = Guid.NewGuid(),
                    Username = dto.Username,
                    Email = dto.Email,
                    FullName = dto.FullName,
                    UserCategory = dto.UserCategory,
                    Role = dto.Role,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    //OrganizationId = orgId,
                    Department = dto.Department,
                    PhoneNumber = dto.PhoneNumber,
                    PasswordHash = !string.IsNullOrEmpty(dto.Password)
                        ? _authService.HashPassword(dto.Password)
                        : _authService.HashPassword("TempPass123!")
                };

                _context.DashboardUsers.Add(user);
                await _context.SaveChangesAsync();

                return Ok(new DashboardUserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email ?? "",
                    FullName = user.FullName ?? "",
                    Role = user.Role ?? "",
                    UserCategory = user.UserCategory ?? "",
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin,
                    //OrganizationId = user.OrganizationId == null ? null : user.OrganizationId.ToString(),
                    Department = user.Department,
                    PhoneNumber = user.PhoneNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PATCH /api/users/{id}
        [HttpPatch("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateDashboardUserRequestDto dto)
        {
            try
            {
                var user = await _context.DashboardUsers.FindAsync(id);
                if (user == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(dto.FullName))
                    user.FullName = dto.FullName;

                if (!string.IsNullOrEmpty(dto.UserCategory))
                    user.UserCategory = dto.UserCategory;

                if (!string.IsNullOrEmpty(dto.Role))
                    user.Role = dto.Role;

                if (dto.IsActive.HasValue)
                    user.IsActive = dto.IsActive.Value;

                //if (!string.IsNullOrEmpty(dto.OrganizationId) && Guid.TryParse(dto.OrganizationId, out var orgId))
                //    user.OrganizationId = orgId;

                if (!string.IsNullOrEmpty(dto.Department))
                    user.Department = dto.Department;

                if (!string.IsNullOrEmpty(dto.PhoneNumber))
                    user.PhoneNumber = dto.PhoneNumber;

                await _context.SaveChangesAsync();

                return Ok(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /api/users/{id}/block
        [HttpPost("{id:guid}/block")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> BlockUser(Guid id)
        {
            try
            {
                var user = await _context.DashboardUsers.FindAsync(id);
                if (user == null)
                    return NotFound();

                user.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { message = "User blocked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /api/users/{id}/unblock
        [HttpPost("{id:guid}/unblock")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UnblockUser(Guid id)
        {
            try
            {
                var user = await _context.DashboardUsers.FindAsync(id);
                if (user == null)
                    return NotFound();

                user.IsActive = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "User unblocked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // DELETE /api/users/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var user = await _context.DashboardUsers.FindAsync(id);
                if (user == null)
                    return NotFound();

                _context.DashboardUsers.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /api/users/drivers
        [HttpGet("drivers")]
        public async Task<ActionResult<IEnumerable<DashboardUserDto>>> GetDrivers([FromQuery] string? orgId)
        {
            try
            {
                var query = _context.DashboardUsers
                    //.Include(u => u.Organization)
                    .Where(u => u.Role == "Driver" && u.IsActive);

                //if (!string.IsNullOrEmpty(orgId) && Guid.TryParse(orgId, out var orgGuid))
                //    query = query.Where(u => u.OrganizationId == orgGuid);

                var drivers = await query
                    .Select(u => new DashboardUserDto
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        Email = u.Email ?? "",
                        FullName = u.FullName ?? "",
                        Role = u.Role ?? "",
                        UserCategory = u.UserCategory ?? "",
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        LastLogin = u.LastLogin,
                        //OrganizationId = u.OrganizationId == null ? null : u.OrganizationId.ToString(),
                        //OrganizationName = u.Organization == null ? null : u.Organization.Name,
                        Department = u.Department,
                        PhoneNumber = u.PhoneNumber
                    })
                    .ToListAsync();

                return Ok(drivers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drivers");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}