using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class Institution
{
    public Guid InstitutionId { get; set; }

    public string InstitutionName { get; set; } = null!;

    public string InstitutionCode { get; set; } = null!;

    public string? CampusName { get; set; }

    public string Status { get; set; } = null!;

    public Guid? OrganizationId { get; set; }

    public virtual ICollection<VehicleRoute> Routes { get; set; } = new List<VehicleRoute>();

    public virtual ICollection<Faculty> Faculties { get; set; } = new List<Faculty>();

    public virtual Organization? Organization { get; set; }

    public virtual ICollection<PickupZone> PickupZones { get; set; } = new List<PickupZone>();

    public virtual ICollection<Residence> Residences { get; set; } = new List<Residence>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
