using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Mappings;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Domain.Queries;

namespace TaskManagement.Application.Services;

public sealed class TaskService : ITaskService
{
    private readonly ITaskReadRepository _readRepository;
    private readonly ITaskWriteRepository _writeRepository;
    private readonly IValidator<CreateTaskRequest> _createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator;
    private readonly IValidator<TaskFilterRequest> _filterValidator;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ITaskReadRepository readRepository,
        ITaskWriteRepository writeRepository,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator,
        IValidator<TaskFilterRequest> filterValidator,
        TimeProvider timeProvider,
        ILogger<TaskService> logger)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _filterValidator = filterValidator;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // "Today" follows the server's local time zone, which is where the operation runs.
    private DateOnly Today => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);

        var task = TaskItem.Create(
            request.Title!,
            request.Description,
            request.DueDate,
            request.Status!.Value,
            _timeProvider.GetUtcNow());

        await _writeRepository.AddAsync(task, cancellationToken);

        _logger.LogInformation("Task {TaskId} created with status {Status}", task.Id, task.Status);
        return task.ToResponse(Today);
    }

    public async Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await GetExistingAsync(id, cancellationToken);
        return task.ToResponse(Today);
    }

    public async Task<IReadOnlyList<TaskStatusChangeResponse>> GetHistoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var task = await GetExistingAsync(id, cancellationToken);
        return task.ToHistoryResponse();
    }

    public async Task<IReadOnlyList<TaskResponse>> ListAsync(
        TaskFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_filterValidator, filter, cancellationToken);

        var today = Today;
        var criteria = new TaskListCriteria(
            filter.Status,
            filter.DueDate,
            filter.Overdue,
            today);

        var tasks = await _readRepository.ListAsync(criteria, cancellationToken);
        return tasks.ToResponse(today);
    }

    public async Task<IReadOnlyList<TaskResponse>> SearchAsync(
        string? term,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            _logger.LogWarning("Search rejected: empty search term");
            throw new ValidationException([new ValidationFailure("term", "Search term is required.")]);
        }

        var tasks = await _readRepository.SearchAsync(term.Trim(), cancellationToken);
        return tasks.ToResponse(Today);
    }

    public async Task<TaskResponse> UpdateAsync(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);

        var task = await GetForChangeAsync(id, cancellationToken);
        var previousStatus = task.Status;

        task.Update(
            request.Title!,
            request.Description,
            request.DueDate,
            request.Status!.Value,
            _timeProvider.GetUtcNow());

        await _writeRepository.UpdateAsync(task, cancellationToken);

        if (previousStatus != task.Status)
        {
            _logger.LogInformation(
                "Task {TaskId} updated; status changed from {PreviousStatus} to {Status}",
                task.Id, previousStatus, task.Status);
        }
        else
        {
            _logger.LogInformation("Task {TaskId} updated with status {Status}", task.Id, task.Status);
        }

        return task.ToResponse(Today);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await GetForChangeAsync(id, cancellationToken);

        await _writeRepository.RemoveAsync(task, cancellationToken);

        _logger.LogInformation("Task {TaskId} deleted", id);
    }

    private async Task<TaskItem> GetExistingAsync(Guid id, CancellationToken cancellationToken) =>
        await _readRepository.GetByIdAsync(id, cancellationToken)
        ?? throw new TaskNotFoundException(id);

    private async Task<TaskItem> GetForChangeAsync(Guid id, CancellationToken cancellationToken) =>
        await _writeRepository.FindForUpdateAsync(id, cancellationToken)
        ?? throw new TaskNotFoundException(id);

    private async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        _logger.LogWarning(
            "Validation failed for {RequestType}: {Properties}",
            typeof(T).Name,
            string.Join(", ", result.Errors.Select(e => e.PropertyName).Distinct()));

        throw new ValidationException(result.Errors);
    }
}
