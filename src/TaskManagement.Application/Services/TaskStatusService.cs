using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Mappings;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Services;

public sealed class TaskStatusService : ITaskStatusService
{
    private readonly ITaskStatusReadRepository _repository;

    public TaskStatusService(ITaskStatusReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TaskStatusResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await _repository.ListAsync(cancellationToken);
        return statuses.Select(status => status.ToResponse()).ToList();
    }
}
