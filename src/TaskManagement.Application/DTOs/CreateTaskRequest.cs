namespace TaskManagement.Application.DTOs;

/// <summary>
/// Data required to create a task.
/// </summary>
public sealed record CreateTaskRequest
{
    /// <summary>Task title (required, up to 200 characters).</summary>
    /// <example>Implementar API</example>
    public string? Title { get; init; }

    /// <summary>Optional description (up to 2000 characters).</summary>
    /// <example>Criar endpoints de tarefas</example>
    public string? Description { get; init; }

    /// <summary>Optional due date, in the format yyyy-MM-dd.</summary>
    /// <example>2026-10-01</example>
    public DateOnly? DueDate { get; init; }

    /// <summary>Task status: "Pendente", "Em progresso" or "Concluída".</summary>
    /// <example>Pendente</example>
    public string? Status { get; init; }
}
