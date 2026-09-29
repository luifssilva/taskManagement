using TaskManagement.Domain.Entities;

namespace TaskManagement.Domain.Interfaces;

/// <summary>
/// Persistence of task changes. Every method commits its change.
/// </summary>
public interface ITaskWriteRepository
{
    /// <summary>Loads a task that is going to be modified.</summary>
    Task<TaskItem?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);

    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);

    Task RemoveAsync(TaskItem task, CancellationToken cancellationToken = default);
}
