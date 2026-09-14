namespace KmOcr.Application.Common;

/// <summary>
/// Exception used when user input violates an application rule.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>
    /// Creates a validation exception with a client-safe message.
    /// </summary>
    public ValidationException(string message)
        : base(message)
    {
    }
}
