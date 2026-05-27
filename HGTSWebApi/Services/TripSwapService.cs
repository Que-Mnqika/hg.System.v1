using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;

namespace HGTSWebApi.Services
{
    public class TripSwapService : ITripSwapService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TripSwapService> _logger;

        public TripSwapService(AppDbContext context, ILogger<TripSwapService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TripSwapResponseDto> SwapTripAsync(Guid originalTripId, TripSwapRequestDto request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Get original trip
                var originalTrip = await _context.Trips
                    .Include(t => t.BusRoute)
                    .Include(t => t.Vehicle)
                    .FirstOrDefaultAsync(t => t.TripId == originalTripId && t.Status == "InProgress");

                if (originalTrip == null)
                {
                    return new TripSwapResponseDto
                    {
                        Success = false,
                        Message = "Original trip not found or not in progress"
                    };
                }

                // 2. Get the replacement vehicle
                var newVehicle = await _context.Vehicles.FindAsync(request.NewVehicleId);
                if (newVehicle == null || newVehicle.Status != "Active")
                {
                    return new TripSwapResponseDto
                    {
                        Success = false,
                        Message = "Replacement vehicle not available"
                    };
                }

                // 3. Get the device
                var device = await _context.Devices.FindAsync(originalTrip.DeviceId);
                if (device == null)
                {
                    return new TripSwapResponseDto
                    {
                        Success = false,
                        Message = "Device not found"
                    };
                }

                // 4. Count passengers who boarded this trip
                var passengerCount = await _context.BoardingLogs
                    .CountAsync(l => l.TripId == originalTrip.TripId && l.Allowed == true);

                // FIX: Calculate remaining duration safely with null checks
                TimeSpan remainingTime = TimeSpan.FromHours(1); // Default 1 hour
                if (originalTrip.EndTime.HasValue && originalTrip.StartTime.HasValue)
                {
                    var originalDuration = originalTrip.EndTime.Value - originalTrip.StartTime.Value;
                    var elapsedTime = DateTime.UtcNow - originalTrip.StartTime.Value;
                    remainingTime = originalDuration - elapsedTime;
                    if (remainingTime <= TimeSpan.Zero)
                        remainingTime = TimeSpan.FromHours(1);
                }

                // 6. Create new trip for the replacement vehicle
                var newTrip = new Trip
                {
                    TripId = Guid.NewGuid(),
                    RouteId = originalTrip.RouteId,
                    DeviceId = originalTrip.DeviceId,
                    VehicleId = request.NewVehicleId,
                    ResidenceId = originalTrip.ResidenceId,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.Add(remainingTime),
                    Status = "InProgress",
                    SwappedFromTripId = originalTrip.TripId
                };

                _context.Trips.Add(newTrip);

                // 7. Mark original trip as Swapped
                originalTrip.Status = "Swapped";
                originalTrip.EndTime = DateTime.UtcNow;
                originalTrip.SwappedToTripId = newTrip.TripId;

                // 8. Record the transfer
                var transfer = new TripTransfer
                {
                    TransferId = Guid.NewGuid(),
                    OriginalTripId = originalTrip.TripId,
                    NewTripId = newTrip.TripId,
                    DeviceId = device.DeviceIdentifier,
                    VehicleId = request.NewVehicleId,
                    Reason = request.Reason,
                    TransferredAt = DateTime.UtcNow,
                    TransferredBy = request.TransferredBy,
                    Notes = request.Notes
                };
                _context.TripTransfers.Add(transfer);

                // 9. Update boarding logs to associate with new trip
                var boardingLogs = await _context.BoardingLogs
                    .Where(l => l.TripId == originalTrip.TripId && l.Allowed == true)
                    .ToListAsync();

                foreach (var log in boardingLogs)
                {
                    log.TripId = newTrip.TripId;
                    log.IsTransferred = true;
                    log.TransferReason = request.Reason;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Trip swap completed: {OriginalTripId} -> {NewTripId}, Passengers: {Count}, Reason: {Reason}",
                    originalTrip.TripId, newTrip.TripId, passengerCount, request.Reason);

                return new TripSwapResponseDto
                {
                    Success = true,
                    NewTripId = newTrip.TripId,
                    TransferredPassengers = passengerCount,
                    Message = $"Trip swapped successfully. {passengerCount} passengers transferred to {newVehicle.RegistrationNumber}."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during trip swap for {TripId}", originalTripId);
                return new TripSwapResponseDto
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<bool> EmergencyEndTripAsync(Guid tripId, EmergencyEndRequestDto request)
        {
            var trip = await _context.Trips
                .Include(t => t.BoardingLogs)
                .FirstOrDefaultAsync(t => t.TripId == tripId && t.Status == "InProgress");

            if (trip == null) return false;

            trip.Status = "EmergencyEnded";
            trip.EndTime = DateTime.UtcNow;
            trip.IsEmergencyEnded = true;
            trip.EmergencyReason = request.Reason;

            // Mark all boarding logs for this trip as emergency ended
            foreach (var log in trip.BoardingLogs)
            {
                log.Reason = $"Emergency ended: {request.Reason}";
            }

            await _context.SaveChangesAsync();
            _logger.LogWarning("Trip {TripId} emergency ended. Reason: {Reason}", tripId, request.Reason);

            return true;
        }

        public async Task<List<TripTransferDto>> GetTripTransfersAsync(Guid tripId)
        {
            var transfers = await _context.TripTransfers
                .Include(t => t.OriginalTrip)
                    .ThenInclude(ot => ot != null ? ot.BusRoute : null)
                .Include(t => t.OriginalTrip)
                    .ThenInclude(ot => ot != null ? ot.Vehicle : null)
                .Include(t => t.NewTrip)
                    .ThenInclude(nt => nt != null ? nt.BusRoute : null)
                .Include(t => t.NewTrip)
                    .ThenInclude(nt => nt != null ? nt.Vehicle : null)
                .Include(t => t.TransferredByNavigation)
                .Where(t => t.OriginalTripId == tripId || t.NewTripId == tripId)
                .Select(t => new TripTransferDto
                {
                    TransferId = t.TransferId,
                    OriginalTripId = t.OriginalTripId,
                    OriginalRouteName = t.OriginalTrip != null && t.OriginalTrip.BusRoute != null ? t.OriginalTrip.BusRoute.RouteName : null,
                    OriginalVehicleLabel = t.OriginalTrip != null && t.OriginalTrip.Vehicle != null ? t.OriginalTrip.Vehicle.RegistrationNumber : null,
                    NewTripId = t.NewTripId,
                    NewRouteName = t.NewTrip != null && t.NewTrip.BusRoute != null ? t.NewTrip.BusRoute.RouteName : null,
                    NewVehicleLabel = t.NewTrip != null && t.NewTrip.Vehicle != null ? t.NewTrip.Vehicle.RegistrationNumber : null,
                    Reason = t.Reason,
                    TransferredAt = t.TransferredAt,
                    PassengerCount = _context.BoardingLogs.Count(l => l.TripId == t.NewTripId && l.IsTransferred == true),
                    TransferredBy = t.TransferredByNavigation != null ? t.TransferredByNavigation.FullName : null,
                    Notes = t.Notes
                })
                .ToListAsync();

            return transfers;
        }

        public async Task<ActiveTripWithSwapInfoDto> GetActiveTripWithSwapInfoAsync(string deviceId)
        {
            try
            {
                var device = await _context.Devices
                    .FirstOrDefaultAsync(d => d.DeviceIdentifier == deviceId || d.DeviceId.ToString() == deviceId);

                if (device == null)
                {
                    return new ActiveTripWithSwapInfoDto
                    {
                        HasActiveTrip = false,
                        Message = "Device not found"
                    };
                }

                var now = DateTime.UtcNow;
                var activeTrip = await _context.Trips
                    .Include(t => t.BusRoute)
                    .Include(t => t.Residence)
                    .Include(t => t.Vehicle)
                    .FirstOrDefaultAsync(t => t.DeviceId == device.DeviceId
                        && t.Status == "InProgress"
                        && t.StartTime <= now
                        && (!t.EndTime.HasValue || t.EndTime > now));

                if (activeTrip == null)
                {
                    return new ActiveTripWithSwapInfoDto
                    {
                        HasActiveTrip = false,
                        Message = "No active trip for this device"
                    };
                }

                // Check if this trip was swapped from another trip
                bool isSwapped = activeTrip.SwappedFromTripId.HasValue;
                Guid? originalTripId = activeTrip.SwappedFromTripId;
                string? swapReason = null;
                DateTime? swappedAt = null;

                if (isSwapped)
                {
                    var transfer = await _context.TripTransfers
                        .FirstOrDefaultAsync(t => t.NewTripId == activeTrip.TripId);
                    if (transfer != null)
                    {
                        swapReason = transfer.Reason;
                        swappedAt = transfer.TransferredAt;
                    }
                }

                // FIX: Safe calculation of duration hours with null checks
                int durationHours = 1;
                if (activeTrip.EndTime.HasValue && activeTrip.StartTime.HasValue)
                {
                    var duration = activeTrip.EndTime.Value - activeTrip.StartTime.Value;
                    durationHours = (int)Math.Ceiling(duration.TotalHours);
                }
                if (durationHours <= 0) durationHours = 1;

                return new ActiveTripWithSwapInfoDto
                {
                    HasActiveTrip = true,
                    TripId = activeTrip.TripId.ToString(),
                    RouteId = activeTrip.RouteId.ToString(),
                    RouteName = activeTrip.BusRoute?.RouteName ?? "Unknown Route",
                    ResidenceId = activeTrip.ResidenceId?.ToString(),
                    ResidenceName = activeTrip.Residence?.ResidenceName ?? "Unknown Residence",
                    DurationHours = durationHours,
                    StartTime = activeTrip.StartTime,
                    EndTime = activeTrip.EndTime,
                    IsSwapped = isSwapped,
                    OriginalTripId = originalTripId,
                    SwapReason = swapReason,
                    SwappedAt = swappedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetActiveTripWithSwapInfoAsync for device {DeviceId}", deviceId);
                return new ActiveTripWithSwapInfoDto
                {
                    HasActiveTrip = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<bool> IsTripSwappedAsync(Guid tripId)
        {
            var trip = await _context.Trips.FindAsync(tripId);
            if (trip == null) return false;
            return trip.SwappedFromTripId.HasValue || trip.Status == "Swapped";
        }
    }
}