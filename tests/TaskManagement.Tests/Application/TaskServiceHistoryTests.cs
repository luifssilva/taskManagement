using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Support;

namespace TaskManagement.Tests.Application;

public class TaskServiceHistoryTests
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
}
