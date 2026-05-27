namespace HGTSWebApi.DTOs
{
    public class BoardingLogResponseDto
    {
        public Guid LogId { get; set; }
        public string CredentialUid { get; set; } = string.Empty;
        public string CredentialType { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public string? StudentNumber { get; set; }
        public int DeviceId { get; set; }
        public Guid? TripId { get; set; }
        public DateTime ClientTimestamp { get; set; }
        public DateTime ServerTimestamp { get; set; }
        public bool AccessGranted { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? Message { get; set; }
        public bool IsOffline { get; set; }
        public Guid? ResidenceId { get; set; }
        public string? ResidenceName { get; set; }
        public string FormattedDate => ClientTimestamp.ToString("yyyy-MM-dd HH:mm:ss");
    }
}