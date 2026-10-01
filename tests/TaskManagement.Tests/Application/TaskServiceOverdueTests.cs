using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Enums;
using TaskManagement.Tests.Support;

namespace TaskManagement.Tests.Application;

/// <summary>
/// "Today" is 2026-09-29 (see <see cref="TaskServiceBuilder.Now"/>).
/// </summary>
public class TaskServiceOverdueTests
{
    private static readonly DateOnly Yesterday = new(2026, 9, 28);
    private static readonly DateOnly Today = new(2026, 9, 29);
    private static readonly DateOnly Tomorrow = new(2026, 9, 30);

    private readonly TaskServiceBuilder _builder = new();

    [Theory]
    [InlineData(TaskItemStatus.Pendente, true)]
    [InlineData(TaskItemStatus.EmProgresso, true)]
    [InlineData(TaskItemStatus.Concluida, false)]
    public async Task Response_PastDueDate_IsOverdueUnlessCompleted(TaskItemStatus status, bool expected)
    {
        var service = _builder.Build();

        var task = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest(dueDate: Yesterday, status: status));

        Assert.Equal(expected, task.IsOverdue);
    }

    [Fact]
    public async Task Response_DueToday_IsNotOverdue()
    {
        var service = _builder.Build();

        var task = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest(dueDate: Today));

        Assert.False(task.IsOverdue);
    }

    [Fact]
    public async Task Response_WithoutDueDate_IsNotOverdue()
    {
        var service = _builder.Build();

        var task = await service.CreateAsync(new CreateTaskRequest { Title = "Sem prazo", Status = TaskItemStatus.Pendente });

        Assert.False(task.IsOverdue);
    }

    [Fact]
    public async Task Response_BecomesOverdueWhenTheDayPasses()
    {
        var service = _builder.Build();
        var created = await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest(dueDate: Today));

        _builder.Clock.Now = TaskServiceBuilder.Now.AddDays(1);

        Assert.True((await service.GetByIdAsync(created.Id)).IsOverdue);
    }

    [Fact]
    public async Task ListAsync_OverdueTrue_ReturnsOnlyOverdueTasks()
    {
        var service = await SeedAsync();

        var tasks = await service.ListAsync(new TaskFilterRequest { Overdue = true });

        Assert.Equal(["Atrasada em progresso", "Atrasada pendente"], tasks.Select(t => t.Title).Order());
        Assert.All(tasks, t => Assert.True(t.IsOverdue));
    }

    [Fact]
    public async Task ListAsync_OverdueFalse_ReturnsOnlyTasksNotOverdue()
    {
        var service = await SeedAsync();

        var tasks = await service.ListAsync(new TaskFilterRequest { Overdue = false });

        Assert.Equal(["Atrasada concluída", "Sem prazo", "Vence amanhã", "Vence hoje"], tasks.Select(t => t.Title).Order());
        Assert.All(tasks, t => Assert.False(t.IsOverdue));
    }

    [Fact]
    public async Task ListAsync_OverdueCombinedWithStatus_AppliesBothFilters()
    {
        var service = await SeedAsync();

        var tasks = await service.ListAsync(new TaskFilterRequest { Overdue = true, Status = TaskItemStatus.Pendente });

        Assert.Equal("Atrasada pendente", Assert.Single(tasks).Title);
    }

    private async Task<ITaskService> SeedAsync()
    {
        var service = _builder.Build();
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest("Atrasada pendente", dueDate: Yesterday, status: TaskItemStatus.Pendente));
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest("Atrasada em progresso", dueDate: Yesterday, status: TaskItemStatus.EmProgresso));
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest("Atrasada concluída", dueDate: Yesterday, status: TaskItemStatus.Concluida));
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest("Vence hoje", dueDate: Today));
        await service.CreateAsync(TaskServiceBuilder.ValidCreateRequest("Vence amanhã", dueDate: Tomorrow));
        await service.CreateAsync(new CreateTaskRequest { Title = "Sem prazo", Status = TaskItemStatus.Pendente });
        return service;
    }
}
