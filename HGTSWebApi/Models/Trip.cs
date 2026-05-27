using System;
using System.Collections;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class Trip
{
    public Guid TripId { get; set; }

    public Guid RouteId { get; set; }

    public Guid DeviceId { get; set; }

    public Guid VehicleId { get; set; }  // Kept as VehicleId

    public Guid? ResidenceId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    //When the trip ACTUALLY started(first tap or manual start)
    public DateTime? ActualStartTime { get; set; }

    public string Status { get; set; } = null!;

    public bool IsEmergencyEnded { get; set; }

    public string? EmergencyReason { get; set; }

    public Guid? SwappedToTripId { get; set; }

    public Guid? SwappedFromTripId { get; set; }
    public DateTime? ScheduledStartTime { get; set; }  // e.g. 17:00
    public DateTime? ScheduledEndTime { get; set; }    // e.g. 17:30

    public virtual ICollection<BoardingLog> BoardingLogs { get; set; } = new List<BoardingLog>();

    public virtual Vehicle Vehicle { get; set; } = null!;  // Changed from Bus to Vehicle

    public virtual Device Device { get; set; } = null!;

    public virtual ICollection<Trip> InverseSwappedFromTrip { get; set; } = new List<Trip>();

    public virtual ICollection<Trip> InverseSwappedToTrip { get; set; } = new List<Trip>();

    public virtual Residence? Residence { get; set; }

    public virtual VehicleRoute BusRoute { get; set; } = null!;

    public virtual Trip? SwappedFromTrip { get; set; }

    public virtual Trip? SwappedToTrip { get; set; }

    public virtual ICollection<TripTransfer> TripTransferNewTrips { get; set; } = new List<TripTransfer>();

    public virtual ICollection<TripTransfer> TripTransferOriginalTrips { get; set; } = new List<TripTransfer>();
    public virtual ICollection<TripStop> TripStops { get; set; } = new List<TripStop>();

}