using KmOcr.Domain.WorkflowEngine;

namespace KmOcr.Application.Contracts.Persistence;

/// <summary>
/// Repository contract for node-based workflow definitions.
/// </summary>
public interface IWorkflowDefinitionRepository
{
    /// <summary>
    /// Adds a workflow definition to the current unit of work.
    /// </summary>
    Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a workflow definition by identifier.
    /// </summary>
    Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Lists workflow definitions.
    /// </summary>
    Task<IReadOnlyList<WorkflowDefinition>> ListAsync(CancellationToken cancellationToken);
}
