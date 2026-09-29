namespace TaskManagement.Domain.Exceptions;

/// <summary>
/// Raised when a task was changed by someone else since the version the caller based its change on.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(Guid taskId)
        : base($"Task '{taskId}' was modified by another request. Reload it and try again.")
    {
        TaskId = taskId;
    }

    public Guid TaskId { get; }
}
