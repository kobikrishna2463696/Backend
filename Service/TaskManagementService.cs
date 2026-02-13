using TimeTrack.API.DTOs.Task;
using TimeTrack.API.Models;
using TimeTrack.API.Repository.IRepository;

namespace TimeTrack.API.Service;

public class TaskManagementService : ITaskManagementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public TaskManagementService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<TaskResponseDto> CreateTaskAsync(int creatorId, CreateTaskDto dto)
    {
        var assignedUser = await _unitOfWork.Users.GetByIdAsync(dto.AssignedToUserId);
        if (assignedUser == null || assignedUser.Status != "Active")
        {
            throw new InvalidOperationException("Cannot assign task to inactive or non-existent user");
        }

        var taskEntity = new TaskEntity
        {
            Title = dto.Title,
            Description = dto.Description,
            AssignedToUserId = dto.AssignedToUserId,
            CreatedByUserId = creatorId,
            ProjectId = dto.ProjectId,
            EstimatedHours = dto.EstimatedHours,
            Status = "Pending",
            Priority = dto.Priority,
            DueDate = dto.DueDate,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.Tasks.AddAsync(taskEntity);
        await _unitOfWork.SaveChangesAsync();

        // Send notification to assigned user
        await _notificationService.SendTaskAssignmentNotificationAsync(
            dto.AssignedToUserId, 
            dto.Title
        );

        return await BuildTaskResponseDto(taskEntity);
    }

    public async Task<TaskResponseDto> UpdateTaskAsync(int taskId, CreateTaskDto dto)
    {
        var taskEntity = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (taskEntity == null)
        {
            throw new KeyNotFoundException($"Task with ID {taskId} not found");
        }

        taskEntity.Title = dto.Title;
        taskEntity.Description = dto.Description;
        taskEntity.AssignedToUserId = dto.AssignedToUserId;
        taskEntity.ProjectId = dto.ProjectId;
        taskEntity.EstimatedHours = dto.EstimatedHours;
        taskEntity.Priority = dto.Priority;
        taskEntity.Status = dto.Status;
        taskEntity.DueDate = dto.DueDate;

        _unitOfWork.Tasks.Update(taskEntity);
        await _unitOfWork.SaveChangesAsync();

        return await BuildTaskResponseDto(taskEntity);
    }

    public async Task<bool> DeleteTaskAsync(int taskId)
    {
        var taskEntity = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (taskEntity == null)
        {
            return false;
        }

        if (taskEntity.Status == "Completed")
        {
            throw new InvalidOperationException("Cannot delete completed tasks");
        }

        _unitOfWork.Tasks.Delete(taskEntity);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<TaskResponseDto> GetTaskByIdAsync(int taskId)
    {
        var taskEntity = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (taskEntity == null)
        {
            throw new KeyNotFoundException($"Task with ID {taskId} not found");
        }

        return await BuildTaskResponseDto(taskEntity);
    }

    public async Task<IEnumerable<TaskResponseDto>> GetUserTasksAsync(int userId)
    {
        var tasks = await _unitOfWork.Tasks.GetTasksByAssignedUserAsync(userId);
        var taskResponses = new List<TaskResponseDto>();

        foreach (var task in tasks)
        {
            taskResponses.Add(await BuildTaskResponseDto(task));
        }

        return taskResponses;
    }

    public async Task<IEnumerable<TaskResponseDto>> GetCreatedTasksAsync(int creatorId)
    {
        var tasks = await _unitOfWork.Tasks.GetTasksByCreatorAsync(creatorId);
        var taskResponses = new List<TaskResponseDto>();

        foreach (var task in tasks)
        {
            taskResponses.Add(await BuildTaskResponseDto(task));
        }

        return taskResponses;
    }

    public async Task<bool> UpdateTaskStatusAsync(int taskId, string status)
    {
        var validStatuses = new[] { "Pending", "InProgress", "Completed" };
        if (!validStatuses.Contains(status))
        {
            throw new ArgumentException($"Invalid status. Valid statuses: {string.Join(", ", validStatuses)}");
        }

        var taskEntity = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (taskEntity == null)
        {
            return false;
        }

        taskEntity.Status = status;

        if (status == "Completed")
        {
            taskEntity.CompletedDate = DateTime.UtcNow;
            
            // Notify task creator about completion
            await _notificationService.CreateNotificationAsync(
                taskEntity.CreatedByUserId,
                "TaskCompleted",
                $"Task '{taskEntity.Title}' has been completed by {taskEntity.AssignedToUser?.Name}"
            );
        }

        _unitOfWork.Tasks.Update(taskEntity);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> LogTaskTimeAsync(int userId, LogTaskTimeDto dto)
    {
        var taskEntity = await _unitOfWork.Tasks.GetByIdAsync(dto.TaskId);
        if (taskEntity == null)
        {
            throw new KeyNotFoundException($"Task with ID {dto.TaskId} not found");
        }

        if (taskEntity.AssignedToUserId != userId)
        {
            throw new UnauthorizedAccessException("You can only log time for tasks assigned to you");
        }

        var taskTime = new TaskTimeEntity
        {
            TaskId = dto.TaskId,
            UserId = userId,
            Date = dto.Date.Date,
            HoursSpent = dto.HoursSpent,
            WorkDescription = dto.WorkDescription,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.TaskTimes.AddAsync(taskTime);

        // Auto-update task status to InProgress if currently Pending
        if (taskEntity.Status == "Pending")
        {
            taskEntity.Status = "InProgress";
            _unitOfWork.Tasks.Update(taskEntity);
        }

        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<TaskResponseDto>> GetOverdueTasksAsync()
    {
        var overdueTasks = await _unitOfWork.Tasks.GetOverdueTasksAsync();
        var taskResponses = new List<TaskResponseDto>();

        foreach (var task in overdueTasks)
        {
            taskResponses.Add(await BuildTaskResponseDto(task));
        }

        return taskResponses;
    }

    private async Task<TaskResponseDto> BuildTaskResponseDto(TaskEntity task)
    {
        var actualHours = await _unitOfWork.TaskTimes.GetTotalHoursForTaskAsync(task.TaskId);

        return new TaskResponseDto
        {
            TaskId = task.TaskId,
            Title = task.Title,
            Description = task.Description,
            AssignedToUserId = task.AssignedToUserId,
            AssignedToUserName = task.AssignedToUser?.Name ?? "Unknown",
            CreatedByUserId = task.CreatedByUserId,
            CreatedByUserName = task.CreatedByUser?.Name ?? "Unknown",
            ProjectId = task.ProjectId,
            ProjectName = task.Project?.ProjectName,
            EstimatedHours = task.EstimatedHours,
            ActualHoursSpent = actualHours,
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            CreatedDate = task.CreatedDate,
            CompletedDate = task.CompletedDate
        };
    }
}