using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Services;
using TaskManagement.Application.Validators;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Repositories;

namespace TaskManagement.Tests.Support;

/// <summary>
/// Builds a <see cref="TaskService"/> backed by an isolated EF Core InMemory database
/// (with the status domain table seeded and no tasks), so each test starts from a clean store.
/// </summary>
internal sealed class TaskServiceBuilder
{
    public static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    public TaskServiceBuilder()
    {
        Context = new TaskDbContext(
            new DbContextOptionsBuilder<TaskDbContext>()
                .UseInMemoryDatabase($"tests-{Guid.NewGuid()}")
                .Options);
        Context.Database.EnsureCreated();
    }

    public FixedTimeProvider Clock { get; } = new(Now);

    public TaskDbContext Context { get; }

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
        string title = "Conferir carga do pedido 4521",
        string? description = "Validar volumes e lacres antes da expedição",
        DateOnly? dueDate = null,
        TaskItemStatus status = TaskItemStatus.Pendente) => new()
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

    // Keeps "today" deterministic regardless of the machine running the tests.
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => Now;
}
