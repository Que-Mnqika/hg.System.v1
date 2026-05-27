using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class NoGoZone
{
    public Guid NoGoZoneId { get; set; }

    //public Guid OrganizationId { get; set; }

    public string? Reason { get; set; }

    public string? Name { get; set; }

    public string? Geometry { get; set; }

    public bool IsActive { get; set; }

    //public virtual Organization Organization { get; set; } = null!;
}
