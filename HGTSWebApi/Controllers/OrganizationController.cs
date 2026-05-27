using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;

namespace HGTSWebApi.Controllers
{
    // [Authorize]  <-- REMOVED
    [ApiController]
    [Route("api/organizations")]
    public class OrganizationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrganizationsController> _logger;

        public OrganizationsController(AppDbContext context, ILogger<OrganizationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/organizations
        [HttpGet]  // no [Authorize]
        public async Task<ActionResult<IEnumerable<OrganizationDto>>> GetAll(
            [FromQuery] string? status = null,
            [FromQuery] string? q = null)
        {
            try
            {
                var query = _context.Organizations
                    .Include(o => o.DashboardUsers)
                    .Include(o => o.Institutions)
                    .Include(o => o.Devices)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(o => o.Status == status);

                if (!string.IsNullOrEmpty(q))
                    query = query.Where(o => o.Name.Contains(q) || o.Code.Contains(q));

                var organizations = await query
                    .OrderBy(o => o.Name)
                    .Select(o => new OrganizationDto
                    {
                        OrganizationId = o.OrganizationId,
                        Name = o.Name,
                        Code = o.Code,
                        Description = o.Description,
                        Address = o.Address,
                        Phone = o.Phone,
                        Email = o.Email,
                        Timezone = o.Timezone,
                        Status = o.Status,
                        CreatedAt = o.CreatedAt,
                        UserCount = o.DashboardUsers.Count,
                        InstitutionCount = o.Institutions.Count,
                        DeviceCount = o.Devices.Count
                    })
                    .ToListAsync();

                return Ok(organizations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organizations");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /api/organizations/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<OrganizationDto>> GetById(Guid id)
        {
            try
            {
                var organization = await _context.Organizations
                    .Include(o => o.DashboardUsers)
                    .Include(o => o.Institutions)
                    .Include(o => o.Devices)
                    .FirstOrDefaultAsync(o => o.OrganizationId == id);

                if (organization == null)
                    return NotFound();

                var dto = new OrganizationDto
                {
                    OrganizationId = organization.OrganizationId,
                    Name = organization.Name,
                    Code = organization.Code,
                    Description = organization.Description,
                    Address = organization.Address,
                    Phone = organization.Phone,
                    Email = organization.Email,
                    Timezone = organization.Timezone,
                    Status = organization.Status,
                    CreatedAt = organization.CreatedAt,
                    UserCount = organization.DashboardUsers.Count,
                    InstitutionCount = organization.Institutions.Count,
                    DeviceCount = organization.Devices.Count
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organization");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /api/organizations
        [HttpPost]  // no [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<OrganizationDto>> Create([FromBody] CreateOrganizationDto dto)
        {
            try
            {
                var existing = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.Code == dto.Code);

                if (existing != null)
                    return BadRequest(new { error = "Organization code already exists" });

                var organization = new Organization
                {
                    OrganizationId = Guid.NewGuid(),
                    Name = dto.Name,
                    Code = dto.Code.ToUpper(),
                    Description = dto.Description,
                    Address = dto.Address,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    Timezone = dto.Timezone ?? "UTC",
                    Status = dto.Status ?? "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null
                };

                _context.Organizations.Add(organization);
                await _context.SaveChangesAsync();

                var response = new OrganizationDto
                {
                    OrganizationId = organization.OrganizationId,
                    Name = organization.Name,
                    Code = organization.Code,
                    Description = organization.Description,
                    Address = organization.Address,
                    Phone = organization.Phone,
                    Email = organization.Email,
                    Timezone = organization.Timezone,
                    Status = organization.Status,
                    CreatedAt = organization.CreatedAt,
                    UserCount = 0,
                    InstitutionCount = 0,
                    DeviceCount = 0
                };

                return CreatedAtAction(nameof(GetById), new { id = organization.OrganizationId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating organization");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PATCH /api/organizations/{id}
        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrganizationDto dto)
        {
            try
            {
                var organization = await _context.Organizations.FindAsync(id);
                if (organization == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(dto.Code) && dto.Code != organization.Code)
                {
                    var existing = await _context.Organizations
                        .FirstOrDefaultAsync(o => o.Code == dto.Code && o.OrganizationId != id);

                    if (existing != null)
                        return BadRequest(new { error = "Organization code already exists" });

                    organization.Code = dto.Code.ToUpper();
                }

                if (!string.IsNullOrEmpty(dto.Name))
                    organization.Name = dto.Name;

                if (dto.Description != null)
                    organization.Description = dto.Description;

                if (dto.Address != null)
                    organization.Address = dto.Address;

                if (dto.Phone != null)
                    organization.Phone = dto.Phone;

                if (dto.Email != null)
                    organization.Email = dto.Email;

                if (!string.IsNullOrEmpty(dto.Timezone))
                    organization.Timezone = dto.Timezone;

                if (!string.IsNullOrEmpty(dto.Status))
                    organization.Status = dto.Status;

                organization.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Organization updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organization");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // DELETE /api/organizations/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var organization = await _context.Organizations
                    .Include(o => o.DashboardUsers)
                    .Include(o => o.Institutions)
                    .Include(o => o.Devices)
                    .FirstOrDefaultAsync(o => o.OrganizationId == id);

                if (organization == null)
                    return NotFound();

                if (organization.DashboardUsers.Any() || organization.Institutions.Any() || organization.Devices.Any())
                    return BadRequest(new { error = "Cannot delete organization with existing relationships" });

                _context.Organizations.Remove(organization);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Organization deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting organization");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}