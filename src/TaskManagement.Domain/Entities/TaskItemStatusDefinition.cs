using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

/// <summary>
/// Row of the status domain table: one entry per <see cref="TaskItemStatus"/> value,
/// with a human-readable description.
/// </summary>
public sealed class TaskItemStatusDefinition
{
    public const int DescriptionMaxLength = 50;

    /// <summary>The domain table contents, used to seed the database.</summary>
    public static IReadOnlyList<TaskItemStatusDefinition> All { get; } =
    [
        new(TaskItemStatus.Pendente, "Pendente"),
        new(TaskItemStatus.EmProgresso, "Em progresso"),
        new(TaskItemStatus.Concluida, "Concluída")
    ];

    private TaskItemStatusDefinition(TaskItemStatus id, string description)
    {
        Id = id;
        Description = description;
    }

    public TaskItemStatus Id { get; private set; }

    public string Description { get; private set; }
}
