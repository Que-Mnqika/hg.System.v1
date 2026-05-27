namespace HGTSWebApi.Services
{
    public class IoTHubDeviceRegistrationResult
    {
        public bool Enabled { get; set; }
        public bool Registered { get; set; }
        public string Status { get; set; } = "Disabled";
        public string Message { get; set; } = string.Empty;
        public string? HubHostName { get; set; }
        public string? DeviceId { get; set; }
        public string? ConnectionState { get; set; }
        public string? GenerationId { get; set; }
        public DateTime? LastActivityTimeUtc { get; set; }
    }
}
