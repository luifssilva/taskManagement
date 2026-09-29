namespace TaskManagement.Domain.Enums;

/// <summary>
/// Possible states of a task during its life cycle.
/// </summary>
/// <remarks>
/// The numeric values are the identifiers of the status domain table
/// (<see cref="Entities.TaskItemStatusDefinition"/>) and must not change.
/// </remarks>
public enum TaskItemStatus
{
    Pendente = 1,
    EmProgresso = 2,
    Concluida = 3
}
