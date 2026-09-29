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
        "Status must be one of: " +
        string.Join(", ", TaskItemStatusExtensions.AllowedDisplayNames.Select(s => $"'{s}'")) + ".";

    public static IRuleBuilderOptions<T, string?> ValidTitle<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithMessage("Title is required.")
            .Must(title => title is null || title.Trim().Length <= TaskItem.TitleMaxLength)
            .WithMessage($"Title must have at most {TaskItem.TitleMaxLength} characters.");

    public static IRuleBuilderOptions<T, string?> ValidDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(description => description is null || description.Trim().Length <= TaskItem.DescriptionMaxLength)
            .WithMessage($"Description must have at most {TaskItem.DescriptionMaxLength} characters.");

    public static IRuleBuilderOptions<T, string?> RequiredStatus<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(status => !string.IsNullOrWhiteSpace(status)).WithMessage("Status is required.")
            .Must(status => string.IsNullOrWhiteSpace(status) || TaskItemStatusExtensions.TryParse(status, out _))
            .WithMessage(AllowedStatusesMessage);

    public static IRuleBuilderOptions<T, string?> OptionalStatus<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(status => status is null || TaskItemStatusExtensions.TryParse(status, out _))
            .WithMessage(AllowedStatusesMessage);
}
