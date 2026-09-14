using System.Text.Json;
using KmOcr.Api.Models;
using KmOcr.Application.Auth;
using KmOcr.Application.Ai;
using KmOcr.Application.Common;
using KmOcr.Application.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmOcr.Api.Controllers;

/// <summary>
/// Document upload, metadata, and OCR APIs.
/// </summary>
[ApiController]
[Route("api/v1/documents")]
[Produces("application/json")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentModule _documents;
    private readonly IAiModule _ai;

    /// <summary>
    /// Creates the controller with document and AI application modules.
    /// </summary>
    public DocumentsController(IDocumentModule documents, IAiModule ai)
    {
        _documents = documents;
        _ai = ai;
    }

    /// <summary>
    /// Uploads a document file, stores metadata, and queues OCR processing.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsWrite)]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentDto>> UploadAsync(
        [FromForm] IFormFile file,
        [FromForm] string uploadedBy,
        [FromForm] string documentType,
        [FromForm] Guid? folderId,
        [FromForm] string? metadataJson,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new ValidationException("Uploaded file is required.");
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadDocumentCommand(
            file.FileName,
            file.ContentType,
            stream,
            uploadedBy,
            documentType,
            ParseMetadata(metadataJson),
            folderId);
        var document = await _documents.UploadAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = document.Id }, document);
    }

    /// <summary>
    /// Uploads a new binary version for an existing document.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsWrite)]
    [HttpPost("{id:guid}/versions")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> UploadVersionAsync(
        Guid id,
        [FromForm] IFormFile file,
        [FromForm] string createdBy,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new ValidationException("Uploaded file is required.");
        }

        await using var stream = file.OpenReadStream();
        var document = await _documents.UploadVersionAsync(new UploadDocumentVersionCommand(id, file.FileName, file.ContentType, stream, createdBy), cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = document.Id }, document);
    }

    /// <summary>
    /// Lists documents with optional free-text filtering.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsRead)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> ListAsync([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var documents = await _documents.SearchAsync(q, cancellationToken);
        return Ok(documents);
    }

    /// <summary>
    /// Gets a document by identifier.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsRead)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _documents.GetAsync(id, cancellationToken);
        return Ok(document);
    }

    /// <summary>
    /// Downloads the current version of a document.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsRead)]
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAsync(Guid id, CancellationToken cancellationToken)
    {
        var download = await _documents.DownloadAsync(new DownloadDocumentCommand(id, null), cancellationToken);
        return File(download.Content, download.ContentType, download.FileName);
    }

    /// <summary>
    /// Downloads a specific document version.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsRead)]
    [HttpGet("{id:guid}/versions/{versionNumber:int}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadVersionAsync(Guid id, int versionNumber, CancellationToken cancellationToken)
    {
        var download = await _documents.DownloadAsync(new DownloadDocumentCommand(id, versionNumber), cancellationToken);
        return File(download.Content, download.ContentType, download.FileName);
    }

    /// <summary>
    /// Gets a time-limited preview URL for the current document version.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsRead)]
    [HttpGet("{id:guid}/preview")]
    [ProducesResponseType(typeof(DocumentPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentPreviewDto>> PreviewAsync(Guid id, CancellationToken cancellationToken)
    {
        var preview = await _documents.PreviewAsync(id, cancellationToken);
        return Ok(preview);
    }

    /// <summary>
    /// Updates metadata values for a document.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsWrite)]
    [HttpPut("{id:guid}/metadata")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> UpdateMetadataAsync(Guid id, UpdateMetadataRequest request, CancellationToken cancellationToken)
    {
        var document = await _documents.UpdateMetadataAsync(new UpdateMetadataCommand(id, request.Metadata, request.Actor), cancellationToken);
        return Ok(document);
    }

    /// <summary>
    /// Moves a document into a folder or root.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsWrite)]
    [HttpPost("{id:guid}/move")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> MoveAsync(Guid id, MoveDocumentRequest request, CancellationToken cancellationToken)
    {
        var document = await _documents.MoveAsync(new MoveDocumentCommand(id, request.FolderId, request.Actor), cancellationToken);
        return Ok(document);
    }

    /// <summary>
    /// Soft-deletes a document while retaining storage objects for audit and retention.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsDelete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, DeleteDocumentRequest request, CancellationToken cancellationToken)
    {
        await _documents.DeleteAsync(new DeleteDocumentCommand(id, request.Actor), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Queues a document for OCR reprocessing.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsWrite)]
    [HttpPost("{id:guid}/reprocess-ocr")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> ReprocessOcrAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _documents.QueueOcrAsync(id, cancellationToken);
        return Accepted(document);
    }

    /// <summary>
    /// Gets OCR result details for a document.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsRead)]
    [HttpGet("{id:guid}/ocr")]
    [ProducesResponseType(typeof(OcrResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OcrResultResponse>> GetOcrAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _documents.GetAsync(id, cancellationToken);
        return Ok(new OcrResultResponse(document.Id, document.OcrText, document.OcrConfidence, document.Status));
    }

    /// <summary>
    /// Runs AI classification and metadata extraction for a document.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsAi)]
    [HttpPost("{id:guid}/ai-extraction")]
    [ProducesResponseType(typeof(AiExtractionDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiExtractionDto>> RunAiExtractionAsync(Guid id, CancellationToken cancellationToken)
    {
        var extraction = await _ai.RunExtractionAsync(id, cancellationToken);
        return Accepted(extraction);
    }

    /// <summary>
    /// Gets AI classification and metadata extraction output for a document.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsRead)]
    [HttpGet("{id:guid}/ai-extraction")]
    [ProducesResponseType(typeof(AiExtractionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiExtractionDto>> GetAiExtractionAsync(Guid id, CancellationToken cancellationToken)
    {
        var extraction = await _ai.GetExtractionAsync(id, cancellationToken);
        return Ok(extraction);
    }

    /// <summary>
    /// Parses JSON metadata from multipart form data.
    /// </summary>
    private static IReadOnlyDictionary<string, string> ParseMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson)
                ?? new Dictionary<string, string>();
        }
        catch (JsonException exception)
        {
            throw new ValidationException($"Metadata JSON is invalid: {exception.Message}");
        }
    }
}
