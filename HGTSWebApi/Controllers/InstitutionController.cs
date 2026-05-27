using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("api/institution")]  // Changed to api/institution
    public class InstitutionController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<InstitutionController> _logger;

        public InstitutionController(AppDbContext context, ILogger<InstitutionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/institution
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InstitutionDto>>> GetAll()
        {
            try
            {
                var institutions = await _context.Institutions
                    //.Include(i => i.Organization)
                    .Select(i => new InstitutionDto
                    {
                        InstitutionId = i.InstitutionId,
                        InstitutionName = i.InstitutionName,
                        InstitutionCode = i.InstitutionCode,
                        CampusName = i.CampusName,
                        //OrganizationId = i.OrganizationId,
                        //OrganizationName = i.Organization != null ? i.Organization.Name : null,
                        Status = i.Status,
                        StudentCount = _context.Students.Count(s => s.InstitutionId == i.InstitutionId),
                        FacultyCount = _context.Faculties.Count(f => f.InstitutionId == i.InstitutionId),
                        ResidenceCount = _context.Residences.Count(r => r.InstitutionId == i.InstitutionId),
                        RouteCount = _context.VehicleRoutes.Count(r => r.InstitutionId == i.InstitutionId)
                    })
                    .ToListAsync();

                return Ok(institutions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting institutions");
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // GET /api/institution/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<InstitutionDto>> GetById(Guid id)
        {
            try
            {
                var institution = await _context.Institutions
                    //.Include(i => i.Organization)
                    .FirstOrDefaultAsync(i => i.InstitutionId == id);

                if (institution == null)
                    return NotFound();

                var dto = new InstitutionDto
                {
                    InstitutionId = institution.InstitutionId,
                    InstitutionName = institution.InstitutionName,
                    InstitutionCode = institution.InstitutionCode,
                    CampusName = institution.CampusName,
                    //OrganizationId = institution.OrganizationId,
                    //OrganizationName = institution.Organization?.Name,
                    Status = institution.Status,
                    StudentCount = await _context.Students.CountAsync(s => s.InstitutionId == id),
                    FacultyCount = await _context.Faculties.CountAsync(f => f.InstitutionId == id),
                    ResidenceCount = await _context.Residences.CountAsync(r => r.InstitutionId == id),
                    RouteCount = await _context.VehicleRoutes.CountAsync(r => r.InstitutionId == id)
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting institution {Id}", id);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // POST /api/institution
        [HttpPost]
        public async Task<ActionResult<InstitutionDto>> Create([FromBody] CreateInstitutionDto dto)
        {
            try
            {
                // Check if institution code already exists
                var existing = await _context.Institutions
                    .FirstOrDefaultAsync(i => i.InstitutionCode == dto.InstitutionCode);

                if (existing != null)
                    return BadRequest(new { error = "Institution code already exists" });

                var institution = new Institution
                {
                    InstitutionId = Guid.NewGuid(),
                    InstitutionName = dto.InstitutionName,
                    InstitutionCode = dto.InstitutionCode.ToUpper(),
                    CampusName = dto.CampusName,
                    OrganizationId = dto.OrganizationId,
                    Status = dto.Status ?? "Active"
                };

                _context.Institutions.Add(institution);
                await _context.SaveChangesAsync();

                var response = new InstitutionDto
                {
                    InstitutionId = institution.InstitutionId,
                    InstitutionName = institution.InstitutionName,
                    InstitutionCode = institution.InstitutionCode,
                    CampusName = institution.CampusName,
                    //OrganizationId = institution.OrganizationId,
                    Status = institution.Status,
                    StudentCount = 0,
                    FacultyCount = 0,
                    ResidenceCount = 0,
                    RouteCount = 0
                };

                return CreatedAtAction(nameof(GetById), new { id = institution.InstitutionId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating institution");
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // PUT /api/institution/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInstitutionDto dto)
        {
            try
            {
                var institution = await _context.Institutions.FindAsync(id);
                if (institution == null)
                    return NotFound();

                // Check if new institution code conflicts with another institution
                if (!string.IsNullOrEmpty(dto.InstitutionCode) && dto.InstitutionCode != institution.InstitutionCode)
                {
                    var existing = await _context.Institutions
                        .FirstOrDefaultAsync(i => i.InstitutionCode == dto.InstitutionCode && i.InstitutionId != id);

                    if (existing != null)
                        return BadRequest(new { error = "Institution code already exists" });

                    institution.InstitutionCode = dto.InstitutionCode.ToUpper();
                }

                if (!string.IsNullOrEmpty(dto.InstitutionName))
                    institution.InstitutionName = dto.InstitutionName;

                if (!string.IsNullOrEmpty(dto.CampusName))
                    institution.CampusName = dto.CampusName;

                //if (dto.OrganizationId.HasValue)
                //    institution.OrganizationId = dto.OrganizationId.Value;

                if (!string.IsNullOrEmpty(dto.Status))
                    institution.Status = dto.Status;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Institution updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating institution {Id}", id);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // DELETE /api/institution/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var institution = await _context.Institutions
                    .Include(i => i.Students)
                    .Include(i => i.Faculties)
                    .Include(i => i.Residences)
                    .Include(i => i.Routes)
                    .FirstOrDefaultAsync(i => i.InstitutionId == id);

                if (institution == null)
                    return NotFound();

                // Check if there are related records
                if (institution.Students.Any() || institution.Faculties.Any() ||
                    institution.Residences.Any() || institution.Routes.Any())
                {
                    return BadRequest(new { error = "Cannot delete institution with existing relationships" });
                }

                _context.Institutions.Remove(institution);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Institution deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting institution {Id}", id);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }
    }
}