using System;
using System.Collections.Generic;

namespace HGTSWebApi.Models;

public partial class NFCCredential
{
    public Guid CredentialId { get; set; }

    public string CredentialUid { get; set; } = null!;

    public string CredentialType { get; set; } = null!;

    public Guid StudentId { get; set; }

    public DateTime IssuedDate { get; set; }

    public bool IsActive { get; set; }

    public string? DeviceIdentifier { get; set; }

    public string? DeviceModel { get; set; }

    public string? Platform { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public string? CardSerial { get; set; }

    public virtual ICollection<BoardingLog> BoardingLogs { get; set; } = new List<BoardingLog>();

    public virtual Student Student { get; set; } = null!;
}