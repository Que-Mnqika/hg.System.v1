using System.ComponentModel.DataAnnotations;

namespace HGTSWebApi.DTOs
{
    public class NFCCredentialDto
    {
        public Guid CredentialId { get; set; }
        public string CredentialUid { get; set; } = string.Empty;
        public string CredentialType { get; set; } = string.Empty;
        public Guid StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentNumber { get; set; }
        public DateTime IssuedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateNFCCredentialDto
    {
        [Required]
        public string CredentialUid { get; set; } = string.Empty;

        [Required]
        [StringLength(20, MinimumLength = 1)]
        public string CredentialType { get; set; } = string.Empty;

        [Required]
        public Guid StudentId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateNFCCredentialDto
    {
        public string? CredentialType { get; set; }
        public bool IsActive { get; set; }
    }
}