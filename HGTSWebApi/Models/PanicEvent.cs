using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class PanicEvent
{
    public Guid PanicEventId { get; set; }

    //public Guid OrganizationId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? Details { get; set; }

    //public virtual Organization Organization { get; set; } = null!;

    public virtual ICollection<PanicChatMessage> PanicChatMessages { get; set; } = new List<PanicChatMessage>();
}
