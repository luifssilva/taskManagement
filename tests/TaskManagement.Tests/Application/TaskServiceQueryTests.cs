using FluentValidation;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Support;

namespace TaskManagement.Tests.Application;

public class TaskServiceQueryTests
{
    private static readonly DateOnly October1 = new(2026, 10, 1);
    private static readonly DateOnly October2 = new(2026, 10, 2);

    private readonly TaskServiceBuilder _builder = new();

    [Fact]
    public async Task ListAsync_WithoutFilters_ReturnsAllTasks()
    {
        var service = _builder.Build();
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest(title: "A"));
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest(title: "B"));

        var tasks = await service.ListAsync(new TaskFilterRequest());

        Assert.Equal(2, tasks.Count);
    }

    [Fact]
    public async Task ListAsync_WhenEmpty_ReturnsEmptyList()
    {
        var service = _builder.Build();

        var tasks = await service.ListAsync(new TaskFilterRequest());

        Assert.Empty(tasks);
    }

    [Fact]
    public async Task ListAsync_OrdersByDueDateWithUndatedTasksLast()
    {
        var service = _builder.Build();
        await service.CreateAsync(new CreateTaskRequest { Title = "Sem data", Status = TaskItemStatus.Pendente });
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest(title: "Depois", dueDate: October2));
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest(title: "Antes", dueDate: October1));

        var tasks = await service.ListAsync(new TaskFilterRequest());

        Assert.Equal(["Antes", "Depois", "Sem data"], tasks.Select(t => t.Title));
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskExists_ReturnsTask()
    {
        var service = _builder.Build();
        var created = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest());

        var found = await service.GetByIdAsync(created.Id);

        Assert.Equal(created, found);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskDoesNotExist_ThrowsTaskNotFoundException()
    {
        var service = _builder.Build();
        var id = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<TaskNotFoundException>(() => service.GetByIdAsync(id));

        Assert.Equal(id, exception.TaskId);
    }

    [Fact]
    public async Task ListAsync_FilterByStatus_ReturnsOnlyMatchingTasks()
    {
        var service = await SeedAsync();

        var tasks = await service.ListAsync(new TaskFilterRequest { Status = TaskItemStatus.Pendente });

        Assert.Equal(2, tasks.Count);
        Assert.All(tasks, t => Assert.Equal(TaskItemStatus.Pendente, t.Status));
    }

    [Fact]
    public async Task ListAsync_FilterByDueDate_ReturnsOnlyMatchingTasks()
    {
        var service = await SeedAsync();

        var tasks = await service.ListAsync(new TaskFilterRequest { DueDate = October1 });

        Assert.Equal(2, tasks.Count);
        Assert.All(tasks, t => Assert.Equal(October1, t.DueDate));
    }

    [Fact]
    public async Task ListAsync_FilterByStatusAndDueDate_ReturnsOnlyTasksMatchingBoth()
    {
        var service = await SeedAsync();

        var tasks = await service.ListAsync(new TaskFilterRequest { Status = TaskItemStatus.Pendente, DueDate = October1 });

        var task = Assert.Single(tasks);
        Assert.Equal("Pendente em 1/10", task.Title);
    }

    [Fact]
    public async Task ListAsync_FilterWithoutMatches_ReturnsEmptyList()
    {
        var service = await SeedAsync();

        var tasks = await service.ListAsync(new TaskFilterRequest { Status = TaskItemStatus.Concluida, DueDate = October2 });

        Assert.Empty(tasks);
    }

    [Fact]
    public async Task ListAsync_WithInvalidStatusFilter_ThrowsValidationException()
    {
        var service = _builder.Build();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.ListAsync(new TaskFilterRequest { Status = (TaskItemStatus)99 }));

        Assert.Contains(exception.Errors, e => e.PropertyName == nameof(TaskFilterRequest.Status));
    }

    [Fact]
    public async Task SearchAsync_ByTitle_ReturnsMatchingTasks()
    {
        var service = await SeedSearchAsync();

        var tasks = await service.SearchAsync("relatório");

        var task = Assert.Single(tasks);
        Assert.Equal("Emitir relatório de entregas", task.Title);
    }

    [Fact]
    public async Task SearchAsync_ByDescription_ReturnsMatchingTasks()
    {
        var service = await SeedSearchAsync();

        var tasks = await service.SearchAsync("fornecedor");

        var task = Assert.Single(tasks);
        Assert.Equal("Agendar coleta", task.Title);
    }

    [Fact]
    public async Task SearchAsync_IsCaseInsensitiveAndMatchesTitleOrDescription()
    {
        var service = await SeedSearchAsync();

        var tasks = await service.SearchAsync("  TRANSPORTADORA ");

        Assert.Equal(2, tasks.Count);
    }

    [Fact]
    public async Task SearchAsync_WithoutMatches_ReturnsEmptyList()
    {
        var service = await SeedSearchAsync();

        var tasks = await service.SearchAsync("inexistente");

        Assert.Empty(tasks);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_WithEmptyTerm_ThrowsValidationException(string? term)
    {
        var service = _builder.Build();

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.SearchAsync(term));

        Assert.Contains(exception.Errors, e => e.PropertyName == "term");
    }

    private async Task<ITaskService> SeedAsync()
    {
        var service = _builder.Build();
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest("Pendente em 1/10", dueDate: October1, status: TaskItemStatus.Pendente));
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest("Pendente em 2/10", dueDate: October2, status: TaskItemStatus.Pendente));
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest("Concluída em 1/10", dueDate: October1, status: TaskItemStatus.Concluida));
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest("Em progresso em 2/10", dueDate: October2, status: TaskItemStatus.EmProgresso));
        return service;
    }

    private async Task<ITaskService> SeedSearchAsync()
    {
        var service = _builder.Build();
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest("Emitir relatório de entregas", "Consolidar entregas do dia para a transportadora"));
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest("Agendar coleta", "Confirmar janela de coleta com o fornecedor"));
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest("Revisar contrato da Transportadora Sul", null));
        return service;
    }
}
