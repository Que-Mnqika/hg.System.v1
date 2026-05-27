using System.Text.Json.Serialization;
namespace HGTSWebApi.Models
{
    public class TripManifest
    {
        public Guid TripManifestId { get; set; }
        public Guid TripId { get; set; }
        public int Version { get; set; }
        public string ManifestHash { get; set; } = null!;
        public string Status { get; set; } = "Draft";
        public int PassengerCount { get; set; }
        public int CredentialCount { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public string? PolicyProfile { get; set; }

        [JsonIgnore]
        public virtual Trip Trip { get; set; } = null!;
        [JsonIgnore]
        public virtual ICollection<TripManifestPassenger> Passengers { get; set; } = new List<TripManifestPassenger>();
    }
}