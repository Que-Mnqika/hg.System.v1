using System.Text.Json.Serialization;
namespace HGTSWebApi.Models
{
    public class TripManifestCredential
    {
        public Guid TripManifestCredentialId { get; set; }
        public Guid TripManifestPassengerId { get; set; }
        public Guid CredentialId { get; set; }
        public string CredentialToken { get; set; } = null!;
        public string CredentialType { get; set; } = null!;
        public bool IsActive { get; set; } = true;

        [JsonIgnore]
        public virtual TripManifestPassenger TripManifestPassenger { get; set; } = null!;
        [JsonIgnore]
        public virtual NFCCredential Credential { get; set; } = null!;
    }
}