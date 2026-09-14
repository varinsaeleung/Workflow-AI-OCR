using KmOcr.Api.Models;
using KmOcr.Application.Auth;
using KmOcr.Application.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmOcr.Api.Controllers;

/// <summary>
/// Workflow APIs for review, approval, rejection, and task assignment.
/// </summary>
[ApiController]
[Route("api/v1/workflows")]
[Produces("application/json")]
public sealed class WorkflowController : ControllerBase
{
    private readonly IWorkflowModule _workflows;

    /// <summary>
    /// Creates the controller with the workflow application module.
    /// </summary>
    public WorkflowController(IWorkflowModule workflows)
    {
        _workflows = workflows;
    }

    /// <summary>
    /// Starts a workflow for a document.
    /// </summary>
    [Authorize(Policy = PermissionCodes.WorkflowWrite)]
    [HttpPost]
    [ProducesResponseType(typeof(WorkflowDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkflowDto>> StartAsync(StartWorkflowRequest request, CancellationToken cancellationToken)
    {
        var workflow = await _workflows.StartAsync(
            new StartWorkflowCommand(request.DocumentId, request.TemplateKey, request.StartedBy),
            cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = workflow.Id }, workflow);
    }

    /// <summary>
    /// Gets a workflow by identifier.
    /// </summary>
    [Authorize(Policy = PermissionCodes.WorkflowRead)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkflowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await _workflows.GetAsync(id, cancellationToken);
        return Ok(workflow);
    }

    /// <summary>
    /// Lists tasks assigned to one user.
    /// </summary>
    [Authorize(Policy = PermissionCodes.WorkflowRead)]
    [HttpGet("tasks/my")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkflowTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<WorkflowTaskDto>>> MyTasksAsync([FromQuery] string assignee, CancellationToken cancellationToken)
    {
        var tasks = await _workflows.GetMyTasksAsync(assignee, cancellationToken);
        return Ok(tasks);
    }

    /// <summary>
    /// Assigns a workflow task to another user.
    /// </summary>
    [Authorize(Policy = PermissionCodes.WorkflowWrite)]
    [HttpPost("{workflowId:guid}/tasks/{taskId:guid}/assign")]
    [ProducesResponseType(typeof(WorkflowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowDto>> AssignAsync(Guid workflowId, Guid taskId, AssignWorkflowTaskRequest request, CancellationToken cancellationToken)
    {
        var workflow = await _workflows.AssignAsync(new AssignWorkflowTaskCommand(workflowId, taskId, request.Assignee), cancellationToken);
        return Ok(workflow);
    }

    /// <summary>
    /// Approves a workflow task.
    /// </summary>
    [Authorize(Policy = PermissionCodes.WorkflowApprove)]
    [HttpPost("{workflowId:guid}/tasks/{taskId:guid}/approve")]
    [ProducesResponseType(typeof(WorkflowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowDto>> ApproveAsync(Guid workflowId, Guid taskId, CompleteWorkflowTaskRequest request, CancellationToken cancellationToken)
    {
        var workflow = await _workflows.ApproveAsync(new CompleteWorkflowTaskCommand(workflowId, taskId, request.Actor, request.Comment), cancellationToken);
        return Ok(workflow);
    }

    /// <summary>
    /// Rejects a workflow task.
    /// </summary>
    [Authorize(Policy = PermissionCodes.WorkflowApprove)]
    [HttpPost("{workflowId:guid}/tasks/{taskId:guid}/reject")]
    [ProducesResponseType(typeof(WorkflowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowDto>> RejectAsync(Guid workflowId, Guid taskId, CompleteWorkflowTaskRequest request, CancellationToken cancellationToken)
    {
        var workflow = await _workflows.RejectAsync(new CompleteWorkflowTaskCommand(workflowId, taskId, request.Actor, request.Comment), cancellationToken);
        return Ok(workflow);
    }
}
