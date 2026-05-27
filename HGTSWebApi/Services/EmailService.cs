using System.Net;
using System.Net.Mail;

namespace HGTSWebApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendOtpEmailAsync(string email, string otp, string studentName)
        {
            try
            {
                // For development/testing, just log the OTP
                _logger.LogInformation("📧 OTP for {Email}: {Otp}", email, otp);

                // For production, you would use an email service like SendGrid, SMTP, etc.
                // Example SMTP (configure in appsettings.json):

                // var smtpClient = new SmtpClient(_configuration["Email:SmtpServer"])
                // {
                //     Port = int.Parse(_configuration["Email:Port"]),
                //     Credentials = new NetworkCredential(
                //         _configuration["Email:Username"],
                //         _configuration["Email:Password"]),
                //     EnableSsl = true,
                // };
                //
                // var mailMessage = new MailMessage
                // {
                //     From = new MailAddress(_configuration["Email:From"]),
                //     Subject = "HGTransit Password Reset OTP",
                //     Body = $@"
                //         <html>
                //         <body>
                //             <h2>Password Reset Request</h2>
                //             <p>Hi {studentName},</p>
                //             <p>We received a request to reset your password. Use the OTP below to continue:</p>
                //             <h1 style='font-size: 32px; letter-spacing: 5px;'>{otp}</h1>
                //             <p>This OTP will expire in 15 minutes.</p>
                //             <p>If you didn't request this, please ignore this email.</p>
                //             <hr/>
                //             <p>HGTransit Support</p>
                //         </body>
                //         </html>
                //     ",
                //     IsBodyHtml = true,
                // };
                //
                // mailMessage.To.Add(email);
                // await smtpClient.SendMailAsync(mailMessage);

                // For now, just return true (OTP logged in console)
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", email);
                return false;
            }
        }
    }
}