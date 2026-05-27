using HGTSWebApi.DTOs;

namespace HGTSWebApi.Services
{
    public interface IAuthService
    {
        // Dashboard authentication
        Task<DashboardAuthResponseDto?> AuthenticateDashboardAsync(string username, string password);

        // Student mobile authentication
        Task<StudentAuthResponseDto?> AuthenticateStudentAsync(string studentNumber, string password, string? deviceToken);

        // Dev login (bypass for testing)
        Task<object?> DevAuthenticateAsync(string username, string userType);

        // Get current user info
        Task<DashboardUserInfoDto?> GetDashboardUserInfoAsync(Guid userId);
        Task<StudentInfoDto?> GetStudentInfoAsync(Guid studentId);

        // Password management
        Task<bool> ChangeDashboardPasswordAsync(Guid userId, string currentPassword, string newPassword);
        Task<bool> ChangeStudentPasswordAsync(Guid studentId, string currentPassword, string newPassword);

        // 🔥 ADD THESE PASSWORD RESET METHODS
        Task<ForgotPasswordResponseDto> ForgotPasswordAsync(string email);
        Task<VerifyOtpResponseDto> VerifyOtpAsync(string email, string otp);
        Task<ResetPasswordResponseDto> ResetPasswordAsync(string resetToken, string newPassword);

        // Password hashing
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }
}