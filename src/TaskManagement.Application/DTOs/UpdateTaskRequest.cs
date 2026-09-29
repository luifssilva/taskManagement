namespace TaskManagement.Application.DTOs;

/// <summary>
/// Full replacement of a task's editable data (PUT semantics: omitted optional fields are cleared).
/// </summary>
public sealed record UpdateTaskRequest
{
    /// <summary>Task title (required, up to 200 characters).</summary>
    /// <example>Implementar API</example>
    public string? Title { get; init; }

    /// <summary>Optional description (up to 2000 characters).</summary>
    /// <example>Criar endpoints de tarefas e documentação</example>
    public string? Description { get; init; }

    /// <summary>Optional due date, in the format yyyy-MM-dd.</summary>
    /// <example>2026-10-15</example>
    public DateOnly? DueDate { get; init; }

    /// <summary>Task status: "Pendente", "Em progresso" or "Concluída".</summary>
    /// <example>Em progresso</example>
    public string? Status { get; init; }
}
