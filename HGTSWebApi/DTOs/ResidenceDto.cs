using System.ComponentModel.DataAnnotations;

namespace HGTSWebApi.DTOs
{
    public class ResidenceDto
    {
        public Guid ResidenceId { get; set; }
        public string ResidenceCode { get; set; } = string.Empty;
        public string ResidenceName { get; set; } = string.Empty;
        public Guid InstitutionId { get; set; }
        public string? InstitutionName { get; set; }
        public Guid? PickupZoneId { get; set; }
        public string? PickupZoneCode { get; set; }
        public string? PickupZoneName { get; set; }
        public int StudentCount { get; set; }
    }

    public class CreateResidenceDto
    {
        [Required]
        public string ResidenceCode { get; set; } = string.Empty;

        [Required]
        public string ResidenceName { get; set; } = string.Empty;

        [Required]
        public Guid InstitutionId { get; set; }

        public Guid? PickupZoneId { get; set; }
    }

    public class UpdateResidenceDto
    {
        public Guid? InstitutionId { get; set; }
        public string? ResidenceName { get; set; }
        public Guid? PickupZoneId { get; set; }
    }
}