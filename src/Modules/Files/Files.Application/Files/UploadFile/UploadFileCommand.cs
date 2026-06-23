using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using Files.Application.Abstractions;
using Files.Application.Files;
using Files.Application.Options;
using Files.Application.Validation;
using BuildingBlocks.Application.Errors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Files.Application.Files.UploadFile;

public sealed record UploadFileCommand(
    Stream FileStream,
    string OriginalFileName,
    string ContentType,
    long SizeInBytes,
    string? ModuleName,
    string? ReferenceType,
    string? ReferenceId,
    string? Description,
    bool IsTemporary) : ICommand<UploadFileResponse>;

public sealed class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator(IOptions<FileStorageOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;
        var maxBytes = options.MaxFileSizeMb * 1024L * 1024L;
        var allowedExtensions = options.AllowedExtensions
            .Select(FileValidationRules.NormalizeExtension)
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowedContentTypes = options.AllowedContentTypes
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        RuleFor(command => command.FileStream)
            .NotNull()
            .WithMessage("File is required.");

        RuleFor(command => command.SizeInBytes)
            .GreaterThan(0)
            .WithErrorCode(FileErrors.Empty);

        RuleFor(command => command.SizeInBytes)
            .LessThanOrEqualTo(maxBytes)
            .WithErrorCode(FileErrors.TooLarge);

        RuleFor(command => command.OriginalFileName)
            .NotEmpty()
            .WithErrorCode(FileErrors.InvalidFileName)
            .Must(fileName => !FileValidationRules.HasUnsafeFileNameCharacters(fileName))
            .WithErrorCode(FileErrors.InvalidFileName);

        RuleFor(command => command.OriginalFileName)
            .Must(fileName =>
            {
                var extension = FileValidationRules.GetExtensionFromFileName(fileName);
                return allowedExtensions.Contains(extension);
            })
            .WithErrorCode(FileErrors.ExtensionNotAllowed);

        RuleFor(command => command.ContentType)
            .NotEmpty()
            .Must(contentType => allowedContentTypes.Contains(contentType))
            .WithErrorCode(FileErrors.ContentTypeNotAllowed);

        RuleFor(command => command.ModuleName)
            .MaximumLength(100);

        RuleFor(command => command.ReferenceType)
            .MaximumLength(100);

        RuleFor(command => command.ReferenceId)
            .MaximumLength(100);

        RuleFor(command => command.Description)
            .MaximumLength(1000);
    }
}

public sealed class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result<UploadFileResponse>>
{
    private readonly IFileService _fileService;

    public UploadFileCommandHandler(IFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task<Result<UploadFileResponse>> Handle(
        UploadFileCommand request,
        CancellationToken cancellationToken)
    {
        var response = await _fileService.UploadAsync(
            request.FileStream,
            request.OriginalFileName,
            request.ContentType,
            request.SizeInBytes,
            request.ModuleName,
            request.ReferenceType,
            request.ReferenceId,
            request.Description,
            request.IsTemporary,
            cancellationToken);

        return Result<UploadFileResponse>.Success(response);
    }
}
