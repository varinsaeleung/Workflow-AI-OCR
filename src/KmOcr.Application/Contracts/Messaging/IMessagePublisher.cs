namespace KmOcr.Application.Contracts.Messaging;

/// <summary>
/// Abstraction for publishing asynchronous processing jobs.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a document OCR job to the message broker.
    /// </summary>
    Task PublishOcrJobAsync(OcrJobMessage message, CancellationToken cancellationToken);
}
