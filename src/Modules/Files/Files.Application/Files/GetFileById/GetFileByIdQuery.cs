using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using Files.Application.Abstractions;
using Files.Application.Files;
using FluentValidation;
using MediatR;

namespace Files.Application.Files.GetFileById;

public sealed record GetFileByIdQuery(Guid Id) : IQuery<FileResourceDetailResponse>;

public sealed class GetFileByIdQueryValidator : AbstractValidator<GetFileByIdQuery>
{
    public GetFileByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}

public sealed class GetFileByIdQueryHandler : IRequestHandler<GetFileByIdQuery, Result<FileResourceDetailResponse>>
{
    private readonly IFileService _fileService;

    public GetFileByIdQueryHandler(IFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task<Result<FileResourceDetailResponse>> Handle(
        GetFileByIdQuery request,
        CancellationToken cancellationToken)
    {
        var file = await _fileService.GetByIdAsync(request.Id, cancellationToken);

        if (file is null)
        {
            throw new NotFoundException(
                FileErrors.NotFound,
                $"File with id '{request.Id}' was not found.");
        }

        return Result<FileResourceDetailResponse>.Success(file);
    }
}
