using System.ComponentModel.DataAnnotations;

namespace TimeTrack.API.DTOs.Task;

public class CreateTaskDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    [Required]
    public int AssignedToUserId { get; set; }

    public int? ProjectId { get; set; }

    [Required]
    [Range(0.1, 999.99)]
    public decimal EstimatedHours { get; set; }

    [Required]
    public string Priority { get; set; } = "Medium";

    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    public DateTime? DueDate { get; set; }
}