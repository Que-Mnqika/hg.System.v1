using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HGTSWebApi.DTOs
{
    public class PickupZoneDto
    {
        public Guid PickupZoneId { get; set; }
        public string PickupZoneCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Guid InstitutionId { get; set; }
        public string? InstitutionName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int RouteCount { get; set; }
    }

    public class CreatePickupZoneDto
    {
        [Required]
        public string PickupZoneCode { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public Guid InstitutionId { get; set; }

        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdatePickupZoneDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }
}