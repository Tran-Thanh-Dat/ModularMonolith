using AsyncTasks.Domain.Constants;
using FluentValidation;

namespace AsyncTasks.Application.Validators;

public sealed class SubmitEmailDemoTaskCommandValidator : AbstractValidator<Commands.SubmitEmailDemoTaskCommand>
{
    public SubmitEmailDemoTaskCommandValidator()
    {
        RuleFor(c => c.EmailTo).MaximumLength(255);
        RuleFor(c => c.Subject).MaximumLength(255);
        RuleFor(c => c.Body).MaximumLength(2000);
    }
}

public sealed class SubmitFileProcessingDemoTaskCommandValidator : AbstractValidator<Commands.SubmitFileProcessingDemoTaskCommand>
{
    public SubmitFileProcessingDemoTaskCommandValidator()
    {
        RuleFor(c => c.FileName).MaximumLength(255);
    }
}

public sealed class SubmitFailDemoTaskCommandValidator : AbstractValidator<Commands.SubmitFailDemoTaskCommand>
{
    public SubmitFailDemoTaskCommandValidator()
    {
        RuleFor(c => c.FailReason).MaximumLength(1000);
    }
}

public sealed class SubmitLongRunningDemoTaskCommandValidator : AbstractValidator<Commands.SubmitLongRunningDemoTaskCommand>
{
    public SubmitLongRunningDemoTaskCommandValidator()
    {
        RuleFor(c => c.DurationSeconds).InclusiveBetween(1, 120);
        RuleFor(c => c.Steps).InclusiveBetween(1, 100);
    }
}
