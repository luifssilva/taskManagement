using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Queries;

/// <summary>
/// Criteria for listing tasks. Every non-null filter must match (AND).
/// </summary>
/// <param name="Status">Only tasks with this status.</param>
/// <param name="DueDate">Only tasks due on this exact date.</param>
/// <param name="Overdue"><c>true</c>: only overdue tasks; <c>false</c>: only tasks that are not overdue.</param>
/// <param name="Today">Reference date used to evaluate <paramref name="Overdue"/>.</param>
public sealed record TaskListCriteria(
    TaskItemStatus? Status,
    DateOnly? DueDate,
    bool? Overdue,
    DateOnly Today);
