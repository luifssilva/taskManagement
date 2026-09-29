using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Tests.Domain;

public class TaskItemOverdueAndHistoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 29);

    [Theory]
    [InlineData(-1, TaskItemStatus.Pendente, true)]
    [InlineData(-1, TaskItemStatus.EmProgresso, true)]
    [InlineData(-1, TaskItemStatus.Concluida, false)]
    [InlineData(0, TaskItemStatus.Pendente, false)]
    [InlineData(1, TaskItemStatus.Pendente, false)]
    public void IsOverdue_AndOverdueAsOf_Agree(int daysFromToday, TaskItemStatus status, bool expected)
    {
        var task = TaskItem.Create("Tarefa", null, Today.AddDays(daysFromToday), status, Now);

        Assert.Equal(expected, task.IsOverdue(Today));
        Assert.Equal(expected, TaskItem.OverdueAsOf(Today).Compile()(task));
    }

    [Fact]
    public void IsOverdue_WithoutDueDate_IsFalse()
    {
        var task = TaskItem.Create("Tarefa", null, null, TaskItemStatus.Pendente, Now);

        Assert.False(task.IsOverdue(Today));
        Assert.False(TaskItem.OverdueAsOf(Today).Compile()(task));
    }

    [Fact]
    public void Create_StartsHistoryAndVersion()
    {
        var task = TaskItem.Create("Tarefa", null, null, TaskItemStatus.EmProgresso, Now);

        var entry = Assert.Single(task.StatusHistory);
        Assert.Null(entry.FromStatus);
        Assert.Equal(TaskItemStatus.EmProgresso, entry.ToStatus);
        Assert.Equal(Now, entry.ChangedAt);
        Assert.Equal(1, task.Version);
    }

    [Fact]
    public void Update_WithNewStatus_AppendsHistoryEntry()
    {
        var task = TaskItem.Create("Tarefa", null, null, TaskItemStatus.Pendente, Now);

        task.Update("Tarefa", null, null, TaskItemStatus.Concluida, Now.AddHours(1));

        var last = task.StatusHistory.Last();
        Assert.Equal(TaskItemStatus.Pendente, last.FromStatus);
        Assert.Equal(TaskItemStatus.Concluida, last.ToStatus);
        Assert.Equal(Now.AddHours(1), last.ChangedAt);
        Assert.Equal(2, task.Version);
    }

    [Fact]
    public void Update_WithSameStatus_KeepsHistoryButBumpsVersion()
    {
        var task = TaskItem.Create("Tarefa", null, null, TaskItemStatus.Pendente, Now);

        task.Update("Tarefa renomeada", null, null, TaskItemStatus.Pendente, Now.AddHours(1));

        Assert.Single(task.StatusHistory);
        Assert.Equal(2, task.Version);
    }
}
