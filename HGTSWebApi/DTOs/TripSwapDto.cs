using System;

namespace HGTSWebApi.DTOs
{
    // Request DTO for swapping a trip
    public class TripSwapRequestDto
    {
        public Guid NewVehicleId { get; set; }  // Changed from NewBusId
        public string Reason { get; set; } = string.Empty; // TRAFFIC, ACCIDENT, BREAKDOWN, ROUTE_CHANGE
        public string? Notes { get; set; }
        public Guid? TransferredBy { get; set; }
    }

    // Response DTO for trip swap
    public class TripSwapResponseDto
    {
        public bool Success { get; set; }
        public Guid NewTripId { get; set; }
        public int TransferredPassengers { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // DTO for trip transfer history
    public class TripTransferDto
    {
        public Guid TransferId { get; set; }
        public Guid OriginalTripId { get; set; }
        public string? OriginalRouteName { get; set; }
        public string? OriginalVehicleLabel { get; set; }  // Changed from OriginalBusLabel
        public Guid NewTripId { get; set; }
        public string? NewRouteName { get; set; }
        public string? NewVehicleLabel { get; set; }  // Changed from NewBusLabel
        public string Reason { get; set; } = string.Empty;
        public DateTime TransferredAt { get; set; }
        public int PassengerCount { get; set; }
        public string? TransferredBy { get; set; }
        public string? Notes { get; set; }
    }

    // DTO for emergency trip end
    public class EmergencyEndRequestDto
    {
        public string Reason { get; set; } = string.Empty;
        public Guid? EndedBy { get; set; }
    }

    // DTO for active trip with swap info
    public class ActiveTripWithSwapInfoDto : ActiveTripResponseDto
    {
        public bool IsSwapped { get; set; }
        public Guid? OriginalTripId { get; set; }
        public string? SwapReason { get; set; }
        public DateTime? SwappedAt { get; set; }
    }
}