using KmOcr.Application.Common;
using KmOcr.Application.Contracts.Persistence;
using KmOcr.Domain.Workflows;
using Microsoft.Extensions.Logging;

namespace KmOcr.Application.Workflows;

/// <summary>
/// Implements workflow use cases without depending on infrastructure frameworks.
/// </summary>
public sealed class WorkflowModule : IWorkflowModule
{
    private readonly IDocumentRepository _documents;
    private readonly IWorkflowRepository _workflows;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WorkflowModule> _logger;

    /// <summary>
    /// Creates the workflow module with persistence abstractions.
    /// </summary>
    public WorkflowModule(IDocumentRepository documents, IWorkflowRepository workflows, IUnitOfWork unitOfWork, ILogger<WorkflowModule> logger)
    {
        _documents = documents;
        _workflows = workflows;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Starts a workflow and commits it.
    /// </summary>
    public async Task<WorkflowDto> StartAsync(StartWorkflowCommand command, CancellationToken cancellationToken)
    {
        ValidateStart(command);
        var document = await LoadDocumentAsync(command.DocumentId, cancellationToken);
        var workflow = WorkflowInstance.Start(command.DocumentId, command.TemplateKey, command.StartedBy);
        document.MoveToWorkflow();
        await _workflows.AddAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Workflow {WorkflowId} started for document {DocumentId}.", workflow.Id, workflow.DocumentId);
        return Map(workflow);
    }

    /// <summary>
    /// Loads a workflow or throws when it does not exist.
    /// </summary>
    public async Task<WorkflowDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await LoadWorkflowAsync(id, cancellationToken);
        return Map(workflow);
    }

    /// <summary>
    /// Lists tasks assigned to one user.
    /// </summary>
    public async Task<IReadOnlyList<WorkflowTaskDto>> GetMyTasksAsync(string assignee, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(assignee))
        {
            throw new ValidationException("Assignee is required.");
        }

        var tasks = await _workflows.GetTasksForUserAsync(assignee.Trim(), cancellationToken);
        return tasks.Select(MapTask).ToList();
    }

    /// <summary>
    /// Assigns a workflow task and commits the change.
    /// </summary>
    public async Task<WorkflowDto> AssignAsync(AssignWorkflowTaskCommand command, CancellationToken cancellationToken)
    {
        var workflow = await LoadWorkflowAsync(command.WorkflowId, cancellationToken);
        workflow.AssignTask(command.TaskId, command.Assignee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Workflow task {TaskId} assigned to {Assignee}.", command.TaskId, command.Assignee);
        return Map(workflow);
    }

    /// <summary>
    /// Approves a workflow task and commits the change.
    /// </summary>
    public async Task<WorkflowDto> ApproveAsync(CompleteWorkflowTaskCommand command, CancellationToken cancellationToken)
    {
        var workflow = await LoadWorkflowAsync(command.WorkflowId, cancellationToken);
        workflow.ApproveTask(command.TaskId, command.Actor, command.Comment);
        await UpdateDocumentStatusAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Workflow task {TaskId} approved by {Actor}.", command.TaskId, command.Actor);
        return Map(workflow);
    }

    /// <summary>
    /// Rejects a workflow task and commits the change.
    /// </summary>
    public async Task<WorkflowDto> RejectAsync(CompleteWorkflowTaskCommand command, CancellationToken cancellationToken)
    {
        var workflow = await LoadWorkflowAsync(command.WorkflowId, cancellationToken);
        workflow.RejectTask(command.TaskId, command.Actor, command.Comment);
        await UpdateDocumentStatusAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Workflow task {TaskId} rejected by {Actor}.", command.TaskId, command.Actor);
        return Map(workflow);
    }

    /// <summary>
    /// Loads a document aggregate and converts missing data into an application exception.
    /// </summary>
    private async Task<Domain.Documents.Document> LoadDocumentAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _documents.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Document '{id}' was not found.");
    }

    /// <summary>
    /// Applies workflow completion state back to the related document.
    /// </summary>
    private async Task UpdateDocumentStatusAsync(WorkflowInstance workflow, CancellationToken cancellationToken)
    {
        if (workflow.Tasks.Any(task => task.Status == WorkflowTaskStatus.Rejected))
        {
            var rejectedDocument = await LoadDocumentAsync(workflow.DocumentId, cancellationToken);
            rejectedDocument.Reject();
            return;
        }

        if (workflow.Tasks.All(task => task.Status == WorkflowTaskStatus.Approved))
        {
            var approvedDocument = await LoadDocumentAsync(workflow.DocumentId, cancellationToken);
            approvedDocument.Approve();
        }
    }

    /// <summary>
    /// Loads a workflow aggregate and converts missing data into an application exception.
    /// </summary>
    private async Task<WorkflowInstance> LoadWorkflowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _workflows.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Workflow '{id}' was not found.");
    }

    /// <summary>
    /// Validates workflow start input before creating the aggregate.
    /// </summary>
    private static void ValidateStart(StartWorkflowCommand command)
    {
        if (command.DocumentId == Guid.Empty)
        {
            throw new ValidationException("Document id is required.");
        }

        if (string.IsNullOrWhiteSpace(command.TemplateKey))
        {
            throw new ValidationException("Template key is required.");
        }

        if (string.IsNullOrWhiteSpace(command.StartedBy))
        {
            throw new ValidationException("Starter is required.");
        }
    }

    /// <summary>
    /// Maps a workflow aggregate to a workflow DTO.
    /// </summary>
    private static WorkflowDto Map(WorkflowInstance workflow)
    {
        return new WorkflowDto(
            workflow.Id,
            workflow.DocumentId,
            workflow.TemplateKey,
            workflow.StartedBy,
            workflow.CreatedAt,
            workflow.Tasks.Select(MapTask).ToList());
    }

    /// <summary>
    /// Maps a workflow task entity to a workflow task DTO.
    /// </summary>
    private static WorkflowTaskDto MapTask(WorkflowTask task)
    {
        return new WorkflowTaskDto(
            task.Id,
            task.WorkflowInstanceId,
            task.Name,
            task.AssignedTo,
            task.Status.ToString(),
            task.CompletedBy,
            task.Comment,
            task.CompletedAt);
    }
}
