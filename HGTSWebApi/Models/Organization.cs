using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models
{
    public class Organization
    {
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string Timezone { get; set; } = "UTC";
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<DashboardUser> DashboardUsers { get; set; } = new List<DashboardUser>();
        public virtual ICollection<Institution> Institutions { get; set; } = new List<Institution>();
        public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
    }
}