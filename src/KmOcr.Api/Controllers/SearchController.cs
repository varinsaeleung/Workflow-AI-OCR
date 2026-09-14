using KmOcr.Application.Auth;
using KmOcr.Application.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmOcr.Api.Controllers;

/// <summary>
/// Search APIs for document discovery.
/// </summary>
[ApiController]
[Route("api/v1/search")]
[Produces("application/json")]
[Authorize(Policy = PermissionCodes.DocumentsRead)]
public sealed class SearchController : ControllerBase
{
    private readonly ISearchModule _search;
    private readonly ISearchIndexingModule _indexing;

    /// <summary>
    /// Creates the controller with search and indexing application modules.
    /// </summary>
    public SearchController(ISearchModule search, ISearchIndexingModule indexing)
    {
        _search = search;
        _indexing = indexing;
    }

    /// <summary>
    /// Searches indexed documents by document fields, OCR text, AI output, metadata, and filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(DocumentSearchResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentSearchResponseDto>> SearchAsync(
        [FromQuery] string? q,
        [FromQuery] string? documentType,
        [FromQuery] string? status,
        [FromQuery] Guid? folderId,
        [FromQuery] string? metadataKey,
        [FromQuery] string? metadataValue,
        [FromQuery] int size,
        [FromQuery] int from,
        CancellationToken cancellationToken)
    {
        var response = await _search.SearchAsync(
            new SearchDocumentsQuery(q, documentType, status, folderId, metadataKey, metadataValue, size <= 0 ? 20 : size, Math.Max(from, 0)),
            cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Rebuilds the OpenSearch index document for one PostgreSQL document.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsWrite)]
    [HttpPost("documents/{documentId:guid}/index")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IndexDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        await _indexing.IndexDocumentAsync(documentId, cancellationToken);
        return Accepted();
    }
}
