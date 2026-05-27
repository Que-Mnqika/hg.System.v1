using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class Placement
{
    public Guid PlacementId { get; set; }

    public Guid StudentId { get; set; }

    public string LocationName { get; set; } = null!;

    public string? LocationAddress { get; set; }

    public string PlacementType { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public Guid? RouteId { get; set; }

    public string Status { get; set; } = null!;

    public virtual VehicleRoute? Route { get; set; }

    public virtual Student Student { get; set; } = null!;
}
