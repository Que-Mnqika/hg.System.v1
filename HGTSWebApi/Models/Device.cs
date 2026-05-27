using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class Device
{
    public Guid DeviceId { get; set; }

    public string DeviceIdentifier { get; set; } = null!;

    public string? DeviceName { get; set; }

    public string? FirmwareVersion { get; set; }

    public string? HardwareVersion { get; set; }

    public DateTime? LastSeen { get; set; }

    public bool IsOnline { get; set; }

    public string Status { get; set; } = null!;

    public DateTime RegisteredDate { get; set; }

    public string? Location { get; set; }

    public string? Description { get; set; }

    //public Guid? OrganizationId { get; set; }

    public int TripDurationHours { get; set; }

    public DateTime? LastConfigUpdate { get; set; }

    public bool ActivationMode { get; set; }

    public Guid? PendingStudentId { get; set; }

    public DateTime? ActivationModeExpiry { get; set; }

    //public virtual Organization? Organization { get; set; }

    public virtual Student? PendingStudent { get; set; }

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
