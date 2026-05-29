using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models
{
    public class VehicleAvailability
    {
        public Guid VehicleAvailabilityId { get; set; }
        public Guid VehicleId { get; set; }
        public DateTime Date { get; set; }
        public bool IsAvailable { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Vehicle Vehicle { get; set; } = null!;
    }
}