using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs;

/// <summary>
/// One entry of a task's status history.
/// </summary>
public sealed record TaskStatusChangeResponse
{
    /// <summary>Previous status; null for the entry created with the task.</summary>
    /// <example>Pendente</example>
    public TaskItemStatus? FromStatus { get; init; }

    /// <summary>Status the task moved to.</summary>
    /// <example>EmProgresso</example>
    public TaskItemStatus ToStatus { get; init; }

    /// <summary>When the change happened (UTC).</summary>
    public DateTimeOffset ChangedAt { get; init; }
}
