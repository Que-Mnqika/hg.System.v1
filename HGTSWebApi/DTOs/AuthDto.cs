using System.ComponentModel.DataAnnotations;

namespace HGTSWebApi.DTOs
{
    // ================================================================
    // LOGIN DTOs (Both Admin and Student)
    // ================================================================

    public class LoginRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? StudentNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public string UserCategory { get; set; } = string.Empty;
        public bool IsAdminPortal { get; set; }
    }

    public class CurrentUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string UserCategory { get; set; } = string.Empty;
        public string? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public string PermissionLevel { get; set; } = "READ_WRITE";
        public bool IsAdminPortal { get; set; }
    }

    // ================================================================
    // DASHBOARD USER (Admin Portal) DTOs
    // ================================================================

    public class DashboardUserDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string UserCategory { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public string? Department { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class CreateDashboardUserRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string UserCategory { get; set; } = string.Empty;
        public string? OrganizationId { get; set; }
        public string? Department { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class UpdateDashboardUserRequestDto
    {
        public string? FullName { get; set; }
        public string? Role { get; set; }
        public string? UserCategory { get; set; }
        public bool? IsActive { get; set; }
        public string? OrganizationId { get; set; }
        public string? Department { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class DashboardAuthResponseDto
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public DashboardUserInfoDto? User { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime ExpireAt { get; set; }
    }

    public class DashboardUserInfoDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string UserCategory { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public Guid? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public string? Department { get; set; }
        public string? PhoneNumber { get; set; }
    }

    // ================================================================
    // STUDENT (Mobile App) DTOs
    // ================================================================

    public class StudentAuthResponseDto
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public StudentInfoDto? Student { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class StudentInfoDto
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? CellNumber { get; set; }
        public string? ResidenceName { get; set; }
        public string? FacultyName { get; set; }
        public string? InstitutionName { get; set; }
        public bool HasActiveCredential { get; set; }
    }

    public class RegisterStudentRequestDto
    {
        public string StudentNumber { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string Password { get; set; } = string.Empty;
    }

    // ================================================================
    // PASSWORD RESET DTOs
    // ================================================================

    public class ChangePasswordRequestDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ForgotPasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool EmailSent { get; set; }
        public string? MaskedEmail { get; set; }
    }

    public class VerifyOtpRequestDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;
    }

    public class VerifyOtpResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ResetToken { get; set; }
    }

    public class ResetPasswordRequestDto
    {
        [Required]
        public string ResetToken { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
