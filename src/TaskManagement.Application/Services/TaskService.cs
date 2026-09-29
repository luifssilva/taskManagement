using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Mappings;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;

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

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);

        var task = TaskItem.Create(
            request.Title!,
            request.Description,
            request.DueDate,
            ParseStatus(request.Status),
            _timeProvider.GetUtcNow());

        await _writeRepository.AddAsync(task, cancellationToken);

        _logger.LogInformation("Task {TaskId} created with status {Status}", task.Id, task.Status);
        return task.ToResponse();
    }

    public async Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _readRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new TaskNotFoundException(id);

        return task.ToResponse();
    }

    public async Task<IReadOnlyList<TaskResponse>> ListAsync(
        TaskFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_filterValidator, filter, cancellationToken);

        TaskItemStatus? status = filter.Status is null ? null : ParseStatus(filter.Status);
        var tasks = await _readRepository.ListAsync(status, filter.DueDate, cancellationToken);

        return tasks.ToResponse();
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
        return tasks.ToResponse();
    }

    public async Task<TaskResponse> UpdateAsync(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);

        var task = await _writeRepository.FindForUpdateAsync(id, cancellationToken)
                   ?? throw new TaskNotFoundException(id);

        task.Update(
            request.Title!,
            request.Description,
            request.DueDate,
            ParseStatus(request.Status),
            _timeProvider.GetUtcNow());

        await _writeRepository.UpdateAsync(task, cancellationToken);

        _logger.LogInformation("Task {TaskId} updated with status {Status}", task.Id, task.Status);
        return task.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _writeRepository.FindForUpdateAsync(id, cancellationToken)
                   ?? throw new TaskNotFoundException(id);

        await _writeRepository.RemoveAsync(task, cancellationToken);

        _logger.LogInformation("Task {TaskId} deleted", id);
    }

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

    // Only called after validation, so an unknown value here is a programming error.
    private static TaskItemStatus ParseStatus(string? status) =>
        TaskItemStatusExtensions.TryParse(status, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Status '{status}' should have been rejected by validation.");
}
