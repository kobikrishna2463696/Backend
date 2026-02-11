using TimeTrack.API.Models;

namespace TimeTrack.API.Repository.IRepository;

public interface IPendingRegistrationRepository : IGenericRepository<PendingRegistrationEntity>
{
    Task<PendingRegistrationEntity?> GetByEmailAsync(string email);
    Task<IEnumerable<PendingRegistrationEntity>> GetByStatusAsync(string status);
    Task<bool> EmailExistsAsync(string email);
}