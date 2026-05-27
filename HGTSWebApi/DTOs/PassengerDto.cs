namespace HGTSWebApi.DTOs
{
    // Profile
    public class PassengerProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ResidenceName { get; set; }
        public string? FacultyName { get; set; }
        public string? InstitutionName { get; set; }
        public bool HasCompletedOnboarding { get; set; }
    }

    // Onboarding - First step (verify email)
    public class OnboardVerifyRequestDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class OnboardVerifyResponseDto
    {
        public bool Found { get; set; }
        public bool IsReturningUser { get; set; }
        public string? Reason { get; set; }
        public PassengerProfileDto? Profile { get; set; }
    }

    // Onboarding - Complete (set password)
    public class OnboardCompleteRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string Password { get; set; } = string.Empty;
        public string DeviceToken { get; set; } = string.Empty;
    }

    public class OnboardCompleteResponseDto
    {
        public bool Success { get; set; }
        public bool IsReturningUser { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public PassengerProfileDto User { get; set; } = new();
    }

    // Feed
    public class PassengerTripDto
    {
        public string Id { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Eta { get; set; }
        public string Capacity { get; set; } = string.Empty;
        public string VehicleLabel { get; set; } = string.Empty;
    }

    // Credentials
    public class PassengerCredentialDto
    {
        public string Uid { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? RegisteredAt { get; set; }
    }

    public class RegisterPhoneCredentialDto
    {
        public string DeviceToken { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
    }

    // History
    public class PassengerHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Route { get; set; } = string.Empty;
        public string VehicleLabel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public bool AccessGranted { get; set; }
        public string? Message { get; set; }
    }
}