namespace TimeTrack.API.DTOs.User;

public class UserUpdateDto
{
    public string? FullName { get; set; }
    public string? Role { get; set; }
    public string? Department { get; set; }
    public string? Status { get; set; }
    public int? ManagerId { get; set; }  // For employees: assign ONE manager
    public List<int>? AssignedEmployeeIds { get; set; }  // For managers: assign MANY employees
}