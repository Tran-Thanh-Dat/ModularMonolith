# Files Module — Storage Foundation

Reusable file upload, download, and metadata management for the modular monolith.

## Storage provider abstraction

`IFileStorageProvider` is the extension point for physical storage:

- `SaveAsync` — persist stream, return `StoredFileResult`
- `OpenReadAsync` — read stored file
- `ExistsAsync` / `DeleteAsync` — storage lifecycle helpers
- `ProviderName` — e.g. `Local`, future `S3`, `MinIO`, `AzureBlob`

Register the active provider in `Files.Infrastructure` based on `FileStorage:Provider`.

## Local provider configuration

`appsettings.json`:

```json
"FileStorage": {
  "Provider": "Local",
  "Local": {
    "RootPath": "uploads",
    "PublicBaseUrl": ""
  },
  "MaxFileSizeMb": 20,
  "AllowedExtensions": [ ".jpg", ".jpeg", ".png", ".pdf", ".docx", ".xlsx", ".csv" ],
  "AllowedContentTypes": [ ... ]
}
```

### Production RootPath guidance

- **Development:** relative `RootPath` such as `uploads` under ApiHost content root is fine.
- **Production:** configure an **absolute path outside** the repository and deployment folder (e.g. `D:/AppData/modular-api/uploads` or `/var/app/uploads`). Do not store production uploads inside source control or the published app directory.

## Upload atomicity (compensating delete)

Upload writes the physical file before `TransactionBehavior` commits metadata. To prevent orphan files:

1. After a successful `SaveAsync`, the storage path is tracked in scoped `IFileStorageCompensationBuffer`.
2. `FileStorageCompensationPostCommitHook` (`IPostCommitHook`):
   - **On commit success:** clears the buffer without deleting files.
   - **On rollback** (`Result.Failure` or exception): best-effort `DeleteAsync` for each pending path, then clears the buffer.
3. Cleanup failures are logged and do not hide the original business exception.

Successful upload: physical file + metadata + post-commit activity log + audit log.

Failed upload after disk write: physical file removed, no metadata, no false success activity log.

## Unit of work

- Inject **`FilesUnitOfWork`** in Files services — not plain `IUnitOfWork`.
- Also register `FilesUnitOfWork` as `IUnitOfWork` so `TransactionBehavior` receives it via `IEnumerable<IUnitOfWork>`.

## Soft delete vs physical delete

- `DELETE /api/v1/files/{id}` soft-deletes **metadata** only.
- Physical files remain on disk by default (prepared for a future background cleanup phase).
- `IFileStorageProvider.DeleteAsync` is used for compensating rollback and future explicit purge jobs — not for normal API delete.

## Temporary files

`IsTemporary` and `ExpiresAt` are stored for future background cleanup. No cleanup worker in this phase.

## Security notes

- Extension and content-type whitelist validation on upload.
- Basic magic-byte checks for PDF, PNG, JPEG, DOCX/XLSX (ZIP header); CSV relies on non-empty content.
- Extension + content-type validation is **not** strong file-type verification on its own.
- **TODO:** expand magic-byte validation and integrate antivirus scanning (see `LocalFileStorageProvider.SaveAsync`).
- Original file names are sanitized; stored names are server-generated GUIDs.
- Storage paths are server-controlled; absolute local paths are never returned in API responses.

## API routes

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/api/v1/files` | File.View |
| GET | `/api/v1/files/{id}` | File.View |
| POST | `/api/v1/files/upload` | File.Upload |
| GET | `/api/v1/files/{id}/download` | File.Download |
| DELETE | `/api/v1/files/{id}` | File.Delete |
| PATCH | `/api/v1/files/{id}/mark-permanent` | File.MarkPermanent |

## EF migrations

```powershell
dotnet ef migrations add <Name> --project src/Modules/Files/Files.Infrastructure --startup-project src/ApiHost --context FilesDbContext
dotnet ef database update --project src/Modules/Files/Files.Infrastructure --startup-project src/ApiHost --context FilesDbContext
```

## Smoke test checklist

1. Login as Admin (`POST /api/v1/auth/login`).
2. Upload valid PDF or PNG (`POST /api/v1/files/upload`) → **201**.
3. Download uploaded file → file stream returned.
4. GET file detail and list with filters → **200**.
5. PATCH mark permanent → **200**.
6. PATCH mark permanent again → **200**, no duplicate misleading activity log.
7. DELETE file → soft-deleted metadata.
8. Invalid upload (wrong extension, empty file, oversize) → **400**, no orphan physical file.
9. If possible, force DB failure after storage write → physical file cleaned up.
10. ActivityLog appears after successful upload only.
11. AuditLog records create / soft delete / mark permanent.

Restart ApiHost after adding File permissions so `IdentitySeeder` assigns them to Admin/SuperAdmin on existing dev databases.
