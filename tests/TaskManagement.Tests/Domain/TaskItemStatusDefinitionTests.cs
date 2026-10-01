using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Tests.Domain;

public class TaskItemStatusDefinitionTests
{
    [Fact]
    public void All_HasExactlyOneEntryPerEnumValue()
    {
        Assert.Equal(Enum.GetValues<TaskItemStatus>(), TaskItemStatusDefinition.All.Select(s => s.Id));
    }

    [Fact]
    public void All_HasDescriptions()
    {
        Assert.Equal(
            ["Pendente", "Em progresso", "Concluída"],
            TaskItemStatusDefinition.All.Select(s => s.Description));
    }
}
