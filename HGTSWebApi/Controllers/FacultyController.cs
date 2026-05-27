using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FacultyController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FacultyController> _logger;

        public FacultyController(AppDbContext context, ILogger<FacultyController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /faculty
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FacultyDto>>> GetAll()
        {
            try
            {
                var faculties = await _context.Faculties
                    .Include(f => f.Institution)
                    .Include(f => f.Students)
                    .Select(f => new FacultyDto
                    {
                        FacultyId = f.FacultyId,
                        FacultyName = f.FacultyName,
                        InstitutionId = f.InstitutionId,
                        InstitutionName = f.Institution != null ? f.Institution.InstitutionName : null,
                        StudentCount = f.Students.Count
                    })
                    .ToListAsync();

                return Ok(faculties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting faculties");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /faculty/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<FacultyDto>> GetById(Guid id)
        {
            try
            {
                var faculty = await _context.Faculties
                    .Include(f => f.Institution)
                    .Include(f => f.Students)
                    .FirstOrDefaultAsync(f => f.FacultyId == id);

                if (faculty == null)
                    return NotFound();

                var dto = new FacultyDto
                {
                    FacultyId = faculty.FacultyId,
                    FacultyName = faculty.FacultyName,
                    InstitutionId = faculty.InstitutionId,
                    InstitutionName = faculty.Institution?.InstitutionName,
                    StudentCount = faculty.Students.Count
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting faculty");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /faculty
        [HttpPost]
        public async Task<ActionResult<FacultyDto>> Create([FromBody] CreateFacultyDto dto)
        {
            try
            {
                // Check if institution exists
                var institution = await _context.Institutions.FindAsync(dto.InstitutionId);
                if (institution == null)
                    return BadRequest(new { error = "Institution not found" });

                var faculty = new Faculty
                {
                    FacultyId = Guid.NewGuid(),
                    FacultyName = dto.FacultyName,
                    InstitutionId = dto.InstitutionId
                };

                _context.Faculties.Add(faculty);
                await _context.SaveChangesAsync();

                var response = new FacultyDto
                {
                    FacultyId = faculty.FacultyId,
                    FacultyName = faculty.FacultyName,
                    InstitutionId = faculty.InstitutionId,
                    InstitutionName = institution.InstitutionName,
                    StudentCount = 0
                };

                return CreatedAtAction(nameof(GetById), new { id = faculty.FacultyId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating faculty");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PUT /faculty/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFacultyDto dto)
        {
            try
            {
                var faculty = await _context.Faculties.FindAsync(id);
                if (faculty == null)
                    return NotFound();

                // Check if new institution exists
                if (dto.InstitutionId.HasValue && dto.InstitutionId != faculty.InstitutionId)
                {
                    var institution = await _context.Institutions.FindAsync(dto.InstitutionId.Value);
                    if (institution == null)
                        return BadRequest(new { error = "Institution not found" });

                    faculty.InstitutionId = dto.InstitutionId.Value;
                }

                if (!string.IsNullOrEmpty(dto.FacultyName))
                    faculty.FacultyName = dto.FacultyName;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Faculty updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating faculty");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // DELETE /faculty/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var faculty = await _context.Faculties
                    .Include(f => f.Students)
                    .FirstOrDefaultAsync(f => f.FacultyId == id);

                if (faculty == null)
                    return NotFound();

                if (faculty.Students.Any())
                    return BadRequest(new { error = "Cannot delete faculty with assigned students" });

                _context.Faculties.Remove(faculty);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Faculty deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting faculty");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /faculty/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult<object>> CreateBulkFaculties([FromBody] List<CreateFacultyDto> requests)
        {
            try
            {
                var created = new List<FacultyDto>();
                var errors = new List<string>();

                foreach (var request in requests)
                {
                    try
                    {
                        // Check if institution exists
                        var institution = await _context.Institutions.FindAsync(request.InstitutionId);
                        if (institution == null)
                        {
                            errors.Add($"Institution {request.InstitutionId} not found for faculty {request.FacultyName}");
                            continue;
                        }

                        var faculty = new Faculty
                        {
                            FacultyId = Guid.NewGuid(),
                            FacultyName = request.FacultyName,
                            InstitutionId = request.InstitutionId
                        };

                        _context.Faculties.Add(faculty);

                        var response = new FacultyDto
                        {
                            FacultyId = faculty.FacultyId,
                            FacultyName = faculty.FacultyName,
                            InstitutionId = faculty.InstitutionId,
                            InstitutionName = institution.InstitutionName,
                            StudentCount = 0
                        };

                        created.Add(response);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error adding {request.FacultyName}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Successfully created {created.Count} faculties",
                    totalRequested = requests.Count,
                    created,
                    errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk faculties");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}