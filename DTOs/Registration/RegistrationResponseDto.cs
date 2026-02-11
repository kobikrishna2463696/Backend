namespace TimeTrack.API.DTOs.Registration;

public class RegistrationResponseDto
{
    public int RegistrationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}