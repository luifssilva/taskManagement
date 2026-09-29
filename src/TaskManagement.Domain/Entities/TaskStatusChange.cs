using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

/// <summary>
/// Value object recording that a task moved to a new status at a given moment,
/// giving each task a traceable history.
/// </summary>
public sealed class TaskStatusChange
{
    public TaskStatusChange(TaskItemStatus? fromStatus, TaskItemStatus toStatus, DateTimeOffset changedAt)
    {
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedAt = changedAt;
    }

    // Required by EF Core.
    private TaskStatusChange()
    {
    }

    /// <summary>Previous status; <c>null</c> for the entry created with the task.</summary>
    public TaskItemStatus? FromStatus { get; private set; }

    public TaskItemStatus ToStatus { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }
}
