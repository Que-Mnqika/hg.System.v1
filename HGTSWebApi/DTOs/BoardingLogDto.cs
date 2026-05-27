namespace HGTSWebApi.DTOs
{
    public class BoardingLogDto
    {
        public Guid LogId { get; set; }
        public Guid? CredentialId { get; set; }
        public string? CredentialToken { get; set; }
        public string? CredentialType { get; set; }
        public string? StudentName { get; set; }
        public string? StudentNumber { get; set; }
        public string   DeviceId { get; set; }
        public Guid? TripId { get; set; }
        public string? RouteName { get; set; }
        public DateTime ClientTimestamp { get; set; }
        public DateTime ServerTimestamp { get; set; }
        public bool AccessGranted { get; set; }
        public bool RouteMismatch { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? Message { get; set; }
        public bool IsOffline { get; set; }
        public Guid? ResidenceId { get; set; }
        public string? ResidenceName { get; set; }
    }

    public class BoardingLogListResponseDto
    {
        public List<BoardingLogDto> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}