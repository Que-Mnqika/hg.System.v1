using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("api/residence")] 
    public class ResidenceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ResidenceController> _logger;

        public ResidenceController(AppDbContext context, ILogger<ResidenceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /residence
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResidenceDto>>> GetAll()
        {
            try
            {
                var residences = await _context.Residences
                    .Include(r => r.Institution)
                    .Include(r => r.PickupZone)
                    .Include(r => r.Students)
                    .Select(r => new ResidenceDto
                    {
                        ResidenceId = r.ResidenceId,
                        ResidenceCode = r.ResidenceCode,
                        ResidenceName = r.ResidenceName,
                        InstitutionId = r.InstitutionId,
                        InstitutionName = r.Institution != null ? r.Institution.InstitutionName : null,
                        PickupZoneId = r.PickupZoneId,
                        PickupZoneCode = r.PickupZone != null ? r.PickupZone.PickupZoneCode : null,
                        PickupZoneName = r.PickupZone != null ? r.PickupZone.Name : null,
                        StudentCount = r.Students != null ? r.Students.Count : 0
                    })
                    .ToListAsync();

                return Ok(residences);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting residences");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /residence/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ResidenceDto>> GetById(Guid id)
        {
            try
            {
                var residence = await _context.Residences
                    .Include(r => r.Institution)
                    .Include(r => r.PickupZone)
                    .Include(r => r.Students)
                    .FirstOrDefaultAsync(r => r.ResidenceId == id);

                if (residence == null)
                    return NotFound();

                var dto = new ResidenceDto
                {
                    ResidenceId = residence.ResidenceId,
                    ResidenceCode = residence.ResidenceCode,
                    ResidenceName = residence.ResidenceName,
                    InstitutionId = residence.InstitutionId,
                    InstitutionName = residence.Institution?.InstitutionName,
                    PickupZoneId = residence.PickupZoneId,
                    PickupZoneCode = residence.PickupZone?.PickupZoneCode,
                    PickupZoneName = residence.PickupZone?.Name,
                    StudentCount = residence.Students?.Count ?? 0
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting residence");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /residence
        [HttpPost]
        public async Task<ActionResult<ResidenceDto>> Create([FromBody] CreateResidenceDto dto)
        {
            try
            {
                // Check if institution exists
                var institution = await _context.Institutions.FindAsync(dto.InstitutionId);
                if (institution == null)
                    return BadRequest(new { error = "Institution not found" });

                // Check if pickup zone exists if provided
                if (dto.PickupZoneId.HasValue)
                {
                    var pickupZone = await _context.PickupZones.FindAsync(dto.PickupZoneId.Value);
                    if (pickupZone == null)
                        return BadRequest(new { error = "Pickup zone not found" });
                }

                var residence = new Residence
                {
                    ResidenceId = Guid.NewGuid(),
                    ResidenceCode = dto.ResidenceCode.ToUpper(),
                    ResidenceName = dto.ResidenceName,
                    InstitutionId = dto.InstitutionId,
                    PickupZoneId = dto.PickupZoneId
                };

                _context.Residences.Add(residence);
                await _context.SaveChangesAsync();

                var response = new ResidenceDto
                {
                    ResidenceId = residence.ResidenceId,
                    ResidenceCode = residence.ResidenceCode,
                    ResidenceName = residence.ResidenceName,
                    InstitutionId = residence.InstitutionId,
                    InstitutionName = institution.InstitutionName,
                    PickupZoneId = residence.PickupZoneId,
                    StudentCount = 0
                };

                if (residence.PickupZoneId.HasValue)
                {
                    var pickupZone = await _context.PickupZones.FindAsync(residence.PickupZoneId.Value);
                    response.PickupZoneCode = pickupZone?.PickupZoneCode;
                    response.PickupZoneName = pickupZone?.Name;
                }

                return CreatedAtAction(nameof(GetById), new { id = residence.ResidenceId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating residence");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        // POST /residence/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult<object>> CreateBulk([FromBody] List<CreateResidenceDto> requests)
        {
            try
            {
                var created = new List<ResidenceDto>();
                var errors = new List<string>();

                foreach (var request in requests)
                {
                    try
                    {
                        // Check if institution exists
                        var institution = await _context.Institutions.FindAsync(request.InstitutionId);
                        if (institution == null)
                        {
                            errors.Add($"Institution {request.InstitutionId} not found for residence {request.ResidenceCode}");
                            continue;
                        }

                        // Check if pickup zone exists if provided
                        if (request.PickupZoneId.HasValue)
                        {
                            var pickupZone = await _context.PickupZones.FindAsync(request.PickupZoneId.Value);
                            if (pickupZone == null)
                            {
                                errors.Add($"Pickup zone {request.PickupZoneId} not found for residence {request.ResidenceCode}");
                                continue;
                            }
                        }

                        var residence = new Residence
                        {
                            ResidenceId = Guid.NewGuid(),
                            ResidenceCode = request.ResidenceCode.ToUpper(),
                            ResidenceName = request.ResidenceName,
                            InstitutionId = request.InstitutionId,
                            PickupZoneId = request.PickupZoneId
                        };

                        _context.Residences.Add(residence);

                        var response = new ResidenceDto
                        {
                            ResidenceId = residence.ResidenceId,
                            ResidenceCode = residence.ResidenceCode,
                            ResidenceName = residence.ResidenceName,
                            InstitutionId = residence.InstitutionId,
                            InstitutionName = institution.InstitutionName,
                            PickupZoneId = residence.PickupZoneId,
                            StudentCount = 0
                        };

                        if (residence.PickupZoneId.HasValue)
                        {
                            var pickupZone = await _context.PickupZones.FindAsync(residence.PickupZoneId.Value);
                            response.PickupZoneCode = pickupZone?.PickupZoneCode;
                            response.PickupZoneName = pickupZone?.Name;
                        }

                        created.Add(response);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error adding {request.ResidenceCode}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Successfully created {created.Count} residences",
                    totalRequested = requests.Count,
                    created,
                    errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk residences");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PUT /residence/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateResidenceDto dto)
        {
            try
            {
                var residence = await _context.Residences.FindAsync(id);
                if (residence == null)
                    return NotFound();

                // Check if institution exists if being updated
                if (dto.InstitutionId.HasValue && dto.InstitutionId != residence.InstitutionId)
                {
                    var institution = await _context.Institutions.FindAsync(dto.InstitutionId.Value);
                    if (institution == null)
                        return BadRequest(new { error = "Institution not found" });

                    residence.InstitutionId = dto.InstitutionId.Value;
                }

                // Check if pickup zone exists if provided
                if (dto.PickupZoneId != residence.PickupZoneId)
                {
                    if (dto.PickupZoneId.HasValue)
                    {
                        var pickupZone = await _context.PickupZones.FindAsync(dto.PickupZoneId.Value);
                        if (pickupZone == null)
                            return BadRequest(new { error = "Pickup zone not found" });
                    }
                    residence.PickupZoneId = dto.PickupZoneId;
                }

                if (!string.IsNullOrEmpty(dto.ResidenceName))
                    residence.ResidenceName = dto.ResidenceName;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Residence updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating residence");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // DELETE /residence/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var residence = await _context.Residences
                    .Include(r => r.Students)
                    .FirstOrDefaultAsync(r => r.ResidenceId == id);

                if (residence == null)
                    return NotFound();

                if (residence.Students.Any())
                    return BadRequest(new { error = "Cannot delete residence with assigned students" });

                _context.Residences.Remove(residence);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Residence deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting residence");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}