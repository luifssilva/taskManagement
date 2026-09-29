using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Queries;

namespace TaskManagement.Domain.Interfaces;

/// <summary>
/// Read-only access to tasks.
/// </summary>
public interface ITaskReadRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lists tasks matching <paramref name="criteria"/>.</summary>
    Task<IReadOnlyList<TaskItem>> ListAsync(TaskListCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>Finds tasks whose title or description contains <paramref name="term"/> (case-insensitive).</summary>
    Task<IReadOnlyList<TaskItem>> SearchAsync(string term, CancellationToken cancellationToken = default);
}
