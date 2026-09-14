using KmOcr.Application.Common;
using KmOcr.Application.Contracts.Persistence;
using KmOcr.Domain.WorkflowEngine;
using Microsoft.Extensions.Logging;

namespace KmOcr.Application.WorkflowEngine;

/// <summary>
/// Implements workflow designer use cases.
/// </summary>
public sealed class WorkflowDesignerModule : IWorkflowDesignerModule
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WorkflowDesignerModule> _logger;

    /// <summary>
    /// Creates the module with persistence abstractions.
    /// </summary>
    public WorkflowDesignerModule(IWorkflowDefinitionRepository definitions, IUnitOfWork unitOfWork, ILogger<WorkflowDesignerModule> logger)
    {
        _definitions = definitions;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Creates and persists a workflow definition.
    /// </summary>
    public async Task<WorkflowDefinitionDto> CreateAsync(CreateWorkflowDefinitionCommand command, CancellationToken cancellationToken)
    {
        var definition = WorkflowDefinition.Create(command.Code, command.Name, command.CreatedBy);
        await _definitions.AddAsync(definition, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Workflow definition {WorkflowDefinitionId} created.", definition.Id);
        return Map(definition);
    }

    /// <summary>
    /// Lists workflow definitions.
    /// </summary>
    public async Task<IReadOnlyList<WorkflowDefinitionDto>> ListAsync(CancellationToken cancellationToken)
    {
        var definitions = await _definitions.ListAsync(cancellationToken);
        return definitions.Select(Map).ToList();
    }

    /// <summary>
    /// Gets one workflow definition.
    /// </summary>
    public async Task<WorkflowDefinitionDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return Map(await LoadAsync(id, cancellationToken));
    }

    /// <summary>
    /// Replaces graph nodes and edges for a workflow definition.
    /// </summary>
    public async Task<WorkflowDefinitionDto> UpdateGraphAsync(Guid id, WorkflowGraphDto graph, CancellationToken cancellationToken)
    {
        var definition = await LoadAsync(id, cancellationToken);
        definition.ReplaceGraph(
            graph.Nodes.Select(node => new WorkflowNodeDraft(node.NodeKey, ParseNodeType(node.NodeType), node.PositionX, node.PositionY, node.ConfigJson)),
            graph.Edges.Select(edge => new WorkflowEdgeDraft(edge.SourceNodeKey, edge.TargetNodeKey, edge.ConditionExpression)));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(definition);
    }

    /// <summary>
    /// Validates a workflow definition graph.
    /// </summary>
    public async Task<WorkflowValidationDto> ValidateAsync(Guid id, CancellationToken cancellationToken)
    {
        var definition = await LoadAsync(id, cancellationToken);
        var errors = definition.Validate();
        return new WorkflowValidationDto(errors.Count == 0, errors);
    }

    /// <summary>
    /// Publishes a workflow definition after validation passes.
    /// </summary>
    public async Task<WorkflowDefinitionDto> PublishAsync(Guid id, string publishedBy, CancellationToken cancellationToken)
    {
        var definition = await LoadAsync(id, cancellationToken);
        definition.Publish(publishedBy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Workflow definition {WorkflowDefinitionId} published as version {VersionNumber}.", definition.Id, definition.PublishedVersionNumber);
        return Map(definition);
    }

    /// <summary>
    /// Loads a workflow definition or throws when missing.
    /// </summary>
    private async Task<WorkflowDefinition> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _definitions.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Workflow definition '{id}' was not found.");
    }

    /// <summary>
    /// Parses a node type string into the domain enum.
    /// </summary>
    private static WorkflowNodeType ParseNodeType(string nodeType)
    {
        if (!Enum.TryParse<WorkflowNodeType>(nodeType, ignoreCase: true, out var parsed))
        {
            throw new ValidationException($"Workflow node type '{nodeType}' is not supported.");
        }

        return parsed;
    }

    /// <summary>
    /// Maps a domain definition to a DTO.
    /// </summary>
    private static WorkflowDefinitionDto Map(WorkflowDefinition definition)
    {
        return new WorkflowDefinitionDto(
            definition.Id,
            definition.Code,
            definition.Name,
            definition.CreatedBy,
            definition.IsPublished,
            definition.PublishedVersionNumber,
            new WorkflowGraphDto(
                definition.Nodes.Select(node => new WorkflowNodeDto(node.NodeKey, node.NodeType.ToString(), node.PositionX, node.PositionY, node.ConfigJson)).ToList(),
                definition.Edges.Select(edge => new WorkflowEdgeDto(edge.SourceNodeKey, edge.TargetNodeKey, edge.ConditionExpression)).ToList()));
    }
}
