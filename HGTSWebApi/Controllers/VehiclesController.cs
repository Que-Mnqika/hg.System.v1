using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HGTSWebApi.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/vehicles")]
    public class VehiclesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VehiclesController> _logger;

        public VehiclesController(AppDbContext context, ILogger<VehiclesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/vehicles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleDto>>> GetVehicles([FromQuery] string? orgId, [FromQuery] string? status)
        {
            try
            {
                var query = _context.Vehicles
                    .Include(b => b.Trips)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(b => b.Status == status);

                var vehicles = await query
                    .OrderBy(b => b.VehicleName)
                    .Select(b => new VehicleDto
                    {
                        VehicleId = b.VehicleId,
                        VehicleName = b.VehicleName,
                        RegistrationNumber = b.RegistrationNumber,
                        Capacity = b.Capacity,
                        Status = b.Status,
                        TripCount = b.Trips.Count,
                        ActiveTripCount = b.Trips.Count(t => t.Status == "InProgress")
                    })
                    .ToListAsync();

                return Ok(vehicles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicles");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET /api/vehicles/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<VehicleDto>> GetVehicle(Guid id)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(b => b.Trips)
                    .FirstOrDefaultAsync(b => b.VehicleId == id);

                if (vehicle == null)
                    return NotFound();

                return Ok(new VehicleDto
                {
                    VehicleId = vehicle.VehicleId,
                    VehicleName = vehicle.VehicleName,
                    RegistrationNumber = vehicle.RegistrationNumber,
                    Capacity = vehicle.Capacity,
                    Status = vehicle.Status,
                    TripCount = vehicle.Trips.Count,
                    ActiveTripCount = vehicle.Trips.Count(t => t.Status == "InProgress")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicle");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /api/vehicles
        [HttpPost]
        //[Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<VehicleDto>> CreateVehicle([FromBody] CreateVehicleDto dto)
        {
            try
            {
                var vehicle = new Vehicle
                {
                    VehicleId = Guid.NewGuid(),
                    VehicleName = dto.VehicleName,
                    RegistrationNumber = dto.RegistrationNumber,
                    Capacity = dto.Capacity,
                    Status = dto.Status ?? "Active"
                };

                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();

                return Ok(new VehicleDto
                {
                    VehicleId = vehicle.VehicleId,
                    VehicleName = dto.VehicleName,
                    RegistrationNumber = vehicle.RegistrationNumber,
                    Status = vehicle.Status,
                    Capacity=vehicle.Capacity,
                    TripCount = 0,
                    ActiveTripCount = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vehicle");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // PATCH /api/vehicles/{id}
        [HttpPatch("{id:guid}")]
        //[Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateVehicle(Guid id, [FromBody] UpdateVehicleDto dto)
        {
            try
            {
                var vehicle = await _context.Vehicles.FindAsync(id);
                if (vehicle == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(dto.RegistrationNumber))
                    vehicle.RegistrationNumber = dto.RegistrationNumber;

                if (!string.IsNullOrEmpty(dto.VehicleName))
                    vehicle.VehicleName = dto.VehicleName;

                if (!dto.Capacity.Equals(0))
                    vehicle.Capacity = dto.Capacity;

                if (!string.IsNullOrEmpty(dto.Status))
                    vehicle.Status = dto.Status;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Vehicle updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vehicle");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // DELETE /api/vehicles/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteVehicle(Guid id)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(b => b.Trips)
                    .FirstOrDefaultAsync(b => b.VehicleId == id);

                if (vehicle == null)
                    return NotFound();

                if (vehicle.Trips.Any())
                    return BadRequest(new { error = "Cannot delete vehicle with existing trips" });

                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Vehicle deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vehicle");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}