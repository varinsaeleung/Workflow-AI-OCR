namespace KmOcr.Domain.Documents;

/// <summary>
/// Defines the processing lifecycle of a document.
/// </summary>
public enum DocumentStatus
{
    /// <summary>
    /// The document binary and initial metadata have been saved.
    /// </summary>
    Uploaded = 0,

    /// <summary>
    /// The document has been placed on the OCR queue.
    /// </summary>
    OcrQueued = 1,

    /// <summary>
    /// OCR processing finished successfully.
    /// </summary>
    OcrCompleted = 2,

    /// <summary>
    /// OCR processing failed and may be retried.
    /// </summary>
    OcrFailed = 3,

    /// <summary>
    /// The document is currently in an approval workflow.
    /// </summary>
    InWorkflow = 4,

    /// <summary>
    /// The document has been approved.
    /// </summary>
    Approved = 5,

    /// <summary>
    /// The document has been rejected.
    /// </summary>
    Rejected = 6,

    /// <summary>
    /// The document has been soft-deleted or archived.
    /// </summary>
    Archived = 7
}
