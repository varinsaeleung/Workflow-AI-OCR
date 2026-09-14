namespace KmOcr.Application.WorkflowEngine;

/// <summary>
/// Command data required to create a workflow definition.
/// </summary>
public sealed record CreateWorkflowDefinitionCommand(string Code, string Name, string CreatedBy);
