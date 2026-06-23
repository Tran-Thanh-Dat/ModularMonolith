using FluentValidation;

namespace ApiHost.Diagnostics;

public sealed class TestPipelineCommandValidator : AbstractValidator<TestPipelineCommand>
{
    public TestPipelineCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("Name is required.");
    }
}
