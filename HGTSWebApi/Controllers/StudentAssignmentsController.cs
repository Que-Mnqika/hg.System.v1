using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;

namespace HGTSWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/student-assignments")]
    public class StudentAssignmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StudentAssignmentsController> _logger;

        public StudentAssignmentsController(AppDbContext context, ILogger<StudentAssignmentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/student-assignments?orgId=&studentId=&routeId=&status=
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentAssignmentDto>>> GetAll(
            [FromQuery] string? orgId,
            [FromQuery] string? studentId,
            [FromQuery] string? routeId,
            [FromQuery] string? status)
        {
            try
            {
                var query = _context.Placements
                    .Include(p => p.Student)
                        .ThenInclude(s => s != null ? s.Institution : null)
                    .Include(p => p.Student)
                        .ThenInclude(s => s != null ? s.Residence : null)
                    .Include(p => p.Route)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(orgId))
                {
                    query = query.Where(p => p.Student != null && p.Student.InstitutionId.ToString() == orgId);
                }

                if (!string.IsNullOrEmpty(studentId))
                {
                    query = query.Where(p => p.StudentId.ToString() == studentId);
                }

                if (!string.IsNullOrEmpty(routeId))
                {
                    query = query.Where(p => p.RouteId.ToString() == routeId);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                var assignments = await query
                    .OrderByDescending(p => p.StartDate)
                    .Select(p => new StudentAssignmentDto
                    {
                        AssignmentId = p.PlacementId,
                        StudentId = p.StudentId,
                        StudentName = p.Student != null ? p.Student.FullName : null,
                        StudentNumber = p.Student != null ? p.Student.StudentNumber : null,
                        RouteId = p.RouteId,
                        RouteName = p.Route != null ? p.Route.RouteName : null,
                        RouteCode = p.Route != null ? p.Route.RouteCode : null,
                        LocationName = p.LocationName,
                        LocationAddress = p.LocationAddress,
                        PlacementType = p.PlacementType,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        Status = p.Status,
                        CreatedAt = p.StartDate,
                        OrganizationId = p.Student != null ? p.Student.InstitutionId.ToString() : null,
                        OrganizationName = p.Student != null && p.Student.Institution != null ? p.Student.Institution.InstitutionName : null
                    })
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student assignments");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /api/student-assignments/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<StudentAssignmentDto>> GetById(Guid id)
        {
            try
            {
                var assignment = await _context.Placements
                    .Include(p => p.Student)
                        .ThenInclude(s => s != null ? s.Institution : null)
                    .Include(p => p.Student)
                        .ThenInclude(s => s != null ? s.Residence : null)
                    .Include(p => p.Route)
                    .FirstOrDefaultAsync(p => p.PlacementId == id);

                if (assignment == null)
                    return NotFound();

                var dto = new StudentAssignmentDto
                {
                    AssignmentId = assignment.PlacementId,
                    StudentId = assignment.StudentId,
                    StudentName = assignment.Student?.FullName,
                    StudentNumber = assignment.Student?.StudentNumber,
                    RouteId = assignment.RouteId,
                    RouteName = assignment.Route?.RouteName,
                    RouteCode = assignment.Route?.RouteCode,
                    LocationName = assignment.LocationName,
                    LocationAddress = assignment.LocationAddress,
                    PlacementType = assignment.PlacementType,
                    StartDate = assignment.StartDate,
                    EndDate = assignment.EndDate,
                    Status = assignment.Status,
                    CreatedAt = assignment.StartDate,
                    OrganizationId = assignment.Student?.InstitutionId.ToString(),
                    OrganizationName = assignment.Student?.Institution?.InstitutionName
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student assignment");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /api/student-assignments
        [HttpPost]
        [Authorize(Policy = "Operations")]
        public async Task<ActionResult<StudentAssignmentDto>> Create([FromBody] CreateStudentAssignmentDto dto)
        {
            try
            {
                // Validate student exists
                var student = await _context.Students.FindAsync(dto.StudentId);
                if (student == null)
                    return BadRequest(new { error = "Student not found" });

                // Validate route exists if provided
                if (dto.RouteId.HasValue)
                {
                    var route = await _context.VehicleRoutes.FindAsync(dto.RouteId.Value);
                    if (route == null)
                        return BadRequest(new { error = "Route not found" });
                }

                // Validate dates
                if (dto.EndDate <= dto.StartDate)
                    return BadRequest(new { error = "End date must be after start date" });

                var assignment = new Placement
                {
                    PlacementId = Guid.NewGuid(),
                    StudentId = dto.StudentId,
                    LocationName = dto.LocationName,
                    LocationAddress = dto.LocationAddress,
                    PlacementType = dto.PlacementType,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    RouteId = dto.RouteId,
                    Status = dto.Status ?? "Active"
                };

                _context.Placements.Add(assignment);
                await _context.SaveChangesAsync();

                var response = new StudentAssignmentDto
                {
                    AssignmentId = assignment.PlacementId,
                    StudentId = assignment.StudentId,
                    StudentName = student.FullName,
                    StudentNumber = student.StudentNumber,
                    RouteId = assignment.RouteId,
                    LocationName = assignment.LocationName,
                    LocationAddress = assignment.LocationAddress,
                    PlacementType = assignment.PlacementType,
                    StartDate = assignment.StartDate,
                    EndDate = assignment.EndDate,
                    Status = assignment.Status,
                    CreatedAt = assignment.StartDate,
                    OrganizationId = student.InstitutionId.ToString(),
                    OrganizationName = student.Institution?.InstitutionName
                };

                if (assignment.RouteId.HasValue)
                {
                    var route = await _context.VehicleRoutes.FindAsync(assignment.RouteId.Value);
                    response.RouteName = route?.RouteName;
                    response.RouteCode = route?.RouteCode;
                }

                return CreatedAtAction(nameof(GetById), new { id = assignment.PlacementId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student assignment");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PATCH /api/student-assignments/{id}
        [HttpPatch("{id:guid}")]
        [Authorize(Policy = "Operations")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentAssignmentDto dto)
        {
            try
            {
                var assignment = await _context.Placements.FindAsync(id);
                if (assignment == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(dto.LocationName))
                    assignment.LocationName = dto.LocationName;

                if (!string.IsNullOrEmpty(dto.LocationAddress))
                    assignment.LocationAddress = dto.LocationAddress;

                if (!string.IsNullOrEmpty(dto.PlacementType))
                    assignment.PlacementType = dto.PlacementType;

                if (!string.IsNullOrEmpty(dto.Status))
                    assignment.Status = dto.Status;

                if (dto.StartDate.HasValue)
                    assignment.StartDate = dto.StartDate.Value;

                if (dto.EndDate.HasValue)
                    assignment.EndDate = dto.EndDate.Value;

                if (dto.RouteId != assignment.RouteId)
                {
                    if (dto.RouteId.HasValue)
                    {
                        var route = await _context.VehicleRoutes.FindAsync(dto.RouteId.Value);
                        if (route == null)
                            return BadRequest(new { error = "Route not found" });
                    }
                    assignment.RouteId = dto.RouteId;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Student assignment updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student assignment");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // DELETE /api/student-assignments/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "Operations")]
        public async Task<IActionResult> Delete(Guid id, [FromQuery] string? orgId)
        {
            try
            {
                var assignment = await _context.Placements.FindAsync(id);
                if (assignment == null)
                    return NotFound();

                // Optional: Verify orgId matches if provided
                if (!string.IsNullOrEmpty(orgId))
                {
                    var student = await _context.Students.FindAsync(assignment.StudentId);
                    if (student != null && student.InstitutionId.ToString() != orgId)
                        return Forbid();
                }

                _context.Placements.Remove(assignment);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Student assignment deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student assignment");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /api/student-assignments/student/{studentId}
        [HttpGet("student/{studentId:guid}")]
        public async Task<ActionResult<IEnumerable<StudentAssignmentDto>>> GetByStudentId(Guid studentId)
        {
            try
            {
                var assignments = await _context.Placements
                    .Include(p => p.Student)
                    .Include(p => p.Route)
                    .Where(p => p.StudentId == studentId)
                    .OrderByDescending(p => p.StartDate)
                    .Select(p => new StudentAssignmentDto
                    {
                        AssignmentId = p.PlacementId,
                        StudentId = p.StudentId,
                        StudentName = p.Student != null ? p.Student.FullName : null,
                        StudentNumber = p.Student != null ? p.Student.StudentNumber : null,
                        RouteId = p.RouteId,
                        RouteName = p.Route != null ? p.Route.RouteName : null,
                        RouteCode = p.Route != null ? p.Route.RouteCode : null,
                        LocationName = p.LocationName,
                        LocationAddress = p.LocationAddress,
                        PlacementType = p.PlacementType,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        Status = p.Status,
                        CreatedAt = p.StartDate
                    })
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignments by student");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}