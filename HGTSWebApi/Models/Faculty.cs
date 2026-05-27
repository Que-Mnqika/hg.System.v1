using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class Faculty
{
    public Guid FacultyId { get; set; }

    public string FacultyName { get; set; } = null!;

    public Guid InstitutionId { get; set; }

    public virtual Institution Institution { get; set; } = null!;

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
