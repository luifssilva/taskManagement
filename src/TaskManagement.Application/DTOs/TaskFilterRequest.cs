using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs;

/// <summary>
/// Optional filters for listing tasks. Filters are combined with AND.
/// </summary>
public sealed record TaskFilterRequest
{
    /// <summary>Only tasks with this status: Pendente, EmProgresso or Concluida.</summary>
    /// <example>Pendente</example>
    public TaskItemStatus? Status { get; init; }

    /// <summary>Only tasks due on this date (yyyy-MM-dd).</summary>
    /// <example>2026-10-01</example>
    public DateOnly? DueDate { get; init; }

    /// <summary>
    /// true: only overdue tasks (due date before today and not Concluida);
    /// false: only tasks that are not overdue.
    /// </summary>
    /// <example>true</example>
    public bool? Overdue { get; init; }
}
