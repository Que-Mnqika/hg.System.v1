namespace HGTSWebApi.Services
{
    public class IoTHubOptions
    {
        public string HostName { get; set; } = string.Empty;
        public string SharedAccessKeyName { get; set; } = string.Empty;
        public string SharedAccessKey { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = "2021-04-12";
        public int SasTokenTtlMinutes { get; set; } = 60;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(HostName)
            && !string.IsNullOrWhiteSpace(SharedAccessKeyName)
            && !string.IsNullOrWhiteSpace(SharedAccessKey);
    }
}
