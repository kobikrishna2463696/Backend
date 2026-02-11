using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TimeTrack.API.DTOs.Common;
using TimeTrack.API.DTOs.Registration;
using TimeTrack.API.Service;

namespace TimeTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _registrationService;

    public RegistrationController(IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    /// <summary>
    /// Submit a new registration request (Employee/Manager only)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponseDto<RegistrationResponseDto>>> Register(
        [FromBody] RegistrationRequestDto request)
    {
        try
        {
            var result = await _registrationService.SubmitRegistrationAsync(request);
            return Ok(ApiResponseDto<RegistrationResponseDto>.SuccessResponse(result, result.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponseDto<RegistrationResponseDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponseDto<RegistrationResponseDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get all registrations (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<PendingRegistrationDto>>>> GetAll()
    {
        var registrations = await _registrationService.GetAllRegistrationsAsync();
        return Ok(ApiResponseDto<IEnumerable<PendingRegistrationDto>>.SuccessResponse(
            registrations, "All registrations retrieved"));
    }

    /// <summary>
    /// Get pending registrations (Admin only)
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<PendingRegistrationDto>>>> GetPending()
    {
        var registrations = await _registrationService.GetPendingRegistrationsAsync();
        return Ok(ApiResponseDto<IEnumerable<PendingRegistrationDto>>.SuccessResponse(
            registrations, "Pending registrations retrieved"));
    }

    /// <summary>
    /// Get approved registrations (Admin only)
    /// </summary>
    [HttpGet("approved")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<PendingRegistrationDto>>>> GetApproved()
    {
        var registrations = await _registrationService.GetApprovedRegistrationsAsync();
        return Ok(ApiResponseDto<IEnumerable<PendingRegistrationDto>>.SuccessResponse(
            registrations, "Approved registrations retrieved"));
    }

    /// <summary>
    /// Get rejected registrations (Admin only)
    /// </summary>
    [HttpGet("rejected")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<PendingRegistrationDto>>>> GetRejected()
    {
        var registrations = await _registrationService.GetRejectedRegistrationsAsync();
        return Ok(ApiResponseDto<IEnumerable<PendingRegistrationDto>>.SuccessResponse(
            registrations, "Rejected registrations retrieved"));
    }

    /// <summary>
    /// Get pending registration count (Admin only)
    /// </summary>
    [HttpGet("pending/count")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponseDto<int>>> GetPendingCount()
    {
        var count = await _registrationService.GetPendingCountAsync();
        return Ok(ApiResponseDto<int>.SuccessResponse(count, "Pending count retrieved"));
    }

    /// <summary>
    /// Approve a registration (Admin only)
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponseDto<RegistrationResponseDto>>> Approve(int id)
    {
        try
        {
            var adminUserId = GetCurrentUserId();
            var result = await _registrationService.ApproveRegistrationAsync(id, adminUserId);
            return Ok(ApiResponseDto<RegistrationResponseDto>.SuccessResponse(result, result.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<RegistrationResponseDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponseDto<RegistrationResponseDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Reject a registration (Admin only)
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponseDto<RegistrationResponseDto>>> Reject(int id)
    {
        try
        {
            var adminUserId = GetCurrentUserId();
            var result = await _registrationService.RejectRegistrationAsync(id, adminUserId);
            return Ok(ApiResponseDto<RegistrationResponseDto>.SuccessResponse(result, result.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<RegistrationResponseDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponseDto<RegistrationResponseDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Delete a registration record (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponseDto<string>>> Delete(int id)
    {
        try
        {
            await _registrationService.DeleteRegistrationAsync(id);
            return Ok(ApiResponseDto<string>.SuccessResponse("Deleted", "Registration deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponseDto<string>.ErrorResponse(ex.Message));
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("userId")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}