using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

/// <summary>
/// Queries over the status domain table.
/// </summary>
public interface ITaskStatusService
{
    Task<IReadOnlyList<TaskStatusResponse>> ListAsync(CancellationToken cancellationToken = default);
}
