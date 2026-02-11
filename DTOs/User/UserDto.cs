namespace TimeTrack.API.DTOs.User;

public class UserDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public List<int> AssignedEmployeeIds { get; set; } = new();
}