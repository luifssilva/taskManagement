using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

/// <summary>
/// Use cases for managing tasks.
/// </summary>
/// <remarks>
/// Invalid input raises <see cref="FluentValidation.ValidationException"/>;
/// unknown identifiers raise <see cref="Exceptions.TaskNotFoundException"/>.
/// </remarks>
public interface ITaskService
{
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);

    Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskResponse>> ListAsync(TaskFilterRequest filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskResponse>> SearchAsync(string? term, CancellationToken cancellationToken = default);

    Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
