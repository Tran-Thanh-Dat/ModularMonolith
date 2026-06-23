using BuildingBlocks.Application.Errors;
using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using Files.Api.Contracts;
using Files.Application.Files;
using Files.Application.Files.DeleteFile;
using Files.Application.Files.DownloadFile;
using Files.Application.Files.GetFileById;
using Files.Application.Files.GetFiles;
using Files.Application.Files.MarkFileAsPermanent;
using Files.Application.Files.UploadFile;
using Files.Application.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Files.Api.Controllers;

[Authorize]
[Route("api/v1/files")]
public sealed class FilesController : BaseApiController
{
    private readonly ISender _sender;

    public FilesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [HasPermission(FilesPermissionCodes.View)]
    public async Task<ActionResult<PagedResponse<FileResourceListItemResponse>>> GetFiles(
        [FromQuery] string? keyword,
        [FromQuery] string? moduleName,
        [FromQuery] string? referenceType,
        [FromQuery] string? referenceId,
        [FromQuery] string? contentType,
        [FromQuery] string? fileExtension,
        [FromQuery] bool? isTemporary,
        [FromQuery] bool? isActive,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetFilesQuery(
                keyword,
                moduleName,
                referenceType,
                referenceId,
                contentType,
                fileExtension,
                isTemporary,
                isActive,
                fromDate,
                toDate,
                pageIndex,
                pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(FilesPermissionCodes.View)]
    public async Task<ActionResult<ApiResponse<FileResourceDetailResponse>>> GetFileById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetFileByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost("upload")]
    [HasPermission(FilesPermissionCodes.Upload)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<UploadFileResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<UploadFileResponse>>> UploadFile(
        [FromForm] UploadFileRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            throw new BuildingBlocks.Application.Exceptions.ValidationException(
                new Dictionary<string, string[]>
                {
                    [nameof(request.File)] = ["File is required."]
                });
        }

        await using var stream = request.File.OpenReadStream();
        var result = await _sender.Send(
            new UploadFileCommand(
                stream,
                request.File.FileName,
                request.File.ContentType,
                request.File.Length,
                request.ModuleName,
                request.ReferenceType,
                request.ReferenceId,
                request.Description,
                request.IsTemporary),
            cancellationToken);

        return CreatedFromResult(
            nameof(GetFileById),
            new { id = result.IsSuccess ? result.Data!.Id : Guid.Empty },
            result);
    }

    [HttpGet("{id:guid}/download")]
    [HasPermission(FilesPermissionCodes.Download)]
    public async Task<IActionResult> DownloadFile(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DownloadFileQuery(id), cancellationToken);

        if (!result.IsSuccess || result.Data is null)
        {
            var response = ApiResponse<FileDownloadResult>.Fail(
                result.Code,
                result.Message,
                traceId: TraceId);

            return StatusCode(ResultStatusMapper.MapStatusCode(result.Code), response);
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(FilesPermissionCodes.Delete)]
    public async Task<ActionResult<ApiResponse>> DeleteFile(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteFileCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/mark-permanent")]
    [HasPermission(FilesPermissionCodes.MarkPermanent)]
    public async Task<ActionResult<ApiResponse>> MarkFileAsPermanent(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MarkFileAsPermanentCommand(id), cancellationToken);
        return FromResult(result);
    }
}
