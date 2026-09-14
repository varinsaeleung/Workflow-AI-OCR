namespace KmOcr.Application.Workflows;

/// <summary>
/// Command data required to start a workflow for a document.
/// </summary>
public sealed record StartWorkflowCommand(Guid DocumentId, string TemplateKey, string StartedBy);
