using HGTSWebApi.DTOs;

namespace HGTSWebApi.Services
{
    public interface ICredentialService
    {
        Task<List<CredentialDto>> GetCredentialsAsync(Guid studentId);
        Task<AssignPhoneCredentialResponseDto> GetOrCreatePhoneTokenAsync(Guid studentId);
        Task<AssignPhoneCredentialResponseDto> AssignPhoneCredentialAsync(Guid studentId, AssignPhoneCredentialRequestDto request);
        Task<AssignPhoneCredentialResponseDto> RebindPhoneCredentialAsync(Guid studentId, RebindPhoneRequestDto request);
        Task<CredentialDto> AddCardCredentialAsync(Guid studentId, AddCardCredentialRequestDto request);
        Task<bool> ToggleCredentialAsync(Guid studentId, Guid credentialId);
        Task<bool> RevokeCredentialAsync(Guid studentId, Guid credentialId);
        Task<(bool IsValid, Guid? StudentId, string CredentialType)> ValidateCredentialTokenAsync(string token);
    }
}