using KmOcr.Application.Contracts.Persistence;
using KmOcr.Domain.WorkflowEngine;
using KmOcr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KmOcr.Infrastructure.Repositories;

/// <summary>
/// Entity Framework repository for node-based workflow definitions.
/// </summary>
public sealed class WorkflowDefinitionRepository : IWorkflowDefinitionRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Creates the repository with the application DbContext.
    /// </summary>
    public WorkflowDefinitionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adds a workflow definition to the current unit of work.
    /// </summary>
    public async Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        await _context.WorkflowDefinitions.AddAsync(definition, cancellationToken);
    }

    /// <summary>
    /// Loads a workflow definition with its graph and versions.
    /// </summary>
    public async Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.WorkflowDefinitions
            .Include(definition => definition.Nodes)
            .Include(definition => definition.Edges)
            .Include(definition => definition.Versions)
            .SingleOrDefaultAsync(definition => definition.Id == id, cancellationToken);
    }

    /// <summary>
    /// Lists workflow definitions with graph details.
    /// </summary>
    public async Task<IReadOnlyList<WorkflowDefinition>> ListAsync(CancellationToken cancellationToken)
    {
        return await _context.WorkflowDefinitions
            .Include(definition => definition.Nodes)
            .Include(definition => definition.Edges)
            .OrderByDescending(definition => definition.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }
}
