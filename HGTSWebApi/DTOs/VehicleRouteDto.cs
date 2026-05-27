using System.ComponentModel.DataAnnotations;

namespace HGTSWebApi.DTOs
{
    public class VehicleRouteDto
    {
        public Guid RouteId { get; set; }
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public Guid InstitutionId { get; set; }
        public string? InstitutionName { get; set; }
        public Guid? PickupZoneId { get; set; }
        public string? PickupZoneCode { get; set; }
        public string? PickupZoneName { get; set; }
        public Guid? ResidenceId { get; set; }
        public string? ResidenceCode { get; set; }
        public string? ResidenceName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        // These are the missing properties your controller is trying to use
        public int ResidenceCount { get; set; }
        public int TripCount { get; set; }
        public int PlacementCount { get; set; }
    }

    public class CreateVehicleRouteDto
    {
        [Required]
        public string RouteCode { get; set; } = string.Empty;

        [Required]
        public string RouteName { get; set; } = string.Empty;

        [Required]
        public Guid InstitutionId { get; set; }

        public Guid? PickupZoneId { get; set; }
        public Guid? ResidenceId { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateVehicleRouteDto
    {
        public string? RouteName { get; set; }
        public Guid? PickupZoneId { get; set; }
        public Guid? ResidenceId { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }
}