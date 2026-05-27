using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class Residence
{
    public Guid ResidenceId { get; set; }

    public string ResidenceName { get; set; } = null!;

    public string ResidenceCode { get; set; } = null!;

    public Guid InstitutionId { get; set; }

    public Guid? PickupZoneId { get; set; }

    public Guid? RouteId { get; set; }

    public string? Timezone { get; set; } = "UTC";  // ADD THIS PROPERTY

    public virtual Institution Institution { get; set; } = null!;

    public virtual PickupZone? PickupZone { get; set; }

    public virtual VehicleRoute? Route { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}