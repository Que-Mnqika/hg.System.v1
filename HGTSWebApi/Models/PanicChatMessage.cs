using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class PanicChatMessage
{
    public Guid PanicChatMessageId { get; set; }

    public Guid PanicEventId { get; set; }

    public Guid SenderId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public virtual PanicEvent PanicEvent { get; set; } = null!;
}
