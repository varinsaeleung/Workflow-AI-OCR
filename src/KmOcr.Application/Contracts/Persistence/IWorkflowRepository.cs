using KmOcr.Domain.Workflows;

namespace KmOcr.Application.Contracts.Persistence;

/// <summary>
/// Repository contract for workflow aggregate persistence.
/// </summary>
public interface IWorkflowRepository
{
    /// <summary>
    /// Adds a workflow instance to the current unit of work.
    /// </summary>
    Task AddAsync(WorkflowInstance workflow, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a workflow instance by identifier.
    /// </summary>
    Task<WorkflowInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Loads workflow tasks assigned to a user.
    /// </summary>
    Task<IReadOnlyList<WorkflowTask>> GetTasksForUserAsync(string assignee, CancellationToken cancellationToken);
}
