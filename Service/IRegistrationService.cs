using TimeTrack.API.DTOs.Registration;

namespace TimeTrack.API.Service;

public interface IRegistrationService
{
    Task<RegistrationResponseDto> SubmitRegistrationAsync(RegistrationRequestDto request);
    Task<IEnumerable<PendingRegistrationDto>> GetAllRegistrationsAsync();
    Task<IEnumerable<PendingRegistrationDto>> GetPendingRegistrationsAsync();
    Task<IEnumerable<PendingRegistrationDto>> GetApprovedRegistrationsAsync();
    Task<IEnumerable<PendingRegistrationDto>> GetRejectedRegistrationsAsync();
    Task<RegistrationResponseDto> ApproveRegistrationAsync(int registrationId, int adminUserId);
    Task<RegistrationResponseDto> RejectRegistrationAsync(int registrationId, int adminUserId, string? reason);
    Task DeleteRegistrationAsync(int registrationId);
    Task<int> GetPendingCountAsync();
}