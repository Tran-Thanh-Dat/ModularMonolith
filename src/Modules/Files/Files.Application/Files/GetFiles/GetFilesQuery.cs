using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using Files.Application.Abstractions;
using Files.Application.Files;
using FluentValidation;
using MediatR;

namespace Files.Application.Files.GetFiles;

public sealed record GetFilesQuery(
    string? Keyword,
    string? ModuleName,
    string? ReferenceType,
    string? ReferenceId,
    string? ContentType,
    string? FileExtension,
    bool? IsTemporary,
    bool? IsActive,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<FileResourceListItemResponse>>;

public sealed class GetFilesQueryValidator : AbstractValidator<GetFilesQuery>
{
    public GetFilesQueryValidator()
    {
        RuleFor(query => query.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetFilesQueryHandler : IRequestHandler<GetFilesQuery, Result<PagedResult<FileResourceListItemResponse>>>
{
    private readonly IFileService _fileService;

    public GetFilesQueryHandler(IFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task<Result<PagedResult<FileResourceListItemResponse>>> Handle(
        GetFilesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _fileService.GetListAsync(
            request.Keyword,
            request.ModuleName,
            request.ReferenceType,
            request.ReferenceId,
            request.ContentType,
            request.FileExtension,
            request.IsTemporary,
            request.IsActive,
            request.FromDate,
            request.ToDate,
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        return Result<PagedResult<FileResourceListItemResponse>>.Success(result);
    }
}
