using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace Files.Domain.FileResources;

public sealed class FileResource : SoftDeletableEntity
{
    private FileResource()
    {
    }

    private FileResource(
        Guid id,
        string originalFileName,
        string storedFileName,
        string fileExtension,
        string contentType,
        long sizeInBytes,
        string storageProvider,
        string storagePath,
        string? publicUrl,
        string checksum,
        string? moduleName,
        string? referenceType,
        string? referenceId,
        string? description,
        bool isTemporary,
        DateTimeOffset? expiresAt,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        OriginalFileName = originalFileName;
        StoredFileName = storedFileName;
        FileExtension = fileExtension;
        ContentType = contentType;
        SizeInBytes = sizeInBytes;
        StorageProvider = storageProvider;
        StoragePath = storagePath;
        PublicUrl = publicUrl;
        Checksum = checksum;
        ModuleName = moduleName;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Description = description;
        IsTemporary = isTemporary;
        ExpiresAt = expiresAt;
        IsActive = true;
        SetCreated(createdBy, createdAt);
    }

    public string OriginalFileName { get; private set; } = default!;

    public string StoredFileName { get; private set; } = default!;

    public string FileExtension { get; private set; } = default!;

    public string ContentType { get; private set; } = default!;

    public long SizeInBytes { get; private set; }

    public string StorageProvider { get; private set; } = default!;

    public string StoragePath { get; private set; } = default!;

    public string? PublicUrl { get; private set; }

    public string Checksum { get; private set; } = default!;

    public string? ModuleName { get; private set; }

    public string? ReferenceType { get; private set; }

    public string? ReferenceId { get; private set; }

    public string? Description { get; private set; }

    public bool IsTemporary { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public bool IsActive { get; private set; }

    public static FileResource Create(
        string originalFileName,
        string storedFileName,
        string fileExtension,
        string contentType,
        long sizeInBytes,
        string storageProvider,
        string storagePath,
        string? publicUrl,
        string checksum,
        string? moduleName,
        string? referenceType,
        string? referenceId,
        string? description,
        bool isTemporary,
        DateTimeOffset? expiresAt,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new DomainException("Original file name is required.", "File.InvalidFileName");
        }

        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            throw new DomainException("Stored file name is required.", "File.InvalidFileName");
        }

        if (sizeInBytes <= 0)
        {
            throw new DomainException("File size must be greater than zero.", "File.Empty");
        }

        if (string.IsNullOrWhiteSpace(storageProvider))
        {
            throw new DomainException("Storage provider is required.", "File.StorageProviderNotFound");
        }

        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new DomainException("Storage path is required.", "File.UploadFailed");
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new DomainException("Content type is required.", "File.ContentTypeNotAllowed");
        }

        return new FileResource(
            Guid.NewGuid(),
            originalFileName.Trim(),
            storedFileName,
            fileExtension,
            contentType,
            sizeInBytes,
            storageProvider,
            storagePath,
            publicUrl,
            checksum,
            string.IsNullOrWhiteSpace(moduleName) ? null : moduleName.Trim(),
            string.IsNullOrWhiteSpace(referenceType) ? null : referenceType.Trim(),
            string.IsNullOrWhiteSpace(referenceId) ? null : referenceId.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            isTemporary,
            expiresAt,
            createdAt,
            createdBy);
    }

    public void MarkAsPermanent()
    {
        if (!IsTemporary)
        {
            return;
        }

        IsTemporary = false;
        ExpiresAt = null;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("File is already deleted.", "File.AlreadyDeleted");
        }

        MarkDeleted(deletedBy, deletedAt);
    }
}
