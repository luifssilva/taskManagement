namespace TaskManagement.Application.DTOs;

/// <summary>
/// Optional filters for listing tasks. Filters are combined with AND.
/// </summary>
public sealed record TaskFilterRequest
{
    /// <summary>Only tasks with this status ("Pendente", "Em progresso" or "Concluída").</summary>
    /// <example>Pendente</example>
    public string? Status { get; init; }

    /// <summary>Only tasks due on this date (yyyy-MM-dd).</summary>
    /// <example>2026-10-01</example>
    public DateOnly? DueDate { get; init; }
}
