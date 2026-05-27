using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.Models;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("/routes/no-go-zones")]
    public class NoGoZoneController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NoGoZoneController> _logger;

        public NoGoZoneController(AppDbContext context, ILogger<NoGoZoneController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var zones = await _context.NoGoZones
                .Select(z => new {
                    id = z.NoGoZoneId,
                    name = string.IsNullOrEmpty(z.Name) ? z.Reason ?? "Unnamed" : z.Name,
                    reason = z.Reason,
                    geometry = z.Geometry,
                    isActive = z.IsActive
                })
                .ToListAsync();

            return Ok(zones);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NoGoZone request)
        {
            if (request == null)
                return BadRequest();

            // Ensure name persistence: if name is empty, set from reason
            if (string.IsNullOrEmpty(request.Name) && !string.IsNullOrEmpty(request.Reason))
                request.Name = request.Reason;

            request.NoGoZoneId = Guid.NewGuid();
            _context.NoGoZones.Add(request);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), new { id = request.NoGoZoneId }, request);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var zone = await _context.NoGoZones.FindAsync(id);
            if (zone == null)
                return NotFound();

            _context.NoGoZones.Remove(zone);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted" });
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> Patch(Guid id, [FromBody] NoGoZone update)
        {
            var zone = await _context.NoGoZones.FindAsync(id);
            if (zone == null)
                return NotFound();

            if (!string.IsNullOrEmpty(update.Name))
                zone.Name = update.Name;
            if (!string.IsNullOrEmpty(update.Reason))
                zone.Reason = update.Reason;
            if (!string.IsNullOrEmpty(update.Geometry))
                zone.Geometry = update.Geometry;

            if (update.IsActive != zone.IsActive)
                zone.IsActive = update.IsActive;

            await _context.SaveChangesAsync();
            return Ok(zone);
        }
    }
}
