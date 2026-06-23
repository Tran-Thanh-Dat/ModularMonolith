using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using Files.Application.Abstractions;
using Files.Application.Files;
using FluentValidation;
using MediatR;

namespace Files.Application.Files.DownloadFile;

public sealed record DownloadFileQuery(Guid Id) : IQuery<FileDownloadResult>;

public sealed class DownloadFileQueryValidator : AbstractValidator<DownloadFileQuery>
{
    public DownloadFileQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}

public sealed class DownloadFileQueryHandler : IRequestHandler<DownloadFileQuery, Result<FileDownloadResult>>
{
    private readonly IFileService _fileService;

    public DownloadFileQueryHandler(IFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task<Result<FileDownloadResult>> Handle(
        DownloadFileQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _fileService.DownloadAsync(request.Id, cancellationToken);
        return Result<FileDownloadResult>.Success(result);
    }
}
