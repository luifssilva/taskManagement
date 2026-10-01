using System.Linq.Expressions;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.Domain.Entities;

/// <summary>
/// A task to be done. Named <c>TaskItem</c> to avoid clashing with <see cref="System.Threading.Tasks.Task"/>.
/// </summary>
/// <remarks>
/// The entity protects its own invariants: state can only change through <see cref="Create"/>
/// and <see cref="Update"/>, which reject invalid titles, descriptions and statuses and record every
/// status change in <see cref="StatusHistory"/>.
/// </remarks>
public class TaskItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private readonly List<TaskStatusChange> _statusHistory = [];

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Every status the task went through, starting with the initial one.</summary>
    public IReadOnlyCollection<TaskStatusChange> StatusHistory => _statusHistory.AsReadOnly();

    // Required by EF Core.
    private TaskItem()
    {
    }

    /// <summary>
    /// A task is overdue when its due date is before <paramref name="today"/> and it is not completed.
    /// A task due today is not overdue yet.
    /// </summary>
    public static Expression<Func<TaskItem, bool>> OverdueAsOf(DateOnly today) =>
        task => task.Status != TaskItemStatus.Concluida && task.DueDate != null && task.DueDate < today;

    public static TaskItem Create(
        string title,
        string? description,
        DateOnly? dueDate,
        TaskItemStatus status,
        DateTimeOffset createdAt)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            CreatedAt = createdAt
        };

        task.Apply(title, description, dueDate, status);
        task._statusHistory.Add(new TaskStatusChange(null, status, createdAt));
        return task;
    }

    public void Update(
        string title,
        string? description,
        DateOnly? dueDate,
        TaskItemStatus status,
        DateTimeOffset updatedAt)
    {
        var previousStatus = Status;

        Apply(title, description, dueDate, status);

        if (previousStatus != status)
        {
            _statusHistory.Add(new TaskStatusChange(previousStatus, status, updatedAt));
        }

        UpdatedAt = updatedAt;
    }

    /// <inheritdoc cref="OverdueAsOf"/>
    public bool IsOverdue(DateOnly today) =>
        Status != TaskItemStatus.Concluida && DueDate.HasValue && DueDate.Value < today;

    private void Apply(string title, string? description, DateOnly? dueDate, TaskItemStatus status)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Title is required.");
        }

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > TitleMaxLength)
        {
            throw new DomainException($"Title must have at most {TitleMaxLength} characters.");
        }

        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (trimmedDescription?.Length > DescriptionMaxLength)
        {
            throw new DomainException($"Description must have at most {DescriptionMaxLength} characters.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainException($"Status '{status}' is not valid.");
        }

        Title = trimmedTitle;
        Description = trimmedDescription;
        DueDate = dueDate;
        Status = status;
    }
}
