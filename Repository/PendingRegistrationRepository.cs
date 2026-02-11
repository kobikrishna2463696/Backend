using Microsoft.EntityFrameworkCore;
using TimeTrack.API.Data;
using TimeTrack.API.Models;
using TimeTrack.API.Repository.IRepository;

namespace TimeTrack.API.Repository;

public class PendingRegistrationRepository : GenericRepository<PendingRegistrationEntity>, IPendingRegistrationRepository
{
    public PendingRegistrationRepository(TimeTrackDbContext context) : base(context)
    {
    }

    public async Task<PendingRegistrationEntity?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(r => r.ProcessedByUser)
            .FirstOrDefaultAsync(r => r.Email.ToLower() == email.ToLower());
    }

    public async Task<IEnumerable<PendingRegistrationEntity>> GetByStatusAsync(string status)
    {
        return await _dbSet
            .Include(r => r.ProcessedByUser)
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.AppliedDate)
            .ToListAsync();
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(r => r.Email.ToLower() == email.ToLower());
    }
}