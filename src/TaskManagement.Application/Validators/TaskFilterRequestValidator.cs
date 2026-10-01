using FluentValidation;
using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Validators;

public sealed class TaskFilterRequestValidator : AbstractValidator<TaskFilterRequest>
{
    public TaskFilterRequestValidator()
    {
        RuleFor(x => x.Status).OptionalStatus();
    }
}
