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

        var now = DateTime.UtcNow;
        var status = dto.Status ?? "Pending";

        var taskEntity = new TaskEntity
        {
            Title = dto.Title,
            Description = dto.Description,
            AssignedToUserId = dto.AssignedToUserId,
            CreatedByUserId = creatorId,
            ProjectId = dto.ProjectId,
            EstimatedHours = dto.EstimatedHours,
            Status = status,
            Priority = dto.Priority,
            DueDate = dto.DueDate,
            CreatedDate = now,
            // Set real-time values based on status
            StartedDate = status is "InProgress" or "Completed" or "Approved" ? now : null,
            CompletedDate = status is "Completed" or "Approved" ? now : null,
            IsApproved = status == "Approved",
            ApprovedDate = status == "Approved" ? now : null,
            ApprovedByUserId = status == "Approved" ? creatorId : null
        };

        await _unitOfWork.Tasks.AddAsync(taskEntity);
        await _unitOfWork.SaveChangesAsync();

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

        var now = DateTime.UtcNow;
        var previousStatus = taskEntity.Status;
        var newStatus = dto.Status ?? taskEntity.Status;

        taskEntity.Title = dto.Title;
        taskEntity.Description = dto.Description;
        taskEntity.AssignedToUserId = dto.AssignedToUserId;
        taskEntity.ProjectId = dto.ProjectId;
        taskEntity.EstimatedHours = dto.EstimatedHours;
        taskEntity.Priority = dto.Priority;
        taskEntity.DueDate = dto.DueDate;

        // Update status and set real-time timestamps when status changes
        if (newStatus != previousStatus)
        {
            taskEntity.Status = newStatus;

            // Set StartedDate when moving to InProgress
            if (newStatus == "InProgress" && taskEntity.StartedDate == null)
            {
                taskEntity.StartedDate = now;
            }

            // Set CompletedDate when moving to Completed
            if (newStatus == "Completed")
            {
                taskEntity.CompletedDate = now;
                taskEntity.IsApproved = false; // Pending approval
            }

            // Set ApprovedDate when moving to Approved
            if (newStatus == "Approved")
            {
                taskEntity.CompletedDate ??= now;
                taskEntity.IsApproved = true;
                taskEntity.ApprovedDate = now;
                // ApprovedByUserId should be set via ApproveTaskAsync with manager ID
            }

            // Reset dates if moving backwards in workflow
            if (newStatus == "Pending")
            {
                taskEntity.StartedDate = null;
                taskEntity.CompletedDate = null;
                taskEntity.IsApproved = false;
                taskEntity.ApprovedDate = null;
                taskEntity.ApprovedByUserId = null;
            }

            if (newStatus == "InProgress" && previousStatus is "Completed" or "Approved")
            {
                taskEntity.CompletedDate = null;
                taskEntity.IsApproved = false;
                taskEntity.ApprovedDate = null;
                taskEntity.ApprovedByUserId = null;
            }
        }

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

        if (taskEntity.Status == "Completed" || taskEntity.IsApproved)
        {
            throw new InvalidOperationException("Cannot delete completed or approved tasks");
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

    // ==================== NEW: START TASK ====================
    public async Task<TaskResponseDto> StartTaskAsync(int taskId, int userId)
    {
        var taskEntity = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (taskEntity == null)
        {
            throw new KeyNotFoundException($"Task with ID {taskId} not found");
        }

        if (taskEntity.AssignedToUserId != userId)
        {
            throw new UnauthorizedAccessException("You can only start tasks assigned to you");
        }

        if (taskEntity.Status != "Pending")
        {
            throw new InvalidOperationException($"Cannot start task. Current status: {taskEntity.Status}. Only 'Pending' tasks can be started.");
        }

        taskEntity.Status = "InProgress";
        taskEntity.StartedDate = DateTime.UtcNow;

        _unitOfWork.Tasks.Update(taskEntity);
        await _unitOfWork.SaveChangesAsync();

        // Notify manager that task has been started
        await _notificationService.CreateNotificationAsync(
            taskEntity.CreatedByUserId,
            "TaskStarted",
            $"Task '{taskEntity.Title}' has been started by {taskEntity.AssignedToUser?.Name}"
        );

        return await BuildTaskResponseDto(taskEntity);
    }

    // ==================== NEW: COMPLETE TASK ====================
    public async Task<TaskResponseDto> CompleteTaskAsync(int taskId, int userId)
    {
        var taskEntity = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (taskEntity == null)
        {
            throw new KeyNotFoundException($"Task with ID {taskId} not found");
        }

        if (taskEntity.AssignedToUserId != userId)
        {
            throw new UnauthorizedAccessException("You can only complete tasks assigned to you");
        }

        if (taskEntity.Status != "InProgress")
        {
            throw new InvalidOperationException($"Cannot complete task. Current status: {taskEntity.Status}. Only 'InProgress' tasks can be completed.");
        }

        taskEntity.Status = "Completed";
        taskEntity.CompletedDate = DateTime.UtcNow;
        taskEntity.IsApproved = false; // Pending approval

        _unitOfWork.Tasks.Update(taskEntity);
        await _unitOfWork.SaveChangesAsync();

        // Notify manager that task is completed and pending approval
        await _notificationService.CreateNotificationAsync(
            taskEntity.CreatedByUserId,
            "TaskPendingApproval",
            $"Task '{taskEntity.Title}' has been completed by {taskEntity.AssignedToUser?.Name} and is awaiting your approval"
        );

        return await BuildTaskResponseDto(taskEntity);
    }

    // ==================== NEW: APPROVE TASK ====================
    public async Task<TaskResponseDto> ApproveTaskAsync(int taskId, int managerId)
    {
        var taskEntity = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (taskEntity == null)
        {
            throw new KeyNotFoundException($"Task with ID {taskId} not found");
        }

        if (taskEntity.Status != "Completed")
        {
            throw new InvalidOperationException("Only completed tasks can be approved");
        }

        if (taskEntity.IsApproved)
        {
            throw new InvalidOperationException("Task is already approved");
        }

        taskEntity.IsApproved = true;
        taskEntity.ApprovedDate = DateTime.UtcNow;
        taskEntity.ApprovedByUserId = managerId;
        taskEntity.Status = "Approved";

        _unitOfWork.Tasks.Update(taskEntity);
        await _unitOfWork.SaveChangesAsync();

        // Notify employee that task has been approved
        await _notificationService.CreateNotificationAsync(
            taskEntity.AssignedToUserId,
            "TaskApproved",
            $"Your task '{taskEntity.Title}' has been approved!"
        );

        return await BuildTaskResponseDto(taskEntity);
    }

    // ==================== NEW: REJECT TASK ====================
    public async Task<TaskResponseDto> RejectTaskAsync(int taskId, int managerId, string reason)
    {
        var taskEntity = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (taskEntity == null)
        {
            throw new KeyNotFoundException($"Task with ID {taskId} not found");
        }

        if (taskEntity.Status != "Completed")
        {
            throw new InvalidOperationException("Only completed tasks can be rejected");
        }

        // Send back to InProgress
        taskEntity.Status = "InProgress";
        taskEntity.CompletedDate = null;
        taskEntity.IsApproved = false;

        _unitOfWork.Tasks.Update(taskEntity);
        await _unitOfWork.SaveChangesAsync();

        // Notify employee about rejection
        await _notificationService.CreateNotificationAsync(
            taskEntity.AssignedToUserId,
            "TaskRejected",
            $"Your task '{taskEntity.Title}' was rejected. Reason: {reason}"
        );

        return await BuildTaskResponseDto(taskEntity);
    }

    // ==================== NEW: GET TASKS PENDING APPROVAL ====================
    public async Task<IEnumerable<TaskResponseDto>> GetTasksPendingApprovalAsync(int managerId)
    {
        var tasks = await _unitOfWork.Tasks.GetTasksByCreatorAsync(managerId);
        var pendingApprovalTasks = tasks.Where(t => t.Status == "Completed" && !t.IsApproved);
        
        var taskResponses = new List<TaskResponseDto>();
        foreach (var task in pendingApprovalTasks)
        {
            taskResponses.Add(await BuildTaskResponseDto(task));
        }

        return taskResponses;
    }

    public async Task<bool> UpdateTaskStatusAsync(int taskId, string status)
    {
        var validStatuses = new[] { "Pending", "InProgress", "Completed", "Approved" };
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

        if (status == "InProgress" && taskEntity.StartedDate == null)
        {
            taskEntity.StartedDate = DateTime.UtcNow;
        }

        if (status == "Completed")
        {
            taskEntity.CompletedDate = DateTime.UtcNow;
            
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
            StartedDate = task.StartedDate,
            CompletedDate = task.CompletedDate,
            IsApproved = task.IsApproved,
            ApprovedDate = task.ApprovedDate,
            ApprovedByUserName = task.ApprovedByUser?.Name
        };
    }
}