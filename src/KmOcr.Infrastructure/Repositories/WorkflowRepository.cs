using KmOcr.Application.Contracts.Persistence;
using KmOcr.Domain.Workflows;
using KmOcr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KmOcr.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of workflow repository operations.
/// </summary>
public sealed class WorkflowRepository : IWorkflowRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Creates the repository with an EF Core context.
    /// </summary>
    public WorkflowRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adds a workflow aggregate to the DbContext change tracker.
    /// </summary>
    public async Task AddAsync(WorkflowInstance workflow, CancellationToken cancellationToken)
    {
        await _context.WorkflowInstances.AddAsync(workflow, cancellationToken);
    }

    /// <summary>
    /// Loads one workflow including its tasks.
    /// </summary>
    public async Task<WorkflowInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.WorkflowInstances
            .Include(workflow => workflow.Tasks)
            .SingleOrDefaultAsync(workflow => workflow.Id == id, cancellationToken);
    }

    /// <summary>
    /// Loads tasks currently assigned to a user.
    /// </summary>
    public async Task<IReadOnlyList<WorkflowTask>> GetTasksForUserAsync(string assignee, CancellationToken cancellationToken)
    {
        return await _context.WorkflowTasks
            .Where(task => task.AssignedTo == assignee)
            .OrderByDescending(task => task.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }
}
