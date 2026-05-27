using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("api/student")]
    public class StudentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StudentController> _logger;

        public StudentController(AppDbContext context, ILogger<StudentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /student
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentDto>>> GetAll()
        {
            try
            {
                var students = await _context.Students
                    .Include(s => s.Institution)
                    .Include(s => s.Residence)
                    .Include(s => s.Faculty)
                    .Include(s => s.Credentials)
                    .Include(s => s.Placements)
                    .Select(s => new StudentDto
                    {
                        StudentId = s.StudentId,
                        FullName = s.FullName,
                        StudentNumber = s.StudentNumber,
                        Email = s.Email,
                        CellNumber = s.CellNumber,
                        InstitutionId = s.InstitutionId,
                        InstitutionName = s.Institution != null ? s.Institution.InstitutionName : null,
                        ResidenceId = s.ResidenceId,
                        ResidenceName = s.Residence != null ? s.Residence.ResidenceName : null,
                        FacultyId = s.FacultyId,
                        FacultyName = s.Faculty != null ? s.Faculty.FacultyName : null,
                        Status = s.Status,
                        CredentialCount = s.Credentials.Count,
                        PlacementCount = s.Placements.Count
                    })
                    .ToListAsync();

                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /student/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<StudentDto>> GetById(Guid id)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.Institution)
                    .Include(s => s.Residence)
                    .Include(s => s.Faculty)
                    .Include(s => s.Credentials)
                    .Include(s => s.Placements)
                    .FirstOrDefaultAsync(s => s.StudentId == id);

                if (student == null)
                    return NotFound();

                var dto = new StudentDto
                {
                    StudentId = student.StudentId,
                    FullName = student.FullName,
                    StudentNumber = student.StudentNumber,
                    Email = student.Email,
                    CellNumber = student.CellNumber,
                    InstitutionId = student.InstitutionId,
                    InstitutionName = student.Institution?.InstitutionName,
                    ResidenceId = student.ResidenceId,
                    ResidenceName = student.Residence?.ResidenceName,
                    FacultyId = student.FacultyId,
                    FacultyName = student.Faculty?.FacultyName,
                    Status = student.Status,
                    CredentialCount = student.Credentials.Count,
                    PlacementCount = student.Placements.Count
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /student/number/{studentNumber}
        [HttpGet("number/{studentNumber}")]
        public async Task<ActionResult<StudentDto>> GetByStudentNumber(string studentNumber)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.Institution)
                    .Include(s => s.Residence)
                    .Include(s => s.Faculty)
                    .FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);

                if (student == null)
                    return NotFound();

                var dto = new StudentDto
                {
                    StudentId = student.StudentId,
                    FullName = student.FullName,
                    StudentNumber = student.StudentNumber,
                    Email = student.Email,
                    CellNumber = student.CellNumber,
                    InstitutionId = student.InstitutionId,
                    InstitutionName = student.Institution?.InstitutionName,
                    ResidenceId = student.ResidenceId,
                    ResidenceName = student.Residence?.ResidenceName,
                    FacultyId = student.FacultyId,
                    FacultyName = student.Faculty?.FacultyName,
                    Status = student.Status,
                    CredentialCount = student.Credentials.Count,
                    PlacementCount = student.Placements.Count
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student by number");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /student
        [HttpPost]
        public async Task<ActionResult<StudentDto>> Create([FromBody] CreateStudentDto dto)
        {
            try
            {
                // Check if student number already exists
                var existing = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentNumber == dto.StudentNumber);

                if (existing != null)
                    return BadRequest(new { error = "Student number already exists" });

                // Check if institution exists
                var institution = await _context.Institutions.FindAsync(dto.InstitutionId);
                if (institution == null)
                    return BadRequest(new { error = "Institution not found" });

                // Check if residence exists if provided
                if (dto.ResidenceId.HasValue)
                {
                    var residence = await _context.Residences.FindAsync(dto.ResidenceId.Value);
                    if (residence == null)
                        return BadRequest(new { error = "Residence not found" });
                }

                // Check if faculty exists if provided
                if (dto.FacultyId.HasValue)
                {
                    var faculty = await _context.Faculties.FindAsync(dto.FacultyId.Value);
                    if (faculty == null)
                        return BadRequest(new { error = "Faculty not found" });
                }

                var student = new Student
                {
                    StudentId = Guid.NewGuid(),
                    FullName = dto.FullName,
                    StudentNumber = dto.StudentNumber,
                    Email = dto.Email,
                    CellNumber = dto.CellNumber,
                    InstitutionId = dto.InstitutionId,
                    ResidenceId = dto.ResidenceId,
                    FacultyId = dto.FacultyId,
                    Status = dto.Status ?? "Active"
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                var response = new StudentDto
                {
                    StudentId = student.StudentId,
                    FullName = student.FullName,
                    StudentNumber = student.StudentNumber,
                    Email = student.Email,
                    CellNumber = student.CellNumber,
                    InstitutionId = student.InstitutionId,
                    InstitutionName = institution.InstitutionName,
                    ResidenceId = student.ResidenceId,
                    FacultyId = student.FacultyId,
                    Status = student.Status,
                    CredentialCount = 0,
                    PlacementCount = 0
                };

                return CreatedAtAction(nameof(GetById), new { id = student.StudentId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PUT /student/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentDto dto)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(dto.FullName))
                    student.FullName = dto.FullName;

                if (!string.IsNullOrEmpty(dto.Email))
                    student.Email = dto.Email;

                if (!string.IsNullOrEmpty(dto.CellNumber))
                    student.CellNumber = dto.CellNumber;

                if (!string.IsNullOrEmpty(dto.Status))
                    student.Status = dto.Status;

                if (dto.ResidenceId != student.ResidenceId)
                {
                    if (dto.ResidenceId.HasValue)
                    {
                        var residence = await _context.Residences.FindAsync(dto.ResidenceId.Value);
                        if (residence == null)
                            return BadRequest(new { error = "Residence not found" });
                    }
                    student.ResidenceId = dto.ResidenceId;
                }

                if (dto.FacultyId != student.FacultyId)
                {
                    if (dto.FacultyId.HasValue)
                    {
                        var faculty = await _context.Faculties.FindAsync(dto.FacultyId.Value);
                        if (faculty == null)
                            return BadRequest(new { error = "Faculty not found" });
                    }
                    student.FacultyId = dto.FacultyId;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Student updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // DELETE /student/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.Credentials)
                    .Include(s => s.Placements)
                    .FirstOrDefaultAsync(s => s.StudentId == id);

                if (student == null)
                    return NotFound();

                if (student.Credentials.Any() || student.Placements.Any())
                    return BadRequest(new { error = "Cannot delete student with existing credentials or placements" });

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Student deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /student/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult<object>> CreateBulkStudents([FromBody] List<CreateStudentDto> requests)
        {
            try
            {
                var created = new List<StudentDto>();
                var errors = new List<string>();

                foreach (var request in requests)
                {
                    try
                    {
                        // Check if student number already exists
                        var existing = await _context.Students
                            .FirstOrDefaultAsync(s => s.StudentNumber == request.StudentNumber);

                        if (existing != null)
                        {
                            errors.Add($"Student number {request.StudentNumber} already exists");
                            continue;
                        }

                        // Check if institution exists
                        var institution = await _context.Institutions.FindAsync(request.InstitutionId);
                        if (institution == null)
                        {
                            errors.Add($"Institution {request.InstitutionId} not found for student {request.StudentNumber}");
                            continue;
                        }

                        // Check if residence exists if provided
                        if (request.ResidenceId.HasValue)
                        {
                            var residence = await _context.Residences.FindAsync(request.ResidenceId.Value);
                            if (residence == null)
                            {
                                errors.Add($"Residence {request.ResidenceId} not found for student {request.StudentNumber}");
                                continue;
                            }
                        }

                        // Check if faculty exists if provided
                        if (request.FacultyId.HasValue)
                        {
                            var faculty = await _context.Faculties.FindAsync(request.FacultyId.Value);
                            if (faculty == null)
                            {
                                errors.Add($"Faculty {request.FacultyId} not found for student {request.StudentNumber}");
                                continue;
                            }
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

                        var response = new StudentDto
                        {
                            StudentId = student.StudentId,
                            FullName = student.FullName,
                            StudentNumber = student.StudentNumber,
                            Email = student.Email,
                            CellNumber = student.CellNumber,
                            InstitutionId = student.InstitutionId,
                            InstitutionName = institution.InstitutionName,
                            ResidenceId = student.ResidenceId,
                            FacultyId = student.FacultyId,
                            Status = student.Status,
                            CredentialCount = 0,
                            PlacementCount = 0
                        };

                        if (student.ResidenceId.HasValue)
                        {
                            var residence = await _context.Residences.FindAsync(student.ResidenceId.Value);
                            response.ResidenceName = residence?.ResidenceName;
                        }

                        if (student.FacultyId.HasValue)
                        {
                            var faculty = await _context.Faculties.FindAsync(student.FacultyId.Value);
                            response.FacultyName = faculty?.FacultyName;
                        }

                        created.Add(response);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error adding {request.StudentNumber}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Successfully created {created.Count} students",
                    totalRequested = requests.Count,
                    created,
                    errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk students");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}