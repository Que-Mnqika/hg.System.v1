using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace HGTSWebApi.Services
{
    public class IoTHubDeviceRegistryService : IIoTHubDeviceRegistryService
    {
        private readonly HttpClient _httpClient;
        private readonly IoTHubOptions _options;
        private readonly ILogger<IoTHubDeviceRegistryService> _logger;

        public IoTHubDeviceRegistryService(
            HttpClient httpClient,
            IOptions<IoTHubOptions> options,
            ILogger<IoTHubDeviceRegistryService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<IoTHubDeviceRegistrationResult> EnsureDeviceRegisteredAsync(
            string deviceIdentifier,
            CancellationToken cancellationToken = default)
        {
            if (!_options.IsConfigured)
            {
                return new IoTHubDeviceRegistrationResult
                {
                    Enabled = false,
                    Registered = false,
                    Status = "Skipped",
                    Message = "IoT Hub settings are not configured."
                };
            }

            try
            {
                var requestUri = BuildRegistryUri(deviceIdentifier);
                using var request = new HttpRequestMessage(HttpMethod.Put, requestUri);
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(BuildSasToken(requestUri));

                var payload = new
                {
                    deviceId = deviceIdentifier,
                    status = "enabled",
                    authentication = new
                    {
                        type = "sas"
                    }
                };

                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "IoT Hub registration failed for {DeviceIdentifier}. Status: {StatusCode}. Body: {Body}",
                        deviceIdentifier,
                        (int)response.StatusCode,
                        body);

                    return new IoTHubDeviceRegistrationResult
                    {
                        Enabled = true,
                        Registered = false,
                        Status = "Failed",
                        Message = $"IoT Hub returned {(int)response.StatusCode}.",
                        HubHostName = _options.HostName,
                        DeviceId = deviceIdentifier
                    };
                }

                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                return new IoTHubDeviceRegistrationResult
                {
                    Enabled = true,
                    Registered = true,
                    Status = "Registered",
                    Message = "IoT Hub device identity is ready.",
                    HubHostName = _options.HostName,
                    DeviceId = root.TryGetProperty("deviceId", out var deviceIdValue) ? deviceIdValue.GetString() : deviceIdentifier,
                    ConnectionState = root.TryGetProperty("connectionState", out var connectionStateValue) ? connectionStateValue.GetString() : null,
                    GenerationId = root.TryGetProperty("generationId", out var generationIdValue) ? generationIdValue.GetString() : null,
                    LastActivityTimeUtc = root.TryGetProperty("lastActivityTime", out var lastActivityValue)
                        && DateTime.TryParse(lastActivityValue.GetString(), out var parsedLastActivity)
                        ? parsedLastActivity
                        : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IoT Hub registration threw for {DeviceIdentifier}", deviceIdentifier);
                return new IoTHubDeviceRegistrationResult
                {
                    Enabled = true,
                    Registered = false,
                    Status = "Error",
                    Message = ex.Message,
                    HubHostName = _options.HostName,
                    DeviceId = deviceIdentifier
                };
            }
        }

        private Uri BuildRegistryUri(string deviceIdentifier)
        {
            var escapedDeviceId = Uri.EscapeDataString(deviceIdentifier);
            return new Uri($"https://{_options.HostName}/devices/{escapedDeviceId}?api-version={_options.ApiVersion}");
        }

        private string BuildSasToken(Uri requestUri)
        {
            var expiry = DateTimeOffset.UtcNow.AddMinutes(_options.SasTokenTtlMinutes).ToUnixTimeSeconds();
            var resourceUri = $"{requestUri.Host}{requestUri.AbsolutePath}".TrimEnd('/').ToLowerInvariant();
            var encodedResourceUri = Uri.EscapeDataString(resourceUri);
            var stringToSign = $"{encodedResourceUri}\n{expiry}";

            using var hmac = new HMACSHA256(Convert.FromBase64String(_options.SharedAccessKey));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

            return $"SharedAccessSignature sr={encodedResourceUri}&sig={Uri.EscapeDataString(signature)}&se={expiry}&skn={Uri.EscapeDataString(_options.SharedAccessKeyName)}";
        }
    }
}
