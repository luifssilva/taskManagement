using FluentValidation;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Validators;

/// <summary>
/// Rules shared by the task validators.
/// </summary>
internal static class TaskValidationRules
{
    public static string AllowedStatusesMessage { get; } =
        "Status must be one of: " + string.Join(", ", Enum.GetNames<TaskItemStatus>()) + ".";

    public static IRuleBuilderOptions<T, string?> ValidTitle<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithMessage("Title is required.")
            .Must(title => title is null || title.Trim().Length <= TaskItem.TitleMaxLength)
            .WithMessage($"Title must have at most {TaskItem.TitleMaxLength} characters.");

    public static IRuleBuilderOptions<T, string?> ValidDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(description => description is null || description.Trim().Length <= TaskItem.DescriptionMaxLength)
            .WithMessage($"Description must have at most {TaskItem.DescriptionMaxLength} characters.");

    public static IRuleBuilderOptions<T, TaskItemStatus?> RequiredStatus<T>(this IRuleBuilder<T, TaskItemStatus?> rule) =>
        rule
            .NotNull().WithMessage("Status is required.")
            .IsInEnum().WithMessage(AllowedStatusesMessage);

    // Numeric JSON values outside the enum (e.g. 99) deserialize fine, so they are rejected here.
    public static IRuleBuilderOptions<T, TaskItemStatus?> OptionalStatus<T>(this IRuleBuilder<T, TaskItemStatus?> rule) =>
        rule.IsInEnum().WithMessage(AllowedStatusesMessage);
}
