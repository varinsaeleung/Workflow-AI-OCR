namespace KmOcr.Application.Common;

/// <summary>
/// Exception used when an application use case cannot find a requested resource.
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>
    /// Creates a not found exception with a resource-specific message.
    /// </summary>
    public NotFoundException(string message)
        : base(message)
    {
    }
}
