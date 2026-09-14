namespace KmOcr.Application.Contracts.Messaging;

/// <summary>
/// Message sent to RabbitMQ when a document should be processed by OCR workers.
/// </summary>
public sealed record OcrJobMessage(Guid DocumentId, string StoragePath, string ContentType);
