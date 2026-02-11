namespace TimeTrack.API.Repository.IRepository;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    ITimeLogRepository TimeLogs { get; }
    ITaskRepository Tasks { get; }
    ITaskTimeRepository TaskTimes { get; }
    INotificationRepository Notifications { get; }
    IPendingRegistrationRepository PendingRegistrations { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}