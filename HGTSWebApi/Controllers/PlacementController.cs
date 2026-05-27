using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("/api/placement")]
    public class PlacementController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PlacementController> _logger;

        public PlacementController(AppDbContext context, ILogger<PlacementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /placement
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlacementDto>>> GetAll()
        {
            try
            {
                var placements = await _context.Placements
                    .Include(p => p.Student)
                    .Include(p => p.Route)
                    .Select(p => new PlacementDto
                    {
                        PlacementId = p.PlacementId,
                        StudentId = p.StudentId,
                        StudentName = p.Student != null ? p.Student.FullName : null,
                        StudentNumber = p.Student != null ? p.Student.StudentNumber : null,
                        LocationName = p.LocationName,
                        LocationAddress = p.LocationAddress,
                        PlacementType = p.PlacementType,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        RouteId = p.RouteId,
                        RouteName = p.Route != null ? p.Route.RouteName : null,
                        Status = p.Status
                    })
                    .ToListAsync();

                return Ok(placements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting placements");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /placement/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PlacementDto>> GetById(Guid id)
        {
            try
            {
                var placement = await _context.Placements
                    .Include(p => p.Student)
                    .Include(p => p.Route)
                    .FirstOrDefaultAsync(p => p.PlacementId == id);

                if (placement == null)
                    return NotFound();

                var dto = new PlacementDto
                {
                    PlacementId = placement.PlacementId,
                    StudentId = placement.StudentId,
                    StudentName = placement.Student?.FullName,
                    StudentNumber = placement.Student?.StudentNumber,
                    LocationName = placement.LocationName,
                    LocationAddress = placement.LocationAddress,
                    PlacementType = placement.PlacementType,
                    StartDate = placement.StartDate,
                    EndDate = placement.EndDate,
                    RouteId = placement.RouteId,
                    RouteName = placement.Route?.RouteName,
                    Status = placement.Status
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting placement");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /placement/student/{studentId}
        [HttpGet("student/{studentId:guid}")]
        public async Task<ActionResult<IEnumerable<PlacementDto>>> GetByStudentId(Guid studentId)
        {
            try
            {
                var placements = await _context.Placements
                    .Include(p => p.Student)
                    .Include(p => p.Route)
                    .Where(p => p.StudentId == studentId)
                    .Select(p => new PlacementDto
                    {
                        PlacementId = p.PlacementId,
                        StudentId = p.StudentId,
                        StudentName = p.Student != null ? p.Student.FullName : null,
                        StudentNumber = p.Student != null ? p.Student.StudentNumber : null,
                        LocationName = p.LocationName,
                        LocationAddress = p.LocationAddress,
                        PlacementType = p.PlacementType,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        RouteId = p.RouteId,
                        RouteName = p.Route != null ? p.Route.RouteName : null,
                        Status = p.Status
                    })
                    .ToListAsync();

                return Ok(placements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting placements for student");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /placement/active/{studentId}
        [HttpGet("active/{studentId:guid}")]
        public async Task<ActionResult<IEnumerable<PlacementDto>>> GetActiveByStudentId(Guid studentId)
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                var placements = await _context.Placements
                    .Include(p => p.Student)
                    .Include(p => p.Route)
                    .Where(p => p.StudentId == studentId
                        && p.Status == "Active"
                        && p.StartDate <= today
                        && p.EndDate >= today)
                    .Select(p => new PlacementDto
                    {
                        PlacementId = p.PlacementId,
                        StudentId = p.StudentId,
                        StudentName = p.Student != null ? p.Student.FullName : null,
                        StudentNumber = p.Student != null ? p.Student.StudentNumber : null,
                        LocationName = p.LocationName,
                        LocationAddress = p.LocationAddress,
                        PlacementType = p.PlacementType,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        RouteId = p.RouteId,
                        RouteName = p.Route != null ? p.Route.RouteName : null,
                        Status = p.Status
                    })
                    .ToListAsync();

                return Ok(placements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active placements");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /placement
        [HttpPost]
        public async Task<ActionResult<PlacementDto>> Create([FromBody] CreatePlacementDto dto)
        {
            try
            {
                // Check if student exists
                var student = await _context.Students.FindAsync(dto.StudentId);
                if (student == null)
                    return BadRequest(new { error = "Student not found" });

                // Check if route exists if provided
                if (dto.RouteId.HasValue)
                {
                    var route = await _context.VehicleRoutes.FindAsync(dto.RouteId.Value);
                    if (route == null)
                        return BadRequest(new { error = "Route not found" });
                }

                // Validate dates
                if (dto.EndDate <= dto.StartDate)
                    return BadRequest(new { error = "End date must be after start date" });

                var placement = new Placement
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

                _context.Placements.Add(placement);
                await _context.SaveChangesAsync();

                var response = new PlacementDto
                {
                    PlacementId = placement.PlacementId,
                    StudentId = placement.StudentId,
                    StudentName = student.FullName,
                    StudentNumber = student.StudentNumber,
                    LocationName = placement.LocationName,
                    LocationAddress = placement.LocationAddress,
                    PlacementType = placement.PlacementType,
                    StartDate = placement.StartDate,
                    EndDate = placement.EndDate,
                    RouteId = placement.RouteId,
                    Status = placement.Status
                };

                return CreatedAtAction(nameof(GetById), new { id = placement.PlacementId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating placement");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PUT /placement/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlacementDto dto)
        {
            try
            {
                var placement = await _context.Placements.FindAsync(id);
                if (placement == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(dto.LocationName))
                    placement.LocationName = dto.LocationName;

                if (!string.IsNullOrEmpty(dto.LocationAddress))
                    placement.LocationAddress = dto.LocationAddress;

                if (!string.IsNullOrEmpty(dto.PlacementType))
                    placement.PlacementType = dto.PlacementType;

                if (!string.IsNullOrEmpty(dto.Status))
                    placement.Status = dto.Status;

                if (dto.StartDate.HasValue)
                    placement.StartDate = dto.StartDate.Value;

                if (dto.EndDate.HasValue)
                    placement.EndDate = dto.EndDate.Value;

                if (dto.RouteId != placement.RouteId)
                {
                    if (dto.RouteId.HasValue)
                    {
                        var route = await _context.VehicleRoutes.FindAsync(dto.RouteId.Value);
                        if (route == null)
                            return BadRequest(new { error = "Route not found" });
                    }
                    placement.RouteId = dto.RouteId;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Placement updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating placement");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // DELETE /placement/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var placement = await _context.Placements.FindAsync(id);
                if (placement == null)
                    return NotFound();

                _context.Placements.Remove(placement);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Placement deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting placement");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /placement/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult<object>> CreateBulkPlacements([FromBody] List<CreatePlacementDto> requests)
        {
            try
            {
                var created = new List<PlacementDto>();
                var errors = new List<string>();

                foreach (var request in requests)
                {
                    try
                    {
                        // Check if student exists
                        var student = await _context.Students.FindAsync(request.StudentId);
                        if (student == null)
                        {
                            errors.Add($"Student {request.StudentId} not found");
                            continue;
                        }

                        // Check if route exists if provided
                        if (request.RouteId.HasValue)
                        {
                            var route = await _context.VehicleRoutes.FindAsync(request.RouteId.Value);
                            if (route == null)
                            {
                                errors.Add($"Route {request.RouteId} not found");
                                continue;
                            }
                        }

                        // Validate dates
                        if (request.EndDate <= request.StartDate)
                        {
                            errors.Add($"End date must be after start date for placement");
                            continue;
                        }

                        var placement = new Placement
                        {
                            PlacementId = Guid.NewGuid(),
                            StudentId = request.StudentId,
                            LocationName = request.LocationName,
                            LocationAddress = request.LocationAddress,
                            PlacementType = request.PlacementType,
                            StartDate = request.StartDate,
                            EndDate = request.EndDate,
                            RouteId = request.RouteId,
                            Status = request.Status ?? "Active"
                        };

                        _context.Placements.Add(placement);

                        var response = new PlacementDto
                        {
                            PlacementId = placement.PlacementId,
                            StudentId = placement.StudentId,
                            StudentName = student.FullName,
                            StudentNumber = student.StudentNumber,
                            LocationName = placement.LocationName,
                            LocationAddress = placement.LocationAddress,
                            PlacementType = placement.PlacementType,
                            StartDate = placement.StartDate,
                            EndDate = placement.EndDate,
                            RouteId = placement.RouteId,
                            Status = placement.Status
                        };

                        if (placement.RouteId.HasValue)
                        {
                            var route = await _context.VehicleRoutes.FindAsync(placement.RouteId.Value);
                            response.RouteName = route?.RouteName;
                        }

                        created.Add(response);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error creating placement: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Successfully created {created.Count} placements",
                    totalRequested = requests.Count,
                    created,
                    errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk placements");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}