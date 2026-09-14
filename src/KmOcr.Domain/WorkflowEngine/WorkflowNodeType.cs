namespace KmOcr.Domain.WorkflowEngine;

/// <summary>
/// Supported node types for the enterprise workflow designer and engine.
/// </summary>
public enum WorkflowNodeType
{
    /// <summary>
    /// Workflow start node.
    /// </summary>
    Start = 0,

    /// <summary>
    /// PaddleOCR extraction node.
    /// </summary>
    Ocr = 1,

    /// <summary>
    /// AI extraction or classification node.
    /// </summary>
    Ai = 2,

    /// <summary>
    /// Email notification node.
    /// </summary>
    Email = 3,

    /// <summary>
    /// Folder move or copy node.
    /// </summary>
    Folder = 4,

    /// <summary>
    /// Outbound webhook node.
    /// </summary>
    Webhook = 5,

    /// <summary>
    /// Conditional branch node.
    /// </summary>
    Condition = 6,

    /// <summary>
    /// Loop control node.
    /// </summary>
    Loop = 7,

    /// <summary>
    /// Human approval node.
    /// </summary>
    Approval = 8,

    /// <summary>
    /// Workflow end node.
    /// </summary>
    End = 9
}
