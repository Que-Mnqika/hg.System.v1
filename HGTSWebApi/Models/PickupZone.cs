using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class PickupZone
{
    public Guid PickupZoneId { get; set; }

    public string PickupZoneCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public Guid InstitutionId { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<VehicleRoute> Routes { get; set; } = new List<VehicleRoute>();

    public virtual Institution Institution { get; set; } = null!;

    public virtual ICollection<Residence> Residences { get; set; } = new List<Residence>();
}
