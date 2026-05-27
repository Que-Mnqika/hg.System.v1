using System.Text.Json.Serialization;
namespace HGTSWebApi.Models
{
    public class VehicleAvailability
    {
        public Guid AvailabilityId { get; set; }
        
        public Guid VehicleId { get; set; }
        
        /// <summary>
        /// Status: Booked or OutOfService only
        /// Verdict: Remove Maintenance from VehicleAvailability - let WorkOrders own maintenance windows
        /// </summary>
        public string Status { get; set; } = null!; // Booked, OutOfService
        
        public DateTime StartTime { get; set; }
        
        public DateTime EndTime { get; set; }
        
        public string? Reason { get; set; }
        
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [JsonIgnore]
        public virtual Vehicle Vehicle { get; set; } = null!;
    }
}
