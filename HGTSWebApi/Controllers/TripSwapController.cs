using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using HGTSWebApi.DTOs;
using HGTSWebApi.Services;

namespace HGTSWebApi.Controllers
{
    [ApiController]
    [Route("api/trips")]
    public class TripSwapController : ControllerBase
    {
        private readonly ITripSwapService _tripSwapService;
        private readonly ILogger<TripSwapController> _logger;

        public TripSwapController(ITripSwapService tripSwapService, ILogger<TripSwapController> logger)
        {
            _tripSwapService = tripSwapService;
            _logger = logger;
        }

        /// <summary>
        /// Swap a trip to a different Vehicle (for traffic, breakdown, or accident scenarios)
        /// </summary>
        /// <param name="tripId">The ID of the active trip to swap</param>
        /// <param name="request">Swap request containing new Vehicle ID and reason</param>
        /// <returns>Swap result with new trip ID and passenger count</returns>
        [HttpPost("{tripId}/swap")]
        [Authorize(Policy = "Operations")]
        public async Task<ActionResult<TripSwapResponseDto>> SwapTrip(Guid tripId, [FromBody] TripSwapRequestDto request)
        {
            try
            {
                _logger.LogInformation("Swap trip requested for {TripId} to Vehicle {VehicleId}, reason: {Reason}",
                    tripId, request.NewVehicleId, request.Reason);

                var result = await _tripSwapService.SwapTripAsync(tripId, request);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error swapping trip {TripId}", tripId);
                return StatusCode(500, new TripSwapResponseDto
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Emergency end a trip (for accidents or critical situations)
        /// </summary>
        /// <param name="tripId">The ID of the active trip to end</param>
        /// <param name="request">Emergency end request with reason</param>
        /// <returns>Success status</returns>
        [HttpPost("{tripId}/emergency-end")]
        [Authorize(Policy = "Operations")]
        public async Task<IActionResult> EmergencyEndTrip(Guid tripId, [FromBody] EmergencyEndRequestDto request)
        {
            try
            {
                _logger.LogWarning("Emergency end requested for trip {TripId}, reason: {Reason}", tripId, request.Reason);

                var result = await _tripSwapService.EmergencyEndTripAsync(tripId, request);

                if (!result)
                    return NotFound(new { message = "Trip not found or not in progress" });

                return Ok(new { message = "Trip emergency ended successfully", tripId = tripId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error emergency ending trip {TripId}", tripId);
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get transfer history for a specific trip
        /// </summary>
        /// <param name="tripId">The trip ID to get transfer history for</param>
        /// <returns>List of transfers associated with this trip</returns>
        [HttpGet("{tripId}/transfers")]
        public async Task<ActionResult<object>> GetTripTransfers(Guid tripId)
        {
            try
            {
                var transfers = await _tripSwapService.GetTripTransfersAsync(tripId);
                return Ok(new
                {
                    tripId = tripId,
                    transferCount = transfers.Count,
                    transfers = transfers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transfers for trip {TripId}", tripId);
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Check if a trip has been swapped
        /// </summary>
        /// <param name="tripId">The trip ID to check</param>
        /// <returns>Swap status</returns>
        [HttpGet("{tripId}/is-swapped")]
        public async Task<ActionResult<object>> IsTripSwapped(Guid tripId)
        {
            try
            {
                var isSwapped = await _tripSwapService.IsTripSwappedAsync(tripId);
                return Ok(new
                {
                    tripId = tripId,
                    isSwapped = isSwapped
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking swap status for trip {TripId}", tripId);
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }
    }
}