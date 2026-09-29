using TaskManagement.Application.DTOs;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Mappings;

internal static class TaskMappings
{
    public static TaskResponse ToResponse(this TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        DueDate = task.DueDate,
        Status = task.Status.ToDisplayName(),
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };

    public static IReadOnlyList<TaskResponse> ToResponse(this IEnumerable<TaskItem> tasks) =>
        tasks.Select(ToResponse).ToList();
}
