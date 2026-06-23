# Files

File upload, download, metadata, and storage management.

## API

Base route: `/api/v1/files`

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/api/v1/files` | `File.View` |
| GET | `/api/v1/files/{id}` | `File.View` |
| POST | `/api/v1/files/upload` | `File.Upload` |
| GET | `/api/v1/files/{id}/download` | `File.Download` |
| DELETE | `/api/v1/files/{id}` | `File.Delete` |
| PATCH | `/api/v1/files/{id}/mark-permanent` | `File.MarkPermanent` |

## Upload flow

1. Client sends `multipart/form-data` to `POST /api/v1/files/upload`.
2. FluentValidation checks size, extension, content type, filename safety.
3. File bytes saved to storage provider (local disk by default).
4. Magic-byte signature validated against declared extension.
5. `FileResource` metadata persisted in PostgreSQL.
6. On transaction rollback, `FileStorageCompensationPostCommitHook` deletes orphaned files.

## Download flow

1. `GET /api/v1/files/{id}/download` loads metadata (cached detail optional).
2. Storage provider streams binary with correct content type and filename.
3. Returns raw file stream (not wrapped in `ApiResponse`).

## Metadata vs binary

- **Metadata** — stored in `files` schema (`FileResource` entity): id, names, paths, module/reference, flags.
- **Binary** — stored on disk under `FileStorage:Local:RootPath` (default `uploads/`).

## Storage provider

Config section: `FileStorage`

| Setting | Default |
|---------|---------|
| `Provider` | `Local` |
| `Local.RootPath` | `uploads` |
| `MaxFileSizeMb` | 20 |
| `AllowedExtensions` | `.jpg`, `.jpeg`, `.png`, `.pdf`, `.docx`, `.xlsx`, `.csv` |

Abstraction: `IFileStorageProvider` — only Local is implemented today.

## Validation

| Check | Error code |
|-------|------------|
| Empty file | `File.Empty` |
| Over max size | `File.TooLarge` |
| Invalid/unsafe filename | `File.InvalidFileName` |
| Extension not in whitelist | `File.ExtensionNotAllowed` |
| Content-Type not in whitelist (request MIME) | `File.ContentTypeNotAllowed` |
| Magic bytes mismatch with declared extension | `File.ContentTypeNotAllowed` |

Extension whitelist failures use `File.ExtensionNotAllowed`. Both declared content-type violations and magic-byte/signature mismatches map to `File.ContentTypeNotAllowed` — there is no separate `File.InvalidSignature` code.

Filename sanitization removes path segments and unsafe characters before storage.

## Access control

Files APIs are **permission-based only** (`File.View`, `File.Upload`, etc.). Per-user or per-tenant file ownership is **not enforced** by the Files module today. Upload metadata fields (`ModuleName`, `ReferenceType`, `ReferenceId`) are for linking; consuming modules must enforce their own ownership rules if needed. **Backlog:** optional ownership enforcement in Files.

## Temporary vs permanent

- Upload with `isTemporary=true` — eligible for cleanup by recurring job `background-jobs:temporary-file-cleanup`.
- `PATCH .../mark-permanent` — promotes file; idempotent if already permanent.

## Soft delete

`DELETE` soft-deactivates metadata. Physical purge is handled by background jobs when configured (`DeletePhysicalFiles`).

## Security notes

- Never expose storage root path in API responses.
- Download requires `File.Download` permission.
- Validate extension + content type + signature — not extension alone.
- **Backlog:** expand magic-byte coverage and antivirus scanning (see `FileSignatureValidator` TODO).

## Example curl

```bash
# Upload
curl -X POST "http://localhost:5080/api/v1/files/upload" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -F "file=@document.pdf" \
  -F "moduleName=Categories" \
  -F "isTemporary=false"

# Download
curl -o out.pdf "http://localhost:5080/api/v1/files/{id}/download" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

See [POST_COMMIT_HOOKS.md](./POST_COMMIT_HOOKS.md) and [BACKGROUND_JOBS.md](./BACKGROUND_JOBS.md).
