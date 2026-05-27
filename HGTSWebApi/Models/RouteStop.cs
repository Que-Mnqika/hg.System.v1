using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models
{
    public class RouteStop
    {
        public Guid RouteStopId { get; set; }
        public Guid RouteId { get; set; }          // FK to VehicleRoute
        public Guid ResidenceId { get; set; }      // FK to Residence
        public int StopOrder { get; set; }         // 1,2,3...
        public int EstimatedTravelMinutesFromPrevious { get; set; } // from previous stop to this one (0 for first)
        public int DwellMinutes { get; set; } = 2; // time spent at stop (boarding/alighting)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual VehicleRoute Route { get; set; } = null!;
        public virtual Residence Residence { get; set; } = null!;
        public virtual ICollection<TripStop> TripStops { get; set; } = new List<TripStop>();
    }
}