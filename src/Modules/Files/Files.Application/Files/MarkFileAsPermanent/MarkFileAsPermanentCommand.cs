using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using Files.Application.Abstractions;
using FluentValidation;
using MediatR;

namespace Files.Application.Files.MarkFileAsPermanent;

public sealed record MarkFileAsPermanentCommand(Guid Id) : ICommand;

public sealed class MarkFileAsPermanentCommandValidator : AbstractValidator<MarkFileAsPermanentCommand>
{
    public MarkFileAsPermanentCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class MarkFileAsPermanentCommandHandler : IRequestHandler<MarkFileAsPermanentCommand, Result>
{
    private readonly IFileService _fileService;

    public MarkFileAsPermanentCommandHandler(IFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task<Result> Handle(MarkFileAsPermanentCommand request, CancellationToken cancellationToken)
    {
        await _fileService.MarkAsPermanentAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
