using HGTSWebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("api/realtime")]
    public class RealtimeController : ControllerBase
    {
        private readonly ICapacityNotificationService _capacityNotificationService;

        public RealtimeController(ICapacityNotificationService capacityNotificationService)
        {
            _capacityNotificationService = capacityNotificationService;
        }

        [HttpGet("capacity/{tripId:guid}")]
        public async Task<IActionResult> GetCapacitySnapshot(Guid tripId)
        {
            var snapshot = await _capacityNotificationService.GetSnapshotAsync(tripId);
            if (snapshot == null)
            {
                return NotFound(new { error = "No capacity snapshot available for this trip yet." });
            }

            return Ok(snapshot);
        }
    }
}
