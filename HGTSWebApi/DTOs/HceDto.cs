namespace HGTSWebApi.DTOs
{
    // Request to register a phone
    public class RegisterPhoneRequestDto
    {
        public string DeviceIdentifier { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
    }

    // Response after phone registration
    public class HceTokenResponseDto
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public bool IsExisting { get; set; }
        public bool IsPrimary { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // Phone credential info
    public class PhoneCredentialDto
    {
        public Guid CredentialId { get; set; }
        public string CredentialUid { get; set; } = string.Empty;
        public string DeviceIdentifier { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime IssuedDate { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    // Request to switch primary phone
    public class SwitchPrimaryRequestDto
    {
        public string CredentialUid { get; set; } = string.Empty;
    }

    // Request to revoke a phone
    public class RevokePhoneRequestDto
    {
        public string CredentialUid { get; set; } = string.Empty;
    }

    // HCE validation request from ESP32
    public class HceValidationRequestDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    // HCE validation response to ESP32
    public class HceValidationResponseDto
    {
        public bool AccessGranted { get; set; }
        public string Message { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string ResultCode { get; set; } = string.Empty;
    }
}