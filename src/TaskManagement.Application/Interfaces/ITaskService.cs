using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

/// <summary>
/// Use cases for managing tasks.
/// </summary>
/// <remarks>
/// Invalid input raises <see cref="FluentValidation.ValidationException"/>;
/// unknown identifiers raise <see cref="Exceptions.TaskNotFoundException"/>;
/// a stale <c>expectedVersion</c> raises <see cref="Domain.Exceptions.ConcurrencyConflictException"/>.
/// </remarks>
public interface ITaskService
{
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);

    Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskStatusChangeResponse>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskResponse>> ListAsync(TaskFilterRequest filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskResponse>> SearchAsync(string? term, CancellationToken cancellationToken = default);

    /// <param name="id">Task identifier.</param>
    /// <param name="request">New task data.</param>
    /// <param name="expectedVersion">When set, the update only happens if the task is still at this version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TaskResponse> UpdateAsync(
        Guid id,
        UpdateTaskRequest request,
        int? expectedVersion = null,
        CancellationToken cancellationToken = default);

    /// <param name="id">Task identifier.</param>
    /// <param name="expectedVersion">When set, the deletion only happens if the task is still at this version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid id, int? expectedVersion = null, CancellationToken cancellationToken = default);
}
