using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;

namespace HGTSWebApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailService _emailService;

        public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
        }

        // DASHBOARD AUTHENTICATION
        public async Task<DashboardAuthResponseDto?> AuthenticateDashboardAsync(string username, string password)
        {
            var user = await _context.DashboardUsers
                //.Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("Dashboard user not found: {Username}", username);
                return null;
            }

            if (!VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password for dashboard user: {Username}", username);
                return null;
            }

            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return GenerateDashboardToken(user);
        }

        // STUDENT MOBILE AUTHENTICATION
        public async Task<StudentAuthResponseDto?> AuthenticateStudentAsync(string studentNumber, string password, string? deviceToken)
        {
            var student = await _context.Students
                .Include(s => s.Residence)
                .Include(s => s.Faculty)
                .Include(s => s.Credentials)
                .FirstOrDefaultAsync(s => s.StudentNumber == studentNumber && s.Status == "Active");

            if (student == null)
            {
                _logger.LogWarning("Student not found: {StudentNumber}", studentNumber);
                return null;
            }

            var studentAuth = await _context.StudentAuths
                .FirstOrDefaultAsync(sa => sa.StudentId == student.StudentId);

            if (studentAuth == null)
            {
                _logger.LogWarning("Student auth not found for: {StudentNumber}", studentNumber);
                return null;
            }

            if (!VerifyPassword(password, studentAuth.PasswordHash))
            {
                _logger.LogWarning("Invalid password for student: {StudentNumber}", studentNumber);
                return null;
            }

            if (!string.IsNullOrEmpty(deviceToken))
                studentAuth.DeviceToken = deviceToken;

            studentAuth.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return GenerateStudentToken(student, studentAuth);
        }

        // DEV LOGIN
        public async Task<object?> DevAuthenticateAsync(string username, string userType)
        {
            if (userType == "dashboard")
            {
                var user = await _context.DashboardUsers.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    user = new DashboardUser
                    {
                        Username = username,
                        Email = $"{username}@dev.local",
                        FullName = $"Dev {username}",
                        PasswordHash = HashPassword("dev-password"),
                        Role = "Admin",
                        IsActive = true
                    };
                    _context.DashboardUsers.Add(user);
                    await _context.SaveChangesAsync();
                }
                return GenerateDashboardToken(user);
            }
            else if (userType == "student")
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentNumber == username);
                if (student == null) return null;

                var studentAuth = await _context.StudentAuths
                    .FirstOrDefaultAsync(sa => sa.StudentId == student.StudentId);

                if (studentAuth == null)
                {
                    studentAuth = new StudentAuth
                    {
                        StudentId = student.StudentId,
                        PasswordHash = HashPassword("dev-password"),
                        IsActive = true
                    };
                    _context.StudentAuths.Add(studentAuth);
                    await _context.SaveChangesAsync();
                }
                return GenerateStudentToken(student, studentAuth);
            }
            return null;
        }

        // GET DASHBOARD USER INFO
        public async Task<DashboardUserInfoDto?> GetDashboardUserInfoAsync(Guid userId)
        {
            var user = await _context.DashboardUsers
                //.Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return null;

            return new DashboardUserInfoDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                UserCategory = user.UserCategory,
                Department = user.Department,
                //OrganizationId = user.OrganizationId,
                //OrganizationName = user.Organization?.Name
            };
        }

        // GET STUDENT INFO
        public async Task<StudentInfoDto?> GetStudentInfoAsync(Guid studentId)
        {
            var student = await _context.Students
                .Include(s => s.Residence)
                .Include(s => s.Faculty)
                .Include(s => s.Credentials)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null) return null;

            return new StudentInfoDto
            {
                StudentId = student.StudentId,
                FullName = student.FullName,
                StudentNumber = student.StudentNumber,
                Email = student.Email,
                ResidenceName = student.Residence?.ResidenceName,
                FacultyName = student.Faculty?.FacultyName,
                HasActiveCredential = student.Credentials.Any(c => c.IsActive)
            };
        }

        // CHANGE DASHBOARD PASSWORD
        public async Task<bool> ChangeDashboardPasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _context.DashboardUsers.FindAsync(userId);
            if (user == null || !VerifyPassword(currentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        // CHANGE STUDENT PASSWORD
        public async Task<bool> ChangeStudentPasswordAsync(Guid studentId, string currentPassword, string newPassword)
        {
            var studentAuth = await _context.StudentAuths
                .FirstOrDefaultAsync(sa => sa.StudentId == studentId);

            if (studentAuth == null || !VerifyPassword(currentPassword, studentAuth.PasswordHash))
                return false;

            studentAuth.PasswordHash = HashPassword(newPassword);
            studentAuth.PasswordUpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // PASSWORD RESET METHODS
        public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(string email)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == email && s.Status == "Active");

            if (student == null)
            {
                return new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "If the email exists, you will receive a password reset link.",
                    EmailSent = false
                };
            }

            var studentAuth = await _context.StudentAuths
                .FirstOrDefaultAsync(sa => sa.StudentId == student.StudentId);

            if (studentAuth == null)
            {
                return new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "Account not activated. Please complete onboarding first.",
                    EmailSent = false
                };
            }

            string otp = GenerateOtp();
            studentAuth.ResetToken = otp;
            studentAuth.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

            var emailSent = await _emailService.SendOtpEmailAsync(email, otp, student.FullName);
            string maskedEmail = MaskEmail(email);

            return new ForgotPasswordResponseDto
            {
                Success = true,
                Message = emailSent ? "OTP sent to your email" : "Failed to send OTP. Please try again.",
                EmailSent = emailSent,
                MaskedEmail = maskedEmail
            };
        }

        public async Task<VerifyOtpResponseDto> VerifyOtpAsync(string email, string otp)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == email && s.Status == "Active");

            if (student == null)
            {
                return new VerifyOtpResponseDto
                {
                    Success = false,
                    Message = "Invalid request"
                };
            }

            var studentAuth = await _context.StudentAuths
                .FirstOrDefaultAsync(sa => sa.StudentId == student.StudentId);

            if (studentAuth == null || studentAuth.ResetToken != otp)
            {
                return new VerifyOtpResponseDto
                {
                    Success = false,
                    Message = "Invalid OTP"
                };
            }

            if (studentAuth.ResetTokenExpiry < DateTime.UtcNow)
            {
                return new VerifyOtpResponseDto
                {
                    Success = false,
                    Message = "OTP has expired. Please request a new one."
                };
            }

            string resetToken = GenerateResetToken();
            studentAuth.ResetToken = resetToken;
            studentAuth.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(10);
            await _context.SaveChangesAsync();

            return new VerifyOtpResponseDto
            {
                Success = true,
                Message = "OTP verified successfully",
                ResetToken = resetToken
            };
        }

        public async Task<ResetPasswordResponseDto> ResetPasswordAsync(string resetToken, string newPassword)
        {
            var studentAuth = await _context.StudentAuths
                .FirstOrDefaultAsync(sa => sa.ResetToken == resetToken);

            if (studentAuth == null)
            {
                return new ResetPasswordResponseDto
                {
                    Success = false,
                    Message = "Invalid reset token"
                };
            }

            if (studentAuth.ResetTokenExpiry < DateTime.UtcNow)
            {
                return new ResetPasswordResponseDto
                {
                    Success = false,
                    Message = "Reset token has expired. Please request a new one."
                };
            }

            studentAuth.PasswordHash = HashPassword(newPassword);
            studentAuth.PasswordUpdatedAt = DateTime.UtcNow;
            studentAuth.ResetToken = null;
            studentAuth.ResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            return new ResetPasswordResponseDto
            {
                Success = true,
                Message = "Password reset successfully. You can now login with your new password."
            };
        }

        // TOKEN GENERATION
        private DashboardAuthResponseDto GenerateDashboardToken(DashboardUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "1Q2W3E4R5T6Y7U8I9O0PAZSXDCFVGBHN");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("userType", "dashboard"),
                    new Claim("userCategory", user.UserCategory ?? user.Role),
                    new Claim("UserCategory", user.UserCategory ?? user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return new DashboardAuthResponseDto
            {
                Token = tokenHandler.WriteToken(token),
                ExpireAt = tokenDescriptor.Expires.Value,
                User = new DashboardUserInfoDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    UserCategory = user.UserCategory,
                    Department = user.Department,
                    //OrganizationId = user.OrganizationId,
                    ////OrganizationName = user.Organization?.Name
                }
            };
        }

        private StudentAuthResponseDto GenerateStudentToken(Student student, StudentAuth studentAuth)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "1Q2W3E4R5T6Y7U8I9O0PAZSXDCFVGBHN");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, student.StudentId.ToString()),
                    new Claim(ClaimTypes.Name, student.FullName),
                    new Claim(ClaimTypes.Email, student.Email),
                    new Claim("studentNumber", student.StudentNumber),
                    new Claim("userType", "student")
                }),
                Expires = DateTime.UtcNow.AddDays(30),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return new StudentAuthResponseDto
            {
                Token = tokenHandler.WriteToken(token),
                ExpiresAt = tokenDescriptor.Expires.Value,
                Student = new StudentInfoDto
                {
                    StudentId = student.StudentId,
                    FullName = student.FullName,
                    StudentNumber = student.StudentNumber,
                    Email = student.Email,
                    ResidenceName = student.Residence?.ResidenceName,
                    FacultyName = student.Faculty?.FacultyName,
                    HasActiveCredential = student.Credentials?.Any(c => c.IsActive) ?? false
                }
            };
        }

        // PASSWORD HASHING - Using SHA256
        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        public bool VerifyPassword(string password, string hash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword == hash;
        }

        // Helper methods
        private string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private string GenerateResetToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-")
                .Replace("=", "");
        }

        private string MaskEmail(string email)
        {
            var parts = email.Split('@');
            if (parts[0].Length <= 2)
                return email;

            string masked = parts[0].Substring(0, 2) + "***" + parts[0].Substring(parts[0].Length - 1);
            return masked + "@" + parts[1];
        }
    }
}
