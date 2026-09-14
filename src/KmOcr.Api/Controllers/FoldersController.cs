using KmOcr.Api.Models;
using KmOcr.Application.Auth;
using KmOcr.Application.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmOcr.Api.Controllers;

/// <summary>
/// Folder APIs for organizing enterprise documents.
/// </summary>
[ApiController]
[Route("api/v1/folders")]
[Produces("application/json")]
public sealed class FoldersController : ControllerBase
{
    private readonly IDocumentModule _documents;

    /// <summary>
    /// Creates the folder controller with document use cases.
    /// </summary>
    public FoldersController(IDocumentModule documents)
    {
        _documents = documents;
    }

    /// <summary>
    /// Creates a document folder.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsWrite)]
    [HttpPost]
    [ProducesResponseType(typeof(DocumentFolderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentFolderDto>> CreateAsync(CreateFolderRequest request, CancellationToken cancellationToken)
    {
        var folder = await _documents.CreateFolderAsync(new CreateFolderCommand(request.Name, request.ParentFolderId, request.CreatedBy), cancellationToken);
        return CreatedAtAction(nameof(ListAsync), new { parentFolderId = folder.ParentFolderId }, folder);
    }

    /// <summary>
    /// Lists folders under a parent folder.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsRead)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentFolderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentFolderDto>>> ListAsync([FromQuery] Guid? parentFolderId, CancellationToken cancellationToken)
    {
        var folders = await _documents.ListFoldersAsync(parentFolderId, cancellationToken);
        return Ok(folders);
    }

    /// <summary>
    /// Moves a folder under another folder or root.
    /// </summary>
    [Authorize(Policy = PermissionCodes.DocumentsWrite)]
    [HttpPost("{id:guid}/move")]
    [ProducesResponseType(typeof(DocumentFolderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentFolderDto>> MoveAsync(Guid id, MoveFolderRequest request, CancellationToken cancellationToken)
    {
        var folder = await _documents.MoveFolderAsync(new MoveFolderCommand(id, request.ParentFolderId, request.Actor), cancellationToken);
        return Ok(folder);
    }
}
