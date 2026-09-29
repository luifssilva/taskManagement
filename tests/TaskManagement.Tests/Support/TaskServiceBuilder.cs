using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Services;
using TaskManagement.Application.Validators;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Repositories;

namespace TaskManagement.Tests.Support;

/// <summary>
/// Builds a <see cref="TaskService"/> backed by an isolated EF Core InMemory database,
/// so each test starts from an empty store.
/// </summary>
internal sealed class TaskServiceBuilder
{
    public static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    public FixedTimeProvider Clock { get; } = new(Now);

    public TaskDbContext Context { get; } = new(
        new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase($"tests-{Guid.NewGuid()}")
            .Options);

    public TaskService Build()
    {
        var repository = new TaskRepository(Context);

        return new TaskService(
            repository,
            repository,
            new CreateTaskRequestValidator(),
            new UpdateTaskRequestValidator(),
            new TaskFilterRequestValidator(),
            Clock,
            NullLogger<TaskService>.Instance);
    }

    public static CreateTaskRequest ValidCreateRequest(
        string title = "Implementar API",
        string? description = "Criar endpoints de tarefas",
        DateOnly? dueDate = null,
        string status = "Pendente") => new()
    {
        Title = title,
        Description = description,
        DueDate = dueDate ?? new DateOnly(2026, 10, 1),
        Status = status
    };
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
