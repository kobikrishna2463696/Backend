using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

namespace TimeTrack.API.Models;

[Table("Users")]

public class UserEntity

{

    [Key]

    public int UserId { get; set; }

    [Required]

    [StringLength(100)]

    public string Name { get; set; } = string.Empty;

    [Required]

    [StringLength(50)]

    public string Role { get; set; } = string.Empty; // Employee, Manager, Admin

    [Required]

    [StringLength(100)]

    [EmailAddress]

    public string Email { get; set; } = string.Empty;

    [Required]

    [StringLength(256)]

    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(100)]

    public string Department { get; set; } = string.Empty;

    [Required]

    [StringLength(20)]

    public string Status { get; set; } = "Active"; // Active, Inactive

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginDate { get; set; }

    // Manager-Employee Relationship

    public int? ManagerId { get; set; }

    [ForeignKey("ManagerId")]

    public virtual UserEntity? Manager { get; set; }

    // Employees assigned to this manager

    [InverseProperty("Manager")]

    public virtual ICollection<UserEntity> AssignedEmployees { get; set; } = new List<UserEntity>();

    // Navigation Properties

    public virtual ICollection<TimeLogEntity> TimeLogs { get; set; } = new List<TimeLogEntity>();

    public virtual ICollection<TaskEntity> AssignedTasks { get; set; } = new List<TaskEntity>();

    public virtual ICollection<TaskEntity> CreatedTasks { get; set; } = new List<TaskEntity>();

    public virtual ICollection<NotificationEntity> Notifications { get; set; } = new List<NotificationEntity>();

}
