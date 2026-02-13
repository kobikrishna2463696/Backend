using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TimeTrack.API.DTOs.Common;
using TimeTrack.API.Repository.IRepository;

namespace TimeTrack.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public UserController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Gets current user profile information
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponseDto<object>>> GetProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound(ApiResponseDto<object>.ErrorResponse("User not found"));
        }

        var profile = new
        {
            user.UserId,
            user.Name,
            user.Email,
            user.Role,
            user.Department,
            user.Status,
            user.CreatedDate,
            user.LastLoginDate
        };

        return Ok(ApiResponseDto<object>.SuccessResponse(profile));
    }

    /// <summary>
    /// Gets all active users in the system (managers/admins only)
    /// </summary>
    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpGet("all")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<object>>>> GetAllUsers()
    {
        var users = await _unitOfWork.Users.GetActiveUsersAsync();
        
        var userList = users.Select(u => new
        {
            u.UserId,
            u.Name,
            u.Email,
            u.Role,
            u.Department,
            u.Status
        });

        return Ok(ApiResponseDto<IEnumerable<object>>.SuccessResponse(userList));
    }

    /// <summary>
    /// Gets all users in a specific department (managers/admins only)
    /// </summary>
    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpGet("department/{department}")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<object>>>> GetUsersByDepartment(string department)
    {
        var users = await _unitOfWork.Users.GetUsersByDepartmentAsync(department);
        
        var userList = users.Select(u => new
        {
            u.UserId,
            u.Name,
            u.Email,
            u.Role,
            u.Department
        });

        return Ok(ApiResponseDto<IEnumerable<object>>.SuccessResponse(userList));
    }

    /// <summary>
    /// Gets all team members under the logged-in manager
    /// </summary>
    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpGet("my-team")]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<TeamMemberDto>>>> GetMyTeam()
    {
        var managerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var employees = await _unitOfWork.Users.GetEmployeesByManagerIdAsync(managerId);

        var teamMembers = employees.Select(e => new TeamMemberDto
        {
            UserId = e.UserId,
            Name = e.Name
        });

        return Ok(ApiResponseDto<IEnumerable<TeamMemberDto>>.SuccessResponse(teamMembers));
    }

    /// <summary>
    /// Manager dashboard stats
    /// </summary>
    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpGet("manager-dashboard/{managerId}")]
    public async Task<IActionResult> GetManagerDashboard(int managerId)
    {
        // 1) Fetch team count using async count
        var teamCount = await _unitOfWork.Users.GetEmployeesCountByManagerIdAsync(managerId);

        // 2) Fetch team members list (sequential)
        var teamMembers = await _unitOfWork.Users.GetEmployeesByManagerIdAsync(managerId);
        var teamMemberIds = teamMembers.Select(u => u.UserId).ToList();

        // 3) Fetch total team hours for today using repository async Sum (sequential)
        // Use DateTime.Today.Date to match only the date portion
        var today = DateTime.Today.Date;
        decimal teamHoursToday = 0m;
        if (teamMemberIds.Any())
        {
            teamHoursToday = await _unitOfWork.TimeLogs.GetTotalHoursByUsersForDateAsync(teamMemberIds, today);
        }

        // 4) Fetch active tasks count for the team using repository async Count (sequential)
        var activeTasks = 0;
        if (teamMemberIds.Any())
        {
            activeTasks = await _unitOfWork.Tasks.GetActiveTasksCountForUsersAsync(teamMemberIds);
        }

        var payload = new
        {
            teamCount = teamCount,
            teamHoursToday = teamHoursToday,
            activeTasks = activeTasks
        };

        return Ok(new { success = true, data = payload });
    }

    /// <summary>
    /// Deactivates a user account (admins only)
    /// </summary>
    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("{userId}/deactivate")]
    public async Task<ActionResult<ApiResponseDto<bool>>> DeactivateUser(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound(ApiResponseDto<bool>.ErrorResponse("User not found"));
        }

        user.Status = "Inactive";
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponseDto<bool>.SuccessResponse(true, "User deactivated successfully"));
    }

    /// <summary>
    /// Reactivates a user account (admins only)
    /// </summary>
    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("{userId}/activate")]
    public async Task<ActionResult<ApiResponseDto<bool>>> ActivateUser(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound(ApiResponseDto<bool>.ErrorResponse("User not found"));
        }

        user.Status = "Active";
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponseDto<bool>.SuccessResponse(true, "User activated successfully"));
    }
}