using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Data;
using HGTSWebApi.DTOs;
using HGTSWebApi.Models;
using System.Security.Cryptography;

namespace HGTSWebApi.Services
{
    public class CredentialService : ICredentialService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CredentialService> _logger;

        public CredentialService(AppDbContext context, ILogger<CredentialService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CredentialDto>> GetCredentialsAsync(Guid studentId)
        {
            return await _context.NFCCredentials
                .Where(c => c.StudentId == studentId)
                .OrderByDescending(c => c.CredentialType)
                .ThenBy(c => c.IssuedDate)
                .Select(c => new CredentialDto
                {
                    CredentialId = c.CredentialId,
                    CredentialUid = c.CredentialUid,
                    CredentialType = c.CredentialType,
                    IsActive = c.IsActive,
                    IssuedDate = c.IssuedDate,
                    CardSerial = c.CardSerial,
                    DeviceModel = c.DeviceModel,
                    LastSeenAt = c.LastSeenAt
                })
                .ToListAsync();
        }

        public async Task<AssignPhoneCredentialResponseDto> GetOrCreatePhoneTokenAsync(Guid studentId)
        {
            // Check if student already has a phone credential
            var existingPhone = await _context.NFCCredentials
                .FirstOrDefaultAsync(c => c.StudentId == studentId && c.CredentialType == "PHONE" && c.IsActive);

            if (existingPhone != null)
            {
                // Return existing token
                return new AssignPhoneCredentialResponseDto
                {
                    Success = true,
                    CredentialToken = existingPhone.CredentialUid,
                    IsNewCredential = false,
                    Message = "Existing phone credential found"
                };
            }

            // Generate new stable token
            var token = GenerateSecureToken();

            var credential = new NFCCredential
            {
                CredentialId = Guid.NewGuid(),
                CredentialUid = token,
                CredentialType = "PHONE",
                StudentId = studentId,
                IssuedDate = DateTime.UtcNow,
                IsActive = true,
                LastSeenAt = DateTime.UtcNow
            };

            _context.NFCCredentials.Add(credential);
            await _context.SaveChangesAsync();

            return new AssignPhoneCredentialResponseDto
            {
                Success = true,
                CredentialToken = token,
                IsNewCredential = true,
                Message = "Phone credential created successfully"
            };
        }

        public async Task<AssignPhoneCredentialResponseDto> AssignPhoneCredentialAsync(Guid studentId, AssignPhoneCredentialRequestDto request)
        {
            // Check if student already has a phone credential
            var existingPhone = await _context.NFCCredentials
                .FirstOrDefaultAsync(c => c.StudentId == studentId && c.CredentialType == "PHONE");

            if (existingPhone != null)
            {
                // Return existing token (same token always)
                existingPhone.DeviceIdentifier = request.DeviceIdentifier;
                existingPhone.DeviceModel = request.DeviceModel;
                existingPhone.Platform = request.Platform;
                existingPhone.LastSeenAt = DateTime.UtcNow;
                existingPhone.IsActive = true;
                await _context.SaveChangesAsync();

                return new AssignPhoneCredentialResponseDto
                {
                    Success = true,
                    CredentialToken = existingPhone.CredentialUid,
                    IsNewCredential = false,
                    Message = "Phone already registered"
                };
            }

            // Generate new stable token
            var token = GenerateSecureToken();

            var credential = new NFCCredential
            {
                CredentialId = Guid.NewGuid(),
                CredentialUid = token,
                CredentialType = "PHONE",
                StudentId = studentId,
                IssuedDate = DateTime.UtcNow,
                IsActive = true,
                DeviceIdentifier = request.DeviceIdentifier,
                DeviceModel = request.DeviceModel,
                Platform = request.Platform,
                LastSeenAt = DateTime.UtcNow
            };

            _context.NFCCredentials.Add(credential);
            await _context.SaveChangesAsync();

            return new AssignPhoneCredentialResponseDto
            {
                Success = true,
                CredentialToken = token,
                IsNewCredential = true,
                Message = "Phone registered successfully"
            };
        }

        public async Task<AssignPhoneCredentialResponseDto> RebindPhoneCredentialAsync(Guid studentId, RebindPhoneRequestDto request)
        {
            var phoneCred = await _context.NFCCredentials
                .FirstOrDefaultAsync(c => c.StudentId == studentId && c.CredentialType == "PHONE");

            if (phoneCred == null)
            {
                return new AssignPhoneCredentialResponseDto
                {
                    Success = false,
                    CredentialToken = string.Empty,
                    IsNewCredential = false,
                    Message = "No phone credential found. Please assign first."
                };
            }

            phoneCred.DeviceIdentifier = request.DeviceIdentifier;
            phoneCred.DeviceModel = request.DeviceModel;
            phoneCred.Platform = request.Platform;
            phoneCred.LastSeenAt = DateTime.UtcNow;
            phoneCred.IsActive = true;

            await _context.SaveChangesAsync();

            return new AssignPhoneCredentialResponseDto
            {
                Success = true,
                CredentialToken = phoneCred.CredentialUid,
                IsNewCredential = false,
                Message = "Phone credential rebound to new device successfully"
            };
        }

        public async Task<CredentialDto> AddCardCredentialAsync(Guid studentId, AddCardCredentialRequestDto request)
        {
            var existingCard = await _context.NFCCredentials
                .FirstOrDefaultAsync(c => c.StudentId == studentId
                    && c.CredentialType == "CARD"
                    && c.CredentialUid == request.CredentialToken);

            if (existingCard != null)
            {
                existingCard.IsActive = true;
                existingCard.LastSeenAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new CredentialDto
                {
                    CredentialId = existingCard.CredentialId,
                    CredentialUid = existingCard.CredentialUid,
                    CredentialType = existingCard.CredentialType,
                    IsActive = existingCard.IsActive,
                    IssuedDate = existingCard.IssuedDate,
                    CardSerial = existingCard.CardSerial
                };
            }

            var cardCredential = new NFCCredential
            {
                CredentialId = Guid.NewGuid(),
                CredentialUid = request.CredentialToken,
                CredentialType = "CARD",
                StudentId = studentId,
                IssuedDate = DateTime.UtcNow,
                IsActive = true,
                CardSerial = request.CardSerial,
                LastSeenAt = DateTime.UtcNow
            };

            _context.NFCCredentials.Add(cardCredential);
            await _context.SaveChangesAsync();

            return new CredentialDto
            {
                CredentialId = cardCredential.CredentialId,
                CredentialUid = cardCredential.CredentialUid,
                CredentialType = cardCredential.CredentialType,
                IsActive = cardCredential.IsActive,
                IssuedDate = cardCredential.IssuedDate,
                CardSerial = cardCredential.CardSerial
            };
        }

        public async Task<bool> ToggleCredentialAsync(Guid studentId, Guid credentialId)
        {
            var credential = await _context.NFCCredentials
                .FirstOrDefaultAsync(c => c.CredentialId == credentialId && c.StudentId == studentId);

            if (credential == null)
                return false;

            credential.IsActive = !credential.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RevokeCredentialAsync(Guid studentId, Guid credentialId)
        {
            var credential = await _context.NFCCredentials
                .FirstOrDefaultAsync(c => c.CredentialId == credentialId && c.StudentId == studentId);

            if (credential == null)
                return false;

            credential.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool IsValid, Guid? StudentId, string CredentialType)> ValidateCredentialTokenAsync(string token)
        {
            var credential = await _context.NFCCredentials
                .Include(c => c.Student)
                .FirstOrDefaultAsync(c => c.CredentialUid == token && c.IsActive);

            if (credential == null)
                return (false, null, string.Empty);

            if (credential.Student?.Status != "Active")
                return (false, null, string.Empty);

            credential.LastSeenAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, credential.StudentId, credential.CredentialType);
        }

        private string GenerateSecureToken()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes).Replace("/", "_").Replace("+", "-").Replace("=", "");
        }
    }
}