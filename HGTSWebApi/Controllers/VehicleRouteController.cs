using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("api/vehicleroutes")]
    public class VehicleRouteController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VehicleRouteController> _logger;

        public VehicleRouteController(AppDbContext context, ILogger<VehicleRouteController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/vehicleroutes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleRouteDto>>> GetAll()
        {
            try
            {
                var routes = await _context.VehicleRoutes
                    .Include(r => r.Institution)
                    .Include(r => r.PickupZone)
                    .Include(r => r.Residence)
                    .Include(r => r.Residences)
                    .Include(r => r.Trips)
                    .Include(r => r.Placements)
                    .ToListAsync();  // First get the data, then project

                // Map to DTO safely
                var routeDtos = routes.Select(r => new VehicleRouteDto
                {
                    RouteId = r.RouteId,
                    RouteCode = r.RouteCode,
                    RouteName = r.RouteName,
                    InstitutionId = r.InstitutionId,
                    InstitutionName = r.Institution?.InstitutionName,
                    PickupZoneId = r.PickupZoneId,
                    PickupZoneCode = r.PickupZone?.PickupZoneCode,
                    PickupZoneName = r.PickupZone?.Name,
                    ResidenceId = r.ResidenceId,
                    ResidenceCode = r.Residence?.ResidenceCode,
                    ResidenceName = r.Residence?.ResidenceName,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    ResidenceCount = r.Residences?.Count ?? 0,  // Safe null check
                    TripCount = r.Trips?.Count ?? 0,           // Safe null check
                    PlacementCount = r.Placements?.Count ?? 0  // Safe null check
                }).ToList();

                return Ok(routeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting routes: {Message}", ex.Message);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // GET /api/vehicleroutes/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<VehicleRouteDto>> GetById(Guid id)
        {
            try
            {
                var route = await _context.VehicleRoutes
                    .Include(r => r.Institution)
                    .Include(r => r.Residences)
                    .Include(r => r.Trips)
                    .Include(r => r.Placements)
                    .FirstOrDefaultAsync(r => r.RouteId == id);

                if (route == null)
                    return NotFound();

                var dto = new VehicleRouteDto
                {
                    RouteId = route.RouteId,
                    RouteCode = route.RouteCode,
                    RouteName = route.RouteName,
                    InstitutionId = route.InstitutionId,
                    InstitutionName = route.Institution?.InstitutionName,
                    ResidenceCount = route.Residences?.Count ?? 0,
                    TripCount = route.Trips?.Count ?? 0,
                    PlacementCount = route.Placements?.Count ?? 0,
                    IsActive = route.IsActive
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting route {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // POST /api/vehicleroutes
        [HttpPost]
        public async Task<ActionResult<VehicleRouteDto>> Create([FromBody] CreateVehicleRouteDto dto)
        {
            try
            {
                var institution = await _context.Institutions.FindAsync(dto.InstitutionId);
                if (institution == null)
                    return BadRequest(new { error = "Institution not found" });

                var route = new VehicleRoute
                {
                    RouteId = Guid.NewGuid(),
                    RouteCode = dto.RouteCode.ToUpper(),
                    RouteName = dto.RouteName,
                    InstitutionId = dto.InstitutionId,
                    Description = dto.Description,
                    PickupZoneId = dto.PickupZoneId,
                    ResidenceId = dto.ResidenceId,
                    IsActive = dto.IsActive
                };

                _context.VehicleRoutes.Add(route);
                await _context.SaveChangesAsync();

                var response = new VehicleRouteDto
                {
                    RouteId = route.RouteId,
                    RouteCode = route.RouteCode,
                    RouteName = route.RouteName,
                    InstitutionId = route.InstitutionId,
                    InstitutionName = institution.InstitutionName,
                    PickupZoneId = route.PickupZoneId,
                    ResidenceId = route.ResidenceId,
                    Description = route.Description,
                    IsActive = route.IsActive,
                    ResidenceCount = 0,
                    TripCount = 0,
                    PlacementCount = 0
                };

                return CreatedAtAction(nameof(GetById), new { id = route.RouteId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating route: {Message}", ex.Message);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // PUT /api/vehicleroutes/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleRouteDto dto)
        {
            try
            {
                var route = await _context.VehicleRoutes.FindAsync(id);
                if (route == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(dto.RouteName))
                    route.RouteName = dto.RouteName;

                if (dto.IsActive.HasValue)
                    route.IsActive = dto.IsActive.Value;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Route updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating route {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }

        // DELETE /api/vehicleroutes/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var route = await _context.VehicleRoutes
                    .Include(r => r.Residences)
                    .Include(r => r.Trips)
                    .Include(r => r.Placements)
                    .FirstOrDefaultAsync(r => r.RouteId == id);

                if (route == null)
                    return NotFound();

                if ((route.Residences?.Any() ?? false) ||
                    (route.Trips?.Any() ?? false) ||
                    (route.Placements?.Any() ?? false))
                    return BadRequest(new { error = "Cannot delete route with existing relationships" });

                _context.VehicleRoutes.Remove(route);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Route deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting route {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
            }
        }
    }
}