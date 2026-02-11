using Microsoft.EntityFrameworkCore;
using TimeTrack.API.Models;
using TimeTrack.API.Models.Enums;

namespace TimeTrack.API.Data;

public class TimeTrackDbContext : DbContext
{
    public TimeTrackDbContext(DbContextOptions<TimeTrackDbContext> options) : base(options)
    {
    }

    public DbSet<UserEntity> Users { get; set; }
    public DbSet<TimeLogEntity> TimeLogs { get; set; }
    public DbSet<TaskEntity> Tasks { get; set; }
    public DbSet<TaskTimeEntity> TaskTimes { get; set; }
    public DbSet<NotificationEntity> Notifications { get; set; }
    public DbSet<ProjectEntity> Projects { get; set; }
    public DbSet<PendingRegistrationEntity> PendingRegistrations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Entity Configuration
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.Status).HasDefaultValue("Active");
        });

        // TimeLog Entity Configuration
        modelBuilder.Entity<TimeLogEntity>(entity =>
        {
            entity.HasOne(t => t.User)
                  .WithMany(u => u.TimeLogs)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.Date });
        });

        // Task Entity Configuration
        modelBuilder.Entity<TaskEntity>(entity =>
        {
            entity.HasOne(t => t.AssignedToUser)
                  .WithMany(u => u.AssignedTasks)
                  .HasForeignKey(t => t.AssignedToUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.CreatedByUser)
                  .WithMany(u => u.CreatedTasks)
                  .HasForeignKey(t => t.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(t => t.Status).HasDefaultValue("Pending");
            entity.Property(t => t.Priority).HasDefaultValue("Medium");
            entity.HasIndex(e => e.ProjectId);
        });

        // TaskTime Entity Configuration
        modelBuilder.Entity<TaskTimeEntity>(entity =>
        {
            entity.HasOne(tt => tt.Task)
                  .WithMany(t => t.TaskTimes)
                  .HasForeignKey(tt => tt.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tt => tt.User)
                  .WithMany()
                  .HasForeignKey(tt => tt.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.TaskId, e.UserId, e.Date });
        });

        // Notification Entity Configuration
        modelBuilder.Entity<NotificationEntity>(entity =>
        {
            entity.HasOne(n => n.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(n => n.Status).HasDefaultValue("Unread");
            entity.HasIndex(e => new { e.UserId, e.Status });
        });

        // Projects Configuration
        modelBuilder.Entity<ProjectEntity>(entity =>
        {
            entity.HasKey(e => e.ProjectId);
            
            entity.Property(e => e.ProjectName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.ClientName)
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Active");

            entity.Property(e => e.Budget)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.CreatedDate)
                .IsRequired();

            entity.HasOne(p => p.Manager)
                .WithMany()
                .HasForeignKey(p => p.ManagerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(p => p.Tasks)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ManagerUserId);
        });

        // PendingRegistration Entity Configuration
        modelBuilder.Entity<PendingRegistrationEntity>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Status).HasDefaultValue("Pending");
            
            entity.HasOne(e => e.ProcessedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ProcessedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Seed Initial Admin User
        modelBuilder.Entity<UserEntity>().HasData(
            new UserEntity
            {
                UserId = 1,
                Name = "System Administrator",
                Email = "admin@timetrack.com",
                PasswordHash = "$2a$11$X7ZQ3Z3Z3Z3Z3Z3Z3Z3Z3uK8vJ8vJ8vJ8vJ8vJ8vJ8vJ8vJ8vJ8vJ8",
                Role = "Admin",
                Department = DepartmentType.MultiCloud,
                Status = "Active",
                CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}