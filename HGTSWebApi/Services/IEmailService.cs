namespace HGTSWebApi.Services
{
    public interface IEmailService
    {
        Task<bool> SendOtpEmailAsync(string email, string otp, string studentName);
    }
}