using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;
using System.Text.Json;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PickupZoneController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PickupZoneController> _logger;

        public PickupZoneController(AppDbContext context, ILogger<PickupZoneController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /pickupzone
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PickupZoneDto>>> GetAll()
        {
            try
            {
                var zones = await _context.PickupZones
                    .Include(z => z.Institution)
                    .Include(z => z.Routes)
                    .Select(z => new PickupZoneDto
                    {
                        PickupZoneId = z.PickupZoneId,
                        PickupZoneCode = z.PickupZoneCode,
                        Name = z.Name,
                        InstitutionId = z.InstitutionId,
                        InstitutionName = z.Institution != null ? z.Institution.InstitutionName : null,
                        Description = z.Description,
                        IsActive = z.IsActive,
                        RouteCount = z.Routes.Count
                    })
                    .ToListAsync();

                return Ok(zones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pickup zones");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /pickupzone/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PickupZoneDto>> GetById(Guid id)
        {
            try
            {
                var zone = await _context.PickupZones
                    .Include(z => z.Institution)
                    .Include(z => z.Routes)
                    .FirstOrDefaultAsync(z => z.PickupZoneId == id);

                if (zone == null)
                    return NotFound();

                var dto = new PickupZoneDto
                {
                    PickupZoneId = zone.PickupZoneId,
                    PickupZoneCode = zone.PickupZoneCode,
                    Name = zone.Name,
                    InstitutionId = zone.InstitutionId,
                    InstitutionName = zone.Institution?.InstitutionName,
                    Description = zone.Description,
                    IsActive = zone.IsActive,
                    RouteCount = zone.Routes.Count
                };

               

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pickup zone");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /pickupzone/code/{code}
        [HttpGet("code/{code}")]
        public async Task<ActionResult<PickupZoneDto>> GetByCode(string code)
        {
            try
            {
                var zone = await _context.PickupZones
                    .Include(z => z.Institution)
                    .FirstOrDefaultAsync(z => z.PickupZoneCode == code);

                if (zone == null)
                    return NotFound();

                var dto = new PickupZoneDto
                {
                    PickupZoneId = zone.PickupZoneId,
                    PickupZoneCode = zone.PickupZoneCode,
                    Name = zone.Name,
                    InstitutionId = zone.InstitutionId,
                    InstitutionName = zone.Institution?.InstitutionName,
                    Description = zone.Description,
                    IsActive = zone.IsActive
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pickup zone by code");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /pickupzone
        [HttpPost]
        public async Task<ActionResult<PickupZoneDto>> Create([FromBody] CreatePickupZoneDto dto)
        {
            try
            {
                // Check if code already exists
                var existing = await _context.PickupZones
                    .FirstOrDefaultAsync(z => z.PickupZoneCode == dto.PickupZoneCode);

                if (existing != null)
                    return BadRequest(new { error = "Pickup zone code already exists" });

                // Check if institution exists
                var institution = await _context.Institutions.FindAsync(dto.InstitutionId);
                if (institution == null)
                    return BadRequest(new { error = "Institution not found" });

                var zone = new PickupZone
                {
                    PickupZoneId = Guid.NewGuid(),
                    PickupZoneCode = dto.PickupZoneCode.ToUpper(),
                    Name = dto.Name,
                    InstitutionId = dto.InstitutionId,
                    Description = dto.Description,
                    IsActive = dto.IsActive
                };

                _context.PickupZones.Add(zone);
                await _context.SaveChangesAsync();

                var response = new PickupZoneDto
                {
                    PickupZoneId = zone.PickupZoneId,
                    PickupZoneCode = zone.PickupZoneCode,
                    Name = zone.Name,
                    InstitutionId = zone.InstitutionId,
                    InstitutionName = institution.InstitutionName,
                    Description = zone.Description,
                    IsActive = zone.IsActive,
                    RouteCount = 0
                };

                return CreatedAtAction(nameof(GetById), new { id = zone.PickupZoneId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pickup zone");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /pickupzone/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult<object>> CreateBulk([FromBody] List<CreatePickupZoneDto> requests)
        {
            try
            {
                var created = new List<PickupZoneDto>();
                var errors = new List<string>();

                foreach (var request in requests)
                {
                    try
                    {
                        // Check if code already exists
                        var existing = await _context.PickupZones
                            .FirstOrDefaultAsync(z => z.PickupZoneCode == request.PickupZoneCode);

                        if (existing != null)
                        {
                            errors.Add($"Pickup zone code {request.PickupZoneCode} already exists");
                            continue;
                        }

                        // Check if institution exists
                        var institution = await _context.Institutions.FindAsync(request.InstitutionId);
                        if (institution == null)
                        {
                            errors.Add($"Institution {request.InstitutionId} not found for zone {request.PickupZoneCode}");
                            continue;
                        }

                        var zone = new PickupZone
                        {
                            PickupZoneId = Guid.NewGuid(),
                            PickupZoneCode = request.PickupZoneCode.ToUpper(),
                            Name = request.Name,
                            InstitutionId = request.InstitutionId,
                            Description = request.Description,
                            IsActive = request.IsActive
                        };

                        _context.PickupZones.Add(zone);

                        var response = new PickupZoneDto
                        {
                            PickupZoneId = zone.PickupZoneId,
                            PickupZoneCode = zone.PickupZoneCode,
                            Name = zone.Name,
                            InstitutionId = zone.InstitutionId,
                            InstitutionName = institution.InstitutionName,
                            Description = zone.Description,
                            IsActive = zone.IsActive,
                            RouteCount = 0
                        };

                        created.Add(response);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error adding {request.PickupZoneCode}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Successfully created {created.Count} pickup zones",
                    totalRequested = requests.Count,
                    created,
                    errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk pickup zones");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PUT /pickupzone/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePickupZoneDto dto)
        {
            try
            {
                var zone = await _context.PickupZones.FindAsync(id);
                if (zone == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(dto.Name))
                    zone.Name = dto.Name;

                if (!string.IsNullOrEmpty(dto.Description))
                    zone.Description = dto.Description;

                if (dto.IsActive.HasValue)
                    zone.IsActive = dto.IsActive.Value;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Pickup zone updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating pickup zone");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // DELETE /pickupzone/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var zone = await _context.PickupZones
                    .Include(z => z.Routes)
                    .FirstOrDefaultAsync(z => z.PickupZoneId == id);

                if (zone == null)
                    return NotFound();

                if (zone.Routes.Any())
                    return BadRequest(new { error = "Cannot delete pickup zone with existing routes" });

                _context.PickupZones.Remove(zone);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Pickup zone deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting pickup zone");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}