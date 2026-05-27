using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("api/routestops")]
    public class RouteStopController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RouteStopController> _logger;

        public RouteStopController(AppDbContext context, ILogger<RouteStopController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/routestops/by-route/{routeId}
        [HttpGet("by-route/{routeId:guid}")]
        public async Task<ActionResult<IEnumerable<RouteStopDto>>> GetByRoute(Guid routeId)
        {
            try
            {
                var routeExists = await _context.VehicleRoutes.AnyAsync(r => r.RouteId == routeId);
                if (!routeExists)
                    return NotFound(new { error = "Route not found" });

                var stops = await _context.RouteStops
                    .Include(rs => rs.Residence)
                    .Where(rs => rs.RouteId == routeId)
                    .OrderBy(rs => rs.StopOrder)
                    .Select(rs => new RouteStopDto
                    {
                        RouteStopId = rs.RouteStopId,
                        RouteId = rs.RouteId,
                        RouteName = rs.Route != null ? rs.Route.RouteName : null,
                        ResidenceId = rs.ResidenceId,
                        ResidenceName = rs.Residence != null ? rs.Residence.ResidenceName : null,
                        StopOrder = rs.StopOrder,
                        EstimatedTravelMinutesFromPrevious = rs.EstimatedTravelMinutesFromPrevious,
                        DwellMinutes = rs.DwellMinutes,
                        CreatedAt = rs.CreatedAt,
                        UpdatedAt = rs.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(stops);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting route stops for route {RouteId}", routeId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /api/routestops/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<RouteStopDto>> GetById(Guid id)
        {
            try
            {
                var stop = await _context.RouteStops
                    .Include(rs => rs.Route)
                    .Include(rs => rs.Residence)
                    .FirstOrDefaultAsync(rs => rs.RouteStopId == id);

                if (stop == null)
                    return NotFound();

                return Ok(new RouteStopDto
                {
                    RouteStopId = stop.RouteStopId,
                    RouteId = stop.RouteId,
                    RouteName = stop.Route?.RouteName,
                    ResidenceId = stop.ResidenceId,
                    ResidenceName = stop.Residence?.ResidenceName,
                    StopOrder = stop.StopOrder,
                    EstimatedTravelMinutesFromPrevious = stop.EstimatedTravelMinutesFromPrevious,
                    DwellMinutes = stop.DwellMinutes,
                    CreatedAt = stop.CreatedAt,
                    UpdatedAt = stop.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting route stop {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /api/routestops (single)
        [HttpPost]
        public async Task<ActionResult<RouteStopDto>> Create([FromBody] CreateRouteStopDto dto)
        {
            try
            {
                // Validate Route exists
                var route = await _context.VehicleRoutes.FindAsync(dto.RouteId);
                if (route == null)
                    return BadRequest(new { error = "Route not found" });

                // Validate Residence exists
                var residence = await _context.Residences.FindAsync(dto.ResidenceId);
                if (residence == null)
                    return BadRequest(new { error = "Residence not found" });

                // Check stop order uniqueness
                var existingOrder = await _context.RouteStops
                    .AnyAsync(rs => rs.RouteId == dto.RouteId && rs.StopOrder == dto.StopOrder);
                if (existingOrder)
                    return BadRequest(new { error = $"Stop order {dto.StopOrder} already exists for this route" });

                var routeStop = new RouteStop
                {
                    RouteStopId = Guid.NewGuid(),
                    RouteId = dto.RouteId,
                    ResidenceId = dto.ResidenceId,
                    StopOrder = dto.StopOrder,
                    EstimatedTravelMinutesFromPrevious = dto.EstimatedTravelMinutesFromPrevious,
                    DwellMinutes = dto.DwellMinutes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.RouteStops.Add(routeStop);
                await _context.SaveChangesAsync();

                var response = new RouteStopDto
                {
                    RouteStopId = routeStop.RouteStopId,
                    RouteId = routeStop.RouteId,
                    RouteName = route.RouteName,
                    ResidenceId = routeStop.ResidenceId,
                    ResidenceName = residence.ResidenceName,
                    StopOrder = routeStop.StopOrder,
                    EstimatedTravelMinutesFromPrevious = routeStop.EstimatedTravelMinutesFromPrevious,
                    DwellMinutes = routeStop.DwellMinutes,
                    CreatedAt = routeStop.CreatedAt,
                    UpdatedAt = routeStop.UpdatedAt
                };

                return CreatedAtAction(nameof(GetById), new { id = routeStop.RouteStopId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating route stop");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /api/routestops/bulk  <-- NEW BULK ENDPOINT
        [HttpPost("bulk")]
        public async Task<ActionResult<object>> CreateBulk([FromBody] List<CreateRouteStopDto> requests)
        {
            try
            {
                if (requests == null || requests.Count == 0)
                    return BadRequest(new { error = "No route stops provided" });

                var created = new List<RouteStopDto>();
                var errors = new List<string>();

                // Group by RouteId to validate each route once
                var routeIds = requests.Select(r => r.RouteId).Distinct().ToList();
                var routes = await _context.VehicleRoutes
                    .Where(r => routeIds.Contains(r.RouteId))
                    .ToDictionaryAsync(r => r.RouteId, r => r);

                var residenceIds = requests.Select(r => r.ResidenceId).Distinct().ToList();
                var residences = await _context.Residences
                    .Where(res => residenceIds.Contains(res.ResidenceId))
                    .ToDictionaryAsync(res => res.ResidenceId, res => res);

                // For each request, create a RouteStop
                foreach (var dto in requests)
                {
                    try
                    {
                        // Validate Route exists
                        if (!routes.TryGetValue(dto.RouteId, out var route))
                        {
                            errors.Add($"Route {dto.RouteId} not found for stop order {dto.StopOrder}");
                            continue;
                        }

                        // Validate Residence exists
                        if (!residences.TryGetValue(dto.ResidenceId, out var residence))
                        {
                            errors.Add($"Residence {dto.ResidenceId} not found for stop order {dto.StopOrder}");
                            continue;
                        }

                        // Check stop order uniqueness within the route (must query DB for existing stops)
                        var existingOrder = await _context.RouteStops
                            .AnyAsync(rs => rs.RouteId == dto.RouteId && rs.StopOrder == dto.StopOrder);
                        if (existingOrder)
                        {
                            errors.Add($"Stop order {dto.StopOrder} already exists for route {route.RouteName} ({route.RouteId})");
                            continue;
                        }

                        var routeStop = new RouteStop
                        {
                            RouteStopId = Guid.NewGuid(),
                            RouteId = dto.RouteId,
                            ResidenceId = dto.ResidenceId,
                            StopOrder = dto.StopOrder,
                            EstimatedTravelMinutesFromPrevious = dto.EstimatedTravelMinutesFromPrevious,
                            DwellMinutes = dto.DwellMinutes,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.RouteStops.Add(routeStop);

                        created.Add(new RouteStopDto
                        {
                            RouteStopId = routeStop.RouteStopId,
                            RouteId = routeStop.RouteId,
                            RouteName = route.RouteName,
                            ResidenceId = routeStop.ResidenceId,
                            ResidenceName = residence.ResidenceName,
                            StopOrder = routeStop.StopOrder,
                            EstimatedTravelMinutesFromPrevious = routeStop.EstimatedTravelMinutesFromPrevious,
                            DwellMinutes = routeStop.DwellMinutes,
                            CreatedAt = routeStop.CreatedAt,
                            UpdatedAt = routeStop.UpdatedAt
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error adding stop order {dto.StopOrder}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Successfully created {created.Count} route stops",
                    totalRequested = requests.Count,
                    created,
                    errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk route stops");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PUT /api/routestops/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRouteStopDto dto)
        {
            try
            {
                var routeStop = await _context.RouteStops.FindAsync(id);
                if (routeStop == null)
                    return NotFound();

                // Update StopOrder, but ensure uniqueness within route
                if (dto.StopOrder.HasValue && dto.StopOrder.Value != routeStop.StopOrder)
                {
                    var conflict = await _context.RouteStops
                        .AnyAsync(rs => rs.RouteId == routeStop.RouteId && rs.StopOrder == dto.StopOrder.Value && rs.RouteStopId != id);
                    if (conflict)
                        return BadRequest(new { error = $"Stop order {dto.StopOrder} already exists for this route" });

                    routeStop.StopOrder = dto.StopOrder.Value;
                }

                if (dto.EstimatedTravelMinutesFromPrevious.HasValue)
                    routeStop.EstimatedTravelMinutesFromPrevious = dto.EstimatedTravelMinutesFromPrevious.Value;

                if (dto.DwellMinutes.HasValue)
                    routeStop.DwellMinutes = dto.DwellMinutes.Value;

                routeStop.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Route stop updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating route stop {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // DELETE /api/routestops/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var routeStop = await _context.RouteStops
                    .Include(rs => rs.TripStops)
                    .FirstOrDefaultAsync(rs => rs.RouteStopId == id);

                if (routeStop == null)
                    return NotFound();

                // Prevent deletion if any TripStop references it (historical trips)
                if (routeStop.TripStops.Any())
                    return BadRequest(new { error = "Cannot delete route stop because it is referenced by existing trip stops" });

                _context.RouteStops.Remove(routeStop);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Route stop deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting route stop {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}