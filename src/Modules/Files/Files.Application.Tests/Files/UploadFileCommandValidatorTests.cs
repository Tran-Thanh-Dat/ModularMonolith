using BuildingBlocks.Application.Errors;
using Files.Application.Files.UploadFile;
using Files.Application.Options;
using FluentValidation.Results;
using Xunit;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Files.Application.Tests.Files;

public sealed class UploadFileCommandValidatorTests
{
    private readonly UploadFileCommandValidator _validator = new(OptionsFactory.Create(CreateOptions()));

    [Fact]
    public void Validate_WhenExtensionNotAllowed_ReturnsExtensionNotAllowed()
    {
        var command = CreateCommand(originalFileName: "report.exe", contentType: "application/pdf");

        var result = _validator.Validate(command);

        AssertValidationError(result, nameof(UploadFileCommand.OriginalFileName), FileErrors.ExtensionNotAllowed);
    }

    [Fact]
    public void Validate_WhenContentTypeNotAllowed_ReturnsContentTypeNotAllowed()
    {
        var command = CreateCommand(originalFileName: "report.pdf", contentType: "application/octet-stream");

        var result = _validator.Validate(command);

        AssertValidationError(result, nameof(UploadFileCommand.ContentType), FileErrors.ContentTypeNotAllowed);
    }

    [Fact]
    public void Validate_WhenFileEmpty_ReturnsEmptyError()
    {
        var command = CreateCommand(sizeInBytes: 0);

        var result = _validator.Validate(command);

        AssertValidationError(result, nameof(UploadFileCommand.SizeInBytes), FileErrors.Empty);
    }

    [Fact]
    public void Validate_WhenFileTooLarge_ReturnsTooLargeError()
    {
        var command = CreateCommand(sizeInBytes: 30L * 1024 * 1024);

        var result = _validator.Validate(command);

        AssertValidationError(result, nameof(UploadFileCommand.SizeInBytes), FileErrors.TooLarge);
    }

    [Fact]
    public void Validate_WhenFileNameUnsafe_ReturnsInvalidFileName()
    {
        var command = CreateCommand(originalFileName: "../secret.pdf");

        var result = _validator.Validate(command);

        AssertValidationError(result, nameof(UploadFileCommand.OriginalFileName), FileErrors.InvalidFileName);
    }

    private static void AssertValidationError(ValidationResult result, string propertyName, string expectedErrorCode)
    {
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors, e => e.PropertyName == propertyName);
        Assert.Equal(expectedErrorCode, error.ErrorCode);
    }

    private static UploadFileCommand CreateCommand(
        string originalFileName = "document.pdf",
        string contentType = "application/pdf",
        long sizeInBytes = 1024) =>
        new(
            new MemoryStream(new byte[] { 1, 2, 3 }),
            originalFileName,
            contentType,
            sizeInBytes,
            null,
            null,
            null,
            null,
            false);

    private static FileStorageOptions CreateOptions() =>
        new()
        {
            MaxFileSizeMb = 20,
            AllowedExtensions = [".pdf", ".jpg"],
            AllowedContentTypes = ["application/pdf", "image/jpeg"]
        };
}
