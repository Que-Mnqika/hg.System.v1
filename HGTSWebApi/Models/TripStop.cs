using System;
using System.Text.Json.Serialization;

namespace HGTSWebApi.Models
{
    public class TripStop
    {
        public Guid TripStopId { get; set; }
        public Guid TripId { get; set; }
        public Guid RouteStopId { get; set; }           // Link back to the planned stop

        // Denormalized fields (frozen at trip creation)
        public string StopName { get; set; } = null!;    // Copied from Residence.Name
        public string Address { get; set; } = null!;     // Copied from Residence.Address
        public decimal Latitude { get; set; }            // Copied from Residence.Latitude
        public decimal Longitude { get; set; }           // Copied from Residence.Longitude
        public int StopOrder { get; set; }

        // Schedule (planned)
        public DateTime PlannedArrivalTime { get; set; }
        public DateTime? PlannedDepartureTime { get; set; }

        // Actuals (recorded during trip)
        public DateTime? ActualArrivalTime { get; set; }
        public DateTime? ActualDepartureTime { get; set; }
        public int PassengersBoarded { get; set; }
        public int PassengersAlighted { get; set; }

        // Status of this stop for this trip
        public string Status { get; set; } = "Pending"; // Pending, Completed, Skipped

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [JsonIgnore]
        public virtual Trip Trip { get; set; } = null!;
        public virtual RouteStop RouteStop { get; set; } = null!;
    }
}