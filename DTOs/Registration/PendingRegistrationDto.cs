namespace TimeTrack.API.DTOs.Registration;

public class PendingRegistrationDto
{
    public int RegistrationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AppliedDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public string? ProcessedByName { get; set; }
    public string? RejectionReason { get; set; }
}

public class RegistrationRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

public class ApproveRegistrationDto
{
    public int RegistrationId { get; set; }
}

public class RejectRegistrationDto
{
    public int RegistrationId { get; set; }
    public string? Reason { get; set; }
}

public class RegistrationResponseDto
{
    public int RegistrationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}