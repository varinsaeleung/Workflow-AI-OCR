using KmOcr.Api.Models;
using KmOcr.Application.Auth;
using KmOcr.Application.WorkflowEngine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmOcr.Api.Controllers;

/// <summary>
/// Workflow definition APIs for the node-based drag-and-drop designer.
/// </summary>
[ApiController]
[Route("api/v1/workflow-definitions")]
[Produces("application/json")]
public sealed class WorkflowDefinitionsController : ControllerBase
{
    private readonly IWorkflowDesignerModule _designer;

    /// <summary>
    /// Creates the controller with workflow designer use cases.
    /// </summary>
    public WorkflowDefinitionsController(IWorkflowDesignerModule designer)
    {
        _designer = designer;
    }

    /// <summary>
    /// Creates a workflow definition draft.
    /// </summary>
    [Authorize(Policy = PermissionCodes.WorkflowWrite)]
    [HttpPost]
    [ProducesResponseType(typeof(WorkflowDefinitionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkflowDefinitionDto>> CreateAsync(CreateWorkflowDefinitionRequest request, CancellationToken cancellationToken)
    {
        var definition = await _designer.CreateAsync(new CreateWorkflowDefinitionCommand(request.Code, request.Name, request.CreatedBy), cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = definition.Id }, definition);
    }

    /// <summary>
    /// Lists workflow definitions.
    /// </summary>
    [Authorize(Policy = PermissionCodes.WorkflowRead)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkflowDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WorkflowDefinitionDto>>> ListAsync(CancellationToken cancellationToken)
    {
        var definitions = await _designer.ListAsync(cancellationToken);
        return Ok(definitions);
    }

    /// <summary>
    /// Gets one workflow definition by id.
    /// </summary>
    [Authorize(Policy = PermissionCodes.WorkflowRead)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkflowDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowDefinitionDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var definition = await _designer.GetAsync(id, cancellationToken);
        return Ok(definition);
    }

    /// <summary>
    /// Replaces workflow graph nodes and edges.
    /// </summary>
    [Authorize(Policy = PermissionCodes.WorkflowWrite)]
    [HttpPut("{id:guid}/graph")]
    [ProducesResponseType(typeof(WorkflowDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowDefinitionDto>> UpdateGraphAsync(Guid id, WorkflowGraphDto graph, CancellationToken cancellationToken)
    {
        var definition = await _designer.UpdateGraphAsync(id, graph, cancellationToken);
        return Ok(definition);
    }

    /// <summary>
    /// Validates workflow graph rules before publishing.
    /// </summary>
    [Authorize(Policy = PermissionCodes.WorkflowWrite)]
    [HttpPost("{id:guid}/validate")]
    [ProducesResponseType(typeof(WorkflowValidationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowValidationDto>> ValidateAsync(Guid id, CancellationToken cancellationToken)
    {
        var validation = await _designer.ValidateAsync(id, cancellationToken);
        return Ok(validation);
    }

    /// <summary>
    /// Publishes the current graph as an immutable workflow version.
    /// </summary>
    [Authorize(Policy = PermissionCodes.WorkflowWrite)]
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(WorkflowDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowDefinitionDto>> PublishAsync(Guid id, PublishWorkflowDefinitionRequest request, CancellationToken cancellationToken)
    {
        var definition = await _designer.PublishAsync(id, request.PublishedBy, cancellationToken);
        return Ok(definition);
    }
}
