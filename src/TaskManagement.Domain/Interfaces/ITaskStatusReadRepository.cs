using TaskManagement.Domain.Entities;

namespace TaskManagement.Domain.Interfaces;

/// <summary>
/// Read-only access to the status domain table.
/// </summary>
public interface ITaskStatusReadRepository
{
    Task<IReadOnlyList<TaskItemStatusDefinition>> ListAsync(CancellationToken cancellationToken = default);
}
