using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.Tests.Domain;

public class TaskItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_SetsStateAndGeneratesId()
    {
        var task = TaskItem.Create(" Título ", " Descrição ", new DateOnly(2026, 10, 1), TaskItemStatus.Pendente, Now);

        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal("Título", task.Title);
        Assert.Equal("Descrição", task.Description);
        Assert.Equal(new DateOnly(2026, 10, 1), task.DueDate);
        Assert.Equal(TaskItemStatus.Pendente, task.Status);
        Assert.Equal(Now, task.CreatedAt);
        Assert.Null(task.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithoutTitle_ThrowsDomainException(string title)
    {
        Assert.Throws<DomainException>(() => TaskItem.Create(title, null, null, TaskItemStatus.Pendente, Now));
    }

    [Fact]
    public void Create_WithTitleTooLong_ThrowsDomainException()
    {
        var title = new string('a', TaskItem.TitleMaxLength + 1);

        Assert.Throws<DomainException>(() => TaskItem.Create(title, null, null, TaskItemStatus.Pendente, Now));
    }

    [Fact]
    public void Create_WithUndefinedStatus_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => TaskItem.Create("Título", null, null, (TaskItemStatus)99, Now));
    }

    [Fact]
    public void Update_ChangesStateAndSetsUpdatedAt()
    {
        var task = TaskItem.Create("Título", "Descrição", null, TaskItemStatus.Pendente, Now);
        var later = Now.AddDays(1);

        task.Update("Novo", null, new DateOnly(2026, 11, 1), TaskItemStatus.Concluida, later);

        Assert.Equal("Novo", task.Title);
        Assert.Null(task.Description);
        Assert.Equal(new DateOnly(2026, 11, 1), task.DueDate);
        Assert.Equal(TaskItemStatus.Concluida, task.Status);
        Assert.Equal(Now, task.CreatedAt);
        Assert.Equal(later, task.UpdatedAt);
    }

    [Fact]
    public void Update_WithInvalidData_KeepsPreviousState()
    {
        var task = TaskItem.Create("Título", "Descrição", null, TaskItemStatus.Pendente, Now);

        Assert.Throws<DomainException>(() => task.Update(" ", null, null, TaskItemStatus.Concluida, Now));

        Assert.Equal("Título", task.Title);
        Assert.Equal(TaskItemStatus.Pendente, task.Status);
        Assert.Null(task.UpdatedAt);
    }
}
