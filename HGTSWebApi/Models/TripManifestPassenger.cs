using System;

namespace HGTSWebApi.Models
{
    public class TripManifestPassenger
    {
        public Guid TripManifestPassengerId { get; set; }
        public Guid TripManifestId { get; set; }
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public Guid? ResidenceId { get; set; }
        public bool HasBoarded { get; set; }
        public DateTime? BoardedAt { get; set; }
        public int? SeatNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual TripManifest TripManifest { get; set; } = null!;
        public virtual Student Student { get; set; } = null!;
        public virtual Residence? Residence { get; set; }
    }
}