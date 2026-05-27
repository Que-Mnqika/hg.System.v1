using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class BoardingLog
{
    public Guid LogId { get; set; }

    public Guid? CredentialId { get; set; }

    public Guid? TripId { get; set; }

    public string DeviceId { get; set; } = null!;

    public string? CredentialUid { get; set; }

    public DateTime ClientTimestamp { get; set; }

    public DateTime ServerTimestamp { get; set; }

    public bool Allowed { get; set; }

    public bool RouteMismatch { get; set; }

    public string Result { get; set; } = null!;

    public string? Reason { get; set; }

    public bool IsOffline { get; set; }

    public bool IsTransferred { get; set; }

    public string? TransferReason { get; set; }

    public DateTime? BoardedAt { get; set; }

    public virtual NFCCredential? Credential { get; set; }

    public virtual Trip? Trip { get; set; }
}