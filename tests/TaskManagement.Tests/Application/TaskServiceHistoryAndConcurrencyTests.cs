using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;
using TaskManagement.Tests.Support;

namespace TaskManagement.Tests.Application;

public class TaskServiceHistoryAndConcurrencyTests
{
    private readonly TaskServiceBuilder _builder = new();

    private static UpdateTaskRequest UpdateWithStatus(TaskItemStatus status) => new()
    {
        Title = "Conferir carga do pedido 4521",
        Status = status
    };

    [Fact]
    public async Task GetHistoryAsync_NewTask_HasInitialStatusOnly()
    {
        var service = _builder.Build();
        var created = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());

        var history = await service.GetHistoryAsync(created.Id);

        var entry = Assert.Single(history);
        Assert.Null(entry.FromStatus);
        Assert.Equal(TaskItemStatus.Pendente, entry.ToStatus);
        Assert.Equal(TaskServiceBuilder.Now, entry.ChangedAt);
    }

    [Fact]
    public async Task GetHistoryAsync_RecordsEveryStatusChangeInOrder()
    {
        var service = _builder.Build();
        var created = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());

        _builder.Clock.Now = TaskServiceBuilder.Now.AddHours(1);
        await service.UpdateAsync(created.Id, UpdateWithStatus(TaskItemStatus.EmProgresso));
        _builder.Clock.Now = TaskServiceBuilder.Now.AddHours(2);
        await service.UpdateAsync(created.Id, UpdateWithStatus(TaskItemStatus.Concluida));
        _builder.Clock.Now = TaskServiceBuilder.Now.AddHours(3);
        await service.UpdateAsync(created.Id, UpdateWithStatus(TaskItemStatus.EmProgresso));

        var history = await service.GetHistoryAsync(created.Id);

        Assert.Equal(
            [
                ((TaskItemStatus?)null, TaskItemStatus.Pendente, TaskServiceBuilder.Now),
                (TaskItemStatus.Pendente, TaskItemStatus.EmProgresso, TaskServiceBuilder.Now.AddHours(1)),
                (TaskItemStatus.EmProgresso, TaskItemStatus.Concluida, TaskServiceBuilder.Now.AddHours(2)),
                (TaskItemStatus.Concluida, TaskItemStatus.EmProgresso, TaskServiceBuilder.Now.AddHours(3))
            ],
            history.Select(h => (h.FromStatus, h.ToStatus, h.ChangedAt)));
    }

    [Fact]
    public async Task GetHistoryAsync_UpdateWithoutStatusChange_AddsNoEntry()
    {
        var service = _builder.Build();
        var created = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());

        await service.UpdateAsync(created.Id, UpdateWithStatus(TaskItemStatus.Pendente) with { Description = "Nova descrição" });

        Assert.Single(await service.GetHistoryAsync(created.Id));
    }

    [Fact]
    public async Task GetHistoryAsync_WhenTaskDoesNotExist_ThrowsTaskNotFoundException()
    {
        var service = _builder.Build();

        await Assert.ThrowsAsync<TaskNotFoundException>(() => service.GetHistoryAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_IncrementsVersion()
    {
        var service = _builder.Build();
        var created = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());

        var updated = await service.UpdateAsync(created.Id, UpdateWithStatus(TaskItemStatus.EmProgresso));

        Assert.Equal(1, created.Version);
        Assert.Equal(2, updated.Version);
    }

    [Fact]
    public async Task UpdateAsync_WithCurrentVersion_Succeeds()
    {
        var service = _builder.Build();
        var created = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());

        var updated = await service.UpdateAsync(created.Id, UpdateWithStatus(TaskItemStatus.EmProgresso), expectedVersion: created.Version);

        Assert.Equal(TaskItemStatus.EmProgresso, updated.Status);
    }

    [Fact]
    public async Task UpdateAsync_WithStaleVersion_ThrowsConflictAndKeepsTask()
    {
        var service = _builder.Build();
        var created = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());
        var afterOtherOperator = await service.UpdateAsync(created.Id, UpdateWithStatus(TaskItemStatus.EmProgresso));

        var exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => service.UpdateAsync(created.Id, UpdateWithStatus(TaskItemStatus.Concluida), expectedVersion: created.Version));

        Assert.Equal(created.Id, exception.TaskId);
        Assert.Equal(afterOtherOperator, await service.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task UpdateAsync_MissingTaskWithVersion_ThrowsNotFound()
    {
        var service = _builder.Build();

        await Assert.ThrowsAsync<TaskNotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), UpdateWithStatus(TaskItemStatus.Concluida), expectedVersion: 1));
    }

    [Fact]
    public async Task DeleteAsync_WithStaleVersion_ThrowsConflictAndKeepsTask()
    {
        var service = _builder.Build();
        var created = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());
        await service.UpdateAsync(created.Id, UpdateWithStatus(TaskItemStatus.EmProgresso));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => service.DeleteAsync(created.Id, expectedVersion: created.Version));

        Assert.NotNull(await service.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_WithCurrentVersion_RemovesTask()
    {
        var service = _builder.Build();
        var created = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());

        await service.DeleteAsync(created.Id, expectedVersion: created.Version);

        await Assert.ThrowsAsync<TaskNotFoundException>(() => service.GetByIdAsync(created.Id));
    }
}
