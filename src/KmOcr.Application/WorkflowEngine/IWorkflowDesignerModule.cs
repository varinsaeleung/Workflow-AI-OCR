namespace KmOcr.Application.WorkflowEngine;

/// <summary>
/// Application module for workflow designer use cases.
/// </summary>
public interface IWorkflowDesignerModule
{
    /// <summary>
    /// Creates a new workflow definition.
    /// </summary>
    Task<WorkflowDefinitionDto> CreateAsync(CreateWorkflowDefinitionCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Lists workflow definitions.
    /// </summary>
    Task<IReadOnlyList<WorkflowDefinitionDto>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a workflow definition.
    /// </summary>
    Task<WorkflowDefinitionDto> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces a workflow definition graph.
    /// </summary>
    Task<WorkflowDefinitionDto> UpdateGraphAsync(Guid id, WorkflowGraphDto graph, CancellationToken cancellationToken);

    /// <summary>
    /// Validates a workflow definition graph.
    /// </summary>
    Task<WorkflowValidationDto> ValidateAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Publishes a workflow definition graph.
    /// </summary>
    Task<WorkflowDefinitionDto> PublishAsync(Guid id, string publishedBy, CancellationToken cancellationToken);
}
