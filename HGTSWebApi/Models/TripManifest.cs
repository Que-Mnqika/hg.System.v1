using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models
{
    public class TripManifest
    {
        public Guid TripManifestId { get; set; }
        public Guid TripId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DownloadedAt { get; set; }
        public int TotalPassengers { get; set; }
        public string Status { get; set; } = "Active"; // Active, Expired, Downloaded

        // Navigation properties
        public virtual Trip Trip { get; set; } = null!;
        public virtual ICollection<TripManifestCredential> Credentials { get; set; } = new List<TripManifestCredential>();
        public virtual ICollection<TripManifestPassenger> Passengers { get; set; } = new List<TripManifestPassenger>();
    }
}