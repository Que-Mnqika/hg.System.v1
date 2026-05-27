namespace HGTSWebApi.DTOs
{
    // Response for getting credentials
    public class CredentialDto
    {
        public Guid CredentialId { get; set; }
        public string CredentialUid { get; set; } = string.Empty;
        public string CredentialType { get; set; } = string.Empty; // "PHONE" or "CARD"
        public bool IsActive { get; set; }
        public DateTime IssuedDate { get; set; }
        public string? CardSerial { get; set; }
        public string? DeviceModel { get; set; }
        public DateTime? LastSeenAt { get; set; }
    }

    // Request to assign phone credential
    public class AssignPhoneCredentialRequestDto
    {
        public string DeviceIdentifier { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty; // "ANDROID" or "IOS"
    }

    // Response after assigning phone credential
    public class AssignPhoneCredentialResponseDto
    {
        public bool Success { get; set; }
        public string CredentialToken { get; set; } = string.Empty;
        public bool IsNewCredential { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // Request to rebind phone (same token, new phone)
    public class RebindPhoneRequestDto
    {
        public string DeviceIdentifier { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
    }

    // Request to toggle credential active status
    public class ToggleCredentialRequestDto
    {
        public Guid CredentialId { get; set; }
    }

    // Request to add physical card
    public class AddCardCredentialRequestDto
    {
        public string CredentialToken { get; set; } = string.Empty; // The physical card UID
        public string? CardSerial { get; set; }
    }
}