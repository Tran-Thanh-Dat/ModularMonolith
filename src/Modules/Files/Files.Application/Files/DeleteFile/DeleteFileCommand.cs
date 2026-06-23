using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using Files.Application.Abstractions;
using FluentValidation;
using MediatR;

namespace Files.Application.Files.DeleteFile;

public sealed record DeleteFileCommand(Guid Id) : ICommand;

public sealed class DeleteFileCommandValidator : AbstractValidator<DeleteFileCommand>
{
    public DeleteFileCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, Result>
{
    private readonly IFileService _fileService;

    public DeleteFileCommandHandler(IFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task<Result> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        await _fileService.DeleteAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
