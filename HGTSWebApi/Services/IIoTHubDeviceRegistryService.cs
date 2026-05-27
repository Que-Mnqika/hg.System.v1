namespace HGTSWebApi.Services
{
    public interface IIoTHubDeviceRegistryService
    {
        Task<IoTHubDeviceRegistrationResult> EnsureDeviceRegisteredAsync(
            string deviceIdentifier,
            CancellationToken cancellationToken = default);
    }
}
