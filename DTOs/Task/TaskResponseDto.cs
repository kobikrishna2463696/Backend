namespace TimeTrack.API.DTOs.Task;

public class TaskResponseDto
{
    public int TaskId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int AssignedToUserId { get; set; }
    public string AssignedToUserName { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; }
    public int? ProjectId { get; set; }
    public string ProjectName { get; set; }
    public decimal EstimatedHours { get; set; }
    public decimal ActualHoursSpent { get; set; }
    public string Status { get; set; }
    public string Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
}