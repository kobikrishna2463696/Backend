using Microsoft.EntityFrameworkCore.Storage;
using TimeTrack.API.Data;
using TimeTrack.API.Repository.IRepository;

namespace TimeTrack.API.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly TimeTrackDbContext _context;
    private IDbContextTransaction? _transaction;

    public IUserRepository Users { get; }
    public ITimeLogRepository TimeLogs { get; }
    public ITaskRepository Tasks { get; }
    public ITaskTimeRepository TaskTimes { get; }
    public INotificationRepository Notifications { get; }
    public IPendingRegistrationRepository PendingRegistrations { get; }

    public UnitOfWork(TimeTrackDbContext context)
    {
        _context = context;
        Users = new UserRepository(_context);
        TimeLogs = new TimeLogRepository(_context);
        Tasks = new TaskRepository(_context);
        TaskTimes = new TaskTimeRepository(_context);
        Notifications = new NotificationRepository(_context);
        PendingRegistrations = new PendingRegistrationRepository(_context);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            await _transaction!.CommitAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context?.Dispose();
    }
}