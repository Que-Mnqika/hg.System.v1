using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class StudentAuth
{
    public Guid AuthId { get; set; }
    public string? Username { get; set; }

    public Guid StudentId { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string? DeviceToken { get; set; }

    public DateTime? LastLogin { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? ResetToken { get; set; }

    public DateTime? ResetTokenExpiry { get; set; }

    public DateTime? PasswordUpdatedAt { get; set; }

    public virtual Student Student { get; set; } = null!;
}
