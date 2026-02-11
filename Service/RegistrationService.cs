using BCrypt.Net;
using TimeTrack.API.DTOs.Registration;
using TimeTrack.API.Models;
using TimeTrack.API.Models.Enums;
using TimeTrack.API.Repository.IRepository;

namespace TimeTrack.API.Service;

public class RegistrationService : IRegistrationService
{
    private readonly IUnitOfWork _unitOfWork;

    public RegistrationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RegistrationResponseDto> SubmitRegistrationAsync(RegistrationRequestDto request)
    {
        // Validate role - only Employee and Manager can register via this flow
        var allowedRoles = new[] { "Employee", "Manager" };
        if (!allowedRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only Employee and Manager roles can register through this portal.");
        }

        // Validate department
        if (!DepartmentType.IsValid(request.Department))
        {
            throw new ArgumentException($"Invalid department. Allowed: {DepartmentType.GetValidDepartmentsString()}");
        }

        // Check if email already exists in Users table
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
        {
            throw new InvalidOperationException("Email already registered as an active user.");
        }

        // Check if email already has a pending registration
        var existingRegistration = await _unitOfWork.PendingRegistrations.GetByEmailAsync(request.Email);
        if (existingRegistration != null)
        {
            if (existingRegistration.Status == "Pending")
            {
                throw new InvalidOperationException("A registration request with this email is already pending approval.");
            }
            if (existingRegistration.Status == "Rejected")
            {
                throw new InvalidOperationException("This email was previously rejected. Please contact the administrator.");
            }
        }

        var registration = new PendingRegistrationEntity
        {
            Name = request.Name,
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            Department = request.Department,
            Status = "Pending",
            AppliedDate = DateTime.UtcNow
        };

        await _unitOfWork.PendingRegistrations.AddAsync(registration);
        await _unitOfWork.SaveChangesAsync();

        return new RegistrationResponseDto
        {
            RegistrationId = registration.RegistrationId,
            Message = "Registration submitted successfully. Please wait for admin approval.",
            Status = "Pending"
        };
    }

    public async Task<IEnumerable<PendingRegistrationDto>> GetAllRegistrationsAsync()
    {
        var registrations = await _unitOfWork.PendingRegistrations.GetAllAsync();
        return registrations.Select(MapToDto).OrderByDescending(r => r.AppliedDate);
    }

    public async Task<IEnumerable<PendingRegistrationDto>> GetPendingRegistrationsAsync()
    {
        var registrations = await _unitOfWork.PendingRegistrations.GetByStatusAsync("Pending");
        return registrations.Select(MapToDto);
    }

    public async Task<IEnumerable<PendingRegistrationDto>> GetApprovedRegistrationsAsync()
    {
        var registrations = await _unitOfWork.PendingRegistrations.GetByStatusAsync("Approved");
        return registrations.Select(MapToDto);
    }

    public async Task<IEnumerable<PendingRegistrationDto>> GetRejectedRegistrationsAsync()
    {
        var registrations = await _unitOfWork.PendingRegistrations.GetByStatusAsync("Rejected");
        return registrations.Select(MapToDto);
    }

    public async Task<RegistrationResponseDto> ApproveRegistrationAsync(int registrationId, int adminUserId)
    {
        var registration = await _unitOfWork.PendingRegistrations.GetByIdAsync(registrationId);
        if (registration == null)
        {
            throw new KeyNotFoundException("Registration not found.");
        }

        if (registration.Status != "Pending")
        {
            throw new InvalidOperationException($"Registration has already been {registration.Status.ToLower()}.");
        }

        // Create the user in the Users table
        var user = new UserEntity
        {
            Name = registration.Name,
            Email = registration.Email,
            PasswordHash = registration.PasswordHash,
            Role = registration.Role,
            Department = registration.Department,
            Status = "Active",
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);

        // Update registration status
        registration.Status = "Approved";
        registration.ProcessedDate = DateTime.UtcNow;
        registration.ProcessedByUserId = adminUserId;
        _unitOfWork.PendingRegistrations.Update(registration);

        await _unitOfWork.SaveChangesAsync();

        return new RegistrationResponseDto
        {
            RegistrationId = registrationId,
            Message = $"Registration approved. {registration.Name} can now login.",
            Status = "Approved"
        };
    }

    public async Task<RegistrationResponseDto> RejectRegistrationAsync(int registrationId, int adminUserId, string? reason)
    {
        var registration = await _unitOfWork.PendingRegistrations.GetByIdAsync(registrationId);
        if (registration == null)
        {
            throw new KeyNotFoundException("Registration not found.");
        }

        if (registration.Status != "Pending")
        {
            throw new InvalidOperationException($"Registration has already been {registration.Status.ToLower()}.");
        }

        // Update registration status - keep record in database as rejected
        registration.Status = "Rejected";
        registration.ProcessedDate = DateTime.UtcNow;
        registration.ProcessedByUserId = adminUserId;
        registration.RejectionReason = reason;
        _unitOfWork.PendingRegistrations.Update(registration);

        await _unitOfWork.SaveChangesAsync();

        return new RegistrationResponseDto
        {
            RegistrationId = registrationId,
            Message = $"Registration rejected for {registration.Name}.",
            Status = "Rejected"
        };
    }

    public async Task DeleteRegistrationAsync(int registrationId)
    {
        var registration = await _unitOfWork.PendingRegistrations.GetByIdAsync(registrationId);
        if (registration == null)
        {
            throw new KeyNotFoundException("Registration not found.");
        }

        _unitOfWork.PendingRegistrations.Delete(registration);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<int> GetPendingCountAsync()
    {
        var pending = await _unitOfWork.PendingRegistrations.GetByStatusAsync("Pending");
        return pending.Count();
    }

    private static PendingRegistrationDto MapToDto(PendingRegistrationEntity entity)
    {
        return new PendingRegistrationDto
        {
            RegistrationId = entity.RegistrationId,
            Name = entity.Name,
            Email = entity.Email,
            Role = entity.Role,
            Department = entity.Department,
            Status = entity.Status,
            AppliedDate = entity.AppliedDate,
            ProcessedDate = entity.ProcessedDate,
            ProcessedByName = entity.ProcessedByUser?.Name,
            RejectionReason = entity.RejectionReason
        };
    }
}