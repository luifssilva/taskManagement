using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Support;

namespace TaskManagement.Tests.Application;

public class TaskServiceCommandTests
{
    private readonly TaskServiceBuilder _builder = new();

    private static UpdateTaskRequest ValidUpdateRequest() => new()
    {
        Title = "Título atualizado",
        Description = "Descrição atualizada",
        DueDate = new DateOnly(2026, 10, 15),
        Status = TaskItemStatus.EmProgresso
    };

    [Fact]
    public async Task UpdateAsync_WhenTaskExists_UpdatesAllFields()
    {
        var service = _builder.Build();
        var created = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());
        var updatedAt = TaskServiceBuilder.Now.AddHours(1);
        _builder.Clock.Now = updatedAt;

        var updated = await service.UpdateAsync(created.Id, ValidUpdateRequest());

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Título atualizado", updated.Title);
        Assert.Equal("Descrição atualizada", updated.Description);
        Assert.Equal(new DateOnly(2026, 10, 15), updated.DueDate);
        Assert.Equal(TaskItemStatus.EmProgresso, updated.Status);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.Equal(updatedAt, updated.UpdatedAt);

        var persisted = await service.GetByIdAsync(created.Id);
        Assert.Equal(updated, persisted);
    }

    [Fact]
    public async Task UpdateAsync_WithoutOptionalFields_ClearsThem()
    {
        var service = _builder.Build();
        var created = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());

        var updated = await service.UpdateAsync(created.Id, new UpdateTaskRequest { Title = "Só título", Status = TaskItemStatus.Concluida });

        Assert.Null(updated.Description);
        Assert.Null(updated.DueDate);
        Assert.Equal(TaskItemStatus.Concluida, updated.Status);
    }

    [Fact]
    public async Task UpdateAsync_WhenTaskDoesNotExist_ThrowsTaskNotFoundException()
    {
        var service = _builder.Build();
        var id = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<TaskNotFoundException>(
            () => service.UpdateAsync(id, ValidUpdateRequest()));

        Assert.Equal(id, exception.TaskId);
    }

    [Theory]
    [InlineData("", TaskItemStatus.Pendente, nameof(UpdateTaskRequest.Title))]
    [InlineData("Título", (TaskItemStatus)99, nameof(UpdateTaskRequest.Status))]
    [InlineData("Título", null, nameof(UpdateTaskRequest.Status))]
    public async Task UpdateAsync_WithInvalidData_ThrowsValidationExceptionAndKeepsTask(
        string title,
        TaskItemStatus? status,
        string invalidProperty)
    {
        var service = _builder.Build();
        var created = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());
        var request = new UpdateTaskRequest { Title = title, Status = status };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(created.Id, request));

        Assert.Contains(exception.Errors, e => e.PropertyName == invalidProperty);
        Assert.Equal(created, await service.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidDataForMissingTask_ReportsValidationFirst()
    {
        var service = _builder.Build();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateAsync(Guid.NewGuid(), new UpdateTaskRequest { Title = "" }));
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskExists_RemovesTask()
    {
        var service = _builder.Build();
        var created = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());
        var other = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest(title: "Outra"));

        await service.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<TaskNotFoundException>(() => service.GetByIdAsync(created.Id));
        var remaining = await _builder.Context.Tasks.Select(t => t.Id).ToListAsync();
        Assert.Equal([other.Id], remaining);
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskDoesNotExist_ThrowsTaskNotFoundException()
    {
        var service = _builder.Build();
        var id = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<TaskNotFoundException>(() => service.DeleteAsync(id));

        Assert.Equal(id, exception.TaskId);
    }

    [Fact]
    public async Task DeleteAsync_CalledTwice_ThrowsTaskNotFoundOnSecondCall()
    {
        var service = _builder.Build();
        var created = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());

        await service.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<TaskNotFoundException>(() => service.DeleteAsync(created.Id));
    }
}
