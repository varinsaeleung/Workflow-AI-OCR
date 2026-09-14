namespace KmOcr.Application.Auth;

/// <summary>
/// Centralizes permission policy codes used by API authorization and seed data.
/// </summary>
public static class PermissionCodes
{
    /// <summary>
    /// Allows reading documents and document-derived OCR or AI output.
    /// </summary>
    public const string DocumentsRead = "documents.read";

    /// <summary>
    /// Allows creating, updating, and reprocessing documents.
    /// </summary>
    public const string DocumentsWrite = "documents.write";

    /// <summary>
    /// Allows running AI extraction against documents.
    /// </summary>
    public const string DocumentsAi = "documents.ai";

    /// <summary>
    /// Allows soft-deleting documents.
    /// </summary>
    public const string DocumentsDelete = "documents.delete";

    /// <summary>
    /// Allows reading workflow instances and tasks.
    /// </summary>
    public const string WorkflowRead = "workflow.read";

    /// <summary>
    /// Allows starting and assigning workflow work.
    /// </summary>
    public const string WorkflowWrite = "workflow.write";

    /// <summary>
    /// Allows approving and rejecting workflow tasks.
    /// </summary>
    public const string WorkflowApprove = "workflow.approve";

    /// <summary>
    /// Allows reading operational dashboard metrics.
    /// </summary>
    public const string DashboardRead = "dashboard.read";

    /// <summary>
    /// Allows user and role administration.
    /// </summary>
    public const string AdminUsers = "admin.users";

    /// <summary>
    /// Gets every platform permission code.
    /// </summary>
    public static IReadOnlyList<string> All { get; } =
    [
        DocumentsRead,
        DocumentsWrite,
        DocumentsAi,
        DocumentsDelete,
        WorkflowRead,
        WorkflowWrite,
        WorkflowApprove,
        DashboardRead,
        AdminUsers
    ];

    /// <summary>
    /// Returns the module name for a permission code.
    /// </summary>
    public static string GetModule(string code)
    {
        return code.Split('.', 2, StringSplitOptions.TrimEntries)[0];
    }

    /// <summary>
    /// Returns a readable permission name for seed data.
    /// </summary>
    public static string GetName(string code)
    {
        return code.Replace('.', ' ');
    }
}
