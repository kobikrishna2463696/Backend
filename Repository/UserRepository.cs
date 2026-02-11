using Microsoft.EntityFrameworkCore;
using TimeTrack.API.Data;
using TimeTrack.API.Models;
using TimeTrack.API.Repository.IRepository;

namespace TimeTrack.API.Repository;

public class UserRepository : GenericRepository<UserEntity>, IUserRepository
{
    public UserRepository(TimeTrackDbContext context) : base(context) { }

    public async Task<UserEntity?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<IEnumerable<UserEntity>> GetActiveUsersAsync()
    {
        return await _dbSet
            .Include(u => u.Manager)
            .Include(u => u.AssignedEmployees)
            .Where(u => u.Status == "Active")
            .ToListAsync();
    }

    public async Task<IEnumerable<UserEntity>> GetUsersByDepartmentAsync(string department)
    {
        return await _dbSet
            .Include(u => u.Manager)
            .Where(u => u.Department == department)
            .ToListAsync();
    }

    public async Task<UserEntity?> GetByIdWithManagerAsync(int userId)
    {
        return await _dbSet
            .Include(u => u.Manager)
            .Include(u => u.AssignedEmployees)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<IEnumerable<UserEntity>> GetAllWithManagerAsync()
    {
        return await _dbSet
            .Include(u => u.Manager)
            .Include(u => u.AssignedEmployees)
            .Where(u => u.Status == "Active")
            .ToListAsync();
    }

    public async Task<IEnumerable<UserEntity>> GetEmployeesByManagerIdAsync(int managerId)
    {
        return await _dbSet
            .Where(u => u.ManagerId == managerId)
            .ToListAsync();
    }
}

// Manager-Employee Self-Referencing Relationship
