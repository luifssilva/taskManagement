namespace TaskManagement.Application.DTOs;

/// <summary>
/// Task data returned by the API.
/// </summary>
public sealed record TaskResponse
{
    /// <summary>Unique identifier of the task.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; init; }

    /// <summary>Task title.</summary>
    /// <example>Implementar API</example>
    public string Title { get; init; } = string.Empty;

    /// <summary>Task description, if any.</summary>
    /// <example>Criar endpoints de tarefas</example>
    public string? Description { get; init; }

    /// <summary>Due date, if any.</summary>
    /// <example>2026-10-01</example>
    public DateOnly? DueDate { get; init; }

    /// <summary>"Pendente", "Em progresso" or "Concluída".</summary>
    /// <example>Pendente</example>
    public string Status { get; init; } = string.Empty;

    /// <summary>When the task was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the task was last updated (UTC), if ever.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }
}
