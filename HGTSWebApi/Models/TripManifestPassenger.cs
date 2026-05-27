using System.Text.Json.Serialization;
namespace HGTSWebApi.Models
{
    public class TripManifestPassenger
    {
        public Guid TripManifestPassengerId { get; set; }
        public Guid TripManifestId { get; set; }
        public Guid PassengerId { get; set; }
        public string PassengerName { get; set; } = null!;
        public string PassengerNumber { get; set; } = null!;
        public string SourceName { get; set; } = null!;
        public string SourceType { get; set; } = null!;
        public string ResidenceName { get; set; } = null!;
        public bool IsBlocked { get; set; }

        [JsonIgnore]
        public virtual TripManifest TripManifest { get; set; } = null!;
        [JsonIgnore]
        public virtual Student Passenger { get; set; } = null!;
        [JsonIgnore]
        public virtual ICollection<TripManifestCredential> Credentials { get; set; } = new List<TripManifestCredential>();
    }
}