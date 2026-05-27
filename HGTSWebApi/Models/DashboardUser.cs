using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class DashboardUser
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? UserCategory { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLogin { get; set; }

    public string? Department { get; set; }

    public string? PhoneNumber { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }

    //public Guid? OrganizationId { get; set; }

    //public virtual Organization? Organization { get; set; }

    public virtual ICollection<TripTransfer> TripTransferDashboardUserUsers { get; set; } = new List<TripTransfer>();

    public virtual ICollection<TripTransfer> TripTransferTransferredByNavigations { get; set; } = new List<TripTransfer>();
}
