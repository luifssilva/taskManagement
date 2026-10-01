using TaskManagement.Domain.Enums;

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
    /// <example>Conferir carga do pedido 4521</example>
    public string Title { get; init; } = string.Empty;

    /// <summary>Task description, if any.</summary>
    /// <example>Validar volumes e lacres antes da expedição</example>
    public string? Description { get; init; }

    /// <summary>Due date, if any.</summary>
    /// <example>2026-10-01</example>
    public DateOnly? DueDate { get; init; }

    /// <summary>Pendente, EmProgresso or Concluida.</summary>
    /// <example>Pendente</example>
    public TaskItemStatus Status { get; init; }

    /// <summary>Whether the due date has passed and the task is not Concluida.</summary>
    /// <example>false</example>
    public bool IsOverdue { get; init; }

    /// <summary>When the task was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the task was last updated (UTC), if ever.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }
}
