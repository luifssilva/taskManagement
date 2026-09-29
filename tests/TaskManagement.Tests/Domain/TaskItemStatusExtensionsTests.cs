using TaskManagement.Domain.Enums;

namespace TaskManagement.Tests.Domain;

public class TaskItemStatusExtensionsTests
{
    [Theory]
    [InlineData(TaskItemStatus.Pendente, "Pendente")]
    [InlineData(TaskItemStatus.EmProgresso, "Em progresso")]
    [InlineData(TaskItemStatus.Concluida, "Concluída")]
    public void ToDisplayName_ReturnsPublicName(TaskItemStatus status, string expected)
    {
        Assert.Equal(expected, status.ToDisplayName());
    }

    [Theory]
    [InlineData("Pendente", TaskItemStatus.Pendente)]
    [InlineData("Em progresso", TaskItemStatus.EmProgresso)]
    [InlineData("em  progresso", TaskItemStatus.EmProgresso)]
    [InlineData("Concluída", TaskItemStatus.Concluida)]
    [InlineData("CONCLUIDA", TaskItemStatus.Concluida)]
    public void TryParse_AcceptsKnownNames(string value, TaskItemStatus expected)
    {
        Assert.True(TaskItemStatusExtensions.TryParse(value, out var status));
        Assert.Equal(expected, status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("Cancelada")]
    public void TryParse_RejectsUnknownValues(string? value)
    {
        Assert.False(TaskItemStatusExtensions.TryParse(value, out _));
    }

    [Fact]
    public void AllowedDisplayNames_ListsEveryStatus()
    {
        Assert.Equal(["Pendente", "Em progresso", "Concluída"], TaskItemStatusExtensions.AllowedDisplayNames);
    }
}
