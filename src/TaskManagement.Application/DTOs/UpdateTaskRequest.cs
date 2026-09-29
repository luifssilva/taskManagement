using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs;

/// <summary>
/// Full replacement of a task's editable data (PUT semantics: omitted optional fields are cleared).
/// </summary>
public sealed record UpdateTaskRequest
{
    /// <summary>Task title (required, up to 200 characters).</summary>
    /// <example>Conferir carga do pedido 4521</example>
    public string? Title { get; init; }

    /// <summary>Optional description (up to 2000 characters).</summary>
    /// <example>Volumes conferidos; aguardando coleta da transportadora</example>
    public string? Description { get; init; }

    /// <summary>Optional due date, in the format yyyy-MM-dd.</summary>
    /// <example>2026-10-02</example>
    public DateOnly? DueDate { get; init; }

    /// <summary>Task status: Pendente, EmProgresso or Concluida (the numeric value 1, 2 or 3 is also accepted).</summary>
    /// <example>EmProgresso</example>
    public TaskItemStatus? Status { get; init; }
}
