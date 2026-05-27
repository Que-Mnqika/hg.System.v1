using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class Student
{
    public Guid StudentId { get; set; }

    public string FullName { get; set; } = null!;

    public string StudentNumber { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? CellNumber { get; set; }

    public Guid InstitutionId { get; set; }

    public Guid? ResidenceId { get; set; }

    public Guid? FacultyId { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();

    public virtual Faculty? Faculty { get; set; }

    public virtual Institution Institution { get; set; } = null!;

    public virtual ICollection<NFCCredential> Credentials { get; set; } = new List<NFCCredential>();

    public virtual ICollection<Placement> Placements { get; set; } = new List<Placement>();

    public virtual Residence? Residence { get; set; }

    public virtual StudentAuth? StudentAuth { get; set; }
}
