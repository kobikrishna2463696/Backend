using Microsoft.EntityFrameworkCore;
using TimeTrack.API.Data;
using TimeTrack.API.Models;
using TimeTrack.API.Repository.IRepository;

namespace TimeTrack.API.Repository;

public class TaskRepository : GenericRepository<TaskEntity>, ITaskRepository
{
    public TaskRepository(TimeTrackDbContext context) : base(context)
    {
    }

    public override async Task<TaskEntity> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(t => t.AssignedToUser)
            .Include(t => t.CreatedByUser)
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.TaskId == id);
    }

    public async Task<IEnumerable<TaskEntity>> GetTasksByAssignedUserAsync(int userId)
    {
        return await _dbSet
            .Include(t => t.AssignedToUser)
            .Include(t => t.CreatedByUser)
            .Include(t => t.Project)
            .Include(t => t.TaskTimes)
            .Where(t => t.AssignedToUserId == userId)
            .OrderBy(t => t.DueDate)
            .ThenByDescending(t => t.CreatedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskEntity>> GetTasksByCreatorAsync(int creatorId)
    {
        return await _dbSet
            .Include(t => t.AssignedToUser)
            .Include(t => t.CreatedByUser)
            .Include(t => t.Project)
            .Include(t => t.TaskTimes)
            .Where(t => t.CreatedByUserId == creatorId)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskEntity>> GetTasksByStatusAsync(string status)
    {
        return await _dbSet
            .Include(t => t.AssignedToUser)
            .Include(t => t.CreatedByUser)
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskEntity>> GetOverdueTasksAsync()
    {
        return await _dbSet
            .Include(t => t.AssignedToUser)
            .Include(t => t.CreatedByUser)
            .Where(t => t.DueDate.HasValue && t.DueDate < DateTime.UtcNow && t.Status != "Completed")
            .OrderBy(t => t.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskEntity>> GetTasksByDepartmentAsync(string department)
    {
        return await _dbSet
            .Include(t => t.AssignedToUser)
            .Include(t => t.CreatedByUser)
            .Where(t => t.AssignedToUser.Department == department)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();
    }

    public async Task<int> GetCompletedTasksCountAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(t => t.AssignedToUserId == userId 
                     && t.Status == "Completed" 
                     && t.CompletedDate.HasValue 
                     && t.CompletedDate >= startDate 
                     && t.CompletedDate <= endDate)
            .CountAsync();
    }

    public async Task<int> GetActiveTasksCountForUsersAsync(IEnumerable<int> userIds)
    {
        if (userIds == null || !userIds.Any()) return 0;
        return await _dbSet
            .Where(t => userIds.Contains(t.AssignedToUserId) && t.Status == "Active")
            .CountAsync();
    }
}