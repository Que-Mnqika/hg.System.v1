using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class Vehicle
{
    public Guid VehicleId { get; set; }
    public string? VehicleName { get; set; }

    public string RegistrationNumber { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int Capacity { get; set; } = 50;  // ADD THIS PROPERTY

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}