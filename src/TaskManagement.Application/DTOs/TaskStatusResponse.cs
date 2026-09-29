using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs;

/// <summary>
/// One row of the status domain table.
/// </summary>
public sealed record TaskStatusResponse
{
    /// <summary>Numeric identifier of the status.</summary>
    /// <example>2</example>
    public int Id { get; init; }

    /// <summary>Value used in requests and responses.</summary>
    /// <example>EmProgresso</example>
    public TaskItemStatus Name { get; init; }

    /// <summary>Human-readable description.</summary>
    /// <example>Em progresso</example>
    public string Description { get; init; } = string.Empty;
}
