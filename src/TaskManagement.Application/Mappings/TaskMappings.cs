using TaskManagement.Application.DTOs;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Mappings;

internal static class TaskMappings
{
    public static TaskResponse ToResponse(this TaskItem task, DateOnly today) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        DueDate = task.DueDate,
        Status = task.Status,
        IsOverdue = task.IsOverdue(today),
        Version = task.Version,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };

    public static IReadOnlyList<TaskResponse> ToResponse(this IEnumerable<TaskItem> tasks, DateOnly today) =>
        tasks.Select(task => task.ToResponse(today)).ToList();

    public static IReadOnlyList<TaskStatusChangeResponse> ToHistoryResponse(this TaskItem task) =>
        task.StatusHistory
            .OrderBy(change => change.ChangedAt)
            .Select(change => new TaskStatusChangeResponse
            {
                FromStatus = change.FromStatus,
                ToStatus = change.ToStatus,
                ChangedAt = change.ChangedAt
            })
            .ToList();

    public static TaskStatusResponse ToResponse(this TaskItemStatusDefinition status) => new()
    {
        Id = (int)status.Id,
        Name = status.Id,
        Description = status.Description
    };
}
