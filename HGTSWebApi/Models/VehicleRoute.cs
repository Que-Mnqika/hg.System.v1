using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HGTSWebApi.Models
{
    public class VehicleRoute
    {
        [Key]
        public Guid RouteId { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(20)]
        public string RouteCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string RouteName { get; set; } = string.Empty;

        [Required]
        public Guid InstitutionId { get; set; }

        public Guid? PickupZoneId { get; set; }
        public Guid? ResidenceId { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Institution? Institution { get; set; }
        public virtual PickupZone? PickupZone { get; set; }
        public virtual Residence? Residence { get; set; }

        // ADD THIS BACK - is being used in VehicleRouteController and TripController
        public virtual ICollection<Residence> Residences { get; set; } = new List<Residence>();

        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
        public virtual ICollection<Placement> Placements { get; set; } = new List<Placement>();
        public virtual ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
    }
}