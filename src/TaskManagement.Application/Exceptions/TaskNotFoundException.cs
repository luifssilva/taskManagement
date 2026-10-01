namespace TaskManagement.Application.Exceptions;

/// <summary>
/// Raised when a task with the requested identifier does not exist.
/// </summary>
public sealed class TaskNotFoundException : Exception
{
    public TaskNotFoundException(Guid taskId)
        : base($"Task '{taskId}' was not found.")
    {
        TaskId = taskId;
    }

    public Guid TaskId { get; }
}
