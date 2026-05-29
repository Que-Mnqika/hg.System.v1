using System;

namespace HGTSWebApi.Models
{
    public class TripManifestCredential
    {
        public Guid TripManifestCredentialId { get; set; }
        public Guid TripManifestId { get; set; }
        public string CredentialUid { get; set; } = string.Empty;
        public string CredentialType { get; set; } = string.Empty;
        public Guid StudentId { get; set; }
        public string? FullName { get; set; }
        public string? StudentNumber { get; set; }
        public Guid? ResidenceId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual TripManifest TripManifest { get; set; } = null!;
        public virtual Student Student { get; set; } = null!;
        public virtual Residence? Residence { get; set; }
    }
}