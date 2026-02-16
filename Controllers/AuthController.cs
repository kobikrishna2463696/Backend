using Microsoft.AspNetCore.Mvc;
using TimeTrack.API.DTOs.Auth;
using TimeTrack.API.DTOs.Common;
using TimeTrack.API.DTOs.Registration;
using TimeTrack.API.Models.Enums;
using TimeTrack.API.Service;

namespace TimeTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly IRegistrationService _registrationService;

    public AuthController(IAuthenticationService authService, IRegistrationService registrationService)
    {
        _authService = authService;
        _registrationService = registrationService;
    }


    /// <summary>
    /// User login endpoint
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponseDto<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponseDto<LoginResponseDto>.SuccessResponse(result, "Login successful"));
    }

    /// <summary>
    /// User registration endpoint - Employee/Manager requires admin approval
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        // Employee and Manager registrations require admin approval
        if (request.Role.Equals("Employee", StringComparison.OrdinalIgnoreCase) ||
            request.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
        {
            // Route to pending registration (stored in PendingRegistrations table)
            var pendingRequest = new RegistrationRequestDto
            {
                Name = request.Name,
                Email = request.Email,
                Password = request.Password,
                Role = request.Role,
                Department = request.Department
            };

            var pendingResult = await _registrationService.SubmitRegistrationAsync(pendingRequest);
            
            return Ok(ApiResponseDto<RegistrationResponseDto>.SuccessResponse(
                pendingResult, 
                "Registration submitted. Please wait for admin approval before logging in."));
        }

        // Block direct Admin registration through this endpoint
        return BadRequest(ApiResponseDto<string>.ErrorResponse(
            "Admin accounts cannot be created through self-registration."));
    }
    //summaryy
    /// <summary>
    /// Get available departments for registration dropdown
    /// </summary>
    [HttpGet("departments")]
    public ActionResult<ApiResponseDto<IEnumerable<string>>> GetDepartments()
    {
        return Ok(ApiResponseDto<IEnumerable<string>>.SuccessResponse(
            DepartmentType.AllDepartments, 
            "Available departments retrieved"));
    }

    /// <summary>
    /// Get available roles for registration dropdown
    /// </summary>
    [HttpGet("roles")]
    public ActionResult<ApiResponseDto<IEnumerable<string>>> GetRoles()
    {
        // Only Employee and Manager can self-register
        var roles = new[] { "Employee", "Manager" };
        return Ok(ApiResponseDto<IEnumerable<string>>.SuccessResponse(
            roles, 
            "Available roles retrieved"));
    }
}