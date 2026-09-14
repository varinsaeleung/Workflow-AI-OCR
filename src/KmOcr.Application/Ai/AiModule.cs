using System.Text.Json;
using KmOcr.Application.Common;
using KmOcr.Application.Contracts.Ai;
using KmOcr.Application.Contracts.Persistence;
using Microsoft.Extensions.Logging;

namespace KmOcr.Application.Ai;

/// <summary>
/// Implements AI extraction use cases without depending on a concrete AI provider.
/// </summary>
public sealed class AiModule : IAiModule
{
    private readonly IDocumentRepository _documents;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiEngine _aiEngine;
    private readonly ILogger<AiModule> _logger;

    /// <summary>
    /// Creates the AI module with repository, unit of work, AI engine, and logger dependencies.
    /// </summary>
    public AiModule(IDocumentRepository documents, IUnitOfWork unitOfWork, IAiEngine aiEngine, ILogger<AiModule> logger)
    {
        _documents = documents;
        _unitOfWork = unitOfWork;
        _aiEngine = aiEngine;
        _logger = logger;
    }

    /// <summary>
    /// Runs AI extraction using OCR text and stores the output on the document.
    /// </summary>
    public async Task<AiExtractionDto> RunExtractionAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await LoadDocumentAsync(documentId, cancellationToken);
        var text = document.OcrResult?.ExtractedText;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ValidationException("OCR text is required before AI extraction.");
        }

        var result = await _aiEngine.ExtractAsync(text, cancellationToken);
        document.CompleteAiExtraction(
            result.Classification,
            result.Summary,
            JsonSerializer.Serialize(result.Entities),
            result.ConfidenceScore,
            result.Engine);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("AI extraction completed for document {DocumentId}.", document.Id);
        return Map(document);
    }

    /// <summary>
    /// Gets AI extraction output for one document.
    /// </summary>
    public async Task<AiExtractionDto> GetExtractionAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await LoadDocumentAsync(documentId, cancellationToken);
        return Map(document);
    }

    /// <summary>
    /// Loads a document aggregate and converts missing data into an application exception.
    /// </summary>
    private async Task<Domain.Documents.Document> LoadDocumentAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _documents.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Document '{id}' was not found.");
    }

    /// <summary>
    /// Maps domain AI extraction state to a client-safe DTO.
    /// </summary>
    private static AiExtractionDto Map(Domain.Documents.Document document)
    {
        if (document.AiExtraction is null)
        {
            return new AiExtractionDto(document.Id, null, null, new Dictionary<string, string>(), null, null);
        }

        var entities = JsonSerializer.Deserialize<Dictionary<string, string>>(document.AiExtraction.ExtractedEntitiesJson)
            ?? new Dictionary<string, string>();
        return new AiExtractionDto(
            document.Id,
            document.AiExtraction.Classification,
            document.AiExtraction.Summary,
            entities,
            document.AiExtraction.ConfidenceScore,
            document.AiExtraction.Engine);
    }
}
