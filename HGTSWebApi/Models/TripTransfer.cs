using System;

namespace HGTSWebApi.Models;

public partial class TripTransfer
{
    public Guid TransferId { get; set; }
    public Guid OriginalTripId { get; set; }
    public Guid NewTripId { get; set; }
    public string DeviceId { get; set; } = null!;
    public Guid VehicleId { get; set; }  // Changed from BusId
    public string Reason { get; set; } = null!;
    public DateTime TransferredAt { get; set; }
    public Guid? TransferredBy { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Vehicle Vehicle { get; set; } = null!;  // Changed from Bus
    public virtual Trip NewTrip { get; set; } = null!;
    public virtual Trip OriginalTrip { get; set; } = null!;
    public virtual DashboardUser? TransferredByNavigation { get; set; }
}