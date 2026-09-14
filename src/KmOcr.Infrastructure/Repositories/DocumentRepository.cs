using KmOcr.Application.Contracts.Persistence;
using KmOcr.Domain.Documents;
using KmOcr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KmOcr.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of document repository operations.
/// </summary>
public sealed class DocumentRepository : IDocumentRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Creates the repository with an EF Core context.
    /// </summary>
    public DocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adds a document aggregate to the DbContext change tracker.
    /// </summary>
    public async Task AddAsync(Document document, CancellationToken cancellationToken)
    {
        await _context.Documents.AddAsync(document, cancellationToken);
    }

    /// <summary>
    /// Adds a document folder to the DbContext change tracker.
    /// </summary>
    public async Task AddFolderAsync(DocumentFolder folder, CancellationToken cancellationToken)
    {
        await _context.DocumentFolders.AddAsync(folder, cancellationToken);
    }

    /// <summary>
    /// Loads one document including metadata and OCR result.
    /// </summary>
    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Documents
            .Include(document => document.Metadata)
            .Include(document => document.Versions)
            .Include(document => document.OcrResult)
            .Include(document => document.AiExtraction)
            .SingleOrDefaultAsync(document => document.Id == id && document.DeletedAt == null, cancellationToken);
    }

    /// <summary>
    /// Loads one document folder by id.
    /// </summary>
    public async Task<DocumentFolder?> GetFolderByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.DocumentFolders
            .SingleOrDefaultAsync(folder => folder.Id == id && folder.DeletedAt == null, cancellationToken);
    }

    /// <summary>
    /// Lists active folders under a parent folder.
    /// </summary>
    public async Task<IReadOnlyList<DocumentFolder>> ListFoldersAsync(Guid? parentFolderId, CancellationToken cancellationToken)
    {
        return await _context.DocumentFolders
            .Where(folder => folder.ParentFolderId == parentFolderId && folder.DeletedAt == null)
            .OrderBy(folder => folder.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Searches documents by filename, type, OCR text, or metadata content.
    /// </summary>
    public async Task<IReadOnlyList<Document>> SearchAsync(string? query, CancellationToken cancellationToken)
    {
        var documents = _context.Documents
            .Include(document => document.Metadata)
            .Include(document => document.Versions)
            .Include(document => document.OcrResult)
            .Include(document => document.AiExtraction)
            .Where(document => document.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            documents = documents.Where(document =>
                document.FileName.ToLower().Contains(term)
                || document.DocumentType.ToLower().Contains(term)
                || document.Metadata.Any(metadata =>
                    metadata.Key.ToLower().Contains(term)
                    || metadata.Value.ToLower().Contains(term))
                || (document.OcrResult != null && document.OcrResult.ExtractedText.ToLower().Contains(term))
                || (document.AiExtraction != null && document.AiExtraction.Summary.ToLower().Contains(term)));
        }

        return await documents
            .OrderByDescending(document => document.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Counts documents grouped by processing status.
    /// </summary>
    public async Task<IReadOnlyDictionary<DocumentStatus, int>> CountByStatusAsync(CancellationToken cancellationToken)
    {
        return await _context.Documents
            .Where(document => document.DeletedAt == null)
            .GroupBy(document => document.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);
    }
}
